# ModManager

Namespace: `MegaCrit.Sts2.Core.Modding`

## Feature: record Heaven clear progression on victory

Relevant vanilla surface:

- `ModManager.OnMetricsUpload`
- `ModManager.CallMetricsHooks(SerializableRun run, bool isVictory, ulong localPlayerId)`

## Implementation note in this repo

Use `ModManager.OnMetricsUpload` as the post-run hook for Heaven progression.

- The event provides:
  - `SerializableRun run`
  - `bool isVictory`
  - `ulong localPlayerId`
- Repo logic reads the Heaven sidecar for `run.StartTime`
- If the run was a victory and the Heaven level is `>= 1`, the mod:
  - resolves the local player's `CharacterId` from `run.Players`
  - records that Heaven level as cleared in the profile-scoped unlock-progress file

This avoids patching run history creation directly and stays aligned with the game's metrics upload flow.
