# Changelog
All notable changes to this project will be documented in this file.

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
