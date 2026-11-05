using UnityEngine;
using Zenject;

[RequireComponent(typeof(Camera))]
public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] private float heightHeight = 0.7f;
    [SerializeField] private float forwardOffset = 0.7f;

    private FirstPersonController target;
    [Inject] private ILocalPlayerProvider playerProvider;

    private void OnEnable()
    {
        playerProvider.Changed += SetTarget;
        SetTarget(playerProvider.Current);
    }

    private void OnDisable()
    {
        playerProvider.Changed -= SetTarget;
        SetTarget(null);
    }

    private void SetTarget(FirstPersonController player)
    {
        if (target != null)
            target.SetInputEnabled(false);

        target = player;
        if (target != null)
            target.SetInputEnabled(true);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        target.UpdatePrediction();

        transform.SetPositionAndRotation(
            target.transform.position + Vector3.up * heightHeight
                + Quaternion.Euler(0f, target.Yaw, 0f) * Vector3.forward * forwardOffset,
            Quaternion.Euler(target.Pitch, target.Yaw, 0f));
    }
}
