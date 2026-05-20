# Changelog

All notable changes to this package are documented here.

The project follows Semantic Versioning. Versions before `1.0.0` may still
change public API between minor releases.

This changelog was reset for the renovated `0.5.0` package. Earlier preview
history is intentionally omitted because the runtime and Workbench architecture
were substantially rebuilt.

## [Unreleased]

### Changed

- Renamed the generated host boot layout and assembly from
  `Assets/Game/Frame` / `Game.Frame` to `Assets/Game/App` / `Game.App`.
- Renamed the generated host component layout and assembly from
  `Assets/Game/MonoBehaviour` / `Game.MonoBehaviour` to
  `Assets/Game/Component` / `Game.Component`.
- Runtime boot now loads manager order from Addressables address
  `App/ManagerOrder`.

## [0.5.3] - 2026-05-18

### Added

- Added `Manager > Import`, a lightweight Workbench tab that scans
  `Templates~/Managers/*.cs` and imports selected Manager templates directly
  into `Assets/Game/Manager`.
- Restored single-file built-in Manager templates for:
  - `EventManager`
  - `MessageManager`
  - `TaskManager`

### Changed

- Manager order now syncs when opening the `Manager > Order` tab and again
  before entering Play Mode, so newly compiled Managers are added and stale
  entries are pruned before runtime registration.
- Manager template import no longer uses a manifest or per-template folders;
  each template is a bare `.cs` file named after the Manager.
- The `Manager > Import` UI now presents a compact name-only list and uses the
  shared `ButtonControl` for importing.

## [0.5.2] - 2026-05-18

This release is a substantial simplification pass. The package drops the
config/data/refresher Manager scaffolding and the popup-driven editor
subsystem, and reorganizes the Dev Workbench around three symmetric script
generators: `Manager`, `MonoBehaviour` and `ScriptableObject`. Each generator
now writes a single `.cs` file with no companion assets.

### Added

- Added a `MonoBehaviour` Workbench page that scaffolds a single
  `class XxxComponent : MonoBehaviour` script under
  `Assets/Game/MonoBehaviour` and the new `Game.MonoBehaviour` host assembly.
- Added a unified `CreatorLayout<TState>` that drives the Manager, MonoBehaviour
  and ScriptableObject creator UI: input field on top, live source preview in
  the middle, create button pinned at the bottom.
- Added reusable `InputControl` and `ButtonControl` editor controls with the
  same `BoxDrawer` styling as the other custom controls.
- Added on-enter "default-select the root node" behavior for every Workbench
  viewer page so the right pane immediately shows the creator panel after a
  tab switch when nothing has been picked yet.
- Added new state-query / state-mutation helpers across the editor controls:
  - `TreeControl.GetSelectedPath()`
  - `TableControl.GetSelectedIndex()` and `ListControl.GetSelectedIndex()`
  - `InputControl.GetValue()` / `InputControl.SetValue(string)`

### Changed

- Switched to lightweight, single-file code generation:
  - Manager creator writes one `.cs` with `public interface IXxxManager : IManager`
    and `internal sealed class XxxManager : IXxxManager`. No more base-class
    partials, data classes, refresher stubs, config `.asset` files or
    Addressables entries.
  - ScriptableObject creator writes one `.cs` with `[CreateAssetMenu]` and
    `public class XxxConfig : ScriptableObject`.
  - MonoBehaviour creator writes one `.cs` with
    `public class XxxComponent : MonoBehaviour`.
- Reorganized the Dev Workbench around three sections: `Manager`,
  `MonoBehaviour` and `ScriptableObject`. The Manager section keeps a
  `Viewer` plus an `Order` tab; the other two each expose a `Viewer` tab.
- Renamed every editor `So*` identifier and the `Editor/Page/So` folder to
  use the full `ScriptableObject*` name, including class names, file names
  and the `WorkbenchPaths.ScriptableObjectRoot` constant. The on-disk asset
  path `Assets/Game/ScriptableObject` is unchanged.
