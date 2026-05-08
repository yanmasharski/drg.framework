using DRG.Core.Logs;

namespace DRG.Framework
{
	/// <summary>
	/// The root of the module tree. Created and owned by UnityRuntimeBridge.
	/// Game-specific modules are created and awaited inside OnInitializeAsync().
	/// </summary>
	public class RootModule : ModuleNode
	{
		public RootModule(IModuleServiceLocator locator, ILogger logger)
			: base("root", locator, logger) { }
	}
}
