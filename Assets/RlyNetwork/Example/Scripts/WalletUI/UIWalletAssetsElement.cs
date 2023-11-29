using TMPro;

using UnityEngine;

namespace RlyNetwork.Example
{
    public class UIWalletAssetsElement : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI assetName;
        [SerializeField] TextMeshProUGUI assetBalance;

        public string AssetName => assetName.text;
        public string Balance => assetBalance.text;
        public string TokenAddress { get; private set; }

        public void Initialize(string address, string name, string balance)
        {
            TokenAddress = address;
            assetName.text = name;
            assetBalance.text = balance;
        }

        public void UpdateBalance(string balance)
        {
            assetBalance.text = balance;
        }
    }
}