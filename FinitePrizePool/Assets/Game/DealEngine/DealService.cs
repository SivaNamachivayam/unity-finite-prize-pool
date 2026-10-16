using System;
using System.Collections.Generic;
using UnityEngine;

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

        private readonly Dictionary<string, DealRecord> _readyDeals =
            new Dictionary<string, DealRecord>();

        private readonly List<SpinRecord> _spinHistory =
            new List<SpinRecord>();

        private readonly List<DealRetirementRecord> _retirementHistory =
            new List<DealRetirementRecord>();

        private readonly IDealPersistence _persistence;

        private readonly Func<GameMode, long, DealRecord>
            _replacementDealFactory;

        public DealService(
            IDealPersistence persistence = null,
            Func<GameMode, long, DealRecord> replacementDealFactory = null)
        {
            _persistence = persistence;
            _replacementDealFactory = replacementDealFactory;

            if (_persistence == null)
                return;

            _persistence.LoadState(
                out List<PlayerRecord> players,
                out List<DealRecord> deals,
                out List<SpinRecord> spins);

            RestoreState(players, deals, spins);

            List<DealRetirementRecord> audits =
                _persistence.LoadRetirementHistory();

            if (audits != null)
                _retirementHistory.AddRange(audits);

            EnsureReadySuccessorsForActiveDeals();
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

            _players.Clear();
            _dealsById.Clear();
            _activeDeals.Clear();
            _readyDeals.Clear();
            _spinHistory.Clear();

            foreach (PlayerRecord player in players)
            {
                if (player == null ||
                    string.IsNullOrWhiteSpace(player.PlayerId) ||
                    player.BalanceCents < 0 ||
                    _players.ContainsKey(player.PlayerId))
                {
                    throw new InvalidOperationException(
                        "Invalid or duplicate player in saved data.");
                }

                _players.Add(player.PlayerId, player);
            }

            foreach (DealRecord deal in deals)
            {
                if (deal == null)
                    throw new InvalidOperationException(
                        "Saved data contains a null deal.");

                DealValidator.ValidateStructure(deal);

                if (_dealsById.ContainsKey(deal.DealId))
                    throw new InvalidOperationException(
                        "Duplicate deal ID in saved data.");

                _dealsById.Add(deal.DealId, deal);

                string key = GetDealKey(deal.Mode, deal.BetCents);

                if (deal.State == DealState.Active)
                {
                    if (_activeDeals.ContainsKey(key))
                        throw new InvalidOperationException(
                            "Multiple active deals exist for the same mode and bet.");

                    _activeDeals.Add(key, deal);
                }
                else if (deal.State == DealState.Ready)
                {
                    if (_readyDeals.ContainsKey(key))
                        throw new InvalidOperationException(
                            "Multiple ready deals exist for the same mode and bet.");

                    _readyDeals.Add(key, deal);
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
                    !_dealsById.TryGetValue(
                        spin.DealId, out DealRecord deal) ||
                    !_players.ContainsKey(spin.PlayerId) ||
                    deal.Tickets == null ||
                    spin.TicketPosition >= deal.Tickets.Length ||
                    deal.Tickets[spin.TicketPosition] == null ||
                    deal.Tickets[spin.TicketPosition].AwardCents
                    != spin.AwardCents ||
                    deal.BetCents != spin.WagerCents)
                {
                    throw new InvalidOperationException(
                        "Invalid spin record in saved data.");
                }

                foreach (SpinRecord existing in _spinHistory)
                {
                    if (existing.SpinId == spin.SpinId)
                        throw new InvalidOperationException(
                            "Duplicate spin ID in saved data.");
                }

                _spinHistory.Add(spin);
            }
        }

        public void RegisterPlayer(
            string playerId,
            string displayName,
            long startingBalanceCents)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("Player ID is required.");

            if (startingBalanceCents < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(startingBalanceCents));

            if (_players.ContainsKey(playerId))
                return;

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

        public void AddBalance(string playerId, long amountCents)
        {
            if (amountCents <= 0)
                throw new ArgumentOutOfRangeException(nameof(amountCents));

            if (!_players.TryGetValue(playerId, out PlayerRecord player))
                throw new KeyNotFoundException("Player was not found.");

            long oldBalance = player.BalanceCents;

            try
            {
                player.BalanceCents = checked(oldBalance + amountCents);
                SaveCurrentState();
            }
            catch
            {
                player.BalanceCents = oldBalance;
                throw;
            }
        }

        public void RegisterActiveDeal(DealRecord deal)
        {
            if (deal == null)
                throw new ArgumentNullException(nameof(deal));

            if (deal.State != DealState.Active)
                throw new InvalidOperationException(
                    "Only an active deal can be registered.");

            DealValidator.ValidateStructure(deal);

            if (string.IsNullOrWhiteSpace(deal.DealId) ||
                _dealsById.ContainsKey(deal.DealId))
            {
                throw new InvalidOperationException(
                    "Deal ID is missing or already exists.");
            }

            string key = GetDealKey(deal.Mode, deal.BetCents);

            if (_activeDeals.ContainsKey(key))
                throw new InvalidOperationException(
                    "An active deal already exists for this mode and bet.");

            DealRecord readySuccessor = null;

            if (_replacementDealFactory != null &&
                !_readyDeals.ContainsKey(key))
            {
                readySuccessor = CreateReadySuccessor(
                    deal, deal.Mode, deal.BetCents);
            }

            _dealsById.Add(deal.DealId, deal);
            _activeDeals.Add(key, deal);

            if (readySuccessor != null)
            {
                _dealsById.Add(readySuccessor.DealId, readySuccessor);
                _readyDeals.Add(key, readySuccessor);
            }

            try
            {
                SaveCurrentState();
            }
            catch
            {
                _dealsById.Remove(deal.DealId);
                _activeDeals.Remove(key);

                if (readySuccessor != null)
                {
                    _dealsById.Remove(readySuccessor.DealId);
                    _readyDeals.Remove(key);
                }

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

            PrizeTicket ticket =
                deal.Tickets[deal.CurrentTicketPosition];

            if (ticket == null || ticket.AwardCents < 0)
                throw new SpinRejectedException("Invalid ticket.");

            long nextBalance = checked(
                player.BalanceCents - betCents + ticket.AwardCents);

            long nextWagers = checked(
                deal.TotalWagersCents + betCents);

            long nextPayouts = checked(
                deal.TotalPayoutsCents + ticket.AwardCents);

            int oldPosition = deal.CurrentTicketPosition;
            DealState oldState = deal.State;
            long oldBalance = player.BalanceCents;
            long oldWagers = deal.TotalWagersCents;
            long oldPayouts = deal.TotalPayoutsCents;

            bool completed = oldPosition + 1 == deal.Tickets.Length;

            DealRecord readySuccessor = null;
            DealRecord nextReadySuccessor = null;

            if (completed)
            {
                if (!_readyDeals.TryGetValue(key, out readySuccessor) ||
                    readySuccessor == null ||
                    readySuccessor.State != DealState.Ready)
                {
                    throw new SpinRejectedException(
                        "No verified Ready successor is available.");
                }

                if (_replacementDealFactory != null)
                {
                    nextReadySuccessor = CreateReadySuccessor(
                        readySuccessor, mode, betCents);
                }
            }

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

            Debug.Log(
                $"[Deal Progress] Deal ID: {deal.DealId} | " +
                $"Used: {deal.CurrentTicketPosition}/{deal.Tickets.Length} | " +
                $"Remaining: {deal.Tickets.Length - deal.CurrentTicketPosition}");

            if (completed)
                deal.State = DealState.Completed;

            _spinHistory.Add(record);

            bool successorActivated = false;
            bool nextSuccessorAdded = false;

            try
            {
                if (completed)
                {
                    _activeDeals.Remove(key);
                    _readyDeals.Remove(key);

                    readySuccessor.State = DealState.Active;
                    _activeDeals.Add(key, readySuccessor);
                    successorActivated = true;

                    if (nextReadySuccessor != null)
                    {
                        _dealsById.Add(
                            nextReadySuccessor.DealId,
                            nextReadySuccessor);

                        _readyDeals.Add(key, nextReadySuccessor);
                        nextSuccessorAdded = true;
                    }
                }

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

                if (nextSuccessorAdded && nextReadySuccessor != null)
                {
                    _dealsById.Remove(nextReadySuccessor.DealId);
                    _readyDeals.Remove(key);
                }

                _readyDeals.Remove(key);
                _activeDeals.Remove(key);

                if (successorActivated)
                    readySuccessor.State = DealState.Ready;

                if (readySuccessor != null)
                    _readyDeals[key] = readySuccessor;

                if (oldState == DealState.Active)
                    _activeDeals[key] = deal;

                throw;
            }

            return record;
        }

        public bool RetireActiveDeal(
            GameMode mode,
            long betCents,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException(
                    "Retirement reason is required.");

            string key = GetDealKey(mode, betCents);

            if (!_activeDeals.TryGetValue(key, out DealRecord deal))
                return false;

            if (!_readyDeals.TryGetValue(key, out DealRecord replacement) ||
                replacement == null ||
                replacement.State != DealState.Ready)
            {
                throw new InvalidOperationException(
                    "No verified Ready successor is available for retirement.");
            }

            DealRecord nextReadySuccessor = null;

            if (_replacementDealFactory != null)
            {
                nextReadySuccessor = CreateReadySuccessor(
                    replacement, mode, betCents);
            }

            int ticketsUsed = deal.CurrentTicketPosition;
            int ticketsRemaining = deal.Tickets.Length - ticketsUsed;

            double actualRtp = deal.TotalWagersCents > 0
                ? deal.TotalPayoutsCents * 100.0 /
                  deal.TotalWagersCents
                : 0.0;

            var audit = new DealRetirementRecord
            {
                AuditId = Guid.NewGuid().ToString("N"),
                RetiredDealId = deal.DealId,
                ReplacementDealId = replacement.DealId,
                Reason = reason.Trim(),
                RetiredAtUtc = DateTime.UtcNow.ToString("O"),
                OldConfigVersion = deal.ConfigVersion,
                NewConfigVersion = replacement.ConfigVersion,
                Mode = deal.Mode,
                BetCents = deal.BetCents,
                TicketsUsed = ticketsUsed,
                TicketsRemaining = ticketsRemaining,
                TotalWagersCents = deal.TotalWagersCents,
                TotalPayoutsCents = deal.TotalPayoutsCents,
                ActualRtpPercent = actualRtp,
                ReachedTargetRtp = false
            };

            DealState oldState = deal.State;

            deal.State = DealState.Retired;
            _activeDeals.Remove(key);
            _readyDeals.Remove(key);

            replacement.State = DealState.Active;
            _activeDeals.Add(key, replacement);

            if (nextReadySuccessor != null)
            {
                _dealsById.Add(
                    nextReadySuccessor.DealId, nextReadySuccessor);

                _readyDeals.Add(key, nextReadySuccessor);
            }

            _retirementHistory.Add(audit);

            try
            {
                SaveCurrentState();

                if (_persistence != null)
                    _persistence.SaveRetirementHistory(_retirementHistory);
            }
            catch
            {
                _retirementHistory.Remove(audit);

                if (nextReadySuccessor != null)
                {
                    _dealsById.Remove(nextReadySuccessor.DealId);
                    _readyDeals.Remove(key);
                }

                _activeDeals.Remove(key);
                replacement.State = DealState.Ready;
                _readyDeals[key] = replacement;

                deal.State = oldState;
                _activeDeals[key] = deal;

                throw;
            }

            Debug.Log(
                $"[Deal Retired] Deal: {deal.DealId} | " +
                $"Replacement: {replacement.DealId} | " +
                $"Used: {ticketsUsed} | Remaining: {ticketsRemaining} | " +
                $"Reason: {reason}");

            return true;
        }

        private DealRecord CreateReadySuccessor(
            DealRecord currentDeal,
            GameMode mode,
            long betCents)
        {
            if (_replacementDealFactory == null)
                throw new InvalidOperationException(
                    "Ready successor factory is not configured.");

            DealRecord ready =
                _replacementDealFactory(mode, betCents);

            if (ready == null)
                throw new InvalidOperationException(
                    "Ready successor factory returned null.");

            if (ready.State == DealState.Active)
                ready.State = DealState.Ready;

            if (ready.State != DealState.Ready ||
                ready.Mode != mode ||
                ready.BetCents != betCents ||
                string.IsNullOrWhiteSpace(ready.DealId) ||
                (currentDeal != null &&
                 ready.DealId == currentDeal.DealId) ||
                _dealsById.ContainsKey(ready.DealId) ||
                ready.CurrentTicketPosition != 0 ||
                ready.Tickets == null ||
                ready.Tickets.Length == 0)
            {
                throw new InvalidOperationException(
                    "Ready successor deal is invalid.");
            }

            DealValidator.ValidateStructure(ready);
            return ready;
        }

        private void EnsureReadySuccessorsForActiveDeals()
        {
            if (_replacementDealFactory == null)
                return;

            bool changed = false;

            List<KeyValuePair<string, DealRecord>> active =
                new List<KeyValuePair<string, DealRecord>>(_activeDeals);

            foreach (KeyValuePair<string, DealRecord> pair in active)
            {
                if (_readyDeals.ContainsKey(pair.Key))
                    continue;

                DealRecord ready = CreateReadySuccessor(
                    pair.Value,
                    pair.Value.Mode,
                    pair.Value.BetCents);

                _dealsById.Add(ready.DealId, ready);
                _readyDeals.Add(pair.Key, ready);
                changed = true;
            }

            if (changed)
                SaveCurrentState();
        }

        public IReadOnlyList<DealRetirementRecord> GetRetirementHistory()
        {
            return _retirementHistory.AsReadOnly();
        }

        public IReadOnlyList<DealRecord> GetCompletedDealHistory()
        {
            var completedDeals = new List<DealRecord>();

            foreach (DealRecord deal in _dealsById.Values)
            {
                if (deal.State == DealState.Completed)
                    completedDeals.Add(deal);
            }

            return completedDeals.AsReadOnly();
        }


        public double GetActualRtpPercent(string dealId)
        {
            DealRecord deal = GetDealById(dealId);

            if (deal == null)
                throw new ArgumentException("Deal not found.", nameof(dealId));

            if (deal.TotalWagersCents <= 0)
                return 0.0;

            return deal.TotalPayoutsCents * 100.0 /
                   deal.TotalWagersCents;
        }


        public DealAuditReport GetDealAuditReport(string dealId)
        {
            DealRecord deal = GetDealById(dealId);

            if (deal == null)
                throw new ArgumentException(
                    "Deal not found.", nameof(dealId));

            int totalTickets = deal.Tickets == null
                ? 0
                : deal.Tickets.Length;

            int ticketsUsed = deal.CurrentTicketPosition;
            int ticketsRemaining = totalTickets - ticketsUsed;

            return new DealAuditReport
            {
                DealId = deal.DealId,
                ConfigVersion = deal.ConfigVersion,
                Mode = deal.Mode,
                BetCents = deal.BetCents,

                TotalTickets = totalTickets,
                TicketsUsed = ticketsUsed,
                TicketsRemaining = ticketsRemaining,

                TotalWagersCents = deal.TotalWagersCents,
                TotalPayoutsCents = deal.TotalPayoutsCents,

                ActualRtpPercent = deal.TotalWagersCents > 0
                    ? deal.TotalPayoutsCents * 100.0 /
                      deal.TotalWagersCents
                    : 0.0
            };
        }


        public bool MarkResultShown(string spinId)
        {
            if (string.IsNullOrWhiteSpace(spinId))
                return false;

            SpinRecord record = GetSpinById(spinId);

            if (record == null || record.ResultShown)
                return false;

            if (_persistence != null)
                _persistence.MarkResultShown(spinId);

            record.ResultShown = true;
            return true;
        }

        public SpinRecord GetSpinById(string spinId)
        {
            return string.IsNullOrWhiteSpace(spinId)
                ? null
                : _spinHistory.Find(x => x.SpinId == spinId);
        }

        public SpinRecord GetPendingResult(string playerId)
        {
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
            if (!_players.TryGetValue(
                    playerId, out PlayerRecord player))
            {
                throw new KeyNotFoundException("Player was not found.");
            }

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

        private static string GetDealKey(
            GameMode mode, long betCents)
        {
            return $"{mode}:{betCents}";
        }
    }

    [Serializable]
    public sealed class DealAuditReport
    {
        public string DealId;
        public string ConfigVersion;
        public GameMode Mode;
        public long BetCents;

        public int TotalTickets;
        public int TicketsUsed;
        public int TicketsRemaining;

        public long TotalWagersCents;
        public long TotalPayoutsCents;

        public double ActualRtpPercent;
    }
}