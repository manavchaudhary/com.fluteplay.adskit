# Fluteplay AdsKit

Reusable Unity ads framework for Fluteplay Games projects.

This package provides a common ads API for games so game code does not directly depend on AdMob implementation details.

Current provider:

* Google AdMob
* Mock Provider for Editor/Standalone testing

Supported ad formats:

* Banner
* Interstitial
* Rewarded

---

## Package Name

```json
com.fluteplay.adskit
```

Current version:

```json
1.3.0
```

---

## Main Goal

Game code should only call simple placement-based APIs:

```csharp
Ads.ShowRewarded("ShopReward");
Ads.ShowRewarded("LastTransaction");
Ads.ShowInterstitial("GameOverInterstitial");
Ads.ShowBanner("HomeBanner", BannerPosition.Bottom, BannerSizeType.Adaptive);
```

The actual AdMob Unit IDs are managed from `AdsSettings_Default`.

---

## Recommended Project Setup

The AdsKit package should remain outside Unity game projects.

Recommended structure:

```text
Workspace/
├── FluteplayPackages/
│   └── com.fluteplay.adskit/
│
├── AdsKitFramework/
│
└── FunGames/
```

In the game project `Packages/manifest.json`, add:

```json
"com.fluteplay.adskit": "file:/Users/manavchaudhary/Desktop/Workspace/FluteplayPackages/com.fluteplay.adskit"
```

---

## Required Game Project Resources

Each game must keep its own AdsKit configuration inside its own `Assets/Resources` folder.

Required structure:

```text
Assets
└── Resources
    └── AdsKit
        ├── Config
        │   └── ads_config
        │
        ├── Profiles
        │   ├── AdsProfile_Dev
        │   └── AdsProfile_Production
        │
        └── Settings
            └── AdsSettings_Default
```

The package should contain code only. Game-specific AdMob IDs should be stored in the game project, not inside the package.

---

## AdsSettings_Default

`AdsSettings_Default` is the central ad placement configuration.

Each placement contains:

```text
Key
Format
Enabled
Cooldown Seconds
AdMob Android Unit Id
AdMob iOS Unit Id
MAX Android Unit Id
MAX iOS Unit Id
```

Example placements:

```text
HomeBanner
LobbyAd
GameOverInterstitial
ShopReward
LastTransaction
AdStreak
SpinWheel
```

Example setup:

```text
ShopReward
Format: Rewarded
Enabled: true
AdMob Android Unit Id: ca-app-pub-xxx/xxxxxxxxxx
```

```text
LastTransaction
Format: Rewarded
Enabled: true
AdMob Android Unit Id: ca-app-pub-xxx/yyyyyyyyyy
```

---

## How AdsSettings_Default Is Loaded

AdsKit loads settings using Unity `Resources.Load`.

Expected path:

```text
Assets/Resources/AdsKit/Settings/AdsSettings_Default.asset
```

The load path used internally:

```csharp
Resources.Load<AdsSettingsAsset>("AdsKit/Settings/AdsSettings_Default");
```

Profile is loaded from:

```text
Assets/Resources/AdsKit/Profiles/AdsProfile_Dev.asset
```

Internal load path:

```csharp
Resources.Load<AdsProfile>("AdsKit/Profiles/AdsProfile_Dev");
```

---

## Initialization Flow

When `AdsService` is created:

```text
AdsService
    ↓
Loads AdsProfile_Dev
    ↓
Loads AdsSettings_Default
    ↓
Creates AdMobProvider
    ↓
Initializes MobileAds SDK
    ↓
Preloads all enabled Rewarded and Interstitial placements
```

Rewarded and Interstitial placements are preloaded automatically.

Banner ads are loaded only when requested by screen code.

---

## Auto-loaded Placements

On SDK initialization, AdsKit automatically loads:

```text
Enabled Rewarded placements
Enabled Interstitial placements
```

Example startup logs:

```text
[AdsKit][AdMob] Initialized
[AdsKit][AdMob] Loading rewarded: placement=ShopReward
[AdsKit][AdMob] Loading rewarded: placement=LastTransaction
[AdsKit][AdMob] Loading interstitial: placement=GameOverInterstitial
```

Banner placements are not auto-loaded.

Use:

```csharp
Ads.ShowBanner("HomeBanner", BannerPosition.Bottom, BannerSizeType.Adaptive);
```

---

## Basic Usage

### Show Banner

```csharp
Ads.ShowBanner("HomeBanner", BannerPosition.Bottom, BannerSizeType.Adaptive);
```

### Hide Banner

```csharp
Ads.HideBanner("HomeBanner");
```

### Show Interstitial

```csharp
AdsResult result = Ads.ShowInterstitial("GameOverInterstitial");

if (!result.Success)
{
    Debug.LogWarning($"Interstitial failed. Code={result.Code}, Message={result.Message}");
}
```

### Show Rewarded

```csharp
AdsResult result = Ads.ShowRewarded("ShopReward");

if (!result.Success)
{
    Debug.LogWarning($"Rewarded failed. Code={result.Code}, Message={result.Message}");
}
```

---

## Rewarded Callback

Subscribe to rewarded result:

