#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.ABI;
using Nethereum.ABI.EIP712;
using Nethereum.Contracts;

using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

using Newtonsoft.Json;

public static class GnsTxHelper
{
    public static CalldataBytes CalculateCalldataBytesZeroNonzero(string calldata)
    {
        var calldataBuf = calldata.Replace("0x", "").HexToByteArray();

        int calldataZeroBytes = calldataBuf.Count(ch => ch == 0);
        int calldataNonzeroBytes = calldataBuf.Length - calldataZeroBytes;

        return new CalldataBytes(calldataZeroBytes, calldataNonzeroBytes);
    }

    public static int CalculateCalldataCost(string msgData, int gtxDataNonZero, int gtxDataZero)
    {
        var calldataBytesZeroNonzero = CalculateCalldataBytesZeroNonzero(msgData);
        return calldataBytesZeroNonzero.CalldataZeroBytes * gtxDataZero + calldataBytesZeroNonzero.CalldataNonzeroBytes * gtxDataNonZero;
    }
    public static string EstimateGasWithoutCallData(GsnTransactionDetails transaction, int gtxDataNonZero, int gtxDataZero)
    {
        var originalGas = transaction.Gas;
        var callDataCost = CalculateCalldataCost(transaction.Data, gtxDataNonZero, gtxDataZero);
        var adjustedGas = BigInteger.Parse(originalGas![2..], System.Globalization.NumberStyles.HexNumber) - callDataCost;

        return $"0x{adjustedGas:X2}";
    }

    public static string EstimateCalldataCostForRequest(RelayRequest relayRequestOriginal, GSNConfig config, Web3 client)
    {
        var relayRequest = new RelayRequest(relayRequestOriginal.Request, new RelayData(
            relayRequestOriginal.RelayData.MaxFeePerGas,
            relayRequestOriginal.RelayData.MaxPriorityFeePerGas,
            "0xffffffffff",
            relayRequestOriginal.RelayData.RelayWorker,
            relayRequestOriginal.RelayData.Paymaster,
            relayRequestOriginal.RelayData.Forwarder,
             "0x" + string.Concat(Enumerable.Repeat("ff", config.MaxPaymasterDataLength)),
            relayRequestOriginal.RelayData.ClientId
        ));

        var maxAcceptanceBudget = "0xffffffffff";
        var signature = "0x" + string.Concat(Enumerable.Repeat("ff", 65));
        var approvalData = "0x" + string.Concat(Enumerable.Repeat("ff", config.MaxApprovalDataLength));

        var function = new IRelayHubABI.RelayCallFunction
        {
            DomainSeparatorName = config.DomainSeparatorName,
            MaxAcceptanceBudget = BigInteger.Parse(maxAcceptanceBudget, System.Globalization.NumberStyles.HexNumber),
            RelayRequest = new IRelayHubABI.RelayRequest
            {
                Request = new IRelayHubABI.ForwardRequest
                {
                    From = relayRequest.Request.From,
                    To = relayRequest.Request.To,
                    Value = BigInteger.Parse(relayRequest.Request.Value, System.Globalization.NumberStyles.HexNumber),
                    Gas = BigInteger.Parse(relayRequest.Request.Gas, System.Globalization.NumberStyles.HexNumber),
                    Nonce = BigInteger.Parse(relayRequest.Request.Nonce, System.Globalization.NumberStyles.HexNumber),
                    Data = relayRequest.Request.Data.HexToByteArray(),
                    ValidUntilTime = BigInteger.Parse(relayRequest.Request.ValidUntilTime, System.Globalization.NumberStyles.HexNumber)
                },
                RelayData = new IRelayHubABI.RelayData
                {
                    MaxFeePerGas = BigInteger.Parse(relayRequest.RelayData.MaxFeePerGas, System.Globalization.NumberStyles.HexNumber),
                    MaxPriorityFeePerGas = BigInteger.Parse(relayRequest.RelayData.MaxPriorityFeePerGas, System.Globalization.NumberStyles.HexNumber),
                    RelayWorker = relayRequest.RelayData.RelayWorker,
                    Paymaster = relayRequest.RelayData.Paymaster,
                    Forwarder = relayRequest.RelayData.Forwarder,
                    PaymasterData = relayRequest.RelayData.PaymasterData.HexToByteArray(),
                    ClientId = BigInteger.Parse(relayRequest.RelayData.ClientId)
                }
            },
            Signature = signature.HexToByteArray(),
            ApprovalData = approvalData.HexToByteArray()
        };

        var txData = function.GetCallData();

        return BigInteger.Parse(CalculateCalldataCost(txData.ToHex(true), config.GtxDataNonZero, config.GtxDataZero).ToString(), System.Globalization.NumberStyles.HexNumber).ToString("X2");
    }

