using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "Fluteplay/Ads/Ads Settings")]
    public class AdsSettingsAsset : ScriptableObject
    {
        [Header("Global")]
        public bool adsEnabled = true;
        public bool testMode = true;
        public string logLevel = "info";
        public int timeoutMs = 1500;

        [Header("Formats")]
        public bool bannerEnabled = true;

        public bool interstitialEnabled = true;
        public int interstitialCooldownSeconds = 45;
        public int interstitialMaxPerSession = 20;

        public bool rewardedEnabled = true;

        [Header("Placements")]
        public List<AdsPlacementEntry> placements = new();
    }

    [Serializable]
    public class AdsPlacementEntry
    {
        [Header("Placement")]
        public string key;
        public AdFormat format;
        public bool enabled = true;
        public int cooldownSeconds = 0;

        [Header("AdMob Unit IDs")]
        public string admobAndroidUnitId = "";
        public string admobIosUnitId = "";

        [Header("MAX Unit IDs - Future")]
        public string maxAndroidUnitId = "";
        public string maxIosUnitId = "";
    }
}