using System;
using NUnit.Framework;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.Tests
{
    public class DealCalculatorTests
    {
        [Test]
        public void Calculate_ComputesTotalWagersCorrectly()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 1080, 12);

            Assert.AreEqual(1200, result.TotalWagersCents);
            Assert.AreEqual(1080, result.TotalPayoutsCents);
            Assert.AreEqual(12, result.TotalTickets);
            Assert.AreEqual(10, result.PaidSpins);
            Assert.AreEqual(2, result.LosingTickets);
            Assert.IsTrue(result.HasExactly90PercentRtp);
        }

        [Test]
        public void Calculate_ComputesLosingTickets()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 8, 720, 10);

            Assert.AreEqual(2, result.LosingTickets);
        }

        [Test]
        public void Calculate_ComputesActualRtpPercent()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 900, 10);

            Assert.AreEqual(90m, result.ActualRtpPercent);
        }

        [Test]
        public void Calculate_ComputesNonExactRtpPercent()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 899, 10);

            Assert.AreEqual(89.9m, result.ActualRtpPercent);
        }

        [Test]
        public void Calculate_IdentifiesExactly90PercentRtp()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 900, 10);

            Assert.IsTrue(result.HasExactly90PercentRtp);
        }

        [Test]
        public void Calculate_RejectsZeroBet()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealCalculator.Calculate(0, 10, 900, 10));
        }

        [Test]
        public void Calculate_RejectsNegativeBet()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealCalculator.Calculate(-100, 10, 900, 10));
        }

        [Test]
        public void Calculate_RejectsFewerTicketsThanPaidSpins()
        {
            Assert.Throws<ArgumentException>(() =>
                DealCalculator.Calculate(100, 10, 900, 5));
        }

        [Test]
        public void Calculate_RejectsNegativePayout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealCalculator.Calculate(100, 10, -1, 10));
        }

        [Test]
        public void Calculate_RejectsZeroPaidSpins()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealCalculator.Calculate(100, 0, 0, 10));
        }

        [Test]
        public void Calculate_RejectsNegativePaidSpins()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealCalculator.Calculate(100, -1, 0, 10));
        }

        [Test]
        public void Calculate_RejectsNegativeTicketCount()
        {
            Assert.Throws<ArgumentException>(() =>
                DealCalculator.Calculate(100, 1, 90, -1));
        }

        [Test]
        public void Calculate_IdentifiesPayoutThatIsNotExactly90Percent()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 899, 10);

            Assert.IsFalse(result.HasExactly90PercentRtp);
        }

        [Test]
        public void Calculate_RejectsWagerOverflow()
        {
            Assert.Throws<OverflowException>(() =>
                DealCalculator.Calculate(long.MaxValue, 2, 0, 2));
        }

        [Test]
        public void CalculateTotalPayouts_SumsAwards()
        {
            long total = DealCalculator.CalculateTotalPayouts(
                new long[] { 0, 100, 250, 0 });

            Assert.AreEqual(350, total);
        }

        [Test]
        public void CalculateTotalPayouts_RejectsNullArray()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DealCalculator.CalculateTotalPayouts(null));
        }

        [Test]
        public void CalculateTotalPayouts_RejectsNegativeAward()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealCalculator.CalculateTotalPayouts(
                    new long[] { 100, -1 }));
        }

        [Test]
        public void CalculateTotalPayouts_RejectsOverflow()
        {
            Assert.Throws<OverflowException>(() =>
                DealCalculator.CalculateTotalPayouts(
                    new long[] { long.MaxValue, 1 }));
        }

        [Test]
        public void ValidateExact90Percent_AcceptsExactRtp()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 900, 10);

            Assert.DoesNotThrow(() =>
                DealCalculator.ValidateExact90Percent(result));
        }

        [Test]
        public void ValidateExact90Percent_RejectsNonExactRtp()
        {
            DealCalculation result =
                DealCalculator.Calculate(100, 10, 899, 10);

            Assert.Throws<InvalidOperationException>(() =>
                DealCalculator.ValidateExact90Percent(result));
        }
    }
}