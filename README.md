# Stratum

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![Unity 2022.3](https://img.shields.io/badge/Unity-2022.3%20LTS-black.svg?logo=unity)](https://unity.com/releases/editor/whats-new/2022.3.60)
[![Package](https://img.shields.io/badge/UPM-com.l47coder.stratum-1a7ad4.svg)](./Packages/com.l47coder.stratum)

This repository hosts the source of the **Stratum** Unity package
(`com.l47coder.stratum`) together with a minimal Unity host project that
is used for developing and dogfooding the package.

- **Package source** — [`Packages/com.l47coder.stratum/`](./Packages/com.l47coder.stratum)
- **Package README** — [`Packages/com.l47coder.stratum/README.md`](./Packages/com.l47coder.stratum/README.md)
- **Changelog** — [`CHANGELOG.md`](./Packages/com.l47coder.stratum/CHANGELOG.md)
- **License** — [MIT](./LICENSE)

Stratum is a Unity **Manager / Component** framework powered by
`ScriptableObject` + `Addressables`, shipped with an in-editor **Dev Workbench**
panel (`Tools → Dev Workbench`) that provides one-click creation and visual
management of Managers, Components, Addressable groups and the framework-wide
Sync step.

## Repository layout

```
.
├── Assets/                              # Minimal host project (Unity 2022.3.60f1)
│   ├── Game/                            # Auto-provisioned on first workbench launch
│   │   ├── Frame/                       #   Game.Frame.asmdef, GameBoot.cs, order SOs
│   │   ├── Manager/                     #   Game.Managers.asmdef + on-demand templates
│   │   └── Component/                   #   Game.Components.asmdef
│   └── Scenes/SampleScene.unity
├── Packages/
│   ├── com.l47coder.stratum/            # The package source (embedded)
│   └── manifest.json                    # Host project dependencies
├── ProjectSettings/                     # Unity project settings
├── .editorconfig
├── .gitattributes
├── .gitignore
├── CONTRIBUTING.md
├── LICENSE
└── README.md                            # (this file)
```

Because the package is kept as an **embedded** package (not a git submodule),
cloning this repository and opening it in Unity gives you a working editor
install — any change you make under `Packages/com.l47coder.stratum/` is
picked up live.

## Install the package in your own project

Open `Window → Package Manager`, click `+ → Add package from git URL…` and
paste:

```
https://github.com/L47-Coder/unity-stratum.git?path=Packages/com.l47coder.stratum
```

Or edit your project's `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.l47coder.stratum": "https://github.com/L47-Coder/unity-stratum.git?path=Packages/com.l47coder.stratum"
  }
}
```

The `?path=` segment is required because this repository is an entire Unity
project — without it, UPM would try to treat the repo root as the package.

See the package README for full usage details, quick-start and host-project
layout conventions.

## Requirements

- Unity **2022.3 LTS** or newer (developed against `2022.3.60f1`)
- UPM dependencies resolved automatically by `package.json`:
  - `com.unity.addressables` 1.22.3
  - `com.cysharp.unitask` 2.5.10
  - `jp.hadashikick.vcontainer` 1.17.0

## Development

Clone and open the repository as a regular Unity project:

```bash
git clone https://github.com/L47-Coder/unity-stratum.git
```

Then open the folder in **Unity 2022.3 LTS**. The embedded package is live —
edit `Packages/com.l47coder.stratum/…` and Unity will recompile.

Contribution guidelines live in [`CONTRIBUTING.md`](./CONTRIBUTING.md).

## License

Released under the [MIT License](./LICENSE). See
[`Third Party Notices.md`](./Packages/com.l47coder.stratum/Third%20Party%20Notices.md)
for upstream dependency licenses.
