using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.UI
{
    public sealed class FinitePrizePoolUI : MonoBehaviour
    {
        [Header("Dropdowns")] [SerializeField] private TMP_Dropdown modeDropdown;
        [SerializeField] private TMP_Dropdown betDropdown;
        [SerializeField] private TMP_Dropdown playerDropdown;

        [Header("Button")] [SerializeField] private Button spinButton;

        [Header("UI Text")] [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text statusText;

        [Header("Deal Audit")] [SerializeField]
        private DealAuditPanel dealAuditPanel;

        private GameMode selectedMode = GameMode.Inner;
        private long selectedBetCents = 100;

        private DealService _dealService;
        private string _playerId = "P1";

        private readonly long[] _betLevelsCents =
        {
            25, 50, 75, 100, 125,
            150, 175, 200, 225, 250,
            275, 300, 325, 350, 375,
            400, 425, 450, 475, 500
        };

        private void Awake()
        {
            SetupModeDropdown();
            SetupBetDropdown();
            SetupPlayerDropdown();

            if (modeDropdown != null)
                modeDropdown.onValueChanged.AddListener(OnModeChanged);

            if (betDropdown != null)
                betDropdown.onValueChanged.AddListener(OnBetChanged);

            if (playerDropdown != null)
                playerDropdown.onValueChanged.AddListener(OnPlayerChanged);

            if (spinButton != null)
                spinButton.onClick.AddListener(OnSpinClicked);

            if (resultText != null)
                resultText.text = "--";

            if (statusText != null)
                statusText.text = "Ready to spin";

            UpdateLabels();
        }

        private void OnDestroy()
        {
            if (modeDropdown != null)
                modeDropdown.onValueChanged.RemoveListener(OnModeChanged);

            if (betDropdown != null)
                betDropdown.onValueChanged.RemoveListener(OnBetChanged);

            if (playerDropdown != null)
                playerDropdown.onValueChanged.RemoveListener(OnPlayerChanged);

            if (spinButton != null)
                spinButton.onClick.RemoveListener(OnSpinClicked);
        }

        private void SetupModeDropdown()
        {
            if (modeDropdown == null)
                return;

            modeDropdown.ClearOptions();

            modeDropdown.AddOptions(new List<string>
            {
                "Inner",
                "Outer"
            });

            modeDropdown.SetValueWithoutNotify(0);
            selectedMode = GameMode.Inner;
        }

        private void SetupBetDropdown()
        {
            if (betDropdown == null)
                return;

            betDropdown.ClearOptions();

            var options = new List<string>();

            foreach (long betCents in _betLevelsCents)
                options.Add($"${betCents / 100.0:F2}");

            betDropdown.AddOptions(options);
            betDropdown.SetValueWithoutNotify(3);
            selectedBetCents = 100;
        }

        private void SetupPlayerDropdown()
        {
            if (playerDropdown == null)
                return;

            playerDropdown.ClearOptions();

            playerDropdown.AddOptions(new List<string>
            {
                "Player 1",
                "Player 2"
            });

            playerDropdown.SetValueWithoutNotify(0);
            _playerId = "P1";
        }

        public void Initialize(DealService service)
        {
            _dealService = service;

            if (resultText != null)
                resultText.text = "--";

            if (statusText != null)
                statusText.text = "Ready to spin";

            SetPlayer("P1");
            UpdateLabels();
            RefreshDealAudit();

            Debug.Log("[UI] Finite Prize Pool UI initialized.");
        }

        public void SetPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            _playerId = playerId;

            if (playerDropdown != null)
            {
                int index = playerId == "P2" ? 1 : 0;
                playerDropdown.SetValueWithoutNotify(index);
            }

            UpdateLabels();
            RefreshDealAudit();
        }

        private void OnModeChanged(int index)
        {
            selectedMode = index == 1
                ? GameMode.Outer
                : GameMode.Inner;

            SetStatus("Game mode changed");
            UpdateLabels();
            RefreshDealAudit();
        }

        private void OnBetChanged(int index)
        {
            if (index < 0 || index >= _betLevelsCents.Length)
                return;

            selectedBetCents = _betLevelsCents[index];

            SetStatus("Bet amount changed");
            UpdateLabels();
            RefreshDealAudit();
        }

        private void OnPlayerChanged(int index)
        {
            _playerId = index == 1 ? "P2" : "P1";

            UpdateLabels();
            RefreshDealAudit();
        }

        private void OnSpinClicked()
        {
            if (_dealService == null)
            {
                SetStatus("Game service is not initialized");
                Debug.LogError("[UI] DealService is not initialized.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_playerId))
            {
                SetStatus("Please select a player");
                return;
            }

            if (spinButton != null)
                spinButton.interactable = false;

            try
            {
                Debug.Log(
                    $"[Spin Debug] Started | Player: {_playerId} | " +
                    $"Mode: {selectedMode} | " +
                    $"Bet: ${selectedBetCents / 100.0:F2}");

                long balanceBefore =
                    _dealService.GetPlayerBalance(_playerId);

                Debug.Log(
                    $"[Spin Debug] Balance before: " +
                    $"${balanceBefore / 100.0:F2}");

                SpinRecord spin = _dealService.Spin(
                    _playerId,
                    selectedMode,
                    selectedBetCents);

                Debug.Log(
                    $"[Spin Debug] Spin completed | " +
                    $"Spin ID: {spin.SpinId} | " +
                    $"Award: ${spin.AwardCents / 100.0:F2}");

                if (resultText != null)
                {
                    resultText.text = spin.AwardCents > 0
                        ? $"WIN  ${spin.AwardCents / 100.0:F2}"
                        : "NO WIN";
                }

                SetStatus(
                    spin.AwardCents > 0
                        ? "Congratulations! You won!"
                        : "Spin completed");

                _dealService.MarkResultShown(spin.SpinId);

                Debug.Log("[Spin Debug] Result marked as shown.");

                long balanceAfter =
                    _dealService.GetPlayerBalance(_playerId);

                Debug.Log(
                    $"[Spin Debug] Balance after: " +
                    $"${balanceAfter / 100.0:F2}");

                UpdateLabels();
                RefreshDealAudit();
            }
            catch (SpinRejectedException exception)
            {
                SetStatus(exception.Message);

                Debug.LogWarning(
                    "[UI] Spin rejected: " + exception.Message);
            }
            catch (Exception exception)
            {
                SetStatus("Something went wrong");
                Debug.LogException(exception);
            }
            finally
            {
                if (spinButton != null)
                    spinButton.interactable = true;

                UpdateLabels();
                RefreshDealAudit();
            }
        }


        private void UpdateLabels()
        {
            if (balanceText == null)
                return;

            if (_dealService == null)
            {
                balanceText.text = "--";
                return;
            }

            try
            {
                long balance = _dealService.GetPlayerBalance(_playerId);
                balanceText.text = $"${balance / 100.0:F2}";
            }
            catch (Exception exception)
            {
                balanceText.text = "--";
                Debug.LogWarning(
                    "[UI] Could not update balance: " + exception.Message);
            }
        }


        private void RefreshDealAudit()
        {
            if (dealAuditPanel == null || _dealService == null)
                return;

            dealAuditPanel.RefreshAudit(
                _dealService,
                selectedMode,
                selectedBetCents);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}