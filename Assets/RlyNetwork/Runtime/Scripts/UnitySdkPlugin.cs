#nullable enable

using System.Threading.Tasks;

public static class UnitySdkPlugin
{
    static readonly IUnitySdkPlatform _platform;

    static UnitySdkPlugin()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _platform = new AndroidUnitySdkPlugin();
#elif UNITY_IOS && !UNITY_EDITOR
        _platform = new IOSUnitySdkPlugin();
#elif UNITY_EDITOR
        _platform = new EditorUnitySdkPlugin();
#else
        _platform = new NotImplementedUnitySdkPlugin();
#endif
    }

    public static Task<string> GetBundleId() => _platform.GetBundleId();
    public static Task<string?> GetMnemonic() => _platform.GetMnemonic();
    public static Task<string> GenerateNewMnemonic() => _platform.GenerateNewMnemonic();
    public static Task<bool> SaveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure) => _platform.SaveMnemonic(mnemonic, saveToCloud, rejectOnCloudSaveFailure);
    public static Task<bool> DeleteMnemonic() => _platform.DeleteMnemonic();
    public static Task<string> GetPrivateKeyFromMnemonic(string mnemonic) => _platform.GetPrivateKeyFromMnemonic(mnemonic);
}