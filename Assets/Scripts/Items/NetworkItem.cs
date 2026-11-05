using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

[RequireComponent(typeof(NetworkRigidbody))]
public class NetworkItem : NetworkBehaviour
{
    public const ulong NoHolder = ulong.MaxValue;
    private readonly NetworkVariable<ulong> holderId = new(NoHolder);

    private Rigidbody body;
    private Collider[] colliders;
    [Inject] private ItemInteractionHandler interactions;

    public bool IsHeld => holderId.Value != NoHolder;

    private void OnEnable() => RenderPipelineManager.beginCameraRendering += UpdateHeldPose;

    private void OnDisable() => RenderPipelineManager.beginCameraRendering -= UpdateHeldPose;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        GetComponent<NetworkRigidbody>().AutoUpdateKinematicState = false;
        GetComponent<NetworkObject>().DontDestroyWithOwner = true;
    }

    public override void OnNetworkSpawn()
    {
        holderId.OnValueChanged += OnHolderChanged;
        ApplyPhysics();
        if (IsServer)
            interactions.HolderChanged += OnItemHolderChanged;
    }

    public override void OnNetworkDespawn()
    {
        holderId.OnValueChanged -= OnHolderChanged;
        if (IsServer)
        {
            interactions.HolderChanged -= OnItemHolderChanged;
            interactions.Forget(this);
        }
    }

    private void OnItemHolderChanged(NetworkItem item, ulong clientId, Vector3 velocity)
    {
        if (item != this)
            return;

        holderId.Value = clientId;
        if (clientId == NoHolder)
        {
            NetworkObject.RemoveOwnership();
            body.linearVelocity = velocity;
        }
        else
        {
            NetworkObject.ChangeOwnership(clientId);
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || !IsHeld)
            return;

        var holder = interactions.GetHolder(this);
        if (holder == null || !holder.IsSpawned)
        {
            interactions.Throw(this, Vector3.zero);
            return;
        }

        body.MovePosition(holder.HoldPosition);
        body.MoveRotation(holder.HoldRotation);
    }

    private void OnHolderChanged(ulong previous, ulong current) => ApplyPhysics();

    private void UpdateHeldPose(ScriptableRenderContext context, Camera camera)
    {
        if (!IsSpawned || !IsHeld || !IsOwner)
            return;

        var player = NetworkManager.LocalClient.PlayerObject;
        if (player == null)
            return;

        var interaction = player.GetComponent<PlayerItemInteraction>();
        transform.SetPositionAndRotation(interaction.HoldPosition, interaction.HoldRotation);
    }

    private void ApplyPhysics()
    {
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = IsHeld || !IsServer;
        foreach (var itemCollider in colliders)
            itemCollider.enabled = !IsHeld;
    }
}