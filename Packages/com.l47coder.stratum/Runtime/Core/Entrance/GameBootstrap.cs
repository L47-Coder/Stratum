using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DevWorkbench
{
    public interface IAsyncInitManager
    {
        UniTask InitAsync(CancellationToken token);
    }

    internal class GameBootstrap : IAsyncStartable
    {
        private readonly IObjectResolver _container;

        public GameBootstrap(IObjectResolver container) => _container = container;

        public async UniTask StartAsync(CancellationToken token)
        {
            var managers = _container.Resolve<IReadOnlyList<BaseManager>>();

            foreach (var manager in managers)
                await manager.InternalSetManagerDataDict();

            foreach (var manager in managers)
            {
                if (manager is IAsyncInitManager init)
                    await init.InitAsync(token);
            }

            var gameBoot = ResolveGameBoot();
            if (gameBoot == null) return;

            _container.Inject(gameBoot);
            await gameBoot.OnGameStart();
        }

        private static IGameBoot ResolveGameBoot()
        {
            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

            IGameBoot first = null;
            int count = 0;

            foreach (var b in behaviours)
            {
                if (b is not IGameBoot boot) continue;
                first ??= boot;
                count++;
            }

            if (count == 0)
            {
                Debug.LogWarning("[DevWorkbench] No IGameBoot found in scene; OnGameStart will not be called.");
                return null;
            }

            if (count > 1)
                throw new InvalidOperationException(
                    $"[DevWorkbench] {count} IGameBoot implementations found in the scene. " +
                    "Only one is allowed. Remove the extra instances before entering Play Mode.");

            return first;
        }
    }
}
