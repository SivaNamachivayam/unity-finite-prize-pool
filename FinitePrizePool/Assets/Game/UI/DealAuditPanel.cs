using UnityEngine;
using TMPro;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.UI
{
    public sealed class DealAuditPanel : MonoBehaviour
    {
        [Header("Audit Heading")] [SerializeField]
        private TMP_Text headingText;

        [Header("Deal Information")] [SerializeField]
        private TMP_Text dealIdText;

        [SerializeField] private TMP_Text configVersionText;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text betAmountText;

        [Header("Ticket Information")] [SerializeField]
        private TMP_Text totalTicketsText;

        [SerializeField] private TMP_Text ticketsUsedText;
        [SerializeField] private TMP_Text ticketsRemainingText;

        [Header("Financial Information")] [SerializeField]
        private TMP_Text totalWagersText;

        [SerializeField] private TMP_Text totalPayoutsText;
        [SerializeField] private TMP_Text actualRtpText;

        private DealService dealService;

        public void Initialize(DealService service)
        {
            dealService = service;

            if (headingText != null)
                headingText.text = "DEAL AUDIT";

            RefreshAudit();
        }

        public void RefreshAudit(
            DealService service,
            GameMode mode,
            long betCents)
        {
            dealService = service;
            RefreshAudit(mode, betCents);
        }

        public void RefreshAudit()
        {
            RefreshAudit(GameMode.Inner, 100);
        }

        private void RefreshAudit(GameMode mode, long betCents)
        {
            if (headingText != null)
                headingText.text = "DEAL AUDIT";

            if (dealService == null)
            {
                ShowMessage("Service not initialized");
                return;
            }

            DealRecord deal =
                dealService.GetActiveDeal(mode, betCents);

            if (deal == null)
            {
                ShowMessage("No active deal found");
                return;
            }

            DealAuditReport report =
                dealService.GetDealAuditReport(deal.DealId);

            SetText(dealIdText, ShortenDealId(report.DealId));
            SetText(configVersionText, report.ConfigVersion);
            SetText(modeText, report.Mode.ToString());
            SetText(betAmountText, FormatMoney(report.BetCents));

            SetText(totalTicketsText, report.TotalTickets.ToString());
            SetText(ticketsUsedText, report.TicketsUsed.ToString());
            SetText(
                ticketsRemainingText,
                report.TicketsRemaining.ToString());

            SetText(
                totalWagersText,
                FormatMoney(report.TotalWagersCents));

            SetText(
                totalPayoutsText,
                FormatMoney(report.TotalPayoutsCents));

            SetText(
                actualRtpText,
                $"{report.ActualRtpPercent:F2}%");
        }

        private void ShowMessage(string message)
        {
            SetText(dealIdText, message);
            SetText(configVersionText, "--");
            SetText(modeText, "--");
            SetText(betAmountText, "--");
            SetText(totalTicketsText, "--");
            SetText(ticketsUsedText, "--");
            SetText(ticketsRemainingText, "--");
            SetText(totalWagersText, "--");
            SetText(totalPayoutsText, "--");
            SetText(actualRtpText, "--");
        }

        private static void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
                target.text = value;
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100.0:F2}";
        }

        private static string ShortenDealId(string dealId)
        {
            return string.IsNullOrEmpty(dealId) ? "--" : dealId;
        }
    }
}