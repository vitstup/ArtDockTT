using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Zenject;

public class NetworkSession : IInitializable, IDisposable
{
    private const int MaxPlayers = 4;
    private NetworkManager manager => NetworkManager.Singleton;

    private readonly NetworkSceneController scenes;
    private readonly Dictionary<ulong, bool> admitted = new();

    private bool connected;
    private bool leaving;
    private bool isActive;

    public NetworkSession(NetworkSceneController scenes)
    {
        this.scenes = scenes;
    }

    public bool CanConnect => !isActive && !scenes.IsReturningToMenu && !manager.ShutdownInProgress;

    public bool CanStart => manager.IsHost && scenes.Status == SessionStatus.Lobby
        && !leaving && admitted.Values.All(ready => ready);

    public string Message { get; private set; } = string.Empty;

    public event Action Changed;

    void IInitializable.Initialize()
    {
        manager.ConnectionApprovalCallback = ApproveConnection;
        manager.OnConnectionEvent += OnConnection;
        manager.OnClientDisconnectCallback += OnDisconnected;
        manager.OnClientStopped += OnStopped;
    }

    void IDisposable.Dispose()
    {
        if (manager == null)
            return;

        manager.ConnectionApprovalCallback = null;
        manager.OnConnectionEvent -= OnConnection;
        manager.OnClientDisconnectCallback -= OnDisconnected;
        manager.OnClientStopped -= OnStopped;
    }

    public void Connect(bool host, IPAddress address, ushort port)
    {
        if (!CanConnect)
            return;

        var transport = (UnityTransport)manager.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(address.ToString(), port, host ? IPAddress.Any.ToString() : null);

        isActive = true;
        SetMessage(host ? "Starting host..." : "Connecting...");
        if (!(host ? manager.StartHost() : manager.StartClient()))
        {
            isActive = false;
            admitted.Clear();
            SetMessage(host ? "Could not start host. Check the UDP port." : "Could not start the client.");
        }
    }

    public void Kick(ulong targetClientId)
    {
        if (!manager.IsHost || targetClientId == manager.LocalClientId || !manager.IsListening
            || manager.ShutdownInProgress || !manager.ConnectedClients.ContainsKey(targetClientId))
            return;

        manager.DisconnectClient(targetClientId, "You were kicked by the host.");
    }

    public void Leave()
    {
        if (!isActive || leaving)
            return;

        leaving = true;
        SetMessage("Leaving session...");
        manager.Shutdown();
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (request.ClientNetworkId != NetworkManager.ServerClientId)
        {
            if (admitted.Count >= MaxPlayers)
            {
                response.Reason = "Session is full.";
                return;
            }
        }

        admitted.Add(request.ClientNetworkId, false);
        response.Approved = true;
        response.CreatePlayerObject = false;
        NotifyChanged();
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return admitted.TryGetValue(clientId, out var ready) && ready;
    }

    private void OnConnection(NetworkManager network, ConnectionEventData connection)
    {
        if (connection.EventType != ConnectionEvent.ClientConnected)
            return;

        if (network.IsHost)
            admitted[connection.ClientId] = true;

        if (connection.ClientId == network.LocalClientId)
        {
            connected = true;
            Message = string.Empty;
        }

        NotifyChanged();
    }

    private void OnDisconnected(ulong clientId)
    {
        admitted.Remove(clientId);
        if (clientId == manager.LocalClientId && !leaving)
        {
            var reason = manager.DisconnectReason;
            Message = !string.IsNullOrEmpty(reason)
                ? reason
                : connected ? "Connection to the host was lost." : "Could not connect to the host.";
        }

        NotifyChanged();
    }

    private void OnStopped(bool wasHost)
    {
        if (leaving || wasHost)
            Message = "Session ended.";
        else if (string.IsNullOrEmpty(Message) || Message == "Connecting...")
            Message = connected ? "Connection to the host was lost." : "Could not connect to the host.";
        connected = false;
        isActive = false;
        leaving = false;
        admitted.Clear();
        NotifyChanged();
    }

    private void SetMessage(string message)
    {
        Message = message;
        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();
}