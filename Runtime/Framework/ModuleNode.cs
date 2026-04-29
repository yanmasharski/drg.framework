namespace DRG.Framework
{
    using DRG.Core;
    using DRG.Logs;
    using System;

    public enum ModuleState
    {
        Unknown = 0,
        Uninitialized,
        Loading,
        Ready,
        Disabled,
        Failed
    }

    /// <summary>
    /// Base class for all domain modules. Pure C# — no Unity dependencies.
    ///
    /// Rules:
    ///   - One module = one domain area (Inventory, Combat, SlotMachine, ...)
    ///   - A module does not know about its siblings or parent directly
    ///   - Dependencies are resolved in OnInitialize() via Locator.Get/TryGet
    ///   - Always register by interface: Locator.Register&lt;IMyModule&gt;(this)
    ///   - Signals are readonly structs emitted via Bus.Emit()
    ///   - No direct Unity API calls (Transform, GameObject, etc.)
    /// </summary>
    public abstract class ModuleNode : IModuleNode
    {
        public string ModuleId { get; }
        public ModuleState State { get; private set; } = ModuleState.Uninitialized;

        protected readonly IServiceLocator Locator;
        protected readonly IModuleSignalBus Bus;
        protected readonly ILogger Logger;

        protected ModuleNode(string moduleId, ServiceLocator parentLocator, ILogger logger)
        {
            ModuleId = moduleId;
            Logger = logger;

            Locator = new ServiceLocator(parentLocator);

            var parentBus = parentLocator?.OwnerBus;
            Bus = new ModuleSignalBus(moduleId, logger, parentBus);
            Locator.OwnerBus = Bus;

            parentLocator?.RegisterNode(this);
        }

        public void Initialize()
        {
            if (State != ModuleState.Uninitialized) return;
            State = ModuleState.Loading;
            try
            {
                OnInitialize();
                State = ModuleState.Ready;
            }
            catch (Exception e)
            {
                State = ModuleState.Failed;
                Logger.LogError($"[{ModuleId}] Init failed: {e.Message}");
            }
        }

        public void Disable()
        {
            State = ModuleState.Disabled;
            OnDisable();
        }

        public void Tick(float deltaTime)
        {
            if (State != ModuleState.Ready) return;
            OnTick(deltaTime);
            foreach (var child in Locator.GetChildNodes())
                child.Tick(deltaTime);
        }

        public void FlushAllBuses()
        {
            foreach (var child in Locator.GetChildNodes())
                child.FlushAllBuses();
            Bus.FlushSignals();
        }

        public IModuleSignalBus FindBus(string moduleId)
        {
            if (ModuleId == moduleId) return Bus;
            foreach (var child in Locator.GetChildNodes())
            {
                var found = child.FindBus(moduleId);
                if (found != null) return found;
            }
            return null;
        }

        protected abstract void OnInitialize();
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnDisable() { }

        public virtual void Dispose() { }
    }
}
