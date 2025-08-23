using UnityEditor.Timeline;

/// <summary>
/// Store the editor preferences for Timeline.
/// </summary>
public static class TimelinePreferences
{
    /// <summary>
    /// The time unit used by the Timeline Editor when displaying time values.
    /// </summary>
    public const TimeFormat timeFormat = TimeFormat.Frames;

    /// <summary>
    /// Draw the waveforms for all audio clips.
    /// </summary>
    public const bool showAudioWaveform = true;

    /// <summary>
    /// Allow the users to hear audio while scrubbing on audio clip.
    /// Enables audio scrubbing when moving the playhead.
    /// </summary>
    public const bool audioScrubbing = false;

    /// <summary>
    /// Enable Snap to Frame to manipulate clips and align them on frames.
    /// </summary>
    public const bool snapToFrame = true;

    /// <summary>
    /// Enable Timelines to be evaluated on frame during editor preview.
    /// </summary>
    public const bool playbackLockedToFrame = false;


    /// <summary>
    /// Enable the ability to snap clips on the edge of another clip.
    /// </summary>
    public const bool edgeSnap = true;
    /// <summary>
    /// Behavior of the timeline window during playback.
    /// </summary>
    public const PlaybackScrollMode playbackScrollMode = PlaybackScrollMode.Pan;
}

