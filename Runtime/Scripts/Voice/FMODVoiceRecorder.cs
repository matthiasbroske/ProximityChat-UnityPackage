using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Records microphone audio as single channel 16-bit PCM.
    /// </summary>
    public class FMODVoiceRecorder : MonoBehaviour
    {
        // Recording parameters
        private VoiceFormat _outputFormat;
        private int _driverIndex;
        private bool _isRecording;
        private uint _prevRecordPosition;
        private byte[] _recordedBytesBuffer;
        private int _recordedBytesCount;
        private short[] _recordedSamplesBuffer;
        private int _recordedSamplesCount;
        // Sound parameters
        private Sound _voiceSound;
        private CREATESOUNDEXINFO _soundParams;
        private int _sampleRate;
        private const int CHANNEL_COUNT = 1;
        private const uint SAMPLE_SIZE = sizeof(short) * CHANNEL_COUNT;
        // Initialized
        private bool _initialized = false;

        /// <summary>
        /// Is the recording currently recording audio.
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// Invoked every time a new chunk of voice audio data has been recorded
        /// and is ready to be read.
        /// </summary>
        /// <remarks>
        /// Subscribe to this event to get notified as to when to call
        /// <see cref="GetVoiceBytes"/> or <see cref="GetVoiceSamples"/>.
        /// </remarks>
        public event Action PingVoiceRecorded;
        
        /// <summary>
        /// Initializes recorder with input driver and output format.
        /// </summary>
        /// <param name="driverIndex">Index of recording driver</param>
        /// <param name="outputFormat">Output voice audio format.
        /// When set to <see cref="VoiceFormat.PCM16Bytes"/> use <see cref="GetVoiceBytes"/>
        /// to get recorded audio data, and when set to <see cref="VoiceFormat.PCM16Samples"/>
        /// use <see cref="GetVoiceSamples"/> instead</param>
        public void Init(int driverIndex = 0, VoiceFormat outputFormat = VoiceFormat.PCM16Bytes)
        {
            _outputFormat = outputFormat;
            
            // Initialize recording with input device
            SetupRecordingWithDriver(driverIndex);
            
            // Flag initialized
            _initialized = true;
        }

        /// <summary>
        /// Gets the most recently recorded voice audio bytes.
        /// Call this on <see cref="PingVoiceRecorded"/> for best results.
        /// </summary>
        /// <remarks>
        /// Recorder must be initialized with <see cref="VoiceFormat.PCM16Bytes"/> output format
        /// to use this method.
        /// </remarks>
        /// <exception cref="Exception">Throws an exception if output format is not <see cref="VoiceFormat.PCM16Bytes"/></exception>
        public Span<byte> GetVoiceBytes()
        {
            if (_outputFormat != VoiceFormat.PCM16Bytes)
                throw new Exception("Incorrect output format. Failed to get voice bytes.");
            
            return new Span<byte>(_recordedBytesBuffer, 0, _recordedBytesCount);
        }

        /// <summary>
        /// Gets the most recently recorded voice audio samples.
        /// Call this on <see cref="PingVoiceRecorded"/> for best results.
        /// </summary>
        /// <remarks>
        /// Recorder must be initialized with <see cref="VoiceFormat.PCM16Samples"/> output format
        /// to use this method.
        /// </remarks>
        /// <exception cref="Exception">Throws an exception if output format is not <see cref="VoiceFormat.PCM16Samples"/></exception>
        public Span<short> GetVoiceSamples()
        {
            if (_outputFormat != VoiceFormat.PCM16Samples)
                throw new Exception("Incorrect output format. Failed to get voice samples.");
            
            return new Span<short>(_recordedSamplesBuffer, 0, _recordedSamplesCount);
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
            RuntimeManager.CoreSystem.getRecordDriverInfo(_driverIndex, out _, 0, out _, out _sampleRate, out _, out _, out _);
            
            // Initialize sound parameters
            _soundParams.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            _soundParams.numchannels = CHANNEL_COUNT;
            _soundParams.defaultfrequency = _sampleRate;
            _soundParams.format = SOUND_FORMAT.PCM16;
            _soundParams.length = (uint)_sampleRate * SAMPLE_SIZE;
            
            // Initialize record buffer based on output format
            if (_outputFormat == VoiceFormat.PCM16Bytes)
                _recordedBytesBuffer = new byte[_soundParams.length];
            else
                _recordedSamplesBuffer = new short[_soundParams.length / sizeof(short)];
            
            // Create sound in loop mode and open it direct reading/writing
            RuntimeManager.CoreSystem.createSound(_soundParams.userdata, MODE.LOOP_NORMAL | MODE.OPENUSER, ref _soundParams, out _voiceSound);
        }
        
        private uint GetAmountRecorded(uint recordStartPosition, uint recordEndPosition)
        {
            return recordEndPosition >= recordStartPosition
                ? recordEndPosition - recordStartPosition
                : _soundParams.length - recordStartPosition + recordEndPosition;
        }
        
        void Update()
        {
            if (!_initialized) return;

            if (_isRecording)
            {
                // Get the current record position in PCM bytes time units
                RuntimeManager.CoreSystem.getRecordPosition(_driverIndex, out uint recordPosition);
                recordPosition *= SAMPLE_SIZE;
                
                // Determine the amount recorded since last frame
                uint amountRecordedSinceLastFrame = GetAmountRecorded(_prevRecordPosition, recordPosition);
                // Read that much data from the sound
                if (amountRecordedSinceLastFrame > 0)
                {
                    _voiceSound.@lock(_prevRecordPosition, amountRecordedSinceLastFrame, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
                    if (_outputFormat == VoiceFormat.PCM16Bytes)
                    {
                        Marshal.Copy(ptr1, _recordedBytesBuffer, 0, (int)len1);
                        Marshal.Copy(ptr2, _recordedBytesBuffer, (int)len1, (int)len2);
                        _recordedBytesCount = (int)amountRecordedSinceLastFrame;
                        PingVoiceRecorded?.Invoke();
                    }
                    else
                    {
                        Marshal.Copy(ptr1, _recordedSamplesBuffer, 0, (int)(len1 / sizeof(short)));
                        Marshal.Copy(ptr2, _recordedSamplesBuffer, (int)(len1 / sizeof(short)), (int)(len2 / sizeof(short)));
                        _recordedSamplesCount = (int)amountRecordedSinceLastFrame / sizeof(short);
                        PingVoiceRecorded?.Invoke();
                    }
                    _voiceSound.unlock(ptr1, ptr2, len1, len2);
                }

                _prevRecordPosition = recordPosition;
            }
        }
    }
}
