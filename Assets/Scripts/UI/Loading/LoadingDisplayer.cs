using TMPro;
using UnityEngine;

public class LoadingDisplayer : MonoBehaviour
{
    private const float FrameDuration = 0.35f;

    [SerializeField] private TextMeshProUGUI label;

    private readonly string[] frames = { "Loading", "Loading.", "Loading..", "Loading..." };
    private int frameIndex;
    private float nextFrameTime;

    public void StartLoading() => gameObject.SetActive(true);
    public void StopLoading() => gameObject.SetActive(false);

    private void OnEnable()
    {
        frameIndex = 0;
        label.text = frames[frameIndex];
        nextFrameTime = Time.unscaledTime + FrameDuration;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextFrameTime)
            return;

        frameIndex = (frameIndex + 1) % frames.Length;
        label.text = frames[frameIndex];
        nextFrameTime = Time.unscaledTime + FrameDuration;
    }
}