using System;

namespace FinitePrizePool.DealEngine
{
    public enum GameMode
    {
        Inner,
        Outer
    }

    public enum PrizeType
    {
        Loss,
        Regular,
        Big,
        Bonus,
        Multiplier
    }

    public enum DealState
    {
        Ready,
        Active,
        Completed,
        Retired
    }

    [Serializable]
    public sealed class PrizeTicket
    {
        public string TicketId;
        public PrizeType Type;
        public long AwardCents;
        public int CycleNumber;
    }

    [Serializable]
    public sealed class DealRecord
    {
        public string DealId;
        public string ConfigVersion;
        public GameMode Mode;
        public long BetCents;
        public DealState State;

        public PrizeTicket[] Tickets;
        public int CurrentTicketPosition;

        public long TotalWagersCents;
        public long TotalPayoutsCents;
    }

    [Serializable]
    public sealed class PlayerRecord
    {
        public string PlayerId;
        public string DisplayName;
        public long BalanceCents;
    }

    [Serializable]
    public sealed class SpinRecord
    {
        public string SpinId;
        public string DealId;
        public string PlayerId;

        public int TicketPosition;
        public long WagerCents;
        public long AwardCents;

        public bool ResultShown;
    }

    [Serializable]
    public sealed class DealRetirementRecord
    {
        public string AuditId;
        public string RetiredDealId;
        public string ReplacementDealId;

        public string Reason;
        public string RetiredAtUtc;

        public string OldConfigVersion;
        public string NewConfigVersion;

        public GameMode Mode;
        public long BetCents;

        public int TicketsUsed;
        public int TicketsRemaining;

        public long TotalWagersCents;
        public long TotalPayoutsCents;

        public double ActualRtpPercent;
        public bool ReachedTargetRtp;
    }
}