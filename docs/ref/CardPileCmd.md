# CardPileCmd

## Heaven 9 hooks

Relevant methods:

- `MegaCrit.Sts2.Core.Commands.CardPileCmd.ShuffleIfNecessary(PlayerChoiceContext, Player)`
- `MegaCrit.Sts2.Core.Hooks.Hook.AfterCardDrawn(CombatState, PlayerChoiceContext, CardModel, bool)`
- `MegaCrit.Sts2.Core.Combat.CombatManager.SetupPlayerTurn(Player, HookPlayerChoiceContext)`

Current mod strategy in this repo:

- patch `ShuffleIfNecessary(...)` to count how many times the draw pile was actually exhausted and reshuffled during the current turn
- patch `Hook.AfterCardDrawn(...)` so every card drawn after the first reshuffle gains a temporary local cost increase equal to the current shuffle count
- patch `SetupPlayerTurn(...)` at the start of the next turn to remove those temporary H9 cost increases and reset the per-turn shuffle counter

Why this split works:

- shuffle count is a turn-level state
- cost increase must be applied per-card, after the card has actually been drawn
- cleanup should happen before the next turn's draw begins
