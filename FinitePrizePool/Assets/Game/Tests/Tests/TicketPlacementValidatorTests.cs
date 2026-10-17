using NUnit.Framework;
using FinitePrizePool.DealEngine;

public sealed class TicketPlacementValidatorTests
{
    private static PrizeTicket Ticket(PrizeType type)
    {
        return new PrizeTicket
        {
            TicketId = System.Guid.NewGuid().ToString("N"),
            Type = type,
            AwardCents = type == PrizeType.Regular ? 400 :
                type == PrizeType.Big ? 500 : 0,
            CycleNumber = 1
        };
    }

    [Test]
    public void CalculateLimits_RejectsDealWithoutPrizeTickets()
    {
        PrizeTicket[] tickets =
        {
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Loss)
        };

        Assert.Throws<System.ArgumentException>(() => TicketPlacementValidator.CalculateLimits(tickets));
    }

    [Test]
    public void Validate_RejectsNullTickets()
    {
        Assert.IsFalse(
            TicketPlacementValidator.Validate(null, out string error));

        Assert.IsNotEmpty(error);
    }

    [Test]
    public void Validate_AcceptsValidTenTicketDeal()
    {
        PrizeTicket[] tickets =
        {
            Ticket(PrizeType.Regular),
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Big),
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Regular),
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Loss),
            Ticket(PrizeType.Big)
        };

        Assert.IsTrue(
            TicketPlacementValidator.Validate(tickets, out string error),
            error);
    }
}