using System.Collections.Generic;

namespace HeavenMode;

/// <summary>
/// Shared runtime state for the Heaven selection layered on top of official ascension 0.
/// SelectedOption: 0 = Off, 1..10 = Heaven levels
/// </summary>
internal static class HeavenState
{
    public const int MaxLevel = 10;
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

    public static int GetAncientInitialOptionLimit(int currentActIndex) => currentActIndex <= 0 ? 2 : 1;

    public static string GetRunTitle(int level) =>
        level <= 0 ? string.Empty : Loc.Get($"HEAVEN_RUN_TITLE_{level}", $"Heaven {level}");

    public static string GetFeatureTitle(int level) => level switch
    {
        1 => Loc.Get("HEAVEN_TITLE_1", "Human World"),
        2 => Loc.Get("HEAVEN_TITLE_2", "Hell of Tongue Pulling"),
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
                "Includes official Ascension 10 effects. Ancients offer 2 options in Act 1 and 1 option in Acts 2 and 3.");
        }

        if (level == EventPenaltyLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_2",
                "Includes official Ascension 10 and Human World effects. Event and merchant card removal also make you lose 3 HP, clamped to at least 1.");
        }

        if (level == ActRecoveryLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_3",
                "Includes official Ascension 10 and all Heaven 1-2 effects. Start Act 1 with Blood Vial. Entering Acts 2 and 3 restores 60% of missing HP.");
        }

        if (level == BurnLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_4",
                "Includes official Ascension 10 and all Heaven 1-3 effects. Starting from round 3, add a Burn into your draw pile each round.");
        }

        if (level == CostIncreaseLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_5",
                "Includes official Ascension 10 and all Heaven 1-4 effects. On the first turn of each combat, increase the cost of a random card in your hand by 1.");
        }

        if (level == KillPunishLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_7",
                "Includes official Ascension 10 and all Heaven 1-6 effects. Whenever a monster dies, you take 2 damage.");
        }

        if (level == PotionLimitLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_8",
                "Includes official Ascension 10 and all Heaven 1-7 effects. Reduce your starting potion slots to 1 and start with a Potion-Shaped Rock.");
        }

        if (level == ShuffleTaxLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_9",
                "Includes official Ascension 10 and all Heaven 1-8 effects. Each time you shuffle your discard into your draw pile this turn, cards drawn afterward cost 1 more per shuffle. This extra cost is cleared at the start of your next turn.");
        }

        if (level == TripleBossLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_10",
                "Includes official Ascension 10 and all Heaven 1-9 effects. In Act 3 you must defeat a third boss. Starting from round 2 of that final boss fight, after the players act, it burns a random draw-pile card from ceil(n/2) players.");
        }

        if (level == NeowHpLevel)
        {
            return Loc.Get(
                "HEAVEN_DESC_6",
                "Includes official Ascension 10 and all Heaven 1-5 effects. When Neow starts, your current HP is set to 36.");
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
