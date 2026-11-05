using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

[RequireComponent(typeof(FirstPersonController))]
public class PlayerItemInteraction : NetworkBehaviour
{
    [SerializeField] private InputActionReference interactAction;

    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private float holdDistance = 1.5f;
    [SerializeField] private float throwSpeed = 8f;
    [SerializeField] private Vector3 viewOffset = new(0f, 0.7f, 0.7f);

    [Inject] private ItemInteractionHandler interactions;

    private FirstPersonController controller;

    public Quaternion HoldRotation => controller.ViewRotation;
    public Vector3 HoldPosition => ViewOrigin(HoldRotation) + HoldRotation * Vector3.forward * holdDistance;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<FirstPersonController>();
        if (IsOwner)
            interactAction.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && interactions.GetHeldItem(this) is { } item)
            interactions.Throw(item, Vector3.zero);

        if (IsOwner)
            interactAction.action.Disable();
    }

    private void Update()
    {
        if (IsSpawned && IsOwner && interactAction.action.WasPressedThisFrame())
            InteractRpc(controller.Yaw, controller.Pitch);
    }

    private Vector3 ViewOrigin(Quaternion rotation) =>
        transform.position + Quaternion.Euler(0f, rotation.eulerAngles.y, 0f) * viewOffset;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void InteractRpc(float yaw, float pitch)
    {
        if (!(Mathf.Abs(yaw) <= 360f && Mathf.Abs(pitch) <= 89f))
            return;

        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var direction = rotation * Vector3.forward;

        var heldItem = interactions.GetHeldItem(this);
        if (heldItem != null)
        {
            interactions.Throw(heldItem, direction * throwSpeed);
            return;
        }

        if (Physics.Raycast(ViewOrigin(rotation), direction, out var hit, pickupDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.TryGetComponent<NetworkItem>(out var item))
            interactions.TryTake(this, item);
    }
}