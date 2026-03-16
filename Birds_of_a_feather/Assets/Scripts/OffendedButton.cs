using System.Diagnostics;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class OffendedButton : MonoBehaviour
{
    public void OpenOffendedPage()
    {
        if (RemoteConfigManager.Instance == null)
        {
            UnityEngine.Debug.LogWarning("RemoteConfigManager not available.");
            return;
        }

        string url = RemoteConfigManager.Instance.OffendedUrl;

        UnityEngine.Debug.Log("Opening offended page: " + url);

        UnityEngine.Application.OpenURL(url);
    }
}