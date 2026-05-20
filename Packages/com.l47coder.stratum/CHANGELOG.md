# Changelog

All notable changes to this package are documented here.

The project follows Semantic Versioning. Versions before `1.0.0` may still
change public API between minor releases.

Earlier preview notes were removed after the package was reduced to the
current runtime contracts, generated host layout and focused editor commands.

## [0.6.0] - 2026-05-21

### Added

- Added a generated host `Game.Core` assembly under `Assets/Game/Core` as the
  lowest game layer; it references `Stratum` only.
- Added `Tools > Stratum > Initialize Game Architecture` for copying missing
  host skeleton files and registering `App/ManagerOrder`.
- Added `Tools > Stratum > Manager Order`, a lightweight drag-sort window for
  `ManagerOrder.asset`.

### Changed

- Rebuilt the generated host layout around `App`, `Core`, `Component`,
  `Manager` and `ScriptableObject` assemblies.
- Runtime boot now loads manager order from Addressables address
  `App/ManagerOrder`.
- Opening the Manager Order window now syncs compiled Managers once, and the
  same sync still runs before entering Play Mode.
- `Game.Managers` now references `Game.Component`, `Game.Core` and
  `Game.ScriptableObject`.
- `Game.Component` now references `Game.Core`, `Game.ScriptableObject` and
  `UniTask`.
- Documentation now describes the current command-based workflow only.

### Removed

- Removed the legacy editor window, page system, custom IMGUI controls and
  source scaffolding stack.
- Removed the persisted editor layout asset and orphaned built-in Manager code
  templates.
