# VR-InteractiveMaps

Unity VR project targeting Meta Quest and PICO headsets.

- Unity 6000.6.0f1, Universal Render Pipeline, started from Unity's VR template
- OpenXR with the XR Interaction Toolkit, Android build target (Vulkan, IL2CPP, ARM64, min API 29)
- Meta Quest support comes from Unity's OpenXR and Meta OpenXR packages
- PICO support comes from the PICO OpenXR SDK 1.4.0, embedded in `Packages/com.unity.xr.openxr.picoxr`

## Switching headset target

Quest and PICO use different OpenXR vendor features and controller profiles, which can't be enabled together. Pick the target before building:

- In the Editor: **VR Map > XR Target > Meta Quest** or **PICO**
- From the command line: `-executeMethod VRInteractiveMaps.Editor.XRTargetSwitcher.SwitchToQuest` (or `SwitchToPico`)

The project is committed with Meta Quest selected.

## XR Origin

The scene rig is `Assets/XR/Prefabs/XR Origin (Hands Only).prefab`. It is built from the XR Interaction Toolkit hands demo rig with controllers, gaze, teleport and all locomotion removed, so interaction is hand tracking only (poke and pinch) and the player moves only physically (floor tracking origin, no character controller).

- **VR Map > XR Origin > Rebuild Hands-Only Prefab** regenerates the prefab from the XRI sample.
- **VR Map > XR Origin > Use Hands-Only Rig In Open Scene** swaps the open scene's XR Origin for it.

The rig includes a **palm exit button**: turn your left palm toward your face and a red Exit button appears above it; poke it with your right index finger to quit the app. It's there because the PICO system gesture doesn't reliably leave the app while hand tracking is active. The button is built by the rig builder (`PalmExitButton` in `Assets/XR/Scripts`).

Both XR targets enable OpenXR hand tracking and the hand interaction profile. Quest also enables Meta hand tracking aim; PICO turns on hand tracking in its project setting.

## PICO SDK patch

The PICO OpenXR SDK 1.4.0 doesn't compile on Unity 6.5+ because two Enterprise samples call `Object.GetInstanceID()`, which is now an error. The embedded copy replaces those calls with `CompositionLayerManager.TryGetLayerId` in:

- `Enterprise/Sample/VirtualDisplay/OpenXR/VirtualDisplayDemo_OpenXR.cs`
- `Enterprise/Sample/CameraRendering/OpenXR/EnterpriseAPI_OpenXR.cs`

When PICO ships a fixed release, remove the embedded folder and add the package by git URL instead.
