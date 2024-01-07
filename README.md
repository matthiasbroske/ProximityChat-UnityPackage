# ProximityChat

## About
A proximity voice chat solution for Unity using FMOD and the Opus audio codec, compatible with Netcode for GameObjects.

## Installation
This package relies on FMOD in order to record and play audio. 
For those already familiar with how to setup FMOD in Unity, feel free to skip Step #1.

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
    - If you want to access a particular release or branch, you can append ```#<tag or branch>``` at the end, e.g. ```git@github.com:matthiasbroske/ProximityChat-UnityPackage.git#main```
