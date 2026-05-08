using System;
using System.Collections.Generic;

namespace DRG.Framework
{
	/// <summary>
	/// Hierarchical dependency container. Each module owns its own locator scope.
	/// A child locator can resolve services from its parent, but not the other way around.
	/// </summary>
	public class ModuleServiceLocator : IModuleServiceLocator
	{
		private readonly IModuleServiceLocator _parent;
		private readonly Dictionary<Type, object> _services = new();
		private readonly List<IModuleNode> _childNodes = new();

		public IModuleSignalBus ownerBus { get; set; }

		public ModuleServiceLocator(IModuleServiceLocator parent = null)
		{
			_parent = parent;
		}

		public void Register<T>(T service) where T : class
			=> _services[typeof(T)] = service;

		public bool TryGet<T>(out T service) where T : class
		{
			if (_services.TryGetValue(typeof(T), out var s))
			{
				service = (T)s;
				return true;
			}

			if (_parent != null)
			{
				return _parent.TryGet(out service);
			}

			service = null;
			return false;
		}

		public void RegisterNode(IModuleNode node) => _childNodes.Add(node);

		public IReadOnlyList<IModuleNode> GetChildNodes() => _childNodes;

		public IModuleServiceLocator CreateChildLocator() => new ModuleServiceLocator(this);
	}
}
