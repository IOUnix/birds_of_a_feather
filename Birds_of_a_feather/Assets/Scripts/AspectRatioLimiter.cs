using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HybridAspectLimiter : MonoBehaviour
{
    [Header("Fixed aspect ratio")]
    [SerializeField] private float targetWidth = 9f;
    [SerializeField] private float targetHeight = 20f;

    [Header("Base camera size")]
    [SerializeField] private float designOrthographicSize = 5f;

    private Camera cam;
    private float lastScreenW;
    private float lastScreenH;

    public float CurrentEffectiveAspect { get; private set; }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
        {
            Apply();
        }
    }

    private void Apply()
    {
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;

        float screenAspect = (float)Screen.width / Screen.height;
        float targetAspect = targetWidth / targetHeight; // 9:20 = 0.45

        CurrentEffectiveAspect = targetAspect;

        if (screenAspect < targetAspect)
        {
            // Screen is narrower than target - add letterbox bars (top/bottom)
            float normalizedHeight = screenAspect / targetAspect;
            float yInset = (1f - normalizedHeight) * 0.5f;
            cam.rect = new Rect(0f, yInset, 1f, normalizedHeight);
            cam.orthographicSize = designOrthographicSize;
        }
        else
        {
            // Screen is wider than target - add pillarbox bars (left/right)
            float normalizedWidth = targetAspect / screenAspect;
            float xInset = (1f - normalizedWidth) * 0.5f;
            cam.rect = new Rect(xInset, 0f, normalizedWidth, 1f);
            cam.orthographicSize = designOrthographicSize;
        }
    }
}