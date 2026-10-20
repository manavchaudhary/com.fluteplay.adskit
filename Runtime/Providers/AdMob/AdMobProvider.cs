using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public class AdMobProvider : IAdsProvider
    {
        public string Name => "ADMOB";
        public bool IsInitialized { get; private set; }

        private AdsSettingsAsset _settings;
        private AdsProfile _profile;

        private readonly Dictionary<string, BannerView> _bannerViews = new();
        private readonly Dictionary<string, InterstitialAd> _interstitialAds = new();
        private readonly Dictionary<string, RewardedAd> _rewardedAds = new();

        private readonly Dictionary<string, bool> _interstitialReady = new();
        private readonly Dictionary<string, bool> _rewardedReady = new();

        private readonly Dictionary<string, Action<AdsResult>> _pendingInterstitialClosed = new();
        private readonly Dictionary<string, Action<AdsResult>> _pendingRewardedCompleted = new();

        private readonly Dictionary<string, AdsResult> _rewardedLoadErrors = new();
        private readonly Dictionary<string, AdsResult> _interstitialLoadErrors = new();
        private readonly Dictionary<string, bool> _rewardEarnedThisShow = new();

        // Rewarded load state / recovery
        private readonly Dictionary<string, bool> _rewardedLoading = new();
        private readonly Dictionary<string, int> _rewardedRetryAttempt = new();
        private readonly Dictionary<string, bool> _rewardedRetryScheduled = new();
        private readonly Dictionary<string, DateTime> _rewardedLoadedAtUtc = new();
        private readonly Dictionary<string, bool> _rewardedShowCompleted = new();

        private const int MaxRewardedRetryAttempts = 5;
        private const float RewardedRetryBaseSeconds = 2f;
        private const float RewardedRetryMaxSeconds = 60f;
        private static readonly TimeSpan RewardedAdMaxAge = TimeSpan.FromMinutes(55);



        public void Initialize(AdsSettingsAsset settings, AdsProfile profile)
        {
            _settings = settings;
            _profile = profile;

            if (_settings == null)
            {
                Debug.LogWarning("[AdsKit][AdMob] Settings is null.");
                return;
            }

            if (_profile == null)
            {
                Debug.LogWarning("[AdsKit][AdMob] Profile is null.");
                return;
            }

            Debug.Log("[AdsKit][AdMob] Initializing MobileAds...");

            // Keep Google Mobile Ads callbacks on Unity's main thread.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = true;
                Debug.Log("[AdsKit][AdMob] Initialized");
                PreloadConfiguredPlacements();
            });
        }

        public bool IsReady(AdFormat format, string placementKey)
        {
            switch (format)
            {
                case AdFormat.Banner:
                    return _bannerViews.TryGetValue(placementKey, out var bannerView) && bannerView != null;

                case AdFormat.Interstitial:
                    return _interstitialAds.TryGetValue(placementKey, out var interstitialAd)
                           && interstitialAd != null
                           && interstitialAd.CanShowAd()
                           && _interstitialReady.TryGetValue(placementKey, out var interstitialReady)
                           && interstitialReady;

                case AdFormat.Rewarded:
                    if (!IsRewardedUsable(placementKey))
                    {
                        if (_rewardedAds.ContainsKey(placementKey) && IsRewardedExpired(placementKey))
                        {
                            Debug.LogWarning($"[AdsKit][AdMob] Rewarded cache expired: placement={placementKey}. Reloading.");
                            CleanupRewardedAd(placementKey);
                            LoadRewarded(placementKey);
                        }
                        return false;
                    }
                    return true;

                default:
                    return false;
            }
        }

        public void Load(AdFormat format, string placementKey)
        {
            if (!IsInitialized || _settings == null || _profile == null)
                return;

            switch (format)
            {
                case AdFormat.Banner:
                    break;

                case AdFormat.Interstitial:
                    LoadInterstitial(placementKey);
                    break;

                case AdFormat.Rewarded:
                    LoadRewarded(placementKey);
                    break;
            }
        }

        public void ShowBanner(string placementKey, BannerPosition position, BannerSizeType sizeType)
        {
            string unitId = GetUnitId(placementKey);
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning($"[AdsKit][AdMob] Banner unit id missing. placement={placementKey}");
                return;
            }

            if (_bannerViews.TryGetValue(placementKey, out var existingBanner))
            {
                existingBanner?.Destroy();
                _bannerViews.Remove(placementKey);
            }

            AdPosition adPosition = position == BannerPosition.Top ? AdPosition.Top : AdPosition.Bottom;
            AdSize adSize = GetBannerSize(sizeType);

            var bannerView = new BannerView(unitId, adSize, adPosition);
            _bannerViews[placementKey] = bannerView;

            bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log($"[AdsKit][AdMob] Banner loaded: placement={placementKey}");
                bannerView.Show();
            };

            bannerView.OnBannerAdLoadFailed += error =>
            {
                Debug.LogWarning($"[AdsKit][AdMob] Banner load failed: placement={placementKey} error={error}");
                bannerView.Destroy();
                _bannerViews.Remove(placementKey);
            };

            bannerView.OnAdPaid += adValue =>
            {
                Debug.Log($"[AdsKit][AdMob] Banner paid event: placement={placementKey} valueMicros={adValue.Value} currency={adValue.CurrencyCode}");
            };

            bannerView.OnAdImpressionRecorded += () =>
            {
                Debug.Log($"[AdsKit][AdMob] Banner impression: placement={placementKey}");
            };

            bannerView.OnAdClicked += () =>
            {
                Debug.Log($"[AdsKit][AdMob] Banner clicked: placement={placementKey}");
            };

            bannerView.LoadAd(new AdRequest());
            Debug.Log($"[AdsKit][AdMob] LoadBanner called: placement={placementKey} position={position} size={sizeType}");
        }

        public void HideBanner()
        {
            foreach (var pair in _bannerViews)
            {
                pair.Value?.Hide();
            }

            Debug.Log("[AdsKit][AdMob] HideBanner all active banners");
        }

        public AdsResult ShowInterstitial(string placementKey, Action<AdsResult> onClosed = null)
{
    Debug.Log($"[AdsKit][AdMob] ShowInterstitial called: placement={placementKey} ready={IsReady(AdFormat.Interstitial, placementKey)}");

    string unitId = GetUnitId(placementKey);

    if (string.IsNullOrEmpty(unitId))
    {
        return AdsResult.Fail(
            placementKey,
            AdFormat.Interstitial,
            AdsErrorCodes.AdUnitIdMissing,
            Name
        );
    }

    if (!IsReady(AdFormat.Interstitial, placementKey))
    {
        Debug.LogWarning($"[AdsKit][AdMob] Interstitial not ready: placement={placementKey}");

        LoadInterstitial(placementKey);

        if (_interstitialLoadErrors.TryGetValue(placementKey, out AdsResult loadError))
        {
            return loadError;
        }

        return AdsResult.Fail(
            placementKey,
            AdFormat.Interstitial,
            AdsErrorCodes.AdNotLoaded,
            Name
        );
    }

    _pendingInterstitialClosed[placementKey] = onClosed;
    _interstitialReady[placementKey] = false;

    try
    {
        _interstitialAds[placementKey].Show();

        return AdsResult.Ok(placementKey, AdFormat.Interstitial, Name);
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[AdsKit][AdMob] Interstitial show exception: placement={placementKey} error={ex}");

        _pendingInterstitialClosed.Remove(placementKey);

        LoadInterstitial(placementKey);

        return AdsResult.Fail(
            placementKey,
            AdFormat.Interstitial,
            AdsErrorCodes.AdFailedToShow,
            Name,
            ex.ToString()
        );
    }
}

