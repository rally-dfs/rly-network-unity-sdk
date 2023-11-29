#nullable enable

using System;
using System.Numerics;

using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

public static class TokenABI
{
    [Function("nonces", "uint256")]
    public class NoncesFunction : FunctionMessage
    {
        [Parameter("address", "owner", 1)]
        public virtual string Owner { get; set; } = string.Empty;
    }

    [Function("getNonce", "uint256")]
    public class GetNonceFunction : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; } = string.Empty;
    }
}

public static class TokenFaucetABI
{
    public partial class ClaimFunction : ClaimFunctionBase { }

    [Function("claim", "bool")]
    public class ClaimFunctionBase : FunctionMessage
    {

    }
}

public static class IForwarderABI
{
    public partial class GetNonceFunction : GetNonceFunctionBase { }

    [Function("getNonce", "uint256")]
    public class GetNonceFunctionBase : FunctionMessage
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; } = string.Empty;
    }
}

public static class IRelayHubABI
{
    public partial class RelayCallFunction : RelayCallFunctionBase { }

    [Function("relayCall", typeof(RelayCallOutputDTO))]
    public class RelayCallFunctionBase : FunctionMessage
    {
        [Parameter("string", "domainSeparatorName", 1)]
        public virtual string DomainSeparatorName { get; set; } = string.Empty;
        [Parameter("uint256", "maxAcceptanceBudget", 2)]
        public virtual BigInteger MaxAcceptanceBudget { get; set; }
        [Parameter("tuple", "relayRequest", 3)]
        public virtual RelayRequest RelayRequest { get; set; } = new RelayRequest();
        [Parameter("bytes", "signature", 4)]
        public virtual byte[] Signature { get; set; } = Array.Empty<byte>();
        [Parameter("bytes", "approvalData", 5)]
        public virtual byte[] ApprovalData { get; set; } = Array.Empty<byte>();
    }

    public partial class RelayCallOutputDTO : RelayCallOutputDTOBase { }

    [FunctionOutput]
    public class RelayCallOutputDTOBase : IFunctionOutputDTO
    {
        [Parameter("bool", "paymasterAccepted", 1)]
        public virtual bool PaymasterAccepted { get; set; }
        [Parameter("uint256", "charge", 2)]
        public virtual BigInteger Charge { get; set; }
        [Parameter("uint8", "status", 3)]
        public virtual byte Status { get; set; }
        [Parameter("bytes", "returnValue", 4)]
        public virtual byte[] ReturnValue { get; set; } = Array.Empty<byte>();
    }

    public partial class ForwardRequest : ForwardRequestBase { }

    public class ForwardRequestBase
    {
        [Parameter("address", "from", 1)]
        public virtual string From { get; set; } = string.Empty;
        [Parameter("address", "to", 2)]
        public virtual string To { get; set; } = string.Empty;
        [Parameter("uint256", "value", 3)]
        public virtual BigInteger Value { get; set; }
        [Parameter("uint256", "gas", 4)]
        public virtual BigInteger Gas { get; set; }
        [Parameter("uint256", "nonce", 5)]
        public virtual BigInteger Nonce { get; set; }
        [Parameter("bytes", "data", 6)]
        public virtual byte[] Data { get; set; } = Array.Empty<byte>();
        [Parameter("uint256", "validUntilTime", 7)]
        public virtual BigInteger ValidUntilTime { get; set; }
    }

    public partial class RelayData : RelayDataBase { }

    public class RelayDataBase
    {
        [Parameter("uint256", "maxFeePerGas", 1)]
        public virtual BigInteger MaxFeePerGas { get; set; }
        [Parameter("uint256", "maxPriorityFeePerGas", 2)]
        public virtual BigInteger MaxPriorityFeePerGas { get; set; }
        [Parameter("uint256", "transactionCalldataGasUsed", 3)]
        public virtual BigInteger TransactionCalldataGasUsed { get; set; }
        [Parameter("address", "relayWorker", 4)]
        public virtual string RelayWorker { get; set; } = string.Empty;
        [Parameter("address", "paymaster", 5)]
        public virtual string Paymaster { get; set; } = string.Empty;
        [Parameter("address", "forwarder", 6)]
        public virtual string Forwarder { get; set; } = string.Empty;
        [Parameter("bytes", "paymasterData", 7)]
        public virtual byte[] PaymasterData { get; set; } = Array.Empty<byte>();
        [Parameter("uint256", "clientId", 8)]
        public virtual BigInteger ClientId { get; set; }
    }

    public partial class RelayRequest : RelayRequestBase { }

    public class RelayRequestBase
    {
        [Parameter("tuple", "request", 1)]
        public virtual ForwardRequest Request { get; set; } = new ForwardRequest();
        [Parameter("tuple", "relayData", 2)]
        public virtual RelayData RelayData { get; set; } = new RelayData();
    }
}