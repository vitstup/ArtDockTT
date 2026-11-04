using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PingChecker : NetworkBehaviour
{
    [SerializeField] private float refreshInterval = 1f;

    private readonly Dictionary<ulong, ushort> pings = new();
    private float nextRefreshTime;

    public ushort? GetPing(ulong playerId)
    {
        return pings.TryGetValue(playerId, out var ping) ? ping : null;
    }

    public override void OnNetworkDespawn()
    {
        pings.Clear();
        nextRefreshTime = 0;
    }

    private void Update()
    {
        var manager = Unity.Netcode.NetworkManager.Singleton;
        if (!IsSpawned || !IsServer || manager.ShutdownInProgress || Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        var players = manager.ConnectedClientsIds;
        var transport = manager.NetworkConfig.NetworkTransport;
        var snapshot = new PlayerPing[players.Count];

        for (var i = 0; i < players.Count; i++)
        {
            var playerId = players[i];
            var ping = playerId == manager.LocalClientId ? 0UL : transport.GetCurrentRtt(playerId);
            snapshot[i] = new PlayerPing(playerId, (ushort)Math.Min(ping, ushort.MaxValue));
        }

        UpdatePingsRpc(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void UpdatePingsRpc(PlayerPing[] snapshot)
    {
        pings.Clear();
        foreach (var entry in snapshot)
            pings[entry.PlayerId] = entry.Ping;
    }

    private struct PlayerPing : INetworkSerializeByMemcpy
    {
        public ulong PlayerId { get; }
        public ushort Ping { get; }

        public PlayerPing(ulong playerId, ushort ping)
        {
            PlayerId = playerId;
            Ping = ping;
        }
    }
}