using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class MusicButtonIcon : MonoBehaviour
{
    private UnityEngine.UI.Image iconImage;

    private void Awake()
    {
        iconImage = GetComponent<UnityEngine.UI.Image>();
    }

    private void OnEnable()
    {
        AudioManager.OnMuteStateChanged += UpdateIcon;
        Refresh();
    }

    private void OnDisable()
    {
        AudioManager.OnMuteStateChanged -= UpdateIcon;
    }

    private void Refresh()
    {
        if (AudioManager.Instance == null) return;
        iconImage.sprite = AudioManager.Instance.GetCurrentIcon();
    }

    private void UpdateIcon(bool isMuted)
    {
        Refresh();
    }
}