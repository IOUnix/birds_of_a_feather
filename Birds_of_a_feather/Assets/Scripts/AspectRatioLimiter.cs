using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioLimiter : MonoBehaviour
{
    [Header("Designed aspect ratio")]
    [Tooltip("Width of your intended game aspect")]
    [SerializeField] private float targetWidth = 9f;

    [Tooltip("Height of your intended game aspect")]
    [SerializeField] private float targetHeight = 18f;

    [Header("Clamp behavior")]
    [Tooltip("If true, do not allow screens wider than the target ratio.")]
    [SerializeField] private bool clampMaxWidth = true;

    [Tooltip("If true, do not allow screens taller/narrower than the target ratio.")]
    [SerializeField] private bool clampMaxHeight = false;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyAspect();
    }

    private void OnEnable()
    {
        ApplyAspect();
    }

    private void Update()
    {
        // Helps when device rotates, resizes, or changes window dimensions
        ApplyAspect();
    }

    private void ApplyAspect()
    {
        float targetAspect = targetWidth / targetHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        Rect rect = new Rect(0f, 0f, 1f, 1f);

        // Screen is wider than allowed -> pillarbox left/right
        if (clampMaxWidth && screenAspect > targetAspect)
        {
            float normalizedWidth = targetAspect / screenAspect;
            float xInset = (1f - normalizedWidth) * 0.5f;

            rect = new Rect(xInset, 0f, normalizedWidth, 1f);
        }
        // Screen is taller/narrower than allowed -> letterbox top/bottom
        else if (clampMaxHeight && screenAspect < targetAspect)
        {
            float normalizedHeight = screenAspect / targetAspect;
            float yInset = (1f - normalizedHeight) * 0.5f;

            rect = new Rect(0f, yInset, 1f, normalizedHeight);
        }

        if (cam.rect != rect)
        {
            cam.rect = rect;
        }
    }
}