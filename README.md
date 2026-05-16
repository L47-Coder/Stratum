# Stratum

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![Unity 2022.3](https://img.shields.io/badge/Unity-2022.3%20LTS-black.svg?logo=unity)](https://unity.com/releases/editor/whats-new/2022.3.60)
[![Package](https://img.shields.io/badge/UPM-com.l47coder.stratum-1a7ad4.svg)](./Packages/com.l47coder.stratum)

This repository contains the **Stratum** Unity package
(`com.l47coder.stratum`) and a minimal Unity host project used to develop and
dogfood the package.

- Package source: [Packages/com.l47coder.stratum/](./Packages/com.l47coder.stratum)
- Package README: [Packages/com.l47coder.stratum/README.md](./Packages/com.l47coder.stratum/README.md)
- Changelog: [Packages/com.l47coder.stratum/CHANGELOG.md](./Packages/com.l47coder.stratum/CHANGELOG.md)
- License: [MIT](./LICENSE)

Stratum is a Manager and ScriptableObject workflow package for Unity. It
provides Addressables-backed Manager bootstrapping, VContainer registration,
config tables and an IMGUI Dev Workbench at `Tools > Stratum > Dev Workbench`.

## Repository Layout

```text
.
+-- Assets/                              # Unity host project
|   +-- Game/                            # Generated/dogfooded Stratum host layout
|   |   +-- Editor/
|   |   +-- Frame/
|   |   +-- Manager/
|   |   +-- ScriptableObject/
|   +-- Scenes/
|   +-- StratumWorkbenchExamples/        # Local examples for editor controls
+-- Packages/
|   +-- com.l47coder.stratum/            # The embedded UPM package
|   +-- manifest.json                    # Host project dependencies
+-- ProjectSettings/
+-- CONTRIBUTING.md
+-- LICENSE
+-- README.md
```

Because the package is embedded, cloning this repository and opening it in
Unity gives you a live package development environment. Changes under
`Packages/com.l47coder.stratum/` are compiled by the host project directly.

## Install In Another Project

Install from Git URL in Unity Package Manager:

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.l47coder.stratum": "https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum"
  }
}
```

The `?path=` segment is required because this repository is a full Unity
project and the package lives in a subfolder. To install a tagged release:

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.1
```

## Requirements

- Unity **2022.3 LTS** or newer. The current host project uses
  `2022.3.60f1c1`.
- Addressables `1.22.3`.
- UniTask `2.5.10`.
- VContainer `1.17.0`.

The host project's manifest shows the dependency setup used during
development, including Git URLs for UniTask and VContainer.

## Development

```bash
git clone https://github.com/L47-Coder/Stratum.git
cd Stratum
```

Open the folder with Unity Hub. Unity resolves the host project dependencies on
first open. The main package is in `Packages/com.l47coder.stratum/`; the
`Assets/` folder exists for development, examples and manual verification.

See [CONTRIBUTING.md](./CONTRIBUTING.md) for release and contribution notes.

## License

Released under the [MIT License](./LICENSE). See
[Third Party Notices.md](./Packages/com.l47coder.stratum/Third%20Party%20Notices.md)
for upstream dependency license notes.
