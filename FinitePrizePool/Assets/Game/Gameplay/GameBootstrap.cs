using System;
using UnityEngine;
using FinitePrizePool.DealEngine;
using FinitePrizePool.Persistence;
using FinitePrizePool.UI;

namespace FinitePrizePool.Gameplay
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Game Configuration")] [SerializeField]
        private GameConfig gameConfig;

        [SerializeField] private FinitePrizePoolUI gameUI;

        [Header("Deal Audit UI")] [SerializeField]
        private DealAuditPanel dealAuditPanel;

        [Header("Starting Balance - New Players Only")] [SerializeField]
        private long player1StartingBalanceCents = 10000;

        [SerializeField] private long player2StartingBalanceCents = 10000;

        [Header("Optional Balance Addition")] [SerializeField]
        private long balanceToAddCents = 10000;

        private DealService _dealService;

        private void Awake()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            try
            {
                if (gameConfig == null || gameUI == null)
                {
                    StopInitialization(
                        "Assign GameConfig and Game UI in the Inspector.");
                    return;
                }

                if (!gameConfig.Validate(out string error))
                {
                    StopInitialization($"Invalid GameConfig: {error}");
                    return;
                }

                if (!gameConfig.HasConfiguredPrizeBags())
                {
                    StopInitialization(
                        "Configure the Regular and Big prize bags.");
                    return;
                }

                if (player1StartingBalanceCents < 0 ||
                    player2StartingBalanceCents < 0)
                {
                    StopInitialization(
                        "Starting balances cannot be negative.");
                    return;
                }

                IDealPersistence persistence = new JsonDealPersistence();

                _dealService = new DealService(
                    persistence,
                    CreateReplacementDeal
                );

                EnsurePlayer("P1", player1StartingBalanceCents);
                EnsurePlayer("P2", player2StartingBalanceCents);

                foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
                {
                    foreach (long betCents in gameConfig.BetLevelsCents)
                    {
                        if (_dealService.GetActiveDeal(mode, betCents) == null)
                        {
                            DealRecord deal = CreateReplacementDeal(
                                mode,
                                betCents
                            );

                            DealBuilder.ActivateDeal(deal);
                            _dealService.RegisterActiveDeal(deal);
                        }
                    }
                }

                gameUI.Initialize(_dealService);
                gameUI.SetPlayer("P1");

                if (dealAuditPanel != null)
                {
                    dealAuditPanel.Initialize(_dealService);
                }

                LogBalances();
            }
            catch (Exception ex)
            {
                StopInitialization(
                    $"Game initialization failed: {ex.Message}");
            }
        }

        private void EnsurePlayer(
            string playerId,
            long startingBalanceCents)
        {
            try
            {
                _dealService.GetPlayerBalance(playerId);
            }
            catch (SpinRejectedException)
            {
                _dealService.RegisterPlayer(
                    playerId,
                    playerId,
                    startingBalanceCents
                );
            }
        }

        [ContextMenu("Add Balance To Player 1")]
        private void AddBalanceToPlayer1()
        {
            AddBalanceToPlayer("P1");
        }

        [ContextMenu("Add Balance To Player 2")]
        private void AddBalanceToPlayer2()
        {
            AddBalanceToPlayer("P2");
        }

        private void AddBalanceToPlayer(string playerId)
        {
            if (_dealService == null)
            {
                Debug.LogError(
                    "Game is not initialized. Enter Play mode first.");
                return;
            }

            if (balanceToAddCents <= 0)
            {
                Debug.LogError(
                    "Balance to add must be greater than zero.");
                return;
            }

            try
            {
                _dealService.AddBalance(
                    playerId,
                    balanceToAddCents
                );

                gameUI.SetPlayer(playerId);
                LogBalances();

                Debug.Log(
                    $"Added ${balanceToAddCents / 100f:F2} to {playerId}.");
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Could not add balance to {playerId}: {ex.Message}");
            }
        }

        private DealRecord CreateReplacementDeal(
            GameMode mode,
            long betCents)
        {
            long regularPayout =
                DealCalculator.CalculateTotalPayouts(
                    gameConfig.RegularBagPayoutsCents);

            long bigPayout =
                DealCalculator.CalculateTotalPayouts(
                    gameConfig.BigBagPayoutsCents);

            long payoutPerCycle = checked(
                regularPayout + bigPayout);

            int prizeTicketsPerCycle =
                gameConfig.RegularBagPayoutsCents.Length +
                gameConfig.BigBagPayoutsCents.Length;

            CalculateDealPlan(
                betCents,
                payoutPerCycle,
                prizeTicketsPerCycle,
                out int pairedCycleCount,
                out int lossTicketCount);

            DealRecord deal = DealBuilder.GenerateDeal(
                Guid.NewGuid().ToString("N"),
                gameConfig,
                mode,
                betCents,
                pairedCycleCount,
                lossTicketCount);

            Debug.Log(
                $"Created READY deal | Mode: {mode} | Bet: {betCents} cents | " +
                $"Tickets: {deal.Tickets.Length}");

            return deal;
        }

        private void CalculateDealPlan(
            long betCents,
            long payoutPerCycle,
            int prizeTicketsPerCycle,
            out int pairedCycleCount,
            out int lossTicketCount)
        {
            if (betCents <= 0)
                throw new ArgumentOutOfRangeException(nameof(betCents));

            if (payoutPerCycle <= 0 || prizeTicketsPerCycle <= 0)
                throw new InvalidOperationException(
                    "Payout and prize ticket count must be positive.");

            long numerator = checked(10L * payoutPerCycle);
            long denominator = checked(9L * betCents);

            long gcd = GreatestCommonDivisor(numerator, denominator);

            long cycles = denominator / gcd;
            long totalTickets = numerator / gcd;
            long prizeTickets = checked(cycles * prizeTicketsPerCycle);
            long losses = totalTickets - prizeTickets;

            if (cycles <= 0 ||
                cycles > int.MaxValue ||
                losses < 0 ||
                losses > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Could not calculate a valid deal plan.");
            }

            pairedCycleCount = (int)cycles;
            lossTicketCount = (int)losses;
        }

        private static long GreatestCommonDivisor(long a, long b)
        {
            while (b != 0)
            {
                long remainder = a % b;
                a = b;
                b = remainder;
            }

            return Math.Abs(a);
        }

        private void LogBalances()
        {
            Debug.Log(
                $"[Balance] P1: " +
                $"${_dealService.GetPlayerBalance("P1") / 100f:F2} | " +
                $"P2: ${_dealService.GetPlayerBalance("P2") / 100f:F2}");
        }

        private void StopInitialization(string message)
        {
            Debug.LogError($"[Finite Prize Pool] {message}");
            enabled = false;
        }
    }
}