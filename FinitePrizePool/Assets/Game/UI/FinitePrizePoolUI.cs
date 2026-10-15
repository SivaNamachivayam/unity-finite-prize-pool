
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FinitePrizePool.DealEngine;

namespace FinitePrizePool.UI
{
    public sealed class FinitePrizePoolUI : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private GameMode selectedMode = GameMode.Inner;
        [SerializeField] private long selectedBetCents = 100;

        [Header("UI References")]
        [SerializeField] private TMP_Dropdown modeDropdown;
        [SerializeField] private TMP_Dropdown betDropdown;
        [SerializeField] private TMP_Dropdown playerDropdown;
        [SerializeField] private Button spinButton;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text statusText;

        private DealService _dealService;
        private string _playerId;

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

            if (spinButton != null)
                spinButton.onClick.AddListener(OnSpinClicked);

            if (modeDropdown != null)
                modeDropdown.onValueChanged.AddListener(OnModeChanged);

            if (betDropdown != null)
                betDropdown.onValueChanged.AddListener(OnBetChanged);

            if (playerDropdown != null)
                playerDropdown.onValueChanged.AddListener(OnPlayerChanged);

            UpdateLabels();
        }

        private void OnDestroy()
        {
            if (spinButton != null)
                spinButton.onClick.RemoveListener(OnSpinClicked);

            if (modeDropdown != null)
                modeDropdown.onValueChanged.RemoveListener(OnModeChanged);

            if (betDropdown != null)
                betDropdown.onValueChanged.RemoveListener(OnBetChanged);

            if (playerDropdown != null)
                playerDropdown.onValueChanged.RemoveListener(OnPlayerChanged);
        }

        public void Initialize(DealService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            _dealService = service;
            UpdateLabels();
        }

        public void SetPlayer(string playerId)
        {
            _playerId = playerId;
            UpdateLabels();
        }

        private void SetupModeDropdown()
        {
            if (modeDropdown == null)
                return;

            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(
                new System.Collections.Generic.List<string>
                {
                    "Inner",
                    "Outer"
                });

            modeDropdown.SetValueWithoutNotify(
                selectedMode == GameMode.Inner ? 0 : 1);
        }

        private void SetupBetDropdown()
        {
            if (betDropdown == null)
                return;

            var options = new System.Collections.Generic.List<string>();

            foreach (long cents in _betLevelsCents)
                options.Add("$" + (cents / 100m).ToString("0.00"));

            betDropdown.ClearOptions();
            betDropdown.AddOptions(options);

            int index = Array.IndexOf(_betLevelsCents, selectedBetCents);
            betDropdown.SetValueWithoutNotify(index >= 0 ? index : 3);
        }

        private void SetupPlayerDropdown()
        {
            if (playerDropdown == null)
                return;

            playerDropdown.ClearOptions();
            playerDropdown.AddOptions(
                new System.Collections.Generic.List<string>
                {
                    "Player 1",
                    "Player 2"
                });

            playerDropdown.SetValueWithoutNotify(0);
            _playerId = "P1";
        }

        private void OnModeChanged(int index)
        {
            selectedMode = index == 0
                ? GameMode.Inner
                : GameMode.Outer;

            UpdateLabels();
        }

        private void OnBetChanged(int index)
        {
            if (index < 0 || index >= _betLevelsCents.Length)
                return;

            selectedBetCents = _betLevelsCents[index];
            UpdateLabels();
        }

        private void OnPlayerChanged(int index)
        {
            _playerId = index == 0 ? "P1" : "P2";
            UpdateLabels();
        }

        private void OnSpinClicked()
        {
            if (_dealService == null)
            {
                SetStatus("Game service is not connected.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_playerId))
            {
                SetStatus("Select a player.");
                return;
            }

            spinButton.interactable = false;

            try
            {
                SpinRecord spin = _dealService.Spin(
                    _playerId,
                    selectedMode,
                    selectedBetCents);

                if (resultText != null)
                {
                    resultText.text =
                        spin.AwardCents > 0
                            ? "You won $" + (spin.AwardCents / 100m).ToString("0.00")
                            : "No prize this time.";
                }

                SetStatus("Spin saved successfully.");

                _dealService.MarkResultShown(spin.SpinId);
            }
            catch (SpinRejectedException exception)
            {
                SetStatus(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus("Spin failed. Check the Console.");
            }
            finally
            {
                spinButton.interactable = true;
                UpdateLabels();
            }
        }

        private void UpdateLabels()
        {
            if (_dealService == null)
            {
                if (balanceText != null)
                    balanceText.text = "Balance: --";

                return;
            }

            if (!string.IsNullOrWhiteSpace(_playerId) &&
                balanceText != null)
            {
                try
                {
                    balanceText.text =
                        "Balance: $" +
                        (_dealService.GetPlayerBalance(_playerId) / 100m)
                        .ToString("0.00");
                }
                catch (Exception)
                {
                    balanceText.text = "Balance: unavailable";
                }
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
