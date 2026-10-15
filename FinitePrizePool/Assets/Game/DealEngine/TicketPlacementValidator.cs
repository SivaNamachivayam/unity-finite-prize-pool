using System;

namespace FinitePrizePool.DealEngine
{
    public static class TicketPlacementValidator
    {
        public sealed class PlacementLimits
        {
            public int MaxConsecutiveLosses { get; }
            public int MaxRegularGap { get; }
            public int MaxBigGap { get; }

            public PlacementLimits(
                int maxConsecutiveLosses,
                int maxRegularGap,
                int maxBigGap)
            {
                MaxConsecutiveLosses = maxConsecutiveLosses;
                MaxRegularGap = maxRegularGap;
                MaxBigGap = maxBigGap;
            }
        }

        public static PlacementLimits CalculateLimits(
            PrizeTicket[] tickets)
        {
            if (tickets == null || tickets.Length == 0)
                throw new ArgumentException(
                    "Tickets cannot be null or empty.",
                    nameof(tickets));

            int total = tickets.Length;
            int losses = 0;
            int regular = 0;
            int big = 0;

            foreach (PrizeTicket ticket in tickets)
            {
                if (ticket == null)
                    throw new ArgumentException(
                        "Ticket list contains a null ticket.");

                switch (ticket.Type)
                {
                    case PrizeType.Loss:
                        losses++;
                        break;

                    case PrizeType.Regular:
                        regular++;
                        break;

                    case PrizeType.Big:
                        big++;
                        break;
                }
            }

            int prizeSlots = total - losses;

            if (prizeSlots == 0)
                throw new ArgumentException(
                    "A deal must contain at least one prize ticket.");

            if (regular == 0 || big == 0)
                throw new ArgumentException(
                    "A deal must contain Regular and Big tickets.");

            int maxLosses = checked(
                2 * DivideRoundUp(losses, prizeSlots + 1));

            int maxRegularGap = checked(
                2 * DivideRoundUp(total, regular + 1));

            int maxBigGap = checked(
                2 * DivideRoundUp(total, big + 1));

            return new PlacementLimits(
                maxLosses,
                maxRegularGap,
                maxBigGap);
        }

        public static bool Validate(
            PrizeTicket[] tickets,
            out string error)
        {
            error = null;

            PlacementLimits limits;

            try
            {
                limits = CalculateLimits(tickets);
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
            catch (OverflowException)
            {
                error = "Placement limits exceed supported integer range.";
                return false;
            }

            int consecutiveLosses = 0;
            int regularGap = 0;
            int bigGap = 0;

            foreach (PrizeTicket ticket in tickets)
            {
                if (ticket.Type == PrizeType.Loss)
                {
                    consecutiveLosses++;

                    if (consecutiveLosses >
                        limits.MaxConsecutiveLosses)
                    {
                        error =
                            "Maximum consecutive Loss tickets exceeded.";
                        return false;
                    }
                }
                else
                {
                    consecutiveLosses = 0;
                }

                if (ticket.Type == PrizeType.Regular)
                {
                    regularGap = 0;
                }
                else
                {
                    regularGap++;

                    if (regularGap > limits.MaxRegularGap)
                    {
                        error =
                            "Maximum Regular ticket gap exceeded.";
                        return false;
                    }
                }

                if (ticket.Type == PrizeType.Big)
                {
                    bigGap = 0;
                }
                else
                {
                    bigGap++;

                    if (bigGap > limits.MaxBigGap)
                    {
                        error =
                            "Maximum Big ticket gap exceeded.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            if (divisor <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisor));

            return value == 0
                ? 0
                : 1 + ((value - 1) / divisor);
        }
    }
}