using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Potions;
using Godot;

namespace HeavenMode;

internal static class Patches_PotionUi
{
    private static readonly AccessTools.FieldRef<NPotionContainer, List<NPotionHolder>> HoldersRef =
        AccessTools.FieldRefAccess<NPotionContainer, List<NPotionHolder>>("_holders");

    private static readonly AccessTools.FieldRef<NPotionContainer, Player?> PlayerRef =
        AccessTools.FieldRefAccess<NPotionContainer, Player?>("_player");

    private static readonly AccessTools.FieldRef<NPotionContainer, Control> PotionHoldersRef =
        AccessTools.FieldRefAccess<NPotionContainer, Control>("_potionHolders");

    internal static void AfterInitialize(NPotionContainer __instance)
    {
        RefreshVisiblePotionSlots(__instance);
    }

    internal static void AfterGrowPotionHolders(NPotionContainer __instance, int newMaxPotionSlots)
    {
        RefreshVisiblePotionSlots(__instance);
    }

    private static void RefreshVisiblePotionSlots(NPotionContainer container)
    {
        try
        {
            var holders = HoldersRef(container);
            int visibleCount = Math.Max(1, PlayerRef(container)?.MaxPotionCount ?? holders.Count);
            float visibleWidth = 0f;

            for (int i = 0; i < holders.Count; i++)
            {
                NPotionHolder holder = holders[i];
                bool shouldShow = i < visibleCount;
                holder.Visible = shouldShow;
                holder.FocusMode = shouldShow ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
                holder.MouseFilter = shouldShow ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
                if (shouldShow)
                {
                    holder.Scale = Vector2.One;
                    holder.CustomMinimumSize = Vector2.Zero;
                }
                else
                {
                    holder.Scale = Vector2.One;
                    holder.CustomMinimumSize = Vector2.Zero;
                }

                if (shouldShow)
                {
                    Vector2 minSize = holder.GetCombinedMinimumSize();
                    visibleWidth += minSize.X > 0f ? minSize.X : holder.Size.X;
                }
            }

            Control potionHolders = PotionHoldersRef(container);
            if (potionHolders != null)
            {
                if (potionHolders is BoxContainer boxContainer)
                {
                    boxContainer.Alignment = visibleCount == 1
                        ? BoxContainer.AlignmentMode.Center
                        : BoxContainer.AlignmentMode.Begin;
                }

                if (visibleWidth > 0f)
                    potionHolders.CustomMinimumSize = new Vector2(visibleWidth, potionHolders.CustomMinimumSize.Y);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] RefreshVisiblePotionSlots failed: {ex}");
        }
    }
}
