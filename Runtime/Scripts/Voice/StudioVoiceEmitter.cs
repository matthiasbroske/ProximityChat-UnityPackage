using System;
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
        [SerializeField] private EventReference _voiceEventReference;
        // Programmer instrument event
        private EVENT_CALLBACK _voiceCallback;
        private EventInstance _voiceEventInstance;

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
            // Attach it to this to get spatial audio
            RuntimeManager.AttachInstanceToGameObject(_voiceEventInstance, transform, true);
        }
        
        /// <inheritdoc />
        public override void SetVolume(float volume)
        {
            _voiceEventInstance.setVolume(volume);
        }

        protected override uint GetPlaybackPositionBytes()
        {
            _voiceEventInstance.getTimelinePosition(out int positionMs);
            uint positionSamples = (uint)positionMs * _sampleRate / 1000;
            uint positionBytes = positionSamples * (uint)_channelCount * VoiceConsts.SampleSize;
            return positionBytes;
        }

        protected override void SetPlaybackPositionBytes(uint playbackPosition)
        {
            int positionSamples = (int)playbackPosition / (_channelCount * (int)VoiceConsts.SampleSize);
            int positionMs = (int)((positionSamples * 1000f) / _sampleRate);
            _voiceEventInstance.setTimelinePosition(positionMs);
        }

        protected override void SetPaused(bool isPaused)
        {
            _voiceEventInstance.setPaused(isPaused);
        }

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        RESULT VoiceEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            switch (type)
            {
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
    }
}
