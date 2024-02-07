#nullable enable

using System.Threading.Tasks;

public interface IUnitySdkPlatform
{
    Task<string> GetBundleId();
    Task<string?> GetMnemonic();
    Task<string> GenerateNewMnemonic();
    Task<bool> SaveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure);
    Task<bool> DeleteMnemonic();
    Task<string> GetPrivateKeyFromMnemonic(string mnemonic);
}