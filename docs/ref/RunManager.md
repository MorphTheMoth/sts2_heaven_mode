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

## Heaven 10 hooks

- `MegaCrit.Sts2.Core.Runs.RunManager.ProceedFromTerminalRewardsScreen()`

Heaven 10 uses a prefix here to intercept the terminal reward proceed flow after an Act 3 boss room when the flow actually goes through the terminal proceed path. Instead of letting vanilla continue, the mod can immediately launch a third boss combat, then resume the vanilla final-act proceed flow after that third boss is cleared.

- `MegaCrit.Sts2.Core.Runs.RunManager.EnterNextAct()`

Act 3 second-boss rewards do not always proceed through `ProceedFromTerminalRewardsScreen()`. In the vanilla double-boss path, the rewards screen can instead route through `ActChangeSynchronizer -> EnterNextAct()`. Heaven 10 therefore also prefixes `EnterNextAct()` and, when the current room is the final act's second boss, replaces the act transition with a third boss launch. After that third boss is cleared, the same prefix allows the vanilla final-act ending flow to continue.

- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpNewSinglePlayer(...)`
- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpNewMultiPlayer(...)`
- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpSavedSinglePlayer(...)`
- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpSavedMultiPlayer(...)`

These are used to reset or prepare the Heaven 10 runtime state and determine the extra Act 3 boss encounter that is not already assigned to vanilla `BossEncounter` or `SecondBossEncounter`.
