using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Plays 16-bit PCM voice audio as a 3D FMOD sound.
    /// </summary>
    public class CoreVoiceEmitter : VoiceEmitter
    {
        // Sound parameters
        private Channel _channel;
        private ChannelGroup _channelGroup;
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
            _voiceSound.set3DMinMaxDistance(1, 10000);
            // Play the sound, paused
            RuntimeManager.CoreSystem.playSound(_voiceSound, _channelGroup, true, out _channel);
        }
        
        /// <summary>
        /// Sets the emitted voice volume.
        /// </summary>
        /// <param name="volume">Volume from 0 to 1</param>
        public override void SetVolume(float volume)
        {
            _channel.setVolume(volume);
        }

        protected override uint GetPlaybackPositionBytes()
        {
            _channel.getPosition(out uint playbackPosition, TIMEUNIT.PCMBYTES);
            return playbackPosition;
        }

        protected override void SetPlaybackPositionBytes(uint playbackPosition)
        {
            _channel.setPosition(playbackPosition, TIMEUNIT.PCMBYTES);
        }

        protected override void SetPaused(bool isPaused)
        {
            _channel.setPaused(isPaused);
        }

        protected override void Update()
        {
            base.Update();
            
            // Set the 3D attributes
            Vector3 position = transform.position;
            Vector3 velocity = (position - _prevPosition) / Time.deltaTime;
            ATTRIBUTES_3D attributes3D = RuntimeUtils.To3DAttributes(transform, velocity);
            _channel.set3DAttributes(ref attributes3D.position, ref attributes3D.velocity);
            _prevPosition = position;
        }
    }
}
