using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PlayerItemSpawner : NetworkBehaviour
{
    [SerializeField] private InputActionReference spawnAction;
    [SerializeField] private NetworkObject itemPrefab;

    [SerializeField] private float spawnDistance = 2f;
    [SerializeField] private float spawnHeight = 0.7f;

    [SerializeField, Min(0f)] private float spawnCooldown = 0.25f;

    private double nextSpawnTime;
    [Inject] private DiContainer container;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            spawnAction.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            spawnAction.action.Disable();
    }

    private void Update()
    {
        if (IsSpawned && IsOwner && spawnAction.action.WasPressedThisFrame())
            SpawnItemRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SpawnItemRpc()
    {
        var now = NetworkManager.ServerTime.Time;
        if (now < nextSpawnTime)
            return;

        nextSpawnTime = now + spawnCooldown;

        var position = transform.position + transform.forward * spawnDistance + Vector3.up * spawnHeight;
        var item = container.InstantiatePrefabForComponent<NetworkObject>(
            itemPrefab, position, Quaternion.identity, null);
        item.Spawn(destroyWithScene: true);
    }
}