# Stratum v0.5.2

Release date: 2026-05-18

## Summary

This release is a substantial simplification pass. The package drops the
config/data/refresher Manager scaffolding and the popup-driven editor
subsystem, and reorganizes the Dev Workbench around three symmetric script
generators: `Manager`, `MonoBehaviour` and `ScriptableObject`. Each
generator now writes a single `.cs` file with no companion assets.

## Highlights

- Lightweight single-file code generation for `Manager`, `MonoBehaviour` and
  `ScriptableObject`. No more base-class partials, data classes, refresher
  stubs, per-Manager `.asset` configs or `ManagerConfig` Addressables
  entries.
- New `MonoBehaviour` Workbench page and host assembly `Game.MonoBehaviour`
  for generated `MonoBehaviour` data-holder scripts.
- Editor `So*` symbols and the `Editor/Page/So` folder renamed to
  `ScriptableObject*` for consistency; the on-disk asset path
  `Assets/Game/ScriptableObject` is unchanged.
- Unified `Draw(Rect)` signature across every custom editor control
  (`TextControl`, `InputControl`, `ButtonControl`, `TreeControl`,
  `ListControl`, `TableControl`).
- `IManager` is now a bare marker interface; Managers implement it
  directly. `IAsyncInitManager.InitAsync` remains the optional async init
  hook.
- Fixed a stale `TextField` display bug when switching Workbench pages by
  clearing keyboard focus on every page switch.

## Breaking Or Migration Notes

- `BaseManager<TConfig, TData>`, `BaseManagerConfig<TData>` and
  `BaseManagerData` are removed. Migrate Managers to implement `IManager`
  (and optionally `IAsyncInitManager`) directly.
- `Templates~/Managers/manifest.json` and the bundled `AssetManager`,
  `EventManager`, `MessageManager` and `TaskManager` templates are removed.
  Either copy the implementations you need from a `v0.5.1` checkout or
  reimplement them on top of the simplified `IManager` contract.
- `[EditorSync]` and `Framework > Sync` are removed. Move any sync work into
  ordinary editor scripts or `Tools` menu items.
- `DropdownAttribute`, `ExpandableAttribute` and the entire `Editor/Popup`
  subsystem are removed. `TableControl` cells now render through native
  Unity field controls.
- `FieldAttribute` is renamed to `TableAttribute` and scoped to table column
  metadata.
- `TextControl.WordWrap` is removed; long preview text scrolls horizontally
  instead of wrapping.
- The briefly-introduced `Container` Workbench page and `Game.Container`
  template assembly are removed. Define Manager-bound interfaces inside the
  Manager file directly when needed.

## Install

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.2
```
