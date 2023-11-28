using System;

using UnityEngine;
using UnityEngine.UI;

namespace RlyNetwork.Example
{
    public class UIAddTokenPopup : UIPopupBase
    {
        [SerializeField] Button addButton;

        string tokenAddress = string.Empty;

        public override void Initialize(string title, string description, Action<string> action)
        {
            base.Initialize(title, description, action);

            EvaluateButtonStates();
        }

        public void OnTokenAddressChanged(string address)
        {
            tokenAddress = address;
            EvaluateButtonStates();
        }

        public void EvaluateButtonStates()
        {
            if (!tokenAddress.StartsWith("0x") || tokenAddress.Length != 42)
                addButton.interactable = false;
            else
                addButton.interactable = true;
        }

        public override void OnButtonClicked(string id)
        {
            action?.Invoke(tokenAddress);
            Close();
        }

        public void OnCancelButtonClicked()
        {
            action?.Invoke(string.Empty);
            Close();
        }
    }
}
