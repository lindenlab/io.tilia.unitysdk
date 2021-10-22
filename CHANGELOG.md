# Changelog
All notable changes to this project will be documented in this file.

## [0.9.7] - 2021-10-21
### Changed
 - Dependency on local HTML file removed for web widget. Now opens directly to Tilia.IO website for widget. This fixes the KYC widget flow failing to load properly.
 - Widget/TiliaPayIntegrator.html moved to Samples folder, as it is no longer required, but may be a useful reference for developers to create their own alternative to the default.
 - Browser specific code removed from TiliaPay and implemented into TiliaBrowser class instead.
 - Added second set of credentials for Production environment, as they will sometimes be different than the Staging environment credentials.
 - Moved 3rd Party folder out of Runtime.
 - After installing the Tilia Unity SDK, you must now pick a web browser support package to install as well. Included in Tilia/Browsers folder.

### Added
 - URL parameters added to TiliaPay component for specifying Production and Staging widget URLs.
 - New TiliaBrowser abstract class for handling widget operations outside of main TiliaPay class. This allows for adding support for different web browser plugins in the future.
 - Added TiliaBrowserSWB class that implements TiliaBrowser for the SimpleWebBrowser 3rd-party browser plugin included with the Tilia SDK.
 - Added TiliaBrowserZF class that implements TiliaBrowser for the Embedded Web Browser by Zen Fulcrum (Available on Unity Asset Store, not included).

## [0.9.6] - 2021-10-07
### Changed
 - Namespace changed from Tilia.Pay to Tilia
 - Primary class name changed from Tilia to TiliaPay to remove ambiguity with namespace.
 - Removed Test JS button from sample demo scene which no longer serves any purpose.
 - Cleaned up some commented out lines of code that are no longer needed.
 - Clarified in sample demo scene that price is in cents, not dollars.
 - Deleted unnecessary/unused 60mb zip file from third party SimpleWebBrowser component.
 - Added Editor-only assembly definition to third-party SimpleWebBrowser/Editor directory.
 - Fixed post build editor script in third party SimpleWebBrowser to remove hard-coded file paths. Now relies on AssetDatabase.GUIDToAssetPath.
 - Added HideUI option to third-party SimpleWebBrowser to prevent URL bar from showing up. Separates this behaviour from UIEnabled which has other side effects.
 - Removed all PII variables from TiliaPaymentMethods.TiliaPaymentMethod as these are considered deprecated.
 - Removed redundant PaymentMethodID from TiliaPaymentMethods.TiliaPaymentMethod, API sends it as a duplicate of ID
 - Removed AccountAlreadyExists bool from TiliaRegistration, deprecated in API.
 - LoggingEnabled switch on TiliaPay SDK is no longer hidden from the inspector.

### Added
 - New Payout Testing panel in sample demo scene for sandbox testing of payouts.
 - Headers and tooltips added to TiliaPay component in inspector.

## [0.9.5] - 2021-09-27
### Changed
 - Fixed FormatException errors that could crop up when server returned empty or malformed datetime fields in escrow invoices.
 - Fixed third-party SimpleWebBrowser not hiding the address bar like it was supposed to.
 - Logging to console now disabled by default when not run from Unity Test Runner.

## [0.9.4] - 2021-09-22
### Added
 - Prefabs folder added with TiliaPay SDK prefab.
 - Updated documentation on getting started.
 - Tilia now has it own assembly definition.

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
