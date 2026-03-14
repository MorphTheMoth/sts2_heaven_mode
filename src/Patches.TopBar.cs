using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace HeavenMode;

[HarmonyPatch(typeof(NTopBarPortraitTip))]
internal static class Patches_TopBar
{
    private const string HeavenFireTimerName = "__HeavenTopBarFireTimer";
    private const string HeavenEmberTimerName = "__HeavenTopBarEmberTimer";
    private const string HeavenEmberContainerName = "__HeavenTopBarEmberContainer";
    private static readonly StringName HParam = new("h");
    private static readonly StringName VParam = new("v");
    private static readonly StringName FontOutlineTheme = "font_outline_color";
    private static readonly Color HeavenOutlineColor = new("160818");
    private static readonly Color HeavenIconTint = new(0.88f, 0.84f, 0.98f, 1f);
    private static readonly Color HeavenEmberBright = new("960aef");
    private static readonly Color HeavenEmberDark = new("8f25d3");

    private static readonly AccessTools.FieldRef<NTopBarPortraitTip, IHoverTip> HoverTipRef =
        AccessTools.FieldRefAccess<NTopBarPortraitTip, IHoverTip>("_hoverTip");

    private static readonly FieldInfo? HoverTipTitleField =
        typeof(HoverTip).GetField("<Title>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? HoverTipDescriptionField =
        typeof(HoverTip).GetField("<Description>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly AccessTools.FieldRef<NTopBar, Control> AscensionIconRef =
        AccessTools.FieldRefAccess<NTopBar, Control>("_ascensionIcon");

    private static readonly AccessTools.FieldRef<NTopBar, MegaCrit.Sts2.addons.mega_text.MegaLabel> AscensionLabelRef =
        AccessTools.FieldRefAccess<NTopBar, MegaCrit.Sts2.addons.mega_text.MegaLabel>("_ascensionLabel");

    private static readonly AccessTools.FieldRef<NTopBar, ShaderMaterial> AscensionHsvRef =
        AccessTools.FieldRefAccess<NTopBar, ShaderMaterial>("_ascensionHsv");

    private static ImageTexture? _emberTexture;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NTopBarPortraitTip.Initialize))]
    private static void AfterInitialize(NTopBarPortraitTip __instance, IRunState runState)
    {
        try
        {
            if (HeavenState.SelectedOption < 1)
                return;

            if (HoverTipRef(__instance) is not HoverTip hoverTip)
                return;

            string portraitSuffix = GetPortraitSuffix(HeavenState.SelectedOption);
            if (string.IsNullOrWhiteSpace(portraitSuffix))
                return;

            var localPlayer = LocalContext.GetMe(runState);
            if (localPlayer?.Character == null)
                return;

            string characterTitle = localPlayer.Character.Title.GetFormattedText();
            string heavenDescription = AppendHeavenEntries(hoverTip.Description, GetActiveHeavenTitles());

            object boxed = hoverTip;
            HoverTipTitleField?.SetValue(boxed, $"{characterTitle} - {portraitSuffix}");
            HoverTipDescriptionField?.SetValue(boxed, heavenDescription);
            HoverTipRef(__instance) = (IHoverTip)(HoverTip)boxed;
            Log.Info($"[HeavenMode] Updated portrait hover tip for Heaven={HeavenState.SelectedOption}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterInitialize portrait tip failed: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
    private static void AfterTopBarInitialize(NTopBar __instance, IRunState runState)
    {
        try
        {
            ApplyHeavenTopBarFireVisuals(__instance, runState);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterTopBarInitialize failed: {ex}");
        }
    }

    private static string GetPortraitSuffix(int level) => HeavenState.GetRunTitle(level);

    private static IReadOnlyList<string> GetActiveHeavenTitles()
    {
        List<string> titles = new();
        for (int level = 1; level <= HeavenState.SelectedOption; level++)
            titles.Add(HeavenState.GetFeatureTitle(level));
        return titles;
    }

    private static string AppendHeavenEntries(string baseDescription, IReadOnlyList<string> heavenTitles)
    {
        string result = baseDescription ?? string.Empty;
        foreach (string heavenTitle in heavenTitles.Where(title => !string.IsNullOrWhiteSpace(title)))
        {
            string line = $" +{heavenTitle}";
            if (!result.Contains(line, StringComparison.Ordinal))
                result = string.IsNullOrEmpty(result) ? line : $"{result}\n{line}";
        }

        return result;
    }

    private static void ApplyHeavenTopBarFireVisuals(NTopBar topBar, IRunState runState)
    {
        Control icon = AscensionIconRef(topBar);
        var label = AscensionLabelRef(topBar);
        ShaderMaterial shader = AscensionHsvRef(topBar);

        if (HeavenState.SelectedOption <= 0 || runState.AscensionLevel <= 0)
        {
            RestoreOfficialTopBarFire(icon, label, shader, runState);
            return;
        }

        EnsureTopBarFireTimer(topBar, icon, label, shader);
        EnsureTopBarEmberTimer(topBar, icon);
        AnimateTopBarHeavenFire(icon, label, shader);
    }

    private static void EnsureTopBarFireTimer(
        NTopBar topBar,
        Control icon,
        MegaCrit.Sts2.addons.mega_text.MegaLabel label,
        ShaderMaterial shader)
    {
        Timer? timer = topBar.GetNodeOrNull<Timer>(HeavenFireTimerName);
        if (timer == null)
        {
            timer = new Timer
            {
                Name = HeavenFireTimerName,
                WaitTime = 0.16,
                OneShot = false,
                Autostart = false,
            };
            timer.Timeout += () => AnimateTopBarHeavenFire(icon, label, shader);
            topBar.AddChild(timer);
        }

        if (timer.IsStopped())
            timer.Start();
    }

    private static void EnsureTopBarEmberTimer(NTopBar topBar, Control icon)
    {
        Timer? timer = topBar.GetNodeOrNull<Timer>(HeavenEmberTimerName);
        if (timer == null)
        {
            timer = new Timer
            {
                Name = HeavenEmberTimerName,
                WaitTime = 0.2,
                OneShot = false,
                Autostart = false,
            };
            timer.Timeout += () => SpawnTopBarHeavenEmber(icon);
            topBar.AddChild(timer);
        }

        if (timer.IsStopped())
            timer.Start();
    }

    private static void AnimateTopBarHeavenFire(
        Control icon,
        MegaCrit.Sts2.addons.mega_text.MegaLabel label,
        ShaderMaterial shader)
    {
        if (!icon.IsInsideTree() || HeavenState.SelectedOption <= 0)
            return;

        double t = Time.GetTicksMsec() / 1000.0;
        float pulse = 0.5f + 0.5f * Mathf.Sin((float)(t * 1.35));
        float flicker = 0.5f + 0.5f * Mathf.Sin((float)(t * 2.4 + HeavenState.SelectedOption * 0.6f));

        float hue = Mathf.Lerp(0.80f, 0.84f, pulse);
        float value = Mathf.Lerp(0.19f, 0.3f, flicker);
        float scale = Mathf.Lerp(0.99f, 1.03f, pulse);

        shader.SetShaderParameter(HParam, hue);
        shader.SetShaderParameter(VParam, value);
        ((Control)label).AddThemeColorOverride(FontOutlineTheme, HeavenOutlineColor);
        icon.Scale = new Vector2(scale, scale);
        icon.Modulate = HeavenIconTint;
    }

    private static void RestoreOfficialTopBarFire(
        Control icon,
        MegaCrit.Sts2.addons.mega_text.MegaLabel label,
        ShaderMaterial shader,
        IRunState runState)
    {
        if (runState.Players.Count > 1)
        {
            shader.SetShaderParameter(HParam, 0.52f);
            shader.SetShaderParameter(VParam, 1.2f);
        }
        else
        {
            shader.SetShaderParameter(HParam, 1f);
            shader.SetShaderParameter(VParam, 1f);
        }

        icon.Scale = Vector2.One;
        icon.Modulate = Colors.White;
        Node? emberContainer = icon.GetNodeOrNull(HeavenEmberContainerName);
        emberContainer?.QueueFree();
    }

    private static void SpawnTopBarHeavenEmber(Control icon)
    {
        if (!icon.IsInsideTree() || HeavenState.SelectedOption <= 0)
            return;

        Node2D container = EnsureTopBarEmberContainer(icon);
        Sprite2D ember = new()
        {
            Texture = GetOrCreateEmberTexture(),
            Centered = true,
            Position = new Vector2(
                (float)GD.RandRange(-10.0, 10.0),
                (float)GD.RandRange(8.0, 16.0)),
            Scale = Vector2.One * (float)GD.RandRange(0.18, 0.42),
            Modulate = HeavenEmberBright.Lerp(HeavenEmberDark, (float)GD.RandRange(0.0, 0.45)),
        };
        ember.Modulate = ember.Modulate with { A = (float)GD.RandRange(0.3, 0.65) };
        container.AddChild(ember);

        Vector2 targetPosition = ember.Position + new Vector2(
            (float)GD.RandRange(-4.0, 4.0),
            (float)GD.RandRange(-14.0, -22.0));
        Vector2 targetScale = Vector2.One * (float)GD.RandRange(0.06, 0.14);
        double duration = GD.RandRange(0.5, 0.85);

        Tween tween = ember.CreateTween().SetParallel(true);
        tween.TweenProperty(ember, "position", targetPosition, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ember, "scale", targetScale, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ember, "modulate:a", 0.0f, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Finished += ember.QueueFree;
    }

    private static Node2D EnsureTopBarEmberContainer(Control icon)
    {
        Node2D? container = icon.GetNodeOrNull<Node2D>(HeavenEmberContainerName);
        if (container != null)
            return container;

        container = new Node2D
        {
            Name = HeavenEmberContainerName,
            Position = icon.Size * 0.5f,
            ZIndex = 2,
        };
        icon.AddChild(container);
        return container;
    }

    private static Texture2D GetOrCreateEmberTexture()
    {
        if (_emberTexture != null)
            return _emberTexture;

        Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        Vector2 center = new(7.5f, 7.5f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = center.DistanceTo(new Vector2(x, y));
                float alpha = Mathf.Clamp(1f - distance / 7.5f, 0f, 1f);
                alpha *= alpha;
                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        _emberTexture = ImageTexture.CreateFromImage(image);
        return _emberTexture;
    }
}
