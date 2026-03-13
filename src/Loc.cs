using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;

namespace HeavenMode;

internal static class Loc
{
    private const string ModFolder = "HeavenMode";

    private static readonly Dictionary<string, Dictionary<string, string>> Cache = new();

    public static string Get(string key, string fallback = "")
    {
        string lang = GetLanguageCode();

        if (TryGetValue(lang, key, out string value))
            return value;

        if (lang != "en_us" && TryGetValue("en_us", key, out value))
            return value;

        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    private static bool TryGetValue(string lang, string key, out string value)
    {
        Dictionary<string, string> table = LoadTable(lang);
        if (table.TryGetValue(key, out string? result) && result != null)
        {
            value = result;
            return true;
        }
        value = string.Empty;
        return false;
    }

    private static Dictionary<string, string> LoadTable(string lang)
    {
        if (Cache.TryGetValue(lang, out Dictionary<string, string>? cached))
            return cached;

        string path = $"res://{ModFolder}/localization/{lang}.json";
        Dictionary<string, string> table = new();
        try
        {
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file != null)
            {
                Dictionary<string, string>? parsed =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(file.GetAsText());
                if (parsed != null)
                    table = parsed;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[HeavenMode] Failed to load localization {path}: {ex.Message}");
        }

        Cache[lang] = table;
        return table;
    }

    private static string GetLanguageCode()
    {
        string language = LocManager.Instance?.Language ?? "eng";
        return language.ToLowerInvariant() switch
        {
            "zhs" or "zh_cn" => "zh_cn",
            _                => "en_us",
        };
    }
}
