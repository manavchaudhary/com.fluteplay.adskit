using System;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public class MockProvider : IAdsProvider
    {
        public string Name => "MOCK";
        public bool IsInitialized { get; private set; }

        private AdsSettingsAsset _settings;
        private AdsProfile _profile;

        public void Initialize(AdsSettingsAsset settings, AdsProfile profile)
        {
            _settings = settings;
            _profile = profile;

            IsInitialized = true;
            Debug.Log("[AdsKit][MOCK] Initialized");
        }

        public bool IsReady(AdFormat format, string placementKey)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return true;
#else
            return false;
#endif
        }

        public void Load(AdFormat format, string placementKey)
        {
            Debug.Log($"[AdsKit][MOCK] Load called: format={format}, placement={placementKey}");
        }

        public void ShowBanner(string placementKey, BannerPosition position, BannerSizeType sizeType)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            Debug.Log($"[AdsKit][MOCK] ShowBanner: {placementKey} position={position} size={sizeType}");
#else
            Debug.LogWarning($"[AdsKit][MOCK] ShowBanner failed: {placementKey}");
#endif
        }

        public void HideBanner()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            Debug.Log("[AdsKit][MOCK] HideBanner");
#endif
        }

        public AdsResult ShowInterstitial(string placementKey, Action<AdsResult> onClosed = null)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            Debug.Log($"[AdsKit][MOCK] ShowInterstitial: {placementKey}");

            AdsResult result = AdsResult.Ok(
                placementKey,
                AdFormat.Interstitial,
                Name
            );

            onClosed?.Invoke(result);
            return result;
#else
            AdsResult result = AdsResult.Fail(
                placementKey,
                AdFormat.Interstitial,
                AdsErrorCodes.SdkNotInitialized,
                Name,
                "Mock provider is only available in Editor/Standalone"
            );

            onClosed?.Invoke(result);
            return result;
#endif
        }

        public AdsResult ShowRewarded(string placementKey, Action<AdsResult> onCompleted)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            Debug.Log($"[AdsKit][MOCK] ShowRewarded: {placementKey} -> completed=true");

            AdsResult result = AdsResult.Ok(
                placementKey,
                AdFormat.Rewarded,
                Name
            );

            onCompleted?.Invoke(result);
            return result;
#else
            AdsResult result = AdsResult.Fail(
                placementKey,
                AdFormat.Rewarded,
                AdsErrorCodes.SdkNotInitialized,
                Name,
                "Mock provider is only available in Editor/Standalone"
            );

            onCompleted?.Invoke(result);
            return result;
#endif
        }
    }
}