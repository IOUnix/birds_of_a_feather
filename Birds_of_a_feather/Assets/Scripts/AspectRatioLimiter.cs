using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HybridAspectLimiter : MonoBehaviour
{
    [Header("Design aspect")]
    [SerializeField] private float designWidth = 9f;
    [SerializeField] private float designHeight = 18f;

    [Header("Flexible fill limit")]
    [SerializeField] private float flexibleWidth = 9f;
    [SerializeField] private float flexibleHeight = 16f;

    [Header("Base camera size at design aspect")]
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
        float designAspect = designWidth / designHeight;      // 9:18 = 0.5
        float flexibleAspect = flexibleWidth / flexibleHeight; // 9:16 = 0.5625

        // Screen is narrow enough: use full screen, no bars
        if (screenAspect <= flexibleAspect)
        {
            cam.rect = new Rect(0f, 0f, 1f, 1f);

            // Keep world width locked, crop top/bottom as screen gets wider
            cam.orthographicSize = designOrthographicSize * (designAspect / screenAspect);

            CurrentEffectiveAspect = screenAspect;
        }
        else
        {
            // Stop widening beyond flexibleAspect
            float lockedSize = designOrthographicSize * (designAspect / flexibleAspect);
            cam.orthographicSize = lockedSize;

            // Add pillarbox bars beyond flexibleAspect
            float normalizedWidth = flexibleAspect / screenAspect;
            float xInset = (1f - normalizedWidth) * 0.5f;
            cam.rect = new Rect(xInset, 0f, normalizedWidth, 1f);

            CurrentEffectiveAspect = flexibleAspect;
        }
    }
}