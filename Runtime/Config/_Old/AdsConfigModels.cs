using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    [Serializable]
    public class AdsConfig
    {
        public GlobalConfig global = new GlobalConfig();
        public List<ProviderConfig> providers = new List<ProviderConfig>();
        public FormatsConfig formats = new FormatsConfig();
        public List<PlacementConfig> placements = new List<PlacementConfig>();

        public ProviderConfig GetProvider(string name)
        {
            return providers.Find(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
        }

        public PlacementConfig GetPlacement(string key)
        {
            return placements.Find(p => string.Equals(p.key, key, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Serializable]
    public class GlobalConfig
    {
        public bool adsEnabled = true;
        public bool testMode = true;
        public string logLevel = "info"; // debug/info/error
        public int timeoutMs = 1500;
    }

    [Serializable]
    public class ProviderConfig
    {
        public string name;      // "MAX" / "ADMOB" / "MOCK"
        public bool enabled = true;
        public int priority = 0; // lower = earlier
    }

    [Serializable]
    public class FormatsConfig
    {
        public BannerFormat banner = new BannerFormat();
        public InterstitialFormat interstitial = new InterstitialFormat();
        public RewardedFormat rewarded = new RewardedFormat();
    }

    [Serializable]
    public class BannerFormat
    {
        public bool enabled = true;
    }

    [Serializable]
    public class InterstitialFormat
    {
        public bool enabled = true;
        public int cooldownSeconds = 45;
        public int maxPerSession = 20;
    }

    [Serializable]
    public class RewardedFormat
    {
        public bool enabled = true;
    }

    [Serializable]
    public class PlacementConfig
    {
        public string key;
        public string format; // "banner"/"interstitial"/"rewarded"
        public bool enabled = true;

        // optional overrides
        public int cooldownSeconds = -1; // -1 = use format default
        public int maxPerDay = -1;       // optional future use
        public string providerOverride;  // "MAX"/"ADMOB" optional
    }
}