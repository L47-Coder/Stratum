using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Stratum
{
    internal sealed class GameBootstrap : IAsyncStartable
    {
        private readonly IObjectResolver _container;

        public GameBootstrap(IObjectResolver container) => _container = container;

        public async UniTask StartAsync(CancellationToken token)
        {
            var managers = _container.Resolve<IReadOnlyList<IManager>>();
            foreach (var manager in managers)
                if (manager is IAsyncInitManager init) await init.InitAsync(token);

            var boot = ResolveGameBoot();
            if (boot == null) return;
            _container.Inject(boot);
            await boot.OnGameStart();
        }

        private static IGameBoot ResolveGameBoot()
        {
            var boots = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
                .OfType<IGameBoot>().ToList();
            if (boots.Count == 0) { Debug.LogWarning("[Stratum] No IGameBoot found; OnGameStart skipped."); return null; }
            if (boots.Count > 1) throw new InvalidOperationException($"[Stratum] {boots.Count} IGameBoot found; only one allowed.");
            return boots[0];
        }
    }
}
