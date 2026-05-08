using System.Collections.Generic;
using DRG.Core;

namespace DRG.Framework
{

	/// <summary>
	/// Hierarchical dependency container. Each module owns its own locator scope.
	/// A child locator can resolve services from its parent, but not the other way around.
	/// </summary>
	public interface IModuleServiceLocator : IServiceLocator
	{
		IModuleSignalBus ownerBus { get; set; }

		IModuleServiceLocator CreateChildLocator();

		IReadOnlyList<IModuleNode> GetChildNodes();

		void RegisterNode(IModuleNode node);
	}
}
