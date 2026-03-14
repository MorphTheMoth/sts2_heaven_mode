using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HeavenMode;

internal static class Patches_Heaven4
{
    internal static Task AfterSetupPlayerTurn(Task __result, Player player)
    {
        if (HeavenState.SelectedOption < HeavenState.BurnLevel)
            return __result;

        return ApplyHeavenBurn(__result, player);
    }

    private static async Task ApplyHeavenBurn(Task originalTask, Player player)
    {
        await originalTask;

        try
        {
            Creature? creature = player.Creature;
            if (creature == null || creature.IsDead || creature.CombatState == null)
                return;

            if (creature.CombatState.CurrentSide != MegaCrit.Sts2.Core.Combat.CombatSide.Player)
                return;

            if (creature.CombatState.RoundNumber < 3)
                return;

            await CardPileCmd.AddToCombatAndPreview<Burn>(creature, PileType.Draw, 1, false, CardPilePosition.Random);
            Log.Info($"[HeavenMode] Added Burn to draw pile after draw step at round {creature.CombatState.RoundNumber} for Heaven={HeavenState.SelectedOption}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyHeavenBurn failed: {ex}");
        }
    }
}
