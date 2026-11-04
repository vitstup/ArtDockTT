using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class NetworkSceneController : IInitializable, ITickable, IDisposable
{
    private const string MenuScene = "MenuScene";
    private const string MainScene = "MainScene";
    private const string StatusMessage = "SessionStatus";

    private NetworkManager manager => NetworkManager.Singleton;

    private NetworkSceneManager networkScenes;
    private AsyncOperation localSceneLoad;

    public SessionStatus Status { get; private set; }
    public bool IsReturningToMenu { get; private set; }

    public event Action Changed;

    void IInitializable.Initialize()
    {
        manager.OnClientStarted += OnStarted;
        manager.OnConnectionEvent += OnConnection;
        manager.OnClientStopped += OnStopped;
    }

    void IDisposable.Dispose()
    {
        DetachSceneEvents();

        if (manager == null)
            return;

        manager.OnClientStarted -= OnStarted;
        manager.OnConnectionEvent -= OnConnection;
        manager.OnClientStopped -= OnStopped;
        manager.CustomMessagingManager?.UnregisterNamedMessageHandler(StatusMessage);
    }

    void ITickable.Tick()
    {
        if (!IsReturningToMenu || manager.ShutdownInProgress || (localSceneLoad != null && !localSceneLoad.isDone))
            return;

        IsReturningToMenu = false;
        if (SceneManager.GetActiveScene().name != MenuScene)
            SceneManager.LoadScene(MenuScene, LoadSceneMode.Single);

        Changed?.Invoke();
    }

    public SceneEventProgressStatus StartGame()
    {
        if (!manager.IsHost || Status != SessionStatus.Lobby || networkScenes == null)
            return SceneEventProgressStatus.SceneEventInProgress;

        var result = networkScenes.LoadScene(MainScene, LoadSceneMode.Single);
        if (result == SceneEventProgressStatus.Started)
            PublishStatus(SessionStatus.Loading);

        return result;
    }

    private void OnStarted()
    {
        localSceneLoad = null;
        networkScenes = manager.SceneManager;
        networkScenes.OnSceneEvent += OnSceneEvent;

        if (manager.IsHost)
            networkScenes.SetClientSynchronizationMode(LoadSceneMode.Single);
        else
            manager.CustomMessagingManager.RegisterNamedMessageHandler(StatusMessage, ReceiveStatus);
    }

    private void OnStopped(bool wasHost)
    {
        DetachSceneEvents();
        IsReturningToMenu = true;
        SetStatus(SessionStatus.None);
    }

    private void DetachSceneEvents()
    {
        if (networkScenes == null)
            return;

        networkScenes.OnSceneEvent -= OnSceneEvent;
        networkScenes = null;
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.AsyncOperation != null)
            localSceneLoad = sceneEvent.AsyncOperation;

        if (!manager.IsHost || sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted
            || sceneEvent.SceneName != MainScene)
            return;

        foreach (var clientId in sceneEvent.ClientsThatTimedOut)
        {
            if (clientId != manager.LocalClientId && manager.ConnectedClients.ContainsKey(clientId))
                manager.DisconnectClient(clientId, "Game scene loading timed out.");
        }

        PublishStatus(SessionStatus.InGame);
    }

    private void OnConnection(NetworkManager network, ConnectionEventData connection)
    {
        if (!network.IsHost || connection.EventType != ConnectionEvent.ClientConnected)
            return;

        if (connection.ClientId == network.LocalClientId)
            PublishStatus(SessionStatus.Lobby);
        else
            SendStatus(connection.ClientId);
    }

    private void PublishStatus(SessionStatus status)
    {
        Status = status;
        foreach (var clientId in manager.ConnectedClientsIds)
        {
            if (clientId != manager.LocalClientId)
                SendStatus(clientId);
        }

        Changed?.Invoke();
    }

    private void SendStatus(ulong clientId)
    {
        using var writer = new FastBufferWriter(sizeof(byte), Allocator.Temp);
        writer.WriteValueSafe((byte)Status);
        manager.CustomMessagingManager.SendNamedMessage(StatusMessage, clientId, writer);
    }

    private void ReceiveStatus(ulong senderId, FastBufferReader reader)
    {
        if (senderId != NetworkManager.ServerClientId || !reader.TryBeginRead(sizeof(byte)))
            return;

        reader.ReadValueSafe(out byte value);
        if (value >= (byte)SessionStatus.Lobby && value <= (byte)SessionStatus.InGame)
            SetStatus((SessionStatus)value);
    }

    private void SetStatus(SessionStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }
}

public enum SessionStatus
{
    None,
    Lobby,
    Loading,
    InGame
}