    public static async Task<string> GetSenderNonce(string sender, string forwarderAddress, Web3 client)
    {
        var function = client.Eth.GetContractQueryHandler<IForwarderABI.GetNonceFunction>();
        var result = await function.QueryAsync<BigInteger>(sender);

        return result.ToString();
    }

    public static string SignRequest(RelayRequest relayRequest, string domainSeparatorName, string chainId, Account wallet, NetworkConfig config)
    {
        var types = new Dictionary<string, MemberDescription[]>
        {
            ["EIP712Domain"] = new[]
            {
                new MemberDescription { Name = "name", Type = "string" },
                new MemberDescription { Name = "version", Type = "string" },
                new MemberDescription { Name = "chainId", Type = "uint256" },
                new MemberDescription { Name = "verifyingContract", Type = "address" }
            },
            ["RelayRequest"] = new[]
            {
                new MemberDescription { Name = "from", Type = "address" },
                new MemberDescription { Name = "to", Type = "address" },
                new MemberDescription { Name = "value", Type = "uint256" },
                new MemberDescription { Name = "gas", Type = "uint256" },
                new MemberDescription { Name = "nonce", Type = "uint256" },
                new MemberDescription { Name = "data", Type = "bytes" },
                new MemberDescription { Name = "validUntilTime", Type = "uint256" },
                new MemberDescription { Name = "relayData", Type = "RelayData" },
            },
            ["RelayData"] = new[]
            {
                new MemberDescription { Name = "maxFeePerGas", Type = "uint256" },
                new MemberDescription { Name = "maxPriorityFeePerGas", Type = "uint256" },
                new MemberDescription { Name = "transactionCalldataGasUsed", Type = "uint256" },
                new MemberDescription { Name = "relayWorker", Type = "address" },
                new MemberDescription { Name = "paymaster", Type = "address" },
                new MemberDescription { Name = "forwarder", Type = "address" },
                new MemberDescription { Name = "paymasterData", Type = "bytes" },
                new MemberDescription { Name = "clientId", Type = "uint256" }
            }
        };

        const string primaryType = "RelayRequest";

        var domainSeperator = new Domain
        {
            Name = domainSeparatorName,
            Version = "3",
            ChainId = BigInteger.Parse(chainId),
            VerifyingContract = config.Gsn.ForwarderAddress
        };

        var messageData = new MemberValue[] {
            new() { TypeName = "address", Value = relayRequest.Request.From },
            new() { TypeName = "address", Value = relayRequest.Request.To },
            new() { TypeName = "uint256", Value = relayRequest.Request.Value },
            new() { TypeName = "uint256", Value = relayRequest.Request.Gas },
            new() { TypeName = "uint256", Value = relayRequest.Request.Nonce },
            new() { TypeName = "bytes", Value = relayRequest.Request.Data },
            new() { TypeName = "uint256", Value = relayRequest.Request.ValidUntilTime },
            new() { TypeName = "RelayData", Value = relayRequest.RelayData.ToMap() }
        };

        var data = new TypedData<Domain>
        {
            Types = types,
            PrimaryType = primaryType,
            Domain = domainSeperator,
            Message = messageData
        };

        var signature = wallet.SignTypedData(data);
        return signature;
    }

