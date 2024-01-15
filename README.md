# ProximityChat

## About
A proximity voice chat solution for Unity, compatible with Netcode for GameObjects.
- Uses [FMOD](https://www.fmod.com/) for audio recording and playback
- Uses [Opus](https://opus-codec.org/) for audio encoding and decoding via [Concentus](https://github.com/lostromb/concentus)

## Installation
This package supports voice playback through FMOD Studio as a programmer instrument event, or directly through the core FMOD Engine API instead.
Playback through FMOD Studio is highly recommended, as it enables audio auditioning and the ability to efficiently design and alter voice audio effects. 

If you do not plan to use FMOD Studio for voice playback, skip to [Step #2](#step-2-install-fmod-for-unity), and for those already familiar with how to setup FMOD in Unity, skip straight to [Step #3](#step-3-install-proximitychat).

### Step #1: Install FMOD Studio
1. Download and install [FMOD Studio](https://www.fmod.com/download#fmodstudio)
2. Run FMOD Studio and create a ```New Project``` (or use an existing one)
3. Save this project anywhere on your machine

### Step #2: Install FMOD for Unity
1. Add the FMOD for Unity package to your assets from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/audio/fmod-for-unity-161631)
2. In Unity, open ```Window -> Package Manager```
3. Select ```My Assets``` in the ```Packages:``` dropdown
4. Locate FMOD for Unity and click ```Import``` to import it into your project
5. Follow the FMOD Setup Wizard's instructions, linking the FMOD Studio project you created if desired

### Step #3: Install ProximityChat
1. In Unity, open ```Window -> Package Manager```. 
2. Click the ```+``` button
3. Select ```Add package from git URL```
4. Paste ```git@github.com:matthiasbroske/ProximityChat-UnityPackage.git``` to install the latest version
    - If you want to access a particular release or branch, you can append ```#<tag or branch>``` at the end,
      e.g. ```git@github.com:matthiasbroske/ProximityChat-UnityPackage.git#main```

## Setup

### Option #1: FMOD Studio Setup
If you would like to play voice audio through FMOD Studio, follow the instructions below.
1. Open the FMOD Studio project you linked to your Unity project
2. In the ```Events``` tab, right click and select ```Event Defaults > 3D Timeline``` to create a spatialized event through which we will play voice audio,
   and name it something like "Voice Chat"

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/fbbc364b-06b2-4366-bacd-f90d739d7ad4)

3. Assign this event to a bank of your chosing by right clicking on it and selecting ```Assign to Bank > Browse``` and choosing a bank

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/433042c1-7ccc-4d89-9005-ae5318ef3787)

4. Right click inside the track named ```Audio 1``` and select ```Add Programmer Instrument```
   
![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/1016551b-4316-464d-b5a1-adb789a9b0db)

5. Resize the instrument such that it is <ins>exactly 1 second long</ins>, starting from 0:00

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/32f06d2d-4f24-44b4-b6b9-2612f71b9181)

6. Right click in the ```Logic Tracks``` region and select ```New Loop Region```, resizing it to be exactly the same size as
   the programmer instrument

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/fc8e487e-be0c-49d0-91c8-bebbbf929829)

7. Click on the programmer instrument, enabling ```Async``` and ```Loop Instrument``` with an infinite ```Play Count```

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/9b337f37-3c74-410f-84c8-c9e855136fc8)

8. This event is where our voice audio will be played, so feel free to add additional audio effects or tweak the spatialization
   settings of the master track here as desired

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/5346672a-586c-4fd5-bf82-3d7a7408d7a1)

9. Save and build the FMOD Studio project before returning to Unity

![image](https://github.com/matthiasbroske/ProximityChat-UnityPackage/assets/82914350/3ab6eb85-80a2-470a-9296-564350c35051)

10. In Unity, locate the ```Voice Networker (Studio)``` Prefab in the ```Runtime/Prefabs``` folder of the ProximityChat package
11. Drag and drop that prefab onto your networked Player Prefab, ideally attached to the camera at eye/ear-level
12. Locate the ```StudioVoiceEmitter``` component of that prefab, and fill in the ```Voice Event Reference``` field by clicking the magnifying glass and
    selecting the "Voice Chat" event you created in FMOD Studio
13. See [How to Use](#how-to-use) for instructions on how to start/stop voice recording

### Option #2: FMOD Engine Setup
If you would like to play voice audio directly, without using FMOD Studio, follow the instructions below.
1. Locate the ```Voice Networker (Core)``` Prefab in the ```Runtime/Prefabs``` folder of the ProximityChat package
2. Drag and drop this prefab onto your networked Player Prefab, ideally attached to the camera at eye/ear-level
3. See [How to Use](#how-to-use) for instructions on how to start/stop recording voice

## How to Use

### Start/Stop Recording
- Start recording and transmitting audio over the network (perhaps when a push-to-talk key is pressed) by calling the ```StartRecording()``` method of the ```VoiceNetworker``` component
- Stop recording by calling the ```StopRecording()``` method of the ```VoiceNetworker``` component

### Debug
To determine whether or not audio is being sent and received over the network without having to boot up multiple instances of
your project, simply toggle on ```Playback Own Voice ☑``` on the ```VoiceNetworker``` component of the Voice Networker prefab you attached
to your player.

## Networking

### Netcode for GameObjects
Follow the above setup and usage instructions to use immediatly in a Unity project with Netcode for GameObjects.

### Other Networking Frameworks
In theory this package is compatible with almost any networking solution for Unity, as all it needs to function is the ability
to send and receive bytes of encoded audio data over the network.
Take a look at [```VoiceNetworker```](Runtime/Scripts/Voice/VoiceNetworker.cs) to get a feel for how this might be achieved with your networking setup.
