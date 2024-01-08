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
    public class FMODVoiceEmitter : MonoBehaviour
    {
        // Sound parameters
        private VoiceFormat _inputFormat;
        private Sound _voiceSound;
        private CREATESOUNDEXINFO _soundParams;
        private uint _sampleRate;
        private int _channelCount;
        private Channel _channel;
        private ChannelGroup _channelGroup;
        // Playback state
        private VoiceDataQueue<byte> _voiceBytesQueue;
        private VoiceDataQueue<short> _voiceSamplesQueue;
        private byte[] _emptyBytes;
        private uint _writePosition;
        private uint _availablePlaybackByteCount;
        private uint _prevPlaybackPosition;
        // 3D audio
        private Vector3 _prevPosition;
        // Initialized
        private bool _initialized = false;

       /// <summary>
       /// Initialize with sample rate and channel count of incoming voice data.
       /// </summary>
       /// <param name="sampleRate">Audio data sample rate</param>
       /// <param name="channelCount">Audio data channel count</param>
       /// <param name="inputFormat">Input voice audio format.
       /// When set to <see cref="VoiceFormat.PCM16Bytes"/> use <see cref="EnqueueBytesForPlayback"/>
       /// to play audio data, and when set to <see cref="VoiceFormat.PCM16Samples"/>
       /// use <see cref="EnqueueSamplesForPlayback"/> instead</param>
        public void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            _sampleRate = sampleRate;
            _channelCount = channelCount;
            _inputFormat = inputFormat;
            
            // Initialize sound parameters
            _soundParams.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            _soundParams.numchannels = _channelCount;
            _soundParams.defaultfrequency = (int)_sampleRate;
            _soundParams.format = SOUND_FORMAT.PCM16;
            _soundParams.length = _sampleRate * VoiceConsts.SampleSize * (uint)_channelCount;
            
            // Initialize voice data buffers
            _emptyBytes = new byte[_soundParams.length];
            if (_inputFormat == VoiceFormat.PCM16Bytes)
            {
                _voiceBytesQueue = new VoiceDataQueue<byte>(_soundParams.length);
            }
            else
            {
                _voiceSamplesQueue = new VoiceDataQueue<short>(_soundParams.length / VoiceConsts.SampleSize);
            }
            
            // Create 3D sound in loop mode and allow direct writing to sound data
            RuntimeManager.CoreSystem.createSound(_soundParams.userdata,MODE.LOOP_NORMAL | MODE.OPENUSER | MODE._3D, ref _soundParams, out _voiceSound);
            _voiceSound.set3DMinMaxDistance(1, 10000);
            // Play the sound, paused
            RuntimeManager.CoreSystem.playSound(_voiceSound, _channelGroup, true, out _channel);
            // Flag initialized
            _initialized = true;
        }
        
       /// <summary>
       /// Enqueues a span of voice audio bytes to be played back.
       /// </summary>
       /// <remarks>
       /// Emitter must be initialized with <see cref="VoiceFormat.PCM16Bytes"/> input format
       /// to use this method.
       /// </remarks>
       /// <param name="voiceBytes">Bytes to be queued for playback</param>
       /// <exception cref="Exception">Throws an exception if input format is not <see cref="VoiceFormat.PCM16Bytes"/></exception>
        public void EnqueueBytesForPlayback(Span<byte> voiceBytes)
        {
            if (_inputFormat != VoiceFormat.PCM16Bytes)
                throw new Exception("Incorrect input format. Failed to enqueue voice bytes.");
            
            _voiceBytesQueue.Enqueue(voiceBytes);
        }
        
        /// <summary>
        /// Enqueues a span of voice audio samples to be played back.
        /// </summary>
        /// <remarks>
        /// Emitter must be initialized with <see cref="VoiceFormat.PCM16Samples"/> input format
        /// to use this method.
        /// </remarks>
        /// <param name="voiceSamples">Samples to be queued for playback</param>
        /// <exception cref="Exception">Throws an exception if input format is not <see cref="VoiceFormat.PCM16Samples"/></exception>
        public void EnqueueSamplesForPlayback(Span<short> voiceSamples)
        {
            if (_inputFormat != VoiceFormat.PCM16Samples)
                throw new Exception("Incorrect input format. Failed to dequeue voice bytes.");
            
            _voiceSamplesQueue.Enqueue(voiceSamples);
        }
        
        /// <summary>
        /// Write voice data bytes directly to the voice sound.
        /// </summary>
        private void WriteVoiceBytes(byte[] voiceBytes, uint writePosition, uint byteCount)
        {
            if (byteCount <= 0) return;
            
            _voiceSound.@lock(writePosition, byteCount, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            Marshal.Copy(voiceBytes, 0, ptr1, (int)len1);
            Marshal.Copy(voiceBytes, (int)len1, ptr2, (int)len2);
            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }
        
        /// <summary>
        /// Write voice data samples directly to the voice sound.
        /// </summary>
        private void WriteVoiceSamples(short[] voiceSamples, uint writePosition, uint sampleCount)
        {
            if (sampleCount <= 0) return;
            
            _voiceSound.@lock(writePosition, sampleCount * VoiceConsts.SampleSize, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            Marshal.Copy(voiceSamples, 0, ptr1, (int)(len1 / VoiceConsts.SampleSize));
            Marshal.Copy(voiceSamples, (int)(len1 / VoiceConsts.SampleSize), ptr2, (int)(len2 / VoiceConsts.SampleSize));
            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }

        private uint GetPlaybackByteCount(uint playbackStartPosition, uint playbackEndPosition)
        {
            return playbackEndPosition >= playbackStartPosition
                ? playbackEndPosition - playbackStartPosition
                : _soundParams.length - playbackStartPosition + playbackEndPosition;
        }
        
        private uint GetAvailablePlaybackByteCount(uint playbackPosition, uint writePosition, bool soundIsFull = false)
        {
            if (writePosition > playbackPosition)
            {
                return writePosition - playbackPosition;
            }
            else if (writePosition < playbackPosition)
            {
                return _soundParams.length - playbackPosition + writePosition;
            }
            else
            {
                return soundIsFull ? _soundParams.length : 0;
            }
        }
        
        private uint GetAvailableWriteByteCount(uint playbackPosition, uint writePosition, bool soundIsFull = false)
        {
            if (writePosition > playbackPosition)
            {
                return _soundParams.length - writePosition + playbackPosition;
            }
            else if (writePosition < playbackPosition)
            {
                return playbackPosition - writePosition;
            }
            else
            {
                return soundIsFull ? 0 : _soundParams.length;
            }
        }
        
        void Update()
        {
            if (!_initialized) return;
            
            // Set the 3D attributes
            Vector3 position = transform.position;
            Vector3 velocity = (position - _prevPosition) / Time.deltaTime;
            ATTRIBUTES_3D attributes3D = RuntimeUtils.To3DAttributes(transform, velocity);
            _channel.set3DAttributes(ref attributes3D.position, ref attributes3D.velocity);
            _prevPosition = position;
            
            // Voice sound will never be full at the beginning of update
            // (some of it was always played during the last frame
            // OR it was already empty)
            bool soundIsFull = false;
            
            // Get the current playback position
            _channel.getPosition(out uint playbackPosition, TIMEUNIT.PCMBYTES);
            
            // Check if we have played all the available voice data since last frame
            uint bytesPlayedSinceLastFrame = GetPlaybackByteCount(_prevPlaybackPosition, playbackPosition);
            if (bytesPlayedSinceLastFrame > _availablePlaybackByteCount)
            {
                playbackPosition = _writePosition;
                _channel.setPosition(playbackPosition, TIMEUNIT.PCMBYTES);
            }
            
            // Write silence to part of sound that was played back last frame
            if (bytesPlayedSinceLastFrame > 0)
            {
                WriteVoiceBytes(_emptyBytes, _writePosition, bytesPlayedSinceLastFrame);
            }
            
            // Write length is the minimum of available writing space and buffered voice data
            uint availableWriteByteCount = GetAvailableWriteByteCount(playbackPosition, _writePosition, soundIsFull);
            uint writeLength = (_inputFormat == VoiceFormat.PCM16Bytes) ?
                (uint)Mathf.Min(_voiceBytesQueue.EnqueuePosition, availableWriteByteCount) :
                (uint)Mathf.Min(_voiceSamplesQueue.EnqueuePosition, availableWriteByteCount / VoiceConsts.SampleSize);
            if (writeLength > 0)
            {
                // Write voice data to sound
                if (_inputFormat == VoiceFormat.PCM16Bytes)
                {
                    WriteVoiceBytes(_voiceBytesQueue.Data, _writePosition, writeLength);
                    _voiceBytesQueue.Dequeue((int)writeLength);
                }
                else
                {
                    WriteVoiceSamples(_voiceSamplesQueue.Data, _writePosition, writeLength);
                    _voiceSamplesQueue.Dequeue((int)writeLength);
                }

                // Update write position
                uint writeLengthBytes = (_inputFormat == VoiceFormat.PCM16Bytes) ? writeLength : writeLength * VoiceConsts.SampleSize;
                _writePosition = (uint)Mathf.Repeat(_writePosition + writeLengthBytes, _soundParams.length);
                soundIsFull = _writePosition == playbackPosition;
            }
            
            // Track the amount of bytes now available for playback
            _availablePlaybackByteCount = GetAvailablePlaybackByteCount(playbackPosition, _writePosition, soundIsFull);
            
            // Pause the channel if there are no bytes left to play
            _channel.setPaused(_availablePlaybackByteCount == 0);
            
            _prevPlaybackPosition = playbackPosition;
        }
    }
}
