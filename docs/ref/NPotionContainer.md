# NPotionContainer

## Heaven 8 UI note

- `MegaCrit.Sts2.Core.Nodes.Potions.NPotionContainer.Initialize(IRunState runState)`
- `MegaCrit.Sts2.Core.Nodes.Potions.NPotionContainer.GrowPotionHolders(int newMaxPotionSlots)`

Vanilla `NPotionContainer` only grows holder nodes. It does not remove or hide extra holders when the player's max potion slot count is reduced later.

Current mod strategy in this repo:

- keep the real inventory limit on `Player`
- patch `NPotionContainer.Initialize(...)` and `GrowPotionHolders(...)` with postfixes
- do nothing unless vanilla holder count exceeds the player's real `MaxPotionCount`
- when that mismatch happens, only hide surplus holders
- keep the vanilla layout unchanged in all cases

Why this is needed:

- Heaven 8 reduces the actual slot count to `1`
- in mismatch cases, extra holders can remain visible even though the player cannot store another potion there
