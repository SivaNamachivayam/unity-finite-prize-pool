using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.EditorTools
{
    public class DealRtpReportWindow : EditorWindow
    {
        private GameConfig config;
        private DealRtpReport.Report report;
        private string status = "Select your GameConfig asset.";

        [MenuItem("Finite Prize Pool/RTP Report")]
        public static void Open()
        {
            GetWindow<DealRtpReportWindow>("RTP Report");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Finite Prize Pool - RTP Report",
                EditorStyles.boldLabel);

            config = (GameConfig)EditorGUILayout.ObjectField(
                "Game Config",
                config,
                typeof(GameConfig),
                false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate 40-Deal RTP Report"))
            {
                GenerateReport();
            }

            EditorGUILayout.HelpBox(status, MessageType.Info);

            if (report == null)
                return;

            EditorGUILayout.LabelField(
                "Total Deals", report.TotalDeals.ToString());

            EditorGUILayout.LabelField(
                "Passed", report.PassedDeals.ToString());

            EditorGUILayout.LabelField(
                "Failed", report.FailedDeals.ToString());

            EditorGUILayout.LabelField(
                "Overall RTP",
                report.OverallRtpPercent.ToString("F4") + "%");

            EditorGUILayout.LabelField(
                "Overall Exactly 90%",
                report.OverallExactly90Percent ? "YES" : "NO");

            EditorGUILayout.Space();

            if (GUILayout.Button("Export CSV Report"))
            {
                ExportCsv();
            }
        }

        private void GenerateReport()
        {
            report = null;

            if (config == null)
            {
                status = "ERROR: Select the GameConfig asset.";
                return;
            }

            try
            {
                if (!config.Validate(out string error))
                {
                    throw new InvalidOperationException(error);
                }

                if (!config.HasConfiguredPrizeBags())
                {
                    throw new InvalidOperationException(
                        "Configure both Regular and Big prize bags.");
                }

                long[] regular = config.RegularBagPayoutsCents;
                long[] big = config.BigBagPayoutsCents;
                long[] bets = config.BetLevelsCents;

                long payoutPerCycle = 0;

                foreach (long payout in regular)
                    payoutPerCycle = checked(payoutPerCycle + payout);

                foreach (long payout in big)
                    payoutPerCycle = checked(payoutPerCycle + payout);

                int prizesPerCycle = regular.Length + big.Length;

                if (payoutPerCycle <= 0 || prizesPerCycle <= 0)
                {
                    throw new InvalidOperationException(
                        "Prize bag totals must be greater than zero.");
                }

                List<DealRecord> deals = new List<DealRecord>();

                foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
                {
                    foreach (long bet in bets)
                    {
                        long numerator = checked(10L * payoutPerCycle);
                        long denominator = checked(9L * bet);
                        long gcd = Gcd(numerator, denominator);

                        long cyclesLong = denominator / gcd;
                        long ticketsLong = numerator / gcd;

                        if (cyclesLong > int.MaxValue ||
                            ticketsLong > int.MaxValue)
                        {
                            throw new InvalidOperationException(
                                "Deal size is too large for bet " + bet + ".");
                        }

                        int cycles = (int)cyclesLong;
                        int totalTickets = (int)ticketsLong;

                        long lossesLong =
                            (long)totalTickets -
                            (long)cycles * prizesPerCycle;

                        if (lossesLong < 0 || lossesLong > int.MaxValue)
                        {
                            throw new InvalidOperationException(
                                "Invalid loss-ticket count for bet " + bet + ".");
                        }

                        string dealId = mode + "_" + bet;

                        DealRecord deal = DealBuilder.GenerateDeal(
                            dealId,
                            config,
                            mode,
                            bet,
                            cycles,
                            (int)lossesLong);

                        deals.Add(deal);
                    }
                }

                report = DealRtpReport.Generate(deals);

                status = "Report generated. " +
                         report.PassedDeals + " passed; " +
                         report.FailedDeals + " failed.";

                Repaint();
            }
            catch (Exception ex)
            {
                status = "ERROR: " + ex.Message;
                Debug.LogException(ex);
            }
        }

        private void ExportCsv()
        {
            if (report == null)
            {
                status = "Generate the report first.";
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Save RTP Report",
                "",
                "FinitePrizePool_RTP_Report.csv",
                "csv");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                File.WriteAllText(path, report.ToCsv());
                status = "CSV saved: " + path;
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                status = "Export failed: " + ex.Message;
                Debug.LogException(ex);
            }
        }

        private static long Gcd(long a, long b)
        {
            while (b != 0)
            {
                long remainder = a % b;
                a = b;
                b = remainder;
            }

            return Math.Abs(a);
        }
    }
}