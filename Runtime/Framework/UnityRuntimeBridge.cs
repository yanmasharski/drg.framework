namespace DRG.Framework
{
    using DRG.Core;
    using DRG.Logs;
    using UnityEngine;

    /// <summary>
    /// The only MonoBehaviour in the framework core. Owns the module tree lifecycle.
    ///
    /// Usage: create a subclass in your game project and override SetupModules()
    /// to register game-specific modules before Initialize() is called.
    ///
    ///   public class GameEntryPoint : UnityRuntimeBridge
    ///   {
    ///       protected override void SetupModules(RootModule root, ServiceLocator locator, ILogger logger)
    ///       {
    ///           new InventoryModule("inventory", locator, logger);
    ///           new CombatModule("combat", locator, logger);
    ///       }
    ///   }
    ///
    /// Attach it to a persistent GameObject in the scene — it calls DontDestroyOnLoad automatically.
    /// </summary>
    public abstract class UnityRuntimeBridge : MonoBehaviour
    {
        public static UnityRuntimeBridge Instance { get; private set; }

        protected RootModule Root { get; private set; }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var logger = new LoggerUnity();
            var rootLocator = new ServiceLocator();
            Root = new RootModule(rootLocator, logger);

            SetupModules(Root, rootLocator, logger);

            Root.Initialize();
        }

        void Update()
        {
            if (Root?.State != ModuleState.Ready) return;

            Root.Tick(Time.deltaTime);
            Root.FlushAllBuses();
        }

        void OnDestroy() => Root?.Dispose();

        /// <summary>
        /// Override to register game-specific modules before the tree is initialized.
        /// </summary>
        protected abstract void SetupModules(RootModule root, ServiceLocator locator, ILogger logger);

        /// <summary>
        /// Returns the signal bus for a module by id.
        /// </summary>
        public IModuleSignalBus GetModuleBus(string moduleId) => Root?.FindBus(moduleId);
    }
}
