# Stratum

A Unity runtime **Manager / Component** framework powered by `ScriptableObject` +
`Addressables`, shipped with an in-editor **Stratum** panel that provides
one-click creation and visual management of Managers, Components and
Addressable groups.

> Status: **0.2.0** &mdash; the public API may still change before `1.0`.

## Highlights

- **Opinionated runtime core.** A tiny `BaseManager` / `BaseComponent` pair
  standardises data loading, lifecycle and dependency injection, so gameplay
  code only ever sees well-typed, ready-to-use services.
- **Three-layer host separation.** The architecture layer (`Stratum`)
  owns lifecycle and contracts; the dispatch layer (`Game.Managers` /
  `Game.Components`) lives under `Assets/Game/` and is fully editable; the
  top-most glue layer (`Game.Frame`) is where app-level wiring such as
  `GameBoot` lives.
- **Stratum editor panel.** Tools&nbsp;&rarr;&nbsp;Dev&nbsp;Workbench exposes
  four groups &mdash; **Framework / Addressable / Manager / Component** &mdash;
  each with its own Viewer / Order / Creator / Installer tabs as applicable.
  The Creator scaffolds a new Manager or Component (`.cs` + generated
  `Data` / `Config` partials + `ScriptableObject` asset + Addressable entry)
  with a single click.
- **Framework / Sync page.** A dedicated page offers a `Sync Runtime` button
  plus three auto-trigger options (`Manual only` / `When Stratum closes`
  / `Before entering Play Mode`). It replaces the old `Tools / Stratum
  / Sync Runtime` menu entry so the window is the single entry point.
- **Default Managers ship as on-demand templates.** On first launch the
  workbench only provisions the Frame assets and three empty `asmdef`
  containers (`Game.Frame` / `Game.Managers` / `Game.Components`). The bundled
  `Asset / Component / Prefab` Managers are imported from the
  *Manager&nbsp;/&nbsp;Installer* tab as plain source you own and can modify or
  delete.
- **Auto-sync external IDE project files.** Any `.cs` / `.asmdef` / `.asmref`
  / `.rsp` import triggers `CodeEditor.SyncAll()`, so Cursor / VS Code pick
  up freshly scaffolded types without a manual *Regenerate project files*
  step.

## Requirements

- Unity **2022.3 LTS** or newer
- The following UPM dependencies (declared in `package.json`):
  - `com.unity.addressables` 1.22.3
  - `com.cysharp.unitask` 2.5.10
  - `jp.hadashikick.vcontainer` 1.17.0

UniTask and VContainer are usually consumed via their official Git URLs. The
version numbers in `package.json` are used by UPM for compatibility hints only
&mdash; the actual entry points belong in your host project's
`Packages/manifest.json`.

## Installation

### UPM &mdash; Git URL (recommended)

Open `Window&nbsp;&rarr;&nbsp;Package Manager`, click `+&nbsp;&rarr;&nbsp;Add
package from git URL&hellip;` and paste:

```
https://github.com/L47-Coder/unity-stratum.git?path=Packages/com.l47coder.stratum
```

Or add the following entry to `Packages/manifest.json`:

```json
"com.l47coder.stratum": "https://github.com/L47-Coder/unity-stratum.git?path=Packages/com.l47coder.stratum"
```

> The repository is an entire Unity project, so the `?path=` segment is
> **required** &mdash; it tells UPM which sub-directory actually contains
> `package.json`.

### UPM &mdash; local path

Clone / copy this package into your project's `Packages/` folder. Unity will
pick it up automatically as an embedded package.

## Quick Start

1. Install the package.
2. Open `Tools&nbsp;&rarr;&nbsp;Dev&nbsp;Workbench`. The first launch
   auto-provisions:
   - `Assets/Game/Frame/{ManagerOrder,ComponentOrder,PageOrder}.asset`
   - `Assets/Game/Frame/GameBoot.cs` and `Game.Frame.asmdef` (the top-most
     host assembly)
   - `Assets/Game/Manager/Game.Managers.asmdef` and
     `Assets/Game/Component/Game.Components.asmdef` as empty containers
   - Addressable group `Frame` with the two order SO entries registered.
3. Open the *Manager&nbsp;/&nbsp;Installer* tab and import the default
   `Asset / Component / Prefab` Managers on demand. They land under
   `Assets/Game/Manager/{Asset,Component,Prefab}/` as plain source you can
   modify or delete.
4. Use the *Manager&nbsp;/&nbsp;Creator* or *Component&nbsp;/&nbsp;Creator*
   tab to scaffold your own Manager / Component. Addressable entries are
   kept up to date automatically; use *Framework&nbsp;/&nbsp;Sync* to run
   every `[EditorSync]`-marked method on demand (or configure it to run on
   window close / before Play Mode).
5. In your boot scene, drop the generated `GameBoot` MonoBehaviour onto any
   GameObject and override `OnGameStart` to wire up gameplay. A
   `GameLifetimeScope` is spawned automatically by the framework.

## Host Project Layout

Stratum follows a **fixed path convention**. All generated files land
under `Assets/Game/`:

```
Assets/
└── Game/
    ├── Frame/
    │   ├── Game.Frame.asmdef         (top-most host assembly)
    │   ├── GameBoot.cs               (MonoBehaviour : IGameBoot)
    │   ├── ManagerOrder.asset
    │   ├── ComponentOrder.asset
    │   └── PageOrder.asset
    ├── Manager/
    │   ├── Game.Managers.asmdef
    │   ├── Asset/      (installed on demand)
    │   ├── Component/  (installed on demand)
    │   └── Prefab/     (installed on demand)
    └── Component/
        └── Game.Components.asmdef
```

`Game.Managers` has an `[InternalsVisibleTo]` bridge into `Stratum`,
which lets Manager code (e.g. `PrefabManager`) call the framework's internal
dispatch hooks such as `BaseComponent.InternalSetGameObject`. `Game.Components`
only uses the `protected` lifecycle overrides (`OnAdd`, `OnEnable`, …) and
does **not** require internal access. `Game.Frame` sits above both and is
where app-level glue lives &mdash; the shipped `GameBoot.cs` is just a
starting point you own.

## Assembly & Namespace Layout

| Assembly (`.asmdef`)   | Namespace             | Purpose                                                                 |
| ---------------------- | --------------------- | ----------------------------------------------------------------------- |
| `Stratum`         | `Stratum`        | Runtime contracts, bridges, loader utilities.                           |
| `Stratum.Editor`  | `Stratum.Editor` | Editor-only Stratum window, guard and installers.                  |
| `Game.Managers`        | *global*              | Host-project Managers (default + user-authored).                        |
| `Game.Components`      | *global*              | Host-project Components, scaffolded by the Creator.                     |
| `Game.Frame`           | *global*              | App-level glue (boot, scene wiring); references both Managers + Components. |

The host-project layers intentionally stay in the global namespace so that
scaffolded templates read naturally and gameplay code does not need an
extra `using`.

## License

Released under the [MIT License](./LICENSE.md).

Third-party dependency licenses are tracked in
[`Third Party Notices.md`](./Third%20Party%20Notices.md).
