using System;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProximityChat
{
    public class StudioVoiceEmitter : VoiceEmitter
    {
        [Header("FMOD Programmer Instrument Event Reference")]
        [SerializeField] private EventReference _voiceEventReference;
        private EVENT_CALLBACK _voiceCallback;
        private EventInstance _voiceEventInstance;
        // 3D audio
        private Vector3 _prevPosition;

       /// <summary>
       /// Initialize with sample rate and channel count of incoming voice data.
       /// </summary>
       /// <param name="sampleRate">Audio data sample rate</param>
       /// <param name="channelCount">Audio data channel count</param>
       /// <param name="inputFormat">Input voice audio format.
       /// When set to <see cref="VoiceFormat.PCM16Bytes"/> use <see cref="EnqueueBytesForPlayback"/>
       /// to play audio data, and when set to <see cref="VoiceFormat.PCM16Samples"/>
       /// use <see cref="EnqueueSamplesForPlayback"/> instead</param>
        public override void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            base.Init(sampleRate, channelCount, inputFormat);
            // Wireup voice callback
            _voiceCallback = new EVENT_CALLBACK(VoiceEventCallback);
            // Create and initialize an instance of our FMOD voice event
            _voiceEventInstance = RuntimeManager.CreateInstance(_voiceEventReference);
            RuntimeManager.AttachInstanceToGameObject(_voiceEventInstance, transform, true);
            _voiceEventInstance.setCallback(_voiceCallback);
            _voiceEventInstance.start();
            _voiceEventInstance.setPaused(true);
        }
        
        /// <summary>
        /// Sets the emitted voice volume.
        /// </summary>
        /// <param name="volume">Volume from 0 to 1</param>
        public override void SetVolume(float volume)
        {
            _voiceEventInstance.setVolume(volume);
        }

        protected override uint GetPlaybackPositionBytes()
        {
            // _voiceEventInstance.getTimelinePosition(out int positionMs);
            // uint positionSamples = (uint)positionMs * _sampleRate / 1000;
            // uint positionBytes = positionSamples * (uint)_channelCount * VoiceConsts.SampleSize;
            // return positionBytes;
            _voiceEventInstance.getChannelGroup(out ChannelGroup group);
            group.getChannel(0, out Channel channel);
            channel.getPosition(out uint playbackPosition, TIMEUNIT.PCMBYTES);
            return playbackPosition;
        }

        protected override void SetPlaybackPositionBytes(uint playbackPosition)
        {
            // int positionSamples = (int)playbackPosition / (_channelCount * (int)VoiceConsts.SampleSize);
            // int positionMs = positionSamples / (int)_sampleRate * 1000;
            // _voiceEventInstance.setTimelinePosition(positionMs);
            _voiceEventInstance.getChannelGroup(out ChannelGroup group);
            group.getChannel(0, out Channel channel);
            channel.setPosition(playbackPosition, TIMEUNIT.PCMBYTES);
        }

        protected override void SetPaused(bool isPaused)
        {
            _voiceEventInstance.setPaused(isPaused);
        }

        protected override void Update()
        {
            base.Update();
        }

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        RESULT VoiceEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            Debug.Log(type);
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