- Renamed the `Manobehaviour` typo to `MonoBehaviour` across editor pages,
  folders and assembly definitions.
- Renamed `FieldAttribute` to `TableAttribute` and scoped it to table column
  metadata (`Title`, `Hide`, `Readonly`, `Width`).
- Unified the `Draw(Rect)` signature across every custom editor control
  (`TextControl`, `InputControl`, `ButtonControl`, `TreeControl`,
  `ListControl`, `TableControl`). Item data and configuration are exposed
  through public properties or `Set*` / `Get*` functions instead of per-frame
  arguments.
- `TableControl` is no longer generic at the class level; rows are bound
  through `IList Items` and the element type is detected via reflection so
  static state can be safely shared across owners.
- Tightened the `Game.Managers` host assembly references back to `Stratum`,
  `UniTask` and `VContainer`.

### Removed

- Removed the previous Manager scaffolding artefacts: generated base-class
  partials, manager data classes, refresher stubs, per-Manager `.asset`
  configs and the `ManagerConfig/<ManagerName>` Addressables entries.
- Removed `BaseManager<TConfig, TData>`, `BaseManagerConfig<TData>` and
  `BaseManagerData`. `IManager` is now a bare marker interface; Managers
  implement it directly (and optionally `IAsyncInitManager`).
- Removed the `Manager > Installer` Workbench page, the
  `Templates~/Managers/manifest.json` template manifest and the built-in
  `AssetManager`, `EventManager`, `MessageManager` and `TaskManager`
  templates.
- Removed `Framework > Sync` and the `[EditorSync]` attribute, along with the
  previously-required `ManagerOrderSync.Run()` entry point.
- Removed `DropdownAttribute` and `ExpandableAttribute` and the entire
  `Editor/Popup` subsystem (`DropdownPopup`, `FieldPopup`,
  `DropdownAttributeResolver`) and `TableControl.Popups`.
- Removed `TextControl.WordWrap`. Long preview text now scrolls horizontally
  rather than wrapping.
- Removed the briefly-introduced `Container` Workbench page, the
  `Game.Container` template assembly and the `Game.Managers → Game.Container`
  reference. Manager-bound interfaces can be defined directly inside the
  Manager file when needed.
- Removed `Assets/StratumWorkbenchExamples` and any editor code paths that
  depended on the deleted popup APIs.

### Fixed

- Fixed stale `TextField` display when switching Workbench pages by clearing
  keyboard focus on every page switch. Typing in one page's input no longer
  leaks the text into a sibling page's input control.
- Fixed `CreatorLayout` reset behavior so the input field clears after a
  successful create by routing the assignment through
  `InputControl.SetValue` instead of the removed `Value` property.

## [0.5.1] - 2026-05-16

### Changed

- Refreshed the built-in Manager templates from the dogfooded
  `Assets/Game/Manager` implementation, including root assembly definition
  files, generated partials, editor refreshers, config assets and leaf markers.
- Updated `AssetManager` to expose direct address-based loading through
  `LoadAsync<T>(address)` and global release through `ReleaseAllAsync()`.

### Fixed

- Fixed VContainer Manager registration so implementations are not registered
  twice as `IManager` when `.AsImplementedInterfaces()` already includes the
  base Manager contract.
- Hardened `AssetManager` against cache collisions for the same address loaded
  as different types, stale failed-load cache entries and release-all races
  while pending loads complete.
- Hardened `EventManager` async publish timeout handling so preserved publish
  tasks can be awaited safely and cancellation token sources are not disposed
  while callbacks may still observe them.
- Hardened `MessageManager` cancellation cleanup against already-disposed
  cancellation token sources.
- Hardened `TaskManager` against re-running the same builder, collection
  mutation during stop-all and async node cancellation source disposal before
  the async operation has actually completed.

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
