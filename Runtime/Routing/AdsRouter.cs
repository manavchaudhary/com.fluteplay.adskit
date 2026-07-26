using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluteplayGames.AdsKit
{
    public class AdsRouter
    {
        private readonly AdsConfig _config;
        private readonly Dictionary<string, float> _lastShownTime = new Dictionary<string, float>();

        private int _interstitialShownThisSession = 0;

        public AdsRouter(AdsConfig config)
        {
            _config = config;
        }

        public bool CanShow(string placementKey, out PlacementConfig placement, out AdFormat format, out string reason)
        {
            placement = null;
            format = default;
            reason = null;

            if (_config == null)
            {
                reason = "ConfigNull";
                return false;
            }

            if (!_config.global.adsEnabled)
            {
                reason = "AdsDisabledGlobal";
                return false;
            }

            placement = _config.GetPlacement(placementKey);
            if (placement == null)
            {
                reason = $"PlacementNotFound:{placementKey}";
                return false;
            }

            if (!placement.enabled)
            {
                reason = $"PlacementDisabled:{placementKey}";
                return false;
            }

            if (!TryParseFormat(placement.format, out format))
            {
                reason = $"InvalidFormat:{placement.format}";
                return false;
            }

            // Format enabled checks
            if (format == AdFormat.Banner && !_config.formats.banner.enabled)
            {
                reason = "FormatDisabled:banner";
                return false;
            }
            if (format == AdFormat.Interstitial && !_config.formats.interstitial.enabled)
            {
                reason = "FormatDisabled:interstitial";
                return false;
            }
            if (format == AdFormat.Rewarded && !_config.formats.rewarded.enabled)
            {
                reason = "FormatDisabled:rewarded";
                return false;
            }

            // Session cap for interstitial
            if (format == AdFormat.Interstitial)
            {
                if (_interstitialShownThisSession >= _config.formats.interstitial.maxPerSession)
                {
                    reason = "InterstitialSessionCapReached";
                    return false;
                }
            }

            // Cooldown
            int cooldown = GetCooldownSeconds(format, placement);
            if (cooldown > 0)
            {
                if (_lastShownTime.TryGetValue(placementKey, out var lastTime))
                {
                    float elapsed = Time.realtimeSinceStartup - lastTime;
                    if (elapsed < cooldown)
                    {
                        reason = $"CooldownActive:{placementKey} remaining={(cooldown - elapsed):0}s";
                        return false;
                    }
                }
            }

            return true;
        }

        public void MarkShown(string placementKey, AdFormat format)
        {
            _lastShownTime[placementKey] = Time.realtimeSinceStartup;
            if (format == AdFormat.Interstitial) _interstitialShownThisSession++;
        }

        public List<string> GetProviderChain(PlacementConfig placement)
        {
            // If placement has override, use only that provider
            if (!string.IsNullOrWhiteSpace(placement.providerOverride))
                return new List<string> { placement.providerOverride.Trim() };

            // Else use enabled providers sorted by priority
            var list = new List<ProviderConfig>(_config.providers);
            list.Sort((a, b) => a.priority.CompareTo(b.priority));

            var chain = new List<string>();
            foreach (var p in list)
            {
                if (p != null && p.enabled && !string.IsNullOrWhiteSpace(p.name))
                    chain.Add(p.name.Trim());
            }
            return chain;
        }

        private int GetCooldownSeconds(AdFormat format, PlacementConfig placement)
        {
            if (placement.cooldownSeconds >= 0) return placement.cooldownSeconds;

            if (format == AdFormat.Interstitial) return _config.formats.interstitial.cooldownSeconds;
            return 0; // rewarded/banner default no cooldown here
        }

        private bool TryParseFormat(string s, out AdFormat format)
        {
            format = default;
            if (string.IsNullOrWhiteSpace(s)) return false;

            s = s.Trim().ToLowerInvariant();
            switch (s)
            {
                case "banner": format = AdFormat.Banner; return true;
                case "interstitial": format = AdFormat.Interstitial; return true;
                case "rewarded": format = AdFormat.Rewarded; return true;
                default: return false;
            }
        }
    }
}