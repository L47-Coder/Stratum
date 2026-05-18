# Stratum v0.5.3

Release date: 2026-05-18

## Summary

This patch release restores lightweight built-in Manager imports on top of the
single-file Manager model introduced in `0.5.2`, and tightens Manager order
sync timing so runtime registration is updated before Play Mode.

## Highlights

- Added `Manager > Import`, a compact Workbench tab that scans
  `Templates~/Managers/*.cs` and imports selected Manager templates directly
  into `Assets/Game/Manager`.
- Restored bare single-file templates for `EventManager`, `MessageManager` and
  `TaskManager`.
- Removed the old template manifest and per-template folders; template names
  now come directly from `.cs` file names.
- `Manager > Order` now reloads and syncs when opened.
- Manager order also syncs before entering Play Mode, preserving order by
  pruning stale entries and appending new Managers at the end.

## Install

```text
https://github.com/L47-Coder/Stratum.git?path=Packages/com.l47coder.stratum#v0.5.3
```
