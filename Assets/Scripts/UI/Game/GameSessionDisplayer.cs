using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

public class GameSessionDisplayer : MonoBehaviour
{
    [SerializeField] private GameObject playersPanel;
    [SerializeField] private Button leave;

    [SerializeField] private InputActionReference togglePlayersAction;

    [Inject] private NetworkSession session;

    private bool enabledTogglePlayers;

    private void OnEnable()
    {
        var action = togglePlayersAction.action;
        action.performed += ToggleView;

        enabledTogglePlayers = !action.enabled;

        if (enabledTogglePlayers)
            action.Enable();
    }

    private void OnDisable()
    {
        var action = togglePlayersAction.action;
        action.performed -= ToggleView;

        if (enabledTogglePlayers)
            action.Disable();
    }

    private void Start()
    {
        playersPanel.SetActive(false);
        leave.onClick.AddListener(session.Leave);
    }

    private void ToggleView(InputAction.CallbackContext context)
    {
        var showPlayers = !playersPanel.activeSelf;
        playersPanel.SetActive(showPlayers);
    }

    private void OnDestroy()
    {
        if (session != null)
            leave.onClick.RemoveListener(session.Leave);
    }
}