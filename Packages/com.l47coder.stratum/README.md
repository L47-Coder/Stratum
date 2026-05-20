# Stratum

Stratum is a lightweight Unity package that combines Addressables-backed
VContainer bootstrapping with editor menu commands for initializing a layered
`Assets/Game` architecture and syncing runtime Manager order.

Status: **0.6.0**. The package is usable, but the public API is still
pre-`1.0` and may change between minor versions.

## What Ships

- **Runtime Manager contract.** `IManager` is a bare marker interface;
  Managers implement it directly. `IAsyncInitManager.InitAsync(token)` is the
  optional asynchronous init hook.
- **Addressables-backed boot.** `GameLifetimeScope` loads
  `App/ManagerOrder`, resolves Manager types by name, registers them into
  VContainer and hands control to `GameBootstrap`, which awaits any
  `IAsyncInitManager.InitAsync` calls and then injects and runs the scene
  `IGameBoot.OnGameStart`.
- **Architecture initialization.**
  `Tools > Stratum > Initialize Game Architecture` copies missing host
  skeleton files from `Templates~/Game` into `Assets/Game` and registers
  `App/ManagerOrder` in Addressables.
- **Manager order window.** `Tools > Stratum > Manager Order` opens a
  lightweight drag-sort window for `ManagerOrder.asset`. It syncs compiled
  Managers once on open; the same sync runs automatically before entering
  Play Mode.

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
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.6.0
```

## Quick Start

1. Install the dependencies and this package.
2. Run `Tools > Stratum > Initialize Game Architecture`.
3. Add shared business contracts under `Assets/Game/Core`, components under
   `Assets/Game/Component`, config types under `Assets/Game/ScriptableObject`
   and Manager implementations under `Assets/Game/Manager`.
4. Open `Tools > Stratum > Manager Order` and drag Managers into the desired
   initialization order. Newly compiled Managers are added automatically;
   missing ones are pruned.
5. In your boot scene, add a `GameLifetimeScope` component and the generated
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

Managers are registered as `IManager` and as their implemented
interfaces, so they can be injected into `GameBoot` (or any other resolved
type) via VContainer once they exist in the project.

## Editor Commands

| Command | Purpose |
| --- | --- |
| `Tools > Stratum > Initialize Game Architecture` | Ensure Addressables exists, copy missing `Assets/Game` skeleton files and register `App/ManagerOrder`. |
| `Tools > Stratum > Manager Order` | Open the lightweight drag-sort window for `ManagerOrder.asset`; sync runs once on open. |

## Generated Host Layout

```text
Assets/
+-- Game/
    +-- App/
    |   +-- Game.App.asmdef
    |   +-- GameBoot.cs
    |   +-- ManagerOrder.asset
    +-- Core/
    |   +-- Game.Core.asmdef
    +-- Manager/
    |   +-- Game.Managers.asmdef
    +-- Component/
    |   +-- Game.Component.asmdef
    +-- ScriptableObject/
        +-- Game.ScriptableObject.asmdef
```

Each section is its own assembly. `Game.Core` is the lowest generated host
assembly and references only `Stratum`. `Game.ScriptableObject`,
`Game.Managers`, `Game.Component` and `Game.App` can reference `Game.Core` for
shared lower-level types. `Game.Managers` also references
`Game.ScriptableObject`; `Game.Component` references `UniTask` for async
component code.

## Runtime Flow

When a scene contains `GameLifetimeScope`, Play Mode startup performs this
sequence:

1. Load `ManagerOrderConfig` from Addressables address `App/ManagerOrder`.
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
| `Stratum.Editor` | `Stratum.Editor` | Architecture initialization and Manager order window. |
| `Game.App` | global | Host boot layer copied from templates. |
| `Game.Core` | global | Lowest host layer for shared game types; references `Stratum` only. |
| `Game.Managers` | global | Host Manager implementations. |
| `Game.Component` | global | Host MonoBehaviour component scripts. |
| `Game.ScriptableObject` | global | Host ScriptableObject scripts. |

## License

Released under the [MIT License](./LICENSE.md). Third-party dependency notices
are listed in [Third Party Notices.md](./Third%20Party%20Notices.md).
