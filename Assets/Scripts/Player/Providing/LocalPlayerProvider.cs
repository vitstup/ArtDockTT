using System;

public class LocalPlayerProvider : ILocalPlayerProvider
{
    public FirstPersonController Current { get; private set; }

    public event Action<FirstPersonController> Changed;

    public void Register(FirstPersonController player)
    {
        if (ReferenceEquals(Current, player))
            return;

        Current = player;
        Changed?.Invoke(player);
    }

    public void Unregister(FirstPersonController player)
    {
        if (!ReferenceEquals(Current, player))
            return;

        Current = null;
        Changed?.Invoke(null);
    }
}