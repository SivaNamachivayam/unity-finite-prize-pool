using NUnit.Framework;
using UnityEngine;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Tests
{
    public class GameConfigTests
    {
        [Test]
        public void DefaultConfig_HasExpectedBetLevels()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                Assert.IsTrue(config.Validate(out string error), error);
                Assert.AreEqual(90, config.TargetRtpPercent);
                Assert.AreEqual(1, config.BonusUnitValueDollars);

                long[] bets = config.BetLevelsCents;

                Assert.AreEqual(20, bets.Length);
                Assert.AreEqual(25, bets[0]);
                Assert.AreEqual(500, bets[bets.Length - 1]);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultConfig_HasExpectedPlacementSettings()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                Assert.AreEqual(1000, config.MaxGenerationAttempts);
                Assert.AreEqual(2, config.MaxConsecutiveLossesMultiplier);
                Assert.AreEqual(2, config.MaxPrizeGapMultiplier);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultConfig_PrizeBagsAreEmptyUntilConfigured()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                Assert.IsNotNull(config.RegularBagPayoutsCents);
                Assert.IsNotNull(config.BigBagPayoutsCents);

                Assert.AreEqual(0, config.RegularBagPayoutsCents.Length);
                Assert.AreEqual(0, config.BigBagPayoutsCents.Length);

                Assert.IsFalse(config.HasConfiguredPrizeBags());
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PrizeBagGetters_ReturnCopies()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                long[] first = config.RegularBagPayoutsCents;
                long[] second = config.RegularBagPayoutsCents;

                Assert.AreNotSame(first, second);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultConfig_HasValidConfiguration()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                Assert.IsTrue(config.Validate(out string error), error);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultConfig_HasNoConfiguredPrizeBags()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                Assert.IsFalse(config.HasConfiguredPrizeBags());
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PrizeBagGetters_DoNotReturnNull()
        {
            GameConfig config =
                ScriptableObject.CreateInstance<GameConfig>();

            try
            {
                Assert.IsNotNull(config.RegularBagPayoutsCents);
                Assert.IsNotNull(config.BigBagPayoutsCents);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}