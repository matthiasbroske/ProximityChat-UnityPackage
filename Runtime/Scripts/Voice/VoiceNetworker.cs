using System;
using Unity.Netcode;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Networks voice audio, recording, encoding and sending it over the network if owner,
    /// otherwise receiving, decoding and playing it back as 3D spatial audio.
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
        private VoiceDecoder _voiceDecoder;

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
                _voiceRecorder.Init();
                // Initialize voice encoder
                _voiceEncoder = new VoiceEncoder(_voiceRecorder.RecordedSamplesQueue);
            }
            // Non-owners should receive encoded voice,
            // decode it and play it as audio
            if (!IsOwner || _debugVoice)
            {
                // Disable voice recorder
                _voiceRecorder.enabled = _debugVoice;
                // Initialize voice emitter
                _voiceEmitter.Init(VoiceConsts.OpusSampleRate);
                // Initialize voice decoder
                _voiceDecoder = new VoiceDecoder();
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
                Span<short> decodedVoiceSamples = _voiceDecoder.DecodeVoiceSamples(encodedVoiceData);
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
                // Encode as much queued voice as possible 
                while (_voiceEncoder.HasVoiceLeftToEncode)
                {
                    Span<byte> encodedVoice = _voiceEncoder.GetEncodedVoice();
                    SendEncodedVoiceServerRpc(encodedVoice.ToArray());
                }
                // If we've stopped recording but there's still more left to be cleared,
                // force encode it with silence
                if (!_voiceRecorder.IsRecording && !_voiceEncoder.QueueIsEmpty)
                {
                    Span<byte> encodedVoice = _voiceEncoder.GetEncodedVoice(true);
                    SendEncodedVoiceServerRpc(encodedVoice.ToArray());
                }
            }
        }
    }
}
