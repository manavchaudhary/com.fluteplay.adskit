using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public static class AdsSettingsLoader
    {
        private const string DefaultPath = "AdsKit/Settings/AdsSettings_Default";

        public static AdsSettingsAsset Load()
        {
            var asset = Resources.Load<AdsSettingsAsset>(DefaultPath);

            if (asset == null)
            {
                Debug.LogError($"[AdsKit] AdsSettings asset not found at Resources/{DefaultPath}");
                return null;
            }

            Debug.Log($"[AdsKit] AdsSettings loaded from Resources/{DefaultPath}");
            return asset;
        }
    }
}