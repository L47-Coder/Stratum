# Changelog

All notable changes to this package will be documented in this file.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] &mdash; 2026-04-26

Stable 0.2 release. Promotes the `0.2.0-preview.1` architecture work and adds
one more hardening pass over Manager / Component scaffolding, config
registration and the Workbench editor UI before publishing the release tag.

### Changed

- **Creator and Order pages now share base implementations.**
  `CreatorShared` (`Editor/Workbench/Shared/CreatorShared.cs`) centralises the
  preview-panel draw logic used by both Manager and Component creators.
  `OrderPageBase` (`Editor/Workbench/Shared/OrderPageBase.cs`) holds the
  common table-draw and entry-management code for both Order tabs. The
  ~300-line Manager/Component mirroring in those four files is eliminated.
- **Installer pages now share one implementation**
  (`Editor/Workbench/Shared/InstallerPageBase.cs`). Manager / Component
  installer tabs keep their original behaviour and copy, but the duplicated
  selection UI, card layout, row rendering, style cache and import button
  logic now live in one shared base.
- **Order config assets share a common runtime base**
  (`Runtime/Frame/Entrance/OrderConfigBase.cs`). `ManagerOrderConfig` and
  `ComponentOrderConfig` preserve their public `Entries` surface and serialized
  `_entries` field while reusing the same list/entry infrastructure.
- **Component config addresses now use a dedicated convention helper**
  (`Editor/Tool/ComponentAddressConvention.cs`), matching the Manager-side
  `ManagerAddressConvention` and keeping address construction out of Creator
  state internals.
- **`Framework / Sync` is now only a sync pass.** `RunSync()` no longer calls
  the full framework ensure step; editor startup still owns one-time skeleton
  and Addressables initialization.

### Fixed

- **Component order entries now store assembly-qualified names**, matching
  Manager order entries and avoiding ambiguity when multiple assemblies contain
  same-named Component types.
- **Creator preview state now resolves generated types by short name** instead
  of full name, so Create / Skip state is correct for global-namespace
  host-project classes.
- **Framework guard contributors only run after ensure succeeds.** Failed
  initialization no longer proceeds into contribution execution.
- **Viewer panels show an unsupported-file message** instead of a blank right
  panel when selecting files such as `.meta`.
- **Config registration counts only real writes.** `EnsureAllRegistered()` now
  increments its change count only when `EnsureAssetAndAddressable()` succeeds.
- **Config asset existence checks use `AssetPathToGUID()`** instead of loading
  as `ScriptableObject`, so non-SO assets are not misreported as missing.
- **Config installer skips are logged.** Manager / Component config types that
  do not follow the naming convention now emit warning logs instead of being
  skipped silently.
- **Component template installer logs missing manifests**, matching Manager
  installer diagnostics.

## [0.2.0-preview.1] &mdash; 2026-04-25

Fourth preview, and the first pass of the "road to 1.0" architecture work.
This release sharpens the package's extension surface (pages, contributions,
config lists) and collapses the scattered `Assets/Game` / package-id string
literals into a single source of truth, so later releases can layer a
user-authored settings asset on top without touching call sites.

### Added

- **`IPage` is now `public`** (`Editor/Workbench/Frame/IPage.cs`). Any Editor
  assembly that references `Stratum.Editor` can contribute a new tab to
  the Dev Workbench window just by implementing `IPage`; `TypeCache`
  discovery picks it up automatically. Full XML docs describe the contract
  and the expected no-op default implementations.
- **`IWorkbenchContribution`** (`Editor/Workbench/Frame/IWorkbenchContribution.cs`).
  A dedicated, single-method interface for project-level `ensure` work
  (asset registration, order-asset sync, &hellip;). `DevWindowFrameworkGuard`
  now fans out to `IWorkbenchContribution` implementations instead of every
  `IPage`, so adding a page no longer implicitly subscribes it to the global
  first-open pass.
