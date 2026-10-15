
using System;
using NUnit.Framework;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Tests
{
    public class DealServiceTests
    {
        private DealService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DealService();

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
        public void Spin_UpdatesPlayerBalanceAndDealTotals()
        {
            SpinRecord result = _service.Spin(
                "P1",
                GameMode.Inner,
                100);

            Assert.AreEqual(50, result.AwardCents);
            Assert.AreEqual(950, _service.GetPlayerBalance("P1"));

            DealRecord deal = _service.GetActiveDeal(
                GameMode.Inner,
                100);

            Assert.IsNotNull(deal);
            Assert.AreEqual(100, deal.TotalWagersCents);
            Assert.AreEqual(50, deal.TotalPayoutsCents);
            Assert.AreEqual(1, deal.CurrentTicketPosition);
            Assert.AreEqual(DealState.Active, deal.State);
        }

        [Test]
        public void Spin_RecordsSpinHistory()
        {
            SpinRecord result = _service.Spin(
                "P1",
                GameMode.Inner,
                100);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, _service.GetSpinHistory().Count);

            Assert.AreEqual(
                result.SpinId,
                _service.GetSpinHistory()[0].SpinId);
        }

        [Test]
        public void Spin_RejectsUnknownPlayer()
        {
            Assert.Throws<SpinRejectedException>(() =>
                _service.Spin(
                    "UNKNOWN",
                    GameMode.Inner,
                    100));
        }

        [Test]
        public void Spin_RejectsInsufficientBalance()
        {
            _service.RegisterPlayer(
                "P2",
                "Player Two",
                50);

            Assert.Throws<SpinRejectedException>(() =>
                _service.Spin(
                    "P2",
                    GameMode.Inner,
                    100));

            Assert.AreEqual(
                50,
                _service.GetPlayerBalance("P2"));

            Assert.AreEqual(
                0,
                _service.GetActiveDeal(
                    GameMode.Inner,
                    100).CurrentTicketPosition);
        }

        [Test]
        public void Spin_RejectsMissingDeal()
        {
            Assert.Throws<SpinRejectedException>(() =>
                _service.Spin(
                    "P1",
                    GameMode.Outer,
                    100));
        }

        [Test]
        public void Spin_CompletesDealAfterLastTicket()
        {
            _service.Spin("P1", GameMode.Inner, 100);
            _service.Spin("P1", GameMode.Inner, 100);

            DealRecord deal = _service.GetDealById("DEAL-001");

            Assert.IsNotNull(deal);
            Assert.AreEqual(DealState.Completed, deal.State);
            Assert.AreEqual(2, deal.CurrentTicketPosition);
            Assert.AreEqual(2, _service.GetSpinHistory().Count);

            Assert.IsNull(
                _service.GetActiveDeal(GameMode.Inner, 100));
        }

        [Test]
        public void Spin_RejectsSpinAfterDealIsCompleted()
        {
            _service.Spin("P1", GameMode.Inner, 100);
            _service.Spin("P1", GameMode.Inner, 100);

            Assert.Throws<SpinRejectedException>(() =>
                _service.Spin("P1", GameMode.Inner, 100));

            Assert.AreEqual(
                2,
                _service.GetSpinHistory().Count);

            Assert.AreEqual(
                850,
                _service.GetPlayerBalance("P1"));

            DealRecord deal = _service.GetDealById("DEAL-001");

            Assert.IsNotNull(deal);
            Assert.AreEqual(DealState.Completed, deal.State);
            Assert.AreEqual(2, deal.CurrentTicketPosition);
        }
    }
}
