using System;
using Unity.Netcode;
using UnityEngine;
using Zenject;

public class NetworkInstaller : MonoInstaller
{
    [SerializeField] private NetworkManager networkManagerPrefab;
    [SerializeField] private PingChecker pingCheckerPrefab;

    private NetworkManager manager => NetworkManager.Singleton;

    public override void InstallBindings()
    {
        var manager = Instantiate(networkManagerPrefab);
        if (!manager.NetworkConfig.Prefabs.Contains(pingCheckerPrefab.gameObject))
            manager.AddNetworkPrefab(pingCheckerPrefab.gameObject);

        manager.OnServerStarted += SpawnPingChecker;

        var scenes = new NetworkSceneController();
        var session = new NetworkSession(scenes);

        Container.BindInstance<PingService>(new PingService());

        Container.BindInstance<NetworkSession>(session);
        Container.BindInstance<IInitializable>(session);
        Container.BindInstance<IDisposable>(session);

        Container.BindInstance<NetworkSceneController>(scenes);
        Container.BindInstance<IInitializable>(scenes);
        Container.BindInstance<ITickable>(scenes);
        Container.BindInstance<IDisposable>(scenes);
    }

    private void SpawnPingChecker()
    {
        var pingChecker = Instantiate(pingCheckerPrefab);
        pingChecker.NetworkObject.Spawn(destroyWithScene: false);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnServerStarted -= SpawnPingChecker;
    }
}