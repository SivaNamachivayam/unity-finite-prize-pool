using System;
using System.Reflection;
using NUnit.Framework;
using FinitePrizePool.DealEngine;
using UnityEngine;

public class DealBuilderTests
{
    [Test]
    public void CreateDealShell_CreatesReadyDeal()
    {
        DealRecord deal = DealBuilder.CreateDealShell(
            "deal-001", "1.0.0", GameMode.Inner, 100);

        Assert.IsNotNull(deal);
        Assert.AreEqual("deal-001", deal.DealId);
        Assert.AreEqual("1.0.0", deal.ConfigVersion);
        Assert.AreEqual(GameMode.Inner, deal.Mode);
        Assert.AreEqual(100, deal.BetCents);
        Assert.AreEqual(DealState.Ready, deal.State);
        Assert.AreEqual(0, deal.CurrentTicketPosition);
        Assert.AreEqual(0, deal.TotalWagersCents);
        Assert.AreEqual(0, deal.TotalPayoutsCents);
        Assert.IsNotNull(deal.Tickets);
        Assert.AreEqual(0, deal.Tickets.Length);
    }

    [Test]
    public void CreateDealShell_RejectsEmptyDealId()
    {
        Assert.Throws<ArgumentException>(() =>
            DealBuilder.CreateDealShell(
                "", "1.0.0", GameMode.Inner, 100));
    }

