#nullable enable

using System;

using Nethereum.ABI.EIP712;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public static class AccountExtension
{
    public enum TypedDataVersion
    {
        V3,
        V4
    }

    public static Web3 GetEthClient(this Account account, NetworkConfig config) => new(account, config.Gsn.RpcUrl);
    public static Web3 GetEthClient(this Account account, string rpcUrl) => new(account, rpcUrl);

    public static string SignTypedData<TDomain>(this Account account, TypedData<TDomain> eip712Data, TypedDataVersion typedDataVersion = TypedDataVersion.V4)
    {
        switch (typedDataVersion)
        {
            case TypedDataVersion.V3:
                return CustomEip712TypedDataSigner.Current.SignTypedData(eip712Data, new EthECKey(account.PrivateKey));
            case TypedDataVersion.V4:
                return CustomEip712TypedDataSigner.Current.SignTypedDataV4(eip712Data, new EthECKey(account.PrivateKey));
            default:
                throw new ArgumentOutOfRangeException(nameof(typedDataVersion), typedDataVersion, null);
        }
    }
}