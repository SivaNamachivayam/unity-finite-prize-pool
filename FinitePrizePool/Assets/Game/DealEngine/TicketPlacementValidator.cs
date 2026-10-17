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
            {
                throw new ArgumentException(
                    "Tickets cannot be null or empty.",
                    nameof(tickets));
            }

            int total = tickets.Length;
            int losses = 0;
            int regular = 0;
            int big = 0;

            foreach (PrizeTicket ticket in tickets)
            {
                if (ticket == null)
                {
                    throw new ArgumentException(
                        "Ticket list contains a null ticket.");
                }

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

            int prizeSlots = regular + big;

            if (prizeSlots == 0 || regular == 0 || big == 0)
            {
                throw new ArgumentException(
                    "A deal must contain Regular and Big tickets.");
            }

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
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (OverflowException)
            {
                error =
                    "Placement limits exceed supported integer range.";
                return false;
            }

            int n = tickets.Length;

            int firstRegular = -1;
            int lastRegular = -1;
            int firstBig = -1;
            int lastBig = -1;

            int consecutiveLosses = 0;
            int maxObservedLosses = 0;

            for (int i = 0; i < n; i++)
            {
                PrizeType type = tickets[i].Type;

                if (type == PrizeType.Loss)
                {
                    consecutiveLosses++;

                    maxObservedLosses = Math.Max(
                        maxObservedLosses,
                        consecutiveLosses);
                }
                else
                {
                    consecutiveLosses = 0;
                }

                if (type == PrizeType.Regular)
                {
                    if (firstRegular == -1)
                    {
                        firstRegular = i;
                    }

                    if (lastRegular >= 0 &&
                        i - lastRegular - 1 > limits.MaxRegularGap)
                    {
                        error =
                            "Maximum Regular ticket gap exceeded.";
                        return false;
                    }

                    lastRegular = i;
                }

                if (type == PrizeType.Big)
                {
                    if (firstBig == -1)
                    {
                        firstBig = i;
                    }

                    if (lastBig >= 0 &&
                        i - lastBig - 1 > limits.MaxBigGap)
                    {
                        error =
                            "Maximum Big ticket gap exceeded.";
                        return false;
                    }

                    lastBig = i;
                }
            }

            int circularRegularGap =
                firstRegular + n - lastRegular - 1;

            if (circularRegularGap > limits.MaxRegularGap)
            {
                error =
                    "Maximum circular Regular ticket gap exceeded.";
                return false;
            }

            int circularBigGap =
                firstBig + n - lastBig - 1;

            if (circularBigGap > limits.MaxBigGap)
            {
                error =
                    "Maximum circular Big ticket gap exceeded.";
                return false;
            }

            int prefixLosses = 0;
            int suffixLosses = 0;

            while (prefixLosses < n &&
                   tickets[prefixLosses].Type == PrizeType.Loss)
            {
                prefixLosses++;
            }

            while (suffixLosses < n &&
                   tickets[n - 1 - suffixLosses].Type == PrizeType.Loss)
            {
                suffixLosses++;
            }

            if (prefixLosses < n && suffixLosses < n)
            {
                maxObservedLosses = Math.Max(
                    maxObservedLosses,
                    prefixLosses + suffixLosses);
            }
            else
            {
                maxObservedLosses = n;
            }

            if (maxObservedLosses > limits.MaxConsecutiveLosses)
            {
                error =
                    "Maximum consecutive Loss tickets exceeded.";
                return false;
            }

            return true;
        }

        private static int DivideRoundUp(
            int value,
            int divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(divisor));
            }

            return value == 0
                ? 0
                : 1 + ((value - 1) / divisor);
        }
    }
}