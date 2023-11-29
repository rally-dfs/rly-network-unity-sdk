#nullable enable

using System.Collections.Generic;
using System.Numerics;

using Nethereum.Hex.HexConvertors.Extensions;

public class ForwardRequest
{
    public string From { get; set; }
    public string To { get; set; }
    public string Value { get; set; }
    public string Gas { get; set; }
    public string Nonce { get; set; }
    public string Data { get; set; }
    public string ValidUntilTime { get; set; }

    public ForwardRequest(string from, string to, string value, string gas, string nonce, string data, string validUntilTime)
    {
        From = from;
        To = to;
        Value = value;
        Gas = gas;
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
