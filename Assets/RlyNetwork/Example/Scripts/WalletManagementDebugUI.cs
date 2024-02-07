using System;
using System.Collections;

using System.Linq;
using System.Net;
using System.Net.Security;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Nethereum.Contracts.Standards.ENS;
using Nethereum.Contracts.Standards.ERC20;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.JsonRpc.Client;
using Nethereum.Unity.Rpc;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace RlyNetwork.Example
{
    [Serializable]
    public struct TokenFetchData
    {
        public string Symbol;
        public string Name;
        public string Address;
        public decimal Balance;

        public TokenFetchData(string symbol, string name, string address, decimal balance)
        {
            Symbol = symbol;
            Name = name;
            Address = address;
            Balance = balance;
        }
    }

    [Serializable]
    struct VisibilityBlockContainer
    {
        public VisibilityRule[] Array;
    }

    public enum VisibilityRules
    {
        IsBelow,
        IsEactly,
        IsAtLeast
    }

    [Serializable]
    struct VisibilityRule
    {
        public GameObject GameameObject;
        public VisibilityRules Rule;
    }

    public enum WalletManagementStates
    {
        NoWalletConnected,
        WalletConnected,
        WalletDataFetched
    }

    public partial class WalletManagementDebugUI : MonoBehaviour
    {
        public enum SupportedChain
        {
            Polygon = 137,
            Mumbai = 80001,
        }

        private static bool TrustCertificate(object sender, X509Certificate x509Certificate, X509Chain x509Chain, SslPolicyErrors sslPolicyErrors)
        {
            // all certificates are accepted
            return true;
        }

        [SerializeField] WalletSDKConfig walletSDKConfig;

        public TMP_InputField privateKeyInputField;
        public TMP_Text walletAddressText;
        public TMP_Dropdown chainDropdown;

        [SerializeField] VerticalLayoutGroup contentLayoutGroup;
        [SerializeField] UIWalletAssets tokenAssetsUI;
        [SerializeField] UIWalletAssets nftAssetsUI;

        [SerializeField] VisibilityBlockContainer[] walletStateActivators;
        [SerializeField] GameObject[] assetTokenTabContents;
        [SerializeField] GameObject[] assetNFTTabContents;

        WalletManagementStates walletManagementState = WalletManagementStates.NoWalletConnected;

        private Account account;
        private Web3 web3;

        public static WalletManagementDebugUI Instance { get; private set; }
        public string WalletAddress => GetAccountAddress();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            ServicePointManager.ServerCertificateValidationCallback = TrustCertificate;
            InitializeChainDropdown();
        }

        void InitializeChainDropdown()
        {
            chainDropdown.ClearOptions();
            chainDropdown.AddOptions(Enum.GetNames(typeof(SupportedChain)).ToList());
            chainDropdown.onValueChanged.AddListener(RefreshChain);
        }

        public UIWalletAssetsElement[] GetTokenElements() => tokenAssetsUI.GetAllElements();

        string GetAccountAddress()
        {
            return account.Address;
        }

        void SignInWallet()
        {
            var chain = (int)Enum.Parse(typeof(SupportedChain), chainDropdown.options[chainDropdown.value].text);
            UpdateAccount(privateKeyInputField.text, chain);

            ChangeWalletState(WalletManagementStates.WalletConnected);

            UpdateMainAssetTokenAmount();

            ChangeWalletState(WalletManagementStates.WalletDataFetched);
        }

        void RefreshChain(int index)
        {
            var chain = (int)Enum.Parse(typeof(SupportedChain), chainDropdown.options[index].text);
            if (chain != account.ChainId)
            {
                tokenAssetsUI.ClearAll();
                UpdateAccount(account.PrivateKey, chain);

                UpdateMainAssetTokenAmount();
            }
        }

        void UpdateAccount(string privateKey, int chain)
        {
            account = new Account(privateKey, chain);
            web3 = new Web3(account, new UnityWebRequestRpcTaskClient(new Uri(walletSDKConfig.GetChainConfig(chain).URL)));
            web3.TransactionManager.UseLegacyAsDefault = true;
            walletAddressText.text = $"Wallet: {account.Address}";
        }

        async void UpdateMainAssetTokenAmount()
        {
            var balance = await web3.Eth.GetBalance.SendRequestAsync(GetAccountAddress());
            var balanceInEther = Web3.Convert.FromWei(balance.Value);

            var data = (int)account.ChainId switch
            {
                (int)SupportedChain.Polygon => new TokenFetchData("MATIC", "Matic", ENSService.ENS_ZERO_ADDRESS, balanceInEther),
                (int)SupportedChain.Mumbai => new TokenFetchData("MATIC", "Matic", ENSService.ENS_ZERO_ADDRESS, balanceInEther),
                _ => new TokenFetchData("ETH", "Ethereum", ENSService.ENS_ZERO_ADDRESS, balanceInEther),
            };

            tokenAssetsUI.UpdateTokenData(data);

            var chainWrappedETHAddress = walletSDKConfig.GetChainConfig((int)account.ChainId).WrappedETHAddress;
            if (!string.IsNullOrEmpty(chainWrappedETHAddress))
            {
                var wrappedETHSymbol = await FetchTokenSymbol(chainWrappedETHAddress);
                var wrappedETHName = await FetchTokenName(chainWrappedETHAddress);
                var wrappedETHBalance = await GetTokenBalance(chainWrappedETHAddress, GetAccountAddress());
                var wrappedETHData = new TokenFetchData(wrappedETHSymbol, wrappedETHName, chainWrappedETHAddress, wrappedETHBalance);
                tokenAssetsUI.UpdateTokenData(wrappedETHData);
            }
        }

        async Task<string> FetchTokenName(string address)
        {
            var nameHandler = web3.Eth.GetContractQueryHandler<NameFunction>();
            return await nameHandler.QueryAsync<string>(address);
        }


        async Task<string> FetchTokenSymbol(string address)
        {
            try
            {
                if (!address.StartsWith("0x") || address.Length != 42)
                {
                    Debug.LogError("Invalid Ethereum address format.");
                    return string.Empty;
                }

                var symbolHandler = web3.Eth.GetContractQueryHandler<SymbolFunction>();
                return await symbolHandler.QueryAsync<string>(address);
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.LogError("RPC Response Error: " + rpcEx.Message);
                return string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return string.Empty;
            }
        }

        public async Task<decimal> GetTokenBalance(string contractAddress, string walletAddress)
        {
            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = walletAddress
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);
            return Web3.Convert.FromWei(balance, await GetTokenDecimals(contractAddress));
        }

        public async Task<decimal> GetTokenBalance(string contractAddress, string walletAddress, int decimals)
        {
            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = walletAddress
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);
            return Web3.Convert.FromWei(balance, decimals);
        }

        public async Task<BigInteger> GetTokenDecimals(string contractAddress)
        {
            var decimalsHandler = web3.Eth.GetContractQueryHandler<DecimalsFunction>();
            return await decimalsHandler.QueryAsync<BigInteger>(contractAddress);
        }

        IEnumerator TransferEther(string recipientAddress, decimal amountInEth)
        {
            var ethTransfer = new EthTransferUnityRequest(walletSDKConfig.GetChainConfig((int)account.ChainId).URL, account.PrivateKey, account.ChainId);

            yield return ethTransfer.TransferEther(recipientAddress, amountInEth);

            if (ethTransfer.Exception == null)
            {
                PopupManager.Instance.ShowPopup(PopupType.Information, "Send ETH", $"Transaction sent. Tx Hash: {ethTransfer.Result}", null);
            }
            else
            {
                PopupManager.Instance.ShowPopup(PopupType.Information, "Send ETH", $"Transaction failed. Reason: {ethTransfer.Exception.Message}", null);
            }
        }

        async Task TransferToken(string tokenAddress, string recipientAddress, decimal amountInToken, MetaTxMethod? metaTxMethod = null)
        {
            var amount = Web3.Convert.ToWei(amountInToken, (int)await GetTokenDecimals(tokenAddress));
            TransferTokenExact(tokenAddress, recipientAddress, amount, metaTxMethod);
        }

        void TransferTokenExact(string tokenAddress, string recipientAddress, BigInteger amount, MetaTxMethod? metaTxMethod = null)
        {
            if (metaTxMethod.HasValue)
            {
                _ = ((int)account.ChainId switch
                {
                    (int)SupportedChain.Polygon => NetworkProvider.RlyPolygon.WithAccount(account).WithApiKey(walletSDKConfig.ApiKey).TransferExact(recipientAddress, amount, metaTxMethod.Value, tokenAddress),
                    (int)SupportedChain.Mumbai => NetworkProvider.RlyMumbai.WithAccount(account).WithApiKey(walletSDKConfig.ApiKey).TransferExact(recipientAddress, amount, metaTxMethod.Value, tokenAddress),
                    _ => throw new NotImplementedException(),
                }).ContinueWithOnMainThread(t =>
                {
                    if (!t.IsCanceled && !t.IsFaulted)
                    {
                        PopupManager.Instance.ShowPopup(PopupType.Information, "Send Token", $"Transaction sent. Tx Hash: {t.Result}", null);
                    }
                    else
                    {
                        Debug.LogException(t.Exception);
                        PopupManager.Instance.ShowPopup(PopupType.Information, "Send Token", $"Transaction failed. Reason: {t.Exception.Message}", null);
                    }
                });
                return;
            }

            var token = new ERC20Service(web3.Eth).GetContractService(tokenAddress);

            _ = token.TransferRequestAsync(recipientAddress, amount).ContinueWithOnMainThread(t =>
            {
                if (!t.IsCanceled && !t.IsFaulted)
                {
                    PopupManager.Instance.ShowPopup(PopupType.Information, "Send Token", $"Transaction sent. Tx Hash: {t.Result}", null);
                }
                else
                {
                    Debug.LogException(t.Exception);
                    PopupManager.Instance.ShowPopup(PopupType.Information, "Send Token", $"Transaction failed. Reason: {t.Exception.Message}", null);
                }
            });
        }
    }
}