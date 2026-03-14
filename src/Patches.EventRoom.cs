using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Patches_EventRoom
{
    internal static Task<IEnumerable<CardModel>> AfterFromDeckForRemoval(
        Task<IEnumerable<CardModel>> __result,
        Player player)
    {
        if (!HeavenState.HasEventPenalty)
            return __result;

        return ApplyHeavenEventRemovalPenalty(__result, player);
    }

    private static async Task<IEnumerable<CardModel>> ApplyHeavenEventRemovalPenalty(
        Task<IEnumerable<CardModel>> selectionTask,
        Player player)
    {
        IEnumerable<CardModel> selectedCards = await selectionTask;

        try
        {
            if (!LocalContext.IsMe(player))
                return selectedCards;

            if (!selectedCards.Any())
                return selectedCards;

            RunManager? runManager = RunManager.Instance;
            IRunState? runState = runManager?.DebugOnlyGetState();
            if (runState?.CurrentRoom is not EventRoom && runState?.CurrentRoom is not MerchantRoom)
                return selectedCards;

            Creature? creature = player.Creature;
            if (creature == null)
                return selectedCards;

            int oldHp = creature.CurrentHp;
            int newHp = Math.Max(1, oldHp - HeavenState.EventHpLoss);
            if (newHp != oldHp)
            {
                creature.SetCurrentHpInternal((decimal)newHp);
                PlayHpPenaltyFeedback(runState, oldHp - newHp);
            }

            Log.Info($"[HeavenMode] Applied Heaven {HeavenState.SelectedOption} card-removal penalty in {runState.CurrentRoom.RoomType}. hp {oldHp}->{newHp}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyHeavenEventRemovalPenalty failed: {ex}");
        }

        return selectedCards;
    }

    private static void PlayHpPenaltyFeedback(IRunState runState, int hpLost)
    {
        if (hpLost <= 0)
            return;

        try
        {
            Control? parent = GetFeedbackParent(runState);
            Vector2? position = GetFeedbackPosition(parent);
            PlayerHurtVignetteHelper.Play();
            if (parent != null && position.HasValue)
            {
                NDamageNumVfx? damageNum = NDamageNumVfx.Create(position.Value, hpLost);
                if (damageNum != null)
                    parent.AddChild(damageNum);

                VfxCmd.PlayNonCombatVfx(parent, position.Value, VfxCmd.bloodyImpactPath);
            }

            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PlayHpPenaltyFeedback failed: {ex}");
        }
    }

    private static Control? GetFeedbackParent(IRunState runState)
    {
        if (runState.CurrentRoom is EventRoom)
            return NEventRoom.Instance?.VfxContainer ?? NEventRoom.Instance;

        if (runState.CurrentRoom is MerchantRoom)
            return NMerchantRoom.Instance;

        return null;
    }

    private static Vector2? GetFeedbackPosition(Control? parent)
    {
        if (parent == null)
            return null;

        Vector2 size = parent.Size;
        if (size == Vector2.Zero)
            size = parent.GetViewportRect().Size;

        Vector2 anchor = parent.GlobalPosition + new Vector2(size.X * 0.35f, size.Y * 0.5f);
        return anchor;
    }
}
