# Player

## Heaven 8 hook

- `MegaCrit.Sts2.Core.Entities.Players.Player.SubtractFromMaxPotionCount(int)`
- `MegaCrit.Sts2.Core.Entities.Players.Player.AddPotionInternal(PotionModel, int slotIndex = -1, bool silent = false)`

Heaven 8 does not patch the potion UI directly. It modifies the player's actual inventory during new-run setup.

Current mod strategy in this repo:

- after `RunManager.SetUpNewSinglePlayer(...)` / `SetUpNewMultiPlayer(...)`
- shrink each player's max potion slots down to `1`
- if the player does not already have `PotionShapedRock` and still has an open slot, add one directly to inventory

Why this hook:

- the slot count is part of `Player` state and persists into saves
- the potion container UI already listens to `MaxPotionCountChanged`
- starting loadout is best expressed as inventory state, not as a presentation override
