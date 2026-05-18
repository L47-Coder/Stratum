# Stratum

Stratum is a lightweight Unity package that combines Addressables-backed
VContainer bootstrapping with a focused IMGUI Dev Workbench for scaffolding
single-file Manager, MonoBehaviour and ScriptableObject scripts.

Status: **0.5.3**. The package is usable, but the public API is still
pre-`1.0` and may change between minor versions.

## What Ships

- **Runtime Manager contract.** `IManager` is a bare marker interface;
  Managers implement it directly. `IAsyncInitManager.InitAsync(token)` is the
  optional asynchronous init hook.
- **Addressables-backed boot.** `GameLifetimeScope` loads
  `Frame/ManagerOrder`, resolves Manager types by name, registers them into
  VContainer and hands control to `GameBootstrap`, which awaits any
  `IAsyncInitManager.InitAsync` calls and then injects and runs the scene
  `IGameBoot.OnGameStart`.
- **Dev Workbench.** `Tools > Stratum > Dev Workbench` opens a reorderable
  editor window with three symmetric script generators (`Manager`,
  `MonoBehaviour`, `ScriptableObject`), a Manager boot-order editor and a
  Manager template import tab.
- **Manager templates.** Optional single-file `EventManager`,
  `MessageManager` and `TaskManager` templates can be imported from
  `Manager > Import`.
- **Single-file code generation.** Each creator panel writes one `.cs` file:
  - Manager: `public interface IXxxManager : IManager` and
    `internal sealed class XxxManager : IXxxManager`.
  - MonoBehaviour: `public class XxxComponent : MonoBehaviour`.
  - ScriptableObject: `[CreateAssetMenu(menuName = "ScriptableObject/Xxx")]`
    plus `public class XxxConfig : ScriptableObject`.
- **Reusable editor controls.** `InputControl`, `ButtonControl`,
  `TextControl`, `TreeControl`, `ListControl` and `TableControl` are public
  controls in the `Stratum.Editor` assembly, all driven by a uniform
  `Draw(Rect)` signature.

## Requirements

- Unity **2022.3 LTS** or newer. This repository is currently developed with
  `2022.3.60f1c1`.
- `com.unity.addressables` **1.22.3**.
- `com.cysharp.unitask` **2.5.10**.
- `jp.hadashikick.vcontainer` **1.17.0**.

Addressables is available from Unity's registry. UniTask and VContainer are
commonly added to the host project's `Packages/manifest.json` by Git URL:

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10",
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.17.0",
    "com.unity.addressables": "1.22.3"
  }
}
```

## Installation

Install the package from this repository with UPM:

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum
```

Or add it directly to your host project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.l47coder.stratum": "https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum"
  }
}
```

The `?path=` segment is required because this repository is a full Unity
project and the package lives in a subfolder.

For a pinned release, append the tag:

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.3
```

## Quick Start

1. Install the dependencies and this package.
2. Open `Tools > Stratum > Dev Workbench`.
3. On first open, Stratum ensures Addressables exists and copies the host
   skeleton under `Assets/Game/`, then registers `Frame/ManagerOrder`.
4. In `Manager > Viewer`, select the root folder and use the creator panel
   on the right to generate a Manager script.
5. In `MonoBehaviour > Viewer`, generate any data-holder components your
   Managers depend on.
6. In `ScriptableObject > Viewer`, generate ScriptableObject types and
   create matching `.asset` instances directly from the Unity create menu.
7. Optionally import `EventManager`, `MessageManager` or `TaskManager` from
   `Manager > Import`.
8. In `Manager > Order`, arrange the runtime Manager boot order. Newly
   compiled Managers are added automatically; missing ones are pruned.
9. In your boot scene, add a `GameLifetimeScope` component and the generated
   `GameBoot` MonoBehaviour, then implement `IGameBoot.OnGameStart`.

Example boot script:

```csharp
using Cysharp.Threading.Tasks;
using Stratum;
using UnityEngine;

public sealed class GameBoot : MonoBehaviour, IGameBoot
{
    public async UniTask OnGameStart()
    {
        await UniTask.CompletedTask;
    }
}
```

Generated Managers are registered as `IManager` and as their implemented
interfaces, so they can be injected into `GameBoot` (or any other resolved
type) via VContainer once they exist in the project.

## Workbench Pages

| Group | Tab | Purpose |
| --- | --- | --- |
| `Manager` | `Viewer` | Browse `Assets/Game/Manager`, show source files and a single-file Manager creator panel on the selected folder. |
| `Manager` | `Order` | Sync and edit `ManagerOrder.asset`, which controls runtime Manager registration order. |
| `Manager` | `Import` | Import optional single-file Manager templates from `Templates~/Managers`. |
| `MonoBehaviour` | `Viewer` | Browse `Assets/Game/MonoBehaviour` and generate single-file `MonoBehaviour` scripts. |
| `ScriptableObject` | `Viewer` | Browse `Assets/Game/ScriptableObject` and generate single-file ScriptableObject scripts. |

Group order, tab order and the persisted selection live in
`Assets/Game/Frame/PageOrder.asset` and can be reordered by dragging menu
items or tab headers.

## Generated Host Layout

```text
Assets/
+-- Game/
    +-- Frame/
    |   +-- Game.Frame.asmdef
    |   +-- GameBoot.cs
    |   +-- ManagerOrder.asset
    |   +-- PageOrder.asset
    +-- Manager/
    |   +-- Game.Managers.asmdef
    +-- MonoBehaviour/
    |   +-- Game.MonoBehaviour.asmdef
    +-- ScriptableObject/
        +-- Game.ScriptableObject.asmdef
```

Each section is its own assembly. `Game.Managers` references the runtime
`Stratum`, `UniTask` and `VContainer` only. `Game.MonoBehaviour` and
`Game.ScriptableObject` may reference `Game.Managers` so generated
components and SO types can interact with project Managers.

## Runtime Flow

When a scene contains `GameLifetimeScope`, Play Mode startup performs this
sequence:

1. Load `ManagerOrderConfig` from Addressables address `Frame/ManagerOrder`.
2. For every entry, resolve the Manager type by assembly-qualified name
   (with a name-based fallback that scans assemblies referencing `Stratum`).
3. Register each Manager into VContainer as a singleton via
   `AsImplementedInterfaces()`.
4. `GameBootstrap` calls `InitAsync(token)` on every Manager that implements
   `IAsyncInitManager`, in the order from `ManagerOrderConfig`.
5. The single scene `MonoBehaviour` implementing `IGameBoot` is located,
   injected and `OnGameStart()` is awaited.

## Assembly Layout

| Assembly | Namespace | Notes |
| --- | --- | --- |
| `Stratum` | `Stratum` | Runtime contracts, bootstrapping, Addressables loader. |
| `Stratum.Editor` | `Stratum.Editor` | Dev Workbench, pages and reusable editor controls. |
| `Game.Frame` | global | Host boot layer copied from templates. |
| `Game.Managers` | global | Host Manager scripts generated by the Workbench. |
| `Game.MonoBehaviour` | global | Host MonoBehaviour scripts generated by the Workbench. |
| `Game.ScriptableObject` | global | Host ScriptableObject scripts generated by the Workbench. |

## License

Released under the [MIT License](./LICENSE.md). Third-party dependency notices
are listed in [Third Party Notices.md](./Third%20Party%20Notices.md).
