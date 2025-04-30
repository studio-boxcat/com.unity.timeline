using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.Timeline
{
    static class TrackResourceCache
    {
        private static Dictionary<System.Type, Color> s_TrackColorCache = new Dictionary<Type, Color>(10);

        public static Color GetTrackColor(TrackAsset track)
        {
            if (track == null)
                return Color.white;

            // Try to ensure DirectorStyles is initialized first
            // Note: GUISkin.current must exist to be able do so
            if (!DirectorStyles.IsInitialized && GUISkin.current != null)
                DirectorStyles.ReloadStylesIfNeeded();

            Color color;
            if (!s_TrackColorCache.TryGetValue(track.GetType(), out color))
            {
                var attr = track.GetType().GetCustomAttributes(typeof(TrackColorAttribute), true);
                if (attr.Length > 0)
                {
                    color = ((TrackColorAttribute)attr[0]).color;
                }
                else
                {
                    // case 1141958
                    // There was an error initializing DirectorStyles
                    if (!DirectorStyles.IsInitialized)
                        return Color.white;

                    color = DirectorStyles.Instance.customSkin.colorDefaultTrackDrawer;
                }

                s_TrackColorCache[track.GetType()] = color;
            }
            return color;
        }

        public static void ClearTrackColorCache()
        {
            s_TrackColorCache.Clear();
        }

        public static void SetTrackColor<T>(Color c) where T : TrackAsset
        {
            s_TrackColorCache[typeof(T)] = c;
        }
    }
}