public AdsResult ShowRewarded(string placementKey, Action<AdsResult> onCompleted)
{
    Debug.Log($"[AdsKit][AdMob] ShowRewarded called: placement={placementKey} ready={IsReady(AdFormat.Rewarded, placementKey)}");

    string unitId = GetUnitId(placementKey);

    if (string.IsNullOrEmpty(unitId))
    {
        return AdsResult.Fail(
            placementKey,
            AdFormat.Rewarded,
            AdsErrorCodes.AdUnitIdMissing,
            Name
        );
    }

    if (!IsReady(AdFormat.Rewarded, placementKey))
    {
        Debug.LogWarning($"[AdsKit][AdMob] Rewarded not ready: placement={placementKey}");

        LoadRewarded(placementKey);

        if (_rewardedLoadErrors.TryGetValue(placementKey, out AdsResult loadError))
        {
            return loadError;
        }

        return AdsResult.Fail(
            placementKey,
            AdFormat.Rewarded,
            AdsErrorCodes.AdNotLoaded,
            Name
        );
    }

    _pendingRewardedCompleted[placementKey] = onCompleted;
    _rewardEarnedThisShow[placementKey] = false;
    _rewardedShowCompleted[placementKey] = false;
    _rewardedReady[placementKey] = false;

    try
    {
        _rewardedAds[placementKey].Show(reward =>
        {
            _rewardEarnedThisShow[placementKey] = true;
            Debug.Log($"[AdsKit][AdMob] Reward earned: placement={placementKey} amount={reward.Amount} type={reward.Type}");
        });

        return AdsResult.Ok(placementKey, AdFormat.Rewarded, Name);
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[AdsKit][AdMob] Rewarded show exception: placement={placementKey} error={ex}");

        _pendingRewardedCompleted.Remove(placementKey);
        _rewardEarnedThisShow[placementKey] = false;

        LoadRewarded(placementKey);

        return AdsResult.Fail(
            placementKey,
            AdFormat.Rewarded,
            AdsErrorCodes.AdFailedToShow,
            Name,
            ex.ToString()
        );
    }
}

        public void OpenAdInspector()
        {
#if UNITY_ANDROID || UNITY_IOS
            MobileAds.OpenAdInspector(error =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[AdsKit][AdMob] AdInspector failed: {error}");
                }
                else
                {
                    Debug.Log("[AdsKit][AdMob] AdInspector closed.");
                }
            });
