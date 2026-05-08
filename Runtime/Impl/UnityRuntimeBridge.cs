using UnityEngine;
using ILogger = DRG.Core.Logs.ILogger;

namespace DRG.Framework
{
	/// <summary>
	/// The only MonoBehaviour in the framework core. Owns the module tree lifecycle.
	///
	/// Usage: create a subclass in your game project and override SetupModules()
	/// to register platform services and kick off the module tree.
	///
	///   public class GameEntryPoint : UnityRuntimeBridge
	///   {
	///       public override IServiceLocator ServiceLocator { get; } = new ServiceLocator();
	///       protected override ILogger Logger { get; } = new LoggerUnity(ILogger.LogLevel.Debug);
	///
	///       protected override void SetupModules(IModuleNode root, IServiceLocator locator, ILogger logger)
	///       {
	///           locator.Register&lt;IMainThreadDispatcher&gt;(new MainThreadDispatcherAdapter());
	///           locator.Register&lt;IConsentPlatform&gt;(new ConsentPlatformApplovin(locator.Get&lt;IMainThreadDispatcher&gt;(), logger));
	///           _ = root.InitializeAsync(); // starts the async module tree
	///       }
	///   }
	///
	/// Attach it to a persistent GameObject in the scene — it calls DontDestroyOnLoad automatically.
	/// </summary>
	public abstract class UnityRuntimeBridge : MonoBehaviour
	{
		public static UnityRuntimeBridge instance { get; private set; }

		public abstract IModuleServiceLocator serviceLocator { get; }
		protected abstract ILogger logger { get; }
		protected IModuleNode Root { get; private set; }

		protected virtual void Awake()
		{
			instance = this;
			DontDestroyOnLoad(gameObject);

			serviceLocator.Register(logger);

			Root = new RootModule(serviceLocator, logger);

			SetupModules(Root, serviceLocator, logger);
		}

		protected virtual void Update()
		{
			if (Root?.state != ModuleState.Ready)
			{
				return;
			}

			Root.Tick(Time.deltaTime);
			Root.FlushAllBuses();
		}

		protected virtual void OnDestroy() => Root?.Dispose();

		/// <summary>
		/// Override to register platform services and start the module tree.
		/// Call _ = root.InitializeAsync() when you're ready to begin initialization.
		/// </summary>
		protected abstract void SetupModules(IModuleNode root, IModuleServiceLocator locator, ILogger logger);

		/// <summary>
		/// Returns the signal bus for a module by id.
		/// </summary>
		public IModuleSignalBus GetModuleBus(string moduleId) => Root?.FindBus(moduleId);
	}
}
