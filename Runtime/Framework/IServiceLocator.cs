namespace DRG.Framework
{
    using DRG.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Hierarchical dependency container. Each module owns its own locator scope.
    /// A child locator can resolve services from its parent, but not the other way around.
    /// </summary>
    public interface IServiceLocator
    {
        IModuleSignalBus OwnerBus { get; set; }

        /// <summary>
        /// Registers a service by its interface type.
        /// Always register by interface: Register&lt;IMyModule&gt;(this).
        /// </summary>
        void Register<T>(T service) where T : class;

        /// <summary>
        /// Hard dependency — throws if the service is not registered anywhere in the scope chain.
        /// </summary>
        T Get<T>() where T : class;

        /// <summary>
        /// Soft dependency — returns false if not found. Module degrades gracefully.
        /// </summary>
        bool TryGet<T>(out T service) where T : class;

        IReadOnlyList<IModuleNode> GetChildNodes();
    }
}
