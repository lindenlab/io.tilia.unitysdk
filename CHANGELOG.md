# Changelog
All notable changes to this project will be documented in this file.

## [0.9.3] - 2021-08-30
### Added
 - Expanded TiliaDemo scene for escrow testing.

### Changed
 - Removed hard-coded file paths inside third-party SimpleWebBrowser dependency. Now relies on AssetDatabase.GUIDToAssetPath.
 - Removed hard-coded file path for TiliaPayIntegrator.html file used in sandbox testing. Now relies on AssetDatabase.GUIDToAssetPath.
 - CreateEscrow is no longer a private function.

## [0.9.2] - 2021-08-25
### Added
 - Improved support for handling 500 server errors from API backend.
 - Moved input classes to new TiliaInput.cs with abstract parent class.
 - Added TiliaNewInvoice class for consistency with input classes being differentiated from output classes. (e.g. TiliaNewUser, TiliaNewPayout)
 - Additional UI functionality for purchase flow testing on sample TiliaDemo scene.
 - Additional function documentation added to Tilia.cs

### Changed
 - CreateInvoice is no longer a private function.
 - Fixed some 'declared but never used' warnings coming from third-party SimpleWebBrowser dependency.
 - Fixed 'WWW is obsolete' warning coming from third-party SimpleWebBrowser dependency.
 - Fixed 'OnPageLoaded event not used' warning coming from third-party SimpleWebBrowser dependency.
 - Function parameter names for API calls made more consistent.
 - Fixed some integers being incorrectly serialized as strings.
 - Fixed bug in deserializing line_items, subitems, and payment_methods due to API docs showing incorrect payload format.

## [0.9.1] - 2021-08-24
### Added
- README.md file
- Unity package manifest (package.json)
- This Changelog.

### Changed
 - All requests to TiliaPay API now include custom web headers with Unity version/product, and SDK version.
 - Fixed console warning about variable ex being defined but not used in Tilia.cs.
 - Removed dummy test data from demo sample.
 - Forced all integer parsing to InvariantCulture for globalization.

## [0.9.0] - 2021-08-24
### Added
 - All basic API functions outlined in https://www.tilia.io/docs/api-intro/
 - Initial support for widget integration using SimpleWebBrowser (https://github.com/tunerok/unity_browser)
 - Fledgling Unity TiliaDemo scene for sandbox testing the SDK.
 - This is a pre-release alpha build.
