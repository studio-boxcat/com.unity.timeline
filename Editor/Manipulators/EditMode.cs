using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Timeline
{
    static class EditMode
    {
        static MoveItemHandler s_CurrentMoveItemHandler;
        static EditModeInputHandler s_InputHandler = new EditModeInputHandler();
        static TrimItemModeMix s_TrimItemModeMix = new TrimItemModeMix();
        static MoveItemModeMix s_MoveItemModeMix = new MoveItemModeMix();
        static AddDeleteItemModeMix s_AddDeleteItemModeMix = new AddDeleteItemModeMix();

        static ITrimItemMode trimMode => s_TrimItemModeMix;
        static ITrimItemDrawer trimDrawer => s_TrimItemModeMix;
        static IMoveItemMode moveMode => s_MoveItemModeMix;
        static IMoveItemDrawer moveDrawer => s_MoveItemModeMix;
        static IAddDeleteItemMode addDeleteMode => s_AddDeleteItemModeMix;

        public static EditModeInputHandler inputHandler
        {
            get { return s_InputHandler; }
        }

        static Color modeColor => DirectorStyles.kMixToolColor;

        public static void ClearEditMode()
        {
            s_TrimItemModeMix = new TrimItemModeMix();
            s_MoveItemModeMix = new MoveItemModeMix();
            s_AddDeleteItemModeMix = new AddDeleteItemModeMix();
        }

        public static void BeginTrim(ITimelineItem item, TrimEdge trimDirection)
        {
            var itemToTrim = item as ITrimmable;
            if (itemToTrim == null) return;

            trimMode.OnBeforeTrim(itemToTrim, trimDirection);
            UndoExtensions.RegisterTrack(itemToTrim.parentTrack, L10n.Tr("Trim Clip"));
        }

        public static void TrimStart(ITimelineItem item, double time, bool affectTimeScale)
        {
            var itemToTrim = item as ITrimmable;
            if (itemToTrim == null) return;

            trimMode.TrimStart(itemToTrim, time, affectTimeScale);
        }

        public static void TrimEnd(ITimelineItem item, double time, bool affectTimeScale)
        {
            var itemToTrim = item as ITrimmable;
            if (itemToTrim == null) return;

            trimMode.TrimEnd(itemToTrim, time, affectTimeScale);
        }

        public static void DrawTrimGUI(WindowState state, TimelineItemGUI item, TrimEdge edge)
        {
            trimDrawer.DrawGUI(state, item.rect, modeColor, edge);
        }

        public static void FinishTrim()
        {
            TimelineCursors.ClearCursor();
            ClearEditMode();

            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        public static void BeginMove(MoveItemHandler moveItemHandler)
        {
            s_CurrentMoveItemHandler = moveItemHandler;
            moveMode.BeginMove(s_CurrentMoveItemHandler.movingItems);
        }

        public static void UpdateMove()
        {
            moveMode.UpdateMove(s_CurrentMoveItemHandler.movingItems);
        }

        public static void OnTrackDetach(IEnumerable<ItemsPerTrack> grabbedTrackItems)
        {
            moveMode.OnTrackDetach(grabbedTrackItems);
        }

        public static void HandleTrackSwitch(IEnumerable<ItemsPerTrack> grabbedTrackItems)
        {
            moveMode.HandleTrackSwitch(grabbedTrackItems);
        }

        public static bool AllowTrackSwitch()
        {
            return moveMode.AllowTrackSwitch();
        }

        public static double AdjustStartTime(WindowState state, ItemsPerTrack itemsGroup, double time)
        {
            return moveMode.AdjustStartTime(state, itemsGroup, time);
        }

        public static bool ValidateDrag(ItemsPerTrack itemsGroup)
        {
            return moveMode.ValidateMove(itemsGroup);
        }

        public static void DrawMoveGUI(WindowState state, IEnumerable<MovingItems> movingItems)
        {
            moveDrawer.DrawGUI(state, movingItems, modeColor);
        }

        public static void FinishMove()
        {
            var manipulatedItemsList = s_CurrentMoveItemHandler.movingItems;
            moveMode.FinishMove(manipulatedItemsList);

            foreach (var itemsGroup in manipulatedItemsList)
                foreach (var item in itemsGroup.items)
                    item.parentTrack = itemsGroup.targetTrack;

            s_CurrentMoveItemHandler = null;

            TimelineCursors.ClearCursor();
            ClearEditMode();

            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        public static void FinalizeInsertItemsAtTime(IEnumerable<ItemsPerTrack> newItems, double requestedTime)
        {
            addDeleteMode.InsertItemsAtTime(newItems, requestedTime);
        }

        public static void PrepareItemsDelete(IEnumerable<ItemsPerTrack> newItems)
        {
            addDeleteMode.RemoveItems(newItems);
        }
    }
}
