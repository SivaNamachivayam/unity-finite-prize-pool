using System;

namespace FinitePrizePool.DealEngine
{
    public interface IDealService
    {
        SpinRecord Spin(
            string playerId,
            GameMode mode,
            long betCents
        );

        DealRecord GetActiveDeal(
            GameMode mode,
            long betCents
        );

        long GetPlayerBalance(string playerId);
    }

    public sealed class SpinRejectedException : Exception
    {
        public SpinRejectedException(string message)
            : base(message)
        {
        }
    }
}