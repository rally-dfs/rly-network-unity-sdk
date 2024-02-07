#nullable enable

using System;
using System.Threading.Tasks;

using UnityEngine;

public class AndroidUnitySdkPlugin : IUnitySdkPlatform
{
    const string UNITY_SDK_PLUGIN_CLASS = "com.rlynetworkmobilesdk.UnitySdkPlugin";

    class ResultCallback<T> : AndroidJavaProxy
    {
        readonly AndroidUnitySdkPlugin _plugin;
        readonly Action<T> _onSuccess;
        readonly Action<string> _onError;

        public ResultCallback(AndroidUnitySdkPlugin plugin, Action<T> onSuccess, Action<string> onError) : base($"{UNITY_SDK_PLUGIN_CLASS}$ResultCallback")
        {
            _plugin = plugin;
            _onSuccess = onSuccess;
            _onError = onError;
        }

#pragma warning disable IDE1006
        public void onSuccess(T result) => _onSuccess?.Invoke(result);
        public void onError(string error) => _onError?.Invoke(error);
#pragma warning restore IDE1006
    }

    readonly AndroidJavaObject _currentActivity;
    readonly AndroidJavaObject _pluginInstance;

    public AndroidUnitySdkPlugin()
    {
        if (Application.platform != RuntimePlatform.Android)
            throw new InvalidOperationException("AndroidUnitySdkPlugin can only be used on Android");

        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        using var pluginClass = new AndroidJavaClass(UNITY_SDK_PLUGIN_CLASS);
        _pluginInstance = new AndroidJavaObject(UNITY_SDK_PLUGIN_CLASS, _currentActivity);
    }

    public Task<string> GetBundleId()
    {
        var tcs = new TaskCompletionSource<string>();

        _pluginInstance.Call("getBundleId", new ResultCallback<string>(this, bundleId => tcs.SetResult(bundleId), err => tcs.SetException(new Exception(err))));

        return tcs.Task;
    }

    public Task<string?> GetMnemonic()
    {
        var tcs = new TaskCompletionSource<string?>();

        _pluginInstance.Call("getMnemonic", new ResultCallback<string?>(this, mnemonic => tcs.SetResult(mnemonic), err => tcs.SetException(new Exception(err))));

        return tcs.Task;
    }

    public Task<string> GenerateNewMnemonic()
    {
        var tcs = new TaskCompletionSource<string>();

        _pluginInstance.Call("generateNewMnemonic", new ResultCallback<string>(this, mnemonic => tcs.SetResult(mnemonic), err => tcs.SetException(new Exception(err))));

        return tcs.Task;
    }

    public Task<bool> SaveMnemonic(string mnemonic, bool saveToCloud, bool rejectOnCloudSaveFailure)
    {
        var tcs = new TaskCompletionSource<bool>();

        _pluginInstance.Call("saveMnemonic", mnemonic, saveToCloud, rejectOnCloudSaveFailure, new ResultCallback<bool>(this, result => tcs.SetResult(result), err => tcs.SetException(new Exception(err))));

        return tcs.Task;
    }

    public Task<bool> DeleteMnemonic()
    {
        var tcs = new TaskCompletionSource<bool>();

        _pluginInstance.Call("deleteMnemonic", new ResultCallback<bool>(this, result => tcs.SetResult(result), err => tcs.SetException(new Exception(err))));

        return tcs.Task;
    }

    public Task<string> GetPrivateKeyFromMnemonic(string mnemonic)
    {
        var tcs = new TaskCompletionSource<string>();

        _pluginInstance.Call("getPrivateKeyFromMnemonic", mnemonic, new ResultCallback<string>(this, result => tcs.SetResult(result), err => tcs.SetException(new Exception(err))));

        return tcs.Task;
    }
}