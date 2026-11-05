using UnityEngine;

public class CursorStateController : MonoBehaviour
{
    [SerializeField] private RectTransform menuTab;

    private void Update()
    {
        bool show = menuTab.gameObject.activeInHierarchy;

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }
}