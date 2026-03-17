# SaveManager

Namespace: `MegaCrit.Sts2.Core.Saves`

## Feature: delete the current run instead of preserving it

Relevant vanilla methods:

- `SaveManager.DeleteCurrentRun()`
- `SaveManager.DeleteCurrentMultiplayerRun()`

## Vanilla behavior

- `DeleteCurrentRun()` removes the profile's `current_run.save`.
- `DeleteCurrentMultiplayerRun()` removes the profile's `current_run_mp.save`.

## Implementation note in this repo

- Heaven 11 uses these deletion methods when the player clicks `Save and Quit`.
- Which method is called depends on `RunManager.Instance.NetService.Type`:
  - singleplayer => `DeleteCurrentRun()`
  - host multiplayer => `DeleteCurrentMultiplayerRun()`

## Result

- The run save is explicitly removed before returning to the main menu.
- This gives Heaven 11 the intended rule: `Save and Quit` means losing the run instead of suspending it.
