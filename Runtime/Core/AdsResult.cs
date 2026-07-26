using System;

namespace FluteplayGames.AdsKit
{
    [Serializable]
    public struct AdsResult
    {
        public bool Success;
        public int Code;
        public string Message;
        public string PlacementKey;
        public AdFormat Format;
        public string ProviderName;
        public string ProviderError;

        public static AdsResult Ok(string placementKey, AdFormat format, string providerName = "")
        {
            return new AdsResult
            {
                Success = true,
                Code = AdsErrorCodes.Success,
                Message = AdsErrorCodes.GetMessage(AdsErrorCodes.Success),
                PlacementKey = placementKey,
                Format = format,
                ProviderName = providerName,
                ProviderError = string.Empty
            };
        }

        public static AdsResult Fail(
            string placementKey,
            AdFormat format,
            int code,
            string providerName = "",
            string providerError = "")
        {
            return new AdsResult
            {
                Success = false,
                Code = code,
                Message = AdsErrorCodes.GetMessage(code),
                PlacementKey = placementKey,
                Format = format,
                ProviderName = providerName,
                ProviderError = providerError
            };
        }

        public override string ToString()
        {
            return $"Success={Success}, Code={Code}, Message={Message}, Placement={PlacementKey}, Format={Format}, Provider={ProviderName}, ProviderError={ProviderError}";
        }
    }
}