using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.Timeline
{
    [CustomTimelineEditor(typeof(MarkerTrack))]
    class MarkerTrackEditor : TrackEditor
    {
        public const float DefaultMarkerTrackHeight = 18;

        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            options.height = DefaultMarkerTrackHeight;
            return options;
        }
    }
}
