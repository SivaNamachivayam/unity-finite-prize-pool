using System.Collections.Generic;

namespace FinitePrizePool.DealEngine
{
    public interface IDealPersistence
    {
        void SaveState(
            IReadOnlyList<PlayerRecord> players,
            IReadOnlyList<DealRecord> deals,
            IReadOnlyList<SpinRecord> spinHistory);

        void LoadState(
            out List<PlayerRecord> players,
            out List<DealRecord> deals,
            out List<SpinRecord> spinHistory);

        void MarkResultShown(string spinId);

        void SaveRetirementHistory(
            IReadOnlyList<DealRetirementRecord> retirementHistory);

        List<DealRetirementRecord> LoadRetirementHistory();
    }
}