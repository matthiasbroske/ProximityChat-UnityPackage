using System;
using Concentus.Structs;

namespace ProximityChat
{
    public class VoiceDecoder
    {
        // Decoding
        private OpusDecoder _opusDecoder;
        private short[] _decodeBuffer;

        public VoiceDecoder()
        {
            _opusDecoder = new OpusDecoder(VoiceConsts.OpusSampleRate, 1);
            _decodeBuffer = new short[2880];
        }
        
        /// <summary>
        /// Decodes encoded voice audio to decompressed PCM samples.
        /// </summary>
        /// <param name="encodedVoiceData">Encoded voice data returned from <see cref="VoiceEncoder.GetEncodedVoice"/></param>
        /// <returns>Span of array to which audio was decoded</returns>
        public Span<short> DecodeVoiceSamples(Span<byte> encodedVoiceData)
        {
            // Get the frame size from the encoded audio data
            int frameSize = OpusPacketInfo.GetNumSamples(encodedVoiceData, 0, encodedVoiceData.Length, _opusDecoder.SampleRate);
            // Decode the audio data to voice samples
            int decodedSize = _opusDecoder.Decode(encodedVoiceData, _decodeBuffer, frameSize);
            return _decodeBuffer.AsSpan(0, decodedSize);
        }
    }
}
