using System;

namespace FluteplayGames.AdsKit
{
    public enum AdFormat
    {
        Banner,
        Interstitial,
        Rewarded
    }

    public enum BannerSizeType
    {
        Standard,
        Adaptive,
        MediumRectangle
    }

    public enum BannerPosition
    {
        Top,
        Bottom
    }

    public interface IAdsProvider
    {
        string Name { get; }
        bool IsInitialized { get; }

        void Initialize(AdsSettingsAsset settings, AdsProfile profile);

        bool IsReady(AdFormat format, string placementKey);

        void Load(AdFormat format, string placementKey);

        void ShowBanner(string placementKey, BannerPosition position, BannerSizeType sizeType);
        void HideBanner();

       AdsResult ShowRewarded(string placementKey, Action<AdsResult> onCompleted);
       AdsResult ShowInterstitial(string placementKey, Action<AdsResult> onClosed = null);
    }
}