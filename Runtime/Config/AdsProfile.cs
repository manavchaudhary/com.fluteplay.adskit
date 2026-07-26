using System;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    [CreateAssetMenu(fileName = "AdsProfile", menuName = "FluteplayGames/AdsKit/Ads Profile")]
    public class AdsProfile : ScriptableObject
    {
        [Header("Game Info")]
        public string companyName = "FluteplayGames";
        public string gameName = "MyGame";
        public string bundleId = "com.company.mygame";

        [Header("MAX")]
        public string maxSdkKey = "";
        public string maxBannerUnitId = "";
        public string maxInterstitialUnitId = "";
        public string maxRewardedUnitId = "";

        [Header("AdMob App IDs")]
        public string admobAndroidAppId = "";
        public string admobIosAppId = "";

        [Header("AdMob Unit IDs")]
        public string admobBannerUnitId = "";
        public string admobInterstitialUnitId = "";
        public string admobRewardedUnitId = "";

        [Header("General")]
        public bool enableLogging = true;
        public bool forceTestMode = false;
    }
}