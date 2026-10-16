using System;
using System.Collections.Generic;

namespace FinitePrizePool.DealEngine
{
    public static class DealValidator
    {
        public static void ValidateStructure(DealRecord deal)
        {
            if (deal == null)
                throw new ArgumentNullException(nameof(deal));

            if (string.IsNullOrWhiteSpace(deal.DealId))
                throw new InvalidOperationException(
                    "Deal ID is required.");

            if (string.IsNullOrWhiteSpace(deal.ConfigVersion))
                throw new InvalidOperationException(
                    "Configuration version is required.");

            if (!Enum.IsDefined(typeof(GameMode), deal.Mode))
                throw new InvalidOperationException(
                    "Deal contains an invalid game mode.");

            if (deal.BetCents <= 0)
                throw new InvalidOperationException(
                    "Bet must be greater than zero.");

            if (deal.Tickets == null || deal.Tickets.Length == 0)
                throw new InvalidOperationException(
                    "Deal must contain tickets.");

            if (deal.CurrentTicketPosition < 0 ||
                deal.CurrentTicketPosition > deal.Tickets.Length)
                throw new InvalidOperationException(
                    "Ticket position is invalid.");

            if (deal.TotalWagersCents < 0 ||
                deal.TotalPayoutsCents < 0)
                throw new InvalidOperationException(
                    "Deal totals cannot be negative.");

            if (!Enum.IsDefined(typeof(DealState), deal.State))
                throw new InvalidOperationException(
                    "Deal contains an invalid state.");

            if (deal.State == DealState.Active &&
                deal.CurrentTicketPosition >= deal.Tickets.Length)
                throw new InvalidOperationException(
                    "An active deal must have an unused ticket.");

            if (deal.State == DealState.Completed &&
                deal.CurrentTicketPosition != deal.Tickets.Length)
                throw new InvalidOperationException(
                    "A completed deal must have used every ticket.");

            var ticketIds = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (PrizeTicket ticket in deal.Tickets)
            {
                if (ticket == null)
                    throw new InvalidOperationException(
                        "Deal contains a null ticket.");

                if (string.IsNullOrWhiteSpace(ticket.TicketId))
                    throw new InvalidOperationException(
                        "Every ticket needs an ID.");

                if (!ticketIds.Add(ticket.TicketId))
                    throw new InvalidOperationException(
                        "Ticket IDs must be unique.");

                if (!Enum.IsDefined(typeof(PrizeType), ticket.Type))
                    throw new InvalidOperationException(
                        "Ticket contains an invalid prize type.");

                if (ticket.AwardCents < 0)
                    throw new InvalidOperationException(
                        "Ticket award cannot be negative.");

                if (ticket.CycleNumber < 1)
                    throw new InvalidOperationException(
                        "Cycle number must be positive.");

                if (ticket.Type == PrizeType.Loss &&
                    ticket.AwardCents != 0)
                    throw new InvalidOperationException(
                        "Loss tickets must have zero award.");
            }
        }
    }
}