#else
            Debug.Log("[AdsKit][AdMob] AdInspector not supported on this platform.");
#endif
        }

        private void PreloadConfiguredPlacements()
        {
            if (_settings.placements == null)
                return;

            foreach (var placement in _settings.placements)
            {
                if (placement == null || !placement.enabled || string.IsNullOrWhiteSpace(placement.key))
                    continue;

                switch (placement.format)
                {
                    case AdFormat.Interstitial:
                        if (_settings.interstitialEnabled)
                            LoadInterstitial(placement.key);
                        break;

                    case AdFormat.Rewarded:
                        if (_settings.rewardedEnabled)
                            LoadRewarded(placement.key);
                        break;
                }
            }
        }

        private void LoadInterstitial(string placementKey)
        {
            string unitId = GetUnitId(placementKey);
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning($"[AdsKit][AdMob] Interstitial unit id missing: placement={placementKey}");
                return;
            }

            _interstitialReady[placementKey] = false;

            if (_interstitialAds.TryGetValue(placementKey, out var oldAd))
            {
                oldAd?.Destroy();
                _interstitialAds.Remove(placementKey);
            }

            Debug.Log($"[AdsKit][AdMob] Loading interstitial: placement={placementKey}");

            InterstitialAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    AdsResult loadError = AdsResult.Fail(
                        placementKey,
                        AdFormat.Interstitial,
                        AdsErrorCodes.AdLoadFailed,
                        Name,
                        error != null ? error.ToString() : "InterstitialAd.Load returned null ad"
                    );

                    _interstitialLoadErrors[placementKey] = loadError;

                    Debug.LogWarning($"[AdsKit][AdMob] Interstitial load failed: {loadError}");
                    return;
                }

                _interstitialLoadErrors.Remove(placementKey);

                _interstitialAds[placementKey] = ad;
                _interstitialReady[placementKey] = true;

                ad.OnAdPaid += adValue =>
                {
                    Debug.Log($"[AdsKit][AdMob] Interstitial paid event: placement={placementKey} valueMicros={adValue.Value} currency={adValue.CurrencyCode}");
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Interstitial impression: placement={placementKey}");
                };

                ad.OnAdClicked += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Interstitial clicked: placement={placementKey}");
                };

                ad.OnAdFullScreenContentOpened += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Interstitial opened: placement={placementKey}");
                };

               ad.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Interstitial closed: placement={placementKey}");

                    CompleteInterstitial(
                        placementKey,
                        AdsResult.Ok(placementKey, AdFormat.Interstitial, Name)
                    );

                    ad.Destroy();
                    _interstitialAds.Remove(placementKey);

                    LoadInterstitial(placementKey);
                };

                ad.OnAdFullScreenContentFailed += adError =>
                {
                    Debug.LogWarning($"[AdsKit][AdMob] Interstitial failed to open: placement={placementKey} error={adError}");

                    CompleteInterstitial(
                        placementKey,
                        AdsResult.Fail(
                            placementKey,
                            AdFormat.Interstitial,
                            AdsErrorCodes.AdFailedToShow,
                            Name,
                            adError.ToString()
                        )
                    );

    ad.Destroy();
    _interstitialAds.Remove(placementKey);

    LoadInterstitial(placementKey);
};

                Debug.Log($"[AdsKit][AdMob] Interstitial loaded: placement={placementKey}");
            });
        }

        private void LoadRewarded(string placementKey)
        {
            string unitId = GetUnitId(placementKey);
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning($"[AdsKit][AdMob] Rewarded unit id missing: placement={placementKey}");
                return;
            }

            if (_rewardedLoading.TryGetValue(placementKey, out bool isLoading) && isLoading)
            {
                Debug.Log($"[AdsKit][AdMob] Rewarded load skipped (already loading): placement={placementKey}");
                return;
            }

            if (IsRewardedUsable(placementKey))
            {
                Debug.Log($"[AdsKit][AdMob] Rewarded load skipped (already ready): placement={placementKey}");
                return;
            }

            _rewardedLoading[placementKey] = true;
            _rewardedReady[placementKey] = false;
            CleanupRewardedAd(placementKey);

            Debug.Log($"[AdsKit][AdMob] Loading rewarded: placement={placementKey}");

            RewardedAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                _rewardedLoading[placementKey] = false;

                if (error != null || ad == null)
                {
                    string providerError = BuildLoadErrorDetails(error, "RewardedAd.Load returned null ad");

                    AdsResult loadError = AdsResult.Fail(
                        placementKey,
                        AdFormat.Rewarded,
                        AdsErrorCodes.AdLoadFailed,
                        Name,
                        providerError
                    );

                    _rewardedLoadErrors[placementKey] = loadError;
                    _rewardedReady[placementKey] = false;

                    Debug.LogWarning(
                        $"[AdsKit][AdMob][Rewarded][LoadFailed] placement={placementKey} {providerError}"
                    );

                    ScheduleRewardedRetry(placementKey);
                    return;
                }

                _rewardedLoadErrors.Remove(placementKey);
                _rewardedRetryAttempt[placementKey] = 0;
                _rewardedRetryScheduled[placementKey] = false;

                _rewardedAds[placementKey] = ad;
                _rewardedReady[placementKey] = true;
                _rewardedLoadedAtUtc[placementKey] = DateTime.UtcNow;

                ad.OnAdPaid += adValue =>
                {
                    Debug.Log($"[AdsKit][AdMob] Rewarded paid event: placement={placementKey} valueMicros={adValue.Value} currency={adValue.CurrencyCode}");
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Rewarded impression: placement={placementKey}");
                };

                ad.OnAdClicked += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Rewarded clicked: placement={placementKey}");
                };

                ad.OnAdFullScreenContentOpened += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Rewarded opened: placement={placementKey}");
                };

                ad.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log($"[AdsKit][AdMob] Rewarded closed: placement={placementKey}");

                    bool success = _rewardEarnedThisShow.TryGetValue(placementKey, out var earned) && earned;

                    AdsResult result = success
                        ? AdsResult.Ok(placementKey, AdFormat.Rewarded, Name)
                        : AdsResult.Fail(placementKey, AdFormat.Rewarded, AdsErrorCodes.RewardNotEarned, Name);

                    CompleteRewardedOnce(placementKey, result);
                    _rewardEarnedThisShow[placementKey] = false;

                    CleanupRewardedAd(placementKey, ad);
                    LoadRewarded(placementKey);
                };

                ad.OnAdFullScreenContentFailed += adError =>
                {
                    string providerError = BuildAdErrorDetails(adError);

                    Debug.LogWarning(
                        $"[AdsKit][AdMob][Rewarded][ShowFailed] placement={placementKey} {providerError}"
                    );

                    CompleteRewardedOnce(
                        placementKey,
                        AdsResult.Fail(
                            placementKey,
                            AdFormat.Rewarded,
                            AdsErrorCodes.AdFailedToShow,
                            Name,
                            providerError
                        )
                    );

                    _rewardEarnedThisShow[placementKey] = false;

                    CleanupRewardedAd(placementKey, ad);
                    LoadRewarded(placementKey);
                };

                Debug.Log($"[AdsKit][AdMob] Rewarded loaded: placement={placementKey}");
            });
        }

        private void ScheduleRewardedRetry(string placementKey)
        {
            if (_rewardedRetryScheduled.TryGetValue(placementKey, out bool scheduled) && scheduled)
                return;

            int attempt = _rewardedRetryAttempt.TryGetValue(placementKey, out int currentAttempt)
                ? currentAttempt + 1
                : 1;

            _rewardedRetryAttempt[placementKey] = attempt;

            if (attempt > MaxRewardedRetryAttempts)
            {
                Debug.LogWarning(
                    $"[AdsKit][AdMob] Rewarded automatic retry limit reached: placement={placementKey}. " +
                    "The opportunity stays available; the next manual request can load again."
                );
                return;
            }

            float delaySeconds = Mathf.Min(
                RewardedRetryBaseSeconds * Mathf.Pow(2f, attempt - 1),
                RewardedRetryMaxSeconds
            );

            _rewardedRetryScheduled[placementKey] = true;

            Debug.Log(
                $"[AdsKit][AdMob] Rewarded retry scheduled: placement={placementKey} " +
                $"attempt={attempt}/{MaxRewardedRetryAttempts} delay={delaySeconds:0}s"
            );

            if (AdsService.Instance != null)
            {
                AdsService.Instance.StartCoroutine(RetryRewardedAfterDelay(placementKey, delaySeconds));
            }
            else
            {
                _rewardedRetryScheduled[placementKey] = false;
            }
        }

        private System.Collections.IEnumerator RetryRewardedAfterDelay(string placementKey, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);

            _rewardedRetryScheduled[placementKey] = false;

            if (!IsInitialized || IsRewardedUsable(placementKey))
                yield break;

            LoadRewarded(placementKey);
        }

        private bool IsRewardedUsable(string placementKey)
        {
            if (!_rewardedAds.TryGetValue(placementKey, out var ad) || ad == null)
                return false;

            if (!_rewardedReady.TryGetValue(placementKey, out bool ready) || !ready)
                return false;

            if (IsRewardedExpired(placementKey))
                return false;

            return ad.CanShowAd();
        }

        private bool IsRewardedExpired(string placementKey)
        {
            return _rewardedLoadedAtUtc.TryGetValue(placementKey, out DateTime loadedAt) &&
                   DateTime.UtcNow - loadedAt >= RewardedAdMaxAge;
        }

        private void CleanupRewardedAd(string placementKey, RewardedAd specificAd = null)
        {
            if (specificAd != null)
            {
                try
                {
                    specificAd.Destroy();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AdsKit][AdMob] Rewarded destroy warning: placement={placementKey} error={ex.Message}");
                }
            }

            if (_rewardedAds.TryGetValue(placementKey, out var currentAd) &&
                (specificAd == null || ReferenceEquals(currentAd, specificAd)))
            {
                if (specificAd == null)
                {
                    try
                    {
                        currentAd?.Destroy();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[AdsKit][AdMob] Rewarded destroy warning: placement={placementKey} error={ex.Message}");
                    }
                }

                _rewardedAds.Remove(placementKey);
            }

            _rewardedReady[placementKey] = false;
            _rewardedLoadedAtUtc.Remove(placementKey);
        }

        private void CompleteRewardedOnce(string placementKey, AdsResult result)
        {
            if (_rewardedShowCompleted.TryGetValue(placementKey, out bool completed) && completed)
            {
                Debug.LogWarning($"[AdsKit][AdMob] Duplicate rewarded completion ignored: placement={placementKey}");
                return;
            }

            _rewardedShowCompleted[placementKey] = true;
            CompleteRewarded(placementKey, result);
        }

        private static string BuildLoadErrorDetails(LoadAdError error, string fallback)
        {
            if (error == null)
                return fallback;

            string cause = error.GetCause() != null ? error.GetCause().ToString() : "none";
            string responseInfo = error.GetResponseInfo() != null ? error.GetResponseInfo().ToString() : "none";

            return $"Code={error.GetCode()} | Domain={error.GetDomain()} | Message={error.GetMessage()} | Cause={cause} | ResponseInfo={responseInfo}";
        }

        private static string BuildAdErrorDetails(AdError error)
        {
            if (error == null)
                return "AdError=null";

            return $"Code={error.GetCode()} | Domain={error.GetDomain()} | Message={error.GetMessage()}";
        }

       private void CompleteInterstitial(string placementKey, AdsResult result)
        {
            if (_pendingInterstitialClosed.TryGetValue(placementKey, out var callback))
            {
                callback?.Invoke(result);
                _pendingInterstitialClosed.Remove(placementKey);
            }
        }

     private void CompleteRewarded(string placementKey, AdsResult result)
    {
        if (_pendingRewardedCompleted.TryGetValue(placementKey, out var callback))
        {
            callback?.Invoke(result);
            _pendingRewardedCompleted.Remove(placementKey);
        }
    }

        private AdsPlacementEntry GetPlacement(string placementKey)
        {
            if (_settings == null || _settings.placements == null)
                return null;

            return _settings.placements.Find(p => p != null && p.key == placementKey);
        }

        private string GetUnitId(string placementKey)
        {
            var placement = GetPlacement(placementKey);
            if (placement == null)
            {
                Debug.LogWarning($"[AdsKit][AdMob] Placement not found: {placementKey}");
                return string.Empty;
            }

#if UNITY_ANDROID
            if (UseTestAds())
                return GetAndroidTestUnitId(placement.format);

            if (!string.IsNullOrWhiteSpace(placement.admobAndroidUnitId))
                return placement.admobAndroidUnitId;

            return GetProfileFallbackUnitId(placement.format);

#elif UNITY_IOS
            if (UseTestAds())
                return GetIosTestUnitId(placement.format);

            if (!string.IsNullOrWhiteSpace(placement.admobIosUnitId))
                return placement.admobIosUnitId;

            return GetProfileFallbackUnitId(placement.format);

#else
            return string.Empty;
#endif
        }

        private string GetProfileFallbackUnitId(AdFormat format)
        {
            if (_profile == null)
                return string.Empty;

            switch (format)
            {
                case AdFormat.Banner:
                    return _profile.admobBannerUnitId;

                case AdFormat.Interstitial:
                    return _profile.admobInterstitialUnitId;

                case AdFormat.Rewarded:
                    return _profile.admobRewardedUnitId;

                default:
                    return string.Empty;
            }
        }

        private string GetAndroidTestUnitId(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner:
                    return "ca-app-pub-3940256099942544/6300978111";

                case AdFormat.Interstitial:
                    return "ca-app-pub-3940256099942544/1033173712";

                case AdFormat.Rewarded:
                    return "ca-app-pub-3940256099942544/5224354917";

                default:
                    return string.Empty;
            }
        }

        private string GetIosTestUnitId(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner:
                    return "ca-app-pub-3940256099942544/2934735716";

                case AdFormat.Interstitial:
                    return "ca-app-pub-3940256099942544/4411468910";

                case AdFormat.Rewarded:
                    return "ca-app-pub-3940256099942544/1712485313";

                default:
                    return string.Empty;
            }
        }

        private bool UseTestAds()
        {
            return (_profile != null && _profile.forceTestMode) ||
                   (_settings != null && _settings.testMode);
        }

        private AdSize GetBannerSize(BannerSizeType sizeType)
        {
            switch (sizeType)
            {
                case BannerSizeType.Standard:
                    return AdSize.Banner;

                case BannerSizeType.MediumRectangle:
                    return AdSize.MediumRectangle;

                case BannerSizeType.Adaptive:
                default:
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            }
        }
    }
}