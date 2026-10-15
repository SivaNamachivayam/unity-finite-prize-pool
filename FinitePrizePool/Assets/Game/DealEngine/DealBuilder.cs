
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace FinitePrizePool.DealEngine
{
    public static class DealBuilder
    {
        public static DealRecord CreateDealShell(
            string dealId,
            string configVersion,
            GameMode mode,
            long betCents)
        {
            if (string.IsNullOrWhiteSpace(dealId))
                throw new ArgumentException(
                    "Deal ID is required.", nameof(dealId));

            if (string.IsNullOrWhiteSpace(configVersion))
                throw new ArgumentException(
                    "Config version is required.", nameof(configVersion));

            if (!Enum.IsDefined(typeof(GameMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(nameof(betCents));

            return new DealRecord
            {
                DealId = dealId,
                ConfigVersion = configVersion,
                Mode = mode,
                BetCents = betCents,
                State = DealState.Ready,
                Tickets = Array.Empty<PrizeTicket>(),
                CurrentTicketPosition = 0,
                TotalWagersCents = 0,
                TotalPayoutsCents = 0
            };
        }

        public static void ValidateGenerationConfig(GameConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.Validate(out string error))
                throw new InvalidOperationException(
                    "Invalid game configuration: " + error);

            if (!config.HasConfiguredPrizeBags())
                throw new InvalidOperationException(
                    "Regular and Big prize bags must both be configured.");
        }

        public static void SecureShuffle<T>(T[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);

                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        public static DealRecord BuildDeal(
            string dealId,
            GameConfig config,
            GameMode mode,
            long betCents,
            PrizeTicket[] orderedTickets)
        {
            ValidateGenerationConfig(config);
            ValidateModeAndBet(config, mode, betCents);

            if (orderedTickets == null)
                throw new ArgumentNullException(nameof(orderedTickets));

            if (orderedTickets.Length == 0)
                throw new ArgumentException(
                    "A deal must contain tickets.", nameof(orderedTickets));

            DealRecord deal = CreateDealShell(
                dealId, config.ConfigVersion, mode, betCents);

            deal.Tickets = (PrizeTicket[])orderedTickets.Clone();

            DealValidator.ValidateStructure(deal);

            if (!TicketPlacementValidator.Validate(
                    deal.Tickets, out string placementError))
            {
                throw new InvalidOperationException(
                    "Ticket placement validation failed: " + placementError);
            }

            ValidateExactRtp(deal);

            return deal;
        }

        public static DealRecord GenerateDeal(
            string dealId,
            GameConfig config,
            GameMode mode,
            long betCents,
            int pairedCycleCount,
            int lossTicketCount)
        {
            ValidateGenerationConfig(config);
            ValidateModeAndBet(config, mode, betCents);

            if (pairedCycleCount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(pairedCycleCount),
                    "Paired cycle count must be greater than zero.");

            if (lossTicketCount < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(lossTicketCount),
                    "Loss ticket count cannot be negative.");

            long[] regularPayouts = config.RegularBagPayoutsCents;
            long[] bigPayouts = config.BigBagPayoutsCents;

            if (regularPayouts == null || regularPayouts.Length == 0)
                throw new InvalidOperationException(
                    "Regular prize bag has no payouts configured.");

            if (bigPayouts == null || bigPayouts.Length == 0)
                throw new InvalidOperationException(
                    "Big prize bag has no payouts configured.");

            List<PrizeTicket> regularTickets =
                new List<PrizeTicket>();

            List<PrizeTicket> bigTickets =
                new List<PrizeTicket>();

            List<PrizeTicket> lossTickets =
                new List<PrizeTicket>();

            for (int cycle = 1; cycle <= pairedCycleCount; cycle++)
            {
                PrizeTicket[] regularCycle =
                    CreateCycleTickets(
                        regularPayouts,
                        PrizeType.Regular,
                        cycle,
                        "R");

                PrizeTicket[] bigCycle =
                    CreateCycleTickets(
                        bigPayouts,
                        PrizeType.Big,
                        cycle,
                        "B");

                SecureShuffle(regularCycle);
                SecureShuffle(bigCycle);

                regularTickets.AddRange(regularCycle);
                bigTickets.AddRange(bigCycle);
            }

            for (int i = 0; i < lossTicketCount; i++)
            {
                lossTickets.Add(new PrizeTicket
                {
                    TicketId = "L-" + Guid.NewGuid().ToString("N"),
                    Type = PrizeType.Loss,
                    AwardCents = 0,
                    CycleNumber = 1
                });
            }

            List<PrizeTicket> allTickets =
                new List<PrizeTicket>(
                    regularTickets.Count +
                    bigTickets.Count +
                    lossTickets.Count);

            allTickets.AddRange(regularTickets);
            allTickets.AddRange(bigTickets);
            allTickets.AddRange(lossTickets);

            if (allTickets.Count == 0)
                throw new InvalidOperationException(
                    "Generated deal contains no tickets.");

            PrizeType[] categorySlots =
                new PrizeType[allTickets.Count];

            for (int i = 0; i < regularTickets.Count; i++)
                categorySlots[i] = PrizeType.Regular;

            for (int i = 0; i < bigTickets.Count; i++)
                categorySlots[regularTickets.Count + i] = PrizeType.Big;

            for (int i = 0; i < lossTickets.Count; i++)
            {
                categorySlots[
                    regularTickets.Count +
                    bigTickets.Count + i] = PrizeType.Loss;
            }

            DealRecord shell = CreateDealShell(
                dealId, config.ConfigVersion, mode, betCents);

            string lastPlacementError =
                "No valid ticket placement was found.";

            for (int attempt = 0;
                 attempt < config.MaxGenerationAttempts;
                 attempt++)
            {
                PrizeType[] shuffledSlots =
                    (PrizeType[])categorySlots.Clone();

                SecureShuffle(shuffledSlots);

                Queue<PrizeTicket> regularQueue =
                    new Queue<PrizeTicket>(regularTickets);

                Queue<PrizeTicket> bigQueue =
                    new Queue<PrizeTicket>(bigTickets);

                Queue<PrizeTicket> lossQueue =
                    new Queue<PrizeTicket>(lossTickets);

                PrizeTicket[] orderedTickets =
                    new PrizeTicket[shuffledSlots.Length];

                for (int i = 0; i < shuffledSlots.Length; i++)
                {
                    switch (shuffledSlots[i])
                    {
                        case PrizeType.Regular:
                            orderedTickets[i] = regularQueue.Dequeue();
                            break;

                        case PrizeType.Big:
                            orderedTickets[i] = bigQueue.Dequeue();
                            break;

                        case PrizeType.Loss:
                            orderedTickets[i] = lossQueue.Dequeue();
                            break;

                        default:
                            throw new InvalidOperationException(
                                "Unexpected ticket category.");
                    }
                }

                DealRecord candidate = CreateDealShell(
                    dealId, config.ConfigVersion, mode, betCents);

                candidate.Tickets = orderedTickets;

                try
                {
                    DealValidator.ValidateStructure(candidate);

                    if (!TicketPlacementValidator.Validate(
                            candidate.Tickets, out string placementError))
                    {
                        lastPlacementError = placementError;
                        continue;
                    }

                    ValidateExactRtp(candidate);

                    return candidate;
                }
                catch (InvalidOperationException ex)
                {
                    lastPlacementError = ex.Message;
                }
            }

            throw new InvalidOperationException(
                "Could not generate a valid deal after " +
                config.MaxGenerationAttempts +
                " attempts. Last validation error: " +
                lastPlacementError);
        }

        private static PrizeTicket[] CreateCycleTickets(
            long[] payouts,
            PrizeType type,
            int cycleNumber,
            string prefix)
        {
            PrizeTicket[] tickets = new PrizeTicket[payouts.Length];

            for (int i = 0; i < payouts.Length; i++)
            {
                if (payouts[i] < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(payouts),
                        "Prize payout cannot be negative.");

                tickets[i] = new PrizeTicket
                {
                    TicketId = prefix + "-" +
                               cycleNumber + "-" +
                               (i + 1) + "-" +
                               Guid.NewGuid().ToString("N"),

                    Type = type,
                    AwardCents = payouts[i],
                    CycleNumber = cycleNumber
                };
            }

            return tickets;
        }

        private static void ValidateModeAndBet(
            GameConfig config,
            GameMode mode,
            long betCents)
        {
            if (!Enum.IsDefined(typeof(GameMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(nameof(betCents));

            long[] betLevels = config.BetLevelsCents;

            bool found = false;

            for (int i = 0; i < betLevels.Length; i++)
            {
                if (betLevels[i] == betCents)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new ArgumentException(
                    "Bet amount is not present in GameConfig.",
                    nameof(betCents));
        }

       
        private static void ValidateExactRtp(DealRecord deal)
        {
            if (deal == null)
                throw new ArgumentNullException(nameof(deal));

            if (deal.Tickets == null || deal.Tickets.Length == 0)
                throw new InvalidOperationException(
                    "A deal must contain at least one ticket.");

            long totalPayoutsCents = 0;

            for (int i = 0; i < deal.Tickets.Length; i++)
            {
                PrizeTicket ticket = deal.Tickets[i];

                if (ticket == null)
                    throw new InvalidOperationException(
                        "Deal contains a null ticket.");

                if (ticket.AwardCents < 0)
                    throw new InvalidOperationException(
                        "Ticket award cannot be negative.");

                totalPayoutsCents = checked(
                    totalPayoutsCents + ticket.AwardCents);
            }

            int totalTickets = deal.Tickets.Length;

            DealCalculation calculation = DealCalculator.Calculate(
                deal.BetCents,
                totalTickets,
                totalPayoutsCents,
                totalTickets);

            DealCalculator.ValidateExact90Percent(calculation);
        }


        public static void ActivateDeal(DealRecord deal)
        {
            if (deal == null)
                throw new ArgumentNullException(nameof(deal));

            if (deal.State != DealState.Ready)
                throw new InvalidOperationException(
                    "Only a Ready deal can be activated.");

            if (deal.Tickets == null || deal.Tickets.Length == 0)
                throw new InvalidOperationException(
                    "Cannot activate a deal without tickets.");

            if (deal.CurrentTicketPosition != 0)
                throw new InvalidOperationException(
                    "A new deal must start at ticket position zero.");

            DealValidator.ValidateStructure(deal);

            deal.State = DealState.Active;
        }
    }
}
