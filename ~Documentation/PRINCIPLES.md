# Unity Game Framework — Architecture Rules

Use this as context when writing any code for this project.

---

## Core Philosophy

- Unity is a **host**, not a foundation. Business logic has zero Unity dependencies.
- A failure in UI, VFX, or physics **never breaks** business logic.
- Every unit of code must be understandable **without knowing the rest of the system**.

---

## Architectural Principles

These are non-negotiable. Every design decision in this framework follows from them.

**1. Direction of control is strict.**
Commands flow top-down only. A child module never calls its parent directly.
Signals flow bottom-up only. A parent never pushes data into a child via signal.
Violating direction creates hidden coupling that breaks under change.

**2. Call stacks are isolated by layer.**
An exception in UI, VFX, or the game world does not reach the business logic call stack.
Each layer catches its own exceptions. Domain logic always runs to completion.

**3. Modules are independently deployable at runtime.**
Any module can be disabled at runtime without crashing the game.
The rest of the system detects the missing module and degrades gracefully.
This is not error handling — it is a design requirement.

**4. Hard vs soft dependencies are explicit.**
Hard dependency: module cannot initialize without it. Declared with `Locator.Get<>()`.
Soft dependency: module works without it, with reduced functionality. Declared with `Locator.TryGet<>()`.
The type of dependency must be visible at the call site — no hidden assumptions.

**5. Execution phases never interleave.**
All commands for the current frame complete before any signal is processed.
A signal handler may produce new commands — those commands run in the next frame.
This makes execution order deterministic and debuggable.

**6. Signals are queued, not fired.**
`Emit()` never triggers immediate execution. It adds to a queue.
This prevents signal handlers from corrupting an in-progress command execution.

**7. SignalBus scope is enforced.**
A child module only accesses its own scope's SignalBus.
Subscribing to a parent's SignalBus is allowed but must be explicit and intentional.
A module never emits signals onto a bus it does not own.

**8. Modules are hot-reloadable via soft dependencies.**
A module with soft dependencies on a failing module can reinitialize when that module recovers.
The game does not need to restart. Players do not see a crash.

**9. Interfaces are the API surface.**
Other modules, presenters, and LLM only see interfaces — never implementations.
This is what makes the system both extensible and LLM-efficient.
Narrow interfaces are mandatory. Fat interfaces defeat the purpose.

**10. MonoBehaviour is presentation only.**
No business logic inside MonoBehaviour. It subscribes to signals, renders state, forwards input.
It is a wire between Unity and the domain — nothing more.

---

## Layers

```
Unity Runtime Layer     MonoBehaviour, VFX, UI — render and input only
Adapter Layer           Translates Unity events → domain commands
Domain Layer            Pure C#. All business logic lives here.
```

---

## ModuleNode

