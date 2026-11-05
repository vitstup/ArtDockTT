using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerColorSwitcher : NetworkBehaviour
{
    [SerializeField] private InputActionReference changeColorAction;
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.red;

    private readonly NetworkVariable<bool> secondColor = new();
    private readonly List<Material> materials = new();

    private void Awake()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            materials.AddRange(renderer.materials);
    }

    public override void OnNetworkSpawn()
    {
        secondColor.OnValueChanged += OnColorChanged;
        ApplyColor(secondColor.Value);
        if (IsOwner)
            changeColorAction.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        secondColor.OnValueChanged -= OnColorChanged;
        if (IsOwner)
            changeColorAction.action.Disable();
    }

    private void Update()
    {
        if (IsSpawned && IsOwner && changeColorAction.action.WasPressedThisFrame())
            ChangeColorRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ChangeColorRpc() => secondColor.Value = !secondColor.Value;

    private void OnColorChanged(bool previous, bool current) => ApplyColor(current);

    private void ApplyColor(bool useSecondColor)
    {
        foreach (var material in materials)
            material.color = useSecondColor ? color2 : color1;
    }

    public override void OnDestroy()
    {
        foreach (var material in materials)
            Destroy(material);
        base.OnDestroy();
    }
}