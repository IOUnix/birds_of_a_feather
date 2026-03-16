using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AnalyticsEventSender : MonoBehaviour
{
    [SerializeField] private bool showDebugOverlay = false;
    [SerializeField] private int debugFontSize = 26;

    [Header("Worker")]
    [SerializeField] private string analyticsUrl = "https://birds-analytics-worker.littlesaintgames.workers.dev/analytics";
    [SerializeField] private string gameApiToken = "PASTE_YOUR_GAME_API_TOKEN_HERE";

    [Header("Banner Tracking")]
    [SerializeField] private float impressionIntervalSeconds = 60f;


    [Header("Retry")]
    [SerializeField] private float retryEverySeconds = 15f;
    [SerializeField] private int maxEventsPerFlush = 10;

    private const string InstallIdKey = "install_id";
    private const string InstallEventSentKey = "install_event_sent";

    private static AnalyticsEventSender instance;

    private string installId;
    private string sessionId;

    private bool appPaused;
    private bool appFocused = true;
    private bool bannerShouldBeVisible = true;

    private bool sessionEndQueued;
    private bool flushInProgress;

    // Session timing
    private float sessionAccumulatedSeconds;
    private float sessionRunningStartRealtime;
    private bool sessionTimerRunning;

    // Banner timing
    private float bannerAccumulatedVisibleSeconds;
    private float bannerRunningStartRealtime;
    private bool bannerTimerRunning;
    private float nextImpressionAtSeconds;

    private QueueFile queueFile;
    private string QueuePath => Path.Combine(UnityEngine.Application.persistentDataPath, "analytics_queue.json");

    [Serializable]
    private class QueueFile
    {
        public List<AnalyticsEvent> events = new List<AnalyticsEvent>();
    }

    [Serializable]
    private class AnalyticsEvent
    {
        public string event_type;
        public string event_id;
        public string install_id;
        public string session_id;
        public string timestamp_utc;
        public string app_version;
        public string banner_position;
        public bool ads_enabled;
        public string device_model;
        public string device_os;
        public float session_length_seconds;
    }

    private void Awake()
    {
        if (instance != null)
        {
            UnityEngine.Debug.Log("[Analytics] Duplicate AnalyticsEventSender found. Destroying new instance.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        installId = GetOrCreateInstallId();
        sessionId = Guid.NewGuid().ToString();

        sessionAccumulatedSeconds = 0f;
        sessionRunningStartRealtime = 0f;
        sessionTimerRunning = false;

        bannerAccumulatedVisibleSeconds = 0f;
        bannerRunningStartRealtime = 0f;
        bannerTimerRunning = false;
        nextImpressionAtSeconds = impressionIntervalSeconds;

        sessionEndQueued = false;
        flushInProgress = false;

        LoadQueueFromDisk();

        UnityEngine.Debug.Log($"[Analytics] Awake | install={installId} | session={sessionId} | queue={queueFile.events.Count}");
    }

    private void Start()
    {
        if (!HasInstallEventBeenSent())
        {
            EnqueueEvent("install");
        }

        EnqueueEvent("session_start");

        StartSessionTimer();
        RefreshBannerTimerState();

        StartCoroutine(FlushLoop());

        UnityEngine.Debug.Log("[Analytics] Start | FlushLoop started.");
    }

    private void Update()
    {
        RefreshBannerTimerState();

        float currentVisibleSeconds = GetCurrentBannerVisibleSeconds();

        while (currentVisibleSeconds >= nextImpressionAtSeconds)
        {
            EnqueueEvent("impression");
            nextImpressionAtSeconds += impressionIntervalSeconds;

            currentVisibleSeconds = GetCurrentBannerVisibleSeconds();
        }
    }

    private bool ShouldCountBannerTime()
    {
        return !appPaused && appFocused && bannerShouldBeVisible;
    }

    public void SetBannerVisible(bool isVisible)
    {
        bannerShouldBeVisible = isVisible;
        UnityEngine.Debug.Log($"[Analytics] SetBannerVisible | visible={isVisible}");
        RefreshBannerTimerState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        appPaused = pauseStatus;
        UnityEngine.Debug.Log($"[Analytics] OnApplicationPause | pauseStatus={pauseStatus} | session={sessionId}");

        if (pauseStatus)
        {
            StopSessionTimer();
            StopBannerTimer();

            EnqueueSessionEndAndFlush();
        }
        else
        {
            StartSessionTimer();
            RefreshBannerTimerState();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        appFocused = hasFocus;
        UnityEngine.Debug.Log($"[Analytics] OnApplicationFocus | hasFocus={hasFocus} | session={sessionId}");

        if (hasFocus)
        {
            StartSessionTimer();
            RefreshBannerTimerState();
        }
        else
        {
            StopSessionTimer();
            StopBannerTimer();
        }

        SaveQueueToDisk();
    }

    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log($"[Analytics] OnApplicationQuit | session={sessionId}");

        StopSessionTimer();
        StopBannerTimer();

        if (!sessionEndQueued)
        {
            EnqueueSessionEndEvent();
            SaveQueueToDisk();
        }
    }

    private void StartSessionTimer()
    {
        if (sessionTimerRunning)
            return;

        if (appPaused || !appFocused)
            return;

        sessionRunningStartRealtime = Time.realtimeSinceStartup;
        sessionTimerRunning = true;

        UnityEngine.Debug.Log("[Analytics] Session timer started.");
    }

    private void StopSessionTimer()
    {
        if (!sessionTimerRunning)
            return;

        sessionAccumulatedSeconds += Mathf.Max(0f, Time.realtimeSinceStartup - sessionRunningStartRealtime);
        sessionTimerRunning = false;

        UnityEngine.Debug.Log($"[Analytics] Session timer stopped | accumulated={sessionAccumulatedSeconds:F1}");
    }

    private void RefreshBannerTimerState()
    {
        if (ShouldCountBannerTime())
        {
            StartBannerTimer();
        }
        else
        {
            StopBannerTimer();
        }
    }

    private void StartBannerTimer()
    {
        if (bannerTimerRunning)
            return;

        bannerRunningStartRealtime = Time.realtimeSinceStartup;
        bannerTimerRunning = true;

        UnityEngine.Debug.Log("[Analytics] Banner timer started.");
    }

    private void StopBannerTimer()
    {
        if (!bannerTimerRunning)
            return;

        bannerAccumulatedVisibleSeconds += Mathf.Max(0f, Time.realtimeSinceStartup - bannerRunningStartRealtime);
        bannerTimerRunning = false;

        UnityEngine.Debug.Log($"[Analytics] Banner timer stopped | accumulated={bannerAccumulatedVisibleSeconds:F1}");
    }

    private float GetCurrentSessionLengthSeconds()
    {
        float live = sessionTimerRunning
            ? Mathf.Max(0f, Time.realtimeSinceStartup - sessionRunningStartRealtime)
            : 0f;

        return sessionAccumulatedSeconds + live;
    }

    private float GetCurrentBannerVisibleSeconds()
    {
        float live = bannerTimerRunning
            ? Mathf.Max(0f, Time.realtimeSinceStartup - bannerRunningStartRealtime)
            : 0f;

        return bannerAccumulatedVisibleSeconds + live;
    }

    private void EnqueueSessionEndAndFlush()
    {
        EnqueueSessionEndEvent();
        SaveQueueToDisk();

        if (!flushInProgress)
        {
            UnityEngine.Debug.Log("[Analytics] Starting immediate flush for session_end.");
            StartCoroutine(FlushQueueImmediate());
        }
    }

    private void EnqueueSessionEndEvent()
    {
        if (sessionEndQueued)
        {
            UnityEngine.Debug.Log("[Analytics] session_end already queued for this session. Skipping duplicate.");
            return;
        }

        AnalyticsEvent evt = CreateBaseEvent("session_end");
        evt.session_length_seconds = GetCurrentSessionLengthSeconds();
        queueFile.events.Add(evt);
        sessionEndQueued = true;

        UnityEngine.Debug.Log(
            $"[Analytics] Enqueued: session_end | eventId={evt.event_id} | session={evt.session_id} | " +
            $"length={evt.session_length_seconds:F1} | queue={queueFile.events.Count}"
        );
    }

    private void EnqueueEvent(string eventType)
    {
        AnalyticsEvent evt = CreateBaseEvent(eventType);
        queueFile.events.Add(evt);
        SaveQueueToDisk();

        UnityEngine.Debug.Log(
            $"[Analytics] Enqueued: {eventType} | eventId={evt.event_id} | session={evt.session_id} | " +
            $"queue={queueFile.events.Count}"
        );
    }

    private AnalyticsEvent CreateBaseEvent(string eventType)
    {
        return new AnalyticsEvent
        {
            event_type = eventType,
            event_id = Guid.NewGuid().ToString(),
            install_id = installId,
            session_id = sessionId,
            timestamp_utc = DateTime.UtcNow.ToString("o"),
            app_version = UnityEngine.Application.version,
            banner_position = GetBannerPosition(),
            ads_enabled = GetAdsEnabled(),
            device_model = SystemInfo.deviceModel,
            device_os = SystemInfo.operatingSystem,
            session_length_seconds = 0f
        };
    }

    private IEnumerator FlushLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(retryEverySeconds);

            if (!flushInProgress)
            {
                yield return FlushQueue();
            }
        }
    }

    private IEnumerator FlushQueueImmediate()
    {
        yield return FlushQueue();
    }

    private IEnumerator FlushQueue()
    {
        if (flushInProgress)
            yield break;

        if (queueFile.events == null || queueFile.events.Count == 0)
            yield break;

        flushInProgress = true;

        UnityEngine.Debug.Log($"[Analytics] FlushQueue start | queued={queueFile.events.Count}");

        int sentCount = 0;

        while (queueFile.events.Count > 0 && sentCount < maxEventsPerFlush)
        {
            AnalyticsEvent evt = queueFile.events[0];

            bool success = false;
            yield return SendEvent(evt, result => success = result);

            if (!success)
            {
                UnityEngine.Debug.LogWarning(
                    $"[Analytics] FlushQueue stopped on failed send | eventType={evt.event_type} | " +
                    $"eventId={evt.event_id} | remaining={queueFile.events.Count}"
                );

                SaveQueueToDisk();
                flushInProgress = false;
                yield break;
            }

            if (evt.event_type == "install")
            {
                MarkInstallEventAsSent();
            }

            queueFile.events.RemoveAt(0);
            SaveQueueToDisk();
            sentCount++;

            UnityEngine.Debug.Log(
                $"[Analytics] Dequeued after successful send | eventType={evt.event_type} | " +
                $"eventId={evt.event_id} | remaining={queueFile.events.Count}"
            );
        }

        UnityEngine.Debug.Log($"[Analytics] FlushQueue end | sent={sentCount} | remaining={queueFile.events.Count}");
        flushInProgress = false;
    }

    private IEnumerator SendEvent(AnalyticsEvent evt, Action<bool> onDone)
    {
        string json = JsonUtility.ToJson(evt);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(analyticsUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-Game-Token", gameApiToken);
        request.timeout = 15;

        UnityEngine.Debug.Log(
            $"[Analytics] Sending: {evt.event_type} | eventId={evt.event_id} | session={evt.session_id}"
        );

        yield return request.SendWebRequest();

        bool ok =
            request.result == UnityWebRequest.Result.Success &&
            request.responseCode >= 200 &&
            request.responseCode < 300;

        if (ok)
        {
            UnityEngine.Debug.Log(
                $"[Analytics] Sent: {evt.event_type} | eventId={evt.event_id} | " +
                $"responseCode={request.responseCode} | body={request.downloadHandler.text}"
            );
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                $"[Analytics] Send failed: {evt.event_type} | eventId={evt.event_id} | " +
                $"responseCode={request.responseCode} | error={request.error} | body={request.downloadHandler.text}"
            );
        }

        onDone?.Invoke(ok);
    }

    private void LoadQueueFromDisk()
    {
        if (!File.Exists(QueuePath))
        {
            queueFile = new QueueFile();
            UnityEngine.Debug.Log("[Analytics] LoadQueueFromDisk | no existing queue file found.");
            return;
        }

        string json = File.ReadAllText(QueuePath);
        queueFile = JsonUtility.FromJson<QueueFile>(json);

        if (queueFile == null || queueFile.events == null)
            queueFile = new QueueFile();

        UnityEngine.Debug.Log($"[Analytics] LoadQueueFromDisk | loaded {queueFile.events.Count} queued events.");
    }

    private void SaveQueueToDisk()
    {
        if (queueFile == null)
            queueFile = new QueueFile();

        string json = JsonUtility.ToJson(queueFile, true);
        File.WriteAllText(QueuePath, json);
    }

    private string GetOrCreateInstallId()
    {
        if (PlayerPrefs.HasKey(InstallIdKey))
            return PlayerPrefs.GetString(InstallIdKey);

        string newId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(InstallIdKey, newId);
        PlayerPrefs.Save();

        UnityEngine.Debug.Log($"[Analytics] Created new install_id: {newId}");
        return newId;
    }

    private bool HasInstallEventBeenSent()
    {
        return PlayerPrefs.GetInt(InstallEventSentKey, 0) == 1;
    }

    private void MarkInstallEventAsSent()
    {
        PlayerPrefs.SetInt(InstallEventSentKey, 1);
        PlayerPrefs.Save();
        UnityEngine.Debug.Log("[Analytics] Install event marked as successfully sent.");
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = debugFontSize;
        style.alignment = TextAnchor.UpperLeft;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        string text =
            "ANALYTICS SENDER\n\n" +
            "Install ID: " + installId + "\n" +
            "Session ID: " + sessionId + "\n" +
            "Install Sent: " + HasInstallEventBeenSent() + "\n" +
            "Queued Events: " + (queueFile?.events?.Count ?? 0) + "\n" +
            "Flush In Progress: " + flushInProgress + "\n" +
            "Visible Banner Time: " + GetCurrentBannerVisibleSeconds().ToString("F1") + "\n" +
            "Session Length Live: " + GetCurrentSessionLengthSeconds().ToString("F1") + "\n" +
            "Next Impression At: " + nextImpressionAtSeconds.ToString("F1") + "\n" +
            "App Paused: " + appPaused + "\n" +
            "App Focused: " + appFocused + "\n" +
            "Banner Visible: " + bannerShouldBeVisible + "\n" +
            "Config Ads Enabled: " + GetAdsEnabled() + "\n" +
            "Config Banner Position: " + GetBannerPosition() + "\n" +
            "Session Timer Running: " + sessionTimerRunning + "\n" +
            "Banner Timer Running: " + bannerTimerRunning + "\n" +
            "App Version: " + UnityEngine.Application.version + "\n" +
            "Queue Path:\n" + QueuePath;

        GUI.Box(new Rect(20, 460, 950, 450), text, style);
    }

    private bool GetAdsEnabled()
    {
        if (RemoteConfigManager.Instance == null)
            return false;

        return RemoteConfigManager.Instance.AdsEnabled;
    }

    private string GetBannerPosition()
    {
        if (RemoteConfigManager.Instance == null)
            return "bottom";

        return RemoteConfigManager.Instance.BannerPosition;
    }
}