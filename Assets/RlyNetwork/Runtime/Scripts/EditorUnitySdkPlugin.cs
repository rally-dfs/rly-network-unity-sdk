#nullable enable

using System.Threading.Tasks;

using NBitcoin;

using Nethereum.HdWallet;

using UnityEngine;

public class EditorUnitySdkPlugin : IUnitySdkPlatform
{
    const string MnemonicKey = "mnemonic";

    public Task<string> GetBundleId()
    {
        var bundleId = Application.identifier;
        return Task.FromResult(bundleId);
    }

    public Task<string?> GetMnemonic()
    {
        var mnemonic = PlayerPrefs.GetString(MnemonicKey);
        return Task.FromResult(string.IsNullOrWhiteSpace(mnemonic) ? null : mnemonic);
    }

    public Task<string> GenerateNewMnemonic()
    {
        var newMnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve).ToString();
        return Task.FromResult(newMnemonic);
    }

    public Task<bool> SaveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure)
    {
        PlayerPrefs.SetString(MnemonicKey, mnemonic);
        PlayerPrefs.Save();

        return Task.FromResult(true);
    }

    public Task<bool> DeleteMnemonic()
    {
        PlayerPrefs.DeleteKey(MnemonicKey);
        PlayerPrefs.Save();

        return Task.FromResult(true);
    }

    public Task<string> GetPrivateKeyFromMnemonic(string mnemonic)
    {
        var wallet = new Wallet(mnemonic, null);
        var account = wallet.GetAccount(0);
        var privateKey = account.PrivateKey;

        return Task.FromResult(privateKey);
    }
}