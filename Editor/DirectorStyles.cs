using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UnityEditor.Timeline
{
    class DirectorStyles
    {
        const string k_Elipsis = "…";
        const string k_ImagePath = "Packages/com.unity.timeline/Editor/StyleSheets/Images/Icons/{0}.png";
        const string k_ResourcesPath = stylesheetsPath + "res/";
        const string k_DarkSkinPath = k_ResourcesPath + "Timeline_DarkSkin.txt";
        const string k_LightSkinPath = k_ResourcesPath + "Timeline_LightSkin.txt";

        //Timeline resources
        public const string stylesheetsPath = "Packages/com.unity.timeline/Editor/StyleSheets/";
        public const string newTimelineDefaultNameSuffix = "Timeline";

        public static readonly GUIContent referenceTrackLabel = TrTextContent("R", "This track references an external asset");
        public static readonly GUIContent recordingLabel = TrTextContent("Recording...");
        public static readonly GUIContent noTimelineAssetSelected = TrTextContent("To start creating a timeline, select a GameObject");
        public static readonly GUIContent createTimelineOnSelection = TrTextContent("To begin a new timeline with {0}, create {1}");
        public static readonly GUIContent noTimelinesInScene = TrTextContent("No timeline found in the scene");
        public static readonly GUIContent createNewTimelineText = TrTextContent("Create a new Timeline and Director Component for Game Object");
        public static readonly GUIContent previewContent = TrTextContent("Preview", "Enable/disable scene preview mode");
        public static readonly GUIContent previewDisabledContent = L10n.TextContentWithIcon("Preview", "Scene preview is disabled for this TimelineAsset", MessageType.Info);
        public static readonly GUIContent showMarkersOn = TrIconContent("TimelineCollapseMarkerButtonEnabled", "Show / Hide Timeline Markers");
        public static readonly GUIContent showMarkersOff = TrIconContent("TimelineCollapseMarkerButtonDisabled", "Show / Hide Timeline Markers");
        public static readonly GUIContent showMarkersOnTimeline = TrTextContent("Show markers");
        public static readonly GUIContent timelineMarkerTrackHeader = TrTextContentWithIcon("Markers", string.Empty, "TimelineHeaderMarkerIcon");

        //Unity Default Resources
        public static readonly GUIContent playContent = L10n.IconContent("Animation.Play", "Play the timeline (Space)");
        public static readonly GUIContent gotoBeginingContent = L10n.IconContent("Animation.FirstKey", "Go to the beginning of the timeline (Shift+<)");
        public static readonly GUIContent gotoEndContent = L10n.IconContent("Animation.LastKey", "Go to the end of the timeline (Shift+>)");
        public static readonly GUIContent nextFrameContent = L10n.IconContent("Animation.NextKey", "Go to the next frame");
        public static readonly GUIContent previousFrameContent = L10n.IconContent("Animation.PrevKey", "Go to the previous frame");
        public static readonly GUIContent newContent = L10n.IconContent("CreateAddNew", "Add new tracks.");
        public static readonly GUIContent optionsCogIcon = L10n.IconContent("_Popup", "Options");
        public static readonly GUIContent timelineSelectorArrow = L10n.IconContent("icon dropdown", "Timeline Selector");

        public GUIContent playrangeContent;

        public static readonly float kBaseIndent = 15.0f;
        public static readonly float kDurationGuiThickness = 5.0f;

        // matches dark skin warning color.
        public static readonly Color kClipErrorColor = new Color(0.957f, 0.737f, 0.008f, 1f);

        // TODO: Make skinnable? If we do, we should probably also make the associated cursors skinnable...
        public static readonly Color kMixToolColor = Color.white;

        public const string markerDefaultStyle = "MarkerItem";

        public GUIStyle groupBackground;
        public GUIStyle displayBackground;
        public GUIStyle fontClip;
        public GUIStyle fontClipLoop;
        public GUIStyle trackHeaderFont;
        public GUIStyle trackGroupAddButton;
        public GUIStyle groupFont;
        public GUIStyle timeCursor;
        public GUIStyle endmarker;
        public GUIStyle tinyFont;
        public GUIStyle foldout;
        public GUIStyle trackRecordButton;
        public GUIStyle playTimeRangeStart;
        public GUIStyle playTimeRangeEnd;
        public GUIStyle selectedStyle;
        public GUIStyle trackSwatchStyle;
        public GUIStyle connector;
        public GUIStyle keyframe;
        public GUIStyle extrapolationHold;
        public GUIStyle extrapolationLoop;
        public GUIStyle extrapolationPingPong;
        public GUIStyle extrapolationContinue;
        public GUIStyle markerMultiOverlay;
        public GUIStyle bottomShadow;
        public GUIStyle trackOptions;
        public GUIStyle infiniteTrack;
        public GUIStyle clipOut;
        public GUIStyle clipIn;
        public GUIStyle trackLockOverlay;
        public GUIStyle playrange;
        public GUIStyle timelineLockButton;
        public GUIStyle markerWarning;
        public GUIStyle showMarkersBtn;
        public GUIStyle sequenceSwitcher;
        public GUIStyle timeReferenceButton;
        public GUIStyle previewButtonDisabled;

        static internal DirectorStyles s_Instance;

        DirectorNamedColor m_DarkSkinColors;
        DirectorNamedColor m_LightSkinColors;
        DirectorNamedColor m_DefaultSkinColors;

        static readonly GUIContent s_TempContent = new GUIContent();

        public static bool IsInitialized
        {
            get { return s_Instance != null; }
        }

        public static DirectorStyles Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new DirectorStyles();
                    s_Instance.Initialize();
                }

                return s_Instance;
            }
        }

        public static void ReloadStylesIfNeeded()
        {
            if (Instance.ShouldLoadStyles())
            {
                Instance.LoadStyles();
                if (!Instance.ShouldLoadStyles())
                    Instance.Initialize();
            }
        }

        public DirectorNamedColor customSkin
        {
            get { return EditorGUIUtility.isProSkin ? m_DarkSkinColors : m_LightSkinColors; }
            internal set
            {
                if (EditorGUIUtility.isProSkin)
                    m_DarkSkinColors = value;
                else
                    m_LightSkinColors = value;
            }
        }

        DirectorNamedColor LoadColorSkin(string path)
        {
            var asset = EditorGUIUtility.LoadRequired(path) as TextAsset;

            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                return DirectorNamedColor.CreateAndLoadFromText(asset.text);
            }

            return m_DefaultSkinColors;
        }

        static DirectorNamedColor CreateDefaultSkin()
        {
            var nc = ScriptableObject.CreateInstance<DirectorNamedColor>();
            nc.SetDefault();
            return nc;
        }

        public void ExportSkinToFile()
        {
            if (customSkin == m_DarkSkinColors)
                customSkin.ToText(k_DarkSkinPath);

            if (customSkin == m_LightSkinColors)
                customSkin.ToText(k_LightSkinPath);
        }

        public void ReloadSkin()
        {
            if (customSkin == m_DarkSkinColors)
            {
                m_DarkSkinColors = LoadColorSkin(k_DarkSkinPath);
            }
            else if (customSkin == m_LightSkinColors)
            {
                m_LightSkinColors = LoadColorSkin(k_LightSkinPath);
            }
        }

        public void Initialize()
        {
            m_DefaultSkinColors = CreateDefaultSkin();
            m_DarkSkinColors = LoadColorSkin(k_DarkSkinPath);
            m_LightSkinColors = LoadColorSkin(k_LightSkinPath);

            // add the built in colors (control track uses attribute)
            TrackResourceCache.ClearTrackColorCache();
            TrackResourceCache.SetTrackColor<AnimationTrack>(customSkin.colorAnimation);
            TrackResourceCache.SetTrackColor<PlayableTrack>(Color.white);
            TrackResourceCache.SetTrackColor<AudioTrack>(customSkin.colorAudio);
            TrackResourceCache.SetTrackColor<ActivationTrack>(customSkin.colorActivation);
            TrackResourceCache.SetTrackColor<GroupTrack>(customSkin.colorGroup);
            TrackResourceCache.SetTrackColor<ControlTrack>(customSkin.colorControl);
        }

        DirectorStyles()
        {
            LoadStyles();
        }

        bool ShouldLoadStyles()
        {
            return endmarker == null ||
                endmarker.name == GUISkin.error.name;
        }

        void LoadStyles()
        {
            endmarker = GetGUIStyle("Icon-Endmarker");
            groupBackground = GetGUIStyle("groupBackground");
            displayBackground = GetGUIStyle("sequenceClip");
            fontClip = GetGUIStyle("Font-Clip");
            trackHeaderFont = GetGUIStyle("sequenceTrackHeaderFont");
            trackGroupAddButton = GetGUIStyle("sequenceTrackGroupAddButton");
            groupFont = GetGUIStyle("sequenceGroupFont");
            timeCursor = GetGUIStyle("Icon-TimeCursor");
            tinyFont = GetGUIStyle("tinyFont");
            foldout = GetGUIStyle("Icon-Foldout");
            trackRecordButton = GetGUIStyle("trackRecordButton");
            playTimeRangeStart = GetGUIStyle("Icon-PlayAreaStart");
            playTimeRangeEnd = GetGUIStyle("Icon-PlayAreaEnd");
            selectedStyle = GetGUIStyle("Color-Selected");
            trackSwatchStyle = GetGUIStyle("Icon-TrackHeaderSwatch");
            connector = GetGUIStyle("Icon-Connector");
            keyframe = GetGUIStyle("Icon-Keyframe");
            extrapolationHold = GetGUIStyle("Icon-ExtrapolationHold");
            extrapolationLoop = GetGUIStyle("Icon-ExtrapolationLoop");
            extrapolationPingPong = GetGUIStyle("Icon-ExtrapolationPingPong");
            extrapolationContinue = GetGUIStyle("Icon-ExtrapolationContinue");
            bottomShadow = GetGUIStyle("Icon-Shadow");
            trackOptions = GetGUIStyle("Icon-TrackOptions");
            infiniteTrack = GetGUIStyle("Icon-InfiniteTrack");
            clipOut = GetGUIStyle("Icon-ClipOut");
            clipIn = GetGUIStyle("Icon-ClipIn");
            trackLockOverlay = GetGUIStyle("trackLockOverlay");
            playrange = GetGUIStyle("Icon-Playrange");
            timelineLockButton = GetGUIStyle("IN LockButton");
            markerMultiOverlay = GetGUIStyle("MarkerMultiOverlay");
            showMarkersBtn = GetGUIStyle("showMarkerBtn");
            markerWarning = GetGUIStyle("markerWarningOverlay");
            sequenceSwitcher = GetGUIStyle("sequenceSwitcher");
            timeReferenceButton = GetGUIStyle("timeReferenceButton");
            previewButtonDisabled = GetGUIStyle("previewButtonDisabled");

            playrangeContent = new GUIContent(GetBackgroundImage(playrange)) { tooltip = L10n.Tr("Toggle play range markers.") };

            fontClipLoop = new GUIStyle(fontClip) { fontStyle = FontStyle.Bold };
        }

        public static GUIStyle GetGUIStyle(string s)
        {
            return EditorStyles.FromUSS(s);
        }

        public static GUIContent TrIconContent(string iconName, string tooltip = null)
        {
            return L10n.IconContent(iconName == null ? null : ResolveIcon(iconName), tooltip);
        }

        public static GUIContent IconContent(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName == null ? null : ResolveIcon(iconName));
        }

        public static GUIContent TrTextContentWithIcon(string text, string tooltip, string iconName)
        {
            return L10n.TextContentWithIcon(text, tooltip, iconName == null ? null : ResolveIcon(iconName));
        }

        public static GUIContent TrTextContent(string text, string tooltip = null)
        {
            return L10n.TextContent(text, tooltip);
        }

        public static Texture2D LoadIcon(string iconName)
        {
            return EditorGUIUtility.LoadIconRequired(iconName == null ? null : ResolveIcon(iconName));
        }

        static string ResolveIcon(string icon)
        {
            return string.Format(k_ImagePath, icon);
        }

        public static string Elipsify(string label, Rect rect, GUIStyle style)
        {
            var ret = label;

            if (label.Length == 0)
                return ret;

            s_TempContent.text = label;
            float neededWidth = style.CalcSize(s_TempContent).x;

            return Elipsify(label, rect.width, neededWidth);
        }

        public static string Elipsify(string label, float destinationWidth, float neededWidth)
        {
            var ret = label;

            if (label.Length == 0)
                return ret;

            if (destinationWidth < neededWidth)
            {
                float averageWidthOfOneChar = neededWidth / label.Length;
                int floor = Mathf.Max((int)Mathf.Floor(destinationWidth / averageWidthOfOneChar), 0);

                if (floor < k_Elipsis.Length)
                    ret = string.Empty;
                else if (floor == k_Elipsis.Length)
                    ret = k_Elipsis;
                else if (floor < label.Length)
                    ret = label.Substring(0, floor - k_Elipsis.Length) + k_Elipsis;
            }

            return ret;
        }

        public static Texture2D GetBackgroundImage(GUIStyle style, StyleState state = StyleState.normal)
        {
            var blockName = GUIStyleExtensions.StyleNameToBlockName(style.name, false);
            var styleBlock = EditorResources.GetStyle(blockName, state);
            return styleBlock.GetTexture(StyleCatalogKeyword.backgroundImage);
        }

        public static StyleSheet LoadStyleSheet(string path)
        {
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }
    }
}
