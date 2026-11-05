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

    public float Pitch { get; private set; }
    public float Yaw { get; private set; }

    protected override void OnNetworkPostSpawn()
    {
        if (!IsOwner || !NetworkObject.IsPlayerObject)
            return;

        SetInputEnabled(true);

        body = GetComponent<NetworkRigidbody>().Rigidbody;
        Yaw = body.rotation.eulerAngles.y;
        Pitch = 0f;
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
        if (!IsOwner || body.isKinematic)
            return;

        var input = inputEnabled ? Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f) : Vector2.zero;
        var rotation = Quaternion.Euler(0f, Yaw, 0f);
        var velocity = rotation * new Vector3(input.x, 0f, input.y) * moveSpeed;
        velocity.y = body.linearVelocity.y;

        body.MoveRotation(rotation);
        body.linearVelocity = velocity;
    }
}