- **`IConfigListOwner`** (`Runtime/Frame/Contract/IConfigListOwner.cs`).
  Explicit contract that lets tooling read a config asset's underlying list
  (`GetConfigList()` / `ConfigItemType`) without reflecting into a private
  `_configs` backing field. Generated Manager / Component config partials
  now implement this interface.
- **Centralized path constants**
  (`Editor/Workbench/Frame/GameProjectPaths.cs`,
  `Editor/Workbench/Frame/StratumPackageInfo.cs`). Single source of
  truth for `Assets/Game{,/Frame,/Manager,/Component}` and
  `Packages/com.l47coder.stratum/Runtime~/Templates/*`. All Creators,
  Installers, Viewers, Guards and user-visible hints resolve through these
  constants.
- **`ManagerWorkbenchContribution` / `ComponentWorkbenchContribution`**
  (`Editor/Workbench/Page/Manager/`, `Editor/Workbench/Page/Component/`).
  Host the subsystem-level ensure work (`EnsureAllRegistered` + `OrderSync`)
  that previously lived on the Viewer pages' `OnWorkbenchOpen`.

### Changed

- **`BaseManagerConfig` / `BaseComponentConfig` viewers** no longer use
  `BindingFlags.NonPublic` reflection to access `_configs`. They cast to
  `IConfigListOwner` and read the list through its public surface, so the
  serialized field name is free to change without breaking the workbench.
- **Manager / Component code generation** (`ManagerCreationService`,
  `ComponentCreationService`) emits config partials that implement
  `IConfigListOwner` out of the box, alongside the existing
  `EditorConfigs` editor-only helper.
- **`DevWindowFrameworkGuard`** renamed its internal fan-out step from
  `RunAllPageContributions` to `RunAllContributions` and switched its
  `TypeCache` scan from `IPage` to `IWorkbenchContribution`. Exception logs
  now read `&lt;Type&gt;.Contribute threw: &hellip;`.

### Removed

- **`IPage.OnWorkbenchOpen` default method.** Pages are now pure UI.
  Project-level bootstrapping must be expressed as an
  `IWorkbenchContribution`. Because `IPage` only became `public` in this
  same release, no shipped API surface is broken by this removal.
- **Scattered `"Assets/Game*"` and `"Packages/com.l47coder.stratum/&hellip;"`
  string literals** across Creators, Installers, Viewers and the framework
  guard &mdash; all replaced by constants exported from `GameProjectPaths`
  and `StratumPackageInfo`.

### Notes

- `Runtime~/Templates/Managers/*/Editor/*Refresher.cs` still carry
  hard-coded `Assets/Game/Manager/&hellip;/*Config.asset` paths. Those files
  are copied into the user's project verbatim, so parameterising them needs
  a `StratumSettings` ScriptableObject + template placeholders and is
  explicitly deferred to the next release.
- Upgrading from `0.1.0-preview.3`:
  - If you authored a page that implemented `IPage.OnWorkbenchOpen`, move
    the body into a small `IWorkbenchContribution` class. `IPage` no longer
    exposes the method.
  - If you built tooling that reflected into `_configs`, consume
    `IConfigListOwner` instead; generated configs already implement it and
    existing `BaseManagerConfig` / `BaseComponentConfig` subclasses can opt
    in by adding the interface to their class declaration.

## [0.1.0-preview.3] &mdash; 2026-04-23

Third preview. Introduces a dedicated `Game.Frame` assembly as the top-most
host-project layer, reorganises the editor source tree around per-page
`Installer / Order / Viewer` folders, and replaces the old *Sync Runtime*
menu entry with a first-class **Framework / Sync** page.

### Added

