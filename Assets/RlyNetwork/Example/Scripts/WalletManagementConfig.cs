using System;

using UnityEngine;

namespace RlyNetwork.Example
{
    [CreateAssetMenu(menuName = "RlyNetwork/WalletSDKConfig", fileName = "WalletSDKConfig")]
    public class WalletSDKConfig : ScriptableObject
    {
        [field: SerializeField] public ChainConfig[] Chains { get; private set; }

        public ChainConfig GetChainConfig(int chainId)
        {
            foreach (var chain in Chains)
                if (chain.Id == chainId)
                    return chain;

            return null;
        }
    }

    [Serializable]
    public class ChainConfig
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public string URL { get; private set; }
        [field: SerializeField] public string ExplorerURL { get; private set; }
        [field: SerializeField] public string WrappedETHAddress { get; private set; }

        [field: SerializeField] public GSNConfig GSN { get; private set; }
    }

    public class GSNConfig
    {
        [field: SerializeField] public string PaymasterAddress { get; private set; }
        [field: SerializeField] public string ForwarderAddress { get; private set; }
        [field: SerializeField] public string RelayHubAddress { get; private set; }
        [field: SerializeField] public string RelayWorkerAddress { get; set; }
        [field: SerializeField] public string RelayUrl { get; private set; }
        [field: SerializeField] public string RpcUrl { get; private set; }
        [field: SerializeField] public string ChainId { get; private set; }
        [field: SerializeField] public string MaxAcceptanceBudget { get; private set; }
        [field: SerializeField] public string DomainSeparatorName { get; private set; }
        [field: SerializeField] public string GtxDataNonZero { get; private set; }
        [field: SerializeField] public string GtxDataZero { get; private set; }
        [field: SerializeField] public string RequestValidSeconds { get; private set; }
        [field: SerializeField] public string MaxPaymasterDataLength { get; private set; }
        [field: SerializeField] public string MaxApprovalDataLength { get; private set; }
        [field: SerializeField] public string MaxRelayNonceGap { get; private set; }

        [field: SerializeField] public string RelayerApiKey { get; private set; }
    }
}