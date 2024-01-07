using System;
using System.Collections;
using System.Collections.Generic;
using Concentus.Common;
using Concentus.Enums;
using Concentus.Structs;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Encodes voice audio samples using Opus.
    /// </summary>
    public class VoiceEncoder
    {
        // Samples queue
        private VoiceDataQueue<short> _voiceSamplesQueue;
        // Encoding
        private OpusEncoder _opusEncoder;
        private byte[] _encodeBuffer;
        private short[] _emptyShorts;
        private readonly int[] FRAME_SIZES = { 2880, 1920, 960, 480};//, 240, 120 };  // Frame sizes (in sample counts) supported by Opus   
        private int MaxFrameSize => FRAME_SIZES[0];
        private int MinFrameSize => FRAME_SIZES[FRAME_SIZES.Length-1];
        private const int SAMPLE_RATE = 48000; // Opus is built for encoding audio data with a sample rate of 48khz
        private const int SAMPLE_SIZE = sizeof(short);
        
        /// <summary>
        /// Initialize the encoder with a queue of voice samples. The encoder
        /// expects the queue to be filled with samples externally, and will
        /// try to encode anything in the queue every frame.
        /// </summary>
        /// <param name="voiceSamplesQueue">Queue of voice samples. Filled externally, likely
        /// by a <see cref="FMODVoiceRecorder"/></param>
        public VoiceEncoder(VoiceDataQueue<short> voiceSamplesQueue)
        {
            _voiceSamplesQueue = voiceSamplesQueue;
            // Initialize the encoder
            _opusEncoder = new OpusEncoder(SAMPLE_RATE, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            _encodeBuffer = new byte[MaxFrameSize * SAMPLE_SIZE];
            _emptyShorts = new short[MinFrameSize];
        }
        
        /// <summary>
        /// Encodes as many queued voice audio samples as possible.
        /// </summary>
        /// <param name="encodedVoice">Encoded voice as a span of bytes</param>
        /// <param name="forceEncodeWithSilence">Whether or not to force encoding with silence.
        /// When enabled, enough silence will be added to the queued voice audio to meet
        /// the minimum frame size and trigger an encode, ensuring that all audio data
        /// is thereby flushed from the queue.</param>
        /// <returns>Whether or not voice data was encoded.</returns>
        public bool TryGetEncodedVoice(out Span<byte> encodedVoice, bool forceEncodeWithSilence = false)
        {
            encodedVoice = null;
            bool encodeSuccessful = false;
            int totalEncodeSize = 0;
            
            // Keep encoding frames of audio until there is not enough audio
            // left to encode a full frame
            int j = 0;
            do
            {
                // Find the largest frame size we can use to encode the queued voice samples
                int frameSize = 0;
                for (int i = 0; i < FRAME_SIZES.Length; i++)
                {
                    if (_voiceSamplesQueue.EnqueuePosition >= FRAME_SIZES[i])
                    {
                        frameSize = FRAME_SIZES[i];
                        break;
                    }
                }
                // Return early if there's nothing to encode
                if (frameSize == 0) break;
            
                // Encode this frame
                totalEncodeSize += Encode(frameSize, totalEncodeSize);
                if (encodeSuccessful == true) Debug.Log("did more than once");
                encodeSuccessful = true;
                j++;
            } while (_voiceSamplesQueue.EnqueuePosition >= MinFrameSize);
            Debug.Log(_voiceSamplesQueue.EnqueuePosition);
            // If forceEncodeWithSilence is enabled and there's still voice data in the queue,
            // but not enough to trigger an encode, append enough silence so it still gets encoded
            if (forceEncodeWithSilence && _voiceSamplesQueue.EnqueuePosition > 0 && _voiceSamplesQueue.EnqueuePosition < MinFrameSize)
            {
                _voiceSamplesQueue.Enqueue(new Span<short>(_emptyShorts).Slice(0, MinFrameSize-_voiceSamplesQueue.EnqueuePosition));
                totalEncodeSize += Encode(MinFrameSize, totalEncodeSize);
                encodeSuccessful = true;
            }
            
            // Output the encoded voice
            if (encodeSuccessful)
                encodedVoice = new Span<byte>(_encodeBuffer, 0, totalEncodeSize);
            return encodeSuccessful;
        }

        private int Encode(int frameSize, int encodeWritePosition)
        {
            // Resize encode buffer if there's a chance it isn't big enough to store encoded data
            if (_encodeBuffer.Length < encodeWritePosition + frameSize * SAMPLE_SIZE)
                Array.Resize(ref _encodeBuffer, Mathf.Max(_encodeBuffer.Length * 2, encodeWritePosition + frameSize * SAMPLE_SIZE));
            // Encode samples using the determined frame size
            int remainingBufferLength = _encodeBuffer.Length - encodeWritePosition;
            int encodedSize = _opusEncoder.Encode(_voiceSamplesQueue.Data, frameSize, new Span<byte>(_encodeBuffer, encodeWritePosition, remainingBufferLength), remainingBufferLength);
            _voiceSamplesQueue.Dequeue(frameSize);
            return encodedSize;
        }
        
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
            int encodedSize = Encode(frameSize, 0);
            // int encodedSize = _opusEncoder.Encode(voiceSamplesQueue.Data, frameSize, _encodeBuffer, _encodeBuffer.Length);
            // voiceSamplesQueue.Dequeue(frameSize);
            return new Span<byte>(_encodeBuffer, 0, encodedSize);
        }

        public Span<byte> OldWay()
        {
            return EncodeVoiceSamples(_voiceSamplesQueue);
        }

        // void LateUpdate()
        // {
        //     // If we're no longer recording and there's still voice data in the queue,
        //     // but not enough to trigger an encode, we want append enough silence
        //     // to ensure it will still get encoded
        //     if (!_voiceRecorder.IsRecording && _voiceSamplesQueue.EnqueuePosition > 0 && _voiceSamplesQueue.EnqueuePosition < MinFrameSize)
        //     {
        //         _voiceSamplesQueue.Enqueue(new Span<short>(_emptyShorts).Slice(0, MinFrameSize-_voiceSamplesQueue.Length));
        //     }
        //     // Encode what's currently in the queue
        //     if (_voiceSamplesQueue.EnqueuePosition > 0)
        //     {
        //         Span<byte> encodedData = EncodeVoiceSamples(_voiceSamplesQueue);
        //         // Send encoded voice to everyone
        //         if (encodedData != null)
        //         {
        //             SendEncodedVoiceServerRpc(encodedData.ToArray()); // TODO: Any way to avoid this allocation?
        //         }
        //     }
        // }
    }
}
