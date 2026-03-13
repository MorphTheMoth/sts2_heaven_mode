# CreatureCmd

## Feature: command-layer heal execution

Location: `sts2_decompile/sts2/MegaCrit/sts2/Core/Commands/CreatureCmd.cs`

Key method: `public static async Task Heal(Creature creature, Decimal amount, bool playAnim = true)`

Relevant logic:

```csharp
amount = Hook.ModifyHealAmount(..., creature, amount);
Decimal num = Math.Min(amount, (Decimal) (creature.MaxHp - creature.CurrentHp));
creature.HealInternal(amount);

if (playAnim)
{
  ...
}

if (amount > 0M)
  await Hook.AfterCurrentHpChanged(..., creature, amount);
```

Notes:

- `CreatureCmd.Heal(...)` is the command-layer entry point for healing.
- Hook-based heal modifiers run here before `creature.HealInternal(amount)`.
- VFX, history tracking, and `AfterCurrentHpChanged` also depend on the same `amount`.
- If a mod wants Neow's opening heal to end at `10` HP, patching `CreatureCmd.Heal(...)` before execution keeps the whole downstream flow aligned with the capped value.
