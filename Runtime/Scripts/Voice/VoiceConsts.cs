namespace ProximityChat
{
    /// <summary>
    /// Voice/audio related constants.
    /// </summary>
    public static class VoiceConsts
    {
        /// <summary>
        /// Size in bytes of one 16bit PCM sample.
        /// </summary>
        public const uint SampleSize = sizeof(short);
        /// <summary>
        /// Preferred sample rate for all audio going in and out of the Opus codec.
        /// </summary>
        public const int OpusSampleRate = 48000;
    }
}
