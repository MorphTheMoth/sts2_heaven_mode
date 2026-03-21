using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace HeavenMode;

internal static class Patches_Heaven5
{
    private static readonly Dictionary<ulong, CardModel> PendingCostCleanupByPlayer = new();

    internal static void BeforeSetupPlayerTurn(Player player)
    {
        if (HeavenState.SelectedOption < HeavenState.CostIncreaseLevel)
            return;

        if (!PendingCostCleanupByPlayer.TryGetValue(player.NetId, out CardModel? card))
            return;

        try
        {
            card.EnergyCost.AddThisCombat(-1);
            PendingCostCleanupByPlayer.Remove(player.NetId);
            Log.Info($"[HeavenMode] Cleared Heaven5 opening cost increase for {card.Id.Entry}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Heaven5 BeforeSetupPlayerTurn cleanup failed: {ex}");
        }
    }

    internal static Task AfterSetupPlayerTurn(Task __result, Player player)
    {
        if (HeavenState.SelectedOption < HeavenState.CostIncreaseLevel)
            return __result;

        return ApplyOpeningCostIncrease(__result, player);
    }

    private static async Task ApplyOpeningCostIncrease(Task originalTask, Player player)
    {
        await originalTask;

        try
        {
            Creature? creature = player.Creature;
            if (creature?.CombatState == null || creature.IsDead)
                return;

            if (creature.CombatState.CurrentSide != MegaCrit.Sts2.Core.Combat.CombatSide.Player)
                return;

            if (creature.CombatState.RoundNumber != 1)
                return;

            CardPile handPile = PileType.Hand.GetPile(player);
            var eligibleCards = handPile.Cards
                .Where(card => !card.EnergyCost.CostsX && card.EnergyCost.Canonical >= 0)
                .ToList();

            if (eligibleCards.Count == 0)
                return;

            int index = player.RunState.Rng.CombatEnergyCosts.NextInt(eligibleCards.Count);
            CardModel target = eligibleCards[index];
            int oldCost = target.EnergyCost.GetWithModifiers(CostModifiers.Local);
            target.EnergyCost.AddThisCombat(1);
            PendingCostCleanupByPlayer[player.NetId] = target;
            int newCost = target.EnergyCost.GetWithModifiers(CostModifiers.Local);
            Log.Info($"[HeavenMode] Increased opening hand card cost for Heaven={HeavenState.SelectedOption}: {target.Id.Entry} {oldCost}->{newCost}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyOpeningCostIncrease failed: {ex}");
        }
    }
}
