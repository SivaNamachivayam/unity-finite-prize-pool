using System;
using System.Collections.Generic;

namespace FinitePrizePool.DealEngine
{
    public interface IDealService
    {
        SpinRecord Spin(
            string playerId,
            GameMode mode,
            long betCents);

        DealRecord GetActiveDeal(
            GameMode mode,
            long betCents);

        DealRecord GetDealById(string dealId);

        long GetPlayerBalance(string playerId);

        bool RetireActiveDeal(
            GameMode mode,
            long betCents,
            string reason);

        IReadOnlyList<DealRetirementRecord>
            GetRetirementHistory();

        IReadOnlyList<DealRecord>
            GetCompletedDealHistory();
    }

    public sealed class SpinRejectedException : Exception
    {
        public SpinRejectedException(string message)
            : base(message)
        {
        }

        public SpinRejectedException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }
    }
}