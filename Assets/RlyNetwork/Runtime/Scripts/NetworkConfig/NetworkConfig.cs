#nullable enable

public partial class NetworkConfig
{
    public Contracts Contracts { get; }
    public GSNConfig Gsn { get; }
    public string? RelayerApiKey { get; set; }

    public NetworkConfig(Contracts contracts, GSNConfig gsn, string? relayerApiKey = null)
    {
        Contracts = contracts;
        Gsn = gsn;
        RelayerApiKey = relayerApiKey;
    }

    public override string ToString()
    {
        return $"NetworkConfig{{contracts: {Contracts.ToString()}, gsn: {Gsn.ToString()}, relayerApiKey: {RelayerApiKey}}}";
    }
}

public class Contracts
{
    public string TokenFaucet { get; }
    public string RlyERC20 { get; }

    public Contracts(string tokenFaucet, string rlyERC20)
    {
        TokenFaucet = tokenFaucet;
        RlyERC20 = rlyERC20;
    }

    public override string ToString()
    {
        return $"Contracts{{tokenFaucet: {TokenFaucet}, rlyERC20: {RlyERC20}}}";
    }
}

public class GSNConfig
{
    public string PaymasterAddress { get; }
    public string ForwarderAddress { get; }
    public string RelayHubAddress { get; }
    public string RelayWorkerAddress { get; set; }
    public string RelayUrl { get; }
    public string RpcUrl { get; }
    public string ChainId { get; }
    public string MaxAcceptanceBudget { get; }
    public string DomainSeparatorName { get; }
    public int GtxDataZero { get; }
    public int GtxDataNonZero { get; }
    public int RequestValidSeconds { get; }
    public int MaxPaymasterDataLength { get; }
    public int MaxApprovalDataLength { get; }
    public int MaxRelayNonceGap { get; }

    public GSNConfig(string paymasterAddress, string forwarderAddress, string relayHubAddress, string relayWorkerAddress, string relayUrl, string rpcUrl, string chainId, string maxAcceptanceBudget, string domainSeparatorName, int gtxDataZero, int gtxDataNonZero, int requestValidSeconds, int maxPaymasterDataLength, int maxApprovalDataLength, int maxRelayNonceGap)
    {
        PaymasterAddress = paymasterAddress;
        ForwarderAddress = forwarderAddress;
        RelayHubAddress = relayHubAddress;
        RelayWorkerAddress = relayWorkerAddress;
        RelayUrl = relayUrl;
        RpcUrl = rpcUrl;
        ChainId = chainId;
        MaxAcceptanceBudget = maxAcceptanceBudget;
        DomainSeparatorName = domainSeparatorName;
        GtxDataZero = gtxDataZero;
        GtxDataNonZero = gtxDataNonZero;
        RequestValidSeconds = requestValidSeconds;
        MaxPaymasterDataLength = maxPaymasterDataLength;
        MaxApprovalDataLength = maxApprovalDataLength;
        MaxRelayNonceGap = maxRelayNonceGap;
    }

    public override string ToString()
    {
        return $"GSNConfig{{paymasterAddress: {PaymasterAddress}, forwarderAddress: {ForwarderAddress}, relayHubAddress: {RelayHubAddress}, relayWorkerAddress: {RelayWorkerAddress}, relayUrl: {RelayUrl}, rpcUrl: {RpcUrl}, chainId: {ChainId}, maxAcceptanceBudget: {MaxAcceptanceBudget}, domainSeparatorName: {DomainSeparatorName}, gtxDataZero: {GtxDataZero}, gtxDataNonZero: {GtxDataNonZero}, requestValidSeconds: {RequestValidSeconds}, maxPaymasterDataLength: {MaxPaymasterDataLength}, maxApprovalDataLength: {MaxApprovalDataLength}, maxRelayNonceGap: {MaxRelayNonceGap}}}";
    }
}
