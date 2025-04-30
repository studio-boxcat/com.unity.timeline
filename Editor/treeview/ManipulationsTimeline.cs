using System;
using UnityEditor.Timeline.Actions;
using UnityEngine;

namespace UnityEditor.Timeline
{
    class TimelinePanManipulator : Manipulator
    {
        const float k_MaxPanSpeed = 50.0f;
        bool m_Active;

        protected override bool MouseDown(Event evt, WindowState state)
        {
            if ((evt.button == 2 && evt.modifiers == EventModifiers.None) ||
                (evt.button == 0 && evt.modifiers == EventModifiers.Alt))
            {
                TimelineCursors.SetCursor(TimelineCursors.CursorType.Pan);

                m_Active = true;
                return true;
            }

            return false;
        }

        protected override bool MouseUp(Event evt, WindowState state)
        {
            if (m_Active)
            {
                TimelineCursors.ClearCursor();
                state.editorWindow.Repaint();
            }

            return false;
        }

        protected override bool MouseDrag(Event evt, WindowState state)
        {
            // Note: Do not rely on evt.button here as some 3rd party automation
            //       software does not properly set the button data during drag.

            if (!m_Active)
                return false;

            return Pan(evt, state);
        }

        protected override bool MouseWheel(Event evt, WindowState state)
        {
            if (Math.Abs(evt.delta.x) < 1e-5 || Math.Abs(evt.delta.x) <= Math.Abs(evt.delta.y))
                return false;

            TimelineZoomManipulator.InvalidateWheelZoom();

            var panEvent = new Event(evt);
            panEvent.delta = new Vector2(panEvent.delta.x * k_MaxPanSpeed * -1.0f, 0.0f);

            return Pan(panEvent, state);
        }

        static bool Pan(Event evt, WindowState state)
        {
            var cursorRect = TimelineWindow.instance.sequenceContentRect;
            cursorRect.xMax = TimelineWindow.instance.position.xMax;
            cursorRect.yMax = TimelineWindow.instance.position.yMax;

            if (state.GetWindow() != null && state.GetWindow().treeView != null)
            {
                var scroll = state.GetWindow().treeView.scrollPosition;
                scroll.y -= evt.delta.y;
                state.GetWindow().treeView.scrollPosition = scroll;
                state.OffsetTimeArea((int)evt.delta.x);
                return true;
            }

            return false;
        }
    }


    class TimelineZoomManipulator : Manipulator
    {
        float m_FocalTime;
        float m_LastMouseMoveX = -1;
        bool m_WheelUsedLast;

        TimelineZoomManipulator() { }

        public static readonly TimelineZoomManipulator Instance = new TimelineZoomManipulator();

        internal void DoZoom(float zoomFactor)
        {
            var refRange = TimelineEditor.visibleTimeRange;
            DoZoom(zoomFactor, refRange, (refRange.x + refRange.y) / 2);
            // Force resetting the reference zoom after a Framing operation
            InvalidateWheelZoom();
        }

        static void DoZoom(float zoomFactor, Vector2 refRange, float focalTime)
        {
            const float kMinRange = 0.05f; // matches zoomable area.

            if (zoomFactor <= 0)
                return;

            var t = Mathf.Max(focalTime, refRange.x);
            var x = (refRange.x + t * (zoomFactor - 1)) / zoomFactor;
            var y = (refRange.y + t * (zoomFactor - 1)) / zoomFactor;

            var newRange = Mathf.Abs(x - y) < kMinRange ? refRange : new Vector2(
                Mathf.Max(x, -WindowConstants.timeAreaShownRangePadding),
                Mathf.Min(y, WindowState.kMaxShownTime));

            if (newRange != refRange)
                // Zoomable area does not protect 100% against crazy values
                TimelineEditor.visibleTimeRange = newRange;
        }

        internal static void InvalidateWheelZoom()
        {
            Instance.m_WheelUsedLast = false;
        }

        protected override bool MouseWheel(Event evt, WindowState state)
        {
            if (Math.Abs(evt.delta.y) < 1e-5)
                return false;

            var zoomRect = TimelineWindow.instance.sequenceContentRect;
            zoomRect.yMax += TimelineWindow.instance.horizontalScrollbarHeight;

            if (!zoomRect.Contains(evt.mousePosition))
                return false;

            if (!m_WheelUsedLast || Mathf.Abs(m_LastMouseMoveX - evt.mousePosition.x) > 1.0f)
            {
                m_LastMouseMoveX = evt.mousePosition.x;
                m_FocalTime = state.PixelToTime(m_LastMouseMoveX);
            }

            var zoomFactor = -evt.delta.y * 0.02f + 1;
            DoZoom(zoomFactor, state.timeAreaShownRange, m_FocalTime);

            m_WheelUsedLast = true;
            return true;
        }
    }

    class TimelineShortcutManipulator : Manipulator
    {
        protected override bool ValidateCommand(Event evt, WindowState state)
        {
            return evt.commandName == EventCommandNames.Copy ||
                evt.commandName == EventCommandNames.Paste ||
                evt.commandName == EventCommandNames.Duplicate ||
                evt.commandName == EventCommandNames.SelectAll ||
                evt.commandName == EventCommandNames.Delete ||
                evt.commandName == EventCommandNames.SoftDelete ||
                evt.commandName == EventCommandNames.FrameSelected;
        }

        protected override bool ExecuteCommand(Event evt, WindowState state)
        {
            if (state.IsCurrentEditingASequencerTextField())
                return false;

            if (evt.commandName == EventCommandNames.SelectAll)
            {
                Invoker.InvokeWithSelected<SelectAllAction>();
                return true;
            }

            if (evt.commandName == EventCommandNames.SoftDelete)
            {
                Invoker.InvokeWithSelected<DeleteAction>();
                return true;
            }

            if (evt.commandName == EventCommandNames.FrameSelected)
            {
                Invoker.InvokeWithSelected<FrameSelectedAction>();
                return true;
            }

            return ActionManager.HandleShortcut(evt);
        }
    }
}
