using FMOD;
using FMOD.Studio;

namespace ProximityChat
{
    /// <summary>
    /// FMOD utility and helper methods.
    /// </summary>
    public static class FMODUtilities
    {
        /// <summary>
        /// Gets the first channel it can find that the event is playing through.
        /// </summary>
        /// <param name="eventInstance">Event instance</param>
        /// <param name="channel">Out channel</param>
        /// <returns>Whether or not a channel was found</returns>
        public static bool TryGetChannelForEvent(EventInstance eventInstance, out Channel channel)
        {
            eventInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (playbackState != PLAYBACK_STATE.PLAYING)
                UnityEngine.Debug.LogError("Failed to get event channel." +
                                           "Wait for event to be fully created before calling this method");
            
            eventInstance.getChannelGroup(out ChannelGroup rootGroup);
            return TryGetChannelFromGroup(rootGroup, out channel);
        }
        
        /// <summary>
        /// Gets the first channel it can find inside of a channel group.
        /// </summary>
        /// <param name="rootGroup">Root channel group</param>
        /// <param name="channel">Out channel</param>
        /// <returns>Whether or not a channel was found</returns>
        public static bool TryGetChannelFromGroup(ChannelGroup rootGroup, out Channel channel)
        {
            channel = new Channel();
            
            // Return the first possible channel if there are
            // any for this group
            rootGroup.getNumChannels(out int numChannels);
            if (numChannels > 0)
            {
                rootGroup.getChannel(0, out channel);
                return true;
            }
            
            // No channels, but many sub groups, so let's
            // look for channels in those
            rootGroup.getNumGroups(out int numGroups);
            for (int i = 0; i < numGroups; i++)
            {
                rootGroup.getGroup(i, out ChannelGroup subGroup);
                if (TryGetChannelFromGroup(subGroup, out channel))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
