using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace RlyNetwork.Example
{
    public class UISendTokensPopup : UIPopupBase
    {
        [SerializeField] Button addButton;
        [SerializeField] TMP_Dropdown tokenDropdown;
        [SerializeField] TMP_InputField amountInputField;

        UIWalletAssetsElement[] uIWalletAssetsElements;

        string tokenAddress;
        string tokenAmount;
        string targetWalletAddress;

        public override void Initialize(string title, string description, Action<string> action)
        {
            base.Initialize(title, description, action);

            QueryAddedTokensForDropdown();

            EvaluateButtonStates();
        }

        void QueryAddedTokensForDropdown()
        {
            uIWalletAssetsElements = WalletManagementDebugUI.Instance.GetTokenElements();
            tokenDropdown.ClearOptions();

            foreach (var addedToken in uIWalletAssetsElements)
                tokenDropdown.options.Add(new TMP_Dropdown.OptionData(addedToken.AssetName));

            tokenDropdown.value = 0;
            tokenDropdown.RefreshShownValue();

            OnTokenIndexSelectionChanged(tokenDropdown.value);
        }

        public void OnTokenIndexSelectionChanged(int index)
        {
            tokenAddress = uIWalletAssetsElements[index].TokenAddress;
            EvaluateButtonStates();
        }

        public void OnTokenAmountChanged(string amountText)
        {
            tokenAmount = amountText;
            EvaluateButtonStates();
        }

        public void OnTargetWalletAddressChanged(string address)
        {
            targetWalletAddress = address;
            EvaluateButtonStates();
        }

        public void EvaluateButtonStates()
        {
            bool isTokenAddressValid = !string.IsNullOrEmpty(tokenAddress);
            bool isTokenValid = false;

            if (isTokenAddressValid)
            {
                decimal.TryParse(uIWalletAssetsElements[tokenDropdown.value].Balance, out decimal ownedBalance);
                decimal.TryParse(tokenAmount, out decimal inputAmount);
                isTokenValid = inputAmount > 0 && inputAmount <= ownedBalance;
            }

            bool isTargetAddressValid = !string.IsNullOrEmpty(targetWalletAddress)
                && WalletManagementDebugUI.Instance.WalletAddress != targetWalletAddress
                && targetWalletAddress.StartsWith("0x")
                && targetWalletAddress.Length == 42;

            addButton.interactable = isTokenAddressValid && isTokenValid && isTargetAddressValid;
        }

        public override void OnButtonClicked(string id)
        {
            action?.Invoke(string.Join("|", tokenAddress, amountInputField.text, targetWalletAddress));
            Close();
        }

        public void OnCancelButtonClicked()
        {
            action?.Invoke(string.Empty);
            Close();
        }
    }
}