    public static string GetRelayRequestID(Dictionary<string, object> relayRequest, string signature)
    {
        var parameters = new ABIValue[]
        {
            new("address", ((Dictionary<string, object>)relayRequest["request"])["from"]),
            new("uint256", ((Dictionary<string, object>)relayRequest["request"])["nonce"]),
            new("bytes", ((Dictionary<string, object>)relayRequest["relayData"])["paymasterData"]),
        };

        var hash = Sha3Keccack.Current.CalculateHash(new ABIEncode().GetSha3ABIParamsEncoded(parameters)).ToHex();
        var rawRelayRequestId = hash.PadLeft(64, '0');
        const int prefixSize = 8;
        var prefixedRelayRequestId = rawRelayRequestId[..prefixSize] + new string('0', prefixSize);

        return $"0x{prefixedRelayRequestId}";
    }

    public static async Task<GsnTransactionDetails> GetClaimTx(Account wallet, NetworkConfig config, Web3 client)
    {
        var tx = new TokenFaucetABI.ClaimFunction
        {
            FromAddress = wallet.Address
        };

        var estimatedGas = await client.Eth.GetContractTransactionHandler<TokenFaucetABI.ClaimFunction>().EstimateGasAsync(config.Contracts.TokenFaucet, tx);

        var blockInformation = await client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());
        var maxPriorityFeePerGas = BigInteger.Parse("1500000000");
        var maxFeePerGas = blockInformation.BaseFeePerGas.Value * 2 + maxPriorityFeePerGas;

        return new GsnTransactionDetails(wallet.Address, tx.GetCallData().ToHex(true), config.Contracts.TokenFaucet, maxFeePerGas.ToString("X2"), maxPriorityFeePerGas.ToString("X2"), "0", $"0x{estimatedGas.Value:X2)}");
    }

    public static async Task<string> GetClientId()
    {
        var bundleId = await GetBundleIdFromOS();
        var hexValue = new Nethereum.Hex.HexTypes.HexBigInteger(bundleId).Value.ToString();
        return hexValue;
    }

    public static async Task<string> GetBundleIdFromOS()
    {
        var osBundleId = await UnitySdkPlugin.GetBundleId();
        if (osBundleId == null)
            throw new Exception("Unable to get bundle id from OS");

        return osBundleId;
    }

    public static async Task<string> HandleGsnResponse(HttpResponseMessage response, Web3 ethClient)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent) ?? new Dictionary<string, object>();

        if (responseMap.ContainsKey("error"))
        {
            throw new Exception($"RelayError: {responseMap["error"]}");
        }

        var txHash = $"0x{Sha3Keccack.Current.CalculateHash(responseMap["signedTx"].ToString())}";
        TransactionReceipt receipt;
        do
        {
            receipt = await ethClient.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            await Task.Delay(2000);
        }
        while (receipt == null);

        return txHash;
    }

    public static async Task<BigInteger> GetSenderContractNonce(Web3 client, string tokenAddress, string address)
    {
        try
        {
            return await client.Eth.GetContractQueryHandler<TokenABI.NoncesFunction>().QueryAsync<BigInteger>(tokenAddress, new TokenABI.NoncesFunction { Owner = address });
        }
        catch
        {
            return await client.Eth.GetContractQueryHandler<TokenABI.GetNonceFunction>().QueryAsync<BigInteger>(tokenAddress, new TokenABI.GetNonceFunction { From = address });
        }
    }

    public static BigInteger ParseUnits(string value, int decimals)
    {
        var baseValue = BigInteger.Pow(10, decimals);
        var parts = value.Split('.');
        var wholePart = BigInteger.Parse(parts[0]);
        var fractionalPart = parts.Length > 1 ? BigInteger.Parse(parts[1].PadRight(decimals, '0')) : BigInteger.Zero;

        return wholePart * baseValue + fractionalPart;
    }

    public static double BalanceToDouble(BigInteger value, int decimals)
    {
        var baseValue = BigInteger.Pow(10, decimals);
        return (double)value / (double)baseValue;
    }

    public class CalldataBytes
    {
        public int CalldataZeroBytes { get; set; }
        public int CalldataNonzeroBytes { get; set; }

        public CalldataBytes(int calldataZeroBytes, int calldataNonzeroBytes)
        {
            CalldataZeroBytes = calldataZeroBytes;
            CalldataNonzeroBytes = calldataNonzeroBytes;
        }
    }
}