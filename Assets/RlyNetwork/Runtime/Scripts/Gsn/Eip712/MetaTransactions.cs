#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.ABI.EIP712;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public class MetaTransaction
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Salt { get; set; }
    public string VerifyingContract { get; set; }
    public int Nonce { get; set; }
    public string From { get; set; }
    public byte[] FunctionSignature { get; set; }

    public MetaTransaction(string name, string version, string salt, string verifyingContract, int nonce, string from, byte[] functionSignature)
    {
        Name = name;
        Version = version;
        Salt = salt;
        VerifyingContract = verifyingContract;
        Nonce = nonce;
        From = from.ToLowerInvariant();
        FunctionSignature = functionSignature;
    }

    public static TypedData<DomainWithoutChainIdButSalt> GetTypedMetatransaction(MetaTransaction metaTransaction)
    {
        var types = new Dictionary<string, MemberDescription[]>
        {
            {
                "EIP712Domain",
                new MemberDescription[]
                {
                    new() { Name = "name", Type = "string" },
                    new() { Name = "version", Type = "string" },
                    new() { Name = "verifyingContract", Type = "address" },
                    new() { Name = "salt", Type = "bytes32" },
                }
            },
            {
                "MetaTransaction",
                new MemberDescription[]
                {
                    new() { Name = "nonce", Type = "uint256" },
                    new() { Name = "from", Type = "address" },
                    new() { Name = "functionSignature", Type = "bytes" },
                }
            },
        };

        const string primaryType = "MetaTransaction";

        var domainSeperator = new DomainWithoutChainIdButSalt
        {
            Name = metaTransaction.Name,
            Version = metaTransaction.Version,
            VerifyingContract = metaTransaction.VerifyingContract,
            Salt = metaTransaction.Salt.HexToByteArray(),
        };

        var messageData = new MemberValue[] {
            new() { TypeName = "uint256", Value = metaTransaction.Nonce },
            new() { TypeName = "address", Value = metaTransaction.From },
            new() { TypeName = "bytes", Value = metaTransaction.FunctionSignature.ToHex(true) },
        };

        return new TypedData<DomainWithoutChainIdButSalt>
        {
            Types = types,
            PrimaryType = primaryType,
            Domain = domainSeperator,
            Message = messageData
        };
    }

    public static Dictionary<string, byte[]> GetMetatransactionEIP712Signature(Account wallet, string contractName, string contractAddress, byte[] functionSignature, NetworkConfig config, int nonce)
    {
        var chainId = int.Parse(config.Gsn.ChainId);
        var saltHexString = chainId.ToString("X2");
        var paddedSaltHexString = "0x" + saltHexString.PadLeft(64, '0');

        var eip712Data = GetTypedMetatransaction(new MetaTransaction(contractName, "1", paddedSaltHexString, contractAddress, nonce, wallet.Address, functionSignature));

        var signature = wallet.SignTypedData(eip712Data);

        var cleanedSignature = signature.StartsWith("0x") ? signature.Substring(2) : signature;
        var signatureBytes = cleanedSignature.HexToByteArray();

        return new Dictionary<string, byte[]>
        {
            { "r", signatureBytes[0..32] },
            { "s", signatureBytes[32..64] },
            { "v", new byte[] { signatureBytes[64] } }
        };
    }

    public static async Task<GsnTransactionDetails> GetExecuteMetatransactionTx(Account wallet, string destinationAddress, BigInteger amount, NetworkConfig config, string contractAddress, Web3 provider)
    {
        var token = new ERC20Service(provider.Eth).GetContractService(contractAddress);

        var name = await token.NameQueryAsync();

        var nonce = await GnsTxHelper.GetSenderContractNonce(provider, contractAddress, wallet.Address);

        var data = new TransferFunction
        {
            To = destinationAddress,
            Value = amount
        }.GetCallData();

        var signatureData = GetMetatransactionEIP712Signature(wallet, name, contractAddress, data, config, (int)nonce);

        var r = signatureData["r"];
        var s = signatureData["s"];
        var v = signatureData["v"];

        var tx = new ExecuteMetaTransactionFunction
        {
            FromAddress = wallet.Address,
            UserAddress = wallet.Address,
            FunctionSignature = data,
            SigR = r,
            SigS = s,
            SigV = v[0],
        };

        var estimatedGas = await provider.Eth.GetContractTransactionHandler<ExecuteMetaTransactionFunction>().EstimateGasAsync(contractAddress, tx);

        var info = await provider.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());

        var maxPriorityFeePerGas = BigInteger.Parse("1500000000");
        var maxFeePerGas = info.BaseFeePerGas.Value * 2 + maxPriorityFeePerGas;

        return new GsnTransactionDetails(wallet.Address, tx.GetCallData().ToHex(true), contractAddress, maxFeePerGas.ToString(), maxPriorityFeePerGas.ToString(), "0", $"0x{estimatedGas.Value:X2}");
    }

    [Function("executeMetaTransaction", "bytes")]
    public class ExecuteMetaTransactionFunction : FunctionMessage
    {
        [Parameter("address", "userAddress", 1)]
        public virtual string UserAddress { get; set; } = string.Empty;

        [Parameter("bytes", "functionSignature", 2)]
        public virtual byte[] FunctionSignature { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "sigR", 3)]
        public virtual byte[] SigR { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "sigS", 4)]
        public virtual byte[] SigS { get; set; } = Array.Empty<byte>();

        [Parameter("uint8", "sigV", 5)]
        public virtual byte SigV { get; set; }
    }
}