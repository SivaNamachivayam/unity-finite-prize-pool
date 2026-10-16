using System;
using UnityEngine;

namespace FinitePrizePool.DealEngine
{
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "Finite Prize Pool/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Configuration")] [SerializeField]
        private string configVersion = "1.0.0";

        [Header("Game Rules")] [SerializeField, Range(1, 100)]
        private int targetRtpPercent = 90;

        [SerializeField, Min(1)] private int bonusUnitValueDollars = 1;

        [Header("Bet Levels (Cents)")] [SerializeField]
        private long[] betLevelsCents =
        {
            25, 50, 75, 100, 125,
            150, 175, 200, 225, 250,
            275, 300, 325, 350, 375,
            400, 425, 450, 475, 500
        };

        [Header("Prize Bags")] [Tooltip("Enter confirmed Regular prize payout values in cents.")] [SerializeField]
        private long[] regularBagPayoutsCents =
            Array.Empty<long>();

        [Tooltip("Enter confirmed Big prize payout values in cents.")] [SerializeField]
        private long[] bigBagPayoutsCents =
            Array.Empty<long>();

        [Header("Placement Limits")] [SerializeField, Min(1)]
        private int maxGenerationAttempts = 1000;

        [SerializeField, Min(1)] private int maxConsecutiveLossesMultiplier = 2;

        [SerializeField, Min(1)] private int maxPrizeGapMultiplier = 2;

        public string ConfigVersion => configVersion;
        public int TargetRtpPercent => targetRtpPercent;
        public int BonusUnitValueDollars => bonusUnitValueDollars;

        public long[] BetLevelsCents =>
            (long[])betLevelsCents.Clone();

        public long[] RegularBagPayoutsCents =>
            regularBagPayoutsCents == null
                ? Array.Empty<long>()
                : (long[])regularBagPayoutsCents.Clone();

        public long[] BigBagPayoutsCents =>
            bigBagPayoutsCents == null
                ? Array.Empty<long>()
                : (long[])bigBagPayoutsCents.Clone();

        public int MaxGenerationAttempts => maxGenerationAttempts;

        public int MaxConsecutiveLossesMultiplier =>
            maxConsecutiveLossesMultiplier;

        public int MaxPrizeGapMultiplier =>
            maxPrizeGapMultiplier;

        public bool HasConfiguredPrizeBags()
        {
            return regularBagPayoutsCents != null &&
                   regularBagPayoutsCents.Length > 0 &&
                   bigBagPayoutsCents != null &&
                   bigBagPayoutsCents.Length > 0;
        }

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(configVersion))
            {
                error = "Configuration version is required.";
                return false;
            }

            if (targetRtpPercent != 90)
            {
                error = "Target RTP must be exactly 90%.";
                return false;
            }

            if (bonusUnitValueDollars != 1)
            {
                error = "Bonus unit value must be $1.";
                return false;
            }

            if (betLevelsCents == null ||
                betLevelsCents.Length != 20)
            {
                error = "Exactly 20 bet levels are required.";
                return false;
            }

            for (int i = 0; i < betLevelsCents.Length; i++)
            {
                long expected = 25L * (i + 1);

                if (betLevelsCents[i] != expected)
                {
                    error =
                        "Bet levels must run from $0.25 to $5.00 in $0.25 increments.";
                    return false;
                }
            }

            if (maxGenerationAttempts != 1000)
            {
                error =
                    "Generation attempts must be limited to 1000.";
                return false;
            }

            if (maxConsecutiveLossesMultiplier <= 0 ||
                maxPrizeGapMultiplier <= 0)
            {
                error =
                    "Placement limit multipliers must be positive.";
                return false;
            }

            if (!ValidateBag(regularBagPayoutsCents, out error))
                return false;

            if (!ValidateBag(bigBagPayoutsCents, out error))
                return false;

            error = string.Empty;
            return true;
        }

        private static bool ValidateBag(
            long[] bag,
            out string error)
        {
            if (bag == null)
            {
                error = "Prize bag cannot be null.";
                return false;
            }

            foreach (long payout in bag)
            {
                if (payout < 0)
                {
                    error =
                        "Prize payouts cannot be negative.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}