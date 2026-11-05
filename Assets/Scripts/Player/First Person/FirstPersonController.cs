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
    private float serverPitch;
    private double lastInputTime;
    private uint inputSequence;
    private uint lastInputSequence;
    private uint simulatedInput;
    private uint changedInput;

    private Vector3 predictedPosition;
    private Vector3 predictedVelocity;

    private CapsuleCollider capsule;

    public float Pitch { get; private set; }
    public float Yaw { get; private set; }
    public Quaternion ViewRotation => Quaternion.Euler(IsOwner ? Pitch : serverPitch, IsOwner ? Yaw : serverYaw, 0f);

    protected override void OnNetworkPostSpawn()
    {
        body = GetComponent<NetworkRigidbody>().Rigidbody;
        serverYaw = body.rotation.eulerAngles.y;

        if (!IsOwner || !NetworkObject.IsPlayerObject)
            return;

        Yaw = serverYaw;
        Pitch = 0f;
        predictedPosition = body.position;
        capsule = GetComponent<CapsuleCollider>();
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
        {
            predictedVelocity = Vector3.zero;
            return;
        }

        var mouseDelta = lookAction.action.ReadValue<Vector2>() * mouseSensitivity;
        Yaw = Mathf.Repeat(Yaw + mouseDelta.x, 360f);
        Pitch = Mathf.Clamp(Pitch - mouseDelta.y, -89f, 89f);

        var input = Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);
        var velocity = Quaternion.Euler(0f, Yaw, 0f) * new Vector3(input.x, 0f, input.y) * moveSpeed;
        if ((velocity - predictedVelocity).sqrMagnitude > 0.001f)
            changedInput = inputSequence + 1;
        predictedVelocity = velocity;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned)
            return;

        if (IsServer && !IsOwner)
            ConfirmPositionRpc(body.position, body.linearVelocity, simulatedInput);

        if (IsOwner)
        {
            var input = inputEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            SubmitInputRpc(input, Yaw, Pitch, ++inputSequence);
        }

        if (!IsServer)
            return;

        var movement = NetworkManager.ServerTime.Time - lastInputTime < 0.5 ? serverInput : Vector2.zero;
        var rotation = Quaternion.Euler(0f, serverYaw, 0f);
        var velocity = rotation * new Vector3(movement.x, 0f, movement.y) * moveSpeed;
        velocity.y = body.linearVelocity.y;

        body.MoveRotation(rotation);
        body.linearVelocity = velocity;
        simulatedInput = lastInputSequence;
    }

    public void UpdatePrediction()
    {
        if (!IsSpawned || !IsOwner || IsServer)
            return;

        var movement = predictedVelocity * Time.deltaTime;
        var halfHeight = Vector3.up * (capsule.height * 0.5f - capsule.radius);
        var center = predictedPosition + capsule.center;
        if (movement.sqrMagnitude > 0f && Physics.CapsuleCast(center + halfHeight, center - halfHeight,
                capsule.radius - 0.01f, movement.normalized, out var hit, movement.magnitude + 0.01f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.rigidbody != body)
        {
            var allowed = movement.normalized * Mathf.Max(0f, hit.distance - 0.01f);
            movement = allowed + Vector3.ProjectOnPlane(movement - allowed, hit.normal);
        }
        predictedPosition += movement;
        transform.SetPositionAndRotation(predictedPosition, Quaternion.Euler(0f, Yaw, 0f));
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
    private void ConfirmPositionRpc(Vector3 position, Vector3 velocity, uint sequence)
    {
        if (sequence < changedInput)
            return;

        var lead = Mathf.Min((inputSequence - sequence) * Time.fixedDeltaTime, 1f);
        predictedPosition = Vector3.Lerp(predictedPosition, position + Vector3.ProjectOnPlane(velocity, Vector3.up) * lead,
            1f - Mathf.Exp(-10f * Time.fixedDeltaTime));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
    private void SubmitInputRpc(Vector2 input, float yaw, float pitch, uint sequence)
    {
        if (sequence <= lastInputSequence)
            return;

        lastInputSequence = sequence;
        lastInputTime = NetworkManager.ServerTime.Time;
        serverInput = Vector2.ClampMagnitude(input, 1f);
        serverYaw = yaw;
        serverPitch = Mathf.Clamp(pitch, -89f, 89f);
    }
}