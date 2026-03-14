using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HeavenMode;

internal static class HeavenUnlockProgress
{
    private const string ProgressFileName = "heaven_mode_unlock_progress.json";
    private const int RequiredOfficialAscension = 10;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    private sealed class ProgressFileData
    {
        public Dictionary<string, CharacterProgress> Characters { get; set; } = new();
    }

    private sealed class CharacterProgress
    {
        public int MaxClearedHeaven { get; set; }
    }

    public static int GetMaxSelectableHeaven(ModelId characterId, int officialMaxAscension)
    {
        if (HeavenConfig.UnlockAll)
            return HeavenState.MaxLevel;

        if (officialMaxAscension < RequiredOfficialAscension)
            return 0;

        int maxClearedHeaven = GetMaxClearedHeaven(characterId);
        return Math.Clamp(maxClearedHeaven + 1, 1, HeavenState.MaxLevel);
    }

    public static void HandleMetricsUpload(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        try
        {
            if (!isVictory)
                return;

            int heavenLevel = HeavenPersistence.LoadSelection(run.StartTime);
            if (heavenLevel < 1)
                return;

            SerializablePlayer? player = run.Players.FirstOrDefault(p => p.NetId == localPlayerId);
            if (player?.CharacterId == null || player.CharacterId == ModelId.none)
                return;

            RecordVictory(player.CharacterId, heavenLevel);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] HandleMetricsUpload failed: {ex}");
        }
    }

    private static void RecordVictory(ModelId characterId, int heavenLevel)
    {
        try
        {
            ProgressFileData data = LoadOrCreate();
            string key = characterId.ToString();
            if (!data.Characters.TryGetValue(key, out CharacterProgress? progress))
            {
                progress = new CharacterProgress();
                data.Characters[key] = progress;
            }

            int clampedLevel = Math.Clamp(heavenLevel, 1, HeavenState.MaxLevel);
            if (progress.MaxClearedHeaven >= clampedLevel)
                return;

            progress.MaxClearedHeaven = clampedLevel;
            Save(data);
            Log.Info($"[HeavenMode] Recorded Heaven clear for {characterId}: level={clampedLevel}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] RecordVictory failed: {ex}");
        }
    }

    private static int GetMaxClearedHeaven(ModelId characterId)
    {
        try
        {
            ProgressFileData data = LoadOrCreate();
            return data.Characters.TryGetValue(characterId.ToString(), out CharacterProgress? progress)
                ? Math.Clamp(progress.MaxClearedHeaven, 0, HeavenState.MaxLevel)
                : 0;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] GetMaxClearedHeaven failed: {ex}");
            return 0;
        }
    }

    private static ProgressFileData LoadOrCreate()
    {
        string path = GetProgressPath();
        if (!File.Exists(path))
        {
            ProgressFileData defaults = new();
            Save(defaults);
            return defaults;
        }

        try
        {
            ProgressFileData? data = JsonSerializer.Deserialize<ProgressFileData>(File.ReadAllText(path));
            return data ?? new ProgressFileData();
        }
        catch (Exception ex)
        {
            Log.Warn($"[HeavenMode] Failed to parse unlock progress at {path}: {ex.Message}");
            BackupCorruptedProgress(path);
            ProgressFileData defaults = new();
            Save(defaults);
            return defaults;
        }
    }

    private static void Save(ProgressFileData data)
    {
        string path = GetProgressPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(data, WriteOptions));
    }

    private static string GetProgressPath()
    {
        string userPath = UserDataPathProvider.GetProfileScopedPath(
            SaveManager.Instance.CurrentProfileId,
            Path.Combine(UserDataPathProvider.SavesDir, ProgressFileName));
        return ProjectSettings.GlobalizePath(userPath);
    }

    private static void BackupCorruptedProgress(string path)
    {
        if (!File.Exists(path))
            return;

        string backupPath = $"{path}.bak";
        if (File.Exists(backupPath))
            backupPath = $"{path}.{DateTime.Now:yyyyMMddHHmmss}.bak";

        File.Move(path, backupPath);
    }
}
