# Tilia SDK Widget Browser Support

To fully utilize the Tilia Unity SDK you will need to have a supported embedded web browser in your project.

The Tilia SDK currently supports two web browser options (see below).

## Simple Web Browser

This is a basic open-source Chromium web browser available for free under the GNU GPL 3.0. License details can be found at https://github.com/tunerok/unity_browser/blob/master/LICENSE.

 - This browser option currently only supports standalone Windows projects.
 - IL2CPP support: Simple Web Browser currently only supports the Mono scripting backend. If your project requires IL2CPP support, please use another browser option. This is because Unity has not implemented managed process control in the IL2CPP engine yet.

Simple Web Browser Repo: https://github.com/tunerok/unity_browser

Some modifications have been made to this open-source code to make it compatible with the Tilia SDK.

Simple Web Browser is not affiliated with Tilia in any way.

## Zen Fulcrum

The Embedded Web Browser by Zen Fulcrum LLC is supported by the Tilia Unity SDK. To use this popular browser option you will need to purchase it separately from the Unity Asset Store and import it into your project.

 - This browser currently supports standalone MacOS, Windows, and Linux (Experimental) projects.
 - IL2CPP support: This browser currently supports IL2CPP on Windows 64-bit and Mac 64-bit. Mono is supported on all standalone platforms.

Embedded Web Browser: https://assetstore.unity.com/packages/tools/gui/embedded-browser-55459

If you have Zen Fulcrum (ZFBrowser) in your project, you should install the Tilia/Browsers/TiliaBrowserZF.unitypackage package file.

After importing Zen Fulcrum and the TiliaBrowserZF package, you will need add the ZFBrowser assembly definition as a reference to the Tilia assembly definition.
 - Select the Tilia.asmdef file in the Tilia folder.
 - Add a new Assembly Definition Reference
 - Select ZFBrowser for the new reference.
 - Hit Apply at the bottom of the inspector window.

Zen Fulcrum is not affiliated with Tilia in any way.

## Other Browsers

If you do not wish to use any of these options, you can still integrate the SDK with another web browser of your choosing by creating a derived class from the TiliaBrowser component and implementing the browser-specific abstract functions on a new child class.
