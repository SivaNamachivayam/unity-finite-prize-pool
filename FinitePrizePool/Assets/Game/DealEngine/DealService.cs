
using System;
using System.Collections.Generic;

namespace FinitePrizePool.DealEngine
{
    public sealed class DealService : IDealService
    {
        private readonly Dictionary<string, PlayerRecord> _players =
            new Dictionary<string, PlayerRecord>();

        private readonly Dictionary<string, DealRecord> _dealsById =
            new Dictionary<string, DealRecord>();

        private readonly Dictionary<string, DealRecord> _activeDeals =
            new Dictionary<string, DealRecord>();

        private readonly List<SpinRecord> _spinHistory =
            new List<SpinRecord>();

        private readonly IDealPersistence _persistence;

        public DealService(IDealPersistence persistence = null)
        {
            _persistence = persistence;

            if (_persistence != null)
            {
                _persistence.LoadState(
                    out List<PlayerRecord> players,
                    out List<DealRecord> deals,
                    out List<SpinRecord> spinHistory);

                RestoreState(players, deals, spinHistory);
            }
        }

        public void RestoreState(
            IReadOnlyList<PlayerRecord> players,
            IReadOnlyList<DealRecord> deals,
            IReadOnlyList<SpinRecord> spinHistory)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            if (deals == null)
                throw new ArgumentNullException(nameof(deals));

            if (spinHistory == null)
                throw new ArgumentNullException(nameof(spinHistory));

            var restoredPlayers =
                new Dictionary<string, PlayerRecord>();

            var restoredDeals =
                new Dictionary<string, DealRecord>();

            var restoredActiveDeals =
                new Dictionary<string, DealRecord>();

            var restoredHistory = new List<SpinRecord>();
            var spinIds = new HashSet<string>();

            foreach (PlayerRecord player in players)
            {
                if (player == null ||
                    string.IsNullOrWhiteSpace(player.PlayerId) ||
                    player.BalanceCents < 0 ||
                    restoredPlayers.ContainsKey(player.PlayerId))
                {
                    throw new InvalidOperationException(
                        "Invalid or duplicate player in saved data.");
                }

                restoredPlayers.Add(player.PlayerId, player);
            }

            foreach (DealRecord deal in deals)
            {
                if (deal == null)
                    throw new InvalidOperationException(
                        "Saved data contains a null deal.");

                DealValidator.ValidateStructure(deal);

                if (restoredDeals.ContainsKey(deal.DealId))
                    throw new InvalidOperationException(
                        "Duplicate deal ID in saved data.");

                restoredDeals.Add(deal.DealId, deal);

                if (deal.State == DealState.Active)
                {
                    string key = GetDealKey(deal.Mode, deal.BetCents);

                    if (restoredActiveDeals.ContainsKey(key))
                        throw new InvalidOperationException(
                            "Multiple active deals exist for the same mode and bet.");

                    restoredActiveDeals.Add(key, deal);
                }
            }

            foreach (SpinRecord spin in spinHistory)
            {
                if (spin == null ||
                    string.IsNullOrWhiteSpace(spin.SpinId) ||
                    string.IsNullOrWhiteSpace(spin.DealId) ||
                    string.IsNullOrWhiteSpace(spin.PlayerId) ||
                    spin.TicketPosition < 0 ||
                    spin.WagerCents <= 0 ||
                    spin.AwardCents < 0 ||
                    !spinIds.Add(spin.SpinId))
                {
                    throw new InvalidOperationException(
                        "Invalid or duplicate spin record in saved data.");
                }

                if (!restoredDeals.TryGetValue(
                        spin.DealId, out DealRecord spinDeal) ||
                    !restoredPlayers.ContainsKey(spin.PlayerId))
                {
                    throw new InvalidOperationException(
                        "Spin record references a missing deal or player.");
                }

                if (spin.TicketPosition >= spinDeal.Tickets.Length ||
                    spinDeal.Tickets[spin.TicketPosition].AwardCents != spin.AwardCents ||
                    spinDeal.BetCents != spin.WagerCents)
                {
                    throw new InvalidOperationException(
                        "Spin record does not match its saved ticket.");
                }

                restoredHistory.Add(spin);
            }

            _players.Clear();
            _dealsById.Clear();
            _activeDeals.Clear();
            _spinHistory.Clear();

            foreach (var item in restoredPlayers)
                _players.Add(item.Key, item.Value);

            foreach (var item in restoredDeals)
                _dealsById.Add(item.Key, item.Value);

            foreach (var item in restoredActiveDeals)
                _activeDeals.Add(item.Key, item.Value);

            _spinHistory.AddRange(restoredHistory);
        }

        public void RegisterPlayer(
            string playerId,
            string displayName,
            long startingBalanceCents)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("Player ID is required.");

            if (_players.ContainsKey(playerId))
                throw new InvalidOperationException("Player already exists.");

            if (startingBalanceCents < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(startingBalanceCents));

            var player = new PlayerRecord
            {
                PlayerId = playerId,
                DisplayName = displayName,
                BalanceCents = startingBalanceCents
            };

            _players.Add(playerId, player);

