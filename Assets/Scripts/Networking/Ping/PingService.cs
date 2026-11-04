using Unity.Netcode;

public class PingService
{
    private NetworkManager manager => NetworkManager.Singleton;
    private PingChecker pingChecker;

    public ushort? GetPing(ulong playerId)
    {
        if (manager == null || manager.SpawnManager == null || manager.ShutdownInProgress)
            return null;

        if (pingChecker != null && pingChecker.IsSpawned)
            return pingChecker.GetPing(playerId);

        foreach (var networkObject in manager.SpawnManager.SpawnedObjectsList)
        {
            if (!networkObject.TryGetComponent(out PingChecker checker))
                continue;

            pingChecker = checker;
            return pingChecker.GetPing(playerId);
        }

        return null;
    }
}