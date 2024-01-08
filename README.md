# ProximityChat

## About
A proximity voice chat solution for Unity, compatible with Netcode for GameObjects.
- Uses [FMOD](https://www.fmod.com/) for audio recording and playback
- Uses [Opus](https://opus-codec.org/) for audio encoding and decoding via [Concentus](https://github.com/lostromb/concentus)

## Installation
For those already familiar with how to setup FMOD in Unity, skip straight to [Step #2](#step-2-install-proximitychat).

### Step #1: Install FMOD for Unity
1. Add the FMOD for Unity package to your assets from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/audio/fmod-for-unity-161631)
2. In Unity, open ```Window -> Package Manager```
3. Select ```My Assets``` in the ```Packages:``` dropdown
4. Locate FMOD for Unity and click ```Import``` to bring it into your project
5. Follow the FMOD Setup Wizard's instructions

### Step #2: Install ProximityChat
1. In Unity, open ```Window -> Package Manager```. 
2. Click the ```+``` button
3. Select ```Add package from git URL```
4. Paste ```git@github.com:matthiasbroske/ProximityChat-UnityPackage.git``` to install the latest version
    - If you want to access a particular release or branch, you can append ```#<tag or branch>``` at the end,
      e.g. ```git@github.com:matthiasbroske/ProximityChat-UnityPackage.git#main```

## How To Use

### Netcode for GameObjects
To use in a Unity project with Netcode for GameObjects, simply attach the [```VoiceNetworker```](Runtime/Scripts/Voice/VoiceNetworker.cs)
component to your networked Player Prefab, ideally at ear/eye-level. Then, use ```VoiceNetworker.StartRecording()``` and
```VoiceNetworker.StopRecording()``` to start and stop recording when a key is pressed and released, or when your application starts and exits.

### Other
In theory this package is compatible with almost any networking solution for Unity, as all it need is to be able to
send and receive bytes of encoded audio data across the network.
Take a look at [```VoiceNetworker```](Runtime/Scripts/Voice/VoiceNetworker.cs) to get a feel for how this might be achieved with your networking setup.
