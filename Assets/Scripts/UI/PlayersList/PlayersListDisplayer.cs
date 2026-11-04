using System;
using Unity.Netcode;
using UnityEngine;
using Zenject;

public class PlayersListDisplayer : MonoBehaviour
{
    private const float PingRefreshInterval = 0.5f;

    [SerializeField] private PlayerDisplayer[] displayers;

    [Inject] private PingService pingService;

    private NetworkManager manager => NetworkManager.Singleton;

    private float nextPingRefreshTime;

    private void OnEnable()
    {
        if (manager == null)
            return;

        manager.OnConnectionEvent += OnConnection;
        manager.OnClientStopped += OnStopped;
        RefreshPlayers();
    }

    private void OnDisable()
    {
        if (manager == null)
            return;

        manager.OnConnectionEvent -= OnConnection;
        manager.OnClientStopped -= OnStopped;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextPingRefreshTime)
            RefreshPings();
    }

    private void OnConnection(NetworkManager network, ConnectionEventData connection)
    {
        RefreshPlayers();
    }

    private void OnStopped(bool wasHost)
    {
        RefreshPlayers();
    }

    private void RefreshPlayers()
    {
        var players = manager != null && manager.IsConnectedClient
            ? manager.ConnectedClientsIds
            : Array.Empty<ulong>();

        for (var i = 0; i < displayers.Length; i++)
        {
            var displayer = displayers[i];
            var hasPlayer = i < players.Count;

            if (hasPlayer)
                displayer.Init(players[i]);

            displayer.gameObject.SetActive(hasPlayer);
        }

        RefreshPings();
    }

    private void RefreshPings()
    {
        nextPingRefreshTime = Time.unscaledTime + PingRefreshInterval;

        foreach (var displayer in displayers)
        {
            if (displayer.gameObject.activeSelf)
                displayer.SetPing(pingService.GetPing(displayer.DisplayedClientId));
        }
    }
}