            try
            {
                SaveCurrentState();
            }
            catch
            {
                _players.Remove(playerId);
                throw;
            }
        }

        public void RegisterActiveDeal(DealRecord deal)
        {
            if (deal == null)
                throw new ArgumentNullException(nameof(deal));

            if (string.IsNullOrWhiteSpace(deal.DealId))
                throw new ArgumentException("Deal ID is required.");

            if (deal.State != DealState.Active)
                throw new InvalidOperationException(
                    "Only an active deal can be registered.");

            DealValidator.ValidateStructure(deal);

            if (_dealsById.ContainsKey(deal.DealId))
                throw new InvalidOperationException("Deal ID already exists.");

            string key = GetDealKey(deal.Mode, deal.BetCents);

            if (_activeDeals.ContainsKey(key))
                throw new InvalidOperationException(
                    "An active deal already exists for this mode and bet.");

            _dealsById.Add(deal.DealId, deal);
            _activeDeals.Add(key, deal);

            try
            {
                SaveCurrentState();
            }
            catch
            {
                _dealsById.Remove(deal.DealId);
                _activeDeals.Remove(key);
                throw;
            }
        }

        public SpinRecord Spin(
            string playerId,
            GameMode mode,
            long betCents)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new SpinRejectedException("Player ID is required.");

            if (!Enum.IsDefined(typeof(GameMode), mode))
                throw new SpinRejectedException("Invalid game mode.");

            if (betCents <= 0)
                throw new SpinRejectedException("Bet must be positive.");

            if (!_players.TryGetValue(playerId, out PlayerRecord player))
                throw new SpinRejectedException("Player was not found.");

            if (player.BalanceCents < betCents)
                throw new SpinRejectedException("Insufficient balance.");

            string key = GetDealKey(mode, betCents);

            if (!_activeDeals.TryGetValue(key, out DealRecord deal))
                throw new SpinRejectedException("No active deal found.");

            if (deal.State != DealState.Active ||
                deal.Tickets == null ||
                deal.CurrentTicketPosition < 0 ||
                deal.CurrentTicketPosition >= deal.Tickets.Length)
            {
                throw new SpinRejectedException(
                    "Deal has no available tickets.");
            }

            PrizeTicket ticket = deal.Tickets[deal.CurrentTicketPosition];

            if (ticket == null || ticket.AwardCents < 0)
                throw new SpinRejectedException("Invalid ticket.");

            long nextBalance;
            long nextWagers;
            long nextPayouts;

            try
            {
                nextBalance = checked(
                    player.BalanceCents - betCents + ticket.AwardCents);

                nextWagers = checked(deal.TotalWagersCents + betCents);

                nextPayouts = checked(
                    deal.TotalPayoutsCents + ticket.AwardCents);
            }
            catch (OverflowException)
            {
                throw new SpinRejectedException(
                    "Spin totals exceeded the supported limit.");
            }

            int oldPosition = deal.CurrentTicketPosition;
            DealState oldState = deal.State;
            long oldBalance = player.BalanceCents;
            long oldWagers = deal.TotalWagersCents;
            long oldPayouts = deal.TotalPayoutsCents;

            var record = new SpinRecord
            {
                SpinId = Guid.NewGuid().ToString("N"),
                DealId = deal.DealId,
                PlayerId = playerId,
                TicketPosition = oldPosition,
                WagerCents = betCents,
                AwardCents = ticket.AwardCents,
                ResultShown = false
            };

            player.BalanceCents = nextBalance;
            deal.TotalWagersCents = nextWagers;
            deal.TotalPayoutsCents = nextPayouts;
            deal.CurrentTicketPosition = oldPosition + 1;

            bool completed =
                deal.CurrentTicketPosition == deal.Tickets.Length;

            if (completed)
                deal.State = DealState.Completed;

            _spinHistory.Add(record);

            try
            {
                if (completed)
                    _activeDeals.Remove(key);

                SaveCurrentState();
            }
            catch
            {
                player.BalanceCents = oldBalance;
                deal.TotalWagersCents = oldWagers;
                deal.TotalPayoutsCents = oldPayouts;
                deal.CurrentTicketPosition = oldPosition;
                deal.State = oldState;
                _spinHistory.Remove(record);

                if (oldState == DealState.Active)
                    _activeDeals[key] = deal;

                throw;
            }

            return record;
        }

        public bool MarkResultShown(string spinId)
        {
            if (string.IsNullOrWhiteSpace(spinId))
                return false;

            SpinRecord record = _spinHistory.Find(
                spin => spin.SpinId == spinId);

            if (record == null || record.ResultShown)
                return false;

            if (_persistence != null)
                _persistence.MarkResultShown(spinId);

            record.ResultShown = true;
            return true;
        }

        public SpinRecord GetSpinById(string spinId)
        {
            if (string.IsNullOrWhiteSpace(spinId))
                return null;

            return _spinHistory.Find(spin => spin.SpinId == spinId);
        }

        public SpinRecord GetPendingResult(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return null;

            for (int i = _spinHistory.Count - 1; i >= 0; i--)
            {
                SpinRecord spin = _spinHistory[i];

                if (spin.PlayerId == playerId && !spin.ResultShown)
                    return spin;
            }

            return null;
        }

        public DealRecord GetActiveDeal(GameMode mode, long betCents)
        {
            _activeDeals.TryGetValue(
                GetDealKey(mode, betCents),
                out DealRecord deal);

            return deal;
        }

        public DealRecord GetDealById(string dealId)
        {
            if (string.IsNullOrWhiteSpace(dealId))
                return null;

            _dealsById.TryGetValue(dealId, out DealRecord deal);
            return deal;
        }

        public long GetPlayerBalance(string playerId)
        {
            if (!_players.TryGetValue(playerId, out PlayerRecord player))
                throw new KeyNotFoundException("Player was not found.");

            return player.BalanceCents;
        }

        public IReadOnlyList<SpinRecord> GetSpinHistory()
        {
            return _spinHistory.AsReadOnly();
        }

        private void SaveCurrentState()
        {
            if (_persistence == null)
                return;

            _persistence.SaveState(
                new List<PlayerRecord>(_players.Values),
                new List<DealRecord>(_dealsById.Values),
                new List<SpinRecord>(_spinHistory));
        }

        private static string GetDealKey(GameMode mode, long betCents)
        {
            return $"{mode}:{betCents}";
        }
    }
}
