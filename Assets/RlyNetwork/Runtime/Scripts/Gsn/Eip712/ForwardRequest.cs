#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

using Nethereum.Hex.HexConvertors.Extensions;

public class ForwardRequest
{
    public string From { get; }
    public string To { get; }
    public string Value { get; }
    public string Gas { get; }
    public string Nonce { get; }
    public string Data { get; }
    public string ValidUntilTime { get; }

    public ForwardRequest(string from, string to, string value, string gas, string nonce, string data, string validUntilTime)
    {
        From = from.ToLowerInvariant();
        To = to.ToLowerInvariant();
        Value = value;
        Gas = Convert.ToInt32(gas, 16).ToString();
        Nonce = nonce;
        Data = data;
        ValidUntilTime = validUntilTime;
    }

    public List<object> ToJson()
    {
        return new List<object>
        {
            From,
            To,
            BigInteger.Parse(Value),
            BigInteger.Parse(Gas),
            BigInteger.Parse(Nonce),
            Data.HexToByteArray(),
            BigInteger.Parse(ValidUntilTime)
        };
    }

    public Dictionary<string, object> ToMap()
    {
        return new Dictionary<string, object>
        {
            { "from", From },
            { "to", To },
            { "value", Value },
            { "gas", Gas },
            { "nonce", Nonce },
            { "data", Data },
            { "validUntilTime", ValidUntilTime }
        };
    }
}
