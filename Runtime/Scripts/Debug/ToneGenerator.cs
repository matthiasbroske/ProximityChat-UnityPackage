using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// Static class for generating tones to debug audio issues.
    /// </summary>
    public static class ToneGenerator
    {
        /// <summary>
        /// Generates a sine wave tone at the specified hertz
        /// with a length given in seconds.
        /// </summary>
        /// <param name="sampleRate">Tone sample rate</param>
        /// <param name="hertz">Tone frequency</param>
        /// <param name="length">Length in seconds of output</param>
        /// <returns>Array of 16bit sine wave audio samples</returns>
        public static short[] GenerateToneSamples(uint sampleRate, uint hertz, float length = 1)
        {
            short[] toneSamples = new short[(int)(sampleRate * length)];
            for (int i = 0; i < toneSamples.Length; i++)
            {
                float x = (float)i / sampleRate;
                short sample = (short)((Mathf.Sin(hertz*x*Mathf.PI*2)+1)/2 * short.MaxValue);
                toneSamples[i] = sample;
            }
            return toneSamples;
        }
    }
}
