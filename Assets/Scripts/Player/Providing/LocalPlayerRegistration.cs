using Unity.Netcode;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(FirstPersonController))]
public class LocalPlayerRegistration : NetworkBehaviour
{
    [Inject] private ILocalPlayerProvider playerProvider;

    private FirstPersonController controller;

    protected override void OnNetworkPostSpawn()
    {
        if (!IsOwner || !NetworkObject.IsPlayerObject)
            return;

        controller = GetComponent<FirstPersonController>();
        playerProvider.Register(controller);
    }

    public override void OnNetworkDespawn()
    {
        if (controller == null)
            return;

        playerProvider.Unregister(controller);
        controller = null;
    }
}