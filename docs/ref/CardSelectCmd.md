# CardSelectCmd

## Event card-removal hook

- `MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromDeckForRemoval(Player, CardSelectorPrefs, Func<CardModel, bool>?)`
  is the shared selector used by event-driven deck removal flows.
- Event models call this method before `CardPileCmd.RemoveFromDeck(...)`, so it is the cleanest place to detect that the player really entered a remove-card choice and actually selected cards.

## Heaven 2 implementation

- Heaven 2+ uses a Harmony postfix on `CardSelectCmd.FromDeckForRemoval(...)`.
- The postfix wraps the returned `Task<IEnumerable<CardModel>>`.
- When all of these are true:
  - Heaven level is at least `2`
  - the local player selected at least one card
  - the current room is `MegaCrit.Sts2.Core.Rooms.EventRoom` or `MegaCrit.Sts2.Core.Rooms.MerchantRoom`
- then the mod reduces current HP by `3`, clamped to a minimum of `1`.
- After the HP loss, the mod also plays non-combat feedback:
  - `PlayerHurtVignetteHelper.Play()` for the fullscreen hurt vignette
  - `NDamageNumVfx.Create(Vector2, int)` for the floating damage number
  - `VfxCmd.PlayNonCombatVfx(...)` with `VfxCmd.bloodyImpactPath`
  - `NGame.Instance.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short)`

## Why this hook

- It does not grant any free merchant removal.
- It only triggers when an event or merchant actually asks the player to remove cards.
- Relic, rest site, and other non-event, non-merchant removal flows are excluded by the room-type check.
