using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Zenject;

public class ItemInteractionHandler : IInitializable, IDisposable
{
    private readonly Dictionary<PlayerItemInteraction, NetworkItem> playerToItemDict = new();
    private readonly Dictionary<NetworkItem, PlayerItemInteraction> itemToPlayerDict = new();

    private NetworkManager manager => NetworkManager.Singleton;

    public event Action<NetworkItem, ulong, Vector3> HolderChanged;

    public void Initialize() => manager.OnClientDisconnectCallback += OnClientDisconnected;

    public NetworkItem GetHeldItem(PlayerItemInteraction player) =>
        playerToItemDict.TryGetValue(player, out var item) ? item : null;

    public PlayerItemInteraction GetHolder(NetworkItem item) =>
        itemToPlayerDict.TryGetValue(item, out var player) ? player : null;

    public bool TryTake(PlayerItemInteraction player, NetworkItem item)
    {
        if (!manager.IsServer || !player.IsSpawned || !item.IsSpawned
            || playerToItemDict.ContainsKey(player) || itemToPlayerDict.ContainsKey(item))
            return false;

        AddHeldItem(player, item);
        HolderChanged?.Invoke(item, player.OwnerClientId, Vector3.zero);
        return true;
    }

    public void Throw(NetworkItem item, Vector3 velocity)
    {
        if (!manager.IsServer || manager.ShutdownInProgress)
            return;

        if (!RemoveHeldItem(item))
            return;

        HolderChanged?.Invoke(item, NetworkItem.NoHolder, velocity);
    }

    public void Forget(NetworkItem item) => RemoveHeldItem(item);

    private void AddHeldItem(PlayerItemInteraction player, NetworkItem item)
    {
        playerToItemDict.Add(player, item);
        itemToPlayerDict.Add(item, player);
    }

    private bool RemoveHeldItem(NetworkItem item)
    {
        if (!itemToPlayerDict.Remove(item, out var player))
            return false;

        playerToItemDict.Remove(player);
        return true;
    }

    private void ClearHeldItems()
    {
        playerToItemDict.Clear();
        itemToPlayerDict.Clear();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        foreach (var pair in playerToItemDict)
        {
            if (pair.Key.OwnerClientId != clientId)
                continue;

            Throw(pair.Value, Vector3.zero);
            return;
        }
    }

    public void Dispose()
    {
        if (manager != null)
            manager.OnClientDisconnectCallback -= OnClientDisconnected;
        ClearHeldItems();
    }
}