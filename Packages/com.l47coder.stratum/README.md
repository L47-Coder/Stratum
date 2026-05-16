# Stratum

Stratum is a Unity package for building small Manager-based runtime services
and keeping their configuration assets editable from a focused IMGUI workbench.
It combines `ScriptableObject` data, Addressables, UniTask and VContainer with
editor tooling for Manager scaffolding, Manager order, config tables and
ScriptableObject asset folders.

Status: **0.5.0**. The package is usable, but the public API is still
pre-`1.0` and may change between minor versions.

## What Ships

- **Runtime Manager contracts.** `BaseManager<TConfig, TData>`,
  `BaseManagerConfig<TData>`, `BaseManagerData`, `IManager`,
  `IAsyncInitManager` and `IGameBoot` define the runtime service model.
- **Addressables-backed boot.** `GameLifetimeScope` loads
  `Frame/ManagerOrder`, registers Managers into VContainer, loads each
  Manager config, runs optional async init, then calls the scene `IGameBoot`.
- **Dev Workbench.** `Tools > Stratum > Dev Workbench` opens a reorderable
  editor window with `Framework`, `Manager` and `ScriptableObject` sections.
- **Manager workflow.** The Workbench can view Manager source/config folders,
  create new Managers, install built-in Manager templates and edit Manager boot
  order.
- **ScriptableObject workflow.** The Workbench can create SO types under
  `Assets/Game/ScriptableObject`, create/delete/rename matching `.asset` files
  and display them in table form.
- **Reusable editor controls.** `ListControl`, `TableControl`, `TreeControl`,
  `TextControl`, `DropdownPopup` and `FieldPopup` are public editor-side
  controls in the `Stratum.Editor` assembly.

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
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.0
```

## Quick Start

1. Install the dependencies and this package.
2. Open `Tools > Stratum > Dev Workbench`.
3. On first open, Stratum ensures Addressables exists, copies the host
   skeleton under `Assets/Game/`, registers `Frame/ManagerOrder` and creates
   the `ManagerConfig` Addressables group.
4. In `Manager > Installer`, import the built-in Manager templates you want:
   `Asset`, `Event`, `Message` and `Task`.
5. In `Manager > Creator`, create project-specific Managers. The Creator
   writes the Manager interface, Manager partial class, data class, generated
   base-class partials, refresher stub, config asset and Addressables entry.
6. In `Manager > Order`, arrange the runtime Manager boot order.
7. In your boot scene, add a `GameLifetimeScope` component and the generated
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

Managers created or installed through the Workbench are registered by their
interfaces, so they can be injected into `GameBoot` with VContainer once their
templates exist in the project.

## Workbench Pages

| Group | Tab | Purpose |
| --- | --- | --- |
| `Framework` | `Sync` | Runs all `[EditorSync]` methods manually, on Workbench close or before Play Mode. |
| `Manager` | `Viewer` | Browses `Assets/Game/Manager`, shows source files and config tables. |
| `Manager` | `Creator` | Scaffolds a Manager under the selected Manager folder. |
| `Manager` | `Order` | Syncs and edits `ManagerOrder.asset`, which controls runtime registration order. |
| `Manager` | `Installer` | Imports built-in Manager templates from `Templates~/Managers`. |
| `ScriptableObject` | `Viewer` | Creates SO types and manages matching SO assets under `Assets/Game/ScriptableObject`. |

The built-in templates are copied as source into the host project, so they are
intended to be edited, deleted or replaced like normal project code.

## Generated Host Layout

```text
Assets/
+-- Game/
    +-- Editor/
    |   +-- Game.Editor.asmdef
    +-- Frame/
    |   +-- Game.Frame.asmdef
    |   +-- GameBoot.cs
    |   +-- ManagerOrder.asset
    |   +-- PageOrder.asset
    +-- Manager/
    |   +-- Game.Managers.asmdef
    |   +-- Game.Managers.InternalsVisibleTo.cs
    |   +-- Asset/
    |   +-- Event/
    |   +-- Message/
    |   +-- Task/
    +-- ScriptableObject/
        +-- Game.ScriptableObject.asmdef
```

## Runtime Flow

When a scene contains `GameLifetimeScope`, Play Mode startup performs this
sequence:

1. Load `ManagerOrderConfig` from Addressables address `Frame/ManagerOrder`.
2. Resolve each ordered Manager type from its assembly-qualified name.
3. Register Managers into VContainer as `IManager` and as their implemented
   interfaces.
4. `GameBootstrap` calls `SetManagerDataDict()` on every Manager, loading each
   Manager config from `ManagerConfig/<ManagerName>`.
5. Managers implementing `IAsyncInitManager` run `InitAsync`.
6. The single scene `MonoBehaviour` implementing `IGameBoot` receives
   container injection and `OnGameStart()` is awaited.

## Editor Sync

Mark a parameterless `void` method with `[EditorSync]` to make it runnable from
`Framework > Sync`. Static methods are invoked directly. Instance methods are
supported on Manager config assets, where the runner finds all matching assets,
invokes the method and marks the asset dirty.

```csharp
using Stratum;

internal static class InventoryManagerRefresher
{
    [EditorSync]
    public static void Run()
    {
        // Rebuild InventoryManagerConfig data here.
    }
}
```

## Assembly Layout

| Assembly | Namespace | Notes |
| --- | --- | --- |
| `Stratum` | `Stratum` | Runtime contracts, bootstrapping, loader utilities and field attributes. |
| `Stratum.Editor` | `Stratum.Editor` | Dev Workbench, pages, popups and reusable editor controls. |
| `Game.Frame` | global | Host boot layer created from templates. |
| `Game.Managers` | global | Host Manager source and generated Manager partials. |
| `Game.ScriptableObject` | global | Host SO scripts created by the Workbench. |
| `Game.Editor` | global | Host editor helpers and Manager refreshers. |

## License

Released under the [MIT License](./LICENSE.md). Third-party dependency notices
are listed in [Third Party Notices.md](./Third%20Party%20Notices.md).
