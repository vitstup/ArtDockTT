using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkRigidbody))]
public class FirstPersonController : NetworkBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;

    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;

    private Rigidbody body;
    private bool inputEnabled;

    private Vector2 serverInput;
    private float serverYaw;
    private double lastInputTime;
    private uint inputSequence;
    private uint lastInputSequence;

    public float Pitch { get; private set; }
    public float Yaw { get; private set; }

    protected override void OnNetworkPostSpawn()
    {
        body = GetComponent<NetworkRigidbody>().Rigidbody;
        serverYaw = body.rotation.eulerAngles.y;

        if (!IsOwner || !NetworkObject.IsPlayerObject)
            return;

        Yaw = serverYaw;
        Pitch = 0f;
        SetInputEnabled(true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        SetInputEnabled(false);
    }

    public void SetInputEnabled(bool value)
    {
        if (!IsOwner || inputEnabled == value)
            return;

        inputEnabled = value;
        if (value)
        {
            moveAction.action.Enable();
            lookAction.action.Enable();
        }
        else
        {
            moveAction.action.Disable();
            lookAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        var mouseDelta = lookAction.action.ReadValue<Vector2>() * mouseSensitivity;
        Yaw = Mathf.Repeat(Yaw + mouseDelta.x, 360f);
        Pitch = Mathf.Clamp(Pitch - mouseDelta.y, -89f, 89f);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            var input = inputEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            SubmitInputRpc(input, Yaw, ++inputSequence);
        }

        if (!IsServer)
            return;

        var movement = NetworkManager.ServerTime.Time - lastInputTime < 0.5 ? serverInput : Vector2.zero;
        var rotation = Quaternion.Euler(0f, serverYaw, 0f);
        var velocity = rotation * new Vector3(movement.x, 0f, movement.y) * moveSpeed;
        velocity.y = body.linearVelocity.y;

        body.MoveRotation(rotation);
        body.linearVelocity = velocity;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
    private void SubmitInputRpc(Vector2 input, float yaw, uint sequence)
    {
        if (sequence <= lastInputSequence)
            return;

        lastInputSequence = sequence;
        lastInputTime = NetworkManager.ServerTime.Time;
        serverInput = Vector2.ClampMagnitude(input, 1f);
        serverYaw = yaw;
    }
}