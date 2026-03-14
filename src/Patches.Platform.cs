using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Patches_Platform
{
    [HarmonyPatch(typeof(RunManager), "UpdateRichPresence")]
    [HarmonyPostfix]
    internal static void AfterUpdateRichPresence()
    {
        try
        {
            if (HeavenState.SelectedOption < 1)
                return;

            string heavenPresence = $"{HeavenState.SelectedOption} - 天堂";
            PlatformUtil.SetRichPresenceValue("Ascension", heavenPresence);
            Log.Info($"[HeavenMode] Updated Steam rich presence ascension to {heavenPresence}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterUpdateRichPresence failed: {ex}");
        }
    }
}
