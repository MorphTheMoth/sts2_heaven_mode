using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;

namespace HeavenMode;

/// <summary>
/// Override Neow's opening HP restore for Heaven options 1+.
/// Instead of restoring to full inside AncientEventModel.BeforeEventStarted,
/// restore to exactly 10 HP and keep the rest of the event flow intact.
/// </summary>
internal static class Patches_Player
{
    private static readonly MethodInfo? SetHealedAmountMethod =
        AccessTools.PropertySetter(typeof(AncientEventModel), nameof(AncientEventModel.HealedAmount));

    static Patches_Player()
    {
        Log.Info("[HeavenMode] Patches_Player type loaded");
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.SetCurrentHpInternal))]
    [HarmonyPostfix]
    internal static void AfterSetCurrentHp(Creature __instance)
    {
        try
        {
            if (HeavenState.SelectedOption < 1) return;
            if (__instance.Player == null) return;
            if (__instance.CurrentHp == 0)
                Log.Info($"[HeavenMode] Observed HP reset to 0 for player {__instance.Player.NetId}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterSetCurrentHp failed: {ex}");
        }
    }

    [HarmonyPatch(typeof(AncientEventModel), "BeforeEventStarted")]
    [HarmonyPrefix]
    internal static bool BeforeAncientEventStarted(AncientEventModel __instance, ref Task __result)
    {
        try
        {
            if (HeavenState.SelectedOption < 1) return true;
            if (__instance is not Neow) return true;
            if (__instance.Owner?.Creature == null) return true;

            __result = HandleNeowStartAtTenHp(__instance);
            Log.Info($"[HeavenMode] Replaced Neow BeforeEventStarted flow for Heaven={HeavenState.SelectedOption}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeAncientEventStarted failed: {ex}");
            return true;
        }
    }

    private static async Task HandleNeowStartAtTenHp(AncientEventModel ancientEventModel)
    {
        if (ancientEventModel.Owner?.Creature == null)
            return;

        Creature creature = ancientEventModel.Owner.Creature;

        creature.SetCurrentHpInternal(0M);
        int oldHp = creature.CurrentHp;

        await CreatureCmd.Heal(creature, 10M, false);

        if (NRun.Instance != null)
            _ = TaskHelper.RunSafely(NRun.Instance.GlobalUi.TopBar.Hp.LerpAtNeow());

        SetHealedAmountMethod?.Invoke(ancientEventModel, new object[] { creature.CurrentHp - oldHp });
        Log.Info($"[HeavenMode] Set Neow opening HP to {creature.CurrentHp}");
    }
}
