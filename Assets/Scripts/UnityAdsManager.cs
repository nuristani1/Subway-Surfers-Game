using System;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Minimal Unity Ads integration for this project.
///
/// Configured with the game owner's Unity Ads account:
///   Game ID (Android): 800374540
///   Placements: BP_Banner_Android, BP_Interstitial_Android, BP_Rewarded_Android
///
/// Add this component to a GameObject in your startup scene (e.g. SampleScene).
/// It initializes Unity Ads once and exposes Banner / Interstitial / Rewarded helpers.
/// No other project code is modified - this file is the only integration point
/// (plus the "com.unity.ads" entry in Packages/manifest.json).
/// </summary>
[DisallowMultipleComponent]
public class UnityAdsManager : MonoBehaviour, IUnityAdsInitializationListener
{
    // ------------------------------------------------------------------
    // Unity Ads configuration (owner's credentials)
    // ------------------------------------------------------------------

    /// <summary>Unity Ads Game ID for Android.</summary>
    public const string AndroidGameId = "800374540";

    // Placement IDs (Unity Ads calls these "placement IDs").
    public const string BannerPlacement = "BP_Banner_Android";
    public const string InterstitialPlacement = "BP_Interstitial_Android";
    public const string RewardedPlacement = "BP_Rewarded_Android";

    /// <summary>Toggle Unity Ads test mode. Keep false for production builds.</summary>
    public bool testMode = false;

    /// <summary>Singleton accessor so any script can show ads easily.</summary>
    public static UnityAdsManager Instance { get; private set; }

    /// <summary>True once the SDK has initialized.</summary>
    public bool IsInitialized { get; private set; }

    private bool _showBannerAfterInit;

    /// <summary>
    /// Auto-creates the manager at game start so no scene editing is required.
    /// You can still place the component manually in a scene if you prefer.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        var go = new GameObject("[UnityAdsManager]");
        go.AddComponent<UnityAdsManager>();
    }

    private string GameId
    {
        get
        {
            // The owner supplied an Android Game ID. For other platforms set the
            // matching Game ID here (or override it in the Inspector).
            return AndroidGameId;
        }
    }

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
        Initialize();
    }

    // ------------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------------

    /// <summary>Initializes the Unity Ads SDK.</summary>
    public void Initialize()
    {
        if (Advertisement.isInitialized)
        {
            IsInitialized = true;
            OnInitialized();
            return;
        }

        // Initialize the Android game id by default. If you build for iOS, put
        // the iOS game id here or make GameId platform-dependent.
        Advertisement.Initialize(GameId, testMode, this);
    }

    /// <summary>Initializes with an explicit Game ID (e.g. the iOS game id).</summary>
    public void Initialize(string gameId)
    {
        if (Advertisement.isInitialized)
        {
            OnInitialized();
            return;
        }

        Advertisement.Initialize(gameId, testMode, this);
    }

    // IUnityAdsInitializationListener
    public void OnInitializationComplete()
    {
        IsInitialized = true;
        Debug.Log("[UnityAdsManager] Unity Ads initialized. Game ID: " + GameId);
        OnInitialized();
    }

    // IUnityAdsInitializationListener
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        IsInitialized = false;
        Debug.LogError($"[UnityAdsManager] Unity Ads init failed ({error}): {message}");
    }

    private void OnInitialized()
    {
        // If a banner was requested before init finished, show it now.
        if (_showBannerAfterInit)
        {
            _showBannerAfterInit = false;
            ShowBanner();
        }
    }

    // ------------------------------------------------------------------
    // Banner
    // ------------------------------------------------------------------

    /// <summary>Shows the banner ad at the bottom of the screen.</summary>
    public void ShowBanner(BannerPosition position = BannerPosition.BOTTOM_CENTER)
    {
        if (!IsInitialized)
        {
            _showBannerAfterInit = true;
            Initialize();
            return;
        }

        Advertisement.Banner.SetPosition(position);

        var options = new BannerLoadOptions
        {
            loadCallback = () => Advertisement.Banner.Show(BannerPlacement),
            errorCallback = error => Debug.LogWarning("[UnityAdsManager] Banner load error: " + error)
        };

        Advertisement.Banner.Load(BannerPlacement, options);
    }

    /// <summary>Hides the currently displayed banner.</summary>
    public void HideBanner()
    {
        Advertisement.Banner.Hide();
    }

    // ------------------------------------------------------------------
    // Interstitial
    // ------------------------------------------------------------------

    /// <summary>Shows an interstitial ad if one is ready.</summary>
    public void ShowInterstitial()
    {
        if (Advertisement.IsReady(InterstitialPlacement))
        {
            Advertisement.Show(InterstitialPlacement);
        }
        else
        {
            Debug.Log("[UnityAdsManager] Interstitial not ready yet.");
        }
    }

    // ------------------------------------------------------------------
    // Rewarded
    // ------------------------------------------------------------------

    /// <summary>
    /// Shows a rewarded ad. <paramref name="onReward"/> is invoked only when the
    /// user finishes the video and earns the reward.
    /// </summary>
    public void ShowRewarded(Action onReward = null)
    {
        if (!Advertisement.IsReady(RewardedPlacement))
        {
            Debug.Log("[UnityAdsManager] Rewarded ad not ready yet.");
            return;
        }

        var options = new ShowOptions
        {
            resultCallback = result =>
            {
                if (result == ShowResult.Finished)
                {
                    Debug.Log("[UnityAdsManager] Rewarded ad finished - grant reward.");
                    onReward?.Invoke();
                }
                else
                {
                    Debug.Log("[UnityAdsManager] Rewarded ad result: " + result);
                }
            }
        };

        Advertisement.Show(RewardedPlacement, options);
    }

    // ------------------------------------------------------------------
    // Convenience helpers (call from UI buttons / game events)
    // ------------------------------------------------------------------

    /// <summary>True when an interstitial is ready to show.</summary>
    public bool IsInterstitialReady()
    {
        return Advertisement.IsReady(InterstitialPlacement);
    }

    /// <summary>True when a rewarded ad is ready to show.</summary>
    public bool IsRewardedReady()
    {
        return Advertisement.IsReady(RewardedPlacement);
    }
}
