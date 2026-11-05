using System;

public interface ILocalPlayerProvider
{
    FirstPersonController Current { get; }
    event Action<FirstPersonController> Changed;

    void Register(FirstPersonController player);
    void Unregister(FirstPersonController player);
}