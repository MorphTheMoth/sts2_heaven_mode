using System;
using System.Collections;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace HeavenMode;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
internal static class Patches_CharacterSelect
{
    private const string HeavenDescriptionPanelName = "HeavenDescriptionPanel";
    private const string HeavenDescriptionLabelName = "HeavenDescriptionLabel";
    private const float HeavenDescriptionGap = 60f;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    private static void AfterReady(NCharacterSelectScreen __instance)
    {
        try
        {
            InjectHeavenDropdown(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] CharacterSelect patch failed: {ex}");
        }
    }

    private static void InjectHeavenDropdown(NCharacterSelectScreen screen)
    {
        var template = ((Node)screen).GetNode<NActDropdown>("%ActDropdown");
        if (template == null)
        {
            Log.Warn("[HeavenMode] ActDropdown template not found");
            return;
        }

        // Duplicate the existing ActDropdown to reuse its full scene structure
        if (template.Duplicate() is not Control heavenDropdown)
        {
            Log.Warn("[HeavenMode] Failed to duplicate ActDropdown");
            return;
        }
        heavenDropdown.Name = "HeavenDropdown";
        // Reset any position baked into the duplicate from the original scene node
        // 34 = enlarged title font height (~22px) + 12px gap
        heavenDropdown.Position = new Vector2(0, 34);

        // Title label above the dropdown
        var titleLabel = new Label();
        titleLabel.Name = "HeavenTitle";
        titleLabel.Text = Loc.Get("HEAVEN_TITLE", "Heaven");
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.Position = Vector2.Zero;

        // Plain Control wrapper: children are positioned explicitly so layout never overlaps
        var wrapper = new Control();
        wrapper.Name = "HeavenDropdownWrapper";
        wrapper.Position = new Vector2(200, 120);
        wrapper.AddChild(titleLabel);
        wrapper.AddChild(heavenDropdown);

        ((Node)screen).AddChild(wrapper);

        var descriptionPanel = CreateHeavenDescriptionPanel(screen);
        if (descriptionPanel != null)
            ((Node)screen).AddChild(descriptionPanel);

        // Defer text fixup until after all child _Ready() calls have fired
        Callable.From(() => FixupHeavenItems(screen, heavenDropdown)).CallDeferred();
    }

    private static Control? CreateHeavenDescriptionPanel(NCharacterSelectScreen screen)
    {
        var ascensionPanel = ((Node)screen).GetNodeOrNull<Control>("%AscensionPanel");
        var template = ascensionPanel != null
            ? ((Node)ascensionPanel).GetNodeOrNull<Control>("HBoxContainer/AscensionDescription")
            : null;
        if (template == null)
        {
            Log.Warn("[HeavenMode] AscensionDescription template not found");
            return null;
        }

        if (template.Duplicate() is not Control panel)
        {
            Log.Warn("[HeavenMode] Failed to duplicate AscensionDescription template");
            return null;
        }

        panel.Name = HeavenDescriptionPanelName;
        panel.Position = GetHeavenDescriptionPosition(screen);
        panel.Size = template.Size;
        panel.Visible = false;
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);

        var description = ((Node)panel).FindChild("Description", true, false) as MegaRichTextLabel;
        if (description == null)
        {
            Log.Warn("[HeavenMode] Duplicated Heaven description text node not found");
            return null;
        }

        description.Name = HeavenDescriptionLabelName;
        return panel;
    }

    private static Vector2 GetHeavenDescriptionPosition(NCharacterSelectScreen screen)
    {
        var ascensionPanel = ((Node)screen).GetNodeOrNull<Control>("%AscensionPanel");
        if (ascensionPanel == null)
            return new Vector2(1080, 720);

        return ascensionPanel.Position + new Vector2(ascensionPanel.Size.X + HeavenDescriptionGap, 8f);
    }

    private static void FixupHeavenItems(NCharacterSelectScreen screen, Control dropdown)
    {
        try
        {
            var vbox = ((Node)dropdown).GetNodeOrNull<Control>("DropdownContainer/VBoxContainer");
            if (vbox == null)
            {
                Log.Warn("[HeavenMode] DropdownContainer/VBoxContainer not found in heaven dropdown");
                return;
            }

            var items = ((IEnumerable)vbox.GetChildren(false))
                .OfType<NDropdownItem>()
                .ToList();

            if (items.Count >= 1) items[0].Text = Loc.Get("HEAVEN_OFF", "Off");
            if (items.Count >= 2) items[1].Text = Loc.Get("HEAVEN_1", "1");
            if (items.Count >= 3) items[2].Text = Loc.Get("HEAVEN_2", "2");

            // Connect each item's Selected signal to record the chosen option in HeavenState
            for (int i = 0; i < items.Count; i++)
            {
                int captured = i;
                ((GodotObject)items[i]).Connect(
                    NDropdownItem.SignalName.Selected,
                    Callable.From<NDropdownItem>(_ => {
                        HeavenState.SelectedOption = captured;
                        UpdateHeavenDescription(screen, captured);
                        Log.Info($"[HeavenMode] Heaven option {captured} selected");
                    })
                );
            }

            // Reset the current-selection label on the button face to show "Off"
            var labelNode = ((Node)dropdown).GetNodeOrNull("%Label");
            labelNode?.Set("text", Loc.Get("HEAVEN_OFF", "Off"));
            UpdateHeavenDescription(screen, 0);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] FixupHeavenItems failed: {ex}");
        }
    }

    private static void UpdateHeavenDescription(NCharacterSelectScreen screen, int option)
    {
        var panel = ((Node)screen).GetNodeOrNull<Control>(HeavenDescriptionPanelName);
        if (panel == null)
        {
            Log.Warn("[HeavenMode] Heaven description panel not found");
            return;
        }
        var label = ((Node)panel).FindChild(HeavenDescriptionLabelName, true, false) as MegaRichTextLabel;
        if (label == null)
        {
            Log.Warn("[HeavenMode] Heaven description label not found");
            return;
        }

        panel.Position = GetHeavenDescriptionPosition(screen);
        string description = option switch
        {
            1 => $"[b][gold]{Loc.Get("HEAVEN_TITLE_1", "Human World") }[/gold][/b]\n{Loc.Get("HEAVEN_DESC_1", "Neow start: current HP becomes 10.")}",
            2 => $"[b][gold]{Loc.Get("HEAVEN_TITLE_2", "Hell of Tongue Pulling") }[/gold][/b]\n{Loc.Get("HEAVEN_DESC_2", "Includes Heaven 1 effects.")}",
            _ => string.Empty,
        };

        label.Text = description;
        panel.Visible = !string.IsNullOrWhiteSpace(description);
    }
}
