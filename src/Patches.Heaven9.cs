using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace HeavenMode;

internal static class Patches_Heaven9
{
    private static readonly Dictionary<ulong, int> ShuffleCountsByPlayer = new();
    private static readonly Dictionary<ulong, Dictionary<CardModel, int>> PendingCostCleanupByPlayer = new();
    private static readonly HashSet<ulong> PlayersInSetupDraw = new();

    internal static void BeforeShuffleIfNecessary(Player player, out bool __state)
    {
        __state = HeavenState.SelectedOption >= HeavenState.ShuffleTaxLevel
            && !PlayersInSetupDraw.Contains(player.NetId)
            && PileType.Draw.GetPile(player).Cards.Count == 0
            && PileType.Discard.GetPile(player).Cards.Count > 0;
    }

    internal static void BeforeSetupPlayerTurn(Player player)
    {
        if (HeavenState.SelectedOption < HeavenState.ShuffleTaxLevel)
            return;

        ClearPendingCostIncreases(player);
        ShuffleCountsByPlayer[player.NetId] = 0;
        PlayersInSetupDraw.Add(player.NetId);
    }

    internal static Task AfterSetupPlayerTurn(Task __result, Player player)
    {
        return FinishSetupDraw(__result, player);
    }

    private static async Task FinishSetupDraw(Task originalTask, Player player)
    {
        await originalTask;
        PlayersInSetupDraw.Remove(player.NetId);
    }

    internal static Task AfterShuffleIfNecessary(Task __result, Player player, bool __state)
    {
        if (HeavenState.SelectedOption < HeavenState.ShuffleTaxLevel || !__state)
            return __result;

        return TrackShuffle(__result, player);
    }

    internal static Task AfterHookAfterCardDrawn(
        Task __result,
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (HeavenState.SelectedOption < HeavenState.ShuffleTaxLevel)
            return __result;

        return ApplyDrawCostIncrease(__result, card);
    }

    private static async Task TrackShuffle(Task originalTask, Player player)
    {
        await originalTask;

        try
        {
            int newCount = ShuffleCountsByPlayer.TryGetValue(player.NetId, out int currentCount)
                ? currentCount + 1
                : 1;
            ShuffleCountsByPlayer[player.NetId] = newCount;
            Log.Info($"[HeavenMode] Heaven {HeavenState.SelectedOption} shuffle tax count for player {player.NetId}: {newCount}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] TrackShuffle failed: {ex}");
        }
    }

    private static async Task ApplyDrawCostIncrease(Task originalTask, CardModel card)
    {
        await originalTask;

        try
        {
            Player? owner = card.Owner;
            if (owner == null)
                return;

            if (!ShuffleCountsByPlayer.TryGetValue(owner.NetId, out int shuffleCount) || shuffleCount <= 0)
                return;

            if (card.EnergyCost.CostsX || card.EnergyCost.Canonical < 0)
                return;

            card.EnergyCost.AddThisCombat(shuffleCount);

            if (!PendingCostCleanupByPlayer.TryGetValue(owner.NetId, out Dictionary<CardModel, int>? pending))
            {
                pending = new Dictionary<CardModel, int>();
                PendingCostCleanupByPlayer[owner.NetId] = pending;
            }

            pending[card] = pending.TryGetValue(card, out int existingAmount)
                ? existingAmount + shuffleCount
                : shuffleCount;

            Log.Info($"[HeavenMode] Applied Heaven {HeavenState.SelectedOption} shuffle tax to {card.Id.Entry}: +{shuffleCount} this combat until next turn");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyDrawCostIncrease failed: {ex}");
        }
    }

    private static void ClearPendingCostIncreases(Player player)
    {
        try
        {
            if (!PendingCostCleanupByPlayer.TryGetValue(player.NetId, out Dictionary<CardModel, int>? pending))
                return;

            foreach ((CardModel card, int amount) in pending.ToList())
            {
                if (card != null)
                    card.EnergyCost.AddThisCombat(-amount);
            }

            pending.Clear();
            PendingCostCleanupByPlayer.Remove(player.NetId);
            Log.Info($"[HeavenMode] Cleared Heaven {HeavenState.SelectedOption} shuffle tax cost increases for player {player.NetId}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ClearPendingCostIncreases failed: {ex}");
        }
    }
}
