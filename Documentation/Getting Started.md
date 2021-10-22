# Getting Started

## Step 1: Development Sandbox & Production Access

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

## Step 2: The Widget Browser

This version of the Tilia SDK relies on an embedded web browser component to present users with access to the Tilia Web Widget from within Unity. This widget is required to allow users to agree to the TOS, enter their billing information, and add new payment methods.

To full utilize the Tilia SDK, you must install one of the browser support packages found in the Tilia/Browser folder. Please see the README file in that folder for specifics on which browser to use.

Future versions of this SDK are planned to include a fully (as much as possible) native Unity UI for all required aspects of user interaction.<br/><br/>

## Step 3: The Tilia Component

Begin by dropping the TiliaPay SDK into your Unity scene from the prefabs folder of the Tilia package. If you do not see a TiliaPay SDK object, it is likely because you haven't completed Step 2 above. The SDK prefabs are browser-specific, and will be installed along with your chosen browser support.

When viewing the Tilia component in the inspector, there are a few options you will want to configure.

The first task will be to insert your Client ID and Client Secret into the appropriate input fields. There are separete production and staging credentials. If you only have a single set of credentials from Tilia, you can enter the same credentials in both environments.

When testing your Tilia setup, leave the 'Staging Environment' checkbox selected. This switches between the Staging URI (when checked) and the Production URI (when unchecked). Tilia recommends all testing be done on the staging URI until you are ready to deploy your project.

The Staging URI and Production URI do not need to be changed, and can be left at their default values of staging.tilia-inc.com and tilia-inc.com.<br/><br/>

## Demo

The Samples/Demo folder contains a Unity scene with the Tilia prefab and a simple sandbox UI for experimenting with the SDK. If you do not see a Tilia Demo Scene in the Samples/Demo/Scenes folder, it is likely because you haven't completed Step 2 above. The demo scenes are browser-specific, and will be installed along with your chosen browser support.
