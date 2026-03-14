# Creature

## Heaven 10 hook

- `MegaCrit.Sts2.Core.Entities.Creatures.Creature.AfterTurnStart(int roundNumber, CombatSide side)`

Heaven 10 uses `AfterTurnStart(...)` during the Heaven-only third Act 3 boss fight. Starting from round 2, when the enemy side begins its turn, the mod lets the primary boss trigger once per round and burn one random draw-pile card from `ceil(playerCount / 2)` living players. If a selected player has no cards in their draw pile, that player is skipped.
