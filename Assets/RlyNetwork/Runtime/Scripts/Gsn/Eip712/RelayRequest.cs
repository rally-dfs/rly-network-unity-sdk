#nullable enable

using System.Collections.Generic;

public class RelayRequest
{
    public ForwardRequest Request { get; set; }
    public RelayData RelayData { get; set; }

    public RelayRequest(ForwardRequest request, RelayData relayData)
    {
        Request = request;
        RelayData = relayData;
    }

    public List<object> ToJson()
    {
        return new List<object> { Request.ToJson(), RelayData.ToJson() };
    }

    public Dictionary<string, object> ToMap()
    {
        return new Dictionary<string, object>
        {
            { "request", Request.ToMap() },
            { "relayData", RelayData.ToMap() }
        };
    }
}
