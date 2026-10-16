using System;
using NUnit.Framework;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Tests
{
    public sealed class DealServiceTests
    {
        private DealService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DealService(
                replacementDealFactory: (mode, betCents) =>
                {
                    return new DealRecord
                    {
                        DealId = Guid.NewGuid().ToString("N"),
                        ConfigVersion = "1.0.0",
                        Mode = mode,
                        BetCents = betCents,
                        State = DealState.Active,
                        Tickets = new[]
                        {
                            new PrizeTicket
                            {
                                TicketId = Guid.NewGuid().ToString("N"),
                                Type = PrizeType.Loss,
                                AwardCents = 0,
                                CycleNumber = 1
                            }
                        },
                        CurrentTicketPosition = 0,
                        TotalWagersCents = 0,
                        TotalPayoutsCents = 0
                    };
                });

            _service.RegisterPlayer(
                "P1",
                "Player One",
                1000);

            var deal = new DealRecord
            {
                DealId = "DEAL-001",
                ConfigVersion = "1.0.0",
                Mode = GameMode.Inner,
                BetCents = 100,
                State = DealState.Active,

                Tickets = new[]
                {
                    new PrizeTicket
                    {
                        TicketId = "T1",
                        Type = PrizeType.Regular,
                        AwardCents = 50,
                        CycleNumber = 1
                    },
                    new PrizeTicket
                    {
                        TicketId = "T2",
                        Type = PrizeType.Loss,
                        AwardCents = 0,
                        CycleNumber = 1
                    }
                },

                CurrentTicketPosition = 0,
                TotalWagersCents = 0,
                TotalPayoutsCents = 0
            };

            _service.RegisterActiveDeal(deal);
        }

        [Test]
        public void Spin_CompletesDealAfterLastTicket()
        {
            _service.Spin("P1", GameMode.Inner, 100);
            _service.Spin("P1", GameMode.Inner, 100);

            DealRecord deal =
                _service.GetDealById("DEAL-001");

            Assert.IsNotNull(deal);
            Assert.AreEqual(DealState.Completed, deal.State);
            Assert.AreEqual(2, deal.CurrentTicketPosition);
            Assert.AreEqual(2, _service.GetSpinHistory().Count);

            DealRecord replacement =
                _service.GetActiveDeal(GameMode.Inner, 100);

            Assert.IsNotNull(replacement);
            Assert.AreNotEqual("DEAL-001", replacement.DealId);
            Assert.AreEqual(DealState.Active, replacement.State);
            Assert.AreEqual(0, replacement.CurrentTicketPosition);
        }

        [Test]
        public void Spin_RecordsSpinHistory()
        {
            _service.Spin("P1", GameMode.Inner, 100);

            Assert.AreEqual(1, _service.GetSpinHistory().Count);

            SpinRecord spin = _service.GetSpinHistory()[0];

            Assert.AreEqual("P1", spin.PlayerId);
            Assert.AreEqual("DEAL-001", spin.DealId);
            Assert.AreEqual(100L, spin.WagerCents);
            Assert.AreEqual(50L, spin.AwardCents);
            Assert.AreEqual(0, spin.TicketPosition);
        }


        [Test]
        public void Spin_RejectsInsufficientBalance()
        {
            _service = new DealService(
                replacementDealFactory: (mode, betCents) =>
                {
                    return new DealRecord
                    {
                        DealId = Guid.NewGuid().ToString("N"),
                        ConfigVersion = "1.0.0",
                        Mode = mode,
                        BetCents = betCents,
                        State = DealState.Active,
                        Tickets = new[]
                        {
                            new PrizeTicket
                            {
                                TicketId = Guid.NewGuid().ToString("N"),
                                Type = PrizeType.Loss,
                                AwardCents = 0,
                                CycleNumber = 1
                            }
                        },
                        CurrentTicketPosition = 0,
                        TotalWagersCents = 0,
                        TotalPayoutsCents = 0
                    };
                });

            _service.RegisterPlayer("P1", "Player One", 50);

            var deal = new DealRecord
            {
                DealId = "DEAL-001",
                ConfigVersion = "1.0.0",
                Mode = GameMode.Inner,
                BetCents = 100,
                State = DealState.Active,
                Tickets = new[]
                {
                    new PrizeTicket
                    {
                        TicketId = "T1",
                        Type = PrizeType.Loss,
                        AwardCents = 0,
                        CycleNumber = 1
                    }
                },
                CurrentTicketPosition = 0,
                TotalWagersCents = 0,
                TotalPayoutsCents = 0
            };

            _service.RegisterActiveDeal(deal);

            Assert.Throws<SpinRejectedException>(() => _service.Spin("P1", GameMode.Inner, 100));
        }


        [Test]
        public void Spin_RejectsMissingDeal()
        {
            Assert.Throws<SpinRejectedException>(() => _service.Spin("P1", GameMode.Outer, 100));
        }

        [Test]
        public void Spin_RejectsSpinAfterDealIsCompleted()
        {
            _service.Spin("P1", GameMode.Inner, 100);
            _service.Spin("P1", GameMode.Inner, 100);

            DealRecord completedDeal =
                _service.GetDealById("DEAL-001");

            Assert.IsNotNull(completedDeal);
            Assert.AreEqual(
                DealState.Completed,
                completedDeal.State);

            DealRecord replacement =
                _service.GetActiveDeal(GameMode.Inner, 100);

            Assert.IsNotNull(replacement);
            Assert.AreNotEqual("DEAL-001", replacement.DealId);

            _service.Spin("P1", GameMode.Inner, 100);

            Assert.AreEqual(
                DealState.Completed,
                replacement.State);
        }

        [Test]
        public void Spin_RejectsUnknownPlayer()
        {
            Assert.Throws<SpinRejectedException>(() => _service.Spin("UNKNOWN", GameMode.Inner, 100));
        }

        [Test]
        public void Spin_UpdatesPlayerBalanceAndDealTotals()
        {
            _service.Spin("P1", GameMode.Inner, 100);

            Assert.AreEqual(950L, _service.GetPlayerBalance("P1"));

            DealRecord deal =
                _service.GetDealById("DEAL-001");

            Assert.AreEqual(100L, deal.TotalWagersCents);
            Assert.AreEqual(50L, deal.TotalPayoutsCents);
            Assert.AreEqual(1, deal.CurrentTicketPosition);
        }

        [Test]
        public void RetireActiveDeal_RecordsRetirementAudit()
        {
            bool retired = _service.RetireActiveDeal(
                GameMode.Inner,
                100,
                "Testing retirement");

            Assert.IsTrue(retired);

            DealRecord oldDeal =
                _service.GetDealById("DEAL-001");

            Assert.IsNotNull(oldDeal);
            Assert.AreEqual(DealState.Retired, oldDeal.State);

            DealRecord replacement =
                _service.GetActiveDeal(GameMode.Inner, 100);

            Assert.IsNotNull(replacement);
            Assert.AreNotEqual("DEAL-001", replacement.DealId);
            Assert.AreEqual(DealState.Active, replacement.State);

            var history = _service.GetRetirementHistory();

            Assert.AreEqual(1, history.Count);

            DealRetirementRecord audit = history[0];

            Assert.AreEqual("DEAL-001", audit.RetiredDealId);
            Assert.AreEqual(
                replacement.DealId,
                audit.ReplacementDealId);
            Assert.AreEqual("Testing retirement", audit.Reason);
            Assert.AreEqual(0, audit.TicketsUsed);
            Assert.AreEqual(2, audit.TicketsRemaining);
            Assert.AreEqual(0L, audit.TotalWagersCents);
            Assert.AreEqual(0L, audit.TotalPayoutsCents);
            Assert.IsFalse(audit.ReachedTargetRtp);
            Assert.IsFalse(
                string.IsNullOrEmpty(audit.RetiredAtUtc));
        }
    }
}