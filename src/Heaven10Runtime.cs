using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Heaven10Runtime
{
    private static ModelId? _thirdBossEncounterId;
    private static bool _thirdBossCombatActive;
    private static bool _thirdBossCombatCompleted;

    internal static void Reset()
    {
        _thirdBossEncounterId = null;
        _thirdBossCombatActive = false;
        _thirdBossCombatCompleted = false;
    }

    internal static void PrepareForRun(RunState? state)
    {
        Reset();

        try
        {
            if (state == null || HeavenState.SelectedOption < HeavenState.TripleBossLevel)
                return;

            ActModel finalAct = state.Acts.Last();
            List<EncounterModel> candidates = finalAct.AllBossEncounters
                .Where(e => e.Id != finalAct.BossEncounter.Id && e.Id != finalAct.SecondBossEncounter?.Id)
                .OrderBy(e => e.Id.Entry)
                .ToList();

            _thirdBossEncounterId = candidates.FirstOrDefault()?.Id;
            if (_thirdBossEncounterId != null)
                Log.Info($"[HeavenMode] Prepared Heaven 10 third boss: {_thirdBossEncounterId.Entry}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PrepareForRun failed: {ex}");
        }
    }

    internal static EncounterModel? GetThirdBossEncounter(RunState state)
    {
        try
        {
            if (HeavenState.SelectedOption < HeavenState.TripleBossLevel)
                return null;

            ActModel finalAct = state.Acts.Last();
            EncounterModel? encounter = finalAct.AllBossEncounters
                .FirstOrDefault(e => e.Id == _thirdBossEncounterId);

            if (encounter != null)
                return encounter;

            encounter = finalAct.AllBossEncounters
                .Where(e => e.Id != finalAct.BossEncounter.Id && e.Id != finalAct.SecondBossEncounter?.Id)
                .OrderBy(e => e.Id.Entry)
                .FirstOrDefault();

            _thirdBossEncounterId = encounter?.Id;
            return encounter;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] GetThirdBossEncounter failed: {ex}");
            return null;
        }
    }

    internal static bool ShouldLaunchThirdBoss(RunState? state)
    {
        if (state == null ||
            HeavenState.SelectedOption < HeavenState.TripleBossLevel ||
            _thirdBossCombatActive ||
            _thirdBossCombatCompleted ||
            state.CurrentRoom is not CombatRoom currentRoom ||
            currentRoom.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss ||
            state.CurrentActIndex != state.Acts.Count - 1)
        {
            return false;
        }

        return state.Act.SecondBossEncounter != null
            && currentRoom.ModelId == state.Act.SecondBossEncounter.Id
            && GetThirdBossEncounter(state) != null;
    }

    internal static bool ShouldAllowFinalProceed(RunState? state)
    {
        if (state == null ||
            HeavenState.SelectedOption < HeavenState.TripleBossLevel ||
            !_thirdBossCombatActive ||
            state.CurrentRoom is not CombatRoom currentRoom ||
            currentRoom.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss ||
            state.CurrentActIndex != state.Acts.Count - 1)
        {
            return false;
        }

        return _thirdBossEncounterId != null && currentRoom.ModelId == _thirdBossEncounterId;
    }

    internal static void MarkThirdBossCombatStarted(EncounterModel encounter)
    {
        _thirdBossEncounterId = encounter.Id;
        _thirdBossCombatActive = true;
        _thirdBossCombatCompleted = false;
    }

    internal static void MarkThirdBossCombatCompleted()
    {
        _thirdBossCombatActive = false;
        _thirdBossCombatCompleted = true;
    }

    internal static bool IsThirdBossCombat(CombatRoom? room)
    {
        return room != null
            && _thirdBossCombatActive
            && _thirdBossEncounterId != null
            && room.ModelId == _thirdBossEncounterId;
    }
}
