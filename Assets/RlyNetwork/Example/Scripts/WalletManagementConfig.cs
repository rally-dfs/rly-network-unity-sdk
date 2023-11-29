using System;

using UnityEngine;

namespace RlyNetwork.Example
{
    [CreateAssetMenu(menuName = "RlyNetwork/WalletSDKConfig", fileName = "WalletSDKConfig")]
    public class WalletSDKConfig : ScriptableObject
    {
        [field: SerializeField] public ChainConfig[] Chains { get; private set; }
        [field: SerializeField] public bool SendTokensAsMetaTransactions { get; private set; }
        [field: SerializeField] public string ApiKey { get; private set; }

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
    }
}