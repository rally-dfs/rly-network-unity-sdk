using System.Threading.Tasks;

using Nethereum.Contracts.Standards.ENS;

using UnityEngine;

namespace RlyNetwork.Example
{
    public partial class WalletManagementDebugUI : MonoBehaviour
    {
        public void OnLoadWalletButtonClicked()
        {
            if (string.IsNullOrEmpty(privateKeyInputField.text))
                return;

            SignInWallet();
        }

        public void OnSendTokensButtonClicked()
        {
            PopupManager.Instance.ShowPopup(PopupType.SendToken, "Send ETH/Tokens", "Select the token", (data) =>
            {
                if (string.IsNullOrEmpty(data))
                    return;

                var splitData = data.Split('|');
                var tokenAddress = splitData[0];
                var amount = splitData[1];
                var targetAddress = splitData[2];

                if (string.IsNullOrEmpty(tokenAddress) || string.IsNullOrEmpty(amount) || string.IsNullOrEmpty(targetAddress))
                    return;

                var element = tokenAssetsUI.GetElement(tokenAddress);
                var isETH = element.TokenAddress == ENSService.ENS_ZERO_ADDRESS;

                PopupManager.Instance.ShowPopup(PopupType.TwoOptions, isETH ? "Send ETH" : "Send Tokens", $"Are you sure you want to send {amount} {element.AssetName} to {targetAddress}?", (id) =>
                {
                    if (id == "Acknowledge")
                    {
                        if (isETH)
                            StartCoroutine(TransferEther(targetAddress, decimal.Parse(amount)));
                        else
                            _ = TransferToken(element.TokenAddress, targetAddress, decimal.Parse(amount));
                    }
                });
            });
        }

        public void OnAddNewTokenButtonClicked()
        {
            PopupManager.Instance.ShowPopup(PopupType.AddToken, "Add Token", "Enter the token address", (tokenAddress) => _ = AddNewToken(tokenAddress));
        }

        async Task AddNewToken(string tokenAddress)
        {
            if (string.IsNullOrEmpty(tokenAddress))
                return;

            var tokenSymbol = await FetchTokenSymbol(tokenAddress);
            var tokenName = await FetchTokenName(tokenAddress);
            var tokenBalance = await GetTokenBalance(tokenAddress, GetAccountAddress());

            if (string.IsNullOrEmpty(tokenSymbol) || string.IsNullOrEmpty(tokenName))
            {
                PopupManager.Instance.ShowPopup(PopupType.Information, "Add Token", $"No token with the address {tokenAddress} has been found", null);
                return;
            }

            tokenAssetsUI.AddTokenData(new TokenFetchData(tokenSymbol, tokenName, tokenAddress, tokenBalance));
        }

        public void OnAddNewNFTButtonClicked()
        {
            PopupManager.Instance.ShowPopup(PopupType.AddToken, "Add NFT", "Enter the NFT address", async (tokenAddress) =>
            {
                if (string.IsNullOrEmpty(tokenAddress))
                    return;

                var tokenSymbol = await FetchTokenSymbol(tokenAddress);
                var tokenName = await FetchTokenName(tokenAddress);
                var tokenBalance = await GetTokenBalance(tokenAddress, GetAccountAddress());

                nftAssetsUI.AddTokenData(new TokenFetchData(tokenSymbol, tokenName, tokenAddress, tokenBalance));
            });
        }

        public void UnloadWallet()
        {
            ChangeWalletState(WalletManagementStates.NoWalletConnected);

            tokenAssetsUI.ClearAll();
            nftAssetsUI.ClearAll();
        }

        public void OnAssetTokenTabValueChanged(bool state)
        {
            foreach (var content in assetTokenTabContents)
                content.SetActive(state);
        }

        public void OnAssetNFTTabValueChanged(bool state)
        {
            foreach (var content in assetNFTTabContents)
                content.SetActive(state);
        }

        public void ChangeWalletState(WalletManagementStates state)
        {
            walletManagementState = state;

            for (int i = 0; i < walletStateActivators.Length; i++)
            {
                for (int j = 0; j < walletStateActivators[i].Array.Length; j++)
                {
                    switch (walletStateActivators[i].Array[j].Rule)
                    {
                        case VisibilityRules.IsBelow:
                            walletStateActivators[i].Array[j].GameameObject.SetActive(i < (int)walletManagementState);
                            break;
                        case VisibilityRules.IsEactly:
                            walletStateActivators[i].Array[j].GameameObject.SetActive(i == (int)walletManagementState);
                            break;
                        case VisibilityRules.IsAtLeast:
                            walletStateActivators[i].Array[j].GameameObject.SetActive(i <= (int)walletManagementState);
                            break;
                    }
                }
            }

            if (state == WalletManagementStates.WalletDataFetched)
            {
                OnAssetTokenTabValueChanged(true);
                OnAssetNFTTabValueChanged(false);
            }

            Canvas.ForceUpdateCanvases();

            contentLayoutGroup.enabled = false;
            contentLayoutGroup.enabled = true;
        }
    }
}