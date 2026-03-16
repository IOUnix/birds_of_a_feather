using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class RemoteConfigManager : MonoBehaviour
{
    public static RemoteConfigManager Instance { get; private set; }

    [Header("Remote Config")]
    [SerializeField] private string configUrl = "https://littlesaintgames.com/game-config.json";
    [SerializeField] private bool showRemoteCongifOverlay = false;

    [Serializable]
    public class GameConfig
    {
        public bool ads_enabled = false;
        public string banner_position = "bottom";
        public string offended = "https://littlesaintgames.com/offended";
    }

    public GameConfig CurrentConfig { get; private set; }

    private string CachePath => Path.Combine(UnityEngine.Application.persistentDataPath, "game-config-cache.json");

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadConfigImmediately();
    }

    private void Start()
    {
        StartCoroutine(FetchRemoteConfig());
    }

    private void LoadConfigImmediately()
    {
        if (File.Exists(CachePath))
        {
            try
            {
                string json = File.ReadAllText(CachePath);
                GameConfig cached = JsonUtility.FromJson<GameConfig>(json);

                if (cached != null)
                {
                    CurrentConfig = NormalizeConfig(cached);
                    UnityEngine.Debug.Log("[RemoteConfig] Loaded cached config.");
                    return;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[RemoteConfig] Failed to read cached config: " + ex.Message);
            }
        }

        CurrentConfig = new GameConfig();
        UnityEngine.Debug.Log("[RemoteConfig] Using built-in default config.");
    }

    private IEnumerator FetchRemoteConfig()
    {
        using UnityWebRequest request = UnityWebRequest.Get(configUrl);
        request.timeout = 10;

        yield return request.SendWebRequest();

        bool ok =
            request.result == UnityWebRequest.Result.Success &&
            request.responseCode >= 200 &&
            request.responseCode < 300;

        if (!ok)
        {
            UnityEngine.Debug.LogWarning("[RemoteConfig] Fetch failed: " + request.error);
            yield break;
        }

        GameConfig downloaded = null;

        try
        {
            downloaded = JsonUtility.FromJson<GameConfig>(request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("[RemoteConfig] Invalid JSON: " + ex.Message);
            yield break;
        }

        if (downloaded == null)
        {
            UnityEngine.Debug.LogWarning("[RemoteConfig] Downloaded config was null.");
            yield break;
        }

        CurrentConfig = NormalizeConfig(downloaded);
        SaveCachedConfig();

        UnityEngine.Debug.Log(
            $"[RemoteConfig] Updated from server | ads_enabled={CurrentConfig.ads_enabled} | " +
            $"banner_position={CurrentConfig.banner_position} | offended={CurrentConfig.offended}"
        );
    }

    private GameConfig NormalizeConfig(GameConfig config)
    {
        if (config == null)
            config = new GameConfig();

        if (string.IsNullOrWhiteSpace(config.banner_position))
            config.banner_position = "bottom";

        config.banner_position = config.banner_position.ToLowerInvariant();

        if (config.banner_position != "top" && config.banner_position != "bottom")
            config.banner_position = "bottom";

        if (string.IsNullOrWhiteSpace(config.offended))
            config.offended = "https://littlesaintgames.com/offended";

        return config;
    }

    private void SaveCachedConfig()
    {
        try
        {
            string json = JsonUtility.ToJson(CurrentConfig, true);
            File.WriteAllText(CachePath, json);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("[RemoteConfig] Failed to save cache: " + ex.Message);
        }
    }

    private void OnGUI()
    {
        if (!showRemoteCongifOverlay)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 22;
        style.alignment = TextAnchor.UpperLeft;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        string text =
            "REMOTE CONFIG\n\n" +
            "Ads Enabled: " + AdsEnabled + "\n" +
            "Banner Position: " + BannerPosition + "\n" +
            "Offended URL: " + OffendedUrl;

        GUI.Box(new Rect(20, 20, 900, 180), text, style);
    }

    public bool AdsEnabled => CurrentConfig != null && CurrentConfig.ads_enabled;

    public string BannerPosition =>
        CurrentConfig != null ? CurrentConfig.banner_position : "bottom";

    public string OffendedUrl =>
        CurrentConfig != null ? CurrentConfig.offended : "https://littlesaintgames.com/offended";
}