```csharp
private void OnEnable()
{
    Ads.Service.OnRewardedResult += OnRewardedResult;
}

private void OnDisable()
{
    Ads.Service.OnRewardedResult -= OnRewardedResult;
}

private void OnRewardedResult(AdsResult result)
{
    if (result.PlacementKey != "LastTransaction")
        return;

    if (result.Success)
    {
        Debug.Log("Reward earned. Unlock feature.");
    }
    else
    {
        Debug.LogWarning($"Reward failed. Code={result.Code}, Message={result.Message}");
    }
}
```

---

## Example: Transaction History With Rewarded Ad

```csharp
public void OnClickTransactionHistory()
{
    AdsResult result = Ads.ShowRewarded("LastTransaction");

    if (!result.Success)
    {
        Debug.LogWarning($"Ad failed. Code={result.Code}, Message={result.Message}");

        switch (result.Code)
        {
            case AdsErrorCodes.SdkNotInitialized:
                ShowMessage("Ad system is not ready. Please try again.");
                break;

            case AdsErrorCodes.PlacementKeyNotDefined:
                ShowMessage("Ad placement configuration is missing.");
                break;

            case AdsErrorCodes.AdUnitIdMissing:
                ShowMessage("Ad unit ID is missing.");
                break;

            case AdsErrorCodes.AdNotLoaded:
            case AdsErrorCodes.AdLoadFailed:
                ShowMessage("Ad is currently unavailable. Please try again later.");
                break;

            default:
                ShowMessage("Ad could not be shown. Please try again.");
                break;
        }

        return;
    }

    Debug.Log("Rewarded ad started.");
}
```

---

## AdsResult

`AdsResult` provides detailed result information.

Fields:

```csharp
public bool Success;
public int Code;
public string Message;
public string PlacementKey;
public AdFormat Format;
public string ProviderName;
public string ProviderError;
```

Example:

```csharp
AdsResult result = Ads.ShowRewarded("LastTransaction");

Debug.Log(result.Success);
Debug.Log(result.Code);
Debug.Log(result.Message);
Debug.Log(result.ProviderError);
```

---

## Error Codes

Error codes are defined in:

```text
Runtime/Core/AdsErrorCodes.cs
```

Current codes:

```text
0  = Success
1  = Ad SDK is not initialized
2  = Ads settings asset is not loaded
3  = Ads profile asset is not loaded
4  = Placement key is not defined in AdsSettings
5  = Placement format does not match requested ad format
6  = Placement is disabled
7  = Ads are globally disabled
8  = This ad format is disabled from settings
9  = Ad Unit ID is missing for this placement
10 = Ad is not loaded or not ready
11 = Ad failed to show
12 = Rewarded ad closed without earning reward
13 = Ad provider is not created
14 = Ad failed to load / AdMob could not serve ad
99 = Unknown ad error
```

---

## Test Mode

Test ads are used when either of these is enabled:

```text
AdsSettings_Default.testMode = true
AdsProfile_Dev.forceTestMode = true
```

When test mode is enabled, AdsKit uses Google test Ad Unit IDs.

For live ads, disable both:

```text
testMode = false
forceTestMode = false
```

Use live mode carefully and avoid clicking your own live ads.

---

## AdMob Test Unit IDs

Android test IDs:

```text
Banner:       ca-app-pub-3940256099942544/6300978111
Interstitial: ca-app-pub-3940256099942544/1033173712
Rewarded:     ca-app-pub-3940256099942544/5224354917
```

iOS test IDs:

```text
Banner:       ca-app-pub-3940256099942544/2934735716
Interstitial: ca-app-pub-3940256099942544/4411468910
Rewarded:     ca-app-pub-3940256099942544/1712485313
```

---

## Important Notes

### 1. Do not hardcode AdMob IDs in game code

Wrong:

```csharp
string adUnitId = "ca-app-pub-xxx/xxxx";
```

Correct:

```csharp
Ads.ShowRewarded("ShopReward");
```

Set the AdMob Unit ID in `AdsSettings_Default`.

---

### 2. Do not edit AdsKit code inside game projects

AdsKit code should be edited only in:

```text
FluteplayPackages/com.fluteplay.adskit
```

Game projects should only consume the package.

---

### 3. Keep game-specific config inside game project

Game-specific files should be placed in:

```text
Assets/Resources/AdsKit/
```

Not inside:

```text
Packages/com.fluteplay.adskit/
```

---

### 4. Rewarded ads can fail

Rewarded ads may fail because of:

```text
No fill
Network issue
AdMob account issue
Invalid ad unit
SDK not initialized
Ad not loaded yet
Placement disabled
```

Always check `AdsResult`.

---

## Version History

### 1.1.0

Added:

```text
Placement-wise AdMob Unit IDs
Multiple rewarded placement support
Multiple interstitial placement support
AdsResult response model
AdsErrorCodes
Better error handling
Auto-load for enabled rewarded and interstitial placements
```

### 1.0.0

Initial version:

```text
Basic AdMob support
Banner
Interstitial
Rewarded
Mock provider
Basic placement keys
```

---

## Future Roadmap

Planned improvements:

```text
MAX Provider
Unity Ads Provider
Firebase Analytics hooks
Remote Config support
Better consent handling
Ad revenue tracking
Editor tools for validating placement configuration
```

---

## Maintainer

Fluteplay Games
