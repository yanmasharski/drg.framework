namespace DRG.Framework
{
    using DRG.Core;
    using System;

    /// <summary>
    /// Base contract for domain modules. Pure C# — no Unity dependencies.
    /// Implementations define one module per domain area (Inventory, Combat, SlotMachine, etc.).
    /// </summary>
    public interface IModuleNode : IDisposable
    {
        string ModuleId { get; }

        ModuleState State { get; }

        /// <summary>
        /// Transitions the module from Uninitialized → Ready (or Failed on exception).
        /// Safe to call multiple times — idempotent after the first call.
        /// </summary>
        void Initialize();

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
