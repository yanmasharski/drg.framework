# DRG Framework

Module tree, hierarchical DI, scoped signal buses, and Unity runtime bridge.

## Assemblies

| Assembly | Contains |
|---|---|
| `DRG.Framework` | `IModuleNode`, `IModuleServiceLocator`, `IModuleSignalBus`, `ModuleState` |
| `DRG.Framework.Runtime` | `ModuleNode`, `ModuleServiceLocator`, `ModuleSignalBus`, `RootModule`, `UnityRuntimeBridge`, `LoggerUnity` |

## Key types

- **`ModuleNode`** — abstract base class for domain modules. Override `OnInitializeAsync()` to initialize, `OnTick()` for per-frame logic. Pure C# — no Unity API.
- **`UnityRuntimeBridge`** — MonoBehaviour entry point. Subclass it in your game, override `SetupModules()` to wire services and start the module tree.
- **`ModuleServiceLocator`** — hierarchical DI scope. Child locators can resolve services from parents, not vice versa.
- **`ModuleSignalBus`** — scoped signal bus per module. Signals bubble up to the parent bus after local dispatch.
- **`LoggerUnity`** — `ILogger` implementation backed by `UnityEngine.Debug`.

## Module lifecycle

```
Uninitialized → Loading → Ready
                        ↘ Failed
Ready → Disabled
Ready → Uninitialized  (Reset + re-init for hot-reload)
```

## Usage

```csharp
public class GameEntryPoint : UnityRuntimeBridge
{
    public override IModuleServiceLocator ServiceLocator { get; } = new ModuleServiceLocator();
    protected override ILogger Logger { get; } = new LoggerUnity(ILogger.LogLevel.Debug);

    protected override void SetupModules(IModuleNode root, IModuleServiceLocator locator, ILogger logger)
    {
        locator.Register<IMainThreadDispatcher>(new MainThreadDispatcherAdapter());
        _ = root.InitializeAsync();
    }
}
```

## Dependencies

- `com.drg.core`

## Install

```
https://github.com/yanmasharski/drg.framework.git#1.0.0
```
