# Known Issues — drg.framework

> Code review: 2026-05-27. Ordered by severity.

## Critical

- [ ] Add thread safety to `ModuleServiceLocator` (`Register` / `TryGet`) (`ModuleServiceLocator.cs`)
- [ ] Fix `ModuleNode` null `parentLocator` — guard before `CreateChildLocator()` (`ModuleNode.cs:32-43`)

## Major

- [ ] Cascade `ModuleNode.Dispose()` to children — clear bus listeners and `StateChanged` (`ModuleNode.cs:148`)
- [ ] Log full exception (`e.ToString()`) on `InitializeAsync` failure (`ModuleNode.cs:65`)
- [ ] Align `ModuleNode.Disable()` ordering with `Reset()` — call `OnDisable()` before state change (`ModuleNode.cs:80-84`)
- [ ] Move `LoggerUnity` out of `DRG.Core.Logs` namespace (layering) (`LoggerUnity.cs:4`)
- [ ] Guard `UnityRuntimeBridge` against duplicate instances in `Awake()` (`UnityRuntimeBridge.cs:29,37`)
