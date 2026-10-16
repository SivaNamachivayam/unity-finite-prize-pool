using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using FinitePrizePool.DealEngine;
using FinitePrizePool.Persistence;

namespace FinitePrizePool.Tests
{
    public sealed class JsonDealPersistenceTests
    {
        private string _testFolder;
        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            _testFolder = Path.Combine(
                Path.GetTempPath(),
                "FinitePrizePoolTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_testFolder);

            _testSavePath = Path.Combine(
                _testFolder,
                "test-save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testFolder) &&
                Directory.Exists(_testFolder))
            {
                Directory.Delete(_testFolder, true);
            }
        }

        [Test]
        public void RetirementHistory_SavesAndLoadsCorrectly()
        {
            IDealPersistence persistence =
                new JsonDealPersistence(_testSavePath);

            var originalHistory = new List<DealRetirementRecord>
            {
                new DealRetirementRecord
                {
                    AuditId = "AUDIT-001",
                    RetiredDealId = "DEAL-001",
                    ReplacementDealId = "DEAL-002",
                    Reason = "Testing retirement",
                    RetiredAtUtc = "2026-10-16T10:00:00.0000000Z",
                    OldConfigVersion = "1.0.0",
                    NewConfigVersion = "1.0.0",
                    Mode = GameMode.Inner,
                    BetCents = 100,
                    TicketsUsed = 3,
                    TicketsRemaining = 7,
                    TotalWagersCents = 300,
                    TotalPayoutsCents = 150,
                    ActualRtpPercent = 50.0,
                    ReachedTargetRtp = false
                }
            };

            persistence.SaveRetirementHistory(originalHistory);

            IDealPersistence reloadedPersistence =
                new JsonDealPersistence(_testSavePath);

            List<DealRetirementRecord> loadedHistory =
                reloadedPersistence.LoadRetirementHistory();

            Assert.IsNotNull(loadedHistory);
            Assert.AreEqual(1, loadedHistory.Count);

            DealRetirementRecord loaded = loadedHistory[0];

            Assert.AreEqual("AUDIT-001", loaded.AuditId);
            Assert.AreEqual("DEAL-001", loaded.RetiredDealId);
            Assert.AreEqual("DEAL-002", loaded.ReplacementDealId);
            Assert.AreEqual("Testing retirement", loaded.Reason);
            Assert.AreEqual(
                "2026-10-16T10:00:00.0000000Z",
                loaded.RetiredAtUtc);
            Assert.AreEqual("1.0.0", loaded.OldConfigVersion);
            Assert.AreEqual("1.0.0", loaded.NewConfigVersion);
            Assert.AreEqual(GameMode.Inner, loaded.Mode);
            Assert.AreEqual(100L, loaded.BetCents);
            Assert.AreEqual(3, loaded.TicketsUsed);
            Assert.AreEqual(7, loaded.TicketsRemaining);
            Assert.AreEqual(300L, loaded.TotalWagersCents);
            Assert.AreEqual(150L, loaded.TotalPayoutsCents);
            Assert.AreEqual(50.0, loaded.ActualRtpPercent);
            Assert.IsFalse(loaded.ReachedTargetRtp);
        }
    }
}