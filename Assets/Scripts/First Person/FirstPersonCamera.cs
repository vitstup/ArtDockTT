using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] private float heightHeight = 0.7f;
    [SerializeField] private float forwardOffset = 0.7f;

    private FirstPersonController target;

    private void OnEnable()
    {
        FirstPersonController.LocalPlayerChanged += SetTarget;
    }

    private void OnDisable()
    {
        FirstPersonController.LocalPlayerChanged -= SetTarget;
        SetTarget(null);
    }

    private void SetTarget(FirstPersonController player)
    {
        if (target != null)
            target.SetInputEnabled(false);

        target = player;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(
            target.transform.position + Vector3.up * heightHeight + target.transform.forward * forwardOffset,
            Quaternion.Euler(target.Pitch, target.Yaw, 0f));
    }
}