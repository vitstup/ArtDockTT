using Unity.Netcode;
using UnityEngine;
using Zenject;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;

    [Inject] private NetworkSession session;
    [Inject] private NetworkSceneController scenes;

    private NetworkManager manager => NetworkManager.Singleton;

    private void Start()
    {
        session.Changed += SpawnPlayers;
        scenes.Changed += SpawnPlayers;
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        if (manager == null || !manager.IsHost || manager.ShutdownInProgress
            || scenes.Status != SessionStatus.InGame)
            return;

        foreach (var clientId in manager.ConnectedClientsIds)
        {
            if (!session.IsPlayerReady(clientId) || !manager.ConnectedClients.TryGetValue(clientId, out var client)
                || client.PlayerObject != null)
                continue;

            var position = new Vector3(2f * (clientId % 5), 1f, 2f * ((clientId / 5) % 5));
            var player = Instantiate(playerPrefab, position, Quaternion.identity);
            player.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }
    }

    private void OnDestroy()
    {
        if (session != null)
            session.Changed -= SpawnPlayers;

        if (scenes != null)
            scenes.Changed -= SpawnPlayers;
    }
}