using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

namespace RlyNetwork.Example
{
    public class UIWalletAssets : MonoBehaviour
    {
        [SerializeField] UIWalletAssetsElement walletAssetsElementPrefab;
        [SerializeField] ScrollView scrollView;
        [SerializeField] RectTransform scrollViewContent;

        Dictionary<string, UIWalletAssetsElement> uIWalletAssetsElements = new();

        public UIWalletAssetsElement GetElement(string address) => uIWalletAssetsElements[address];

        public void UpdateData()
        {
            ClearAll();
        }

        public void UpdateTokenData(TokenFetchData data)
        {
            if (uIWalletAssetsElements.ContainsKey(data.Address))
                uIWalletAssetsElements[data.Address].Initialize(data.Address, $"({data.Symbol}) {data.Name}", data.Balance.ToString());
            else
                AddTokenData(data);
        }

        public void AddTokenData(TokenFetchData data)
        {
            if (uIWalletAssetsElements.ContainsKey(data.Address))
            {
                PopupManager.Instance.ShowPopup(PopupType.Information, "Error", "Token with same address has already been added", null);
                return;
            }

            var element = Instantiate(walletAssetsElementPrefab, scrollViewContent);
            element.Initialize(data.Address, $"({data.Symbol}) {data.Name}", data.Balance.ToString());
            uIWalletAssetsElements.Add(data.Address, element);
        }

        public void AddTokenDataArray(TokenFetchData[] data)
        {
            foreach (var asset in data)
                AddTokenData(asset);
        }

        public UIWalletAssetsElement[] GetAllElements() => uIWalletAssetsElements.Values.ToArray();

        public void ClearAll()
        {
            foreach (var element in uIWalletAssetsElements.Values)
                Destroy(element.gameObject);
            uIWalletAssetsElements.Clear();
        }
    }
}
