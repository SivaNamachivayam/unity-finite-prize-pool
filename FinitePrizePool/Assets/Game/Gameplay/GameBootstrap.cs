using System;
using UnityEngine;
using FinitePrizePool.DealEngine;
using FinitePrizePool.UI;

namespace FinitePrizePool.Gameplay
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;

        [Header("UI")]
        [SerializeField] private FinitePrizePoolUI gameUI;

        [Header("Demo Player Balances")]
        [SerializeField, Min(0)]
        private long playerOneStartingBalanceCents = 10000;

        [SerializeField, Min(0)]
        private long playerTwoStartingBalanceCents = 10000;

        private DealService _dealService;

        private void Awake()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            if (gameConfig == null)
            {
                StopInitialization("GameConfig is not assigned.");
                return;
            }

            if (gameUI == null)
            {
                StopInitialization("FinitePrizePoolUI is not assigned.");
                return;
            }

            if (playerOneStartingBalanceCents < 0 ||
                playerTwoStartingBalanceCents < 0)
            {
                StopInitialization("Starting balance cannot be negative.");
                return;
            }

            if (!gameConfig.Validate(out string error))
            {
                StopInitialization("Invalid config: " + error);
                return;
            }

            if (!gameConfig.HasConfiguredPrizeBags())
            {
                StopInitialization(
                    "Configure Regular and Big payout arrays first.");
                return;
            }

            try
            {
                DealBuilder.ValidateGenerationConfig(gameConfig);

                long[] regular = gameConfig.RegularBagPayoutsCents;
                long[] big = gameConfig.BigBagPayoutsCents;

                long payoutPerCycle = 0;

                checked
                {
                    foreach (long payout in regular)
                        payoutPerCycle += payout;

                    foreach (long payout in big)
                        payoutPerCycle += payout;
                }

                int prizeTicketsPerCycle =
                    checked(regular.Length + big.Length);

                if (payoutPerCycle <= 0 ||
                    prizeTicketsPerCycle <= 0)
                {
                    StopInitialization(
                        "Demo payout total and prize ticket count " +
                        "must be greater than zero.");
                    return;
                }

                _dealService = new DealService();

                _dealService.RegisterPlayer(
                    "P1", "Player 1",
                    playerOneStartingBalanceCents);

                _dealService.RegisterPlayer(
                    "P2", "Player 2",
                    playerTwoStartingBalanceCents);

                int generatedDeals = 0;

                foreach (GameMode mode in
                         Enum.GetValues(typeof(GameMode)))
                {
                    foreach (long betCents in gameConfig.BetLevelsCents)
                    {
                        CalculateDealPlan(
                            betCents,
                            payoutPerCycle,
                            prizeTicketsPerCycle,
                            out int cycles,
                            out int losses);

                        DealRecord deal = DealBuilder.GenerateDeal(
                            Guid.NewGuid().ToString("N"),
                            gameConfig,
                            mode,
                            betCents,
                            cycles,
                            losses);

                        DealBuilder.ActivateDeal(deal);
                        _dealService.RegisterActiveDeal(deal);

                        generatedDeals++;
                    }
                }

                gameUI.Initialize(_dealService);
                gameUI.SetPlayer("P1");

                Debug.Log(
                    $"Demo initialized. Deals created: {generatedDeals}. " +
                    "Verify original client payouts before final submission.",
                    this);
            }
            catch (Exception exception)
            {
                StopInitialization(
                    "Deal initialization failed: " + exception.Message);

                Debug.LogException(exception, this);
            }
        }

        private static void CalculateDealPlan(
            long betCents,
            long payoutPerCycle,
            int prizeTicketsPerCycle,
            out int cycles,
            out int losses)
        {
            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(nameof(betCents));

            long numerator;
            long denominator;

            checked
            {
                numerator = 10L * payoutPerCycle;
                denominator = 9L * betCents;
            }

            long gcd = GreatestCommonDivisor(
                numerator, denominator);

            long requiredCycles = denominator / gcd;
            long totalTickets = numerator / gcd;

            long prizeTickets;
            checked
            {
                prizeTickets =
                    requiredCycles * prizeTicketsPerCycle;
            }

            long lossTickets = totalTickets - prizeTickets;

            if (lossTickets < 0)
            {
                throw new InvalidOperationException(
                    $"Bet {betCents} cents cannot achieve exact 90% " +
                    "RTP with the configured prize bag.");
            }

            if (requiredCycles > int.MaxValue ||
                lossTickets > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Required deal size exceeds supported limits.");
            }

            cycles = (int)requiredCycles;
            losses = (int)lossTickets;
        }

        private static long GreatestCommonDivisor(long a, long b)
        {
            while (b != 0)
            {
                long remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }

        private void StopInitialization(string message)
        {
            Debug.LogError("Finite Prize Pool: " + message, this);
        }
    }
}