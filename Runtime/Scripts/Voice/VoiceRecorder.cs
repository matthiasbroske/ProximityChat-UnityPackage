using System;
using System.Runtime.InteropServices;
using Concentus.Common;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Records microphone audio as single channel 16-bit PCM through FMOD,
    /// with support for resampling to a specified output sample rate.
    /// </summary>
    public class VoiceRecorder : MonoBehaviour
    {
        // Recording parameters
        private VoiceFormat _outputFormat;
        private int _driverIndex;
        private bool _isRecording;
        private uint _prevRecordPosition;
        private VoiceDataQueue<byte> _recordedBytesQueue;
        private VoiceDataQueue<short> _recordedSamplesQueue;
        // Resampling parameters
        private SpeexResampler _resampler;
        private bool _resampleIsRequired;
        private int _outputSampleRate;
        private short[] _resampleBuffer;
        private int _resampleQuality;
        // Sound parameters
        private Sound _voiceSound;
        private CREATESOUNDEXINFO _soundParams;
        private int _nativeSampleRate;
        private uint _soundByteLength;
        private uint _soundSampleLength;
        // Initialized
        private bool _initialized;

        /// <summary>
        /// Is the recorder currently recording audio.
        /// </summary>
        public bool IsRecording => _isRecording;
        /// <summary>
        /// Queue of recorded voice audio PCM bytes.
        /// </summary>
        public VoiceDataQueue<byte> RecordedBytesQueue => _recordedBytesQueue;
        /// <summary>
        /// Queue of recorded voice audio PCM samples.
        /// </summary>
        public VoiceDataQueue<short> RecordedSamplesQueue => _recordedSamplesQueue;
        
        /// <summary>
        /// Initializes recorder with input driver and output format.
        /// </summary>
        /// <param name="driverIndex">Index of recording driver</param>
        /// <param name="outputFormat">Output voice audio format.
        /// When set to <see cref="VoiceFormat.PCM16Bytes"/> use <see cref="RecordedBytesQueue"/>
        /// to get recorded audio data, and when set to <see cref="VoiceFormat.PCM16Samples"/>
        /// use <see cref="RecordedSamplesQueue"/> instead</param>
        /// <param name="outputSampleRate">The desired sample rate of the output audio.
        /// Forces resampling if the audio driver does not natively record at this sample rate. </param>
        /// <param name="resampleQuality">Quality of resampling from 0 - 10</param>
        public void Init(int driverIndex = 0, VoiceFormat outputFormat = VoiceFormat.PCM16Samples, int outputSampleRate = 48000, int resampleQuality = 10)
        {
            _outputFormat = outputFormat;
            _outputSampleRate = outputSampleRate;
            _resampleQuality = resampleQuality;
            
            // Initialize recording with input device
            SetupRecordingWithDriver(driverIndex);
            
            // Flag initialized
            _initialized = true;
        }

        /// <summary>
        /// Starts audio recording.
        /// </summary>
        public void StartRecording()
        {
            if (!_initialized || _isRecording) return;
            _prevRecordPosition = 0;
            RuntimeManager.CoreSystem.recordStart(_driverIndex, _voiceSound, true);
            _isRecording = true;
        }

        /// <summary>
        /// Stops audio recording.
        /// </summary>
        public void StopRecording(float delay = 0.5f)
        {
            if (!_initialized || !_isRecording) return;
            RuntimeManager.CoreSystem.recordStop(_driverIndex);
            _prevRecordPosition = 0;
            _isRecording = false;
        }

        /// <summary>
        /// Sets the driver used to record audio.
        /// </summary>
        /// <param name="driverIndex">Index of recording driver</param>
        public void SetRecordDriver(int driverIndex)
        {
            // Return if we're already using this driver
            if (!_initialized || driverIndex == _driverIndex) return;
            // Stop recording before switching drivers
            bool wasRecording = _isRecording;
            if (_isRecording) StopRecording(0);
            // Setup recording for input driver
            SetupRecordingWithDriver(driverIndex);
            // Continue recording if we were before
            if (wasRecording) StartRecording();
        }

        private void SetupRecordingWithDriver(int driverIndex)
        {
            // Set driver index
            _driverIndex = driverIndex;
            
            // Get driver info
            RuntimeManager.CoreSystem.getRecordDriverInfo(_driverIndex, out _, 0, out _, out _nativeSampleRate, out _, out _, out _);

            // Initialize sound parameters
            _soundParams.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            _soundParams.numchannels = 1;
            _soundParams.defaultfrequency = _nativeSampleRate;
            _soundParams.format = SOUND_FORMAT.PCM16;
            _soundParams.length = (uint)_nativeSampleRate * VoiceConsts.SampleSize;
            _soundByteLength = _soundParams.length;
            _soundSampleLength = _soundByteLength / VoiceConsts.SampleSize;
            
            // Initialize record buffer based on output format
            if (_outputFormat == VoiceFormat.PCM16Bytes)
                _recordedBytesQueue = new VoiceDataQueue<byte>(_soundByteLength);
            else
                _recordedSamplesQueue = new VoiceDataQueue<short>(_soundSampleLength);
            
            // Create a resampler if input sample rate is different from output sample rate
            _resampleIsRequired = _nativeSampleRate != _outputSampleRate;
            if (_resampleIsRequired)
            {
                _resampleBuffer = new short[_soundSampleLength];
                _resampler = new SpeexResampler(1, _nativeSampleRate, _outputSampleRate, _resampleQuality);
            }
            
            // Create sound in loop mode and open it direct reading/writing
            RuntimeManager.CoreSystem.createSound(_soundParams.userdata, MODE.LOOP_NORMAL | MODE.OPENUSER, ref _soundParams, out _voiceSound);
        }
        
        private uint GetRecordedSampleCount(uint recordStartPosition, uint recordEndPosition)
        {
            return recordEndPosition >= recordStartPosition
                ? recordEndPosition - recordStartPosition
                : _soundSampleLength - recordStartPosition + recordEndPosition;
        }
        
        private uint GetRecordedByteCount(uint recordStartPosition, uint recordEndPosition)
        {
            return recordEndPosition >= recordStartPosition
                ? recordEndPosition - recordStartPosition
                : _soundByteLength - recordStartPosition + recordEndPosition;
        }
        
        void Update()
        {
            if (!_initialized) return;

            if (_isRecording)
            {
                // Get the current record position in PCM samples
                RuntimeManager.CoreSystem.getRecordPosition(_driverIndex, out uint recordPosition);
                
                // Determine the amount recorded since last frame
                uint samplesRecordedSinceLastFrame = GetRecordedSampleCount(_prevRecordPosition, recordPosition);
                // Read that much data from the sound
                if (samplesRecordedSinceLastFrame > 0)
                {
                    if (_outputFormat == VoiceFormat.PCM16Bytes)
                    {
                        _recordedBytesQueue.ResizeIfNeeded((int)(samplesRecordedSinceLastFrame * VoiceConsts.SampleSize));
                        ReadRecordedVoiceBytes(_recordedBytesQueue.Data, _recordedBytesQueue.EnqueuePosition, _prevRecordPosition * VoiceConsts.SampleSize, samplesRecordedSinceLastFrame * VoiceConsts.SampleSize);
                        _recordedBytesQueue.ModifyWritePosition((int)(samplesRecordedSinceLastFrame * VoiceConsts.SampleSize));
                    }
                    else
                    {
                        // If resampling is necessary, copy the recorded sound to a temporary buffer,
                        // resampling, and then add it to the queue
                        if (_resampleIsRequired)
                        {
                            // Read recorded audio into resample buffer
                            ReadRecordedVoiceSamples(_resampleBuffer, 0, _prevRecordPosition, samplesRecordedSinceLastFrame);
                            // Estimate the length of the recorded audio one it has been resampled
                            // so that we can resize the queue
                            int resampledLength = Mathf.CeilToInt((_outputSampleRate / (float)_nativeSampleRate) * samplesRecordedSinceLastFrame);
                            _recordedSamplesQueue.ResizeIfNeeded(resampledLength);
                            // Resample directly into the samples queue
                            int sampledLength = (int)samplesRecordedSinceLastFrame;
                            _resampler.Process(0, _resampleBuffer, 0, ref sampledLength, _recordedSamplesQueue.Data, _recordedSamplesQueue.EnqueuePosition, ref resampledLength);
                            _recordedSamplesQueue.ModifyWritePosition(resampledLength);
                        }
                        // Otherwise read recorded data directly to the queue
                        else
                        {
                            _recordedSamplesQueue.ResizeIfNeeded((int)samplesRecordedSinceLastFrame);
                            ReadRecordedVoiceSamples(_recordedSamplesQueue.Data, _recordedSamplesQueue.EnqueuePosition, _prevRecordPosition, samplesRecordedSinceLastFrame);
                            _recordedSamplesQueue.ModifyWritePosition((int)samplesRecordedSinceLastFrame);
                        }
                    }
                }

                _prevRecordPosition = recordPosition;
            }
        }
        
        /// <summary>
        /// Read voice data bytes directly from the voice sound.
        /// </summary>
        private void ReadRecordedVoiceBytes(byte[] voiceBytesBuffer, int offset, uint readStartPosition, uint byteCount)
        {
            if (byteCount <= 0) return;
            
            _voiceSound.@lock(readStartPosition, byteCount, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            Marshal.Copy(ptr1, voiceBytesBuffer, offset, (int)len1);
            Marshal.Copy(ptr2, voiceBytesBuffer, offset + (int)len1, (int)len2);
            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }
        
        /// <summary>
        /// Read voice data samples directly from the voice sound.
        /// </summary>
        private void ReadRecordedVoiceSamples(short[] voiceSamplesBuffer, int offset, uint readStartPosition, uint sampleCount)
        {
            if (sampleCount <= 0) return;
            
            _voiceSound.@lock(readStartPosition * VoiceConsts.SampleSize, sampleCount * VoiceConsts.SampleSize, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            Marshal.Copy(ptr1, voiceSamplesBuffer, offset, (int)(len1 / VoiceConsts.SampleSize));
            Marshal.Copy(ptr2, voiceSamplesBuffer, offset + (int)(len1 / VoiceConsts.SampleSize), (int)(len2 / VoiceConsts.SampleSize));
            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }
    }
}
