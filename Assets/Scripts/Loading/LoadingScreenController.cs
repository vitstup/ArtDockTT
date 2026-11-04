using UnityEngine;
using Zenject;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private LoadingDisplayer loadingPrefab;

    [Inject] private NetworkSceneController scenes;

    private LoadingDisplayer displayer;

    private void Start()
    {
        scenes.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (scenes != null)
            scenes.Changed -= Refresh;

        RemoveDisplayer();
    }

    private void Refresh()
    {
        if (scenes.Status == SessionStatus.None)
        {
            RemoveDisplayer();
            return;
        }

        if (displayer == null)
        {
            displayer = Instantiate(loadingPrefab);
            DontDestroyOnLoad(displayer.gameObject);
        }

        if (scenes.Status == SessionStatus.Loading)
            displayer.StartLoading();
        else
            displayer.StopLoading();
    }

    private void RemoveDisplayer()
    {
        if (displayer == null)
            return;

        displayer.StopLoading();
        Destroy(displayer.gameObject);
        displayer = null;
    }
}