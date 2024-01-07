using System;
using Concentus.Enums;
using Concentus.Structs;
using Unity.Netcode;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Networks voice audio, recording, encoding and sending it over the network if owner,
    /// otherwise receiving, decoding and playing it as 3D spatial audio.
    /// </summary>
    [RequireComponent(typeof(FMODVoiceEmitter), typeof(FMODVoiceRecorder))]
    public class VoiceNetworker : NetworkBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _debugVoice;

        // Record/playback
        private FMODVoiceRecorder _voiceRecorder;
        private FMODVoiceEmitter _voiceEmitter;

        // Encoding/decoding
        private VoiceEncoder _voiceEncoder;
        private OpusEncoder _opusEncoder;
        private OpusDecoder _opusDecoder;
        private byte[] _encodeBuffer;
        private short[] _decodeBuffer;
        private short[] _emptyShorts;
        private readonly int[] FRAME_SIZES = { 2880, 1920, 960, 480, 240, 120 };
        private int MaxFrameSize => FRAME_SIZES[0];
        private int MinFrameSize => FRAME_SIZES[FRAME_SIZES.Length-1];

        private const int SAMPLE_RATE = 48000;
        private const int CHANNEL_COUNT = 1;

        void Start()
        {
            _voiceRecorder = GetComponent<FMODVoiceRecorder>();
            _voiceEmitter = GetComponent<FMODVoiceEmitter>();

            // Owner should record voice and encode it
            if (IsOwner)
            {
                // Disable voice emitter
                _voiceEmitter.enabled = _debugVoice;
                // Initialize voice recorder
                _voiceRecorder.Init(0, VoiceFormat.PCM16Samples);
                // Initialize Opus encoder
                _voiceEncoder = new VoiceEncoder(_voiceRecorder.RecordedSamplesQueue);
                _opusEncoder = new OpusEncoder(SAMPLE_RATE, CHANNEL_COUNT, OpusApplication.OPUS_APPLICATION_VOIP);
                _encodeBuffer = new byte[MaxFrameSize * sizeof(short)];
                _emptyShorts = new short[MinFrameSize];
            }
            // Non-owners should receive encoded voice,
            // decode it and play it as audio
            if (!IsOwner || _debugVoice)
            {
                // Disable voice recorder
                _voiceRecorder.enabled = _debugVoice;
                // Initialize voice emitter
                _voiceEmitter.Init(SAMPLE_RATE, CHANNEL_COUNT, VoiceFormat.PCM16Samples);
                // Initialize Opus decoding
                _opusDecoder = new OpusDecoder(SAMPLE_RATE, CHANNEL_COUNT);
                _decodeBuffer = new short[MaxFrameSize];
            }
        }

        [ServerRpc]
        public void SendEncodedVoiceServerRpc(byte[] encodedVoiceData)
        {
            SendEncodedVoiceClientRpc(encodedVoiceData);   
        }

        [ClientRpc]
        public void SendEncodedVoiceClientRpc(byte[] encodedVoiceData)
        {
            if (!IsOwner || _debugVoice)
            {
                Span<short> decodedVoiceSamples = DecodeVoiceSamples(encodedVoiceData);
                _voiceEmitter.EnqueueSamplesForPlayback(decodedVoiceSamples);
            }
        }

        /// <summary>
        /// Starts recording and sending voice data over the network.
        /// </summary>
        public void StartRecording()
        {
            if (!IsOwner) return;
            _voiceRecorder.StartRecording();
        }
        
        /// <summary>
        /// Stops recording and sending voice data over the network.
        /// </summary>
        public void StopRecording()
        {
            if (!IsOwner) return;
            _voiceRecorder.StopRecording();
        }
        
        /// <summary>
        /// Encodes as many queued voice audio samples as possible.
        /// </summary>
        /// <param name="voiceSamplesQueue">Voice audio samples queue</param>
        /// <returns>Span of encoded voice data array</returns>
        private Span<byte> EncodeVoiceSamples(VoiceDataQueue<short> voiceSamplesQueue)
        {
            // Find the largest frame size we can use to encode the queued voice samples
            int frameSize = 0;
            for (int i = 0; i < FRAME_SIZES.Length; i++)
            {
                if (voiceSamplesQueue.EnqueuePosition >= FRAME_SIZES[i])
                {
                    frameSize = FRAME_SIZES[i];
                    break;
                }
            }
            // Return early if there's nothing to encode
            if (frameSize == 0) return null;
            
            // Encode samples using the determined frame size
            int encodedSize = _opusEncoder.Encode(voiceSamplesQueue.Data, frameSize, _encodeBuffer, _encodeBuffer.Length);
            voiceSamplesQueue.Dequeue(frameSize);
            return new Span<byte>(_encodeBuffer, 0, encodedSize);
        }
        
        /// <summary>
        /// Decodes encoded voice audio to decompressed PCM samples.
        /// </summary>
        /// <param name="encodedVoiceData">Encoded voice data returned from <see cref="EncodeVoiceSamples"/>/></param>
        /// <returns>Span of array to which audio was encoded</returns>
        private Span<short> DecodeVoiceSamples(Span<byte> encodedVoiceData)
        {
            // Get the frame size from the encoded audio data
            int frameSize = OpusPacketInfo.GetNumSamples(encodedVoiceData, 0, encodedVoiceData.Length, _opusDecoder.SampleRate);
            // Decode the audio data to voice samples
            int decodedSize = _opusDecoder.Decode(encodedVoiceData, _decodeBuffer, frameSize);
            return new Span<short> (_decodeBuffer, 0, decodedSize);
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (Input.GetKey(KeyCode.Space))
                    StartRecording();
                else
                    StopRecording();
            }
        }

        void LateUpdate()
        {
            if (IsOwner)
            {
                
                // _voiceEmitter.EnqueueSamplesForPlayback( new Span<short>(_voiceRecorder.RecordedSamplesQueue.Data, 0, _voiceRecorder.RecordedSamplesQueue.EnqueuePosition));
                // _voiceRecorder.RecordedSamplesQueue.Dequeue(_voiceRecorder.RecordedSamplesQueue.EnqueuePosition);
                //
                if (_voiceEncoder.TryGetEncodedVoice(out Span<byte> encodedVoice, false))
                {
                    SendEncodedVoiceServerRpc(encodedVoice.ToArray());
                }
                // Span<byte> old = _voiceEncoder.OldWay();
                // if (old != null)
                // {
                //     SendEncodedVoiceServerRpc(old.ToArray());
                // }
                
                // If we're no longer recording and there's still voice data in the queue,
                // but not enough to trigger an encode, we want append enough silence
                // to ensure it will get encoded
                // if (!_voiceRecorder.IsRecording && _voiceRecorder.RecordedSamplesQueue.EnqueuePosition > 0 && _voiceRecorder.RecordedSamplesQueue.EnqueuePosition < MinFrameSize)
                // {
                //     _voiceRecorder.RecordedSamplesQueue.Enqueue(new Span<short>(_emptyShorts).Slice(0, MinFrameSize-_voiceRecorder.RecordedSamplesQueue.EnqueuePosition));
                // }
                // // Encode what's currently in the queue
                // if (_voiceRecorder.RecordedSamplesQueue.EnqueuePosition > 0)
                // {
                //     Span<byte> encodedData = EncodeVoiceSamples(_voiceRecorder.RecordedSamplesQueue);
                //     // Send encoded voice to everyone
                //     if (encodedData != null)
                //     {
                //         SendEncodedVoiceServerRpc(encodedData.ToArray()); // TODO: Any way to avoid this allocation?
                //     }
                // }
            }
        }
    }
}
