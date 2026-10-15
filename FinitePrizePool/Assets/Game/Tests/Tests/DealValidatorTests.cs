
using System;
using NUnit.Framework;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Tests
{
    public class DealValidatorTests
    {
        [Test]
        public void ValidateStructure_NullDeal_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DealValidator.ValidateStructure(null));
        }

        [Test]
        public void ValidateStructure_EmptyDealId_ThrowsException()
        {
            DealRecord deal = CreateValidDeal();
            deal.DealId = "";

            Assert.Throws<InvalidOperationException>(() =>
                DealValidator.ValidateStructure(deal));
        }

        [Test]
        public void ValidateStructure_ZeroBet_ThrowsException()
        {
            DealRecord deal = CreateValidDeal();
            deal.BetCents = 0;

            Assert.Throws<InvalidOperationException>(() =>
                DealValidator.ValidateStructure(deal));
        }

        [Test]
        public void ValidateStructure_DuplicateTicketIds_ThrowsException()
        {
            DealRecord deal = CreateValidDeal();

            deal.Tickets = new[]
            {
                CreateLossTicket("TICKET-001"),
                CreateLossTicket("TICKET-001")
            };

            Assert.Throws<InvalidOperationException>(() =>
                DealValidator.ValidateStructure(deal));
        }

        [Test]
        public void ValidateStructure_InvalidGameMode_ThrowsException()
        {
            DealRecord deal = CreateValidDeal();
            deal.Mode = (GameMode)999;

            Assert.Throws<InvalidOperationException>(() =>
                DealValidator.ValidateStructure(deal));
        }

        [Test]
        public void ValidateStructure_ActiveDealWithoutUnusedTicket_ThrowsException()
        {
            DealRecord deal = CreateValidDeal();
            deal.State = DealState.Active;
            deal.CurrentTicketPosition = deal.Tickets.Length;

            Assert.Throws<InvalidOperationException>(() =>
                DealValidator.ValidateStructure(deal));
        }

        [Test]
        public void ValidateStructure_CompletedDealWithUnusedTicket_ThrowsException()
        {
            DealRecord deal = CreateValidDeal();
            deal.State = DealState.Completed;
            deal.CurrentTicketPosition = 0;

            Assert.Throws<InvalidOperationException>(() =>
                DealValidator.ValidateStructure(deal));
        }

        private DealRecord CreateValidDeal()
        {
            return new DealRecord
            {
                DealId = "TEST-001",
                ConfigVersion = "1.0.0",
                Mode = GameMode.Inner,
                BetCents = 100,
                State = DealState.Ready,
                Tickets = new[]
                {
                    CreateLossTicket("TICKET-001")
                },
                CurrentTicketPosition = 0,
                TotalWagersCents = 0,
                TotalPayoutsCents = 0
            };
        }

        private PrizeTicket CreateLossTicket(string ticketId)
        {
            return new PrizeTicket
            {
                TicketId = ticketId,
                Type = PrizeType.Loss,
                AwardCents = 0,
                CycleNumber = 1
            };
        }
    }
}
