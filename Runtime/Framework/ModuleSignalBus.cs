namespace DRG.Framework
{
    using DRG.Core;
    using DRG.Logs;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Scoped signal bus for a single module. Signals are queued with Emit() and dispatched
    /// in batch by FlushSignals(). After local dispatch, each signal automatically bubbles
    /// up to the parent module's bus.
    ///
    /// This is intentionally different from DRG.Core.SignalBus:
    ///   - DRG.Core.SignalBus: Emit()/FlushSignals(), thread-safe, no scope, for app-level events
    ///   - ModuleSignalBus:    Emit()/FlushSignals(), per-module scope, signals bubble to parent
    /// </summary>
    public class ModuleSignalBus : IModuleSignalBus
    {
        public string ScopeId { get; }

        private readonly ILogger _logger;
        private readonly IModuleSignalBus _parentBus;

        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly Queue<Action> _dispatchQueue = new();

        public ModuleSignalBus(string scopeId, ILogger logger, IModuleSignalBus parentBus = null)
        {
            ScopeId = scopeId;
            _logger = logger;
            _parentBus = parentBus;
        }

        public void Subscribe<T>(ISignalBus.SignalHandler<T> handler) where T : ISignal
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type)) _handlers[type] = new List<Delegate>();
            _handlers[type].Add(handler);
        }

        public void Unsubscribe<T>(ISignalBus.SignalHandler<T> handler) where T : ISignal
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public void ClearSignalListeners<T>() where T : ISignal
            => _handlers.Remove(typeof(T));

        public void ClearAllListeners()
            => _handlers.Clear();

        public void Emit<T>(T signal) where T : ISignal
        {
            _dispatchQueue.Enqueue(() => DispatchTyped(signal));
        }

        public void FlushSignals()
        {
            while (_dispatchQueue.Count > 0)
            {
                var dispatch = _dispatchQueue.Dequeue();
                dispatch();
            }
        }

        private void DispatchTyped<T>(T signal) where T : ISignal
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var handlers))
            {
                var snapshot = new List<Delegate>(handlers);
                foreach (var h in snapshot)
                {
                    try
                    {
                        ((ISignalBus.SignalHandler<T>)h)(signal);
                    }
                    catch (Exception e)
                    {
                        _logger.LogException(e);
                    }
                }
            }

            // Bubbling: enqueue into parent's queue, not dispatched immediately.
            _parentBus?.Emit(signal);
        }
    }
}
