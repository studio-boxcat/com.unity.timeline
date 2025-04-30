using System.Linq;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.Timeline
{
    class TrackDoubleClick : Manipulator
    {
        protected override bool DoubleClick(Event evt, WindowState state)
        {
            if (evt.button != 0)
                return false;

            var trackGUI = PickerUtils.FirstPickedElementOfType<TimelineTrackBaseGUI>();

            if (trackGUI == null)
                return false;

            // Double-click is only available for AnimationTracks: it conflicts with selection mechanics on other tracks
            if ((trackGUI.track as AnimationTrack) == null)
                return false;

            return EditTrackInAnimationWindow.Do(trackGUI.track);
        }
    }

    class TrackShortcutManipulator : Manipulator
    {
        protected override bool KeyDown(Event evt, WindowState state)
        {
            return InternalExecute(evt, state);
        }

        protected override bool ExecuteCommand(Event evt, WindowState state)
        {
            return InternalExecute(evt, state);
        }

        static bool InternalExecute(Event evt, WindowState state)
        {
            if (state.IsCurrentEditingASequencerTextField())
                return false;

            var tracks = SelectionManager.SelectedTracks().ToList();
            var items = SelectionManager.SelectedClipGUI();

            foreach (var item in items)
            {
                var trackGUI = item.parent as TimelineTrackBaseGUI;
                if (trackGUI == null)
                    continue;

                if (!tracks.Contains(trackGUI.track))
                    tracks.Add(trackGUI.track);
            }

            return ActionManager.HandleShortcut(evt,
                ActionManager.TrackActions,
                x => ActionManager.ExecuteTrackAction(x, tracks));
        }
    }
}
