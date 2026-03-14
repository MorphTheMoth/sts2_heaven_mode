using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;

namespace HeavenMode;

/// <summary>
/// Heaven mode ancient adjustments:
/// - Heaven 1+: reduce ancient initial option counts by act.
/// - Heaven 6+: override Neow's opening HP restore to 36.
/// </summary>
internal static class Patches_Player
{
    private static readonly MethodInfo? SetHealedAmountMethod =
        AccessTools.PropertySetter(typeof(AncientEventModel), nameof(AncientEventModel.HealedAmount));

    private static readonly MethodInfo? SetEventStateMethod =
        AccessTools.Method(typeof(EventModel), "SetEventState");

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
            if (!HeavenState.ShouldOverrideNeowOpeningHp) return;
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
            if (__instance.Owner?.Creature == null)
                return true;

            if (__instance is Neow)
            {
                if (!HeavenState.ShouldOverrideNeowOpeningHp)
                    return true;

                __result = HandleNeowStartAtConfiguredHp(__instance);
                Log.Info($"[HeavenMode] Replaced Neow BeforeEventStarted flow for Heaven={HeavenState.SelectedOption}");
                return false;
            }

            if (HeavenState.SelectedOption >= HeavenState.ActRecoveryLevel && __instance.Owner.RunState.CurrentActIndex > 0)
            {
                __result = HandleAncientActHealAtConfiguredAmount(__instance);
                Log.Info($"[HeavenMode] Replaced Ancient act heal flow in act {__instance.Owner.RunState.CurrentActIndex + 1} for Heaven={HeavenState.SelectedOption}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeAncientEventStarted failed: {ex}");
            return true;
        }
    }

    [HarmonyPatch(typeof(AncientEventModel), "SetInitialEventState")]
    [HarmonyPostfix]
    internal static void AfterSetInitialEventState(AncientEventModel __instance, bool isPreFinished)
    {
        try
        {
            if (!HeavenState.HasAncientChoiceRestriction)
                return;
            if (isPreFinished)
                return;
            if (__instance.Owner?.RunState == null)
                return;

            int limit = HeavenState.GetAncientInitialOptionLimit(__instance.Owner.RunState.CurrentActIndex);
            IReadOnlyList<EventOption> currentOptions = __instance.CurrentOptions;
            if (currentOptions.Count <= limit)
                return;

            LocString description = __instance.Description ?? __instance.InitialDescription;
            List<EventOption> trimmedOptions = currentOptions.Take(limit).ToList();
            SetEventStateMethod?.Invoke(__instance, new object[] { description, trimmedOptions });
            Log.Info($"[HeavenMode] Limited {__instance.Id.Entry} options to {limit} in act {__instance.Owner.RunState.CurrentActIndex + 1} for Heaven={HeavenState.SelectedOption}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterSetInitialEventState failed: {ex}");
        }
    }

    private static async Task HandleNeowStartAtConfiguredHp(AncientEventModel ancientEventModel)
    {
        if (ancientEventModel.Owner?.Creature == null)
            return;

        Creature creature = ancientEventModel.Owner.Creature;

        creature.SetCurrentHpInternal(0M);
        int oldHp = creature.CurrentHp;

        await CreatureCmd.Heal(creature, HeavenState.NeowOpeningHp, false);

        if (NRun.Instance != null)
            _ = TaskHelper.RunSafely(NRun.Instance.GlobalUi.TopBar.Hp.LerpAtNeow());

        SetHealedAmountMethod?.Invoke(ancientEventModel, new object[] { creature.CurrentHp - oldHp });
        Log.Info($"[HeavenMode] Set Neow opening HP to {creature.CurrentHp}");
    }

    private static async Task HandleAncientActHealAtConfiguredAmount(AncientEventModel ancientEventModel)
    {
        if (ancientEventModel.Owner?.Creature == null)
            return;

        Creature creature = ancientEventModel.Owner.Creature;
        int oldHp = creature.CurrentHp;
        int missingHp = creature.MaxHp - creature.CurrentHp;
        if (missingHp > 0)
        {
            decimal amount = Math.Ceiling(missingHp * HeavenState.ActRecoveryMissingPercent);
            await CreatureCmd.Heal(creature, amount, false);
        }

        SetHealedAmountMethod?.Invoke(ancientEventModel, new object[] { creature.CurrentHp - oldHp });
        Log.Info($"[HeavenMode] Set Ancient act heal to restore {HeavenState.ActRecoveryMissingPercent:P0} missing HP: {oldHp}->{creature.CurrentHp}");
    }
}
