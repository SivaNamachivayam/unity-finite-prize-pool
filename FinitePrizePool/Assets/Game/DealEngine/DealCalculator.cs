
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
    }
}
