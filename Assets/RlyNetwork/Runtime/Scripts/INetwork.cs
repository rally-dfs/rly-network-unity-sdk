#nullable enable

using System.Numerics;
using System.Threading.Tasks;

using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public interface INetwork
{
    INetwork WithAccount(Account account);
    INetwork WithApiKey(string apiKey);
    Task<Web3> GetClient();
    Task<object> GetBalance(string? tokenAddress = null, bool humanReadable = false);
    Task<double> GetDisplayBalance(string? tokenAddress = null);
    Task<BigInteger> GetExactBalance(string? tokenAddress = null, bool humanReadable = false);
    Task<string> Transfer(string destinationAddress, double amount, MetaTxMethod metaTxMethod, string? tokenAddress = null);
    Task<string> TransferExact(string destinationAddress, BigInteger amount, MetaTxMethod metaTxMethod, string? tokenAddress = null);
    Task<string> SimpleTransfer(string destinationAddress, double amount, string? tokenAddress = null, MetaTxMethod? metaTxMethod = null);
    Task<string> ClaimRly();
    Task<string> RegisterAccount();
    Task<string> Relay(GsnTransactionDetails tx);
    void SetApiKey(string apiKey);
}

public static class NetworkProvider
{
    public static readonly INetwork RlyMumbai = new NetworkImpl(NetworkConfigProvider.Mumbai);
    public static readonly INetwork RlyLocal = new NetworkImpl(NetworkConfigProvider.Local);
    public static readonly INetwork RlyPolygon = new NetworkImpl(NetworkConfigProvider.Polygon);
}