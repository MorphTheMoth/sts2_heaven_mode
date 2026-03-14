# AncientEventModel

## Feature: Ancient option limits and Neow HP override

Location: `sts2_decompile/sts2/MegaCrit/sts2/Core/Models/AncientEventModel.cs`

Key method: `protected override async Task BeforeEventStarted()`

Relevant vanilla logic:

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

- Patch `AncientEventModel.SetInitialEventState(bool)` with a postfix.
- When Heaven `1+` is selected, limit initial ancient options by act:
  - Act 1 ancient: `2` options
  - Act 2 and Act 3 ancient: `1` option
- Patch `AncientEventModel.BeforeEventStarted()` with a Harmony prefix.
- Only when `__instance is Neow` and Heaven `6+` is selected, skip the original method.
- Run a custom replacement flow:
  - `SetCurrentHpInternal(0)`
  - `CreatureCmd.Heal(..., 36M, false)`
  - `LerpAtNeow()`
  - write `HealedAmount`
- Also when Heaven `3+` is selected and the run is entering Act 2 or Act 3, skip the original Ancient heal flow and replace only the heal amount.
- The trigger stays the vanilla Ancient event startup; only the amount changes to `60%` of missing HP.

## Confirmed implementation for this repo

Confirmed hook point:

- Class: `MegaCrit.Sts2.Core.Models.AncientEventModel`
- Methods:
  - `BeforeEventStarted()`
  - `SetInitialEventState(bool isPreFinished)`

Why this works:

- Neow opening HP restoration is initiated directly in `AncientEventModel.BeforeEventStarted()`.
- Patching later points such as `HealInternal` or unrelated event UI state is less reliable.
- A Harmony prefix on `BeforeEventStarted()` can fully replace the Neow-specific start flow and keep the result stable.

Confirmed replacement flow used by this mod for Heaven `6+`:

```csharp
creature.SetCurrentHpInternal(0M);
await CreatureCmd.Heal(creature, 36M, false);
TaskHelper.RunSafely(NRun.Instance.GlobalUi.TopBar.Hp.LerpAtNeow());
SetHealedAmountMethod?.Invoke(ancientEventModel, new object[] { creature.CurrentHp - oldHp });
```

Confirmed replacement flow used by this mod for Heaven `3+` act transitions:

```csharp
int missingHp = creature.MaxHp - creature.CurrentHp;
decimal amount = Math.Ceiling(missingHp * 0.6M);
await CreatureCmd.Heal(creature, amount, false);
SetHealedAmountMethod?.Invoke(ancientEventModel, new object[] { creature.CurrentHp - oldHp });
```

Related helper method also observed:

- Class: `MegaCrit.Sts2.Core.Entities.Creatures.Creature`
- Method: `SetCurrentHpInternal(Decimal amount)`

Usage in this mod:

- A postfix on `SetCurrentHpInternal` is useful for verification logging when Heaven `6+` is active.
- Ancient initial option count is best adjusted after `SetInitialEventState(bool)` has produced the vanilla option list.
