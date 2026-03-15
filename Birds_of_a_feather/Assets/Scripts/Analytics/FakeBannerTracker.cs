using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class FakeBannerTracker : MonoBehaviour
{
    [Header("Fake Banner Settings")]
    [SerializeField] private float refreshIntervalSeconds = 60f;
    [SerializeField] private string bannerPosition = "top";

    private string installId;
    private string sessionId;

    private float visibleBannerTime;
    private int bannerImpressions;
    private float sessionLengthSeconds;

    private bool appPaused;
    private bool appFocused = true;
    private bool gameplayAllowsBanner = true;

    private string SavePath => Path.Combine(UnityEngine.Application.persistentDataPath, "fake_banner_session.json");

    [Serializable]
    private class SessionData
    {
        public string install_id;
        public string session_id;
        public int banner_impressions;
        public float session_length_seconds;
        public string timestamp_utc;
        public string banner_position;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        installId = GetOrCreateInstallId();
        sessionId = Guid.NewGuid().ToString();
        UnityEngine.Debug.Log("[FakeBannerTracker] Save path: " + SavePath);

        LoadPreviousSessionForDebug();
    }

    private void Update()
    {
        sessionLengthSeconds += Time.unscaledDeltaTime;

        if (!ShouldCountBannerTime())
            return;

        visibleBannerTime += Time.unscaledDeltaTime;

        while (visibleBannerTime >= refreshIntervalSeconds)
        {
            visibleBannerTime -= refreshIntervalSeconds;
            bannerImpressions++;
            SaveCurrentSession();
            UnityEngine.Debug.Log($"[FakeBannerTracker] Impression counted. Total = {bannerImpressions}");
        }
    }

    private bool ShouldCountBannerTime()
    {
        return !appPaused && appFocused && gameplayAllowsBanner;
    }

    public void SetBannerVisibleForGameplay(bool isVisible)
    {
        gameplayAllowsBanner = isVisible;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        appPaused = pauseStatus;
        SaveCurrentSession();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        appFocused = hasFocus;
        SaveCurrentSession();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentSession();
    }

    private string GetOrCreateInstallId()
    {
        const string key = "install_id";

        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetString(key);

        string newInstallId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(key, newInstallId);
        PlayerPrefs.Save();
        return newInstallId;
    }

    private void SaveCurrentSession()
    {
        SessionData data = new SessionData
        {
            install_id = installId,
            session_id = sessionId,
            banner_impressions = bannerImpressions,
            session_length_seconds = sessionLengthSeconds,
            timestamp_utc = DateTime.UtcNow.ToString("o"),
            banner_position = bannerPosition
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    private void LoadPreviousSessionForDebug()
    {
        if (!File.Exists(SavePath))
            return;

        string json = File.ReadAllText(SavePath);
        SessionData data = JsonUtility.FromJson<SessionData>(json);

        if (data != null)
        {
            UnityEngine.Debug.Log("[FakeBannerTracker] Previous saved session found:");
            UnityEngine.Debug.Log(json);
        }
    }
}