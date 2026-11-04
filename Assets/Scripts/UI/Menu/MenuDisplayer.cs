using Unity.Netcode;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MenuDisplayer : MonoBehaviour
{
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject lobbyPanel;

    [SerializeField] private TMP_InputField address;
    [SerializeField] private TMP_InputField port;

    [SerializeField] private TextMeshProUGUI joinStatus;
    [SerializeField] private TextMeshProUGUI lobbyStatus;

    [SerializeField] private Button host;
    [SerializeField] private Button join;

    [SerializeField] private Button startGame;
    [SerializeField] private Button leave;

    [Inject] private NetworkSession session;
    [Inject] private NetworkSceneController scenes;

    private NetworkManager manager => NetworkManager.Singleton;

    private void Start()
    {
        session.Changed += Refresh;
        scenes.Changed += Refresh;

        host.onClick.AddListener(Host);
        join.onClick.AddListener(Join);

        startGame.onClick.AddListener(StartGame);
        leave.onClick.AddListener(session.Leave);

        Refresh();
    }

    private void OnDestroy()
    {
        if (session == null)
            return;

        session.Changed -= Refresh;
        scenes.Changed -= Refresh;
        host.onClick.RemoveListener(Host);
        join.onClick.RemoveListener(Join);
        startGame.onClick.RemoveListener(StartGame);
        leave.onClick.RemoveListener(session.Leave);
    }

    private void Host() => Connect(true);
    private void Join() => Connect(false);

    private void Connect(bool asHost)
    {
        if (!ushort.TryParse(port.text, out var udpPort) || udpPort == 0)
        {
            joinStatus.text = "Enter a port from 1 to 65535.";
            return;
        }

        var ipAddress = IPAddress.Loopback;
        if (!asHost && (!IPAddress.TryParse(address.text.Trim(), out ipAddress)
            || ipAddress.AddressFamily != AddressFamily.InterNetwork || ipAddress.Equals(IPAddress.Any)))
        {
            joinStatus.text = "Enter the host's IPv4 address.";
            return;
        }

        session.Connect(asHost, ipAddress, udpPort);
    }

    private void StartGame()
    {
        if (!session.CanStart)
            return;

        var result = scenes.StartGame();
        if (result != SceneEventProgressStatus.Started)
            lobbyStatus.text = $"Could not load game: {result}.";
    }

    private void Refresh()
    {
        var inLobby = scenes.Status == SessionStatus.Lobby;
        joinPanel.SetActive(!inLobby);
        lobbyPanel.SetActive(inLobby);
        host.interactable = join.interactable = session.CanConnect;
        address.interactable = port.interactable = session.CanConnect;
        startGame.gameObject.SetActive(manager.IsHost);
        startGame.interactable = session.CanStart;
        joinStatus.text = lobbyStatus.text = session.Message;
    }
}