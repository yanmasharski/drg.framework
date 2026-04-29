namespace DRG.Framework
{
    using DRG.Logs;

    /// <summary>
    /// The root of the module tree. Created and owned by UnityRuntimeBridge.
    /// Game-specific modules are registered as children during SetupModules().
    /// </summary>
    public class RootModule : ModuleNode
    {
        public RootModule(ServiceLocator locator, ILogger logger)
            : base("root", locator, logger) { }

        protected override void OnInitialize() { }
    }
}
