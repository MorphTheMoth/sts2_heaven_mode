# RunManager

## Heaven 3 hooks

- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpNewSinglePlayer(RunState, bool, DateTimeOffset?)`
- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpNewMultiPlayer(RunState, StartRunLobby, bool, DateTimeOffset?)`

These are used as "new run only" markers. Heaven 3 sets a pending flag here so the relic is only added on fresh runs.

- `MegaCrit.Sts2.Core.Runs.RunManager.FinalizeStartingRelics()`

Heaven 3 injects `BloodVial` immediately before `FinalizeStartingRelics()` iterates starting relics. This is a better fit than `Launch()`, because the relic becomes part of the normal starter-relic initialization chain and its `AfterObtained()` logic runs with the rest of the run setup.

## Related Ancient hook

- Act transition healing itself is not injected through `RunManager`.
- The actual heal trigger is `MegaCrit.Sts2.Core.Models.AncientEventModel.BeforeEventStarted()`.
- Heaven 3 replaces that specific Ancient heal flow in Act 2 and Act 3 so the official trigger still drives the recovery, but the heal amount becomes `60%` of missing HP instead of the vanilla amount.
