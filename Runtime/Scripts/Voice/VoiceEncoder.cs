using System;
using System.Collections;
using System.Collections.Generic;
using Concentus.Common;
using Concentus.Structs;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Encodes voice audio samples using Opus.
    /// </summary>
    public class VoiceEncoder : MonoBehaviour
    {
        // Encoding
        private OpusEncoder _opusEncoder;
        private byte[] _encodeBuffer;
        private short[] _emptyShorts;
        private readonly int[] FRAME_SIZES = { 2880, 1920, 960, 480, 240, 120 };
        private int MaxFrameSize => FRAME_SIZES[0];
        private int MinFrameSize => FRAME_SIZES[FRAME_SIZES.Length-1];
        
        public void Init(int sampleRateIn)
        {
            
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
                if (voiceSamplesQueue.Length >= FRAME_SIZES[i])
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

        void LateUpdate()
        {
            // _resampler.Process(0, );
            //
            // // If we're no longer recording and there's still voice data in the queue,
            // // but not enough to trigger an encode, we want append enough silence
            // // to ensure it will get encoded
            // if (!_voiceRecorder.IsRecording && _voiceSamplesQueue.Length > 0 && _voiceSamplesQueue.Length < MinFrameSize)
            // {
            //     _voiceSamplesQueue.Enqueue(new Span<short>(_emptyShorts).Slice(0, MinFrameSize-_voiceSamplesQueue.Length));
            // }
            // // Encode what's currently in the queue
            // if (_voiceSamplesQueue.Length > 0)
            // {
            //     Span<byte> encodedData = EncodeVoiceSamples(_voiceSamplesQueue);
            //     // Send encoded voice to everyone
            //     if (encodedData != null)
            //     {
            //         SendEncodedVoiceServerRpc(encodedData.ToArray()); // TODO: Any way to avoid this allocation?
            //     }
            // }
        }
    }
}
