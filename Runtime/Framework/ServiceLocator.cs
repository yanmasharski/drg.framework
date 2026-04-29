namespace DRG.Framework
{
    using DRG.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Hierarchical dependency container. Each module owns its own locator scope.
    /// A child locator can resolve services from its parent, but not the other way around.
    /// </summary>
    public class ServiceLocator : IServiceLocator
    {
        private readonly ServiceLocator _parent;
        private readonly Dictionary<Type, object> _services = new();
        private readonly List<ModuleNode> _childNodes = new();

        public IModuleSignalBus OwnerBus { get; set; }

        public ServiceLocator(ServiceLocator parent = null)
        {
            _parent = parent;
        }

        public void Register<T>(T service) where T : class
            => _services[typeof(T)] = service;

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            if (_parent != null) return _parent.Get<T>();
            throw new InvalidOperationException($"[ServiceLocator] {typeof(T).Name} not registered in any scope.");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
            if (_parent != null) return _parent.TryGet(out service);
            service = null;
            return false;
        }

        internal void RegisterNode(ModuleNode node) => _childNodes.Add(node);

        public IReadOnlyList<IModuleNode> GetChildNodes() => _childNodes;
    }
}
