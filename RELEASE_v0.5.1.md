# Stratum v0.5.1

Release date: 2026-05-16

## Summary

This patch release refreshes the built-in Manager templates from the current
dogfooded `Assets/Game/Manager` implementation and fixes several runtime
lifecycle issues found during startup and Manager logic review.

## Highlights

- Fixed VContainer registration conflict caused by registering Managers as
  `IManager` twice.
- Fully refreshed `Templates~/Managers` from the current host Manager tree,
  excluding Unity `.meta` files.
- Included root template assembly files so new template installs match the
  current generated host layout.
- Updated the Manager template manifest for the refreshed built-in templates.
- Hardened Asset, Event, Message and Task Manager runtime behavior around
  failed loads, cancellation, timeout cleanup and task lifecycle handling.

## Breaking Or Migration Notes

- `AssetManager` now exposes direct address-based loading:
  `LoadAsync<T>(string address)` and `ReleaseAllAsync()`.
- Existing projects using the older `IAssetHandle<T>` template API should update
  their calls before installing or copying the refreshed template.

## Verification

- `dotnet build Game.Managers.csproj`
- `dotnet build Stratum.sln --no-restore`

Both builds pass with 0 errors.

## Install

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.1
```
