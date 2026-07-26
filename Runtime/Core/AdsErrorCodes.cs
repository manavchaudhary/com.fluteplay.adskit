using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public static class AdsErrorCodes
    {
        public const int Success = 0;

        public const int SdkNotInitialized = 1;
        public const int SettingsNotLoaded = 2;
        public const int ProfileNotLoaded = 3;
        public const int PlacementKeyNotDefined = 4;
        public const int PlacementFormatMismatch = 5;
        public const int PlacementDisabled = 6;

        public const int AdsGloballyDisabled = 7;
        public const int FormatDisabled = 8;

        public const int AdUnitIdMissing = 9;
        public const int AdNotLoaded = 10;
        public const int AdFailedToShow = 11;
        public const int RewardNotEarned = 12;

        public const int ProviderNotCreated = 13;
        public const int AdLoadFailed = 14;
        public const int Unknown = 99;

        public static string GetMessage(int code)
        {
            switch (code)
            {
                case Success:
                    return "Success";

                case SdkNotInitialized:
                    return "Ad SDK is not initialized";

                case SettingsNotLoaded:
                    return "Ads settings asset is not loaded";

                case ProfileNotLoaded:
                    return "Ads profile asset is not loaded";

                case PlacementKeyNotDefined:
                    return "Placement key is not defined in AdsSettings";

                case PlacementFormatMismatch:
                    return "Placement format does not match requested ad format";

                case PlacementDisabled:
                    return "Placement is disabled";

                case AdsGloballyDisabled:
                    return "Ads are globally disabled";

                case FormatDisabled:
                    return "This ad format is disabled from settings";

                case AdUnitIdMissing:
                    return "Ad Unit ID is missing for this placement";

                case AdNotLoaded:
                    return "Ad is not loaded or not ready";

                case AdFailedToShow:
                    return "Ad failed to show";

                case RewardNotEarned:
                    return "Rewarded ad closed without earning reward";

                case ProviderNotCreated:
                    return "Ad provider is not created";

                case AdLoadFailed:
                    return "Ad failed to load / AdMob could not serve ad";

                case Unknown:
                default:
                    return "Unknown ad error";
            }
        }
    }
}
