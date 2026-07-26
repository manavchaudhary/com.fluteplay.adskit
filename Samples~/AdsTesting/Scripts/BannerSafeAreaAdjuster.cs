using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public class BannerSafeAreaAdjuster : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;

        private float _originalTop;
        private float _originalBottom;

        private void Start()
        {
            if (contentRoot == null)
            {
                Debug.LogWarning("[AdsKit][UI] ContentRoot not assigned.");
                return;
            }

            _originalTop = contentRoot.offsetMax.y;
            _originalBottom = contentRoot.offsetMin.y;

            if (Ads.Service != null)
            {
                Ads.Service.OnBannerShown += HandleBannerShown;
                Ads.Service.OnBannerHidden += HandleBannerHidden;
            }
        }

        private void OnDestroy()
        {
            if (Ads.Service != null)
            {
                Ads.Service.OnBannerShown -= HandleBannerShown;
                Ads.Service.OnBannerHidden -= HandleBannerHidden;
            }
        }

        private void HandleBannerShown(string placementKey, BannerPosition position, BannerSizeType sizeType)
        {
            if (contentRoot == null) return;

            float height = GetBannerHeight(sizeType);

            if (position == BannerPosition.Top)
            {
                contentRoot.offsetMax = new Vector2(contentRoot.offsetMax.x, -height);
                contentRoot.offsetMin = new Vector2(contentRoot.offsetMin.x, _originalBottom);
            }
            else
            {
                contentRoot.offsetMin = new Vector2(contentRoot.offsetMin.x, height);
                contentRoot.offsetMax = new Vector2(contentRoot.offsetMax.x, _originalTop);
            }

            Debug.Log($"[AdsKit][UI] Adjusted for banner. placement={placementKey} position={position} size={sizeType} height={height}");
        }

        private void HandleBannerHidden(string placementKey)
        {
            if (contentRoot == null) return;

            contentRoot.offsetMin = new Vector2(contentRoot.offsetMin.x, _originalBottom);
            contentRoot.offsetMax = new Vector2(contentRoot.offsetMax.x, _originalTop);

            Debug.Log($"[AdsKit][UI] Reset layout after banner hide. placement={placementKey}");
        }

        private float GetBannerHeight(BannerSizeType sizeType)
        {
            switch (sizeType)
            {
                case BannerSizeType.Standard:
                    return 50f;

                case BannerSizeType.MediumRectangle:
                    return 250f;

                case BannerSizeType.Adaptive:
                default:
                    return 90f;
            }
        }
    }
}