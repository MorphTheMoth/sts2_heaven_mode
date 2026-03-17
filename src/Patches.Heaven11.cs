using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HeavenMode;

[HarmonyPatch(typeof(NPauseMenu))]
internal static class Patches_Heaven11
{
    [HarmonyPrefix]
    [HarmonyPatch("OnSaveAndQuitButtonPressed")]
    private static bool BeforeOnSaveAndQuitButtonPressed(NPauseMenu __instance)
    {
        try
        {
            if (!HeavenState.ShouldDestroySaveOnQuit)
                return true;

            TaskHelper.RunSafely(DestroySaveAndReturnToMenu(__instance));
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeOnSaveAndQuitButtonPressed failed: {ex}");
            return true;
        }
    }

    private static async Task DestroySaveAndReturnToMenu(NPauseMenu pauseMenu)
    {
        try
        {
            DisablePauseMenu(pauseMenu);

            RunManager.Instance.ActionQueueSet.Reset();
            NRunMusicController.Instance?.StopMusic();

            if (SaveManager.Instance.CurrentRunSaveTask != null)
            {
                try
                {
                    await SaveManager.Instance.CurrentRunSaveTask;
                }
                catch (Exception ex)
                {
                    Log.Warn($"[HeavenMode] Save task failed before Heaven 11 cleanup: {ex.Message}");
                }
            }

            switch (RunManager.Instance.NetService.Type)
            {
                case NetGameType.Singleplayer:
                    SaveManager.Instance.DeleteCurrentRun();
                    break;
                case NetGameType.Host:
                    SaveManager.Instance.DeleteCurrentMultiplayerRun();
                    break;
            }

            HeavenPersistence.ClearCurrentRunSelection();
            Log.Info("[HeavenMode] Heaven 11 intercepted Save and Quit; deleted current run save instead");

            if (NGame.Instance != null)
                await NGame.Instance.ReturnToMainMenu();
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] DestroySaveAndReturnToMenu failed: {ex}");
        }
    }

    private static void DisablePauseMenu(NPauseMenu pauseMenu)
    {
        try
        {
            pauseMenu.GetNode("%BackButton").Call("Disable");

            string[] buttonNodeNames =
            {
                "%ButtonContainer/Resume",
                "%ButtonContainer/Settings",
                "%ButtonContainer/Compendium",
                "%ButtonContainer/GiveUp",
                "%ButtonContainer/Disconnect",
                "%ButtonContainer/SaveAndQuit",
            };

            foreach (string nodeName in buttonNodeNames)
                pauseMenu.GetNode(nodeName).Call("Disable");
        }
        catch (Exception ex)
        {
            Log.Warn($"[HeavenMode] DisablePauseMenu failed: {ex.Message}");
        }
    }
}
