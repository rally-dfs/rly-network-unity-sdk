#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.ABI.EIP712;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Contracts.Standards.ERC721;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public class Permit
{
    public string Name { get; }
    public string Version { get; }
    public int ChainId { get; }
    public string VerifyingContract { get; }
    public string Owner { get; }
    public string Spender { get; }
    public BigInteger Value { get; }
    public BigInteger Nonce { get; }
    public BigInteger Deadline { get; }
    public string Salt { get; }

    public Permit(string name, string version, int chainId, string verifyingContract, string owner, string spender, BigInteger value, BigInteger nonce, BigInteger deadline, string salt)
    {
        Name = name;
        Version = version;
        ChainId = chainId;
        VerifyingContract = verifyingContract;
        Owner = owner.ToLowerInvariant();
        Spender = spender.ToLowerInvariant();
        Value = value;
        Nonce = nonce;
        Deadline = deadline;
        Salt = salt;
    }

    public static string GetSignedTypedPermitTransaction(Account wallet, Permit permit)
    {
        var hasSalt = !string.IsNullOrEmpty(permit.Salt) && permit.Salt != "0x0000000000000000000000000000000000000000000000000000000000000000";

        var types = new Dictionary<string, MemberDescription[]>
        {
            {
                "EIP712Domain",
                hasSalt ? new MemberDescription[]
                {
                    new() { Name = "name", Type = "string" },
                    new() { Name = "version", Type = "string" },
                    new() { Name = "chainId", Type = "uint256" },
                    new() { Name = "verifyingContract", Type = "address" },
                    new() { Name = "salt", Type = "bytes32" }
                } : new MemberDescription[]
                {
                    new() { Name = "name", Type = "string" },
                    new() { Name = "version", Type = "string" },
                    new() { Name = "chainId", Type = "uint256" },
                    new() { Name = "verifyingContract", Type = "address" }
                }
            },
            {
                "Permit",
                new MemberDescription[]
                {
                    new() { Name = "owner", Type = "address" },
                    new() { Name = "spender", Type = "address" },
                    new() { Name = "value", Type = "uint256" },
                    new() { Name = "nonce", Type = "uint256" },
                    new() { Name = "deadline", Type = "uint256" }
                }
            }
        };

        const string primaryType = "Permit";

        var messageData = new MemberValue[]
        {
            new() { TypeName = "address", Value = permit.Owner },
            new() { TypeName = "address", Value = permit.Spender },
            new() { TypeName = "uint256", Value = permit.Value },
            new() { TypeName = "uint256", Value = permit.Nonce },
            new() { TypeName = "uint256", Value = permit.Deadline }
        };

        if (hasSalt)
        {
            var domainSeparator = new DomainWithSalt
            {
                Name = permit.Name,
                Version = permit.Version,
                ChainId = permit.ChainId,
                VerifyingContract = permit.VerifyingContract,
                Salt = permit.Salt.HexToByteArray()
            };
            var typedData = new TypedData<DomainWithSalt>
            {
                Types = types,
                PrimaryType = primaryType,
                Domain = domainSeparator,
                Message = messageData
            };
            return wallet.SignTypedData(typedData);
        }
        else
        {
            var domainSeparator = new Domain
            {
                Name = permit.Name,
                Version = permit.Version,
                ChainId = permit.ChainId,
                VerifyingContract = permit.VerifyingContract
            };
            var typedData = new TypedData<Domain>
            {
                Types = types,
                PrimaryType = primaryType,
                Domain = domainSeparator,
                Message = messageData
            };
            typedData.EnsureDomainRawValuesAreInitialised();
            return wallet.SignTypedData(typedData);
        }
    }

    public static Dictionary<string, byte[]> GetPermitEIP712Signature(Account wallet, string contractName, string contractAddress, NetworkConfig config, int nonce, BigInteger amount, BigInteger deadline, string salt)
    {
        var chainId = int.Parse(config.Gsn.ChainId);

        var signature = GetSignedTypedPermitTransaction(wallet, new Permit(contractName, "1", chainId, contractAddress, wallet.Address, config.Gsn.PaymasterAddress, amount, nonce, deadline, salt));

        var cleanedSignature = signature.StartsWith("0x") ? signature.Substring(2) : signature;
        var signatureBytes = cleanedSignature.HexToByteArray();

        return new Dictionary<string, byte[]>
        {
            { "r", signatureBytes[0..32] },
            { "s", signatureBytes[32..64] },
            { "v", new byte[] { signatureBytes[64] } }
        };
    }

    public static async Task<GsnTransactionDetails> GetPermitTx(Account wallet, string destinationAddress, BigInteger amount, NetworkConfig config, string contractAddress, Web3 provider)
    {
        var tokenService = new ERC721Service(provider.Eth).GetContractService(contractAddress);

        var nonce = await tokenService.NoncesQueryAsync(wallet.Address);
        var name = await tokenService.NameQueryAsync();

        var deadline = await GetPermitDeadline(provider);
        var eip712DomainCallResult = await provider.Eth.GetContractQueryHandler<EIP712DomainFunction>().QueryAsync<EIP712DomainFunctionOutputDTO>(contractAddress);
        var salt = eip712DomainCallResult.Salt.ToHex(true);

        var signatureData = GetPermitEIP712Signature(wallet, name, contractAddress, config, (int)nonce, amount, deadline, salt);

        var r = signatureData["r"];
        var s = signatureData["s"];
        var v = signatureData["v"];

        var fromTx = new TransferFromFunction()
        {
            From = wallet.Address,
            To = destinationAddress,
            Value = amount
        }.GetCallData();

        var tx = new PermitFunction
        {
            FromAddress = wallet.Address,
            Owner = wallet.Address,
            Spender = config.Gsn.PaymasterAddress,
            Value = amount,
            Deadline = deadline,
            V = v[0],
            R = r,
            S = s,
        };

        var gas = await provider.Eth.GetContractTransactionHandler<PermitFunction>().EstimateGasAsync(contractAddress, tx);

        var paymasterData = $"0x{contractAddress.Replace("0x", "")}{fromTx.ToHex()}";
        var info = await provider.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());

        var maxPriorityFeePerGas = BigInteger.Parse("1500000000");
        var maxFeePerGas = info.BaseFeePerGas.Value * 2 + maxPriorityFeePerGas;

        return new GsnTransactionDetails(wallet.Address, tx.GetCallData().ToHex(true), contractAddress, maxFeePerGas.ToString(), maxPriorityFeePerGas.ToString(), "0", $"0x{gas.Value:X2}", paymasterData);
    }

    public static async Task<BigInteger> GetPermitDeadline(Web3 provider)
    {
        var block = await provider.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());
        return (block.Timestamp.Value + 45) * 1000;
    }

    [Function("transferFrom", "bool")]
    public class TransferFromFunction : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; } = string.Empty;

        [Parameter("address", "to", 2)]
        public virtual string To { get; set; } = string.Empty;

        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
    }

    [Function("permit", "bool")]
    public class PermitFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public string? Owner { get; set; } = string.Empty;

        [Parameter("address", "spender", 2)]
        public string? Spender { get; set; } = string.Empty;

        [Parameter("uint256", "value", 3)]
        public BigInteger Value { get; set; }

        [Parameter("uint256", "deadline", 4)]
        public BigInteger Deadline { get; set; }

        [Parameter("uint8", "v", 5)]
        public byte V { get; set; }

        [Parameter("bytes32", "r", 6)]
        public byte[]? R { get; set; } = Array.Empty<byte>();

        [Parameter("bytes32", "s", 7)]
        public byte[]? S { get; set; } = Array.Empty<byte>();
    }

    [Function("eip712Domain", typeof(EIP712DomainFunctionOutputDTO))]
    public class EIP712DomainFunction : FunctionMessage
    {
    }

    [FunctionOutput]
    public class EIP712DomainFunctionOutputDTO : IFunctionOutputDTO
    {
        [Parameter("bytes1", "fields", 1)]
        public byte Fields { get; set; }

        [Parameter("string", "name", 2)]
        public string Name { get; set; } = string.Empty;

        [Parameter("string", "version", 3)]
        public string Version { get; set; } = string.Empty;

        [Parameter("uint256", "chainId", 4)]
        public BigInteger ChainId { get; set; }

        [Parameter("address", "verifyingContract", 5)]
        public string VerifyingContract { get; set; } = string.Empty;

        [Parameter("bytes32", "salt", 6)]
        public byte[] Salt { get; set; } = Array.Empty<byte>();
    }
}