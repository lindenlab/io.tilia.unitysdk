# Getting Started

## Development Sandbox & Production Access

To get started using the Tilia SDK, you will need to request a Client ID and Client Secret from Tilia which will give you access to the sandbox (staging) environment.

To request credentials and kick off the process to access production, please send an email to <unitySDK@tilia.io> with the information listed below:
* Contact name:
* Contact phone:
* Contact email address:
* Business/app name:
* Business/app description:
* Do you require virtual tokens?:

You'll receive an email response with your sandbox credentials.  
For production, a Tilia agent will be reaching out shortly to get you set up.<br/><br/>

## The Tilia Component

Begin by dropping the TiliaPay SDK into your Unity scene from the prefabs folder of the Tilia package.

When viewing the Tilia component in the inspector, there are a few options you will want to configure.

The first task will be to insert your Client ID and Client Secret into the appropriate input fields.

When testing your Tilia setup, leave the 'Staging Environment' checkbox selected. This switches between the Staging URI (when checked) and the Production URI (when unchecked). Tilia recommends all testing be done on the staging URI until you are ready to deploy your project.

The Staging URI and Production URI do not need to be changed, and can be left at their default values of staging.tilia-inc.com and tilia-inc.com.<br/><br/>

## The Widget Browser

This version of the Tilia SDK relies on a web browser component to present users with access to the Tilia Web Widget from within Unity. This widget is required to allow users to agree to the TOS, enter their billing information, and add new payment methods.

The 'Web Browser' field on the Tilia component specifies the UI gameobject that contains the browser component.

Future versions of this SDK are planned to include a fully native Unity UI for all required aspects of user interaction.<br/><br/>

## Demo

The Samples/Demo folder contains a Unity scene with the Tilia prefab and a simple sandbox UI for experimenting with the SDK.
