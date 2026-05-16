# Changelog

All notable changes to this package are documented here.

The project follows Semantic Versioning. Versions before `1.0.0` may still
change public API between minor releases.

This changelog was reset for the renovated `0.5.0` package. Earlier preview
history is intentionally omitted because the runtime and Workbench architecture
were substantially rebuilt.

## [0.5.0] - 2026-05-16

First public release candidate for the redesigned Stratum package.

### Added

- Added the current Manager runtime core:
  - `IManager`
  - `BaseManager<TConfig, TData>`
  - `BaseManagerConfig<TData>`
  - `BaseManagerData`
  - `IAsyncInitManager`
  - `IGameBoot`
- Added Addressables-backed runtime boot through `GameLifetimeScope` and
  `GameBootstrap`.
- Added `ManagerOrderConfig` and `ManagerOrderEntry` for ordered runtime
  Manager registration.
- Added `FrameworkLoader` for async and sync Addressables asset loading with
  instantiated runtime copies.
- Added runtime field metadata attributes used by editor controls:
  `FieldAttribute`, `DropdownAttribute` and `ExpandableAttribute`.
- Added the rebuilt Dev Workbench at `Tools > Stratum > Dev Workbench`.
- Added `Framework > Sync`, which executes parameterless `[EditorSync]`
  methods manually, on Workbench close or before Play Mode.
- Added Manager Workbench pages:
  - `Manager > Viewer` for browsing Manager folders, source and config tables.
  - `Manager > Creator` for scaffolding Manager source, generated partials,
    config assets, refresher stubs and Addressables entries.
  - `Manager > Order` for syncing and editing runtime Manager order.
  - `Manager > Installer` for optional built-in Manager templates.
- Added ScriptableObject Workbench support under `ScriptableObject > Viewer`,
  including SO type creation, first-asset creation, asset add/remove/rename and
  table-based asset browsing.
- Added built-in Manager templates:
  - `AssetManager` for Addressables asset loading and reference-counted
    release.
  - `EventManager` for typed sync and async publish/subscribe.
  - `MessageManager` for ID-based sync and async request dispatch.
  - `TaskManager` for frame-driven action, wait, loop and lifetime tasks.
- Added reusable public editor controls in `Stratum.Editor`:
  `ListControl`, `TableControl`, `TreeControl`, `TextControl`,
  `DropdownPopup` and `FieldPopup`.
- Added local editor-control examples under `Assets/StratumWorkbenchExamples`.
- Added package and repository documentation for the new `0.5.0` architecture.

### Changed

- Reworked the package focus from the old Manager/Component/Prefab framework
  into a simpler Manager plus ScriptableObject workflow.
- Rebuilt the generated host layout around:
  - `Assets/Game/Frame`
  - `Assets/Game/Manager`
  - `Assets/Game/ScriptableObject`
  - `Assets/Game/Editor`
- Moved generated Manager data into user-authored partial files plus generated
  base-class partials, keeping editable data fields outside generated code.
- Simplified Manager config storage to `BaseManagerConfig<TData>.DataList` and
  the `IManagerConfig.RawDataList` editor access point.
- Updated Manager config Addressables convention to
  `ManagerConfig/<ManagerName>`.
- Updated boot registration so Managers are registered as `IManager` and as
  their implemented interfaces.
- Updated Workbench page discovery to use public `IPage` implementations and a
  persisted, reorderable `PageOrder.asset`.
- Updated Addressables helper behavior to create missing groups and copy
  schema setup from the default group when possible.
- Updated repository README, package README, contribution notes and issue
  template examples for release `0.5.0`.

### Removed

- Removed the previous Component runtime contract, Component Workbench pages
  and Component templates.
- Removed the previous entity/component bridge and physics callback contracts.
- Removed the previous Prefab Workbench and Prefab Manager template.
- Removed the previous Addressable group viewer/order Workbench page.
- Removed `ComponentOrderConfig` and `AddressableGroupOrderConfig`.
- Removed old generated host `Assets/Game/Component` and `Assets/Game/Prefab`
  layout expectations.

### Fixed

- Manager order sync now removes stale entries, adds newly compiled Managers
  and backfills assembly-qualified names for runtime resolution.
- Manager Viewer now opens matching refresher scripts from each Manager config
  table toolbar.
- Manager Creator now detects existing source/config targets across the Manager
  tree before deciding whether to create or skip files.
- Post-compile asset creation now schedules Manager and SO assets when a newly
  generated type is not yet available in the current domain.
- SO type resolution now falls back from existing assets to matching scripts
  and invalidates caches on project changes.
- Table control layout, rendering and row movement behavior were hardened for
  the new Manager and SO table workflows.
