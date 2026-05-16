# Contributing to Stratum

This repository is both a Unity host project and the source of the
`com.l47coder.stratum` UPM package. The package is embedded at
`Packages/com.l47coder.stratum/`, so Unity recompiles package edits directly
while the host project is open.

## Repository Model

```text
Packages/
+-- com.l47coder.stratum/   # Package source
+-- manifest.json           # Host project dependencies

Assets/
+-- Game/                   # Dogfooded generated host layout
+-- StratumWorkbenchExamples/
```

Package code belongs under `Packages/com.l47coder.stratum/`. The `Assets/`
folder is the development host project used for manual testing, examples and
dogfooding generated output.

## Prerequisites

- Unity **2022.3 LTS**. The current project uses `2022.3.60f1c1`.
- Git 2.30 or newer.
- Git LFS if you add binary assets.
- Optional: Rider, Visual Studio or VS Code.

## Getting Started

```bash
git clone https://github.com/L47-Coder/Stratum.git
cd Stratum
```

Open the repository folder in Unity Hub. Unity resolves Addressables, UniTask
and VContainer through `Packages/manifest.json`.

Useful manual checks:

- Open `Tools > Stratum > Dev Workbench`.
- Use `Framework > Sync` after changing generated Manager data or refreshers.
- Exercise `Manager > Viewer`, `Manager > Creator`, `Manager > Order` and
  `Manager > Installer` for Manager workflow changes.
- Exercise `ScriptableObject > Viewer` for SO creation, asset rename/delete and
  table editing changes.
- Open `Tools > Stratum > Test` for local editor-control examples.

## Coding Conventions

- Respect `.editorconfig`: UTF-8, LF line endings, 4-space C# indent and
  2-space JSON/YAML indent.
- Runtime code lives in `Packages/com.l47coder.stratum/Runtime/` and uses the
  `Stratum` namespace.
- Editor code lives in `Packages/com.l47coder.stratum/Editor/` and uses the
  `Stratum.Editor` namespace.
- Host templates live in `Packages/com.l47coder.stratum/Templates~/`.
- Manager templates in `Templates~/Managers/` are copied into user projects as
  source. Treat their public interfaces as user-facing API.
- Keep generated host-project files deterministic and easy to diff.
- Avoid unrelated refactors in feature or fix branches.

## Changelog And Versioning

The package follows Semantic Versioning. It is still pre-`1.0`, so minor
versions may include breaking API changes when the changelog calls them out.

For user-visible changes, update
`Packages/com.l47coder.stratum/CHANGELOG.md`.

For a release:

1. Set `version` in `Packages/com.l47coder.stratum/package.json`.
2. Promote changelog notes to `## [<version>] - YYYY-MM-DD`.
3. Commit with `chore(release): v<version>`.
4. Tag with an annotated tag named `v<version>`.

Example: package version `0.5.0` is tagged as `v0.5.0`.

## Release Flow

1. Start from a clean branch and make sure the target branch is up to date.

   ```bash
   git status
   git pull --ff-only
   ```

2. Update release files.

   ```bash
   git add Packages/com.l47coder.stratum/package.json \
           Packages/com.l47coder.stratum/README.md \
           Packages/com.l47coder.stratum/CHANGELOG.md \
           README.md \
           CONTRIBUTING.md
   git commit -m "chore(release): v0.5.0"
   ```

3. Create and push the annotated tag.

   ```bash
   git tag -a v0.5.0 -m "Stratum 0.5.0"
   git push origin <branch>
   git push origin v0.5.0
   ```

4. Publish the GitHub Release.

   ```bash
   gh release create v0.5.0 \
     --title "v0.5.0" \
     --notes-file release-notes.md
   ```

5. Verify tagged UPM install.

   ```text
   https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.0
   ```

## Pull Requests

1. Keep the scope focused.
2. Make sure the Unity editor compiles without new warnings.
3. Manually run the Workbench flows touched by the change.
4. Update docs and changelog when user-facing behavior changes.
5. Fill in the PR template with verification notes.

## Bug Reports

Include:

- Stratum package version from `Packages/com.l47coder.stratum/package.json`.
- Unity version and editor platform.
- Minimal reproduction steps.
- Relevant Unity Console output or stack trace.

## License

By contributing, you agree that your contributions are licensed under the
[MIT License](./LICENSE).
