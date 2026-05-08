using DRG.Core;

namespace DRG.Framework
{
	/// <summary>
	/// Contract for a scoped signal bus used by module nodes.
	/// Extends ISignalBus with ScopeId for hierarchical module identification.
	/// </summary>
	public interface IModuleSignalBus : ISignalBus
	{
		string scopeId { get; }
	}
}
