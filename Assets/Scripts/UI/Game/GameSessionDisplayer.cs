using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

public class GameSessionDisplayer : MonoBehaviour
{
    [SerializeField] private GameObject playersPanel;
    [SerializeField] private Button leave;

    [SerializeField] private InputActionReference togglePlayers;

    [Inject] private NetworkSession session;

    private bool enabledTogglePlayers;

    private void OnEnable()
    {
        var action = togglePlayers.action;
        action.performed += TogglePlayers;
        enabledTogglePlayers = !action.enabled;

        if (enabledTogglePlayers)
            action.Enable();
    }

    private void OnDisable()
    {
        var action = togglePlayers.action;
        action.performed -= TogglePlayers;

        if (enabledTogglePlayers)
            action.Disable();
    }

    private void Start()
    {
        playersPanel.SetActive(false);
        leave.onClick.AddListener(session.Leave);
    }

    private void TogglePlayers(InputAction.CallbackContext context)
    {
        playersPanel.SetActive(!playersPanel.activeSelf);
    }

    private void OnDestroy()
    {
        if (session != null)
            leave.onClick.RemoveListener(session.Leave);
    }
}