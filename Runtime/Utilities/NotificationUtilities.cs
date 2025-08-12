using System;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
    static class NotificationUtilities
    {
        public static ScriptPlayable<TimeNotificationBehaviour> CreateNotificationsPlayable(PlayableGraph graph, IEnumerable<IMarker> markers, PlayableDirector director)
        {
            return CreateNotificationsPlayable(graph, markers, null, director);
        }

        public static ScriptPlayable<TimeNotificationBehaviour> CreateNotificationsPlayable(PlayableGraph graph, IEnumerable<IMarker> markers, TimelineAsset timelineAsset)
        {
            return CreateNotificationsPlayable(graph, markers, timelineAsset, null);
        }

        static ScriptPlayable<TimeNotificationBehaviour> CreateNotificationsPlayable(PlayableGraph graph, IEnumerable<IMarker> markers, IPlayableAsset asset, PlayableDirector director)
        {
            ScriptPlayable<TimeNotificationBehaviour> notificationPlayable = ScriptPlayable<TimeNotificationBehaviour>.Null;
            DirectorWrapMode extrapolationMode = director != null ? director.extrapolationMode : DirectorWrapMode.None;
            bool didCalculateDuration = false;
            var duration = 0d;

            foreach (IMarker e in markers)
            {
                var notification = e as INotification;
                if (notification == null)
                    continue;

                if (!didCalculateDuration)
                {
                    duration = director != null ? director.playableAsset.duration : asset.duration;
                    didCalculateDuration = true;
                }

                if (notificationPlayable.Equals(ScriptPlayable<TimeNotificationBehaviour>.Null))
                {
                    notificationPlayable = TimeNotificationBehaviour.Create(graph,
                        duration, extrapolationMode);
                }

                var time = (DiscreteTime)e.time;
                var tlDuration = (DiscreteTime)duration;
                if (time >= tlDuration && time <= tlDuration.OneTickAfter() && tlDuration != 0)
                    time = tlDuration.OneTickBefore();

                if (e is INotificationOptionProvider notificationOptionProvider)
                    notificationPlayable.GetBehaviour().AddNotification((double)time, notification, notificationOptionProvider.flags);
                else
                    notificationPlayable.GetBehaviour().AddNotification((double)time, notification);
            }

            return notificationPlayable;
        }

        static readonly Dictionary<Type, bool> _supportsNotificationsCache = new Dictionary<Type, bool>();

        public static bool TrackTypeSupportsNotifications(Type type)
        {
            if (type == typeof(ActivationTrack) // GameObject
                || type == typeof(AnimationTrack) // Animator
                || type == typeof(AudioTrack) // AudioSource
                || type == typeof(MarkerTrack) // GameObject
                // || type == typeof(SignalTrack) // SignalReceiver
               )
            {
                return true;
            }

            if (type == typeof(ControlTrack) // no binding
                || type == typeof(GroupTrack)) // no binding
            {
                return false;
            }

            L.E($"[Timeline] Unknown track type: {type.FullName}");

            if (_supportsNotificationsCache.TryGetValue(type, out var result))
                return result;

            var binding = (TrackBindingTypeAttribute)Attribute.GetCustomAttribute(type, typeof(TrackBindingTypeAttribute));
            result = binding != null &&
                (typeof(Component).IsAssignableFrom(binding.type) ||
                    typeof(GameObject).IsAssignableFrom(binding.type));

            _supportsNotificationsCache.Add(type, result);
            return result;
        }
    }
}
