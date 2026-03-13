using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace HeavenMode;

[ModInitializer("Initialize")]
public static class ModEntry
{
    public static void Initialize()
    {
        try
        {
            Assembly assembly = typeof(ModEntry).Assembly;
            int patchTypeCount = assembly.GetTypes().Count(t =>
                t.GetCustomAttributes(inherit: false).Any(a =>
                    a.GetType().Name is "HarmonyPatch" or "HarmonyPatchAttribute") ||
                t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Any(m => m.GetCustomAttributes(inherit: false).Any(a =>
                        a.GetType().Name is "HarmonyPatch" or "HarmonyPatchAttribute")));

            Harmony harmony = new("com.heavenmode");
            harmony.PatchAll(assembly);
            ApplyManualPatches(harmony);
            Log.Info($"[HeavenMode] PatchAll applied for {assembly.GetName().Name}, patch types discovered: {patchTypeCount}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Initialize PatchAll failed: {ex}");
        }

        Task.Delay(3000).ContinueWith(_ => PrintInit());
    }

    private static void PrintInit()
    {
        try
        {
            Log.Info(Loc.Get("MOD_INIT", "Heaven Mode initialized."));
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PrintInit failed: {ex}");
        }
    }

    private static void ApplyManualPatches(Harmony harmony)
    {
        TryPatch(
            harmony,
            AccessTools.Method(typeof(Creature), nameof(Creature.SetCurrentHpInternal)),
            AccessTools.Method(typeof(Patches_Player), "AfterSetCurrentHp"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(AncientEventModel), "BeforeEventStarted"),
            AccessTools.Method(typeof(Patches_Player), "BeforeAncientEventStarted"),
            isPrefix: true);
    }

    private static void TryPatch(Harmony harmony, MethodInfo? original, MethodInfo? patchMethod, bool isPrefix = false)
    {
        if (original == null || patchMethod == null)
        {
            Log.Error($"[HeavenMode] Manual patch skipped. original={original != null}, patch={patchMethod != null}");
            return;
        }

        try
        {
            if (isPrefix)
            {
                harmony.Patch(original, prefix: new HarmonyMethod(patchMethod));
                Log.Info($"[HeavenMode] Manual prefix applied: {original.DeclaringType?.FullName}.{original.Name} -> {patchMethod.Name}");
            }
            else
            {
                harmony.Patch(original, postfix: new HarmonyMethod(patchMethod));
                Log.Info($"[HeavenMode] Manual postfix applied: {original.DeclaringType?.FullName}.{original.Name} -> {patchMethod.Name}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Manual patch failed for {original.DeclaringType?.FullName}.{original.Name}: {ex}");
        }
    }
}
