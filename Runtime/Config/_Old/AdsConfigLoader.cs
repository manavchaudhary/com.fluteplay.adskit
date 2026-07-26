using System;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public static class AdsConfigLoader
    {
        private const string OverridePath = "AdsKitOverride/ads_config";
        private const string DefaultPath = "AdsKit/Config/ads_config";

        public static AdsConfig Load(out string source)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(OverridePath);

            if (jsonAsset != null)
            {
                source = $"Resources/{OverridePath}";
                return Parse(jsonAsset.text, source);
            }

            jsonAsset = Resources.Load<TextAsset>(DefaultPath);

            if (jsonAsset != null)
            {
                source = $"Resources/{DefaultPath}";
                return Parse(jsonAsset.text, source);
            }

            source = "not found";
            Debug.LogError("[AdsKit] ads_config.json not found in override or default Resources path.");
            return null;
        }

        private static AdsConfig Parse(string json, string source)
        {
            try
            {
                var config = JsonUtility.FromJson<AdsConfig>(json);
                Debug.Log($"[AdsKit] Config loaded from: {source}");
                Debug.Log($"[AdsKit] Config json length: {json.Length}");
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdsKit] Config parse error from {source}: {e.Message}");
                return null;
            }
        }
    }
}