- **`Game.Frame` assembly + relocated `GameBoot.cs`.** The first-time
  bootstrap now provisions `Assets/Game/Frame/Game.Frame.asmdef` and moves
  `GameBoot.cs` out of `Assets/Game/Manager/` into `Assets/Game/Frame/`.
  `Game.Frame` references both `Game.Managers` and `Game.Components` and is
  the intended home for app-level glue (boot, scene wiring, cross-cutting
  concerns). Existing projects with `Assets/Game/Manager/GameBoot.cs` are
  auto-migrated via `AssetDatabase.MoveAsset`, preserving the original GUID.
- **Framework / Sync page.** A new `Framework` group in the Dev Workbench
  window exposes the old *Sync Runtime* action as a prominent button plus
  three auto-trigger options (`Manual only` / `When Dev Workbench closes`
  / `Before entering Play Mode`). The choice is stored per-developer in
  `EditorPrefs` &mdash; it is not committed to the project.
- **`ProjectFilesAutoSync` (`AssetPostprocessor`).** After any `.cs` /
  `.asmdef` / `.asmref` / `.rsp` import, Unity now auto-invokes
  `CodeEditor.CurrentEditor.SyncAll()`. This keeps external IDEs such as
  Cursor / VS Code reading up-to-date `.csproj` files, eliminating spurious
  "type or namespace not found" errors right after scaffolding a new
  Manager / Component / asmdef.
- **`IGameBoot` discovery is now interface-based.** `GameBootstrap` scans
  the scene for any `MonoBehaviour` implementing `IGameBoot` instead of
  resolving a fixed type. The concrete `GameBoot` class is free to live in
  any user assembly (the default template lives in `Game.Frame`).
- **`DevWindowFrameworkGuard`.** A single `[InitializeOnLoad]` gatekeeper
  replaces the previous `FrameworkBootstrapper` / `FrameAssetInstaller`
  pair. It owns the three-phase ensure flow (skeleton copy &rarr;
  Addressables / Order assets &rarr; `IPage.OnWorkbenchOpen` fan-out) and
  survives domain reloads via `SessionState`.

### Changed

- **Editor source tree reorganised.** `Editor/Workbench/Bootstrap/` has been
  retired. Its responsibilities are now split into:
  - `Editor/Workbench/Tool/` &mdash; framework-wide helpers
    (`DevWindowFrameworkGuard`, `AssetFolderCopier`, `AssetPathUtil`,
    `FrameAssetPaths`, `ProjectFilesAutoSync`, ...).
  - `Editor/Workbench/Page/<Group>/<Tab>/` &mdash; per-tab files colocated
    with the page itself. For example the Manager-template installer now
    lives in `Page/Manager/Installer/ManagerTemplateInstaller.cs` alongside
    `ManagerInstallerPage.cs`.
  - `Editor/Workbench/Frame/` keeps `DevWindow`, `PageOrder` and other
    window-chrome code.
- **`GameBootstrap` boot sequence documented** and hardened: `ResolveGameBoot`
  now emits explicit warnings when zero or multiple `IGameBoot` behaviours
  are found in the scene.
- **Default Manager templates** (`AssetManager`, `ComponentManager`,
  `PrefabManager`) gained XML summaries on their public surface and tidied
  their `OnDisable` / teardown ordering so Components receive
  `InternalSetEnabled(false)` before `InternalOnRemove()` in every path.

### Removed

- **`Tools / Dev Workbench / Sync Runtime` menu item.** Its behaviour moved
  into the new *Framework / Sync* page, so the workbench window is now the
  single entry point for that action.
- **`FrameworkBootstrapper`, `FrameAssetInstaller`, `FrameTemplateInstaller`,
  `WorkbenchPageRunner`.** Their logic was consolidated into
  `DevWindowFrameworkGuard` + the per-page installers.
- **`README.zh-CN.md`** was removed to keep a single canonical English README.

### Fixed

- **`PrefabManager.DestroyPoolAsync` / `OnDisable` ordering.** Active
  instances now disable their Components (reverse order) before the
  subsequent remove-pass, matching the lifecycle contract used elsewhere
  in the framework.