Every module extends `ModuleNode` (pure C#).

```csharp
public enum ModuleState { Uninitialized, Loading, Ready, Disabled, Failed }

public abstract class ModuleNode : IDisposable
{
    public ModuleState State { get; private set; }
    public string ModuleId { get; }
    public event Action<ModuleState> StateChanged;

    // ...

    public async Task InitializeAsync()
    {
        if (State != ModuleState.Uninitialized) return;
        SetState(ModuleState.Loading);
        try   { await OnInitializeAsync(); SetState(ModuleState.Ready); }
        catch { SetState(ModuleState.Failed); ... }
    }

    public void Reset() { OnReset(); SetState(ModuleState.Uninitialized); }

    protected virtual Task OnInitializeAsync() => Task.CompletedTask;
    protected virtual void OnReset() { }
}
```

**Rules:**
- A module does not know its parent or siblings directly.
- No Unity API calls inside a module.
- Dependencies are resolved in `OnInitializeAsync()`, not in the constructor.
- Child modules are created and awaited inside `OnInitializeAsync()`.
- One module = one domain area.
- `Reset()` + `InitializeAsync()` is the hot-reload sequence.

---

## ServiceLocator

Locator tree mirrors the module tree. A child locator can see parent services, not vice versa.

```csharp
// Hard dependency — throws if missing. Module will not reach Ready state.
_stats = Locator.Get<IPlayerStats>();

// Soft dependency — module degrades gracefully if missing.
if (Locator.TryGet<ISlotMachineModule>(out var slots)) _slots = slots;
```

Always register by interface:
```csharp
Locator.Register<IInventoryModule>(this);
```

---

## Async Initialization

A module stays in `Loading` while `OnInitializeAsync()` runs. This may span multiple frames.
`Update()` skips `Tick()` for any module that is not `Ready`.

**Pattern — waiting for an external SDK:**
```csharp
protected override async Task OnInitializeAsync()
{
    var platform = Locator.Get<IConsentPlatform>();
    await platform.InitializeAsync();          // waits for SDK callback
    Locator.Register<IConsentModule>(this);
}
```

**Pattern — sequential child modules:**
```csharp
protected override async Task OnInitializeAsync()
{
    var consent = new ConsentModule(Locator, Logger);
    await consent.InitializeAsync();           // consent must be ready first
    Locator.Register<IConsentModule>(consent);

    var ads = new AdsModule(Locator, Logger);  // resolves IConsentModule from Locator
    await ads.InitializeAsync();
}
```

**Pattern — parallel child modules:**
```csharp
var analytics   = new AnalyticsModule(Locator, Logger);
var attribution = new AttributionModule(Locator, Logger);
await Task.WhenAll(analytics.InitializeAsync(), attribution.InitializeAsync());
```

---

## IMainThreadDispatcher

SDK callbacks often fire on background threads. Platform adapters use `IMainThreadDispatcher`
to marshal results back to the main thread before resolving a `TaskCompletionSource`.

`IMainThreadDispatcher` lives in `DRG.Core` (pure C#).
`MainThreadDispatcherAdapter` in `DRG.Utils` wraps Unity's `MainThreadDispatcher`.

Register it in the root locator at startup:
```csharp
locator.Register<IMainThreadDispatcher>(new MainThreadDispatcherAdapter());
```

**Platform adapter pattern:**
```csharp
public class ConsentPlatformApplovin : IConsentPlatform
{
    private readonly IMainThreadDispatcher _dispatcher;

    public Task InitializeAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        SomeSdk.OnInitialized += () =>
            _dispatcher.Dispatch(() => tcs.TrySetResult(true));   // background → main thread → Task completes
        SomeSdk.Initialize();
        return tcs.Task;
    }
}
```

---

## Hot-Reload

`StateChanged` fires whenever a module's state changes. A parent module subscribes to
a child's `StateChanged` to orchestrate recovery.

```csharp
protected override async Task OnInitializeAsync()
{
    _consent = new ConsentModule(Locator, Logger);
    _consent.StateChanged += OnConsentStateChanged;
    await _consent.InitializeAsync();
    // ...
}

private void OnConsentStateChanged(ModuleState state)
{
    if (state == ModuleState.Failed)
        _ = RecoverAsync();
}

private async Task RecoverAsync()
{
    _consent.Reset();
    await _consent.InitializeAsync();
}
```

**Rules:**
- Recovery is always top-down (parent decides, not child).
- A module's `OnReset()` cleans up subscriptions and internal state.
- Non-reloadable modules (ads, attribution) are never Reset — they stay Ready.

---

## Interfaces

**Every module must have an interface.** This is the only thing other modules and LLM need to see.

```csharp
public interface IInventoryModule
{
    bool HasItem(string itemId);
    bool TryAddItem(string itemId, int count);
    bool TryRemoveItem(string itemId, int count);
    int GetItemCount(string itemId);
}
```

**Rules:**
- Keep interfaces narrow — only expose what other modules actually need.
- Name them `I{ModuleName}`.
- Never expose internal implementation details.

---

## Commands and Signals

**Commands** — intent flowing top-down. Direct method calls.
**Signals** — facts flowing bottom-up. Queued, never immediate.

```csharp
public interface ISignal { }

// Signals are structs, not classes.
public readonly struct ItemAddedSignal : ISignal
{
    public readonly string ItemId;
    public readonly int Count;
    public ItemAddedSignal(string itemId, int count) { ItemId = itemId; Count = count; }
}
```

**SignalBus rules:**
- `Emit()` puts a signal in the queue. It does not fire immediately.
- `FlushSignals()` is called only after all commands for the current frame are done.
- A signal handler may create new commands — they run next frame.
- A module subscribes only to its own scope's SignalBus.
- A module may also subscribe to its parent module's SignalBus.

---

## Frame Execution Order

```
1. Command Phase   — Tick() top-down through the module tree (synchronous)
2. Signal Phase    — FlushSignals() bottom-up through signal buses
```

Commands and signals never interleave within a single frame.

---

## UnityRuntimeBridge

One MonoBehaviour for the entire game. It owns the module tree.

```csharp
public class UnityRuntimeBridge : MonoBehaviour
{
    public static UnityRuntimeBridge Instance { get; private set; }
    private RootModule _root;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _root = new RootModule(new ServiceLocator());
        _ = _root.InitializeAsync(); // fire-and-forget: exceptions handled inside ModuleNode
    }

    void Update()
    {
        if (_root.State != ModuleState.Ready) return;
        _root.Tick(Time.deltaTime);      // Command Phase
        _root.FlushAllBuses();           // Signal Phase
    }

    void OnDestroy() => _root?.Dispose();

    public SignalBus GetModuleBus(string moduleId) => _root.FindBus(moduleId);
}
```

---

## Presentation Layer (MonoBehaviour)

A MonoBehaviour only renders. It subscribes to signals and updates visuals.

```csharp
// CORRECT
public class HealthBarView : MonoBehaviour
{
    private SignalBus _bus;

    void Start()
    {
        _bus = UnityRuntimeBridge.Instance.GetModuleBus("combat");
        _bus.Subscribe<HealthChangedSignal>(OnHealthChanged);
    }

    private void OnHealthChanged(HealthChangedSignal s)
        => _slider.value = (float)s.Current / s.Max;

    void OnDestroy() => _bus?.Unsubscribe<HealthChangedSignal>(OnHealthChanged);
}

// WRONG — never do this
private CombatModule _combat;          // direct module reference — forbidden
void Update() { _slider.value = _combat.Health; }  // polling — forbidden
```

---

## Disabling a Module Without a Crash

```csharp
private void OpenSlotMachine()
{
    if (_slotMachine == null || _slotMachine.State != ModuleState.Ready)
    {
        SignalBus.Emit(new FeatureUnavailableSignal("slot_machine"));
        return;
    }
    _slotMachine.Open();
}
```

---

## File Structure

```
Assets/
├── _Framework/              ← core, do not modify
│   ├── ModuleNode.cs
│   ├── ServiceLocator.cs
│   ├── SignalBus.cs
│   ├── UnityRuntimeBridge.cs
│   ├── ISignal.cs
│   └── ICommand.cs
│
└── Game/
    ├── Modules/
    │   └── Inventory/
    │       ├── IInventoryModule.cs    ← interface first
    │       ├── InventoryModule.cs
    │       └── Signals/
    │           └── ItemAddedSignal.cs
    └── Presentation/
        └── InventoryView.cs
```

---

## Checklist — New Module

- [ ] Interface `I{ModuleName}` exists with minimal public API
- [ ] Class extends `ModuleNode` and implements the interface
- [ ] Dependencies resolved in `OnInitializeAsync()` via `Locator.Get<>()` or `TryGet<>()`
- [ ] Registers itself: `Locator.Register<IMyModule>(this)`
- [ ] Child modules created and awaited inside `OnInitializeAsync()`
- [ ] `OnReset()` cleans up if the module supports hot-reload
- [ ] Signals are `readonly struct`
- [ ] No Unity API references inside the module
- [ ] No direct references to other module classes — interfaces only

---

## What to Give LLM for a Task

1. This file (or relevant sections)
2. The interface of the module being used
3. The signals related to the task
4. The task itself

Do not provide module implementations — interfaces only.