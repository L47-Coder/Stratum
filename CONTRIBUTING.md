# Contributing to Stratum

Thanks for your interest in contributing! This document explains how the
repository is laid out, how to get a working development environment, and the
conventions we follow for commits, versioning and pull requests.

## Repository model

This repository is **both** a Unity host project **and** the source of the
`com.l47coder.stratum` package. The package lives at
`Packages/com.l47coder.stratum/` as an *embedded* UPM package, which
means changes to the package source are picked up by Unity immediately — no
re-import or re-install step is required.

```
Packages/
├── com.l47coder.stratum/   <- the package (edit here)
└── manifest.json            <- host project's dependencies
```

Everything under `Assets/` is the host project: it exists so maintainers have
something to click through while iterating on the package. On first launch of
`Tools → Dev Workbench`, the workbench scaffolds the default layout under
`Assets/Game/` from the templates in `Packages/com.l47coder.stratum/Runtime~/Templates/`.

## Prerequisites

- **Unity 2022.3 LTS** (developed against `2022.3.60f1`).
- Git ≥ 2.30 and, if you intend to touch binary art, **Git LFS** — run
  `git lfs install` once per machine.
- Optional: JetBrains Rider or Visual Studio (OmniSharp also works).

## Getting started

```bash
git clone https://github.com/L47-Coder/unity-stratum.git
cd unity-stratum
```

Open the folder with Unity Hub as a normal project. Unity will resolve UPM
dependencies (Addressables, UniTask, VContainer) on first open.

Then:

1. Open `Tools → Dev Workbench`. If the side panel says *"Framework not
   initialised"*, click **Initialise** — this creates the order assets and
   the `Game.Managers.asmdef` container under `Assets/Game/`. Specific
   Manager templates are imported on demand from the Installer tab.
2. Make your changes under `Packages/com.l47coder.stratum/`.
3. Exercise the change through the workbench windows or a scratch scene.

## Coding conventions

- Respect `.editorconfig`: UTF-8, LF line endings, 4-space indent for C#,
  2-space indent for YAML / JSON / UXML / USS, trailing whitespace trimmed.
- `.gitattributes` is the source of truth for Unity YAML merging and LFS
  tracking. Don't commit binary assets without first running
  `git lfs install`.
- Runtime contracts live under `Packages/com.l47coder.stratum/Runtime/`
  and must stay inside the `Stratum` namespace. Editor-only code belongs
  in `Packages/com.l47coder.stratum/Editor/` and uses the
  `Stratum.Editor` namespace.
- Manager templates (`Runtime~/Templates/Managers/`) are **source** that
  ships to the user's `Assets/Game/Manager/` on demand from the Installer
  window — treat them as public API.
- Keep user-visible behavioural changes small and focused; a PR that mixes
  refactor + feature + doc update is hard to review.

## Commit & branch conventions

- Work on a feature branch (`feat/…`, `fix/…`, `docs/…`, `chore/…`).
- Commit messages follow a lightweight
  [Conventional Commits](https://www.conventionalcommits.org/) subset:
  - `feat: …` new functionality
  - `fix: …` bug fix
  - `docs: …` documentation only
  - `refactor: …` no user-visible change
  - `chore: …` tooling / build / CI / meta
  - `test: …` tests only
- Keep commits green — a WIP push that intentionally fails to compile should
  be squashed before review.

## Changelog & versioning

The package follows [Semantic Versioning](https://semver.org/) and
[Keep a Changelog](https://keepachangelog.com/).

- For any user-visible change, append an entry under an `## [Unreleased]`
  heading in [`Packages/com.l47coder.stratum/CHANGELOG.md`](./Packages/com.l47coder.stratum/CHANGELOG.md).
- Releases are cut by bumping `version` in
  [`Packages/com.l47coder.stratum/package.json`](./Packages/com.l47coder.stratum/package.json),
  promoting the `Unreleased` section to the new version heading, and tagging
  the commit as `v<version>` (e.g. `v0.2.0`).

## Release flow

Every release follows the same six-step ritual. The tag name is always
`v<version>`, taken verbatim from the value in `package.json` with a leading
`v` — e.g. `version: "0.2.0"` → tag `v0.2.0`, `version: "1.0.0-rc.1"` → tag
`v1.0.0-rc.1`.

1. **Make sure `main` is clean and up-to-date.**

   ```bash
   git checkout main
   git pull --ff-only
   git status  # should be clean
   ```

2. **Bump the package version.** Edit
   `Packages/com.l47coder.stratum/package.json` and set `version` to the
   new value. Follow [SemVer](https://semver.org/):
   - `0.x` → public API may still break between minors
   - pre-releases use `-preview.N`, `-rc.N`, `-beta.N` suffixes
   - `1.0.0` is the first commitment to a stable public API

3. **Promote the CHANGELOG entry.** In
   `Packages/com.l47coder.stratum/CHANGELOG.md`:
   - rename the top `## [Unreleased]` section to
     `## [<new-version>] — YYYY-MM-DD`
   - open a fresh empty `## [Unreleased]` above it so future PRs have somewhere
     to land

4. **Commit and push.**

   ```bash
   git add Packages/com.l47coder.stratum/package.json \
           Packages/com.l47coder.stratum/CHANGELOG.md
   git commit -m "chore(release): v<new-version>"
   git push origin main
   ```

5. **Create the tag.** Always use an annotated tag (not lightweight) so the
   author and date are recorded:

   ```bash
   git tag -a v<new-version> -m "Stratum <new-version>"
   git push origin v<new-version>
   ```

   If you tagged the wrong commit *and* nobody has depended on it yet, delete
   both sides before re-tagging:

   ```bash
   git tag -d v<new-version>
   git push origin :refs/tags/v<new-version>
   ```

6. **Publish the GitHub Release.** Prefer the CLI:

   ```bash
   # Pre-release (preview / rc / beta) — keeps the "Latest" badge off
   gh release create v<new-version> \
     --title "v<new-version>" \
     --notes-file release-notes.md \
     --prerelease

   # Stable release
   gh release create v<new-version> \
     --title "v<new-version>" \
     --notes-file release-notes.md
   ```

   `release-notes.md` is a throwaway file holding the CHANGELOG section for
   this version. If you don't want to curate it, `--generate-notes` will build
   a PR-based summary automatically.

   Alternatively, open
   `https://github.com/L47-Coder/unity-stratum/releases/new`, pick the
   existing tag, paste the CHANGELOG entry, tick **Set as a pre-release** for
   any `-preview` / `-rc` / `-beta` version, and publish.

### Post-release verification

- The tag appears at <https://github.com/L47-Coder/unity-stratum/tags>.
- The release appears at <https://github.com/L47-Coder/unity-stratum/releases>.
- UPM install with the tag works:

  ```
  https://github.com/L47-Coder/unity-stratum.git?path=Packages/com.l47coder.stratum#v<new-version>
  ```

## Pull requests

1. Open an issue first for non-trivial changes so we can agree on scope.
2. Make sure the editor compiles without new warnings.
3. Run through the flows you touched (Creator, Viewer, Order panels, etc.).
4. Fill in the PR template — especially the *Scope* and *Reproduction / test
   notes* sections.

We squash-merge PRs by default; your commit history on the branch does not
need to be pristine.

## Reporting bugs

Use the **Bug report** issue template. Please include:

- Package version (from `package.json`).
- Unity version and editor platform.
- Minimal steps to reproduce and, when possible, the relevant console output.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](./LICENSE).
