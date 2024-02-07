#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;

using Nethereum.Contracts.Standards.ERC20;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

public class NetworkImpl : INetwork
{
    private readonly NetworkConfig _network;
    private Account? _account;

    async Task<Account> AccountOrThrow()
    {
        if (_account == null)
        {
            _account = await AccountManager.GetInstance().GetAccount();

            if (_account == null)
                throw new InvalidOperationException("Account does not exist");
        }

        return _account;
    }

    public NetworkImpl(NetworkConfig network)
    {
        _network = network;
    }

    public INetwork WithAccount(Account account)
    {
        _account = account;
        return this;
    }

    public INetwork WithApiKey(string apiKey)
    {
        _network.RelayerApiKey = apiKey;
        return this;
    }

    public async Task<Web3> GetClient()
    {
        var account = await AccountOrThrow();

        return account.GetEthClient(_network);
    }

    public async Task<string> ClaimRly()
    {
        var account = await AccountOrThrow();

        var existingBalance = (BigInteger)await GetBalance();
        if (existingBalance > 0)
        {
            throw new PriorDustingException();
        }

        var ethers = account.GetEthClient(_network);

        var claimTx = await GnsTxHelper.GetClaimTx(account, _network, ethers);

        return await Relay(claimTx);
    }

    public async Task<object> GetBalance(string? tokenAddress = null, bool humanReadable = false)
    {
        var account = await AccountOrThrow();

        tokenAddress ??= _network.Contracts.RlyERC20;

        var provider = account.GetEthClient(_network);

        var token = new ERC20Service(provider.Eth).GetContractService(tokenAddress);

        var balanceOfCall = await token.BalanceOfQueryAsync(account.Address);

        var balance = balanceOfCall;

        if (!humanReadable)
        {
            return balance;
        }

        var decimals = await DecimalsForToken(token);
        return GnsTxHelper.BalanceToDouble(balance, (int)decimals);
    }

    public async Task<double> GetDisplayBalance(string? tokenAddress = null)
    {
        var balance = await GetBalance(tokenAddress, true);
        return (double)balance;
    }

    public async Task<BigInteger> GetExactBalance(string? tokenAddress = null, bool humanReadable = false)
    {
        var balance = await GetBalance(tokenAddress, false);
        return (BigInteger)balance;
    }

    public async Task<string> Relay(GsnTransactionDetails tx)
    {
        var account = await AccountOrThrow();

        return await GsnClient.RelayTransaction(account, _network, tx);
    }

    public void SetApiKey(string apiKey)
    {
        _network.RelayerApiKey = apiKey;
    }

    public async Task<string> Transfer(string destinationAddress, double amount, MetaTxMethod metaTxMethod, string? tokenAddress = null)
    {
        var account = await AccountOrThrow();

        tokenAddress ??= _network.Contracts.RlyERC20;

        var sourceBalance = (BigInteger)await GetBalance(tokenAddress);

        var sourceFinalBalance = sourceBalance - new BigInteger(amount);

        if (sourceFinalBalance < 0)
        {
            throw new InsufficientBalanceException();
        }

        var provider = account.GetEthClient(_network);

        var token = new ERC20Service(provider.Eth).GetContractService(tokenAddress);

        var decimals = await DecimalsForToken(token);
        var decimalAmount = GnsTxHelper.ParseUnits(amount.ToString(), (int)decimals);

        return await TransferExact(destinationAddress, decimalAmount, metaTxMethod, tokenAddress);
    }

    public async Task<string> TransferExact(string destinationAddress, BigInteger amount, MetaTxMethod metaTxMethod, string? tokenAddress = null)
    {
        var account = await AccountOrThrow();

        tokenAddress ??= _network.Contracts.RlyERC20;

        var sourceBalance = await GetExactBalance(tokenAddress);

        var sourceFinalBalance = sourceBalance - amount;

        if (sourceFinalBalance < BigInteger.Zero)
        {
            throw new InsufficientBalanceException();
        }

        var provider = account.GetEthClient(_network);

        GsnTransactionDetails transferTx;

        if (metaTxMethod == MetaTxMethod.Permit)
        {
            transferTx = await Permit.GetPermitTx(account, destinationAddress, amount, _network, tokenAddress, provider);
        }
        else
        {
            transferTx = await MetaTransaction.GetExecuteMetatransactionTx(account, destinationAddress, amount, _network, tokenAddress, provider);
        }

        return await Relay(transferTx);
    }

    public async Task<string> RegisterAccount()
    {
        return await ClaimRly();
    }

    public async Task<string> SimpleTransfer(string destinationAddress, double amount, string? tokenAddress = null, MetaTxMethod? metaTxMethod = null)
    {
        var account = await AccountOrThrow();

        var client = account.GetEthClient(_network);

        var transaction = new TransactionInput
        {
            To = destinationAddress,
            GasPrice = new HexBigInteger(1000000),
            Value = new HexBigInteger(BigInteger.Parse("3"))
        };

        return await client.Eth.Transactions.SendTransaction.SendRequestAsync(transaction);
    }

    private async Task<BigInteger> DecimalsForToken(ERC20ContractService token)
    {
        return await token.DecimalsQueryAsync();
    }
}
