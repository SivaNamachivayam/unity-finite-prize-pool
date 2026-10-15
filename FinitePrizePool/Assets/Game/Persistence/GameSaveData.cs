using System;
using System.Collections.Generic;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Persistence
{
    [Serializable]
    public class GameSaveData
    {
        public int SaveVersion = 1;

        public List<PlayerSaveData> Players =
            new List<PlayerSaveData>();

        public List<DealSaveData> Deals =
            new List<DealSaveData>();

        public List<SpinSaveData> SpinHistory =
            new List<SpinSaveData>();
    }

    [Serializable]
    public class PlayerSaveData
    {
        public string PlayerId;
        public string DisplayName;
        public long BalanceCents;
    }

    [Serializable]
    public class DealSaveData
    {
        public string DealId;
        public string ConfigVersion;
        public GameMode Mode;
        public long BetCents;
        public DealState State;
        public int CurrentTicketPosition;

        public long TotalWagersCents;
        public long TotalPayoutsCents;

        public List<TicketSaveData> Tickets =
            new List<TicketSaveData>();
    }

    [Serializable]
    public class TicketSaveData
    {
        public string TicketId;
        public PrizeType Type;
        public long AwardCents;
        public int CycleNumber;
    }

    [Serializable]
    public class SpinSaveData
    {
        public string SpinId;
        public string DealId;
        public string PlayerId;
        public int TicketPosition;
        public long WagerCents;
        public long AwardCents;
        public bool ResultShown;
    }
}