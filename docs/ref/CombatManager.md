# CombatManager

## Heaven 4 hook

- `MegaCrit.Sts2.Core.Combat.CombatManager.SetupPlayerTurn(Player, HookPlayerChoiceContext)`

Heaven 4 uses a postfix on `SetupPlayerTurn(...)`. This is later than `Creature.AfterTurnStart(...)` and happens after the vanilla hand draw for the player's turn is finished.

For this mod:

- only apply to the player whose turn is being prepared
- only apply when `RoundNumber >= 3`
- insert the card after the draw step, so it stays in the draw pile instead of being drawn immediately

Injected behavior:

```csharp
await CardPileCmd.AddToCombatAndPreview<Burn>(player.Creature, PileType.Draw, 1, false, CardPilePosition.Random);
```

Why this hook:

- it runs in the real combat turn-start sequence
- it is after `CardPileCmd.Draw(...)` inside `SetupPlayerTurn(...)`
- it reuses the vanilla `Burn` card model
- it reuses the vanilla card-pile insertion command and preview flow

Related helper:

- `MegaCrit.Sts2.Core.Commands.CardPileCmd.AddToCombatAndPreview<T>(Creature, PileType, int, bool, CardPilePosition)`

## Heaven 5 hook

- `MegaCrit.Sts2.Core.Combat.CombatManager.SetupPlayerTurn(Player, HookPlayerChoiceContext)`

Heaven 5 reuses the same `SetupPlayerTurn(...)` postfix, because the first-turn hand already exists at that point.

For this mod:

- only apply when `RoundNumber == 1`
- only inspect the current hand
- randomly pick one eligible hand card
- apply `card.EnergyCost.AddThisCombat(1)`

This keeps the effect scoped to the current combat while reusing the vanilla temporary energy-cost modifier system.
