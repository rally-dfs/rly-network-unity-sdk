#nullable enable

using System;
using System.Threading.Tasks;

public class NotImplementedUnitySdkPlugin : IUnitySdkPlatform
{
    public Task<string> GetBundleId() => throw new NotImplementedException();
    public Task<string?> GetMnemonic() => throw new NotImplementedException();
    public Task<string> GenerateNewMnemonic() => throw new NotImplementedException();
    public Task<bool> SaveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure) => throw new NotImplementedException();
    public Task<bool> DeleteMnemonic() => throw new NotImplementedException();
    public Task<string> GetPrivateKeyFromMnemonic(string mnemonic) => throw new NotImplementedException();
}