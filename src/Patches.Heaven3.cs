using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Patches_Heaven3
{
    private static RunState? _pendingStarterRelicRunState;

    internal static void AfterSetUpNewSinglePlayer(RunState state)
    {
        QueueStartingRelicGrant(state);
    }

    internal static void AfterSetUpNewMultiPlayer(RunState state)
    {
        QueueStartingRelicGrant(state);
    }

    internal static void BeforeFinalizeStartingRelics(RunManager __instance)
    {
        TryGrantStartingRelic(__instance);
    }

    private static void QueueStartingRelicGrant(RunState state)
    {
        try
        {
            if (HeavenState.SelectedOption < HeavenState.ActRecoveryLevel)
                return;

            if (state.CurrentActIndex != 0)
                return;

            _pendingStarterRelicRunState = state;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] QueueStartingRelicGrant failed: {ex}");
        }
    }

    private static void TryGrantStartingRelic(RunManager runManager)
    {
        try
        {
            RunState? state = _pendingStarterRelicRunState;
            if (state == null)
                return;

            _pendingStarterRelicRunState = null;
            if (HeavenState.SelectedOption < HeavenState.ActRecoveryLevel)
                return;

            Player? localPlayer = state.Players.FirstOrDefault(p => p.NetId == runManager.NetService.NetId)
                ?? state.Players.FirstOrDefault();
            if (localPlayer == null)
                return;

            bool alreadyHasBloodVial = localPlayer.Relics.Any(r => r is BloodVial);
            if (alreadyHasBloodVial)
                return;

            GiveStartingBloodVial(localPlayer).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] TryGrantStartingRelic failed: {ex}");
        }
    }

    private static async Task GiveStartingBloodVial(Player player)
    {
        try
        {
            var relic = await RelicCmd.Obtain<BloodVial>(player);
            Log.Info($"[HeavenMode] Heaven {HeavenState.SelectedOption} starting relic granted: {relic.Id.Entry}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] GiveStartingBloodVial failed: {ex}");
        }
    }
}
