using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public static class Ads
    {
        public static AdsService Service
        {
            get
            {
                if (AdsService.Instance == null)
                {
                    Debug.Log("[AdsKit] AutoCreate AdsService");

                    var go = new GameObject("[AdsService]");
                    go.AddComponent<AdsService>();
                }

                return AdsService.Instance;
            }
        }

        public static void ShowBanner(string key, BannerPosition pos, BannerSizeType size)
        {
            Service?.ShowBanner(key, pos, size);
        }

        public static void HideBanner(string key)
        {
            Service?.HideBanner(key);
        }

       public static AdsResult ShowInterstitial(string key)
{
    return Service != null
        ? Service.ShowInterstitial(key)
        : AdsResult.Fail(key, AdFormat.Interstitial, AdsErrorCodes.ProviderNotCreated);
}

        public static AdsResult ShowRewarded(string key)
{
    return Service != null
        ? Service.ShowRewarded(key)
        : AdsResult.Fail(key, AdFormat.Rewarded, AdsErrorCodes.ProviderNotCreated);
}

        public static bool IsRewardedReady(string key)
        {
            return Service != null && Service.IsRewardedReady(key);
        }

        public static bool IsInterstitialReady(string key)
        {
            return Service != null && Service.IsInterstitialReady(key);
        }

        public static bool IsBannerReady(string key)
        {
            return Service != null && Service.IsBannerReady(key);
        }

        public static void OpenAdInspector()
        {
            Service?.OpenAdInspector();
        }
    }
}