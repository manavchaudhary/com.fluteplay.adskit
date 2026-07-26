using System;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public class AdsService : MonoBehaviour
    {
        public static AdsService Instance { get; private set; }

        public bool IsReady => _provider != null && _provider.IsInitialized;

        private IAdsProvider _provider;
        private AdsSettingsAsset _settings;
        private AdsProfile _profile;

        public event Action<string, BannerPosition, BannerSizeType> OnBannerShown;
        public event Action<string> OnBannerHidden;
        public event Action<string> OnInterstitialClosed;
        public event Action<string, bool> OnRewardedCompleted;


        public event Action<AdsResult> OnInterstitialResult;
public event Action<AdsResult> OnRewardedResult;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            Debug.Log("[AdsKit] Initializing AdsService...");

            _profile = Resources.Load<AdsProfile>("AdsKit/Profiles/AdsProfile_Dev");
            if (_profile == null)
            {
                Debug.LogError("[AdsKit] AdsProfile not found in Resources!");
                return;
            }

            _settings = AdsSettingsLoader.Load();
            if (_settings == null)
            {
                Debug.LogError("[AdsKit] AdsSettings not loaded!");
                return;
            }

            _provider = new AdMobProvider();
            _provider.Initialize(_settings, _profile);

            Debug.Log("[AdsKit] Provider created. Waiting for SDK init...");
        }

        private AdsPlacementEntry GetPlacement(string key)
        {
            if (_settings == null || _settings.placements == null)
                return null;

            return _settings.placements.Find(p => p.key == key);
        }

        public void ShowBanner(string placementKey, BannerPosition position, BannerSizeType sizeType)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"[AdsService] ShowBanner blocked. Service not ready. placement={placementKey}");
                return;
            }

            var placement = GetPlacement(placementKey);
            if (placement == null || !placement.enabled)
            {
                Debug.LogWarning($"[AdsService] Banner blocked. Invalid or disabled placement: {placementKey}");
                return;
            }

            _provider.ShowBanner(placementKey, position, sizeType);
            Debug.Log($"[AdsService] Banner shown: {placementKey}");
            OnBannerShown?.Invoke(placementKey, position, sizeType);
        }

        public void HideBanner(string placementKey)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"[AdsService] HideBanner blocked. Service not ready. placement={placementKey}");
                return;
            }

            _provider.HideBanner();
            Debug.Log($"[AdsService] Banner hidden: {placementKey}");
            OnBannerHidden?.Invoke(placementKey);
        }

        public AdsResult ShowInterstitial(string placementKey)
        {
            AdsResult validation = ValidatePlacement(AdFormat.Interstitial, placementKey);

            if (!validation.Success)
            {
                Debug.LogWarning($"[AdsService] Interstitial blocked. {validation}");

                OnInterstitialClosed?.Invoke(placementKey);
                OnInterstitialResult?.Invoke(validation);

                return validation;
            }

            AdsResult showResult = _provider.ShowInterstitial(placementKey, result =>
            {
                Debug.Log($"[AdsService] Interstitial result callback: {result}");

                OnInterstitialClosed?.Invoke(placementKey);
                OnInterstitialResult?.Invoke(result);
            });

            Debug.Log($"[AdsService] Interstitial show request result: {showResult}");

            if (!showResult.Success)
            {
                OnInterstitialClosed?.Invoke(placementKey);
                OnInterstitialResult?.Invoke(showResult);
            }

            return showResult;
        }

        public AdsResult ShowRewarded(string placementKey)
        {
            AdsResult validation = ValidatePlacement(AdFormat.Rewarded, placementKey);

            if (!validation.Success)
            {
                Debug.LogWarning($"[AdsService] Rewarded blocked. {validation}");

                OnRewardedCompleted?.Invoke(placementKey, false);
                OnRewardedResult?.Invoke(validation);

                return validation;
            }

            AdsResult showResult = _provider.ShowRewarded(placementKey, result =>
            {
                Debug.Log($"[AdsService] Rewarded result callback: {result}");

                OnRewardedCompleted?.Invoke(placementKey, result.Success);
                OnRewardedResult?.Invoke(result);
            });

            Debug.Log($"[AdsService] Rewarded show request result: {showResult}");

            if (!showResult.Success)
            {
                OnRewardedCompleted?.Invoke(placementKey, false);
                OnRewardedResult?.Invoke(showResult);
            }

            return showResult;
        }

        public bool IsAdReady(AdFormat format, string placementKey)
        {
            return IsReady && _provider.IsReady(format, placementKey);
        }

        public bool IsRewardedReady(string placementKey)
        {
            return IsAdReady(AdFormat.Rewarded, placementKey);
        }

        public bool IsInterstitialReady(string placementKey)
        {
            return IsAdReady(AdFormat.Interstitial, placementKey);
        }

        public bool IsBannerReady(string placementKey)
        {
            return IsAdReady(AdFormat.Banner, placementKey);
        }

        public void OpenAdInspector()
        {
            if (!IsReady)
            {
                Debug.LogWarning("[AdsService] OpenAdInspector blocked. Service not ready.");
                return;
            }

            if (_provider is AdMobProvider adMobProvider)
            {
                adMobProvider.OpenAdInspector();
            }
        }

        private AdsResult ValidatePlacement(AdFormat format, string placementKey)
{
    if (_settings == null)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.SettingsNotLoaded);
    }

    if (_profile == null)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.ProfileNotLoaded);
    }

    if (_provider == null)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.ProviderNotCreated);
    }

    if (!_provider.IsInitialized)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.SdkNotInitialized);
    }

    if (!_settings.adsEnabled)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.AdsGloballyDisabled);
    }

    AdsPlacementEntry placement = GetPlacement(placementKey);

    if (placement == null)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.PlacementKeyNotDefined);
    }

    if (!placement.enabled)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.PlacementDisabled);
    }

    if (placement.format != format)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.PlacementFormatMismatch);
    }

    if (format == AdFormat.Rewarded && !_settings.rewardedEnabled)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.FormatDisabled);
    }

    if (format == AdFormat.Interstitial && !_settings.interstitialEnabled)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.FormatDisabled);
    }

    if (format == AdFormat.Banner && !_settings.bannerEnabled)
    {
        return AdsResult.Fail(placementKey, format, AdsErrorCodes.FormatDisabled);
    }

    return AdsResult.Ok(placementKey, format);
}
    }
}