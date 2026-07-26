using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public class AdsTester : MonoBehaviour
    {
        private void Update()
        {
            if (Ads.Service == null || !Ads.Service.IsReady)
            return;

            // if (Input.GetKeyDown(KeyCode.T))
            //    ShowBannerTop();

            //    if (Input.GetKeyDown(KeyCode.B))
            //        ShowBannerBottom();

            // if (Input.GetKeyDown(KeyCode.H))
            //     HideBanner();   

            // if (Input.GetKeyDown(KeyCode.I))
            //     ShowInterstitial();

            // if (Input.GetKeyDown(KeyCode.R))
            //     ShowReward();
        }


        public void ShowBannerAdaptiveTop()
        {
            Debug.Log("[Tester] ShowBannerAdaptiveTop button pressed");
            Ads.ShowBanner("HomeBanner", BannerPosition.Top, BannerSizeType.Adaptive);
        }

        public void ShowBannerAdaptiveBottom()
        {
            Debug.Log("[Tester] ShowBannerAdaptiveBottom button pressed");
            Ads.ShowBanner("HomeBanner", BannerPosition.Bottom, BannerSizeType.Adaptive);
        }


        public void ShowStandardBannerTop()
        {
            Debug.Log("[Tester] ShowStandardBannerTop button pressed");
            Ads.ShowBanner("HomeBanner", BannerPosition.Top, BannerSizeType.Standard);
        }

        public void ShowStandardBannerBottom()
        {
            Debug.Log("[Tester] ShowStandardBannerBottom button pressed");
            Ads.ShowBanner("HomeBanner", BannerPosition.Bottom, BannerSizeType.Standard);
        }

        public void ShowMediumRectangleBannerTop()
        {
            Debug.Log("[Tester] ShowMediumRectangleBannerTop button pressed");
            Ads.ShowBanner("LobbyAd", BannerPosition.Top, BannerSizeType.MediumRectangle);
        }


        public void ShowMediumRectangleBannerBottom()
        {
            Debug.Log("[Tester] ShowMediumRectangleBannerBottom button pressed");
            Ads.ShowBanner("LobbyAd", BannerPosition.Bottom, BannerSizeType.MediumRectangle);
        }

        public void HideBanner()
        {
            Ads.HideBanner("HomeBanner");
             Ads.HideBanner("LobbyAd");
             Debug.Log("[Tester] HideBanner button pressed");
        }

        public void OnClickShowInterstitial()
        {
            Debug.Log("[Tester] Interstitial button pressed");

            if (Ads.Service == null)
            {
                Debug.LogError("[Tester] Ads.Service is null");
                return;
            }

            if (!Ads.Service.IsReady)
            {
                Debug.LogWarning("[Tester] AdsService not ready");
                return;
            }

            Ads.ShowInterstitial("GameOverInterstitial");
        }

        public void ShowReward()
        {
            Debug.Log("[Tester] Reward button pressed");

            if (Ads.Service == null)
            {
                Debug.LogError("[Tester] Ads.Service is null");
                return;
            }

            if (!Ads.Service.IsReady)
            {
                Debug.LogWarning("[Tester] AdsService not ready");
                return;
            }

            Ads.ShowRewarded("ShopReward");
        }

        
    }
}