    [Test]
    public void CreateDealShell_RejectsInvalidBet()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DealBuilder.CreateDealShell(
                "deal-001", "1.0.0", GameMode.Inner, 0));
    }

    [Test]
    public void ActivateDeal_RejectsNullDeal()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DealBuilder.ActivateDeal(null));
    }

    [Test]
    public void ActivateDeal_RejectsDealWithoutTickets()
    {
        DealRecord deal = DealBuilder.CreateDealShell(
            "deal-002", "1.0.0", GameMode.Inner, 100);

        Assert.Throws<InvalidOperationException>(() =>
            DealBuilder.ActivateDeal(deal));
    }


    [Test]
    public void ActivateDeal_ActivatesReadyDealWithOneLossTicket()
    {
        DealRecord deal = DealBuilder.CreateDealShell(
            "deal-003", "1.0.0", GameMode.Inner, 100);

        deal.Tickets = new[]
        {
            CreateTicket("ticket-001", PrizeType.Regular, 135, 1),
            CreateTicket("ticket-002", PrizeType.Big, 135, 1),
            CreateTicket("ticket-003", PrizeType.Loss, 0, 1)
        };

        DealBuilder.ActivateDeal(deal);

        Assert.AreEqual(DealState.Active, deal.State);
        Assert.AreEqual(0, deal.CurrentTicketPosition);
    }


    [Test]
    public void SecureShuffle_RejectsNullArray()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DealBuilder.SecureShuffle<int>(null));
    }

    [Test]
    public void SecureShuffle_PreservesEveryItem()
    {
        int[] items = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int[] expected = (int[])items.Clone();

        DealBuilder.SecureShuffle(items);

        CollectionAssert.AreEquivalent(expected, items);
    }

    [Test]
    public void SecureShuffle_HandlesSingleItem()
    {
        int[] items = { 42 };

        DealBuilder.SecureShuffle(items);

        Assert.AreEqual(1, items.Length);
        Assert.AreEqual(42, items[0]);
    }

    [Test]
    public void SecureShuffle_HandlesEmptyArray()
    {
        int[] items = Array.Empty<int>();

        Assert.DoesNotThrow(() =>
            DealBuilder.SecureShuffle(items));

        Assert.AreEqual(0, items.Length);
    }

    [Test]
    public void ValidateGenerationConfig_RejectsNullConfig()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DealBuilder.ValidateGenerationConfig(null));
    }

    [Test]
    public void ValidateGenerationConfig_RejectsUnconfiguredPrizeBags()
    {
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                DealBuilder.ValidateGenerationConfig(config));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void ValidateGenerationConfig_RejectsInvalidConfig()
    {
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                DealBuilder.ValidateGenerationConfig(config));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void DealValidator_RejectsDuplicateTicketIds()
    {
        DealRecord deal = DealBuilder.CreateDealShell(
            "deal-004", "1.0.0", GameMode.Inner, 100);

        deal.Tickets = new[]
        {
            CreateTicket("duplicate", PrizeType.Loss, 0, 1),
            CreateTicket("duplicate", PrizeType.Regular, 100, 1)
        };

        Assert.Throws<InvalidOperationException>(() =>
            DealValidator.ValidateStructure(deal));
    }

    [Test]
    public void TicketPlacementValidator_RejectsNullTickets()
    {
        string error;

        Assert.IsFalse(
            TicketPlacementValidator.Validate(null, out error));

        Assert.IsNotEmpty(error);
    }

    [Test]
    public void TicketPlacementValidator_RejectsEmptyTickets()
    {
        string error;

        Assert.IsFalse(
            TicketPlacementValidator.Validate(
                Array.Empty<PrizeTicket>(), out error));

        Assert.IsNotEmpty(error);
    }

    [Test]
    public void TicketPlacementValidator_AcceptsValidTicketSequence()
    {
        PrizeTicket[] tickets =
        {
            CreateTicket("t1", PrizeType.Regular, 100, 1),
            CreateTicket("t2", PrizeType.Big, 200, 1),
            CreateTicket("t3", PrizeType.Loss, 0, 1),
            CreateTicket("t4", PrizeType.Regular, 100, 1),
            CreateTicket("t5", PrizeType.Big, 200, 1),
            CreateTicket("t6", PrizeType.Regular, 100, 1)
        };

        string error;

        Assert.IsTrue(
            TicketPlacementValidator.Validate(tickets, out error),
            error);
    }

    [Test]
    public void GenerateDeal_CreatesDealWithCorrectTicketCounts()
    {
        GameConfig config = CreateTestConfig();

        try
        {
            DealRecord deal = DealBuilder.GenerateDeal(
                "generated-001",
                config,
                GameMode.Inner,
                100,
                1,
                0);

            Assert.AreEqual(DealState.Ready, deal.State);
            Assert.AreEqual(4, deal.Tickets.Length);
            Assert.AreEqual(2, CountType(deal, PrizeType.Regular));
            Assert.AreEqual(2, CountType(deal, PrizeType.Big));
            Assert.AreEqual(0, CountType(deal, PrizeType.Loss));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void GenerateDeal_ProducesExactly90PercentRtpForTestConfig()
    {
        GameConfig config = CreateTestConfig();

        try
        {
            DealRecord deal = DealBuilder.GenerateDeal(
                "generated-002",
                config,
                GameMode.Inner,
                100,
                1,
                0);

            long totalPayout = 0;

            foreach (PrizeTicket ticket in deal.Tickets)
            {
                totalPayout = checked(
                    totalPayout + ticket.AwardCents);
            }

            long totalWager = checked(
                (long)deal.Tickets.Length * deal.BetCents);

            Assert.AreEqual(360L, totalPayout);
            Assert.AreEqual(400L, totalWager);
            Assert.AreEqual(
                90m,
                (decimal)totalPayout * 100m / totalWager);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void GenerateDeal_PreservesCycleOrderForEachPrizeType()
    {
        GameConfig config = CreateTestConfig();

        try
        {
            DealRecord deal = DealBuilder.GenerateDeal(
                "generated-003",
                config,
                GameMode.Inner,
                100,
                2,
                0);

            AssertCycleOrder(deal, PrizeType.Regular, 2);
            AssertCycleOrder(deal, PrizeType.Big, 2);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void GenerateDeal_RejectsZeroPairedCycles()
    {
        GameConfig config = CreateTestConfig();

        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealBuilder.GenerateDeal(
                    "generated-004",
                    config,
                    GameMode.Inner,
                    100,
                    0,
                    0));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void GenerateDeal_RejectsNegativeLossCount()
    {
        GameConfig config = CreateTestConfig();

        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DealBuilder.GenerateDeal(
                    "generated-005",
                    config,
                    GameMode.Inner,
                    100,
                    1,
                    -1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    private static GameConfig CreateTestConfig()
    {
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

        SetPrivateField(
            config,
            "regularBagPayoutsCents",
            new long[] { 40, 140 });

        SetPrivateField(
            config,
            "bigBagPayoutsCents",
            new long[] { 90, 90 });

        return config;
    }

    private static void SetPrivateField<T>(
        GameConfig config,
        string fieldName,
        T value)
    {
        FieldInfo field = typeof(GameConfig).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            Assert.Fail("GameConfig field not found: " + fieldName);
            return;
        }

        field.SetValue(config, value);
    }

    private static PrizeTicket CreateTicket(
        string id,
        PrizeType type,
        long awardCents,
        int cycle)
    {
        return new PrizeTicket
        {
            TicketId = id,
            Type = type,
            AwardCents = awardCents,
            CycleNumber = cycle
        };
    }

    private static int CountType(
        DealRecord deal,
        PrizeType type)
    {
        int count = 0;

        foreach (PrizeTicket ticket in deal.Tickets)
        {
            if (ticket.Type == type)
                count++;
        }

        return count;
    }

    private static void AssertCycleOrder(
        DealRecord deal,
        PrizeType type,
        int expectedCycles)
    {
        int lastCycle = 0;
        int count = 0;

        foreach (PrizeTicket ticket in deal.Tickets)
        {
            if (ticket.Type != type)
                continue;

            Assert.GreaterOrEqual(
                ticket.CycleNumber,
                lastCycle,
                type + " cycle order is incorrect.");

            lastCycle = ticket.CycleNumber;
            count++;
        }

        Assert.AreEqual(expectedCycles * 2, count);
        Assert.AreEqual(expectedCycles, lastCycle);
    }
}