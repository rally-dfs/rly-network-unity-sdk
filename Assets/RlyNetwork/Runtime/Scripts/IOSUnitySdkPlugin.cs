#nullable enable

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

using System.Threading.Tasks;

public class IOSUnitySdkPlugin : IUnitySdkPlatform
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern string getBundleId();

    [DllImport("__Internal")]
    static extern string? getMnemonic();

    [DllImport("__Internal")]
    static extern string generateMnemonic();

    [DllImport("__Internal")]
    static extern bool saveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure);

    [DllImport("__Internal")]
    static extern bool deleteMnemonic();

    [DllImport("__Internal")]
    static extern string getPrivateKeyFromMnemonic(string mnemonic);
#else
#pragma warning disable IDE0060, IDE1006
    static string getBundleId() => string.Empty;
    static string? getMnemonic() => null;
    static string generateMnemonic() => string.Empty;
    static bool saveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure) => false;
    static bool deleteMnemonic() => false;
    static string getPrivateKeyFromMnemonic(string mnemonic) => string.Empty;
#pragma warning restore IDE0060, IDE1006
#endif

    public Task<string> GetBundleId() => Task.FromResult(getBundleId());

    public Task<string?> GetMnemonic() => Task.FromResult(getMnemonic());

    public Task<string> GenerateNewMnemonic() => Task.FromResult(generateMnemonic());

    public Task<bool> SaveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure) => Task.FromResult(saveMnemonic(mnemonic, saveToCloud, rejectOnCloudSaveFailure));

    public Task<bool> DeleteMnemonic() => Task.FromResult(deleteMnemonic());

    public Task<string> GetPrivateKeyFromMnemonic(string mnemonic) => Task.FromResult(getPrivateKeyFromMnemonic(mnemonic));
}
