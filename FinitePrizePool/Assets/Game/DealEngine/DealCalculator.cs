using System;

namespace FinitePrizePool.DealEngine
{
    public readonly struct DealCalculation
    {
        public long TotalWagersCents { get; }
        public long TotalPayoutsCents { get; }
        public int TotalTickets { get; }
        public int PaidSpins { get; }

        public int LosingTickets => TotalTickets - PaidSpins;

        public decimal ActualRtpPercent =>
            TotalWagersCents > 0
                ? (decimal)TotalPayoutsCents * 100m /
                  TotalWagersCents
                : 0m;

        public bool HasExactly90PercentRtp =>
            TotalWagersCents > 0 &&
            (decimal)TotalPayoutsCents * 100m ==
            (decimal)TotalWagersCents * 90m;

        public DealCalculation(
            long totalWagersCents,
            long totalPayoutsCents,
            int totalTickets,
            int paidSpins)
        {
            if (totalWagersCents < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalWagersCents));

            if (totalPayoutsCents < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalPayoutsCents));

            if (totalTickets < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalTickets));

            if (paidSpins < 0 || paidSpins > totalTickets)
                throw new ArgumentOutOfRangeException(
                    nameof(paidSpins));

            TotalWagersCents = totalWagersCents;
            TotalPayoutsCents = totalPayoutsCents;
            TotalTickets = totalTickets;
            PaidSpins = paidSpins;
        }
    }

    public readonly struct DealPlan
    {
        public int PairedCycleCount { get; }
        public int PrizeTickets { get; }
        public int LosingTickets { get; }
        public DealCalculation Calculation { get; }

        public DealPlan(
            int pairedCycleCount,
            int prizeTickets,
            int losingTickets,
            DealCalculation calculation)
        {
            PairedCycleCount = pairedCycleCount;
            PrizeTickets = prizeTickets;
            LosingTickets = losingTickets;
            Calculation = calculation;
        }
    }

    public static class DealCalculator
    {
        public static DealCalculation Calculate(
            long betCents,
            int paidSpins,
            long totalPayoutsCents,
            int totalTickets)
        {
            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(betCents));

            if (paidSpins <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(paidSpins));

            if (totalPayoutsCents < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalPayoutsCents));

            if (totalTickets < paidSpins)
                throw new ArgumentException(
                    "Total tickets cannot be fewer than paid spins.",
                    nameof(totalTickets));

            long totalWagersCents =
                checked(betCents * (long)totalTickets);

            return new DealCalculation(
                totalWagersCents,
                totalPayoutsCents,
                totalTickets,
                paidSpins);
        }

        public static DealPlan CalculateMinimumPairedCyclePlan(
            long betCents,
            long payoutPerPairedCycleCents,
            int prizeTicketsPerPairedCycle)
        {
            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(betCents));

            if (payoutPerPairedCycleCents <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(payoutPerPairedCycleCents));

            if (prizeTicketsPerPairedCycle <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(prizeTicketsPerPairedCycle));

            long numerator = checked(
                10L * payoutPerPairedCycleCents);

            long denominator = checked(9L * betCents);

            long gcd = GreatestCommonDivisor(
                numerator,
                denominator);

            long cycles = denominator / gcd;

            long ticketsPerPlan = numerator / gcd;

            long prizes = checked(
                cycles * prizeTicketsPerPairedCycle);

            if (ticketsPerPlan < prizes)
            {
                throw new InvalidOperationException(
                    "Invalid prize bag configuration: the required " +
                    "number of prize tickets exceeds the total ticket " +
                    "count for an exact 90% RTP deal.");
            }

            long losses = ticketsPerPlan - prizes;

            if (cycles > int.MaxValue ||
                prizes > int.MaxValue ||
                losses > int.MaxValue ||
                ticketsPerPlan > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "The calculated deal exceeds supported limits.");
            }

            long totalWagers = checked(
                betCents * ticketsPerPlan);

            long totalPayouts = checked(
                payoutPerPairedCycleCents * cycles);

            DealCalculation calculation = Calculate(
                betCents,
                (int)prizes,
                totalPayouts,
                (int)ticketsPerPlan);

            ValidateExact90Percent(calculation);

            return new DealPlan(
                (int)cycles,
                (int)prizes,
                (int)losses,
                calculation);
        }

        public static void ValidateExact90Percent(
            DealCalculation calculation)
        {
            if (calculation.TotalWagersCents <= 0)
                throw new InvalidOperationException(
                    "Total wagers must be greater than zero.");

            if (!calculation.HasExactly90PercentRtp)
                throw new InvalidOperationException(
                    "Deal does not achieve exactly 90% RTP. " +
                    "Do not round or accept an approximate result.");
        }

        public static long CalculateTotalPayouts(
            long[] ticketAwardsCents)
        {
            if (ticketAwardsCents == null)
                throw new ArgumentNullException(
                    nameof(ticketAwardsCents));

            long total = 0;

            for (int i = 0; i < ticketAwardsCents.Length; i++)
            {
                if (ticketAwardsCents[i] < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(ticketAwardsCents),
                        "Ticket awards cannot be negative.");

                total = checked(total + ticketAwardsCents[i]);
            }

            return total;
        }

        public static DealCalculation CalculateFromAwards(
            long betCents,
            long[] ticketAwardsCents)
        {
            if (ticketAwardsCents == null)
                throw new ArgumentNullException(
                    nameof(ticketAwardsCents));

            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(betCents));

            int paidSpins = 0;

            for (int i = 0; i < ticketAwardsCents.Length; i++)
            {
                if (ticketAwardsCents[i] < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(ticketAwardsCents),
                        "Ticket awards cannot be negative.");

                if (ticketAwardsCents[i] > 0)
                    paidSpins++;
            }

            long totalPayoutsCents =
                CalculateTotalPayouts(ticketAwardsCents);

            return Calculate(
                betCents,
                paidSpins,
                totalPayoutsCents,
                ticketAwardsCents.Length);
        }

        private static long GreatestCommonDivisor(
            long a,
            long b)
        {
            while (b != 0)
            {
                long remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }
    }
}