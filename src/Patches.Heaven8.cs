using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Patches_Heaven8
{
    private const int TargetPotionSlots = 1;

    internal static void AfterSetUpNewSinglePlayer(RunState state)
    {
        ApplyStartingPotionLoadout(state);
    }

    internal static void AfterSetUpNewMultiPlayer(RunState state)
    {
        ApplyStartingPotionLoadout(state);
    }

    private static void ApplyStartingPotionLoadout(RunState state)
    {
        try
        {
            if (HeavenState.SelectedOption < HeavenState.PotionLimitLevel)
                return;

            foreach (Player player in state.Players)
            {
                while (player.MaxPotionCount > TargetPotionSlots)
                    player.SubtractFromMaxPotionCount(1);

                bool hasPotionShapedRock = player.Potions.Any(p => p is PotionShapedRock);
                if (!hasPotionShapedRock && player.HasOpenPotionSlots)
                {
                    player.AddPotionInternal(ModelDb.Potion<PotionShapedRock>().ToMutable(), silent: true);
                    Log.Info($"[HeavenMode] Heaven {HeavenState.SelectedOption} starting potion granted: PotionShapedRock for player {player.NetId}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyStartingPotionLoadout failed: {ex}");
        }
    }
}
