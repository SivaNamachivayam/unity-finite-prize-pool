using System;
using System.Collections.Generic;
using System.IO;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Persistence
{
    public sealed class JsonDealPersistence : IDealPersistence
    {
        private readonly JsonSaveStorage _storage;
        private GameSaveData _lastSavedData;

        public JsonDealPersistence()
        {
            _storage = new JsonSaveStorage();
        }

        public JsonDealPersistence(string savePath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException(
                    "Save path is required.", nameof(savePath));

            _storage = new JsonSaveStorage(savePath);
        }

        public JsonDealPersistence(JsonSaveStorage storage)
        {
            _storage = storage ??
                       throw new ArgumentNullException(nameof(storage));
        }

        public void SaveState(
            IReadOnlyList<PlayerRecord> players,
            IReadOnlyList<DealRecord> deals,
            IReadOnlyList<SpinRecord> spinHistory)
        {
            if (players == null || deals == null || spinHistory == null)
                throw new ArgumentNullException(
                    "Save collections cannot be null.");

            GameSaveData data =
                _lastSavedData ?? _storage.Load() ?? new GameSaveData();

            if (data.Players == null)
                data.Players = new List<PlayerSaveData>();

            if (data.Deals == null)
                data.Deals = new List<DealSaveData>();

            if (data.SpinHistory == null)
                data.SpinHistory = new List<SpinSaveData>();

            if (data.RetirementHistory == null)
                data.RetirementHistory = new List<DealRetirementSaveData>();

            data.Players.Clear();
            data.Deals.Clear();
            data.SpinHistory.Clear();

            foreach (PlayerRecord player in players)
            {
                if (player == null)
                    throw new InvalidDataException("Null player.");

                data.Players.Add(new PlayerSaveData
                {
                    PlayerId = player.PlayerId,
                    DisplayName = player.DisplayName,
                    BalanceCents = player.BalanceCents
                });
            }

            foreach (DealRecord deal in deals)
            {
                if (deal == null)
                    throw new InvalidDataException("Null deal.");

                var savedDeal = new DealSaveData
                {
                    DealId = deal.DealId,
                    ConfigVersion = deal.ConfigVersion,
                    Mode = deal.Mode,
                    BetCents = deal.BetCents,
                    State = deal.State,
                    CurrentTicketPosition = deal.CurrentTicketPosition,
                    TotalWagersCents = deal.TotalWagersCents,
                    TotalPayoutsCents = deal.TotalPayoutsCents
                };

                if (deal.Tickets != null)
                {
                    foreach (PrizeTicket ticket in deal.Tickets)
                    {
                        if (ticket == null)
                            throw new InvalidDataException("Null ticket.");

                        savedDeal.Tickets.Add(new TicketSaveData
                        {
                            TicketId = ticket.TicketId,
                            Type = ticket.Type,
                            AwardCents = ticket.AwardCents,
                            CycleNumber = ticket.CycleNumber
                        });
                    }
                }

                data.Deals.Add(savedDeal);
            }

            foreach (SpinRecord spin in spinHistory)
            {
                if (spin == null)
                    throw new InvalidDataException("Null spin.");

                data.SpinHistory.Add(new SpinSaveData
                {
                    SpinId = spin.SpinId,
                    DealId = spin.DealId,
                    PlayerId = spin.PlayerId,
                    TicketPosition = spin.TicketPosition,
                    WagerCents = spin.WagerCents,
                    AwardCents = spin.AwardCents,
                    ResultShown = spin.ResultShown
                });
            }

            _storage.Save(data);
            _lastSavedData = data;
        }

        public void LoadState(
            out List<PlayerRecord> players,
            out List<DealRecord> deals,
            out List<SpinRecord> spinHistory)
        {
            players = new List<PlayerRecord>();
            deals = new List<DealRecord>();
            spinHistory = new List<SpinRecord>();

            GameSaveData data = _storage.Load();
            _lastSavedData = data;

            if (data == null)
                return;

            if (data.Players == null)
                data.Players = new List<PlayerSaveData>();

            if (data.Deals == null)
                data.Deals = new List<DealSaveData>();

            if (data.SpinHistory == null)
                data.SpinHistory = new List<SpinSaveData>();

            if (data.RetirementHistory == null)
                data.RetirementHistory = new List<DealRetirementSaveData>();

            foreach (PlayerSaveData player in data.Players)
            {
                if (player == null)
                    throw new InvalidDataException("Null saved player.");

                players.Add(new PlayerRecord
                {
                    PlayerId = player.PlayerId,
                    DisplayName = player.DisplayName,
                    BalanceCents = player.BalanceCents
                });
            }

            foreach (DealSaveData savedDeal in data.Deals)
            {
                if (savedDeal == null)
                    throw new InvalidDataException("Null saved deal.");

                var tickets = new List<PrizeTicket>();

                if (savedDeal.Tickets != null)
                {
                    foreach (TicketSaveData ticket in savedDeal.Tickets)
                    {
                        if (ticket == null)
                            throw new InvalidDataException("Null saved ticket.");

                        tickets.Add(new PrizeTicket
                        {
                            TicketId = ticket.TicketId,
                            Type = ticket.Type,
                            AwardCents = ticket.AwardCents,
                            CycleNumber = ticket.CycleNumber
                        });
                    }
                }

                deals.Add(new DealRecord
                {
                    DealId = savedDeal.DealId,
                    ConfigVersion = savedDeal.ConfigVersion,
                    Mode = savedDeal.Mode,
                    BetCents = savedDeal.BetCents,
                    State = savedDeal.State,
                    CurrentTicketPosition = savedDeal.CurrentTicketPosition,
                    TotalWagersCents = savedDeal.TotalWagersCents,
                    TotalPayoutsCents = savedDeal.TotalPayoutsCents,
                    Tickets = tickets.ToArray()
                });
            }

            foreach (SpinSaveData savedSpin in data.SpinHistory)
            {
                if (savedSpin == null)
                    throw new InvalidDataException("Null saved spin.");

                spinHistory.Add(new SpinRecord
                {
                    SpinId = savedSpin.SpinId,
                    DealId = savedSpin.DealId,
                    PlayerId = savedSpin.PlayerId,
                    TicketPosition = savedSpin.TicketPosition,
                    WagerCents = savedSpin.WagerCents,
                    AwardCents = savedSpin.AwardCents,
                    ResultShown = savedSpin.ResultShown
                });
            }
        }

        public void SaveRetirementHistory(
            IReadOnlyList<DealRetirementRecord> retirementHistory)
        {
            if (retirementHistory == null)
                throw new ArgumentNullException(nameof(retirementHistory));

            GameSaveData data =
                _lastSavedData ?? _storage.Load() ?? new GameSaveData();

            if (data.Players == null)
                data.Players = new List<PlayerSaveData>();

            if (data.Deals == null)
                data.Deals = new List<DealSaveData>();

            if (data.SpinHistory == null)
                data.SpinHistory = new List<SpinSaveData>();

            var savedHistory = new List<DealRetirementSaveData>();

            foreach (DealRetirementRecord record in retirementHistory)
            {
                if (record == null)
                    throw new InvalidDataException("Null retirement audit.");

                savedHistory.Add(new DealRetirementSaveData
                {
                    AuditId = record.AuditId,
                    RetiredDealId = record.RetiredDealId,
                    ReplacementDealId = record.ReplacementDealId,
                    Reason = record.Reason,
                    RetiredAtUtc = record.RetiredAtUtc,
                    OldConfigVersion = record.OldConfigVersion,
                    NewConfigVersion = record.NewConfigVersion,
                    Mode = record.Mode,
                    BetCents = record.BetCents,
                    TicketsUsed = record.TicketsUsed,
                    TicketsRemaining = record.TicketsRemaining,
                    TotalWagersCents = record.TotalWagersCents,
                    TotalPayoutsCents = record.TotalPayoutsCents,
                    ActualRtpPercent = record.ActualRtpPercent,
                    ReachedTargetRtp = record.ReachedTargetRtp
                });
            }

            data.RetirementHistory = savedHistory;
            _storage.Save(data);
            _lastSavedData = data;
        }

        public List<DealRetirementRecord> LoadRetirementHistory()
        {
            GameSaveData data = _lastSavedData ?? _storage.Load();
            var result = new List<DealRetirementRecord>();

            if (data == null || data.RetirementHistory == null)
                return result;

            foreach (DealRetirementSaveData saved in data.RetirementHistory)
            {
                if (saved == null)
                    continue;

                result.Add(new DealRetirementRecord
                {
                    AuditId = saved.AuditId,
                    RetiredDealId = saved.RetiredDealId,
                    ReplacementDealId = saved.ReplacementDealId,
                    Reason = saved.Reason,
                    RetiredAtUtc = saved.RetiredAtUtc,
                    OldConfigVersion = saved.OldConfigVersion,
                    NewConfigVersion = saved.NewConfigVersion,
                    Mode = saved.Mode,
                    BetCents = saved.BetCents,
                    TicketsUsed = saved.TicketsUsed,
                    TicketsRemaining = saved.TicketsRemaining,
                    TotalWagersCents = saved.TotalWagersCents,
                    TotalPayoutsCents = saved.TotalPayoutsCents,
                    ActualRtpPercent = saved.ActualRtpPercent,
                    ReachedTargetRtp = saved.ReachedTargetRtp
                });
            }

            return result;
        }

        public void MarkResultShown(string spinId)
        {
            if (string.IsNullOrWhiteSpace(spinId))
                throw new ArgumentException("Spin ID is required.");

            GameSaveData data = _lastSavedData ?? _storage.Load();

            if (data == null)
                throw new InvalidOperationException("No saved game exists.");

            if (data.SpinHistory == null)
                throw new InvalidDataException("Spin history is missing.");

            SpinSaveData target = data.SpinHistory.Find(spin => spin != null && spin.SpinId == spinId);

            if (target == null)
                throw new KeyNotFoundException(
                    "Spin record not found: " + spinId);

            target.ResultShown = true;
            _storage.Save(data);
            _lastSavedData = data;
        }
    }
}