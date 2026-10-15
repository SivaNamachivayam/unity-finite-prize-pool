
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

        public JsonDealPersistence(JsonSaveStorage storage = null)
        {
            _storage = storage ?? new JsonSaveStorage();
        }

        public void SaveState(
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

            GameSaveData data = new GameSaveData();

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

                DealSaveData savedDeal = new DealSaveData
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
                    throw new InvalidDataException("Null spin record.");

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

            if (data == null)
            {
                _lastSavedData = null;
                return;
            }

            if (data.Players == null ||
                data.Deals == null ||
                data.SpinHistory == null)
            {
                throw new InvalidDataException(
                    "Save data is missing required collections.");
            }

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

                List<PrizeTicket> tickets = new List<PrizeTicket>();

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

            _lastSavedData = data;
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

            SpinSaveData target = null;

            foreach (SpinSaveData spin in data.SpinHistory)
            {
                if (spin != null && spin.SpinId == spinId)
                {
                    target = spin;
                    break;
                }
            }

            if (target == null)
                throw new KeyNotFoundException(
                    "Spin record was not found: " + spinId);

            bool previousValue = target.ResultShown;
            target.ResultShown = true;

            try
            {
                _storage.Save(data);
                _lastSavedData = data;
            }
            catch
            {
                target.ResultShown = previousValue;
                throw;
            }
        }
    }
}
