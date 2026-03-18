using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace HeavenMode;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
internal static class Patches_MultiplayerCharacterSelect
{
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, NAscensionPanel> AscensionPanelRef =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, NAscensionPanel>("_ascensionPanel");

    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, StartRunLobby> LobbyRef =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, StartRunLobby>("_lobby");

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost))]
    private static void AfterInitializeMultiplayerAsHost(NCharacterSelectScreen __instance)
    {
        TryRegister(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient))]
    private static void AfterInitializeMultiplayerAsClient(NCharacterSelectScreen __instance)
    {
        TryRegister(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuOpened))]
    private static void AfterOnSubmenuOpened(NCharacterSelectScreen __instance)
    {
        try
        {
            HeavenMultiplayerSync.OnLobbyOpened(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterOnSubmenuOpened failed: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCharacterSelectScreen.PlayerConnected))]
    private static void AfterPlayerConnected(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        try
        {
            HeavenMultiplayerSync.OnPlayerConnected(__instance, player);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterPlayerConnected failed: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCharacterSelectScreen.AscensionChanged))]
    private static void AfterAscensionChanged(NCharacterSelectScreen __instance)
    {
        try
        {
            StartRunLobby lobby = LobbyRef(__instance);
            if (HeavenState.SelectedOption > 0 || lobby.MaxAscension > 0)
            {
                NAscensionPanel panel = AscensionPanelRef(__instance);
                ((Godot.CanvasItem)panel).Visible = true;
            }
        }

        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterAscensionChanged failed: {ex}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("CleanUpLobby")]
    private static void BeforeCleanUpLobby(NCharacterSelectScreen __instance)
    {
        try
        {
            HeavenMultiplayerSync.UnregisterLobby(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeCleanUpLobby failed: {ex}");
        }
    }

    private static void TryRegister(NCharacterSelectScreen screen)
    {
        try
        {
            HeavenMultiplayerSync.RegisterLobby(screen);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] TryRegister multiplayer Heaven sync failed: {ex}");
        }
    }
}
