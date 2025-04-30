using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace UnityEditor.Timeline
{
    class TimelineTrackGUI : TimelineGroupGUI, IRowGUI
    {
        struct TrackDrawData
        {
            public bool m_AllowsRecording;
            public bool m_ShowTrackBindings;
            public bool m_HasBinding;
            public bool m_IsSubTrack;
            public PlayableBinding m_Binding;
            public Object m_TrackBinding;
            public Texture m_TrackIcon;
        }

        static class Styles
        {
            public static readonly GUIContent kActiveRecordButtonTooltip = DirectorStyles.TrTextContent(string.Empty, "End recording");
            public static readonly GUIContent kInactiveRecordButtonTooltip = DirectorStyles.TrTextContent(string.Empty, "Start recording");
            public static readonly GUIContent kIgnorePreviewRecordButtonTooltip = DirectorStyles.TrTextContent(string.Empty, "Recording is disabled: scene preview is ignored for this TimelineAsset");
            public static readonly GUIContent kDisabledRecordButtonTooltip = DirectorStyles.TrTextContent(string.Empty,
                "Recording is not permitted when Track Offsets are set to Auto. Track Offset settings can be changed in the track menu of the base track.");
            public static Texture2D kProblemIcon = DirectorStyles.GetBackgroundImage(DirectorStyles.Instance.warning);
        }

        static GUIContent s_ArmForRecordContentOn;
        static GUIContent s_ArmForRecordContentOff;
        static GUIContent s_ArmForRecordDisabled;

        readonly InfiniteTrackDrawer m_InfiniteTrackDrawer;
        readonly TrackEditor m_TrackEditor;
        readonly GUIContent m_DefaultTrackIcon;

        TrackItemsDrawer m_ItemsDrawer;
        TrackDrawData m_TrackDrawData;
        TrackDrawOptions m_TrackDrawOptions;

        int m_TrackHash = -1;
        int m_BlendHash = -1;
        int m_LastDirtyIndex = -1;

        public override bool expandable
        {
            get { return hasChildren; }
        }

        static bool DoesTrackAllowsRecording(TrackAsset track)
        {
            // if the root animation track is in auto mode, recording is not allowed
            var animTrack = TimelineUtility.GetSceneReferenceTrack(track) as AnimationTrack;
            if (animTrack != null)
                return animTrack.trackOffset != TrackOffset.Auto;

            return false;
        }

        public bool locked
        {
            get { return track.lockedInHierarchy; }
        }

        public bool showMarkers
        {
            get { return track.GetShowMarkers(); }
        }

        public bool muted
        {
            get { return track.muted; }
        }

        public List<TimelineClipGUI> clips
        {
            get
            {
                return m_ItemsDrawer.clips == null ? new List<TimelineClipGUI>(0) : m_ItemsDrawer.clips;
            }
        }

        TrackAsset IRowGUI.asset { get { return track; } }

        bool showTrackRecordingDisabled
        {
            get
            {
                // if the root animation track is in auto mode, recording is not allowed
                var animTrack = TimelineUtility.GetSceneReferenceTrack(track) as AnimationTrack;
                return animTrack != null && animTrack.trackOffset == TrackOffset.Auto;
            }
        }

        float height => m_TrackDrawOptions.height <= 0.0f ? TrackEditor.DefaultTrackHeight : m_TrackDrawOptions.height;

        public TimelineTrackGUI(TreeViewController tv, TimelineTreeViewGUI w, int id, int depth, TreeViewItem parent, string displayName, TrackAsset sequenceActor)
            : base(tv, w, id, depth, parent, displayName, sequenceActor, false)
        {
            var animationTrack = sequenceActor as AnimationTrack;
            if (animationTrack != null)
                m_InfiniteTrackDrawer = new InfiniteTrackDrawer(new AnimationTrackKeyDataSource(animationTrack));
            else if (sequenceActor.HasAnyAnimatableParameters() && !sequenceActor.clips.Any())
                m_InfiniteTrackDrawer = new InfiniteTrackDrawer(new TrackPropertyCurvesDataSource(sequenceActor));

            var bindings = track.outputs.ToArray();
            m_TrackDrawData.m_HasBinding = bindings.Length > 0;
            if (m_TrackDrawData.m_HasBinding)
                m_TrackDrawData.m_Binding = bindings[0];
            m_TrackDrawData.m_IsSubTrack = IsSubTrack();
            m_TrackDrawData.m_AllowsRecording = DoesTrackAllowsRecording(sequenceActor);
            m_DefaultTrackIcon = TrackResourceCache.GetTrackIcon(track);

            m_TrackEditor = CustomTimelineEditorCache.GetTrackEditor(sequenceActor);
            m_TrackDrawOptions = m_TrackEditor.GetTrackOptions_Safe(track, null);

            m_TrackDrawOptions.errorText = null; // explicitly setting to null for an uninitialized state

            RebuildGUICacheIfNecessary();
        }

        public override float GetVerticalSpacingBetweenTracks()
        {
            if (track != null && track.isSubTrack)
                return 1.0f; // subtracks have less of a gap than tracks
            return base.GetVerticalSpacingBetweenTracks();
        }

        void DetectTrackChanged()
        {
            if (Event.current.type == EventType.Layout)
            {
                // incremented when a track or it's clips changed
                if (m_LastDirtyIndex != track.DirtyIndex)
                {
                    m_TrackEditor.OnTrackChanged_Safe(track);
                    m_LastDirtyIndex = track.DirtyIndex;
                }
                OnTrackChanged();
            }
        }

        // Called when the source track data, including it's clips have changed has changed.
        void OnTrackChanged()
        {
            // recompute blends if necessary
            int newBlendHash = BlendHash();
            if (m_BlendHash != newBlendHash)
            {
                UpdateClipOverlaps();
                m_BlendHash = newBlendHash;
            }

            RebuildGUICacheIfNecessary();
        }

        void UpdateDrawData(WindowState state)
        {
            if (Event.current.type == EventType.Layout)
            {
                m_TrackDrawData.m_ShowTrackBindings = false;
                m_TrackDrawData.m_TrackBinding = null;

                if (state.editSequence.director != null && showSceneReference)
                {
                    m_TrackDrawData.m_ShowTrackBindings = state.GetWindow().currentMode.ShouldShowTrackBindings(state);
                    m_TrackDrawData.m_TrackBinding = state.editSequence.director.GetGenericBinding(track);
                }

                m_TrackDrawOptions = m_TrackEditor.GetTrackOptions_Safe(track, m_TrackDrawData.m_TrackBinding);

                m_TrackDrawData.m_AllowsRecording = DoesTrackAllowsRecording(track);
                m_TrackDrawData.m_TrackIcon = m_TrackDrawOptions.icon;
                if (m_TrackDrawData.m_TrackIcon == null)
                    m_TrackDrawData.m_TrackIcon = m_DefaultTrackIcon.image;
            }
        }

        public override void Draw(Rect headerRect, Rect contentRect, WindowState state)
        {
            DetectTrackChanged();
            UpdateDrawData(state);

            var trackHeaderRect = headerRect;
            var trackContentRect = contentRect;

            if (Event.current.type == EventType.Repaint)
            {
                m_TreeViewRect = trackContentRect;
            }

            track.SetCollapsed(!isExpanded);

            RebuildGUICacheIfNecessary();

            // Prevents from drawing outside of bounds, but does not effect layout or markers
            bool isOwnerDrawSucceed = false;

            Vector2 visibleTime = state.timeAreaShownRange;

            if (drawer != null)
                isOwnerDrawSucceed = drawer.DrawTrack(trackContentRect, track, visibleTime, state);

            if (!isOwnerDrawSucceed)
            {
                using (new GUIViewportScope(trackContentRect))
                    DrawBackground(trackContentRect, track, visibleTime, state);

                if (m_InfiniteTrackDrawer != null)
                    m_InfiniteTrackDrawer.DrawTrack(trackContentRect, track, visibleTime, state);

                // draw after user customization so overlay text shows up
                using (new GUIViewportScope(trackContentRect))
                    m_ItemsDrawer.Draw(trackContentRect, state);
            }

            DrawTrackHeader(trackHeaderRect, state);

            DrawTrackColorKind(headerRect);
            DrawTrackState(contentRect, contentRect, track);
        }

        void DrawErrorIcon(Rect position, WindowState state)
        {
            Rect bindingLabel = position;
            bindingLabel.x = position.xMax + 3;
            bindingLabel.width = state.bindingAreaWidth;
            EditorGUI.LabelField(position, m_ProblemIcon);
        }

        void DrawBackground(Rect trackRect, TrackAsset trackAsset, Vector2 visibleTime, WindowState state)
        {
            bool canDrawRecordBackground = IsRecording(state);
            if (canDrawRecordBackground)
            {
                DrawRecordingTrackBackground(trackRect, trackAsset, visibleTime, state);
            }
            else
            {
                Color trackBackgroundColor;

                if (SelectionManager.Contains(track))
                {
                    trackBackgroundColor = state.IsEditingASubTimeline() ?
                        DirectorStyles.Instance.customSkin.colorTrackSubSequenceBackgroundSelected :
                        DirectorStyles.Instance.customSkin.colorTrackBackgroundSelected;
                }
                else
                {
                    trackBackgroundColor = state.IsEditingASubTimeline() ?
                        DirectorStyles.Instance.customSkin.colorTrackSubSequenceBackground :
                        DirectorStyles.Instance.customSkin.colorTrackBackground;
                }

                EditorGUI.DrawRect(trackRect, trackBackgroundColor);
            }
        }

        public override float GetHeight(WindowState state)
        {
            return GetTrackContentHeight(state);
        }

        float GetTrackContentHeight(WindowState state)
        {
            return height;
        }

        static bool CanDrawIcon(GUIContent icon)
        {
            return icon != null && icon != GUIContent.none && icon.image != null;
        }

        bool showSceneReference
        {
            get
            {
                return track != null &&
                    m_TrackDrawData.m_HasBinding &&
                    !m_TrackDrawData.m_IsSubTrack &&
                    m_TrackDrawData.m_Binding.sourceObject != null &&
                    m_TrackDrawData.m_Binding.outputTargetType != null &&
                    typeof(Object).IsAssignableFrom(m_TrackDrawData.m_Binding.outputTargetType);
            }
        }

        void DrawTrackHeader(Rect trackHeaderRect, WindowState state)
        {
            using (new GUIViewportScope(trackHeaderRect))
            {
                var rect = trackHeaderRect;

                DrawHeaderBackground(trackHeaderRect);
                rect.x += m_Styles.trackSwatchStyle.fixedWidth;

                const float buttonSize = WindowConstants.trackHeaderButtonSize;
                const float padding = WindowConstants.trackHeaderButtonPadding;
                var buttonRect = new Rect(trackHeaderRect.xMax - buttonSize - padding, rect.y + (rect.height - buttonSize) / 2f, buttonSize, buttonSize);

                rect.x += DrawTrackIconKind(rect, state);

                if (track is GroupTrack)
                    return;

                var suiteRect = DrawCustomSuite(state, buttonRect);

                var bindingRect = new Rect(rect.x, rect.y, suiteRect.xMax - rect.x, rect.height);
                DrawTrackBinding(bindingRect, trackHeaderRect);
            }
        }

        Rect DrawCustomSuite(WindowState state, Rect rect)
        {
            var numberOfButtons = 0;
            if (m_TrackDrawData.m_AllowsRecording || showTrackRecordingDisabled)
                numberOfButtons++;
            if (drawer.HasCustomTrackHeaderButton())
                numberOfButtons++;
            if (numberOfButtons == 0)
                return rect;

            var padding = DrawButtonSuite(numberOfButtons, ref rect);

            rect.x -= DrawRecordButton(rect, state);
            rect.x -= DrawCustomTrackButton(rect, state);
            rect.x -= padding;
            return rect;
        }

        void DrawHeaderBackground(Rect headerRect)
        {
            Color backgroundColor = SelectionManager.Contains(track)
                ? DirectorStyles.Instance.customSkin.colorSelection
                : DirectorStyles.Instance.customSkin.colorTrackHeaderBackground;

            var bgRect = headerRect;
            bgRect.x += m_Styles.trackSwatchStyle.fixedWidth;
            bgRect.width -= m_Styles.trackSwatchStyle.fixedWidth;

            EditorGUI.DrawRect(bgRect, backgroundColor);
        }

        void DrawTrackColorKind(Rect rect)
        {
            // subtracks don't draw the color, the parent does that.
            if (track != null && track.isSubTrack)
                return;

            if (rect.width <= 0) return;

            using (new GUIColorOverride(m_TrackDrawOptions.trackColor))
            {
                rect.width = m_Styles.trackSwatchStyle.fixedWidth;
                GUI.Label(rect, GUIContent.none, m_Styles.trackSwatchStyle);
            }
        }

        float DrawTrackIconKind(Rect rect, WindowState state)
        {
            // no icons on subtracks
            if (track != null && track.isSubTrack)
                return 0.0f;

            rect.yMin += (rect.height - 16f) / 2f;
            rect.width = 16.0f;
            rect.height = 16.0f;

            if (!string.IsNullOrEmpty(m_TrackDrawOptions.errorText))
            {
                m_ProblemIcon.image = Styles.kProblemIcon;
                m_ProblemIcon.tooltip = m_TrackDrawOptions.errorText;

                if (CanDrawIcon(m_ProblemIcon))
                    DrawErrorIcon(rect, state);
            }
            else
            {
                var content = GUIContent.Temp(m_TrackDrawData.m_TrackIcon, m_DefaultTrackIcon.tooltip);
                if (CanDrawIcon(content))
                    GUI.Box(rect, content, GUIStyle.none);
            }

            return rect.width;
        }

        void DrawTrackBinding(Rect rect, Rect headerRect)
        {
            if (m_TrackDrawData.m_ShowTrackBindings)
            {
                DoTrackBindingGUI(rect);
                return;
            }

            var textStyle = m_Styles.trackHeaderFont;
            textStyle.normal.textColor = SelectionManager.Contains(track) ? Color.white : m_Styles.customSkin.colorTrackFont;

            string trackName = track.name;

            EditorGUI.BeginChangeCheck();

            // by default the size is just the width of the string (for selection purposes)
            rect.width = m_Styles.trackHeaderFont.CalcSize(new GUIContent(trackName)).x;

            // if we are editing, supply the entire width of the header
            if (GUIUtility.keyboardControl == track.GetInstanceID())
                rect.width = (headerRect.xMax - rect.xMin) - (5 * WindowConstants.trackHeaderButtonSize);

            trackName = EditorGUI.DelayedTextField(rect, GUIContent.none, track.GetInstanceID(), track.name, textStyle);

            if (EditorGUI.EndChangeCheck())
            {
                track.SetNameWithUndo(trackName);
            }
        }

        float DrawRecordButton(Rect rect, WindowState state)
        {
            var style = DirectorStyles.Instance.trackRecordButton;
            const float buttonWidth = WindowConstants.trackHeaderButtonSize + WindowConstants.trackHeaderButtonPadding;

            if (m_TrackDrawData.m_AllowsRecording)
            {
                bool isPlayerDisabled = state.editSequence.director != null && !state.editSequence.director.isActiveAndEnabled;

                GameObject goBinding = m_TrackDrawData.m_TrackBinding as GameObject;
                if (goBinding == null)
                {
                    Component c = m_TrackDrawData.m_TrackBinding as Component;
                    if (c != null)
                        goBinding = c.gameObject;
                }

                if (goBinding == null && m_TrackDrawData.m_IsSubTrack)
                    goBinding = ParentTrack().GetGameObjectBinding(state.editSequence.director);

                var isTrackBindingValid = goBinding != null;
                var trackErrorDisableButton = !string.IsNullOrEmpty(m_TrackDrawOptions.errorText) && isTrackBindingValid && goBinding.activeInHierarchy;
                var disableButton = track.lockedInHierarchy || isPlayerDisabled || trackErrorDisableButton || !isTrackBindingValid || state.ignorePreview;
                using (new EditorGUI.DisabledScope(disableButton))
                {
                    if (IsRecording(state))
                    {
                        state.editorWindow.Repaint();
                        var remainder = Time.realtimeSinceStartup % 1;

                        if (remainder < 0.22f)
                            style = GUIStyle.none;
                        if (GUI.Button(rect, Styles.kActiveRecordButtonTooltip, style) || isPlayerDisabled || !isTrackBindingValid)
                            state.UnarmForRecord(track);
                    }
                    else if (!track.timelineAsset.editorSettings.scenePreview)
                        GUI.Button(rect, Styles.kIgnorePreviewRecordButtonTooltip, style);
                    else
                    {
                        if (GUI.Button(rect, Styles.kInactiveRecordButtonTooltip, style))
                            state.ArmForRecord(track);
                    }
                    return buttonWidth;
                }
            }

            if (showTrackRecordingDisabled)
            {
                using (new EditorGUI.DisabledScope(true))
                    GUI.Button(rect, Styles.kDisabledRecordButtonTooltip, style);
                return buttonWidth;
            }

            return 0.0f;
        }

        float DrawCustomTrackButton(Rect rect, WindowState state)
        {
            if (!drawer.HasCustomTrackHeaderButton())
                return 0.0f;

            drawer.DrawTrackHeaderButton(rect, state);
            return WindowConstants.trackHeaderButtonSize + WindowConstants.trackHeaderButtonPadding;
        }

        static void ObjectBindingField(Rect position, Object obj, PlayableBinding binding, int controlId)
        {
            var allowScene =
                typeof(GameObject).IsAssignableFrom(binding.outputTargetType) ||
                typeof(Component).IsAssignableFrom(binding.outputTargetType);

            var bindingFieldRect = EditorGUI.IndentedRect(position);
            using (new GUIViewportScope(bindingFieldRect))
            {
                EditorGUI.BeginChangeCheck();
                var newObject = UnityEditorInternals.DoObjectField(EditorGUI.IndentedRect(position), obj, binding.outputTargetType, controlId, allowScene, true);
                if (EditorGUI.EndChangeCheck())
                    BindingUtility.BindWithInteractiveEditorValidation(TimelineEditor.inspectedDirector, binding.sourceObject as TrackAsset, newObject);
            }
        }

        void DoTrackBindingGUI(Rect rect)
        {
            var bindingRect = new Rect(
                rect.xMin,
                rect.y + (rect.height - WindowConstants.trackHeaderBindingHeight) / 2f,
                Mathf.Min(rect.width, WindowConstants.trackBindingMaxSize) - WindowConstants.trackBindingPadding,
                WindowConstants.trackHeaderBindingHeight);

            if (m_TrackDrawData.m_Binding.outputTargetType != null && typeof(Object).IsAssignableFrom(m_TrackDrawData.m_Binding.outputTargetType))
            {
                var controlId = GUIUtility.GetControlID("s_ObjectFieldHash".GetHashCode(), FocusType.Passive, rect);
                var previousActiveControlId = DragAndDrop.activeControlID;

                ObjectBindingField(bindingRect, m_TrackDrawData.m_TrackBinding, m_TrackDrawData.m_Binding, controlId);
                if (previousActiveControlId != controlId && DragAndDrop.activeControlID == controlId)
                    TimelineDragging.OnTrackBindingDragUpdate(track);
            }
        }

        bool IsRecording(WindowState state)
        {
            return state.recording && state.IsArmedForRecord(track);
        }

        // background to draw during recording
        void DrawRecordingTrackBackground(Rect trackRect, TrackAsset trackAsset, Vector2 visibleTime, WindowState state)
        {
            if (drawer != null)
                drawer.DrawRecordingBackground(trackRect, trackAsset, visibleTime, state);
        }

        void UpdateClipOverlaps()
        {
            TrackExtensions.ComputeBlendsFromOverlaps(track.clips);
        }

        internal void RebuildGUICacheIfNecessary()
        {
            if (m_TrackHash == track.Hash())
                return;

            m_ItemsDrawer = new TrackItemsDrawer(this);
            m_TrackHash = track.Hash();
        }

        int BlendHash()
        {
            var hash = 0;
            foreach (var clip in track.clips)
            {
                hash = HashUtility.CombineHash(hash,
                    (clip.duration - clip.start).GetHashCode(),
                    ((int)clip.blendInCurveMode).GetHashCode(),
                    ((int)clip.blendOutCurveMode).GetHashCode());
            }
            return hash;
        }

        // callback when the corresponding graph is rebuilt. This can happen, but not have the GUI rebuilt.
        public override void OnGraphRebuilt()
        {
            /* do nothing */
        }

        public void ValidateCurvesSelection() { }
    }
}
