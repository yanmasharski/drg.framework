using System;
using System.Threading.Tasks;
using DRG.Core;
using DRG.Core.Logs;

namespace DRG.Framework
{


	/// <summary>
	/// Base class for all domain modules. Pure C# — no Unity dependencies.
	///
	/// Rules:
	///   - One module = one domain area (Inventory, Combat, SlotMachine, ...)
	///   - A module does not know about its siblings or parent directly
	///   - Dependencies are resolved in OnInitializeAsync() via Locator.Get/TryGet
	///   - Always register by interface: Locator.Register&lt;IMyModule&gt;(this)
	///   - Child modules are created and awaited inside OnInitializeAsync()
	///   - Signals are readonly structs emitted via Bus.Emit()
	///   - No direct Unity API calls (Transform, GameObject, etc.)
	/// </summary>
	public abstract class ModuleNode : IModuleNode
	{
		public string moduleId { get; }
		public ModuleState state { get; private set; } = ModuleState.Uninitialized;
		public event Action<ModuleState> StateChanged;

		protected readonly IModuleServiceLocator Locator;
		protected readonly IModuleSignalBus Bus;
		protected readonly ILogger Logger;

		protected ModuleNode(string moduleId, IModuleServiceLocator parentLocator, ILogger logger)
		{
			this.moduleId = moduleId;
			Logger = logger;

			Locator = parentLocator.CreateChildLocator();

			var parentBus = parentLocator?.ownerBus;
			Bus = new ModuleSignalBus(moduleId, logger, parentBus);
			Locator.ownerBus = Bus;

			parentLocator?.RegisterNode(this);
		}

		public async Task InitializeAsync()
		{
			if (state != ModuleState.Uninitialized)
			{
				return;
			}

			SetState(ModuleState.Loading);
			try
			{
				await OnInitializeAsync();
				SetState(ModuleState.Ready);
			}
			catch (Exception e)
			{
				SetState(ModuleState.Failed);
				Logger.LogError($"[{moduleId}] Init failed: {e.Message}");
			}
		}

		public void Reset()
		{
			if (state == ModuleState.Uninitialized)
			{
				return;
			}

			OnReset();
			SetState(ModuleState.Uninitialized);
		}

		public void Disable()
		{
			SetState(ModuleState.Disabled);
			OnDisable();
		}

		public void Tick(float deltaTime)
		{
			if (state != ModuleState.Ready)
			{
				return;
			}

			OnTick(deltaTime);
			foreach (var child in Locator.GetChildNodes())
			{
				child.Tick(deltaTime);
			}
		}

		public void FlushAllBuses()
		{
			foreach (var child in Locator.GetChildNodes())
			{
				child.FlushAllBuses();
			}

			Bus.FlushSignals();
		}

		public IModuleSignalBus FindBus(string moduleId)
		{
			if (this.moduleId == moduleId)
			{
				return Bus;
			}

			foreach (var child in Locator.GetChildNodes())
			{
				var found = child.FindBus(moduleId);
				if (found != null)
				{
					return found;
				}
			}
			return null;
		}

		private void SetState(ModuleState state)
		{
			this.state = state;
			StateChanged?.Invoke(state);
		}

		/// <summary>
		/// Override to initialize the module. Await child modules and external SDKs here.
		/// The module stays in Loading until this completes. An exception transitions to Failed.
		/// </summary>
		protected virtual Task OnInitializeAsync() => Task.CompletedTask;

		/// <summary>
		/// Override to clean up state before a hot-reload (Reset → InitializeAsync).
		/// </summary>
		protected virtual void OnReset() { }

		protected virtual void OnTick(float deltaTime) { }
		protected virtual void OnDisable() { }

		public virtual void Dispose() { }
	}
}
