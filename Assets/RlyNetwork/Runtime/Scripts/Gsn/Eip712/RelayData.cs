#nullable enable

using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexConvertors.Extensions;

public class RelayData
{
    public string MaxFeePerGas { get; set; }
    public string MaxPriorityFeePerGas { get; set; }
    public string TransactionCalldataGasUsed { get; set; }
    public string RelayWorker { get; set; }
    public string Paymaster { get; set; }
    public string Forwarder { get; set; }
    public string PaymasterData { get; set; }
    public string ClientId { get; set; }

    public RelayData(string maxFeePerGas, string maxPriorityFeePerGas, string transactionCalldataGasUsed, string relayWorker, string paymaster, string forwarder, string paymasterData, string clientId)
    {
        MaxFeePerGas = maxFeePerGas;
        MaxPriorityFeePerGas = maxPriorityFeePerGas;
        TransactionCalldataGasUsed = transactionCalldataGasUsed;
        RelayWorker = relayWorker;
        Paymaster = paymaster;
        Forwarder = forwarder;
        PaymasterData = paymasterData;
        ClientId = clientId;
    }

    public List<object> ToJson()
    {
        return new List<object>
        {
            BigInteger.Parse(MaxFeePerGas),
            BigInteger.Parse(MaxPriorityFeePerGas),
            BigInteger.Parse(TransactionCalldataGasUsed),
            RelayWorker,
            Paymaster,
            Forwarder,
            PaymasterData.HexToByteArray(),
            BigInteger.Parse(ClientId)
        };
    }

    public Dictionary<string, object> ToMap()
    {
        return new Dictionary<string, object>
        {
            { "maxFeePerGas", MaxFeePerGas },
            { "maxPriorityFeePerGas", MaxPriorityFeePerGas },
            { "transactionCalldataGasUsed", TransactionCalldataGasUsed },
            { "relayWorker", RelayWorker },
            { "paymaster", Paymaster },
            { "forwarder", Forwarder },
            { "paymasterData", PaymasterData },
            { "clientId", ClientId }
        };
    }

    public MemberValue[] ToEip712Values()
    {
        return new MemberValue[]
        {
            new() { TypeName = "uint256", Value = MaxFeePerGas },
            new() { TypeName = "uint256", Value = MaxPriorityFeePerGas },
            new() { TypeName = "uint256", Value = TransactionCalldataGasUsed },
            new() { TypeName = "address", Value = RelayWorker },
            new() { TypeName = "address", Value = Paymaster },
            new() { TypeName = "address", Value = Forwarder },
            new() { TypeName = "bytes", Value = PaymasterData },
            new() { TypeName = "uint256", Value = ClientId }
        };
    }
}
