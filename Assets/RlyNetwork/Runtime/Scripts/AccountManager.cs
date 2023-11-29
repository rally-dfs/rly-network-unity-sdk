#nullable enable

using System;
using System.Threading.Tasks;

using Nethereum.Web3.Accounts;

public class AccountManager
{
    static Account? _cachedWallet;

    static readonly AccountManager _instance = new();

    private AccountManager()
    {
    }

    public static AccountManager GetInstance()
    {
        return _instance;
    }

    public async Task<Account> CreateAccount(bool overwrite = false, (bool saveToCloud, bool rejectOnCloudSaveFailure)? storageOptions = null)
    {
        var existingWallet = await GetAccount();
        if (existingWallet != null && !overwrite)
        {
            throw new InvalidOperationException("Account already exists");
        }

        storageOptions ??= new(true, true);

        var mnemonic = await UnitySdkPlugin.GenerateNewMnemonic() ?? throw new InvalidOperationException("Unable to generate mnemonic, something went wrong at native code layer");
        await UnitySdkPlugin.SaveMnemonic(mnemonic, storageOptions.Value.saveToCloud, storageOptions.Value.rejectOnCloudSaveFailure);
        var newWallet = await MakeAccountFromMnemonic(mnemonic);

        _cachedWallet = newWallet;
        return newWallet;
    }

    public async Task<Account?> GetAccount()
    {
        if (_cachedWallet != null)
        {
            return _cachedWallet;
        }

        var mnemonic = await UnitySdkPlugin.GetMnemonic();

        if (mnemonic == null)
        {
            return null;
        }

        var wallet = await MakeAccountFromMnemonic(mnemonic);

        _cachedWallet = wallet;
        return wallet;
    }

    public async Task<string?> GetPublicAddress()
    {
        var wallet = await GetAccount();
        return wallet?.Address;
    }

    public async Task PermanentlyDeleteAccount()
    {
        await UnitySdkPlugin.DeleteMnemonic();
        _cachedWallet = null;
    }

    public async Task<string?> GetAccountPhrase()
    {
        try
        {
            return await UnitySdkPlugin.GetMnemonic();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<Account> MakeAccountFromMnemonic(string mnemonic)
    {
        var privateKey = await UnitySdkPlugin.GetPrivateKeyFromMnemonic(mnemonic);
        return new Account(privateKey);
    }
}
