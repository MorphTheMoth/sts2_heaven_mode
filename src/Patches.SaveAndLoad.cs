using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace HeavenMode;

internal static class Patches_SaveAndLoad
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer))]
    [HarmonyPostfix]
    internal static void AfterSetUpNewSinglePlayer()
    {
        HeavenPersistence.SaveCurrentRunSelection();
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer))]
    [HarmonyPostfix]
    internal static void AfterSetUpNewMultiPlayer()
    {
        HeavenPersistence.SaveCurrentRunSelection();
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRun))]
    [HarmonyPostfix]
    internal static void AfterSaveRun()
    {
        HeavenPersistence.SaveCurrentRunSelection();
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer))]
    [HarmonyPrefix]
    internal static void BeforeSetUpSavedSinglePlayer(SerializableRun save)
    {
        RestoreFromSave(save);
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer))]
    [HarmonyPrefix]
    internal static void BeforeSetUpSavedMultiPlayer(object lobby)
    {
        try
        {
            if (lobby == null)
                return;

            // Clients receive the Heaven level via network sync (HeavenLoadRunSync) before BeginRun
            // is triggered. Do not overwrite that synced value with local metadata, which may be
            // absent or stale on a reconnecting client.
            if (lobby is LoadRunLobby runLobby && runLobby.NetService.Type == NetGameType.Client)
                return;

            var runProperty = AccessTools.Property(lobby.GetType(), "Run");
            if (runProperty?.GetValue(lobby) is SerializableRun save)
                RestoreFromSave(save);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeSetUpSavedMultiPlayer failed: {ex}");
        }
    }

    [HarmonyPatch(typeof(NContinueRunInfo), "ShowInfo")]
    [HarmonyPostfix]
    internal static void AfterContinueShowInfo(NContinueRunInfo __instance, SerializableRun save)
    {
        try
        {
            int level = HeavenPersistence.LoadSelection(save.StartTime);
            Log.Info($"[HeavenMode] Continue tooltip lookup: startTime={save.StartTime}, heavenLevel={level}");
            if (level < 1)
                return;

            var ascensionLabel = AccessTools.Field(typeof(NContinueRunInfo), "_ascensionLabel")?.GetValue(__instance);
            if (ascensionLabel == null)
                return;

            ascensionLabel.GetType().GetProperty("Text")?.SetValue(
                ascensionLabel,
                HeavenState.GetRunTitle(level));

            var visibleProp = AccessTools.Property(typeof(Godot.CanvasItem), nameof(Godot.CanvasItem.Visible));
            visibleProp?.SetValue(ascensionLabel, true);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterContinueShowInfo failed: {ex}");
        }
    }

    private static void RestoreFromSave(SerializableRun save)
    {
        try
        {
            HeavenPersistence.RestoreSelection(save.StartTime);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] RestoreFromSave failed: {ex}");
        }
    }
}
