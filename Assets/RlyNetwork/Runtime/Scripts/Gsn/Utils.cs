#nullable enable

using System;

using Nethereum.ABI.EIP712;
using Nethereum.ABI.FunctionEncoding.Attributes;

public enum RlyEnv
{
    Local,
}

public enum MetaTxMethod
{
    Permit,
    ExecuteMetaTransaction,
}

public class GsnTransactionDetails
{
    string? gas;

    public string From { get; }
    public string Data { get; }
    public string To { get; }
    public string? Value { get; }
    public string? Gas { get => gas; set => gas = value?.ToLowerInvariant(); }
    public string MaxFeePerGas { get; set; }
    public string MaxPriorityFeePerGas { get; set; }
    public string? PaymasterData { get; }
    public string? ClientId { get; }
    public bool? UseGsn { get; }

    public GsnTransactionDetails(string from, string data, string to, string maxFeePerGas, string maxPriorityFeePerGas, string? value = null, string? gas = null, string? paymasterData = null, string? clientId = null, bool? useGsn = null)
    {
        From = from.ToLowerInvariant();
        Data = data;
        To = to.ToLowerInvariant();
        Value = value;
        Gas = gas;
        MaxFeePerGas = maxFeePerGas;
        MaxPriorityFeePerGas = maxPriorityFeePerGas;
        PaymasterData = paymasterData;
        ClientId = clientId;
        UseGsn = useGsn;
    }

    public override string ToString()
    {
        return $"from: {From}, data: {Data}, to: {To}, value: {Value}, gas: {Gas}, maxFeePerGas: {MaxFeePerGas}, maxPriorityFeePerGas: {MaxPriorityFeePerGas}, paymasterData: {PaymasterData}, clientId: {ClientId}, useGSN: {UseGsn}";
    }
}

[Struct("EIP712Domain")]
public class DomainWithChainIdString : IDomain
{
    [Parameter("string", "name", 1)]
    public virtual string Name { get; set; } = string.Empty;

    [Parameter("string", "version", 2)]
    public virtual string Version { get; set; } = string.Empty;

    [Parameter("uint256", "chainId", 3)]
    public virtual string ChainId { get; set; } = string.Empty;

    [Parameter("address", "verifyingContract", 4)]
    public virtual string VerifyingContract { get; set; } = string.Empty;
}

[Struct("EIP712Domain")]
public class DomainWithoutChainIdButSalt : IDomain
{
    [Parameter("string", "name", 1)]
    public virtual string Name { get; set; } = string.Empty;

    [Parameter("string", "version", 2)]
    public virtual string Version { get; set; } = string.Empty;

    [Parameter("address", "verifyingContract", 3)]
    public virtual string VerifyingContract { get; set; } = string.Empty;

    [Parameter("bytes32", "salt", 4)]
    public virtual byte[] Salt { get; set; } = Array.Empty<byte>();
}