using System;
using System.Collections.Generic;
using System.Text;

namespace FinitePrizePool.DealEngine
{
    public sealed class DealRtpReport
    {
        public sealed class DealResult
        {
            public string DealId;
            public GameMode Mode;
            public long BetCents;
            public int TotalTickets;
            public long TotalWagersCents;
            public long TotalPayoutsCents;
            public decimal ActualRtpPercent;
            public bool Exactly90Percent;
            public string Error;
        }

        public sealed class Report
        {
            public int TotalDeals;
            public int PassedDeals;
            public int FailedDeals;
            public long TotalWagersCents;
            public long TotalPayoutsCents;
            public decimal OverallRtpPercent;
            public bool OverallExactly90Percent;

            public readonly List<DealResult> Results =
                new List<DealResult>();

            public string ToCsv()
            {
                var csv = new StringBuilder();

                csv.AppendLine(
                    "DealId,Mode,BetCents,Tickets,WagersCents," +
                    "PayoutsCents,ActualRTPPercent,Exactly90%,Error");

                foreach (DealResult r in Results)
                {
                    csv.AppendLine(
                        $"{r.DealId},{r.Mode},{r.BetCents}," +
                        $"{r.TotalTickets},{r.TotalWagersCents}," +
                        $"{r.TotalPayoutsCents},{r.ActualRtpPercent:F6}," +
                        $"{r.Exactly90Percent},{Escape(r.Error)}");
                }

                csv.AppendLine();
                csv.AppendLine(
                    "TotalDeals,PassedDeals,FailedDeals," +
                    "TotalWagersCents,TotalPayoutsCents," +
                    "OverallRTPPercent,OverallExactly90%");

                csv.AppendLine(
                    $"{TotalDeals},{PassedDeals},{FailedDeals}," +
                    $"{TotalWagersCents},{TotalPayoutsCents}," +
                    $"{OverallRtpPercent:F6},{OverallExactly90Percent}");

                return csv.ToString();
            }

            private static string Escape(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return "";

                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
        }

        public static Report Generate(IReadOnlyList<DealRecord> deals)
        {
            if (deals == null)
                throw new ArgumentNullException(nameof(deals));

            var report = new Report();
            report.TotalDeals = deals.Count;

            foreach (DealRecord deal in deals)
            {
                var result = new DealResult();

                try
                {
                    if (deal == null)
                        throw new InvalidOperationException(
                            "Deal is null.");

                    if (deal.Tickets == null ||
                        deal.Tickets.Length == 0)
                        throw new InvalidOperationException(
                            "Deal has no tickets.");

                    long payouts = 0;

                    foreach (PrizeTicket ticket in deal.Tickets)
                    {
                        if (ticket == null)
                            throw new InvalidOperationException(
                                "Deal contains a null ticket.");

                        if (ticket.AwardCents < 0)
                            throw new InvalidOperationException(
                                "Negative ticket payout.");

                        payouts = checked(payouts + ticket.AwardCents);
                    }

                    int ticketCount = deal.Tickets.Length;
                    long wagers = checked(
                        deal.BetCents * (long)ticketCount);

                    result.DealId = deal.DealId;
                    result.Mode = deal.Mode;
                    result.BetCents = deal.BetCents;
                    result.TotalTickets = ticketCount;
                    result.TotalWagersCents = wagers;
                    result.TotalPayoutsCents = payouts;

                    result.ActualRtpPercent = wagers > 0
                        ? (decimal)payouts * 100m / wagers
                        : 0m;

                    result.Exactly90Percent =
                        wagers > 0 &&
                        (decimal)payouts * 100m ==
                        (decimal)wagers * 90m;

                    report.TotalWagersCents =
                        checked(report.TotalWagersCents + wagers);

                    report.TotalPayoutsCents =
                        checked(report.TotalPayoutsCents + payouts);

                    if (result.Exactly90Percent)
                        report.PassedDeals++;
                    else
                    {
                        report.FailedDeals++;
                        result.Error = "Deal does not achieve exactly 90% RTP.";
                    }
                }
                catch (Exception ex)
                {
                    report.FailedDeals++;
                    result.Error = ex.Message;
                }

                report.Results.Add(result);
            }

            report.OverallRtpPercent =
                report.TotalWagersCents > 0
                    ? (decimal)report.TotalPayoutsCents * 100m /
                      report.TotalWagersCents
                    : 0m;

            report.OverallExactly90Percent =
                report.TotalWagersCents > 0 &&
                (decimal)report.TotalPayoutsCents * 100m ==
                (decimal)report.TotalWagersCents * 90m;

            return report;
        }
    }
}