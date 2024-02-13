using System;
using System.Collections;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Plays 16-bit PCM voice audio through a user-defined Programmer Instrument Event in FMOD Studio.
    /// </summary>
    public class StudioVoiceEmitter : VoiceEmitter
    {
        [Header("FMOD Programmer Instrument Event Reference")]
        [SerializeField] protected EventReference _voiceEventReference;
        // Programmer instrument event
        protected EVENT_CALLBACK _voiceCallback;
        protected EventInstance _voiceEventInstance;

        /// <inheritdoc />
        public override void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            base.Init(sampleRate, channelCount, inputFormat);
            // Wireup programmer instrument callback
            _voiceCallback = new EVENT_CALLBACK(VoiceEventCallback);
            // Create and initialize an instance of our FMOD voice event
            _voiceEventInstance = RuntimeManager.CreateInstance(_voiceEventReference);
            _voiceEventInstance.setCallback(_voiceCallback);
            _voiceEventInstance.start();
            _voiceEventInstance.setPaused(true);
            // We're not going to be officially initialized until our event instance
            // is created, which takes a little while, so let's re-flag ourself as uninitialized
            _initialized = false;
            StartCoroutine(WaitToGetChannel());
            // Attach it to this to get spatial audio
            RuntimeManager.AttachInstanceToGameObject(_voiceEventInstance, transform, true);
        }
        
        /// <inheritdoc />
        public override void SetVolume(float volume)
        {
            _voiceEventInstance.setVolume(volume);
        }

        protected override void SetPaused(bool isPaused)
        {
            _voiceEventInstance.setPaused(isPaused);
        }

        private IEnumerator WaitToGetChannel()
        {
            // Wait until event is fully created (playback state == playing)
            while (true)
            {
                yield return null;
                _voiceEventInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
                if (playbackState == PLAYBACK_STATE.PLAYING)
                    break;
            }
            
            // Get the channel and initialize
            if (FMODUtilities.TryGetChannelForEvent(_voiceEventInstance, out Channel channel))
            {
                _channel = channel;
                _initialized = true;
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to find channel. Unable to initialize Studio voice emitter.");
            }
        }

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        static RESULT VoiceEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            switch (type)
            {
                // Pass the sound to the programmer instrument
                case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                {
                    var parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
                    parameter.sound = _voiceSound.handle;
                    parameter.subsoundIndex = -1;
                    Marshal.StructureToPtr(parameter, parameterPtr, false);
                    break;
                }
            }
            return RESULT.OK;
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                _voiceSound.release();
                _voiceEventInstance.release();
            }
        }
    }
}
