using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.TestSupport;

namespace HeavenMode;

internal static class Patches_Heaven10
{
    private static readonly MethodInfo? ClearScreensMethod =
        AccessTools.Method(typeof(RunManager), "ClearScreens");

    private static readonly MethodInfo? FadeInMethod =
        AccessTools.Method(typeof(RunManager), "FadeIn");

    private static int _lastBurnRound = -1;

    internal static void AfterSetUpNewSinglePlayer(RunState state)
    {
        Heaven10Runtime.PrepareForRun(state);
    }

    internal static void AfterSetUpNewMultiPlayer(RunState state)
    {
        Heaven10Runtime.PrepareForRun(state);
    }

    internal static void BeforeSetUpSavedSinglePlayer()
    {
        Heaven10Runtime.Reset();
        _lastBurnRound = -1;
    }

    internal static void BeforeSetUpSavedMultiPlayer()
    {
        Heaven10Runtime.Reset();
        _lastBurnRound = -1;
    }

    internal static bool BeforeProceedFromTerminalRewardsScreen(RunManager __instance, ref Task __result)
    {
        RunState? state = __instance.DebugOnlyGetState();
        if (Heaven10Runtime.ShouldLaunchThirdBoss(state))
        {
            __result = ProceedToThirdBoss(__instance, state!);
            return false;
        }

        if (Heaven10Runtime.ShouldAllowFinalProceed(state))
        {
            Heaven10Runtime.MarkThirdBossCombatCompleted();
            Log.Info("[HeavenMode] Heaven 10 third boss cleared; resuming vanilla final-act proceed flow");
        }

        return true;
    }

    internal static Task AfterTurnStart(Task __result, Creature __instance, int roundNumber, CombatSide side)
    {
        if (HeavenState.SelectedOption < HeavenState.TripleBossLevel || side != CombatSide.Enemy)
            return __result;

        return ApplyThirdBossDrawPileBurn(__result, __instance, roundNumber);
    }

    internal static bool BeforeEnterNextAct(RunManager __instance, ref Task __result)
    {
        RunState? state = __instance.DebugOnlyGetState();
        if (Heaven10Runtime.ShouldLaunchThirdBoss(state))
        {
            Log.Info("[HeavenMode] Intercepted EnterNextAct for Heaven 10 second boss; launching third boss instead");
            __result = ProceedToThirdBoss(__instance, state!);
            return false;
        }

        if (Heaven10Runtime.ShouldAllowFinalProceed(state))
        {
            Heaven10Runtime.MarkThirdBossCombatCompleted();
            Log.Info("[HeavenMode] Heaven 10 third boss cleared; allowing EnterNextAct to continue to vanilla ending flow");
        }

        return true;
    }

    private static async Task ProceedToThirdBoss(RunManager runManager, RunState state)
    {
        EncounterModel? encounter = Heaven10Runtime.GetThirdBossEncounter(state);
        if (encounter == null)
        {
            Log.Warn("[HeavenMode] Heaven 10 third boss launch skipped because no remaining boss encounter was available");
            await runManager.ProceedFromTerminalRewardsScreen();
            return;
        }

        try
        {
            if (TestMode.IsOff)
            {
                NGame? game = NGame.Instance;
                if (game?.Transition != null)
                    await game.Transition.RoomFadeOut();
                else
                    Log.Warn("[HeavenMode] Heaven 10 third boss launch skipped room fade because NGame transition was unavailable");
            }

            if (runManager.CombatReplayWriter.IsEnabled)
                runManager.CombatReplayWriter.RecordInitialState(runManager.ToSave(null));

            ClearScreensMethod?.Invoke(runManager, Array.Empty<object>());
            state.CurrentMapPointHistoryEntry?.Rooms.Add(new MapPointRoomHistoryEntry
            {
                RoomType = RoomType.Boss,
                ModelId = encounter.Id,
            });

            Heaven10Runtime.MarkThirdBossCombatStarted(encounter);
            Log.Info($"[HeavenMode] Heaven 10 launching third boss combat: {encounter.Id.Entry}");

            await runManager.EnterRoom(new CombatRoom(encounter.ToMutable(), state));

            if (FadeInMethod?.Invoke(runManager, new object[] { true }) is Task fadeTask)
                await fadeTask;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ProceedToThirdBoss failed: {ex}");
        }
    }

    private static async Task ApplyThirdBossDrawPileBurn(Task originalTask, Creature boss, int roundNumber)
    {
        await originalTask;

        try
        {
            RunState? state = RunManager.Instance?.DebugOnlyGetState();
            CombatRoom? room = state?.CurrentRoom as CombatRoom;
            if (room == null || !Heaven10Runtime.IsThirdBossCombat(room) || roundNumber < 2)
                return;

            if (!boss.IsMonster || boss.IsDead || !IsPrimaryBossTurnOwner(boss, room) || _lastBurnRound == roundNumber)
                return;

            CombatState combatState = room.CombatState;
            List<Player> livingPlayers = combatState.Players.Where(p => p.Creature.IsAlive).ToList();
            if (livingPlayers.Count == 0)
                return;

            int targetCount = Math.Min((livingPlayers.Count + 1) / 2, livingPlayers.Count);
            if (targetCount <= 0)
                return;

            _lastBurnRound = roundNumber;

            List<Player> selectedPlayers = TakeRandomPlayers(livingPlayers, targetCount, combatState);
            Player contextOwner = combatState.Players.First();
            ulong localPlayerId = LocalContext.NetId ?? contextOwner.NetId;
            HookPlayerChoiceContext choiceContext = new(contextOwner, localPlayerId, GameActionType.CombatPlayPhaseOnly);

            foreach (Player player in selectedPlayers)
            {
                CardPile drawPile = PileType.Draw.GetPile(player);
                if (drawPile.Cards.Count == 0)
                {
                    Log.Info($"[HeavenMode] Heaven 10 third boss found no draw-pile card to burn for player {player.NetId}");
                    continue;
                }

                int index = combatState.RunState.Rng.CombatCardSelection.NextInt(drawPile.Cards.Count);
                CardModel card = drawPile.Cards[index];
                await CardCmd.Exhaust(choiceContext, card);
                Log.Info($"[HeavenMode] Heaven 10 third boss burned {card.Id.Entry} from player {player.NetId} draw pile");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyThirdBossDrawPileBurn failed: {ex}");
        }
    }

    private static bool IsPrimaryBossTurnOwner(Creature boss, CombatRoom room)
    {
        ModelId? primaryMonsterId = room.Encounter.MonstersWithSlots.FirstOrDefault().Item1?.Id;
        return primaryMonsterId != null && boss.Monster?.Id == primaryMonsterId;
    }

    private static List<Player> TakeRandomPlayers(List<Player> candidates, int count, CombatState combatState)
    {
        List<Player> pool = new(candidates);
        List<Player> selected = new(count);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = combatState.RunState.Rng.CombatTargets.NextInt(pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }
}
