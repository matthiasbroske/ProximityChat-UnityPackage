using FMOD;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Plays 16-bit PCM voice audio as a 3D FMOD sound directly through FMOD's Core Engine.
    /// </summary>
    public class CoreVoiceEmitter : VoiceEmitter
    {
        // Sound parameters
        protected ChannelGroup _channelGroup;
        // 3D audio
        protected Vector3 _prevPosition;

        /// <inheritdoc />
        public override void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            base.Init(sampleRate, channelCount, inputFormat);
            // Play the sound through FMOD's core engine, pausing it to start
            RuntimeManager.CoreSystem.playSound(_voiceSound, _channelGroup, true, out _channel);
        }
        
        /// <inheritdoc />
        public override void SetVolume(float volume)
        {
            _channel.setVolume(volume);
        }

        protected override void SetPaused(bool isPaused)
        {
            _channel.setPaused(isPaused);
        }

        protected override void Update()
        {
            if (!_initialized) return;
            base.Update();
            
            // Set the 3D attributes for audio spatialization
            Vector3 position = transform.position;
            Vector3 velocity = (position - _prevPosition) / Time.deltaTime;
            ATTRIBUTES_3D attributes3D = RuntimeUtils.To3DAttributes(transform, velocity);
            _channel.set3DAttributes(ref attributes3D.position, ref attributes3D.velocity);
            _prevPosition = position;
        }
    }
}