- **`ManagerRefreshUtil.Sync` no longer throws on blank keys.** List entries
  whose key is `null` / empty / whitespace (typically a half-filled row left
  in the Inspector) are now treated as stale and removed, instead of raising
  `ArgumentNullException` from `Dictionary.ContainsKey(null)` and aborting
  the whole refresh. All three built-in Refreshers (Asset / Component /
  Prefab) and any user-authored Refresher that routes through `Sync` benefit
  automatically.
- **Assembly-definition regressions** in `Game.Frame` / `Stratum.Editor`
  that caused stale csproj references after the tree reshuffle.

## [0.1.0-preview.2] &mdash; 2026-04-20

Second preview. Focuses on making default Managers optional, clarifying the
framework boot sequence, and aligning documentation with the current editor
flow.

### Changed

- **Default Managers are now on-demand templates.** The first-time
  `Initialise` only provisions `Assets/Game/Frame/{ManagerOrder,ComponentOrder,
  PageOrder}.asset` and two empty assembly containers &mdash;
  `Assets/Game/Manager/Game.Managers.asmdef` and
  `Assets/Game/Component/Game.Components.asmdef`. The bundled `Asset /
  Component / Prefab` Managers are imported on demand from the
  *Manager&nbsp;/&nbsp;Installer* tab and land in
  `Assets/Game/Manager/{Asset,Component,Prefab}/` as plain source.
- `GameBootstrap` gained a class-level XML doc describing its three-phase
  sequence (config load &rarr; async init &rarr; `IGameBoot.OnGameStart`),
  and dropped a stray startup `Debug.Log` that printed every Manager's type
  name.
- README: Highlights, Quick Start, Host Project Layout and the Assembly
  table updated to reflect the Installer-driven flow and the new
  `Game.Components` assembly.

## [0.1.0-preview.1] &mdash; 2026-04-19

First public preview release.

### Added

- Runtime architecture layer (`Stratum` assembly / namespace):
  - `BaseManagerConfig`, `BaseManagerData`, `BaseManager`, `IAsyncInitManager`,
    `IManagerRefresher` &mdash; the Manager contract.
  - `BaseComponentConfig`, `BaseComponentData`, `BaseComponent` &mdash; the
    Component contract.
  - `ComponentBridge` / `PhysicsBridge` &mdash; MonoBehaviour bridges that
    forward Unity lifecycle and physics callbacks into Components without
    leaking Manager-layer concepts.
  - `FrameworkLoader` and `ManagerRefreshUtil` utilities.
  - `GameLifetimeScope` (VContainer) and `GameBootstrap` entry points.
  - `ManagerOrderConfig`, `ComponentOrderConfig` &mdash; ordered configuration
    ScriptableObjects.
- Editor layer (`Stratum.Editor` assembly / namespace):
  - `DevWindow` (`Tools&nbsp;&rarr;&nbsp;Dev&nbsp;Workbench`) with Addressable,
    Component and Manager pages, each exposing Viewer / Order / Creator tabs.
  - One-click **framework bootstrap** that provisions the three order assets,
    the three default Manager configs and the matching Addressable entries.
  - Reusable IMGUI controls: `TreeView`, `TableView`, `ListView`, `TextView`
    and tool bars.
- Host project scaffolding (`Game.Managers` assembly):
  - Default `AssetManager`, `ComponentManager` and `PrefabManager`
    deployed on first load into `Assets/Game/Manager/` as plain source.
- Package metadata: `LICENSE.md` (MIT), `Third Party Notices.md`, `README.md`.

### Notes

- All user-facing strings in the editor UI, the bootstrap overlay, dialogs,
  `Debug.Log*` messages and runtime Manager template exceptions are in
  English. Public API XML documentation on the reusable IMGUI controls
  (`TreeView`, `ListView`, `TableView`, `TextView`) is also English-only.
  Chinese is only kept in internal implementation comments that are not
  surfaced to the user.
