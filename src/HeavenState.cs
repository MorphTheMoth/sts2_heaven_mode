using System.Collections.Generic;

namespace HeavenMode;

/// <summary>
/// Shared runtime state for the Heaven selection layered on top of official ascension 0.
/// SelectedOption: 0 = Off, 1..11 = Heaven levels
/// </summary>
internal static class HeavenState
{
    public const int MaxLevel = 11;
    public const int EventPenaltyLevel = 2;
    public const int EventHpLoss = 3;
    public const int ActRecoveryLevel = 3;
    public const decimal ActRecoveryMissingPercent = 0.6M;
    public const int BurnLevel = 4;
    public const int CostIncreaseLevel = 5;
    public const int NeowHpLevel = 6;
    public const int KillPunishLevel = 7;
    public const int PotionLimitLevel = 8;
    public const int ShuffleTaxLevel = 9;
    public const int TripleBossLevel = 10;
    public const int SaveQuitDestroyLevel = 11;
    public const int NeowOpeningHp = 36;

    public static int SelectedOption { get; set; } = 0;

    public static int GetEffectiveAscension(int officialAscension)
    {
        if (officialAscension == 0 && SelectedOption >= 1)
            return 10;

        return officialAscension;
    }

    public static bool HasAncientChoiceRestriction => SelectedOption >= 1;

    public static bool HasEventPenalty => SelectedOption >= EventPenaltyLevel;

    public static bool ShouldOverrideNeowOpeningHp => SelectedOption >= NeowHpLevel;

    public static bool ShouldDestroySaveOnQuit => SelectedOption >= SaveQuitDestroyLevel;

    public static int GetAncientInitialOptionLimit(int currentActIndex) => currentActIndex <= 0 ? 2 : 1;

    public static string GetRunTitle(int level) =>
        level <= 0 ? string.Empty : Loc.Get($"HEAVEN_RUN_TITLE_{level}", $"Heaven {level}");

    public static string GetFeatureTitle(int level) => level switch
    {
        1 => Loc.Get("HEAVEN_TITLE_1", "Fading Choices"),
        2 => Loc.Get("HEAVEN_TITLE_2", "Cruel Bargains"),
        > 0 => Loc.Get($"HEAVEN_TITLE_{level}", GetRunTitle(level)),
        _ => string.Empty,
    };

    public static string GetDescription(int level)
    {
        if (level <= 0)
            return string.Empty;

        if (level == 1)
        {
            return Loc.Get(
                "HEAVEN_DESC_1",
                "The tower gradually strips away your power to choose. Fate begins to narrow.");
        }

        if (level == EventPenaltyLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_2",
                "Every opportunity comes with a price.");
        }

        if (level == ActRecoveryLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_3",
                "Wounds cannot fully heal. Rely on limited supplies to move forward.");
        }

        if (level == BurnLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_4",
                "The tower's flames do not consume you at once. They scorch you slowly.");
        }

        if (level == CostIncreaseLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_5",
                "You lose your rhythm at the start of battle. Strategy is disturbed immediately.");
        }

        if (level == KillPunishLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_7",
                "Every kill demands your own blood as tribute to the tower.");
        }

        if (level == PotionLimitLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_8",
                "Healing and supplies are exceedingly scarce. You must rely on limited resources.");
        }

        if (level == ShuffleTaxLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_9",
                "Repeatedly cycling the deck dulls the mind and slows action.");
        }

        if (level == TripleBossLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_10",
                "The tower tests you tirelessly. In the final battle, the tower devours your cards.");
        }

        if (level == SaveQuitDestroyLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_11",
                "Once you step into the Spire, there is no turning back.");
        }

        if (level == NeowHpLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_6",
                "The adventure begins from a more dangerous state.");
        }

        return Loc.Get(
            $"HEAVEN_DESC_{level}",
            $"Includes official Ascension 10 and all Heaven 1-{level - 1} effects.");
    }

    // Per-player flag: set just before the Neow heal, consumed when HP would exceed 10.
    // This lets us clamp the Neow heal to 10 without affecting subsequent in-run heals.
    private static readonly HashSet<ulong> _pendingHpPlayers = new();

    public static void MarkPendingHp(ulong playerId) => _pendingHpPlayers.Add(playerId);
    public static bool HasPendingHp(ulong playerId) => _pendingHpPlayers.Contains(playerId);
    public static void ClearPendingHp(ulong playerId) => _pendingHpPlayers.Remove(playerId);
}
