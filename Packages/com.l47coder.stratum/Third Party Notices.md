# Third Party Notices

This package depends on the following third-party packages. Each dependency is
distributed under its own license and is **not** bundled into this repository
&mdash; it is pulled in at install time by the Unity Package Manager.

| Dependency | Version | License | Source |
| ---------- | ------- | ------- | ------ |
| [Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) | 1.22.3 | Unity Companion License | `com.unity.addressables` |
| [UniTask](https://github.com/Cysharp/UniTask) | 2.5.10 | MIT License | `com.cysharp.unitask` |
| [VContainer](https://github.com/hadashiA/VContainer) | 1.17.0 | MIT License | `jp.hadashikick.vcontainer` |

## Why this file exists

MIT only requires preservation of the MIT copyright notice when source or
binary of an MIT-licensed work is redistributed. This package does **not**
vendor any third-party source; the dependencies above are declared via UPM and
resolved from their original upstream registries. This file is provided as a
courtesy so that downstream users can find each dependency's upstream project
and license text in one place.

If you fork this package and vendor any of the above sources into the package
tree, you must also copy the corresponding LICENSE file of that dependency
alongside the vendored code, per that dependency's license terms.
