using System;
using System.Threading.Tasks;

namespace DRG.Framework
{
	/// <summary>
	/// Base contract for domain modules. Pure C# — no Unity dependencies.
	/// Implementations define one module per domain area (Inventory, Combat, SlotMachine, etc.).
	/// </summary>
	public interface IModuleNode : IDisposable
	{
		string moduleId { get; }

		ModuleState state { get; }

		/// <summary>
		/// Fires whenever State changes. Used by parent modules to react to child state transitions
		/// (e.g. trigger hot-reload when a dependency recovers from Failed).
		/// </summary>
		event Action<ModuleState> StateChanged;

		/// <summary>
		/// Transitions the module from Uninitialized → Ready (or Failed on exception).
		/// Async — may span multiple frames while waiting for dependencies or external SDKs.
		/// Safe to call after Reset() for hot-reload.
		/// </summary>
		Task InitializeAsync();

		/// <summary>
		/// Returns the module to Uninitialized so InitializeAsync() can be called again.
		/// Used for hot-reload: reset → wait for dependencies → InitializeAsync().
		/// </summary>
		void Reset();

		void Disable();

		/// <summary>
		/// Drives the module tree depth-first (Command Phase).
		/// </summary>
		void Tick(float deltaTime);

		/// <summary>
		/// Flushes signal buses depth-first — leaves first, root last (Signal Phase).
		/// </summary>
		void FlushAllBuses();

		/// <summary>
		/// Finds a module's bus by id in the subtree rooted at this node.
		/// Used by Presenter MonoBehaviours to subscribe to domain events.
		/// </summary>
		IModuleSignalBus FindBus(string moduleId);
	}
}
