using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DevWorkbench
{
    public static class FrameworkLoader
    {
        public static async UniTask<T> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException($"Invalid address: {address}");
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.ToUniTask();
            if (handle.Status != AsyncOperationStatus.Succeeded) throw new Exception($"Load failed: {address}");
            var result = UnityEngine.Object.Instantiate(handle.Result);
            Addressables.Release(handle);
            return result;
        }

        public static T LoadSync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException($"Invalid address: {address}");
            var handle = Addressables.LoadAssetAsync<T>(address);
            handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded) throw new Exception($"Load failed: {address}");
            var result = UnityEngine.Object.Instantiate(handle.Result);
            Addressables.Release(handle);
            return result;
        }
    }
}
