# NPotionContainer

## Heaven 8 UI note

- `MegaCrit.Sts2.Core.Nodes.Potions.NPotionContainer.Initialize(IRunState runState)`
- `MegaCrit.Sts2.Core.Nodes.Potions.NPotionContainer.GrowPotionHolders(int newMaxPotionSlots)`

Vanilla `NPotionContainer` only grows holder nodes. It does not remove or hide extra holders when the player's max potion slot count is reduced later.

Current mod strategy in this repo:

- keep the real inventory limit on `Player`
- patch `NPotionContainer.Initialize(...)` and `GrowPotionHolders(...)` with postfixes
- hide any holder whose index is outside the Heaven-adjusted visible slot count
- collapse extra holders to zero scale/size
- shrink the holder container width to the visible slot count

Why this is needed:

- Heaven 8 reduces the actual slot count to `1`
- without a UI patch, the second empty frame can still remain visible or keep occupying layout width even though the player cannot store another potion
