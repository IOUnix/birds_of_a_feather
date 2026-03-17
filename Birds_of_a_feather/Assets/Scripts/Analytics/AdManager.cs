using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [SerializeField] private string androidBannerAdUnitId = "ca-app-pub-3940256099942544/9214589741";

    private BannerView bannerView;
    private bool sdkInitialized;
    private bool bannerRequested;
    private bool subscribedToConfig;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(WaitForRemoteConfigAndSubscribe());
    }

    private IEnumerator WaitForRemoteConfigAndSubscribe()
    {
        while (RemoteConfigManager.Instance == null)
        {
            yield return null;
        }

        if (!subscribedToConfig)
        {
            RemoteConfigManager.Instance.OnConfigUpdated += ApplyConfig;
            subscribedToConfig = true;
            UnityEngine.Debug.Log("[AdManager] Subscribed to remote config updates.");
        }

        ApplyConfig();
    }

    public void ApplyConfig()
    {
        if (RemoteConfigManager.Instance == null)
        {
            UnityEngine.Debug.LogWarning("[AdManager] RemoteConfigManager not ready.");
            return;
        }

        bool adsEnabled = RemoteConfigManager.Instance.AdsEnabled;
        string bannerPosition = RemoteConfigManager.Instance.BannerPosition;

        UnityEngine.Debug.Log("[AdManager] ApplyConfig | adsEnabled=" + adsEnabled + " | bannerPosition=" + bannerPosition);

        if (!adsEnabled)
        {
            UnityEngine.Debug.Log("[AdManager] Ads disabled by config.");
            DestroyBanner();
            return;
        }

        if (!sdkInitialized)
        {
            UnityEngine.Debug.Log("[AdManager] Initializing AdMob...");

            MobileAds.Initialize(initStatus =>
            {
                sdkInitialized = true;
                UnityEngine.Debug.Log("[AdManager] AdMob initialized.");
                LoadBanner();
            });

            return;
        }

        LoadBanner();
    }

    private void LoadBanner()
    {
        if (bannerRequested || bannerView != null)
        {
            UnityEngine.Debug.Log("[AdManager] Banner already exists or already requested.");
            return;
        }

        int deviceWidth = MobileAds.Utils.GetDeviceSafeWidth();
        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(deviceWidth);

        AdPosition adPosition = AdPosition.Bottom;

        if (RemoteConfigManager.Instance != null &&
            RemoteConfigManager.Instance.BannerPosition == "top")
        {
            adPosition = AdPosition.Top;
        }

        bannerView = new BannerView(androidBannerAdUnitId, adaptiveSize, adPosition);

        bannerView.OnBannerAdLoaded += () =>
        {
            UnityEngine.Debug.Log("[AdManager] Banner loaded.");
        };

        bannerView.OnBannerAdLoadFailed += error =>
        {
            UnityEngine.Debug.LogWarning("[AdManager] Banner failed to load: " + error);
            bannerRequested = false;
        };

        bannerRequested = true;
        bannerView.LoadAd(new AdRequest());

        UnityEngine.Debug.Log("[AdManager] Banner request sent.");
    }

    public void DestroyBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        bannerRequested = false;
        UnityEngine.Debug.Log("[AdManager] Banner destroyed.");
    }

    private void OnDestroy()
    {
        if (subscribedToConfig && RemoteConfigManager.Instance != null)
        {
            RemoteConfigManager.Instance.OnConfigUpdated -= ApplyConfig;
            subscribedToConfig = false;
        }
    }
}