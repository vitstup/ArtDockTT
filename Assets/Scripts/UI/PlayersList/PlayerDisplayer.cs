using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PlayerDisplayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI ping;
    [SerializeField] private Button kickButton;

    [Inject] private NetworkSession session;

    private NetworkManager manager => NetworkManager.Singleton;

    public ulong DisplayedClientId { get; private set; }

    public void Init(ulong displayedClientId)
    {
        DisplayedClientId = displayedClientId;
        playerName.text = $"Player {displayedClientId}";
        kickButton.gameObject.SetActive(manager.IsHost && manager.LocalClientId != displayedClientId);

        kickButton.onClick.AddListener(KickPlayer);
    }

    public void SetPing(ulong? pingMilliseconds)
    {
        ping.text = pingMilliseconds.HasValue ? $"{pingMilliseconds.Value} ms" : "-";
    }

    private void KickPlayer()
    {
        session.Kick(DisplayedClientId);
    }
}