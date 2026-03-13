# AncientEventModel

## Feature: Neow start HP reset and heal

Location: `sts2_decompile/sts2/MegaCrit/sts2/Core/Models/AncientEventModel.cs`

Key method: `protected override async Task BeforeEventStarted()`

Relevant logic:

```csharp
if (ancientEventModel is Neow)
  ancientEventModel.Owner.Creature.SetCurrentHpInternal(0M);

int oldHp = ancientEventModel.Owner.Creature.CurrentHp;
Decimal amount = (Decimal) (ancientEventModel.Owner.Creature.MaxHp - ancientEventModel.Owner.Creature.CurrentHp);

if (RunManager.Instance.HasAscension(AscensionLevel.WearyTraveler))
  amount *= 0.8M;

await CreatureCmd.Heal(ancientEventModel.Owner.Creature, amount, false);
```

Notes:

- Neow start does not set current HP directly to full.
- It first sets player HP to `0`.
- It then restores HP through `CreatureCmd.Heal(...)` using the missing HP as the heal amount.
- For Heaven mode option `1`, the stable interception point is the Neow heal command, not a later `HealInternal` postfix.
- `HealedAmount` is computed after the heal from the final `CurrentHp`, so changing the heal amount early keeps later state consistent.

## HeavenMode note

Current mod strategy in this repo:

- Patch `AncientEventModel.BeforeEventStarted()` with a Harmony prefix.
- Only when `__instance is Neow` and Heaven option `1` is selected, skip the original method.
- Run a custom replacement flow:
  - `SetCurrentHpInternal(0)`
  - `CreatureCmd.Heal(..., 10M, false)`
  - `LerpAtNeow()`
  - write `HealedAmount`

## Confirmed implementation for this repo

Goal:

- When the player selects Heaven option `1` or `2`, entering Neow should leave current HP at `10`.
- Current repo logic treats Heaven option `2` as including Heaven option `1` for this effect.

Confirmed hook point:

- Class: `MegaCrit.Sts2.Core.Models.AncientEventModel`
- Method: `BeforeEventStarted()`

Why this works:

- Neow opening HP restoration is initiated directly in `AncientEventModel.BeforeEventStarted()`.
- Patching later points such as `HealInternal` or unrelated event UI state is less reliable.
- A Harmony prefix on `BeforeEventStarted()` can fully replace the Neow-specific start flow and keep the result stable.

Confirmed replacement flow used by this mod:

```csharp
creature.SetCurrentHpInternal(0M);
await CreatureCmd.Heal(creature, 10M, false);
TaskHelper.RunSafely(NRun.Instance.GlobalUi.TopBar.Hp.LerpAtNeow());
SetHealedAmountMethod?.Invoke(ancientEventModel, new object[] { creature.CurrentHp - oldHp });
```

Related helper method also observed:

- Class: `MegaCrit.Sts2.Core.Entities.Creatures.Creature`
- Method: `SetCurrentHpInternal(Decimal amount)`

Usage in this mod:

- A postfix on `SetCurrentHpInternal` is useful for verification logging.
- The actual functional fix should remain at `AncientEventModel.BeforeEventStarted()`.
