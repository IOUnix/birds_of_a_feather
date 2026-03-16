using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private Sprite muteSprite;
    [SerializeField] private Sprite unmuteSprite;

    public bool IsMuted { get; private set; }

    public static event Action<bool> OnMuteStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {

        ApplyMuteState();
    }


    public void ToggleMusic()
    {
        IsMuted = !IsMuted;
        ApplyMuteState();
    }

    public void ButtonTest()
    {
        UnityEngine.Debug.Log("Toggle");

    }

    private void ApplyMuteState()
    {
        musicSource.mute = IsMuted;
        OnMuteStateChanged?.Invoke(IsMuted);
    }

    public Sprite GetCurrentIcon()
    {
        return IsMuted ? unmuteSprite : muteSprite;
    }

    public static void ToggleMusicStatic()
    {
        if (Instance == null) return;
        Instance.ToggleMusic();
    }
}