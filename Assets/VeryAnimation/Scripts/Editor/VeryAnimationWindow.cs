//#define Enable_Profiler

using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Compilation;
#if Enable_Profiler
using UnityEngine.Profiling;
# endif
using System;
using System.Linq;
using System.Collections.Generic;

#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace VeryAnimation
{
    [Serializable]
    internal class VeryAnimationWindow : EditorWindow
    {
        public const string Version = "1.3.6";
        public const int AsmdefVersion = 31;

        [MenuItem("Window/Very Animation/Main")]
        public static void Open()
        {
            static void ReOpen()
            {
                if (instance != null)
                {
                    try
                    {
                        instance.Close();
                    }
                    catch
                    {
                        EditorWindow.DestroyImmediate(instance, true);
                    }
                }
                EditorApplication.delayCall += () =>
                {
                    Open();
                };
            }

            if (instance == null)
            {
                GetWindow<VeryAnimationWindow>();
            }
            else if (instance.VA != null)
            {
                instance.SetGameObject();
                instance.VA.UpdateCurrentInfo();
                if (!instance.VA.IsError && !instance.VA.IsEdit)
                {
                    instance.Initialize();
                    if (!instance.UEditorWindow.HasFocus(instance))
                    {
                        ReOpen();
                    }
                }
                else
                {
                    ReOpen();
                }
            }
            else
            {
                ReOpen();
            }
        }

        public static VeryAnimationWindow instance;

        public GameObject GameObject { get; private set; }
        public Animator Animator { get; private set; }
        public Animation Animation { get; private set; }

        public VeryAnimation.PlayingAnimationInfo[] PlayingAnimationInfos { get; private set; }

        #region Core
        public VeryAnimation VA => va;
        [SerializeField]
        private VeryAnimation va;
        private VeryAnimationEditorWindow VAE => VeryAnimationEditorWindow.instance;
        //private VeryAnimationControlWindow VAC => VeryAnimationControlWindow.instance;

        public EditorSettings EditorSettings { get; private set; }
        #endregion

        #region Reflection
        public UEditorWindow UEditorWindow { get; private set; }
        public USceneView USceneView { get; private set; }
        public UEditorUtility UEditorUtility { get; private set; }
        public UEditorGUIUtility UEditorGUIUtility { get; private set; }
        public UDisc UDisc { get; private set; }
        public UMuscleClipEditorUtilities UMuscleClipQualityInfo { get; private set; }
        public UAnimationUtility UAnimationUtility { get; private set; }
        public UEditorGUI UEditorGUI { get; private set; }
        public UHandleUtility UHandleUtility { get; private set; }
        public UPrefabStage UPrefabStage { get; private set; }
        #endregion

        #region Editor
        public bool Initialized { get; private set; }
        private int undoGroupID = -1;
        private AnimatorStateSave pauseAnimatorStateSave;
        private int beforeErrorCode;
        private AnimationClip beforeAnimationClip;
        private bool handleTransformUpdate = true;
        private Vector3 handlePosition;
        private Quaternion handleRotation;
        private Vector3 handleScale;
        private Vector3 handlePositionSave;
        private Quaternion handleRotationSave;

        private int[] muscleRotationHandleIds;
        [NonSerialized]
        public int[] muscleRotationSliderIds;

        private EditorCommon.ArrowMesh arrowMesh;
        private RootTrail rootTrail;

        public enum RepaintGUI
        {
            None,
            Edit,
            All,
        }
        private RepaintGUI repaintGUI;
        public void SetRepaintGUI(RepaintGUI type)
        {
            if (repaintGUI < type)
                repaintGUI = type;
        }

        private int beforeSelectedTab;

        private Vector2 errorLogScrollPosition;
        private Vector2 helpScrollPosition;

        private GameObject forceChangeObject;

        private bool checkGuiLayoutUpdate;
        #endregion

        #region ClipSelector
#if UNITY_6000_2_OR_NEWER
        public class SelectorTreeView : TreeView<int>
#else
        public class SelectorTreeView : TreeView
#endif
        {
            private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

            public AnimationClip[] AnimationClips { get; set; }

            private readonly UTreeView uTreeView;

#if UNITY_6000_2_OR_NEWER
            public SelectorTreeView(TreeViewState<int> state) : base(state)
#else
            public SelectorTreeView(TreeViewState state) : base(state)
#endif
            {
                uTreeView = new UTreeView();

                showBorder = true;
            }

#if UNITY_6000_2_OR_NEWER
            protected override TreeViewItem<int> BuildRoot()
            {
                var root = new TreeViewItem<int>(int.MinValue, -1, "Root")
                {
                    children = new List<TreeViewItem<int>>()
                };
                if (VAW.GameObject != null)
                {
                    foreach (var clip in AnimationClips)
                    {
                        root.children.Add(new TreeViewItem<int>(clip.GetInstanceID(), 0, clip.name));
                    }
                }
                return root;
            }
#else
            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(int.MinValue, -1, "Root")
                {
                    children = new List<TreeViewItem>()
                };
                if (VAW.GameObject != null)
                {
                    foreach (var clip in AnimationClips)
                    {
                        root.children.Add(new TreeViewItem(clip.GetInstanceID(), 0, clip.name));
                    }
                }
                return root;
            }
#endif

            protected override void SelectionChanged(IList<int> selectedIds)
            {
#if UNITY_6000_3_OR_NEWER
                var clip = EditorUtility.EntityIdToObject(state.lastClickedID) as AnimationClip;
#else
                var clip = EditorUtility.InstanceIDToObject(state.lastClickedID) as AnimationClip;
#endif
                if (clip != null)
                {
                    VAW.VA.SetCurrentClip(clip);
                }
            }

            public void UpdateSelectedIds()
            {
                state.selectedIDs.Clear();
                if (AnimationClips != null)
                {
                    var index = ArrayUtility.IndexOf(AnimationClips, VAW.VA.UAw.GetSelectionAnimationClip());
                    if (index >= 0 && AnimationClips[index] != null)
                        state.selectedIDs.Add(AnimationClips[index].GetInstanceID());
                }
            }

            public void OffsetSelection(int offset)
            {
                uTreeView.OffsetSelection(this, offset);
            }
        };

        [SerializeField]
        private bool clipSelectorFoldout = false;

        private int clipSelectorLayerIndex = -1;

#if UNITY_6000_2_OR_NEWER
        public TreeViewState<int> ClipSelectorTreeState { get; private set; }
#else
        public TreeViewState ClipSelectorTreeState { get; private set; }
#endif
        public SearchField ClipSelectorTreeSearchField { get; private set; }
        public SelectorTreeView ClipSelectorTreeView { get; private set; }


        private void InitializeClipSelector()
        {
            clipSelectorLayerIndex = -1;
#if UNITY_6000_2_OR_NEWER
            ClipSelectorTreeState = new TreeViewState<int>();
#else
            ClipSelectorTreeState = new TreeViewState();
#endif
            ClipSelectorTreeSearchField = new SearchField();
            ClipSelectorTreeView = new SelectorTreeView(ClipSelectorTreeState);
            ClipSelectorTreeSearchField.downOrUpArrowKeyPressed += ClipSelectorTreeView.SetFocusAndEnsureSelectedItem;
        }
        public void UpdateClipSelectorTree()
        {
            if (ClipSelectorTreeView == null) return;

            if (clipSelectorLayerIndex < 0)
                ClipSelectorTreeView.AnimationClips = VA.GetAnimationClips();
            else
                ClipSelectorTreeView.AnimationClips = VA.GetLayerAnimationClips(clipSelectorLayerIndex);

            ClipSelectorTreeView.Reload();
            ClipSelectorTreeView.ExpandAll();
            ClipSelectorTreeView.UpdateSelectedIds();
        }
        public void SetClipSelectorLayerIndex(int index = -1)
        {
            clipSelectorLayerIndex = index;
            checkGuiLayoutUpdate = true;
        }
        #endregion

        #region SelectionRect
        private struct SelectionRect
        {
            public void Reset()
            {
                Enable = false;
                Start = Vector2.zero;
                End = Vector2.zero;
                Distance = 0f;
                if (calcList == null) calcList = new List<GameObject>();
                else calcList.Clear();
                if (virtualCalcList == null) virtualCalcList = new List<HumanBodyBones>();
                else virtualCalcList.Clear();
                if (animatorIKCalcList == null) animatorIKCalcList = new List<AnimatorIKCore.IKTarget>();
                else animatorIKCalcList.Clear();
                if (originalIKCalcList == null) originalIKCalcList = new List<int>();
                else originalIKCalcList.Clear();
                beforeSelection = null;
                virtualBeforeSelection = null;
                beforeAnimatorIKSelection = null;
                beforeOriginalIKSelection = null;
            }
            public void SetStart(Vector2 add)
            {
                Enable = true;
                Start = add;
                End = add;
                Distance = 0f;
            }
            public void SetEnd(Vector2 add)
            {
                Distance += Vector2.Distance(End, add);
                End = add;
            }
            public bool Enable { get; private set; }
            public readonly Vector2 Min => Vector2.Min(Start, End);
            public readonly Vector2 Max => Vector2.Max(Start, End);
            public readonly Rect Rect => new(Min.x, Min.y, Max.x - Min.x, Max.y - Min.y);

            public Vector2 Start { get; private set; }
            public Vector2 End { get; private set; }
            public float Distance { get; private set; }

            public List<GameObject> calcList;
            public List<HumanBodyBones> virtualCalcList;
            public List<AnimatorIKCore.IKTarget> animatorIKCalcList;
            public List<int> originalIKCalcList;
            public GameObject[] beforeSelection;
            public HumanBodyBones[] virtualBeforeSelection;
            public AnimatorIKCore.IKTarget[] beforeAnimatorIKSelection;
            public int[] beforeOriginalIKSelection;
        }
        private SelectionRect selectionRect;
        #endregion

        #region DisableEditor
        public class CustomAssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {
            private static bool enable = true;

#pragma warning disable IDE0051
            static string[] OnWillSaveAssets(string[] paths)
            {
                if (enable)
                {
                    foreach (var w in Resources.FindObjectsOfTypeAll<VeryAnimationWindow>())
                    {
                        if (w.Initialized)
                        {
                            w.Release();
                            Debug.Log("<color=blue>[Very Animation]</color>Editing ended : OnWillSaveAssets");
                        }
                    }
                }
                return paths;
            }
#pragma warning restore IDE0051

            public static void Pause()
            {
                enable = false;
            }
            public static void Resume()
            {
                enable = true;
            }
        }

        [InitializeOnLoad]
        public class CustomCompilationListener
        {
            static CustomCompilationListener()
            {
                CompilationPipeline.compilationStarted += OnCompilationStarted;
            }

            private static void OnCompilationStarted(object context)
            {
                foreach (var w in Resources.FindObjectsOfTypeAll<VeryAnimationWindow>())
                {
                    if (w.Initialized)
                    {
                        w.Release();
                        Selection.activeGameObject = null;
                        Debug.Log("<color=blue>[Very Animation]</color>Editing ended : CompilationPipeline.compilationStarted");
                    }
                    else
                    {
                        w.ClearGameObject();
                    }
                }
            }
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<VeryAnimationWindow>())
            {
                if (w.Initialized)
                {
                    w.Release();
                    Debug.Log("<color=blue>[Very Animation]</color>Editing ended : InitializeOnLoadMethod");
                }
            }
        }

        static void CloseOtherWindows()
        {
            static void ForceCloseWindow(EditorWindow w)
            {
                if (w != null)
                {
                    try
                    {
                        w.Close();
                    }
                    catch
                    {
                        EditorWindow.DestroyImmediate(w, true);
                    }
                }
            }
            ForceCloseWindow(VeryAnimationControlWindow.instance);
            ForceCloseWindow(VeryAnimationEditorWindow.instance);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<VeryAnimationWindow>())
            {
                if (w.Initialized)
                {
                    w.Release();
                    Debug.Log("<color=blue>[Very Animation]</color>Editing ended : OnPlayModeStateChanged");
                }
            }
        }
        private void OnPauseStateChanged(PauseState mode)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<VeryAnimationWindow>())
            {
                if (w.Initialized)
                {
                    w.Release();
                    Debug.Log("<color=blue>[Very Animation]</color>Editing ended : OnPauseStateChanged");
                }
            }
        }
        #endregion

        #region Texture
        private Texture2D circleNormalTex;
        private Texture2D circleActiveTex;
        private Texture2D circle3NormalTex;
        private Texture2D circle3ActiveTex;
        private Texture2D diamondNormalTex;
        private Texture2D diamondActiveTex;
        private Texture2D circleDotNormalTex;
        private Texture2D circleDotActiveTex;
        public Texture2D RedLightTex { get; private set; }
        public Texture2D OrangeLightTex { get; private set; }
        public Texture2D GreenLightTex { get; private set; }
        public Texture2D LightRimTex { get; private set; }
        public Texture2D MirrorTex { get; private set; }

        private void TextureReady()
        {
            circleNormalTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/Circle_normal.psd");
            circleActiveTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/Circle_active.psd");
            circle3NormalTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/Circle3_normal.psd");
            circle3ActiveTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/Circle3_active.psd");
            diamondNormalTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/Diamond_normal.psd");
            diamondActiveTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/Diamond_active.psd");
            circleDotNormalTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/CircleDot_normal.psd");
            circleDotActiveTex = EditorCommon.LoadTexture2DAssetAtPath("Assets/VeryAnimation/Textures/Editor/CircleDot_active.psd");
            RedLightTex = EditorGUIUtility.IconContent("lightMeter/redLight").image as Texture2D;
            OrangeLightTex = EditorGUIUtility.IconContent("lightMeter/orangeLight").image as Texture2D;
            GreenLightTex = EditorGUIUtility.IconContent("lightMeter/greenLight").image as Texture2D;
            LightRimTex = EditorGUIUtility.IconContent("lightMeter/lightRim").image as Texture2D;
            MirrorTex = EditorGUIUtility.IconContent("mirror").image as Texture2D;
        }
        #endregion

        #region GUIStyle
        public bool GuiStyleReady { get; private set; }
        public GUISkin GuiSkinSceneWindow { get; private set; }
        public GUIStyle GuiStyleSceneWindow { get; private set; }
        public GUIStyle GuiStyleSkinBox { get; private set; }
        public GUIStyle GuiStyleBoldButton { get; private set; }
        public GUIStyle GuiStyleCircleButton { get; private set; }
        public GUIStyle GuiStyleCircle3Button { get; private set; }
        public GUIStyle GuiStyleDiamondButton { get; private set; }
        public GUIStyle GuiStyleCircleDotButton { get; private set; }
        public GUIStyle GuiStyleCenterAlignLabel { get; private set; }
        public GUIStyle GuiStyleCenterAlignItalicLabel { get; private set; }
        public GUIStyle GuiStyleCenterAlignYellowLabel { get; private set; }
        public GUIStyle GuiStyleBoldFoldout { get; private set; }
        public GUIStyle GuiStyleDropDown { get; private set; }
        public GUIStyle GuiStyleToolbarBoldButton { get; private set; }
        public GUIStyle GuiStyleAnimationRowEvenStyle { get; private set; }
        public GUIStyle GuiStyleAnimationRowOddStyle { get; private set; }
        public GUIStyle GuiStyleMiddleRightMiniLabel { get; private set; }
        public GUIStyle GuiStyleMiddleLeftGreyMiniLabel { get; private set; }
        public GUIStyle GuiStyleMiddleRightGreyMiniLabel { get; private set; }
        public GUIStyle GuiStyleMirrorButton { get; private set; }
        public GUIStyle GuiStyleLockToggle { get; private set; }
        public GUIStyle GuiStyleIconButton { get; private set; }
        public GUIStyle GuiStyleIconActiveButton { get; private set; }
        public GUIContent[] GuiContentMoveRotateTools { get; private set; }

        private void GUIStyleReady()
        {
            if (GuiSkinSceneWindow == null)
            {
                if (EditorGUIUtility.isProSkin)
                    GuiSkinSceneWindow = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
                else
                    GuiSkinSceneWindow = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
            }
            if (GuiStyleSceneWindow == null)
            {
                if (EditorGUIUtility.isProSkin)
                    GuiStyleSceneWindow = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).window);
                else
                    GuiStyleSceneWindow = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).window);
            }
            if (GuiStyleSkinBox == null)
            {
                GuiStyleSkinBox = new GUIStyle(GUI.skin.box);
                var olBox = new GUIStyle("OL box");
                GuiStyleSkinBox.normal = olBox.normal;
                GuiStyleSkinBox.hover = olBox.hover;
                GuiStyleSkinBox.focused = olBox.focused;
                GuiStyleSkinBox.active = olBox.active;
            }
            GuiStyleBoldButton ??= new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
            if (GuiStyleCircleButton == null || GuiStyleCircleButton.normal.background != circleNormalTex || GuiStyleCircleButton.active.background != circleActiveTex)
            {
                GuiStyleCircleButton = new GUIStyle(GUI.skin.button);
                GuiStyleCircleButton.normal.background = circleNormalTex;
                GuiStyleCircleButton.normal.scaledBackgrounds = null;
                GuiStyleCircleButton.active.background = circleActiveTex;
                GuiStyleCircleButton.active.scaledBackgrounds = null;
                GuiStyleCircleButton.border = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.margin = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.overflow = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.padding = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.imagePosition = ImagePosition.ImageOnly;
            }
            if (GuiStyleCircle3Button == null || GuiStyleCircle3Button.normal.background != circle3NormalTex || GuiStyleCircle3Button.active.background != circle3ActiveTex)
            {
                GuiStyleCircle3Button = new GUIStyle(GUI.skin.button);
                GuiStyleCircle3Button.normal.background = circle3NormalTex;
                GuiStyleCircle3Button.normal.scaledBackgrounds = null;
                GuiStyleCircle3Button.active.background = circle3ActiveTex;
                GuiStyleCircle3Button.active.scaledBackgrounds = null;
                GuiStyleCircle3Button.border = new RectOffset(0, 0, 0, 0);
                GuiStyleCircle3Button.margin = new RectOffset(0, 0, 0, 0);
                GuiStyleCircle3Button.overflow = new RectOffset(0, 0, 0, 0);
                GuiStyleCircle3Button.padding = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.imagePosition = ImagePosition.ImageOnly;
            }
            if (GuiStyleDiamondButton == null || GuiStyleDiamondButton.normal.background != diamondNormalTex || GuiStyleDiamondButton.active.background != diamondActiveTex)
            {
                GuiStyleDiamondButton = new GUIStyle(GUI.skin.button);
                GuiStyleDiamondButton.normal.background = diamondNormalTex;
                GuiStyleDiamondButton.normal.scaledBackgrounds = null;
                GuiStyleDiamondButton.active.background = diamondActiveTex;
                GuiStyleDiamondButton.active.scaledBackgrounds = null;
                GuiStyleDiamondButton.border = new RectOffset(0, 0, 0, 0);
                GuiStyleDiamondButton.margin = new RectOffset(0, 0, 0, 0);
                GuiStyleDiamondButton.overflow = new RectOffset(0, 0, 0, 0);
                GuiStyleDiamondButton.padding = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.imagePosition = ImagePosition.ImageOnly;
            }
            if (GuiStyleCircleDotButton == null || GuiStyleCircleDotButton.normal.background != circleDotNormalTex || GuiStyleCircleDotButton.active.background != circleDotActiveTex)
            {
                GuiStyleCircleDotButton = new GUIStyle(GUI.skin.button);
                GuiStyleCircleDotButton.normal.background = circleDotNormalTex;
                GuiStyleCircleDotButton.normal.scaledBackgrounds = null;
                GuiStyleCircleDotButton.active.background = circleDotActiveTex;
                GuiStyleCircleDotButton.active.scaledBackgrounds = null;
                GuiStyleCircleDotButton.border = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleDotButton.margin = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleDotButton.overflow = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleDotButton.padding = new RectOffset(0, 0, 0, 0);
                GuiStyleCircleButton.imagePosition = ImagePosition.ImageOnly;
            }
            GuiStyleCenterAlignLabel ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GuiStyleCenterAlignItalicLabel ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            if (GuiStyleCenterAlignYellowLabel == null)
            {
                GuiStyleCenterAlignYellowLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                GuiStyleCenterAlignYellowLabel.normal.textColor = Color.yellow;
            }
            GuiStyleBoldFoldout ??= new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };
            GuiStyleDropDown ??= new GUIStyle("DropDown")
            {
                alignment = TextAnchor.MiddleCenter
            };
            GuiStyleToolbarBoldButton ??= new GUIStyle(EditorStyles.toolbarButton)
            {
                fontStyle = FontStyle.Bold
            };
            if (GuiStyleAnimationRowEvenStyle == null)
            {
                GuiStyleAnimationRowEvenStyle = new GUIStyle("AnimationRowEven");
                if (GuiStyleAnimationRowEvenStyle.normal.background == null && GuiStyleAnimationRowEvenStyle.normal.scaledBackgrounds != null && GuiStyleAnimationRowEvenStyle.normal.scaledBackgrounds.Length > 0)
                    GuiStyleAnimationRowEvenStyle.normal.background = GuiStyleAnimationRowEvenStyle.normal.scaledBackgrounds[0];
            }
            if (GuiStyleAnimationRowOddStyle == null)
            {
                GuiStyleAnimationRowOddStyle = new GUIStyle("AnimationRowOdd");
                if (GuiStyleAnimationRowOddStyle.normal.background == null && GuiStyleAnimationRowOddStyle.normal.scaledBackgrounds != null && GuiStyleAnimationRowOddStyle.normal.scaledBackgrounds.Length > 0)
                    GuiStyleAnimationRowOddStyle.normal.background = GuiStyleAnimationRowOddStyle.normal.scaledBackgrounds[0];
            }
            GuiStyleMiddleRightMiniLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            GuiStyleMiddleLeftGreyMiniLabel ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            GuiStyleMiddleRightGreyMiniLabel ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            if (GuiStyleMirrorButton == null)
            {
                GuiStyleMirrorButton = new GUIStyle(GUI.skin.button);
                GuiStyleMirrorButton.normal.background = MirrorTex;
                GuiStyleMirrorButton.normal.scaledBackgrounds = null;
            }
            GuiStyleLockToggle ??= new GUIStyle("IN LockButton");
            GuiStyleIconButton ??= new GUIStyle("IconButton");
            if (GuiStyleIconActiveButton == null)
            {
                GuiStyleIconActiveButton = new GUIStyle(GUI.skin.button);
                GuiStyleIconActiveButton.normal = GuiStyleIconActiveButton.active;
                GuiStyleIconActiveButton.padding = new RectOffset(0, 0, 0, 0);
            }

            GuiContentMoveRotateTools ??= new GUIContent[]
            {
                EditorGUIUtility.IconContent("MoveTool"),
                EditorGUIUtility.IconContent("RotateTool"),
            };

            GuiStyleReady = true;
        }
        private void GUIStyleClear()
        {
            GuiSkinSceneWindow = null;
            GuiStyleSceneWindow = null;
            GuiStyleSkinBox = null;
            GuiStyleBoldButton = null;
            GuiStyleCircleButton = null;
            GuiStyleCircle3Button = null;
            GuiStyleDiamondButton = null;
            GuiStyleCircleDotButton = null;
            GuiStyleCenterAlignLabel = null;
            GuiStyleCenterAlignItalicLabel = null;
            GuiStyleBoldFoldout = null;
            GuiStyleDropDown = null;
            GuiStyleToolbarBoldButton = null;
            GuiStyleAnimationRowEvenStyle = null;
            GuiStyleAnimationRowOddStyle = null;
            GuiStyleMiddleRightMiniLabel = null;
            GuiStyleMiddleLeftGreyMiniLabel = null;
            GuiStyleMiddleRightGreyMiniLabel = null;
            GuiStyleMirrorButton = null;
            GuiStyleLockToggle = null;
            GuiStyleIconButton = null;
            GuiStyleIconActiveButton = null;
            GuiContentMoveRotateTools = null;
            GuiStyleReady = false;
        }
        #endregion

        #region GUI
        public const float GUINonActiveAlpha = 0.5f;

        private bool guiAnimationFoldout;
        private bool guiToolsFoldout;
        private bool guiSettingsFoldout;
        private bool guiHelpFoldout;
        private bool guiPreviewFoldout;

        private bool guiAnimationHelp;
        private bool guiToolsHelp;
        private bool guiSettingsHelp;
        private bool guiHelpHelp;
        private bool guiPreviewHelp;

        public Rect editorWindowSelectionRect = new(8, 17 + 8, 0, 0);
        #endregion

        private void OnEnable()
        {
            instance = this;

            {
                va ??= new VeryAnimation();
                VA.OnEnable();

                EditorSettings = new EditorSettings();

                USceneView = new USceneView();
                UDisc = new UDisc();
                UMuscleClipQualityInfo = new UMuscleClipEditorUtilities();
                UAnimationUtility = new UAnimationUtility();
                UEditorGUI = new UEditorGUI();
                UHandleUtility = new UHandleUtility();
                UEditorUtility = new UEditorUtility();
                UEditorGUIUtility = new UEditorGUIUtility();
                UEditorWindow = new UEditorWindow();
                UPrefabStage = new UPrefabStage();
                InitializeClipSelector();
                TextureReady();
                GUIStyleClear();
            }

            titleContent = new GUIContent("VeryAnimation");
            minSize = new Vector2(320, minSize.y);

            InternalEditorUtility.RepaintAllViews();
        }
        private void OnDisable()
        {
            VA?.OnDisable();
        }
        private void OnDestroy()
        {
            Release();

            VA?.OnDestroy();
            instance = null;
        }

        private void OnSelectionChange()
        {
            checkGuiLayoutUpdate = true;

            if (!Initialized || VA.IsEditError) return;

            VA.SelectGameObjectEvent(true);

            Repaint();
        }
        private void OnFocus()
        {
            instance = this;    //Measures against the problem that OnEnable may not come when repeating Shift + Space.

            checkGuiLayoutUpdate = true;
        }

        private void OnGUI()
        {
            if (VA == null || VA.UAw == null)
                return;

#if Enable_Profiler
            Profiler.BeginSample(string.Format("****VeryAnimationWindow.OnGUI {0}", Event.current));
#endif

            GUIStyleReady();

            Event e = Event.current;

            if (e.type == EventType.Layout)
            {
                if (checkGuiLayoutUpdate)
                {
                    UpdateClipSelectorTree();
                    VA.CheckChangeLayersSettings();
                    checkGuiLayoutUpdate = false;
                }
            }

            if (VA.UAw.Instance == null)
            {
                #region Animation Window is not open
                EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationWindowisnotopen), MessageType.Error);
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Open Animation Window"))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }

                VersionInfoGUI();
                #endregion
            }
            else if (!VA.UAw.HasFocus())
            {
                #region Animation Window is not focus
                EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationWindowisnotfocus), MessageType.Error);
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Focus Animation Window"))
                    {
                        VA.UAw.Instance.Focus();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }

                VersionInfoGUI();
                #endregion
            }
            else if (GameObject == null || (Animator == null && Animation == null))
            {
                #region Selection Error
                if (VA.UAw.GetLinkedWithTimeline())
                    EditorGUILayout.LabelField(Language.GetText(Language.Help.TheSequenceEditortowhichAnimationislinkedisnotenabled), EditorStyles.centeredGreyMiniLabel, GUILayout.Height(48));
                else
                    EditorGUILayout.LabelField(Language.GetText(Language.Help.Noanimatableobjectselected), EditorStyles.centeredGreyMiniLabel, GUILayout.Height(48));

                VersionInfoGUI();
                #endregion
            }
            else if (!VA.IsEdit)
            {
                #region Ready
                VA.UpdateCurrentInfo();
                var clip = VA.CurrentClip;
                if (EditorApplication.isPlaying &&
                    PlayingAnimationInfos != null && PlayingAnimationInfos.Length > 0 && PlayingAnimationInfos[0] != null)
                {
                    clip = PlayingAnimationInfos[0].clip;
                }

                #region Animation
                {
                    EditorGUILayout.BeginVertical(GuiStyleSkinBox);
                    if (VA.UAw.GetLinkedWithTimeline())
                    {
#if VERYANIMATION_TIMELINE
                        EditorGUILayout.LabelField("Linked with Sequence Editor", EditorStyles.centeredGreyMiniLabel);
                        var currentDirector = VA.UAw.GetTimelineCurrentDirector();
                        if (currentDirector != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Playable Director", GUILayout.Width(116));
                            GUILayout.FlexibleSpace();
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(currentDirector, typeof(PlayableDirector), false, GUILayout.Width(180));
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();
                        }
#endif
                    }
                    else
                    {
                        if (EditorApplication.isPlaying)
                        {
                            if (Animator != null)
                                EditorGUILayout.LabelField("Linked with Animator Controller", EditorStyles.centeredGreyMiniLabel);
                            else if (Animation != null)
                                EditorGUILayout.LabelField("Linked with Animation Component", EditorStyles.centeredGreyMiniLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Linked with Animation Window", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    {
                        #region Animatable
                        if (Animator != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Animator", GUILayout.Width(116));
                            GUILayout.FlexibleSpace();
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(Animator, typeof(Animator), false, GUILayout.Width(180));
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();
                        }
                        else if (Animation != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Animation", GUILayout.Width(116));
                            GUILayout.FlexibleSpace();
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(Animation, typeof(Animation), false, GUILayout.Width(180));
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion

                        #region Animation Clip
                        if (VA.UAw.GetLinkedWithTimeline() || EditorApplication.isPlaying)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.LabelField("Animation Clip", GUILayout.Width(116));
                            GUILayout.FlexibleSpace();
                            var isReadOnly = clip != null && (clip.hideFlags & HideFlags.NotEditable) != HideFlags.None;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false, GUILayout.Width(isReadOnly ? 98 : 180));
                            EditorGUI.EndDisabledGroup();
                            if (isReadOnly)
                                EditorGUILayout.LabelField("(Read-Only)", GUILayout.Width(78));
                            EditorGUILayout.EndHorizontal();
                            if (PlayingAnimationInfos != null)
                            {
                                for (int i = 0; i < PlayingAnimationInfos.Length; i++)
                                {
                                    var info = PlayingAnimationInfos[i];
                                    if (info == null)
                                        continue;
                                    EditorGUI.indentLevel++;
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.Slider(string.Format("Layer{0} Time", i), info.time, 0f, info.length);
                                    EditorGUI.EndDisabledGroup();
                                    EditorGUI.indentLevel--;
                                }
                            }
                        }
                        else
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUI.BeginChangeCheck();
                            clipSelectorFoldout = EditorGUILayout.Foldout(clipSelectorFoldout, "Animation Clip", true);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UpdateClipSelectorTree();
                            }
                            GUILayout.FlexibleSpace();
                            var isReadOnly = clip != null && (clip.hideFlags & HideFlags.NotEditable) != HideFlags.None;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false, GUILayout.Width(isReadOnly ? 98 : 180));
                            EditorGUI.EndDisabledGroup();
                            if (isReadOnly)
                                EditorGUILayout.LabelField("(Read-Only)", GUILayout.Width(78));
                            EditorGUILayout.EndHorizontal();
                            if (clipSelectorFoldout)
                            {
                                #region ClipSelector
                                {
                                    ClipSelectorTreeView.UpdateSelectedIds();
                                    {
                                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                                        EditorGUI.indentLevel++;
                                        ClipSelectorTreeView.searchString = ClipSelectorTreeSearchField.OnToolbarGUI(ClipSelectorTreeView.searchString);
                                        EditorGUI.indentLevel--;
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    {
                                        var rect = EditorGUILayout.GetControlRect(false, position.height * 0.4f);
                                        ClipSelectorTreeView.OnGUI(rect);
                                    }
                                }
                                #endregion
                            }
                        }
                        #endregion
                    }
                    EditorGUILayout.EndVertical();
                }
                #endregion

                EditorGUI.BeginDisabledGroup(VA.IsError);
                if (GUILayout.Button("Edit Animation", GuiStyleBoldButton, GUILayout.Height(32)))
                {
                    Initialize();
                }
                EditorGUI.EndDisabledGroup();

                errorLogScrollPosition = EditorGUILayout.BeginScrollView(errorLogScrollPosition);
                {
                    #region UnityVersion
                    {
#if UNITY_6000_3_OR_NEWER || !UNITY_2021_3_OR_NEWER
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.NotSupportUnityMessage), MessageType.Error);
#endif
                    }
                    #endregion

                    #region Error
                    if (VA.UAw.GetSelectionAnimationClip() == null)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.ThereisnoAnimationClip), MessageType.Error);
                    }
                    if (!GameObject.activeInHierarchy)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.GameObjectisnotActive), MessageType.Error);
                    }
                    if (Animator != null && !Animator.hasTransformHierarchy)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.Editingonoptimizedtransformhierarchyisnotsupported), MessageType.Error);
                    }
                    if (Application.isPlaying && Animation != null)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.EditingLegacywhileplayingisnotsupported), MessageType.Error);
                    }
                    if (Application.isPlaying && Animator != null && Animator.runtimeAnimatorController == null)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.EditingNoAnimatorControllernotsupported), MessageType.Error);
                    }
                    if (!VA.UAw.GetLinkedWithTimeline())
                    {
                        if (Animator != null && Animator.runtimeAnimatorController != null && (Animator.runtimeAnimatorController.hideFlags & (HideFlags.DontSave | HideFlags.NotEditable)) != 0)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimatorControllerisnoteditable), MessageType.Error);
                        }
                    }
                    else
                    {
#if VERYANIMATION_TIMELINE
                        if (!VA.UAw.GetTimelineTrackAssetEditable())
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.TheAnimationTracktowhichAnimationislinkedisnotenabled), MessageType.Error);
                        }
                        if (Application.isPlaying)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.EditingTimelinewhileplayingisnotsupported), MessageType.Error);
                        }
                        {
                            var currentDirector = VA.UAw.GetTimelineCurrentDirector();
                            if (currentDirector != null && !currentDirector.gameObject.activeInHierarchy)
                            {
                                EditorGUILayout.HelpBox(Language.GetText(Language.Help.TimelineGameObjectisnotActive), MessageType.Error);
                            }
                            if (currentDirector != null && !currentDirector.enabled)
                            {
                                EditorGUILayout.HelpBox(Language.GetText(Language.Help.TimelinePlayableDirectorisnotEnable), MessageType.Error);
                            }
                        }
                        if (!VA.UAw.GetTimelineHasFocus())
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.TimelineWindowisnotfocus), MessageType.Error);
                        }
#endif
                    }
                    if (UPrefabStage.GetAutoSave(PrefabStageUtility.GetCurrentPrefabStage()))
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.PrefabModeEnableAutoSave), MessageType.Error);
                    }
                    if (PrefabStageUtility.GetCurrentPrefabStage() != null &&
                        !EditorCommon.IsAncestorObject(VA.UAw.GetActiveRootGameObject(), PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot))
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.PrefabModeObjectNotMatch), MessageType.Error);
                    }
                    #endregion

                    #region Warning
                    if (GameObject != null && GameObject.activeInHierarchy && Animator != null && Animator.isHuman && Animator.hasTransformHierarchy && VA.UAvatar.GetHasTDoF(Animator.avatar))
                    {
                        #region TDOF
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.TranslationDoFisEnableWarning), MessageType.Warning);
                        if (!Animator.isInitialized)
                            Animator.Rebind();
                        for (int i = 0; i < VeryAnimation.HumanBonesAnimatorTDOFIndex.Length; i++)
                        {
                            if (VeryAnimation.HumanBonesAnimatorTDOFIndex[i] == null) continue;
                            var hi = (HumanBodyBones)i;
                            if (Animator.GetBoneTransform(hi) != null)
                            {
                                if (Animator.GetBoneTransform(VeryAnimation.HumanBonesAnimatorTDOFIndex[i].parent) == null)
                                    EditorGUILayout.HelpBox(string.Format(Language.GetText(Language.Help.TranslationDoFisdisabled), VeryAnimation.HumanBonesAnimatorTDOFIndex[i].parent, hi), MessageType.Warning);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(string.Format(Language.GetText(Language.Help.TranslationDoFisdisabled), hi, hi), MessageType.Warning);
                            }
                        }
                        #endregion
                    }
                    if (GameObject != null)
                    {
                        foreach (var smRenderer in GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            if (smRenderer.sharedMesh == null)
                                continue;
                            for (int subMesh = 0; subMesh < smRenderer.sharedMesh.subMeshCount; subMesh++)
                            {
                                var topology = smRenderer.sharedMesh.GetTopology(subMesh);
                                if (topology == MeshTopology.Triangles)
                                    continue;
                                EditorGUILayout.HelpBox(string.Format(Language.GetText(Language.Help.MeshTopologyIsNotTriangles), smRenderer.sharedMesh.name, topology), MessageType.Warning);
                            }
                        }
                        foreach (var mfilter in GameObject.GetComponentsInChildren<MeshFilter>(true))
                        {
                            if (mfilter.sharedMesh == null)
                                continue;
                            for (int subMesh = 0; subMesh < mfilter.sharedMesh.subMeshCount; subMesh++)
                            {
                                var topology = mfilter.sharedMesh.GetTopology(subMesh);
                                if (topology == MeshTopology.Triangles)
                                    continue;
                                EditorGUILayout.HelpBox(string.Format(Language.GetText(Language.Help.MeshTopologyIsNotTriangles), mfilter.sharedMesh.name, topology), MessageType.Warning);
                            }
                        }
                    }
                    #endregion
                }
                EditorGUILayout.EndScrollView();

                VersionInfoGUI();
                #endregion
            }
            else if (!VA.IsEditError)
            {
                #region Editing
                #region Toolbar
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        EditorGUI.BeginChangeCheck();
                        guiAnimationFoldout = GUILayout.Toggle(guiAnimationFoldout, "Animation", EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_Main_Animation", guiAnimationFoldout);
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        guiToolsFoldout = GUILayout.Toggle(guiToolsFoldout, "Tools", EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_Main_Tools", guiToolsFoldout);
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        guiSettingsFoldout = GUILayout.Toggle(guiSettingsFoldout, "Settings", EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_Main_Settings", guiSettingsFoldout);
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        guiHelpFoldout = GUILayout.Toggle(guiHelpFoldout, "Help", EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_Main_Help", guiHelpFoldout);
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        guiPreviewFoldout = GUILayout.Toggle(guiPreviewFoldout, "Preview", EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_Main_Preview", guiPreviewFoldout);
                        }
                    }
                    EditorGUILayout.Space();
                    #region Edit
                    if (GUILayout.Button("Exit", GuiStyleToolbarBoldButton, GUILayout.Width(48)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            Release();
                        };
                    }
                    #endregion
                    EditorGUILayout.EndHorizontal();
                }
                #endregion

                #region Animation
                if (guiAnimationFoldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    guiAnimationFoldout = EditorGUILayout.Foldout(guiAnimationFoldout, "Animation", true, GuiStyleBoldFoldout);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool("VeryAnimation_Main_Animation", guiAnimationFoldout);
                    }
                    {
                        EditorGUILayout.Space();
                        VA.AnimationToolbarGUI();
                        EditorGUILayout.Space();
                        if (GUILayout.Button(UEditorGUI.GetHelpIcon(), guiAnimationHelp ? GuiStyleIconActiveButton : GuiStyleIconButton, GUILayout.Width(19)))
                        {
                            guiAnimationHelp = !guiAnimationHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (guiAnimationHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpAnimation), MessageType.Info);
                    }

                    VA.AnimationGUI();
                }
                #endregion

                #region Tools
                if (guiToolsFoldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    guiToolsFoldout = EditorGUILayout.Foldout(guiToolsFoldout, "Tools", true, GuiStyleBoldFoldout);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool("VeryAnimation_Main_Tools", guiToolsFoldout);
                    }
                    {
                        EditorGUILayout.Space();
                        if (GUILayout.Button(UEditorGUI.GetHelpIcon(), guiToolsHelp ? GuiStyleIconActiveButton : GuiStyleIconButton, GUILayout.Width(19)))
                        {
                            guiToolsHelp = !guiToolsHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (guiToolsHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpTools), MessageType.Info);
                    }

                    VA.ToolsGUI();
                }
                #endregion

                #region Settings
                if (guiSettingsFoldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    guiSettingsFoldout = EditorGUILayout.Foldout(guiSettingsFoldout, "Settings", true, GuiStyleBoldFoldout);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool("VeryAnimation_Main_Settings", guiSettingsFoldout);
                    }
                    {
                        EditorGUILayout.Space();
                        if (GUILayout.Button(UEditorGUI.GetHelpIcon(), guiSettingsHelp ? GuiStyleIconActiveButton : GuiStyleIconButton, GUILayout.Width(19)))
                        {
                            guiSettingsHelp = !guiSettingsHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    {
                        if (guiSettingsHelp)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpSettings), MessageType.Info);
                        }

                        EditorSettings.SettingsGUI();
                    }
                }
                #endregion

                #region Help
                if (guiHelpFoldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    guiHelpFoldout = EditorGUILayout.Foldout(guiHelpFoldout, "Help", true, GuiStyleBoldFoldout);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool("VeryAnimation_Main_Help", guiHelpFoldout);
                    }
                    {
                        EditorGUILayout.Space();
                        if (GUILayout.Button(UEditorGUI.GetHelpIcon(), guiHelpHelp ? GuiStyleIconActiveButton : GuiStyleIconButton, GUILayout.Width(19)))
                        {
                            guiHelpHelp = !guiHelpHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    {
                        if (guiHelpHelp)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpHelp), MessageType.Info);
                        }

                        EditorGUILayout.BeginVertical(GuiStyleSkinBox);
                        helpScrollPosition = EditorGUILayout.BeginScrollView(helpScrollPosition);
                        {
                            EditorGUILayout.LabelField("Version", Version);
                            EditorGUILayout.LabelField(Language.GetContent(Language.Help.HelpShortcuts));
                            EditorGUI.indentLevel++;
                            {
                                EditorGUILayout.LabelField("Esc", "[Editor] Exit edit");
                                EditorGUILayout.LabelField("O", "[Editor] Change Clamp");
                                EditorGUILayout.LabelField("J", "[Editor] Change Foot IK");
                                EditorGUILayout.LabelField("M", "[Editor] Change Mirror");
                                EditorGUILayout.LabelField("L", "[Editor] Change Root Correction Mode");
                                EditorGUILayout.LabelField("I", "[Editor] Change selection bone IK");
                                EditorGUILayout.LabelField("1", "[Editor] Pose/Quick Save 1");
                                EditorGUILayout.LabelField("3", "[Editor] Pose/Quick Load 1");
                                EditorGUILayout.LabelField("F5", "[AnimationWindow] Force refresh");
                                EditorGUILayout.LabelField("Page Down", "[AnimationWindow] Next animation clip");
                                EditorGUILayout.LabelField("Page Up", "[AnimationWindow] Previous animation clip");
                                EditorGUILayout.LabelField("Space", "[AnimationWindow] Change playing");
                                EditorGUILayout.LabelField("C", "[AnimationWindow] Switch between curves and dope sheet");
                                EditorGUILayout.LabelField("K", "[AnimationWindow] Add keyframe or [IK] Update IK");
                                EditorGUILayout.LabelField(",", "[AnimationWindow] Move to next frame");
                                EditorGUILayout.LabelField(".", "[AnimationWindow] Move to previous frame");
                                EditorGUILayout.LabelField("Alt + ,", "[AnimationWindow] Move to next keyframe");
                                EditorGUILayout.LabelField("Alt + .", "[AnimationWindow] Move to previous keyframe");
                                EditorGUILayout.LabelField("Shift + ,", "[AnimationWindow] Move to first keyframe");
                                EditorGUILayout.LabelField("Shift + .", "[AnimationWindow] Move to last keyframe");
                                EditorGUILayout.LabelField("Keypad Plus", "[AnimationWindow] Edit Keys/Add In between");
                                EditorGUILayout.LabelField("Keypad Minus", "[AnimationWindow] Edit Keys/Remove In between");
#if UNITY_EDITOR_OSX
                                EditorGUILayout.LabelField("H", "[Hierarchy] Hide select bones");
                                EditorGUILayout.LabelField("Shift + H", "[Hierarchy] Show select bones");
                                EditorGUILayout.LabelField("Alt + Space", "[Preview] Change playing");
                                EditorGUILayout.LabelField("Command + Keypad Plus", "[IK] Add IK - Level / Direction");
                                EditorGUILayout.LabelField("Command + Keypad Minus", "[IK] Sub IK - Level / Direction");
#else
                                EditorGUILayout.LabelField("H", "[Hierarchy] Hide select bones");
                                EditorGUILayout.LabelField("Shift + H", "[Hierarchy] Show select bones");
                                EditorGUILayout.LabelField("Ctrl + Space", "[Preview] Change playing");
                                EditorGUILayout.LabelField("Ctrl + Keypad Plus", "[IK] Add IK - Level / Direction");
                                EditorGUILayout.LabelField("Ctrl + Keypad Minus", "[IK] Sub IK - Level / Direction");
#endif
                            }
                            EditorGUI.indentLevel--;
                            EditorGUILayout.LabelField("Icons");
                            EditorGUI.indentLevel++;
                            {
                                static void IconGUI(string s, Texture2D t)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(s, GUILayout.Width(146));
                                    var rect = EditorGUILayout.GetControlRect();
                                    rect.width = rect.height;
                                    GUI.DrawTexture(rect, t);
                                    EditorGUILayout.EndHorizontal();
                                }
                                IconGUI("Humanoid / Normal", circleNormalTex);
                                IconGUI("Root", circle3NormalTex);
                                IconGUI("Non Humanoid", diamondNormalTex);
                                IconGUI("Humanoid Virtual", circleDotNormalTex);
                            }
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.EndVertical();
                    }
                }
                #endregion

                #region Preview
                if (guiPreviewFoldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    guiPreviewFoldout = EditorGUILayout.Foldout(guiPreviewFoldout, "Preview", true, GuiStyleBoldFoldout);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool("VeryAnimation_Main_Preview", guiPreviewFoldout);
                    }
                    {
                        EditorGUILayout.Space();
                        if (GUILayout.Button(UEditorGUI.GetHelpIcon(), guiPreviewHelp ? GuiStyleIconActiveButton : GuiStyleIconButton, GUILayout.Width(19)))
                        {
                            guiPreviewHelp = !guiPreviewHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    {
                        if (guiPreviewHelp)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpPreview), MessageType.Info);
                        }
                        else
                        {
                            GUILayout.Space(2f);
                        }

                        {
                            VA.PreviewGUI();
                        }
                    }
                }
                #endregion
                #endregion
            }
            else
            {
                #region Error
                EditorApplication.delayCall += () =>
                {
                    Release();
                };
                #endregion
            }

#if Enable_Profiler
            Profiler.EndSample();
#endif
        }
        private void VersionInfoGUI()
        {
            GUILayout.FlexibleSpace();
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Version " + VeryAnimationWindow.Version, GuiStyleMiddleLeftGreyMiniLabel, GUILayout.Width(100f));

                {
                    string packageText = "";
#if VERYANIMATION_TIMELINE || VERYANIMATION_ANIMATIONRIGGING
                    void AddPackageText(string text)
                    {
                        if (!string.IsNullOrEmpty(packageText))
                            packageText += ", ";
                        packageText += text;
                    }
#endif
#if VERYANIMATION_TIMELINE
                    AddPackageText("Timeline");
#endif
#if VERYANIMATION_ANIMATIONRIGGING
                    AddPackageText("Animation Rigging");
#endif
                    EditorGUILayout.LabelField(packageText, GuiStyleMiddleRightGreyMiniLabel);
                }
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("Tutorial Video Playlist", "YouTube"), EditorStyles.linkLabel))
                {
                    if (EditorUtility.DisplayDialog(Language.GetText(Language.Help.TutorialVideoPlaylistDialog),
                                                    Language.GetTooltip(Language.Help.TutorialVideoPlaylistDialog), "ok", "cancel"))
                    {
                        var uri = new System.Uri("https://youtube.com/playlist?list=PLk2zFKUFoq3kqPz9pGIKRrExhM9qdLMGI");
                        Application.OpenURL(uri.AbsoluteUri);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OnPreSceneGUI(SceneView sceneView)
        {
            if (VA.IsEditError || !GuiStyleReady) return;

            Event e = Event.current;

            #region AnimationWindowSampleAnimationOverride
            if (e.type == EventType.Layout)
            {
                VA.AnimationWindowSampleAnimationOverride(false);
            }
            #endregion

            if (sceneView != SceneView.lastActiveSceneView) return;

            if (sceneView == EditorWindow.focusedWindow)
            {
                VA.Commands();
            }
        }
        private void OnSceneGUI(SceneView sceneView)
        {
            if (VA.IsEditError || !GuiStyleReady) return;
            if (sceneView != SceneView.lastActiveSceneView) return;

#if Enable_Profiler
            Profiler.BeginSample(string.Format("****VeryAnimationWindow.OnSceneGUI {0}", Event.current));
#endif

            Handles.matrix = Matrix4x4.identity;
            Event e = Event.current;
            var showGizmo = IsShowSceneGizmo();
            bool repaintScene = false;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            #region Event
            switch (e.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlID);
                    break;
                case EventType.KeyDown:
                    if (focusedWindow is SceneView)
                        VA.HotKeys();
                    break;
                case EventType.KeyUp:
                    break;
                case EventType.MouseMove:
                    handleTransformUpdate = true;
                    selectionRect.Reset();
                    break;
                case EventType.MouseDown:
                    if (IsShowSceneGizmo())
                    {
                        if (e.clickCount == 1)
                        {
                            if (e.button == 0)
                            {
                                handleTransformUpdate = true;
                            }
                            if (!e.alt && e.button == 0)
                            {
                                selectionRect.Reset();
                                selectionRect.SetStart(e.mousePosition);
                                if (Shortcuts.IsKeyControl(e) || e.shift)
                                {
                                    selectionRect.beforeSelection = VA.SelectionGameObjects?.ToArray();
                                    selectionRect.virtualBeforeSelection = VA.SelectionHumanVirtualBones?.ToArray();
                                    selectionRect.beforeAnimatorIKSelection = VA.IsHuman ? VA.animatorIK.ikTargetSelect : null;
                                    selectionRect.beforeOriginalIKSelection = VA.originalIK.ikTargetSelect;
                                }
                            }
                        }
                        else if (e.clickCount == 2)
                        {
                            #region ChangeOtherObject
                            if (!EditorApplication.isPlaying && !VA.UAw.GetLinkedWithTimeline())
                            {
                                if (forceChangeObject == null)
                                {
                                    GameObject go = HandleUtility.PickGameObject(e.mousePosition, false);
                                    if (go != null && VA.BonesIndexOf(go) < 0)
                                    {
                                        #region Check
                                        bool enable = false;
                                        {
                                            var t = go.transform;
                                            while (t != null)
                                            {
                                                if (t.gameObject.activeInHierarchy)
                                                {
                                                    if (t.GetComponent<Animator>() != null || t.GetComponent<Animation>() != null)
                                                    {
                                                        if (AnimationUtility.GetAnimationClips(t.gameObject).Length > 0)
                                                        {
                                                            enable = true;
                                                            go = t.gameObject;
                                                            break;
                                                        }
                                                    }
                                                }
                                                t = t.parent;
                                            }
                                        }
                                        #endregion
                                        if (enable)
                                        {
                                            forceChangeObject = go;
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    SetRepaintGUI(RepaintGUI.Edit);
                    repaintScene = true;
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != 0)
                        handleTransformUpdate = false;
                    else
                        handleTransformUpdate = true;
                    if (selectionRect.Enable)
                    {
                        if (GUIUtility.hotControl == 0 && IsShowSceneGizmo())
                        {
                            selectionRect.SetEnd(e.mousePosition);
                            #region Selection
                            {
                                var rect = selectionRect.Rect;
                                #region Now
                                #region Bone
                                {
                                    selectionRect.calcList.Clear();
                                    for (int i = 0; i < VA.Bones.Length; i++)
                                    {
                                        if (!VA.IsShowBone(i) || (VA.IsHuman && i == 0)) continue;
                                        if (rect.Contains(HandleUtility.WorldToGUIPoint(VA.Skeleton.Bones[i].transform.position)))
                                        {
                                            selectionRect.calcList.Add(VA.Bones[i]);
                                        }
                                    }
                                }
                                #endregion
                                #region VirtualBone
                                {
                                    selectionRect.virtualCalcList.Clear();
                                    if (VA.IsHuman)
                                    {
                                        if (VA.IsShowBone(VA.RootMotionBoneIndex))
                                        {
                                            if (rect.Contains(HandleUtility.WorldToGUIPoint(VA.HumanWorldRootPositionCache)))
                                            {
                                                selectionRect.calcList.Add(GameObject);
                                            }
                                        }
                                        for (int i = 0; i < VeryAnimation.HumanVirtualBones.Length; i++)
                                        {
                                            if (!VA.IsShowVirtualBone((HumanBodyBones)i)) continue;

                                            if (rect.Contains(HandleUtility.WorldToGUIPoint(VA.GetHumanVirtualBonePosition((HumanBodyBones)i))))
                                            {
                                                selectionRect.virtualCalcList.Add((HumanBodyBones)i);
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region AnimatorIK
                                {
                                    selectionRect.animatorIKCalcList.Clear();
                                    if (VA.IsHuman && selectionRect.calcList.Count == 0 && selectionRect.virtualCalcList.Count == 0)
                                    {
                                        for (int i = 0; i < VA.animatorIK.ikData.Length; i++)
                                        {
                                            var data = VA.animatorIK.ikData[i];
                                            if (!data.enable) continue;
                                            var guiPoint = HandleUtility.WorldToGUIPoint(data.WorldPosition);
                                            if (!selectionRect.Rect.Contains(guiPoint)) continue;
                                            selectionRect.animatorIKCalcList.Add((AnimatorIKCore.IKTarget)i);
                                        }
                                    }
                                }
                                #endregion
                                #region OriginalIK
                                {
                                    selectionRect.originalIKCalcList.Clear();
                                    if (selectionRect.calcList.Count == 0 && selectionRect.virtualCalcList.Count == 0 && selectionRect.animatorIKCalcList.Count == 0)
                                    {
                                        for (int i = 0; i < VA.originalIK.ikData.Count; i++)
                                        {
                                            var data = VA.originalIK.ikData[i];
                                            if (!data.enable) continue;
                                            var guiPoint = HandleUtility.WorldToGUIPoint(data.WorldPosition);
                                            if (!selectionRect.Rect.Contains(guiPoint)) continue;
                                            selectionRect.originalIKCalcList.Add(i);
                                        }
                                    }
                                }
                                #endregion
                                #endregion
                                #region Before
                                #region Bone
                                if ((Shortcuts.IsKeyControl(e) || e.shift) && selectionRect.beforeSelection != null)
                                {
                                    if (e.shift)
                                    {
                                        foreach (var go in selectionRect.beforeSelection)
                                        {
                                            if (go == null) continue;
                                            if (!selectionRect.calcList.Contains(go))
                                                selectionRect.calcList.Add(go);
                                        }
                                    }
                                    else if (Shortcuts.IsKeyControl(e))
                                    {
                                        foreach (var go in selectionRect.beforeSelection)
                                        {
                                            if (go == null) continue;
                                            Vector3 pos;
                                            if (VA.IsHuman && go == GameObject)
                                            {
                                                pos = VA.HumanWorldRootPositionCache;
                                            }
                                            else
                                            {
                                                var boneIndex = VA.BonesIndexOf(go);
                                                if (boneIndex >= 0)
                                                    pos = VA.Skeleton.Bones[boneIndex].transform.position;
                                                else
                                                    pos = go.transform.position;
                                            }
                                            if (!rect.Contains(HandleUtility.WorldToGUIPoint(pos)))
                                            {
                                                if (!selectionRect.calcList.Contains(go))
                                                    selectionRect.calcList.Add(go);
                                            }
                                            else
                                            {
                                                selectionRect.calcList.Remove(go);
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region VirtualBone
                                if (VA.IsHuman)
                                {
                                    if ((Shortcuts.IsKeyControl(e) || e.shift) && selectionRect.virtualBeforeSelection != null)
                                    {
                                        if (e.shift)
                                        {
                                            foreach (var go in selectionRect.virtualBeforeSelection)
                                            {
                                                if (!selectionRect.virtualCalcList.Contains(go))
                                                    selectionRect.virtualCalcList.Add(go);
                                            }
                                        }
                                        else if (Shortcuts.IsKeyControl(e))
                                        {
                                            foreach (var go in selectionRect.virtualBeforeSelection)
                                            {
                                                if (!rect.Contains(HandleUtility.WorldToGUIPoint(VA.GetHumanVirtualBonePosition(go))))
                                                {
                                                    if (!selectionRect.virtualCalcList.Contains(go))
                                                        selectionRect.virtualCalcList.Add(go);
                                                }
                                                else
                                                {
                                                    selectionRect.virtualCalcList.Remove(go);
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region AnimatorIK
                                if (VA.IsHuman)
                                {
                                    if ((Shortcuts.IsKeyControl(e) || e.shift) && selectionRect.beforeAnimatorIKSelection != null)
                                    {
                                        if (e.shift)
                                        {
                                            foreach (var target in selectionRect.beforeAnimatorIKSelection)
                                            {
                                                if (!selectionRect.animatorIKCalcList.Contains(target))
                                                    selectionRect.animatorIKCalcList.Add(target);
                                            }
                                        }
                                        else if (Shortcuts.IsKeyControl(e))
                                        {
                                            foreach (var target in selectionRect.beforeAnimatorIKSelection)
                                            {
                                                Vector3 pos = VA.animatorIK.ikData[(int)target].WorldPosition;
                                                if (!rect.Contains(HandleUtility.WorldToGUIPoint(pos)))
                                                {
                                                    if (!selectionRect.animatorIKCalcList.Contains(target))
                                                        selectionRect.animatorIKCalcList.Add(target);
                                                }
                                                else
                                                {
                                                    selectionRect.animatorIKCalcList.Remove(target);
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region OriginalIK
                                {
                                    if ((Shortcuts.IsKeyControl(e) || e.shift) && selectionRect.beforeOriginalIKSelection != null)
                                    {
                                        if (e.shift)
                                        {
                                            foreach (var target in selectionRect.beforeOriginalIKSelection)
                                            {
                                                if (!selectionRect.originalIKCalcList.Contains(target))
                                                    selectionRect.originalIKCalcList.Add(target);
                                            }
                                        }
                                        else if (Shortcuts.IsKeyControl(e))
                                        {
                                            foreach (var target in selectionRect.beforeOriginalIKSelection)
                                            {
                                                Vector3 pos = VA.originalIK.ikData[target].WorldPosition;
                                                if (!rect.Contains(HandleUtility.WorldToGUIPoint(pos)))
                                                {
                                                    if (!selectionRect.originalIKCalcList.Contains(target))
                                                        selectionRect.originalIKCalcList.Add(target);
                                                }
                                                else
                                                {
                                                    selectionRect.originalIKCalcList.Remove(target);
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #endregion
                                {
                                    bool selectionChange = false;
                                    #region IsChanged
                                    {
                                        #region Bone
                                        {
                                            if (VA.SelectionGameObjects == null || VA.SelectionGameObjects.Count != selectionRect.calcList.Count)
                                                selectionChange = true;
                                            else if (VA.SelectionGameObjects != null)
                                            {
                                                for (int i = 0; i < VA.SelectionGameObjects.Count; i++)
                                                {
                                                    if (VA.SelectionGameObjects[i] != selectionRect.calcList[i])
                                                    {
                                                        selectionChange = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region VirtualBone
                                        if (VA.IsHuman)
                                        {
                                            if (VA.SelectionHumanVirtualBones == null || VA.SelectionHumanVirtualBones.Count != selectionRect.virtualCalcList.Count)
                                                selectionChange = true;
                                            else if (VA.SelectionHumanVirtualBones != null)
                                            {
                                                for (int i = 0; i < VA.SelectionHumanVirtualBones.Count; i++)
                                                {
                                                    if (VA.SelectionHumanVirtualBones[i] != selectionRect.virtualCalcList[i])
                                                    {
                                                        selectionChange = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region AnimatorIK
                                        if (VA.IsHuman)
                                        {
                                            if (VA.animatorIK.ikTargetSelect == null || VA.animatorIK.ikTargetSelect.Length != selectionRect.animatorIKCalcList.Count)
                                                selectionChange = true;
                                            else if (VA.animatorIK.ikTargetSelect != null)
                                            {
                                                for (int i = 0; i < VA.animatorIK.ikTargetSelect.Length; i++)
                                                {
                                                    if (VA.animatorIK.ikTargetSelect[i] != selectionRect.animatorIKCalcList[i])
                                                    {
                                                        selectionChange = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region OriginalIK
                                        {
                                            if (VA.originalIK.ikTargetSelect == null || VA.originalIK.ikTargetSelect.Length != selectionRect.originalIKCalcList.Count)
                                                selectionChange = true;
                                            else if (VA.originalIK.ikTargetSelect != null)
                                            {
                                                for (int i = 0; i < VA.originalIK.ikTargetSelect.Length; i++)
                                                {
                                                    if (VA.originalIK.ikTargetSelect[i] != selectionRect.originalIKCalcList[i])
                                                    {
                                                        selectionChange = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                    #endregion
                                    if (selectionChange)
                                    {
                                        VA.SelectGameObjectMouseDrag(selectionRect.calcList.ToArray(), selectionRect.virtualCalcList.ToArray(), selectionRect.animatorIKCalcList.ToArray(), selectionRect.originalIKCalcList.ToArray());
                                        VeryAnimationControlWindow.ForceSelectionChange();
                                    }
                                }
                            }
                            #endregion
                            e.Use();
                        }
                        else
                        {
                            selectionRect.Reset();
                        }
                    }
                    if (e.button == 0 && GUIUtility.hotControl != 0)
                        SetRepaintGUI(RepaintGUI.Edit);
                    repaintScene = true;
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl < 0)
                        GUIUtility.hotControl = 0;
                    else if (GUIUtility.hotControl == 0 && selectionRect.Enable && selectionRect.Distance < 10f)
                    {
                        #region SelectMesh
                        {
                            GameObject go = null;
                            var animatorIKTarget = AnimatorIKCore.IKTarget.None;
                            int originalIKTarget = -1;
                            {
                                var worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                                var lengthSqMin = float.MaxValue;
                                var vertices = new List<Vector3>();
                                var triangles = new List<int>();
                                foreach (var renderer in VA.Renderers)
                                {
                                    if (renderer == null || !renderer.gameObject.activeInHierarchy || !renderer.enabled)
                                        continue;
                                    if (renderer is SkinnedMeshRenderer)
                                    {
                                        #region SkinnedMeshRenderer
                                        var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                                        if (skinnedMeshRenderer.sharedMesh != null)
                                        {
                                            var worldToLocalMatrix = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one).inverse;
                                            var localRay = new Ray(worldToLocalMatrix.MultiplyPoint3x4(worldRay.origin), worldToLocalMatrix.MultiplyVector(worldRay.direction));
                                            Mesh mesh = new();
                                            mesh.hideFlags |= HideFlags.HideAndDontSave;
                                            skinnedMeshRenderer.BakeMesh(mesh);
                                            mesh.GetVertices(vertices);
                                            BoneWeight[] boneWeights = null;
                                            Transform[] boneTransforms = null;
                                            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                                            {
                                                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                                                    continue;
                                                mesh.GetTriangles(triangles, subMesh);
                                                for (int i = 0; i < triangles.Count; i += 3)
                                                {
                                                    if (!EditorCommon.Ray_Triangle(localRay,
                                                                                    vertices[triangles[i + 0]],
                                                                                    vertices[triangles[i + 1]],
                                                                                    vertices[triangles[i + 2]],
                                                                                    out Vector3 posP)) continue;
                                                    var lengthSq = (posP - localRay.origin).sqrMagnitude;
                                                    if (lengthSq > lengthSqMin)
                                                        continue;

                                                    boneWeights ??= skinnedMeshRenderer.sharedMesh.boneWeights;
                                                    boneTransforms ??= skinnedMeshRenderer.bones;

                                                    Transform bone = null;
                                                    {
                                                        Dictionary<int, float> bonePoints = new();
                                                        void AddBonePoint(int boneIndex, float boneWeight)
                                                        {
                                                            if (boneWeight <= 0f || boneIndex < 0 || boneIndex >= boneTransforms.Length)
                                                                return;
                                                            var t = boneTransforms[boneIndex];
                                                            var point = Vector2.Distance(HandleUtility.WorldToGUIPoint(t.position), e.mousePosition);
                                                            point += (point * (1f - boneWeight));
                                                            if (!bonePoints.ContainsKey(boneIndex))
                                                                bonePoints.Add(boneIndex, point);
                                                            else
                                                                bonePoints[boneIndex] = Mathf.Min(bonePoints[boneIndex], point);
                                                        }
                                                        for (int v = 0; v < 3; v++)
                                                        {
                                                            var index = triangles[i + v];
                                                            if (index >= boneWeights.Length) continue;
                                                            AddBonePoint(boneWeights[index].boneIndex0, boneWeights[index].weight0);
                                                            AddBonePoint(boneWeights[index].boneIndex1, boneWeights[index].weight1);
                                                            AddBonePoint(boneWeights[index].boneIndex2, boneWeights[index].weight2);
                                                            AddBonePoint(boneWeights[index].boneIndex3, boneWeights[index].weight3);
                                                        }
                                                        foreach (var pair in bonePoints.OrderBy((x) => x.Value))
                                                        {
                                                            bone = boneTransforms[pair.Key];
                                                            break;
                                                        }
                                                    }
                                                    if (bone != null)
                                                    {
                                                        int boneIndex = VA.BonesIndexOf(bone.gameObject);
                                                        var animatorIKTargetSub = AnimatorIKCore.IKTarget.None;
                                                        int originalIKTargetSub = -1;
                                                        while (boneIndex >= 0 && !VA.IsShowBone(boneIndex))
                                                        {
                                                            #region IKTarget
                                                            if (VA.IsHuman)
                                                            {
                                                                var target = VA.animatorIK.IsIKBone(VA.BoneIndex2humanoidIndex[boneIndex]);
                                                                if (target != AnimatorIKCore.IKTarget.None)
                                                                {
                                                                    animatorIKTargetSub = target;
                                                                    originalIKTargetSub = -1;
                                                                    break;
                                                                }
                                                            }
                                                            {
                                                                var target = VA.originalIK.IsIKBone(boneIndex);
                                                                if (target >= 0)
                                                                {
                                                                    animatorIKTargetSub = AnimatorIKCore.IKTarget.None;
                                                                    originalIKTargetSub = target;
                                                                    break;
                                                                }
                                                            }
                                                            #endregion
                                                            boneIndex = VA.ParentBoneIndexes[boneIndex];
                                                            if (boneIndex == VA.RootMotionBoneIndex)
                                                            {
                                                                if (!VA.IsShowBone(boneIndex))
                                                                    boneIndex = -1;
                                                                break;
                                                            }
                                                        }
                                                        if (boneIndex >= 0)
                                                        {
                                                            lengthSqMin = lengthSq;
                                                            go = VA.Bones[boneIndex];
                                                            animatorIKTarget = animatorIKTargetSub;
                                                            originalIKTarget = originalIKTargetSub;
                                                        }
                                                    }
                                                }
                                            }
                                            Mesh.DestroyImmediate(mesh);
                                        }
                                        #endregion
                                    }
                                    else if (renderer is MeshRenderer)
                                    {
                                        #region MeshRenderer
                                        var worldToLocalMatrix = renderer.transform.worldToLocalMatrix;
                                        var localRay = new Ray(worldToLocalMatrix.MultiplyPoint3x4(worldRay.origin), worldToLocalMatrix.MultiplyVector(worldRay.direction));
                                        var meshFilter = renderer.GetComponent<MeshFilter>();
                                        if (meshFilter != null && meshFilter.sharedMesh != null)
                                        {
                                            var mesh = meshFilter.sharedMesh;
                                            mesh.GetVertices(vertices);
                                            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                                            {
                                                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                                                    continue;
                                                mesh.GetTriangles(triangles, subMesh);
                                                for (int i = 0; i < triangles.Count; i += 3)
                                                {
                                                    if (!EditorCommon.Ray_Triangle(localRay,
                                                                                    vertices[triangles[i + 0]],
                                                                                    vertices[triangles[i + 1]],
                                                                                    vertices[triangles[i + 2]],
                                                                                    out Vector3 posP)) continue;
                                                    posP = renderer.transform.localToWorldMatrix.MultiplyPoint3x4(posP);
                                                    var lengthSq = (posP - worldRay.origin).sqrMagnitude;
                                                    if (lengthSq > lengthSqMin)
                                                        continue;

                                                    var bone = renderer.transform;
                                                    {
                                                        int boneIndex = VA.BonesIndexOf(bone.gameObject);
                                                        var animatorIKTargetSub = AnimatorIKCore.IKTarget.None;
                                                        int originalIKTargetSub = -1;
                                                        while (boneIndex >= 0 && !VA.IsShowBone(boneIndex))
                                                        {
                                                            #region IKTarget
                                                            if (VA.IsHuman)
                                                            {
                                                                var target = VA.animatorIK.IsIKBone(VA.BoneIndex2humanoidIndex[boneIndex]);
                                                                if (target != AnimatorIKCore.IKTarget.None)
                                                                {
                                                                    animatorIKTargetSub = target;
                                                                    originalIKTargetSub = -1;
                                                                    break;
                                                                }
                                                            }
                                                            {
                                                                var target = VA.originalIK.IsIKBone(boneIndex);
                                                                if (target >= 0)
                                                                {
                                                                    animatorIKTargetSub = AnimatorIKCore.IKTarget.None;
                                                                    originalIKTargetSub = target;
                                                                    break;
                                                                }
                                                            }
                                                            #endregion
                                                            boneIndex = VA.ParentBoneIndexes[boneIndex];
                                                            if (boneIndex <= VA.RootMotionBoneIndex)
                                                            {
                                                                if (!VA.IsShowBone(boneIndex))
                                                                    boneIndex = -1;
                                                                break;
                                                            }
                                                        }
                                                        if (boneIndex >= 0)
                                                        {
                                                            lengthSqMin = lengthSq;
                                                            go = VA.Bones[boneIndex];
                                                            animatorIKTarget = animatorIKTargetSub;
                                                            originalIKTarget = originalIKTargetSub;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                }
                            }
                            if (animatorIKTarget != AnimatorIKCore.IKTarget.None)
                            {
                                VA.SelectAnimatorIKTargetPlusKey(animatorIKTarget);
                            }
                            else if (originalIKTarget >= 0)
                            {
                                VA.SelectOriginalIKTargetPlusKey(originalIKTarget);
                            }
                            else
                            {
                                VA.SelectGameObjectPlusKey(go);
                            }
                        }
                        #endregion
                    }
                    if (e.button == 0)
                    {
                        handleTransformUpdate = true;
                    }
                    selectionRect.Reset();
                    SetRepaintGUI(RepaintGUI.Edit);
                    repaintScene = true;
                    break;
            }
            #endregion

            #region RootTrail
            if (VA.extraOptionsRootTrail && showGizmo)
            {
                if (e.type == EventType.Repaint)
                {
                    DrawRootTrail();
                }
            }
            #endregion

            #region SelectionRect
            if (selectionRect.Enable && selectionRect.Rect.width > 0f && selectionRect.Rect.height > 0f)
            {
                Handles.BeginGUI();
                GUI.Box(selectionRect.Rect, "", "SelectionRect");
                Handles.EndGUI();
            }
            #endregion

            #region Tools
            if (showGizmo)
            {
                #region Handle
                {
                    bool genericHandle = false;
                    if (VA.IsHuman)
                    {
                        #region Humanoid
                        var humanoidIndex = VA.SelectionGameObjectHumanoidIndex();
                        if (VA.SelectionActiveBone == VA.RootMotionBoneIndex)
                        {
                            #region Root
                            if (handleTransformUpdate)
                            {
                                handlePosition = VA.HumanWorldRootPositionCache;
                                handleRotation = Tools.pivotRotation == PivotRotation.Local ? VA.HumanWorldRootRotationCache : Tools.handleRotation;
                                handlePositionSave = handlePosition;
                                handleRotationSave = handleRotation;
                            }
                            VA.EnableCustomTools(Tool.None);
                            var currentTool = VA.CurrentTool();
                            if (currentTool == Tool.Move)
                            {
                                EditorGUI.BeginChangeCheck();
                                var position = Handles.PositionHandle(handlePosition, handleRotation);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    VA.SetAnimationValueAnimatorRootT(VA.GetHumanLocalRootPosition(position));
                                    handlePosition = position;
                                }
                            }
                            else if (currentTool == Tool.Rotate)
                            {
                                EditorGUI.BeginChangeCheck();
                                var rotation = Handles.RotationHandle(handleRotation, handlePosition);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (Tools.pivotRotation == PivotRotation.Local)
                                    {
                                        VA.SetAnimationValueAnimatorRootQ(VA.GetHumanLocalRootRotation(rotation));
                                    }
                                    else
                                    {
                                        (Quaternion.Inverse(handleRotation) * rotation).ToAngleAxis(out float angle, out Vector3 axis);
                                        var bodyRotation = VA.HumanWorldRootRotationCache;
                                        bodyRotation = bodyRotation * Quaternion.Inverse(bodyRotation) * Quaternion.AngleAxis(angle, handleRotation * axis) * bodyRotation;
                                        VA.SetAnimationValueAnimatorRootQ(VA.GetHumanLocalRootRotation(bodyRotation));
                                    }
                                    Tools.handleRotation = handleRotation = rotation;
                                }
                            }
                            #endregion
                        }
                        else if (humanoidIndex == HumanBodyBones.Hips)
                        {
                            VA.EnableCustomTools(Tool.None);
                        }
                        else if (humanoidIndex > HumanBodyBones.Hips)
                        {
                            #region Muscle
                            VA.EnableCustomTools(Tool.None);
                            var currentTool = VA.CurrentTool();
                            #region handleTransformUpdate
                            if (handleTransformUpdate)
                            {
                                if (VA.Skeleton.HumanoidBones[(int)humanoidIndex] != null)
                                {
                                    handlePosition = VA.Skeleton.HumanoidBones[(int)humanoidIndex].transform.position;
                                    if (Tools.pivotRotation == PivotRotation.Local)
                                    {
                                        handleRotation = VA.Skeleton.HumanoidBones[(int)humanoidIndex].transform.rotation;
                                    }
                                    else
                                    {
                                        handleRotation = Tools.handleRotation;
                                    }
                                    if (Tools.pivotMode == PivotMode.Center)
                                    {
                                        handlePosition = VA.GetSelectionBounds().center;
                                        if (Tools.pivotRotation == PivotRotation.Local)
                                        {
                                            handleRotation = VA.GetSelectionBoundsRotation();
                                        }
                                    }
                                }
                                else
                                {
                                    handlePosition = VA.GetHumanVirtualBonePosition(humanoidIndex);
                                    handleRotation = Tools.pivotRotation == PivotRotation.Local ? VA.GetHumanVirtualBoneRotation(humanoidIndex) : Tools.handleRotation;
                                }
                                handlePositionSave = handlePosition;
                                handleRotationSave = handleRotation;
                            }
                            #endregion
                            #region CenterLine
                            if (Tools.pivotMode == PivotMode.Center && (currentTool == Tool.Move || currentTool == Tool.Rotate))
                            {
                                var saveColor = Handles.color;
                                Handles.color = EditorSettings.SettingBoneActiveColor;
                                Vector3 pos2;
                                if (VA.Skeleton.HumanoidBones[(int)humanoidIndex] != null)
                                    pos2 = VA.Skeleton.HumanoidBones[(int)humanoidIndex].transform.position;
                                else
                                    pos2 = VA.GetHumanVirtualBonePosition(humanoidIndex);
                                Handles.DrawLine(handlePositionSave, pos2);
                                Handles.color = saveColor;
                            }
                            #endregion
                            void SetCenterRotationAction(Action<HumanBodyBones, Quaternion> action)
                            {
                                var normal = handleRotationSave * Vector3.forward;
                                var vecBase = handleRotationSave * Vector3.right;
                                var hiList = VA.SelectionGameObjectsHumanoidIndex();
                                if (hiList.Count > 1)
                                {
                                    foreach (var hi in hiList)
                                    {
                                        var boneIndex = VA.HumanoidIndex2boneIndex[(int)hi];
                                        if (boneIndex < 0) continue;
                                        Quaternion rotation = Quaternion.identity;
                                        {
                                            var vecSub = VA.Skeleton.Bones[boneIndex].transform.position - handlePositionSave;
                                            vecSub.Normalize();
                                            if (vecSub.sqrMagnitude > 0f)
                                            {
                                                rotation = Quaternion.AngleAxis(Vector3.SignedAngle(vecBase, vecSub, normal), normal);
                                            }
                                        }
                                        action(hi, rotation);
                                    }
                                }
                                else if (hiList.Count == 1)
                                {
                                    var boneIndex = VA.HumanoidIndex2boneIndex[(int)hiList[0]];
                                    if (boneIndex >= 0)
                                    {
                                        action(hiList[0], Quaternion.identity);
                                    }
                                }
                            }
                            if (currentTool == Tool.Move)
                            {
                                #region Move
                                EditorGUI.BeginChangeCheck();
                                var position = Handles.PositionHandle(handlePosition, handleRotation);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    void ChangeTDOF(HumanBodyBones hi, Vector3 move)
                                    {
                                        if (VA.Skeleton.HumanoidBones[(int)hi] == null)
                                            return;
                                        if (VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)hi] == null)
                                            return;
                                        Quaternion rotation;
                                        {
                                            var parentHi = VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)hi].parent;
                                            if (VA.Skeleton.HumanoidBones[(int)parentHi] == null)
                                                return;
                                            rotation = VA.Skeleton.HumanoidBones[(int)parentHi].transform.rotation * VA.GetHumanoidAvatarPostRotation(parentHi);
                                        }
                                        var localAdd = Quaternion.Inverse(rotation) * move;
                                        for (int i = 0; i < 3; i++) //Delete tiny value
                                        {
                                            if (Mathf.Abs(localAdd[i]) < 0.0001f)
                                                localAdd[i] = 0f;
                                        }
                                        {
                                            var mat = (VA.Skeleton.GameObject.transform.worldToLocalMatrix * VA.Skeleton.HumanoidBones[(int)humanoidIndex].transform.localToWorldMatrix).inverse;
                                            EditorCommon.GetTRS(mat, out Vector3 lposition, out Quaternion lrotation, out Vector3 lscale);
                                            localAdd = Vector3.Scale(localAdd, lscale);
                                        }
                                        if (VA.Skeleton.Animator.humanScale > 0f)
                                            localAdd *= 1f / VA.Skeleton.Animator.humanScale;
                                        else
                                            localAdd = Vector3.zero;
                                        VA.SetAnimationValueAnimatorTDOF(VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)hi].index, VA.GetAnimationValueAnimatorTDOF(VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)hi].index) + localAdd);
                                    }
                                    var offset = position - handlePosition;
                                    offset *= VA.GetSelectionSuppressPowerRate();
                                    if (Tools.pivotMode == PivotMode.Center)
                                    {
                                        #region Center
                                        SetCenterRotationAction((hi, different) =>
                                        {
                                            ChangeTDOF(hi, different * offset);
                                        });
                                        #endregion
                                    }
                                    else
                                    {
                                        #region Pivot
                                        foreach (var hi in VA.SelectionGameObjectsHumanoidIndex())
                                        {
                                            if (VA.Skeleton.HumanoidBones[(int)hi] == null)
                                                continue;
                                            ChangeTDOF(hi, offset);
                                        }
                                        #endregion
                                    }
                                    handlePosition = position;
                                }
                                #endregion
                            }
                            else if (currentTool == Tool.Rotate)
                            {
                                #region Rotate
                                void CalcRotation(Quaternion afterRot)
                                {
                                    HumanPose hpAfter = new();
                                    {
                                        var beforePose = new TransformPoseSave(VA.Skeleton.GameObject);
                                        (Quaternion.Inverse(handleRotation) * afterRot).ToAngleAxis(out float angle, out Vector3 axis);
                                        angle *= VA.GetSelectionSuppressPowerRate();
                                        if (Tools.pivotMode == PivotMode.Center)
                                        {
                                            #region Center
                                            SetCenterRotationAction((hi, different) =>
                                            {
                                                var handleRotationSub = different * handleRotation;
                                                VA.Skeleton.HumanoidBones[(int)hi].transform.Rotate(handleRotationSub * axis, angle, Space.World);
                                            });
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Pivot
                                            foreach (var hi in VA.SelectionGameObjectsHumanoidIndex())
                                            {
                                                if (VA.Skeleton.HumanoidBones[(int)hi] == null)
                                                    continue;
                                                VA.Skeleton.HumanoidBones[(int)hi].transform.Rotate(handleRotation * axis, angle, Space.World);
                                            }
                                            #endregion
                                        }
                                        VA.GetSkeletonHumanPose(ref hpAfter);
                                        beforePose.ResetDefaultTransform();
                                    }

                                    if (VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Neck] == null)
                                    {
                                        if (VA.IsSelectionGameObjectsHumanoidIndexContains(HumanBodyBones.Head))
                                        {
                                            for (int dof = 0; dof < 3; dof++)
                                            {
                                                var muscleIndex = HumanTrait.MuscleFromBone((int)HumanBodyBones.Neck, dof);
                                                if (muscleIndex < 0) continue;
                                                VA.SetAnimationValueAnimatorMuscle(muscleIndex, 0f);
                                            }
                                        }
                                    }
                                    foreach (var muscleIndex in VA.SelectionGameObjectsMuscleIndex(-1))
                                    {
                                        var hi = (HumanBodyBones)HumanTrait.BoneFromMuscle(muscleIndex);
                                        if (VA.Skeleton.HumanoidBones[(int)hi] == null)
                                            continue;
                                        var muscle = hpAfter.muscles[muscleIndex];
                                        if (VA.optionsClampMuscle)
                                            muscle = Mathf.Clamp(muscle, -1f, 1f);
                                        VA.SetAnimationValueAnimatorMuscle(muscleIndex, muscle);
                                    }
                                }
                                {
                                    if (muscleRotationHandleIds == null || muscleRotationHandleIds.Length != 3)
                                        muscleRotationHandleIds = new int[3];
                                    for (int i = 0; i < muscleRotationHandleIds.Length; i++)
                                        muscleRotationHandleIds[i] = -1;
                                }
                                if (Tools.pivotRotation == PivotRotation.Local && Tools.pivotMode == PivotMode.Pivot)
                                {
                                    #region LocalPivot
                                    Color saveColor = Handles.color;
                                    float handleSize = HandleUtility.GetHandleSize(handlePosition);
                                    {
                                        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                                        EditorGUI.BeginChangeCheck();
                                        var rotation = Handles.FreeRotateHandle(handleRotation, handlePosition, handleSize);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            CalcRotation(rotation);
                                            Tools.handleRotation = handleRotation = rotation;
                                        }
                                    }
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        int rotDofMode = -1;
                                        float rotDofDist = 0f;
                                        Quaternion rotDofHandleRotation = Quaternion.identity;
                                        Quaternion rotDofAfterRotation = Quaternion.identity;
                                        #region MuscleRotationHandle
                                        Transform t = null;
                                        if (VA.SelectionActiveBone >= 0)
                                            t = VA.Skeleton.Bones[VA.SelectionActiveBone].transform;
                                        Quaternion preRotation = VA.GetHumanoidAvatarPreRotation(humanoidIndex);
                                        var snapRotation = EditorSnapSettings.rotate;
                                        {
                                            if (HumanTrait.MuscleFromBone((int)humanoidIndex, 0) >= 0)
                                            {
                                                Handles.color = Handles.xAxisColor;
                                                EditorGUI.BeginChangeCheck();
                                                Quaternion hRotation;
                                                if (t != null)
                                                    hRotation = VA.UAvatar.GetZYPostQ(VA.Skeleton.Animator.avatar, (int)humanoidIndex, t.parent.rotation, t.rotation);
                                                else
                                                    hRotation = VA.GetHumanVirtualBoneRotation(humanoidIndex);
                                                var rotDofDistSave = UDisc.GetRotationDist();
                                                var rotation = Handles.Disc(hRotation, handlePosition, hRotation * Vector3.right, handleSize, true, snapRotation);
                                                muscleRotationHandleIds[0] = UEditorGUIUtility.GetLastControlID();
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    rotDofMode = 0;
                                                    rotDofDist = UDisc.GetRotationDist() - rotDofDistSave;
                                                    rotDofHandleRotation = hRotation;
                                                    rotDofAfterRotation = rotation;
                                                }
                                            }
                                            if (HumanTrait.MuscleFromBone((int)humanoidIndex, 1) >= 0)
                                            {
                                                Handles.color = Handles.yAxisColor;
                                                EditorGUI.BeginChangeCheck();
                                                Quaternion hRotation;
                                                if (t != null)
                                                    hRotation = t.parent.rotation * preRotation;
                                                else
                                                    hRotation = VA.GetHumanVirtualBoneParentRotation(humanoidIndex);
                                                var rotDofDistSave = UDisc.GetRotationDist();
                                                var rotation = Handles.Disc(hRotation, handlePosition, hRotation * Vector3.up, handleSize, true, snapRotation);
                                                muscleRotationHandleIds[1] = UEditorGUIUtility.GetLastControlID();
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    rotDofMode = 1;
                                                    rotDofDist = UDisc.GetRotationDist() - rotDofDistSave;
                                                    rotDofHandleRotation = hRotation;
                                                    rotDofAfterRotation = rotation;
                                                }
                                            }
                                            if (HumanTrait.MuscleFromBone((int)humanoidIndex, 2) >= 0)
                                            {
                                                Handles.color = Handles.zAxisColor;
                                                EditorGUI.BeginChangeCheck();
                                                Quaternion hRotation;
                                                if (t != null)
                                                    hRotation = t.parent.rotation * preRotation;
                                                else
                                                    hRotation = VA.GetHumanVirtualBoneParentRotation(humanoidIndex);
                                                var rotDofDistSave = UDisc.GetRotationDist();
                                                var rotation = Handles.Disc(hRotation, handlePosition, hRotation * Vector3.forward, handleSize, true, snapRotation);
                                                muscleRotationHandleIds[2] = UEditorGUIUtility.GetLastControlID();
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    rotDofMode = 2;
                                                    rotDofDist = UDisc.GetRotationDist() - rotDofDistSave;
                                                    rotDofHandleRotation = hRotation;
                                                    rotDofAfterRotation = rotation;
                                                }
                                            }
                                        }
                                        #endregion
                                        rotDofDist *= VA.GetSelectionSuppressPowerRate();
                                        if (rotDofMode >= 0 && rotDofMode <= 2)
                                        {
                                            foreach (var hi in VA.SelectionGameObjectsHumanoidIndex())
                                            {
                                                var muscleIndex = HumanTrait.MuscleFromBone((int)hi, rotDofMode);
                                                var muscle = VA.GetAnimationValueAnimatorMuscle(muscleIndex);
                                                {
                                                    var muscleLimit = VA.HumanoidMuscleLimit[(int)hi];
                                                    var value = muscleLimit.max[rotDofMode] - muscleLimit.min[rotDofMode];
                                                    if (value > 0f)
                                                    {
                                                        var add = rotDofDist / (value / 2f);
                                                        Vector3 limitSign;
                                                        if (VA.Skeleton.HumanoidBones[(int)hi] != null)
                                                            limitSign = VA.GetHumanoidAvatarLimitSign(hi);
                                                        else
                                                            limitSign = VA.GetHumanVirtualBoneLimitSign(hi);
                                                        muscle -= add * limitSign[rotDofMode];
                                                    }
                                                }
                                                if (VA.optionsClampMuscle)
                                                    muscle = Mathf.Clamp(muscle, -1f, 1f);
                                                VA.SetAnimationValueAnimatorMuscle(muscleIndex, muscle);
                                            }
                                        }
                                    }
                                    if (VA.Skeleton.HumanoidBones[(int)humanoidIndex] != null)
                                    {
                                        Handles.color = Handles.centerColor;
                                        EditorGUI.BeginChangeCheck();
                                        var rotation = Handles.Disc(handleRotation, handlePosition, Camera.current.transform.forward, handleSize * 1.1f, false, 0f);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            CalcRotation(rotation);
                                            Tools.handleRotation = handleRotation = rotation;
                                        }
                                    }
                                    Handles.color = saveColor;
                                    #endregion
                                }
                                else
                                {
                                    #region Other
                                    if (VA.Skeleton.HumanoidBones[(int)humanoidIndex] != null)
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var rotation = Handles.RotationHandle(handleRotation, handlePosition);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            CalcRotation(rotation);
                                            Tools.handleRotation = handleRotation = rotation;
                                        }
                                    }
                                    #endregion
                                }

                                #endregion
                            }
                            #endregion
                        }
                        else if (VA.SelectionActiveBone >= 0)
                        {
                            genericHandle = true;
                        }
                        else
                        {
                            VA.EnableCustomTools(Tool.None);
                        }
                        #endregion
                    }
                    else if (VA.SelectionActiveBone >= 0)
                    {
                        #region Generic
                        genericHandle = true;
                        #endregion
                    }
                    else
                    {
                        VA.EnableCustomTools(Tool.None);
                    }
                    if (genericHandle && VA.SelectionActiveBone >= 0)
                    {
                        #region GenericHandle
                        VA.EnableCustomTools(Tool.None);
                        var currentTool = VA.CurrentTool();
                        #region handleTransformUpdate
                        if (handleTransformUpdate)
                        {
                            if (Tools.pivotMode == PivotMode.Pivot)
                            {
                                handlePosition = VA.Skeleton.Bones[VA.SelectionActiveBone].transform.position;
                                if (Tools.pivotRotation == PivotRotation.Local)
                                    handleRotation = VA.Skeleton.Bones[VA.SelectionActiveBone].transform.rotation;
                                else
                                    handleRotation = Tools.handleRotation;
                            }
                            else if (Tools.pivotMode == PivotMode.Center)
                            {
                                handlePosition = VA.GetSelectionBounds().center;
                                if (Tools.pivotRotation == PivotRotation.Local)
                                    handleRotation = VA.GetSelectionBoundsRotation();
                                else
                                    handleRotation = Tools.handleRotation;
                            }
                            handleScale = Vector3.one;
                            handlePositionSave = handlePosition;
                            handleRotationSave = handleRotation;
                        }
                        #endregion
                        #region CenterLine
                        if (Tools.pivotMode == PivotMode.Center && (currentTool == Tool.Move || currentTool == Tool.Rotate))
                        {
                            var saveColor = Handles.color;
                            Handles.color = EditorSettings.SettingBoneActiveColor;
                            Handles.DrawLine(handlePositionSave, VA.Skeleton.Bones[VA.SelectionActiveBone].transform.position);
                            Handles.color = saveColor;
                        }
                        #endregion
                        void SetCenterRotationAction(Action<int, Quaternion> action)
                        {
                            var normal = handleRotationSave * Vector3.forward;
                            var vecBase = handleRotationSave * Vector3.right;
                            if (VA.SelectionBones.Count > 1)
                            {
                                foreach (var boneIndex in VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                {
                                    Quaternion rotation = Quaternion.identity;
                                    {
                                        var vecSub = VA.Skeleton.Bones[boneIndex].transform.position - handlePositionSave;
                                        vecSub.Normalize();
                                        if (vecSub.sqrMagnitude > 0f)
                                        {
                                            rotation = Quaternion.AngleAxis(Vector3.SignedAngle(vecBase, vecSub, normal), normal);
                                        }
                                    }
                                    action(boneIndex, rotation);
                                }
                            }
                            else if (VA.SelectionBones.Count == 1)
                            {
                                action(VA.SelectionBones[0], Quaternion.identity);
                            }
                        }
                        if (currentTool == Tool.Move)
                        {
                            #region Move
                            EditorGUI.BeginChangeCheck();
                            var position = Handles.PositionHandle(handlePosition, handleRotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                var offset = position - handlePosition;
                                offset *= VA.GetSelectionSuppressPowerRate();

                                if (!VA.IsHuman)
                                {
                                    VA.SampleAnimationLegacy(VeryAnimation.EditObjectFlag.Skeleton);
                                }

                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    #region Center
                                    SetCenterRotationAction((boneIndex, different) =>
                                    {
                                        var t = VA.Skeleton.Bones[boneIndex].transform;
                                        var save = t.localPosition;
#if VERYANIMATION_TIMELINE
                                        if (VA.UAw.GetLinkedWithTimeline() && boneIndex == 0)
                                        {
                                            VA.UAw.GetTimelineRootMotionOffsets(out Vector3 offsetPosition, out Quaternion offsetRotation);
                                            t.Translate(Quaternion.Inverse(offsetRotation) * (different * offset), Space.World);
                                            var localPosition = VA.GetAnimationValueTransformPosition(boneIndex) + (t.localPosition - save);
                                            VA.SetAnimationValueTransformPosition(boneIndex, localPosition);
                                        }
                                        else
#endif
                                        {
                                            t.Translate(different * offset, Space.World);
                                            VA.SetAnimationValueTransformPosition(boneIndex, t.localPosition);
                                        }
                                        t.localPosition = save;
                                    });
                                    #endregion
                                }
                                else
                                {
                                    #region Pivot
                                    foreach (var boneIndex in VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                    {
                                        var t = VA.Skeleton.Bones[boneIndex].transform;
                                        var save = t.localPosition;
#if VERYANIMATION_TIMELINE
                                        if (VA.UAw.GetLinkedWithTimeline() && boneIndex == 0)
                                        {
                                            VA.UAw.GetTimelineRootMotionOffsets(out Vector3 offsetPosition, out Quaternion offsetRotation);
                                            t.Translate(Quaternion.Inverse(offsetRotation) * offset, Space.World);
                                            var localPosition = VA.GetAnimationValueTransformPosition(boneIndex) + (t.localPosition - save);
                                            VA.SetAnimationValueTransformPosition(boneIndex, localPosition);
                                        }
                                        else
#endif
                                        {
                                            t.Translate(offset, Space.World);
                                            VA.SetAnimationValueTransformPosition(boneIndex, t.localPosition);
                                        }
                                        t.localPosition = save;
                                    }
                                    #endregion
                                }
                                handlePosition = position;
                            }
                            #endregion
                        }
                        else if (currentTool == Tool.Rotate)
                        {
                            #region Rotate
                            EditorGUI.BeginChangeCheck();
                            var rotation = Handles.RotationHandle(handleRotation, handlePosition);
                            if (EditorGUI.EndChangeCheck())
                            {
                                var offset = Quaternion.Inverse(handleRotation) * rotation;
                                offset.ToAngleAxis(out float angle, out Vector3 axis);
                                angle *= VA.GetSelectionSuppressPowerRate();

                                if (!VA.IsHuman)
                                {
                                    VA.SampleAnimationLegacy(VeryAnimation.EditObjectFlag.Skeleton);
                                }

                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    #region Center
                                    SetCenterRotationAction((boneIndex, different) =>
                                    {
                                        var t = VA.Skeleton.Bones[boneIndex].transform;
                                        var save = t.localRotation;
                                        var handleRotationSub = different * handleRotation;
#if VERYANIMATION_TIMELINE
                                        if (VA.UAw.GetLinkedWithTimeline() && boneIndex == 0)
                                        {
                                            t.Rotate(handleRotationSub * axis, angle, Space.World);
                                            var localRotation = VA.GetAnimationValueTransformRotation(boneIndex) * (Quaternion.Inverse(save) * t.localRotation);
                                            VA.SetAnimationValueTransformRotation(boneIndex, localRotation);
                                        }
                                        else
#endif
                                        {
                                            t.Rotate(handleRotationSub * axis, angle, Space.World);
                                            VA.SetAnimationValueTransformRotation(boneIndex, t.localRotation);
                                        }
                                        t.localRotation = save;
                                    });
                                    #endregion
                                }
                                else
                                {
                                    #region Pivot
                                    foreach (var boneIndex in VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                    {
                                        var t = VA.Skeleton.Bones[boneIndex].transform;
                                        var save = t.localRotation;
#if VERYANIMATION_TIMELINE
                                        if (VA.UAw.GetLinkedWithTimeline() && boneIndex == 0)
                                        {
                                            t.Rotate(handleRotation * axis, angle, Space.World);
                                            var localRotation = VA.GetAnimationValueTransformRotation(boneIndex) * (Quaternion.Inverse(save) * t.localRotation);
                                            VA.SetAnimationValueTransformRotation(boneIndex, localRotation);
                                        }
                                        else
#endif
                                        {
                                            t.Rotate(handleRotation * axis, angle, Space.World);
                                            VA.SetAnimationValueTransformRotation(boneIndex, t.localRotation);
                                        }
                                        t.localRotation = save;
                                    }
                                    #endregion
                                }
                                Tools.handleRotation = handleRotation = rotation;
                            }
                            #endregion
                        }
                        else if (currentTool == Tool.Scale)
                        {
                            #region Scale
                            if (Tools.pivotRotation == PivotRotation.Local)
                            {
                                EditorGUI.BeginChangeCheck();
                                var scale = Handles.ScaleHandle(handleScale, handlePosition, handleRotation, HandleUtility.GetHandleSize(handlePosition));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    var offset = scale - handleScale;
                                    offset *= VA.GetSelectionSuppressPowerRate();

                                    if (!VA.IsHuman)
                                    {
                                        VA.SampleAnimationLegacy(VeryAnimation.EditObjectFlag.Skeleton);
                                    }

                                    foreach (var boneIndex in VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                    {
                                        var t = VA.Skeleton.Bones[boneIndex].transform;
                                        VA.SetAnimationValueTransformScale(boneIndex, t.localScale + offset);
                                    }
                                    handleScale = scale;
                                }
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                #endregion
                #region MotionHandle
                if (VA.SelectionMotionTool)
                {
                    if (handleTransformUpdate)
                    {
                        handlePosition = VA.AnimatorWorldRootPositionCache;
                        handleRotation = Tools.pivotRotation == PivotRotation.Local ? VA.AnimatorWorldRootRotationCache : Tools.handleRotation;
                        handlePositionSave = handlePosition;
                        handleRotationSave = handleRotation;
                    }
                    VA.EnableCustomTools(Tool.None);
                    var currentTool = VA.CurrentTool();
                    if (currentTool == Tool.Move)
                    {
                        EditorGUI.BeginChangeCheck();
                        var position = Handles.PositionHandle(handlePosition, handleRotation);
                        if (EditorGUI.EndChangeCheck())
                        {
                            VA.SetAnimationValueAnimatorMotionT(VA.GetAnimatorLocalMotionPosition(position));
                            handlePosition = position;
                        }
                    }
                    else if (currentTool == Tool.Rotate)
                    {
                        EditorGUI.BeginChangeCheck();
                        var rotation = Handles.RotationHandle(handleRotation, handlePosition);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (Tools.pivotRotation == PivotRotation.Local)
                            {
                                VA.SetAnimationValueAnimatorMotionQ(VA.GetAnimatorLocalMotionRotation(rotation));
                            }
                            else
                            {
                                (Quaternion.Inverse(handleRotation) * rotation).ToAngleAxis(out float angle, out Vector3 axis);
                                var bodyRotation = VA.AnimatorWorldRootRotationCache;
                                bodyRotation = bodyRotation * Quaternion.Inverse(bodyRotation) * Quaternion.AngleAxis(angle, handleRotation * axis) * bodyRotation;
                                VA.SetAnimationValueAnimatorMotionQ(VA.GetAnimatorLocalMotionRotation(bodyRotation));
                            }
                            Tools.handleRotation = handleRotation = rotation;
                        }
                    }
                }
                #endregion
                #region IKHandle
                VA.IKHandleGUI();
                VA.IKTargetGUI();
                #endregion

                if (e.type == EventType.Repaint)
                {
                    #region Skeleton
                    if (EditorSettings.SettingsSkeletonFKType != EditorSettings.SkeletonType.None && VA.SkeletonFKShowBoneList != null)
                    {
                        DrawSkeleton();
                    }
                    #endregion

                    #region MuscleLimit
                    if (VA.IsHuman && EditorSettings.SettingBoneMuscleLimit && VA.optionsClampMuscle &&
                        Tools.pivotMode == PivotMode.Pivot &&
                        muscleRotationHandleIds != null && muscleRotationHandleIds.Length == 3 &&
                        muscleRotationSliderIds != null && muscleRotationSliderIds.Length == 3)
                    {
                        var humanoidIndex = (int)VA.SelectionGameObjectHumanoidIndex();
                        if (humanoidIndex >= 0 && VA.CurrentTool() == Tool.Rotate)
                        {
                            Transform t = null;
                            if (VA.Skeleton.HumanoidBones[humanoidIndex] != null)
                                t = VA.Skeleton.HumanoidBones[humanoidIndex].transform;
                            int index1 = HumanTrait.MuscleFromBone(humanoidIndex, 0);
                            int index2 = HumanTrait.MuscleFromBone(humanoidIndex, 1);
                            int index3 = HumanTrait.MuscleFromBone(humanoidIndex, 2);
                            float axisLength = HandleUtility.GetHandleSize(handlePosition);
                            Quaternion quaternion1, quaternion2;
                            {
                                Quaternion preRotation = VA.GetHumanoidAvatarPreRotation((HumanBodyBones)humanoidIndex);
                                Quaternion postRotation = VA.GetHumanoidAvatarPostRotation((HumanBodyBones)humanoidIndex);
                                quaternion1 = t != null ? t.parent.rotation * preRotation : VA.GetHumanVirtualBoneParentRotation((HumanBodyBones)humanoidIndex);
                                quaternion2 = t != null ? t.rotation * postRotation : VA.GetHumanVirtualBoneRotation((HumanBodyBones)humanoidIndex);
                            }
                            Quaternion zyRoll = VA.GetHumanoidAvatarZYRoll((HumanBodyBones)humanoidIndex);
                            Vector3 limitSign = t != null ? VA.GetHumanoidAvatarLimitSign((HumanBodyBones)humanoidIndex) : VA.GetHumanVirtualBoneLimitSign((HumanBodyBones)humanoidIndex);
                            //X
                            Vector3 normalX = Vector3.zero, fromX = Vector3.zero, lineX = Vector3.zero;
                            float angleX = 0f, radiusX = 0f;
                            if (index1 != -1)
                            {
                                Quaternion zyPostQ = t != null ? VA.GetHumanoidAvatarZYPostQ((HumanBodyBones)humanoidIndex, t.parent.rotation, t.rotation) : quaternion1;
                                float angle = VA.HumanoidMuscleLimit[humanoidIndex].min.x;
                                float num = VA.HumanoidMuscleLimit[humanoidIndex].max.x;
                                float length = axisLength;
                                if (VA.MusclePropertyName.Names[index1].StartsWith("Left") || VA.MusclePropertyName.Names[index1].StartsWith("Right")) //why?
                                {
                                    angle *= 0.5f;
                                    num *= 0.5f;
                                }
                                Vector3 vector3_3 = zyPostQ * Vector3.forward;
                                Vector3 vector3_5 = quaternion2 * Vector3.right * limitSign.x;
                                Vector3 from = Quaternion.AngleAxis(angle, vector3_5) * vector3_3;

                                normalX = vector3_5;
                                fromX = from;
                                angleX = num - angle;
                                radiusX = length;
                                Vector3 lineVec = Quaternion.AngleAxis(Mathf.Lerp(angle, num, (VA.GetAnimationValueAnimatorMuscle(index1) + 1f) / 2f), vector3_5) * vector3_3;
                                lineX = handlePosition + lineVec * length;
                            }
                            //Y
                            Vector3 normalY = Vector3.zero, fromY = Vector3.zero, lineY = Vector3.zero;
                            float angleY = 0f, radiusY = 0f;
                            if (index2 != -1)
                            {
                                float angle = VA.HumanoidMuscleLimit[humanoidIndex].min.y;
                                float num = VA.HumanoidMuscleLimit[humanoidIndex].max.y;
                                float length = axisLength;
                                Vector3 vector3_2 = quaternion1 * Vector3.up * limitSign.y;
                                Vector3 vector3_3 = quaternion1 * zyRoll * Vector3.right;
                                Vector3 from = Quaternion.AngleAxis(angle, vector3_2) * vector3_3;

                                normalY = vector3_2;
                                fromY = from;
                                angleY = num - angle;
                                radiusY = length;
                                Vector3 lineVec = Quaternion.AngleAxis(Mathf.Lerp(angle, num, (VA.GetAnimationValueAnimatorMuscle(index2) + 1f) / 2f), vector3_2) * vector3_3;
                                lineY = handlePosition + lineVec * length;
                            }
                            //Z
                            Vector3 normalZ = Vector3.zero, fromZ = Vector3.zero, lineZ = Vector3.zero;
                            float angleZ = 0f, radiusZ = 0f;
                            if (index3 != -1)
                            {
                                float angle = VA.HumanoidMuscleLimit[humanoidIndex].min.z;
                                float num = VA.HumanoidMuscleLimit[humanoidIndex].max.z;
                                float length = axisLength;
                                Vector3 vector3_7 = quaternion1 * Vector3.forward * limitSign.z;
                                Vector3 vector3_8 = quaternion1 * zyRoll * Vector3.right;
                                Vector3 from = Quaternion.AngleAxis(angle, vector3_7) * vector3_8;

                                normalZ = vector3_7;
                                fromZ = from;
                                angleZ = num - angle;
                                radiusZ = length;
                                Vector3 lineVec = Quaternion.AngleAxis(Mathf.Lerp(angle, num, (VA.GetAnimationValueAnimatorMuscle(index3) + 1f) / 2f), vector3_7) * vector3_8;
                                lineZ = handlePosition + lineVec * length;
                            }
                            if (GUIUtility.hotControl == muscleRotationHandleIds[0])
                            {
                                #region DrawY
                                if (index2 != -1)
                                {
                                    Color color = muscleRotationHandleIds[1] == GUIUtility.hotControl || muscleRotationSliderIds[1] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.yAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalY, fromY, angleY, radiusY);
                                    Handles.color = new Color(1f, 0f, 1f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineY);
                                }
                                #endregion
                                #region DrawZ
                                if (index3 != -1)
                                {
                                    Color color = muscleRotationHandleIds[2] == GUIUtility.hotControl || muscleRotationSliderIds[2] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.zAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalZ, fromZ, angleZ, radiusZ);
                                    Handles.color = new Color(1f, 1f, 0f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineZ);
                                }
                                #endregion
                                #region DrawX
                                if (index1 != -1)
                                {
                                    Color color = muscleRotationHandleIds[0] == GUIUtility.hotControl || muscleRotationSliderIds[0] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.xAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalX, fromX, angleX, radiusX);
                                    Handles.color = new Color(0f, 1f, 1f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineX);
                                }
                                #endregion
                            }
                            else if (GUIUtility.hotControl == muscleRotationHandleIds[1])
                            {
                                #region DrawX
                                if (index1 != -1)
                                {
                                    Color color = muscleRotationHandleIds[0] == GUIUtility.hotControl || muscleRotationSliderIds[0] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.xAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalX, fromX, angleX, radiusX);
                                    Handles.color = new Color(0f, 1f, 1f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineX);
                                }
                                #endregion
                                #region DrawZ
                                if (index3 != -1)
                                {
                                    Color color = muscleRotationHandleIds[2] == GUIUtility.hotControl || muscleRotationSliderIds[2] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.zAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalZ, fromZ, angleZ, radiusZ);
                                    Handles.color = new Color(1f, 1f, 0f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineZ);
                                }
                                #endregion
                                #region DrawY
                                if (index2 != -1)
                                {
                                    Color color = muscleRotationHandleIds[1] == GUIUtility.hotControl || muscleRotationSliderIds[1] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.yAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalY, fromY, angleY, radiusY);
                                    Handles.color = new Color(1f, 0f, 1f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineY);
                                }
                                #endregion
                            }
                            else
                            {
                                #region DrawX
                                if (index1 != -1)
                                {
                                    Color color = muscleRotationHandleIds[0] == GUIUtility.hotControl || muscleRotationSliderIds[0] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.xAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalX, fromX, angleX, radiusX);
                                    Handles.color = new Color(0f, 1f, 1f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineX);
                                }
                                #endregion
                                #region DrawY
                                if (index2 != -1)
                                {
                                    Color color = muscleRotationHandleIds[1] == GUIUtility.hotControl || muscleRotationSliderIds[1] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.yAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalY, fromY, angleY, radiusY);
                                    Handles.color = new Color(1f, 0f, 1f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineY);
                                }
                                #endregion
                                #region DrawZ
                                if (index3 != -1)
                                {
                                    Color color = muscleRotationHandleIds[2] == GUIUtility.hotControl || muscleRotationSliderIds[2] == GUIUtility.hotControl ? new Color(1f, 1f, 1f, 0.5f) : new Color(1, 1, 1, 0.2f);
                                    Handles.color = Handles.zAxisColor * color;
                                    Handles.DrawSolidArc(handlePosition, normalZ, fromZ, angleZ, radiusZ);
                                    Handles.color = new Color(1f, 1f, 0f, Handles.color.a);
                                    Handles.DrawLine(handlePosition, lineZ);
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region Collision
                    if (VA.DrawCollision())
                    {
                        repaintScene = true;
                    }
                    #endregion
                }

                #region Bones
                {
                    var bkColor = GUI.color;
                    Handles.BeginGUI();

                    #region Bones
                    for (int i = 0; i < VA.Bones.Length; i++)
                    {
                        if (i == VA.RootMotionBoneIndex) continue;
                        if (!VA.IsShowBone(i)) continue;

                        var pos = HandleUtility.WorldToGUIPoint(VA.Skeleton.Bones[i].transform.position);
                        var rect = new Rect(pos.x - EditorSettings.SettingBoneButtonSize / 2f, pos.y - EditorSettings.SettingBoneButtonSize / 2f, EditorSettings.SettingBoneButtonSize, EditorSettings.SettingBoneButtonSize);
                        bool selected = VA.SelectionGameObjectsIndexOf(VA.Bones[i]) >= 0;
                        GUIStyle guiStyle = GuiStyleCircleButton;
                        if (VA.IsHuman)
                        {
                            if (i == VA.RootMotionBoneIndex)
                                guiStyle = GuiStyleCircle3Button;
                            else if (VA.BoneIndex2humanoidIndex[i] < 0)
                                guiStyle = GuiStyleDiamondButton;
                        }
                        else
                        {
                            if (VA.RootMotionBoneIndex >= 0)
                            {
                                if (i == VA.RootMotionBoneIndex)
                                    guiStyle = GuiStyleCircle3Button;
                            }
                            else
                            {
                                if (i == 0)
                                    guiStyle = GuiStyleCircle3Button;
                            }
                        }
                        {
                            Color color;
                            if (selected) color = EditorSettings.SettingBoneActiveColor;
                            else color = EditorSettings.SettingBoneNormalColor;
                            color.a *= !VA.IsWriteLockBone(i) ? 1f : GUINonActiveAlpha;
                            GUI.color = color;
                        }
                        if (GUI.Button(rect, "", guiStyle))
                        {
                            VA.SelectGameObjectPlusKey(VA.Bones[i]);
                        }
                    }
                    #endregion

                    if (VA.IsHuman)
                    {
                        #region Virtual
                        {
                            for (int i = 0; i < VeryAnimation.HumanVirtualBones.Length; i++)
                            {
                                if (!VA.IsShowVirtualBone((HumanBodyBones)i)) continue;

                                var pos = HandleUtility.WorldToGUIPoint(VA.GetHumanVirtualBonePosition((HumanBodyBones)i));
                                var rect = new Rect(pos.x - EditorSettings.SettingBoneButtonSize / 2f, pos.y - EditorSettings.SettingBoneButtonSize / 2f, EditorSettings.SettingBoneButtonSize, EditorSettings.SettingBoneButtonSize);
                                bool selected = VA.SelectionGameObjectsContains((HumanBodyBones)i);
                                {
                                    Color color;
                                    if (selected) color = EditorSettings.SettingBoneActiveColor;
                                    else color = EditorSettings.SettingBoneNormalColor;
                                    color.a *= !VA.IsWriteLockBone((HumanBodyBones)i) ? 1f : GUINonActiveAlpha;
                                    GUI.color = color;
                                }
                                GUIStyle guiStyle = GuiStyleCircleDotButton;
                                if (GUI.Button(rect, "", guiStyle))
                                {
                                    VA.SelectVirtualBonePlusKey((HumanBodyBones)i);
                                    VeryAnimationControlWindow.ForceSelectionChange();
                                }
                            }
                        }
                        #endregion

                        #region Root
                        if (VA.IsShowBone(VA.RootMotionBoneIndex))
                        {
                            var pos = HandleUtility.WorldToGUIPoint(VA.HumanWorldRootPositionCache);
                            var rect = new Rect(pos.x - EditorSettings.SettingBoneButtonSize / 2f, pos.y - EditorSettings.SettingBoneButtonSize / 2f, EditorSettings.SettingBoneButtonSize, EditorSettings.SettingBoneButtonSize);
                            bool selected = VA.SelectionGameObjectsIndexOf(GameObject) >= 0;
                            {
                                Color color;
                                if (selected) color = EditorSettings.SettingBoneActiveColor;
                                else color = EditorSettings.SettingBoneNormalColor;
                                color.a *= !VA.IsWriteLockBone(VA.RootMotionBoneIndex) ? 1f : GUINonActiveAlpha;
                                GUI.color = color;
                            }
                            if (GUI.Button(rect, "", GuiStyleCircle3Button))
                            {
                                VA.SelectGameObjectPlusKey(GameObject);
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region Root
                        if (VA.IsShowBone(VA.RootMotionBoneIndex))
                        {
                            var pos = HandleUtility.WorldToGUIPoint(VA.Skeleton.Bones[VA.RootMotionBoneIndex].transform.position);
                            var rect = new Rect(pos.x - EditorSettings.SettingBoneButtonSize / 2f, pos.y - EditorSettings.SettingBoneButtonSize / 2f, EditorSettings.SettingBoneButtonSize, EditorSettings.SettingBoneButtonSize);
                            bool selected = VA.SelectionGameObjectsIndexOf(VA.Bones[VA.RootMotionBoneIndex]) >= 0;
                            {
                                Color color;
                                if (selected) color = EditorSettings.SettingBoneActiveColor;
                                else color = EditorSettings.SettingBoneNormalColor;
                                color.a *= !VA.IsWriteLockBone(VA.RootMotionBoneIndex) ? 1f : GUINonActiveAlpha;
                                GUI.color = color;
                            }
                            if (GUI.Button(rect, "", GuiStyleCircle3Button))
                            {
                                VA.SelectGameObjectPlusKey(VA.Bones[VA.RootMotionBoneIndex]);
                            }
                        }
                        #endregion
                    }

                    Handles.EndGUI();
                    GUI.color = bkColor;
                }
                #endregion
            }
            #endregion

            #region SceneWindow
            #region Selection
            if (VAE.EditorSelectionOnScene)
            {
                editorWindowSelectionRect.width = VAE.position.width;
                editorWindowSelectionRect = ResizeSceneViewRect(sceneView, editorWindowSelectionRect);
                editorWindowSelectionRect = GUILayout.Window(EditorGUIUtility.GetControlID(FocusType.Passive, editorWindowSelectionRect), editorWindowSelectionRect, (id) =>
                {
                    var saveSkin = GUI.skin;
                    GUI.skin = GuiSkinSceneWindow;
                    {
                        VAE.EditorGUI_SelectionGUI(true);
                    }
                    GUI.skin = saveSkin;
                    GUI.DragWindow();

                }, "Selection", GuiStyleSceneWindow);
            }
            #endregion
            #endregion

            if (repaintScene)
            {
                sceneView.Repaint();
            }

#if Enable_Profiler
            Profiler.EndSample();
#endif
        }

        private Rect ResizeSceneViewRect(SceneView sceneView, Rect rect)
        {
            var position = sceneView.position;
            if (rect.x + rect.width >= position.width)
                rect.x -= (rect.x + rect.width) - position.width;
            if (rect.y + rect.height >= position.height)
                rect.y -= (rect.y + rect.height) - position.height;
            if (rect.x < 0)
                rect.x -= rect.x;
            if (rect.y < 0)
                rect.y -= rect.y;
            return rect;
        }

        private void DrawRootTrail()
        {
            rootTrail ??= new RootTrail();
            rootTrail.Draw();
        }

        private void DrawSkeleton()
        {
            if (EditorSettings.SettingsSkeletonFKType == EditorSettings.SkeletonType.Mesh ||
                EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Mesh)
            {
                arrowMesh ??= new EditorCommon.ArrowMesh();
            }

#if VERYANIMATION_ANIMATIONRIGGING
            #region AnimationRigging
            {
                if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Line)
                {
                    UHandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                }
                else if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Lines)
                {
                    UHandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                }

                for (int target = 0; target < VA.animatorIK.ikData.Length; target++)
                {
                    if (VA.animatorIK.ikData[target].rigConstraint == null)
                        continue;
                    switch ((AnimatorIKCore.IKTarget)target)
                    {
                        case AnimatorIKCore.IKTarget.Head:
                            {
                                var constraint = VA.animatorIK.ikData[target].rigConstraint as MultiAimConstraint;
                                if (constraint != null)
                                {
                                    if (constraint.data.constrainedObject != null && constraint.data.sourceObjects.Count > 0 && constraint.data.sourceObjects[0].transform != null)
                                    {
                                        var targetIndex = VA.BonesIndexOf(constraint.data.sourceObjects[0].transform.gameObject);
                                        if (targetIndex >= 0)
                                            DrawSkeletonAnimationRiggingBone(VA.BonesIndexOf(constraint.data.constrainedObject.gameObject), targetIndex, VA.AnimationRigging.ArRig.weight * constraint.weight);
                                    }
                                }
                            }
                            break;
                        case AnimatorIKCore.IKTarget.LeftHand:
                        case AnimatorIKCore.IKTarget.RightHand:
                        case AnimatorIKCore.IKTarget.LeftFoot:
                        case AnimatorIKCore.IKTarget.RightFoot:
                            {
                                var constraint = VA.animatorIK.ikData[target].rigConstraint as TwoBoneIKConstraint;
                                if (constraint != null)
                                {
                                    if (constraint.data.mid != null)
                                        DrawSkeletonAnimationRiggingBone(VA.BonesIndexOf(constraint.data.mid.gameObject), -1, VA.AnimationRigging.ArRig.weight * constraint.weight);
                                    if (constraint.data.tip != null)
                                        DrawSkeletonAnimationRiggingBone(VA.BonesIndexOf(constraint.data.tip.gameObject), -1, VA.AnimationRigging.ArRig.weight * constraint.weight);
                                }
                            }
                            break;
                    }
                }

                if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Line ||
                    EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Lines)
                {
                    GL.End();
                    GL.PopMatrix();
                }
            }
            #endregion
#endif
            #region IK
            if (VA.SkeletonIKShowBoneList.Count > 0)
            {
                if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Line)
                {
                    UHandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                    GL.Color(EditorSettings.SettingSkeletonIKColor);
                    foreach (var pair in VA.SkeletonIKShowBoneList)
                    {
                        var boneA = VA.Bones[pair.y >= 0 ? pair.y : VA.ParentBoneIndexes[pair.x]];
                        var boneB = VA.Bones[pair.x];
                        if (boneA == null || boneB == null)
                            continue;
                        var posA = boneA.transform.position;
                        var posB = boneB.transform.position;
                        GL.Vertex(posA);
                        GL.Vertex(posB);
                    }
                    GL.End();
                    GL.PopMatrix();
                }
                else if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Lines)
                {
                    var cameraForward = SceneView.currentDrawingSceneView.camera.transform.forward;
                    float radius = EditorSettings.GetSkeletonTypeLinesRadius(VA.Skeleton.GameObject.transform.position);

                    UHandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                    GL.Color(EditorSettings.SettingSkeletonIKColor);
                    foreach (var pair in VA.SkeletonIKShowBoneList)
                    {
                        var boneA = VA.Bones[pair.y >= 0 ? pair.y : VA.ParentBoneIndexes[pair.x]];
                        var boneB = VA.Bones[pair.x];
                        if (boneA == null || boneB == null)
                            continue;
                        var posA = boneA.transform.position;
                        var posB = boneB.transform.position;

                        var vec = posB - posA;
                        var cross = Vector3.Cross(vec, cameraForward);
                        cross.Normalize();
                        vec.Normalize();

                        var posAL = posA + cross * radius + vec * radius;
                        var posAR = posA - cross * radius + vec * radius;

                        GL.Vertex(posA); GL.Vertex(posAL);
                        GL.Vertex(posAL); GL.Vertex(posB);
                        GL.Vertex(posB); GL.Vertex(posAR);
                        GL.Vertex(posAR); GL.Vertex(posA);
                    }
                    GL.End();
                    GL.PopMatrix();
                }
                else if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Mesh)
                {
                    foreach (var pair in VA.SkeletonIKShowBoneList)
                    {
                        var boneA = VA.Bones[pair.y >= 0 ? pair.y : VA.ParentBoneIndexes[pair.x]];
                        var boneB = VA.Bones[pair.x];
                        if (boneA == null || boneB == null)
                            continue;
                        var posA = boneA.transform.position;
                        var posB = boneB.transform.position;

                        arrowMesh.Material.color = EditorSettings.SettingSkeletonIKColor;
                        arrowMesh.Material.SetPass(0);
                        var vec = posB - posA;
                        var length = vec.magnitude;
                        Quaternion qat = posB != posA ? Quaternion.LookRotation(vec) : Quaternion.identity;
                        Matrix4x4 mat = Matrix4x4.TRS(posA, qat, new Vector3(length, length, length));
                        Graphics.DrawMeshNow(arrowMesh.Mesh, mat);
                    }
                }
            }
            #endregion

            #region FK
            if (VA.SkeletonFKShowBoneList.Count > 0)
            {
                if (EditorSettings.SettingsSkeletonFKType == EditorSettings.SkeletonType.Line)
                {
                    UHandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                    GL.Color(EditorSettings.SettingSkeletonFKColor);
                    foreach (var boneIndex in VA.SkeletonFKShowBoneList)
                    {
                        var boneA = VA.Skeleton.Bones[VA.ParentBoneIndexes[boneIndex]];
                        var boneB = VA.Skeleton.Bones[boneIndex];
                        if (boneA == null || boneB == null)
                            continue;
                        var posA = boneA.transform.position;
                        var posB = boneB.transform.position;
                        GL.Vertex(posA);
                        GL.Vertex(posB);
                    }
                    GL.End();
                    GL.PopMatrix();
                }
                else if (EditorSettings.SettingsSkeletonFKType == EditorSettings.SkeletonType.Lines)
                {
                    var cameraForward = SceneView.currentDrawingSceneView.camera.transform.forward;
                    float radius = EditorSettings.GetSkeletonTypeLinesRadius(VA.Skeleton.GameObject.transform.position);

                    UHandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                    GL.Color(EditorSettings.SettingSkeletonFKColor);
                    foreach (var boneIndex in VA.SkeletonFKShowBoneList)
                    {
                        var boneA = VA.Skeleton.Bones[VA.ParentBoneIndexes[boneIndex]];
                        var boneB = VA.Skeleton.Bones[boneIndex];
                        if (boneA == null || boneB == null)
                            continue;
                        var posA = boneA.transform.position;
                        var posB = boneB.transform.position;

                        var vec = posB - posA;
                        var cross = Vector3.Cross(vec, cameraForward);
                        cross.Normalize();
                        vec.Normalize();

                        var posAL = posA + cross * radius + vec * radius;
                        var posAR = posA - cross * radius + vec * radius;

                        GL.Vertex(posA); GL.Vertex(posAL);
                        GL.Vertex(posAL); GL.Vertex(posB);
                        GL.Vertex(posB); GL.Vertex(posAR);
                        GL.Vertex(posAR); GL.Vertex(posA);
                    }
                    GL.End();
                    GL.PopMatrix();
                }
                else if (EditorSettings.SettingsSkeletonFKType == EditorSettings.SkeletonType.Mesh)
                {
                    foreach (var boneIndex in VA.SkeletonFKShowBoneList)
                    {
                        var boneA = VA.Skeleton.Bones[VA.ParentBoneIndexes[boneIndex]];
                        var boneB = VA.Skeleton.Bones[boneIndex];
                        if (boneA == null || boneB == null)
                            continue;
                        var posA = boneA.transform.position;
                        var posB = boneB.transform.position;

                        arrowMesh.Material.color = EditorSettings.SettingSkeletonFKColor;
                        arrowMesh.Material.SetPass(0);
                        var vec = posB - posA;
                        var length = vec.magnitude;
                        Quaternion qat = posB != posA ? Quaternion.LookRotation(vec) : Quaternion.identity;
                        Matrix4x4 mat = Matrix4x4.TRS(posA, qat, new Vector3(length, length, length));
                        Graphics.DrawMeshNow(arrowMesh.Mesh, mat);
                    }
                }
            }
            #endregion

            #region RootMotion
            {
                var posA = VA.TransformPoseSave.StartPosition;
                var posB = VA.Skeleton.GameObject.transform.position;
                UHandleUtility.ApplyWireMaterial();
                GL.PushMatrix();
                GL.MultMatrix(Handles.matrix);
                GL.Begin(GL.LINES);
                GL.Color(EditorSettings.SettingRootMotionColor);
                {
                    GL.Vertex(posA);
                    GL.Vertex(posB);
                }
                GL.End();
                GL.PopMatrix();
            }
            #endregion
        }
#if VERYANIMATION_ANIMATIONRIGGING
        private void DrawSkeletonAnimationRiggingBone(int boneIndex, int targetIndex, float alpha)
        {
            if (!VA.SkeletonFKShowBoneFlag[boneIndex])
                return;

            var boneA = VA.Bones[targetIndex >= 0 ? targetIndex : VA.ParentBoneIndexes[boneIndex]];
            var boneB = VA.Bones[boneIndex];
            if (boneA == null || boneB == null)
                return;
            var posA = boneA.transform.position;
            var posB = boneB.transform.position;

            var color = EditorSettings.SettingSkeletonIKColor;
            color.a *= alpha;

            if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Line)
            {
                GL.Color(color);

                GL.Vertex(posA);
                GL.Vertex(posB);
            }
            else if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Lines)
            {
                var cameraForward = SceneView.currentDrawingSceneView.camera.transform.forward;
                float radius = EditorSettings.GetSkeletonTypeLinesRadius(VA.Skeleton.GameObject.transform.position);

                GL.Color(color);

                var vec = posB - posA;
                var cross = Vector3.Cross(vec, cameraForward);
                cross.Normalize();
                vec.Normalize();

                var posAL = posA + cross * radius + vec * radius;
                var posAR = posA - cross * radius + vec * radius;

                GL.Vertex(posA); GL.Vertex(posAL);
                GL.Vertex(posAL); GL.Vertex(posB);
                GL.Vertex(posB); GL.Vertex(posAR);
                GL.Vertex(posAR); GL.Vertex(posA);
            }
            else if (EditorSettings.SettingsSkeletonIKType == EditorSettings.SkeletonType.Mesh)
            {
                arrowMesh.Material.color = color;
                arrowMesh.Material.SetPass(0);

                var vec = posB - posA;
                var length = vec.magnitude;
                Quaternion qat = posB != posA ? Quaternion.LookRotation(vec) : Quaternion.identity;
                Matrix4x4 mat = Matrix4x4.TRS(posA, qat, new Vector3(length, length, length));
                Graphics.DrawMeshNow(arrowMesh.Mesh, mat);
            }
        }
#endif

        private void OnInspectorUpdate()
        {
#if Enable_Profiler
            Profiler.BeginSample("****VeryAnimationWindow.OnInspectorUpdate");
#endif
            if (Initialized)
            {
                VA.OnInspectorUpdate();

                #region Repaint
                switch (repaintGUI)
                {
                    case RepaintGUI.Edit:
                        Repaint();
                        VeryAnimationEditorWindow.ForceRepaint();
                        break;
                    case RepaintGUI.All:
                        Repaint();
                        VeryAnimationEditorWindow.ForceRepaint();
                        VeryAnimationControlWindow.ForceRepaint();
                        SceneView.RepaintAll();
                        break;
                }
                repaintGUI = RepaintGUI.None;
                #endregion
            }
            else
            {
                SetGameObject();

                #region Repaint
                {
                    var errorCode = VA.GetErrorCode;
                    if (beforeErrorCode != errorCode)
                    {
                        Repaint();
                        beforeErrorCode = errorCode;
                    }
                }
                #endregion

                #region LastSelectAnimationClip
                if (GameObject != null && !VA.IsEdit && !UnityEditor.AnimationMode.InAnimationMode())
                {
                    var clip = VA.UAw.GetSelectionAnimationClip();
                    if (beforeAnimationClip != clip)
                    {
                        var saveSettings = GameObject.GetComponent<VeryAnimationSaveSettings>();
                        if (saveSettings != null && saveSettings.lastSelectAnimationClip != clip)
                            saveSettings.lastSelectAnimationClip = clip;
                        beforeAnimationClip = clip;
                    }
                }
                #endregion

                #region PlayingAnimation
                if (UpdatePlayingAnimation())
                {
                    Repaint();
                }
                #endregion
            }

#if Enable_Profiler
            Profiler.EndSample();
#endif
        }
        private void CustomUpdate()
        {
            if (Initialized)
            {
                if (VA.IsEditError)
                {
                    var lastTime = VA.CurrentTime;
                    var errorCode = VA.GetErrorCode;
                    Release();
                    if (VA.UAw.GetLinkedWithTimeline() && VA.UAw.GetActiveRootGameObject() != null)
                    {
#if VERYANIMATION_TIMELINE
                        #region ChangeTimelineAnimationTrack
                        SetGameObject();
                        if (!VA.IsError)
                        {
                            Initialize();
                            VA.SetCurrentTime(lastTime);
                            VA.UAw.StopRecording();     //Update immediately with the next Update
                        }
                        #endregion
#endif
                    }
                    else
                    {
                        Debug.LogFormat("<color=blue>[Very Animation]</color>Editing ended : Error code {0}", errorCode);
                    }
                }
                else if (forceChangeObject != null)
                {
                    #region ChangeOtherObject
                    var lastTime = VA.CurrentTime;
                    Release();
                    Selection.activeGameObject = forceChangeObject;
                    forceChangeObject = null;
                    VA.UAw.OnSelectionChange();
                    SetGameObject();
                    if (!VA.IsError)
                    {
                        Initialize();
                        VA.SetCurrentTime(lastTime);
                        VA.UAw.StopRecording();     //Update immediately with the next Update
                    }
                    #endregion
                }
            }
            else
            {
                return;
            }

#if Enable_Profiler
            Profiler.BeginSample("****VeryAnimationWindow.Update");
#endif

            VA.Update();

#if Enable_Profiler
            Profiler.EndSample();
#endif
        }

        public void Initialize()
        {
            Release();

            if (EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPaused)
                    EditorApplication.isPaused = true;
                if (Animator != null)
                    pauseAnimatorStateSave = new AnimatorStateSave(Animator);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(GameObject.scene);
            }

            UpdatePlayingAnimation();

            SetClipSelectorLayerIndex(-1);
            UpdateClipSelectorTree();

            Selection.activeObject = null;

            undoGroupID = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Very Animation Edit");
            Undo.RecordObject(this, "Very Animation Edit");

            VA.Initialize();
            EditorSettings.Initialize();

            if (!UEditorWindow.HasFocus(this))
            {
                beforeSelectedTab = UEditorWindow.GetSelectedTab(this);
                Focus();
            }
            else
            {
                beforeSelectedTab = -1;
            }

            #region VeryAnimationEditorWindow
            if (VeryAnimationEditorWindow.instance == null)
            {
                if (EditorSettings.SettingEditorWindowStyle == EditorSettings.EditorWindowStyle.Floating)
                {
                    var hew = EditorWindow.CreateInstance<VeryAnimationEditorWindow>();
                    hew.ShowUtility();
                }
                else if (EditorSettings.SettingEditorWindowStyle == EditorSettings.EditorWindowStyle.Docking)
                {
                    EditorWindow window = null;
                    foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    {
                        if (w.GetType().Name == "InspectorWindow")
                        {
                            if (UEditorWindow.HasFocus(w))
                            {
                                window = w;
                                break;
                            }
                        }
                    }
                    if (window != null)
                        GetWindow<VeryAnimationEditorWindow>(window.GetType());
                    if (VeryAnimationEditorWindow.instance == null)
                        GetWindow<VeryAnimationEditorWindow>();
                }
            }
            if (VeryAnimationEditorWindow.instance != null)
                VeryAnimationEditorWindow.instance.Initialize();
            #endregion
            #region VeryAnimationControlWindow
            if (VeryAnimationControlWindow.instance == null)
            {
                EditorWindow dockWindow = null;
                foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    if (w.GetType().Name == "SceneHierarchyWindow" &&
                        !UEditorWindow.IsDockBrother(VA.UAw.Instance, w))
                    {
                        dockWindow = w;
                        break;
                    }
                }
                if (dockWindow != null)
                {
                    var controlWindow = CreateInstance<VeryAnimationControlWindow>();
                    UEditorWindow.AddTab(dockWindow, controlWindow);
                }
                if (VeryAnimationControlWindow.instance == null)
                    GetWindow<VeryAnimationControlWindow>();
            }
            if (VeryAnimationControlWindow.instance != null)
                VeryAnimationControlWindow.instance.Initialize();
            #endregion

            #region SaveSettings
            {
                #region EditorPref
                {
                    guiAnimationFoldout = EditorPrefs.GetBool("VeryAnimation_Main_Animation", true);
                    guiToolsFoldout = EditorPrefs.GetBool("VeryAnimation_Main_Tools", false);
                    guiSettingsFoldout = EditorPrefs.GetBool("VeryAnimation_Main_Settings", false);
                    guiHelpFoldout = EditorPrefs.GetBool("VeryAnimation_Main_Help", false);
                    guiPreviewFoldout = EditorPrefs.GetBool("VeryAnimation_Main_Preview", true);
                }
                #endregion
                VA.LoadSaveSettings();
            }
            VA.OnBoneShowFlagsUpdated.Invoke();
            #endregion

            #region SceneWindow
            editorWindowSelectionRect.size = Vector2.zero;
            #endregion

            EditorApplication.update += CustomUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.beforeSceneGui += OnPreSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            Initialized = true;

            OnSelectionChange();

            InternalEditorUtility.RepaintAllViews();
            EditorApplication.delayCall += () =>
            {
                InternalEditorUtility.RepaintAllViews();
                if (VeryAnimationEditorWindow.instance != null)
                    VeryAnimationEditorWindow.instance.Focus();
            };
        }
        public void Release()
        {
            if (instance == null || VA == null || !Initialized) return;
            Initialized = false;

            VA.StopRecording();

            Undo.SetCurrentGroupName("Very Animation Edit");
            Undo.RecordObject(this, "Very Animation Edit");

            #region SaveSettings
            if (GameObject != null)
            {
                var saveSettings = GameObject.GetComponent<VeryAnimationSaveSettings>();
                if (EditorSettings.SettingComponentSaveSettings)
                {
                    #region Disconnect Prefab Component
                    if (saveSettings != null)
                    {
                        var prefabSaveSettings = PrefabUtility.GetCorrespondingObjectFromSource(saveSettings);
                        if (prefabSaveSettings != null)
                        {
                            Undo.DestroyObjectImmediate(saveSettings);
                            saveSettings = null;
                        }
                    }
                    #endregion
                    if (saveSettings == null)
                    {
                        saveSettings = Undo.AddComponent<VeryAnimationSaveSettings>(GameObject);
                        if (saveSettings != null)
                            InternalEditorUtility.SetIsInspectorExpanded(saveSettings, false);
                    }
                    if (saveSettings != null)
                    {
                        Undo.RecordObject(saveSettings, "Very Animation Edit");
                        VA.SaveSaveSettings();
                    }
                }
                else
                {
                    if (saveSettings != null)
                        Undo.DestroyObjectImmediate(saveSettings);
                }
            }
            #endregion

            EditorApplication.update -= CustomUpdate;
            SceneView.beforeSceneGui -= OnPreSceneGUI;
            SceneView.duringSceneGui -= OnSceneGUI;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Language.OnLanguageChanged = null;
            #region Editor
            handleTransformUpdate = true;
            if (arrowMesh != null)
            {
                arrowMesh.Dispose();
                arrowMesh = null;
            }
            rootTrail = null;
            #endregion

            EditorSettings.Release();
            VA.Release();

            if (undoGroupID >= 0)
            {
                Undo.CollapseUndoOperations(undoGroupID);
                undoGroupID = -1;
            }

            if (pauseAnimatorStateSave != null && Animator != null)
                pauseAnimatorStateSave.Load(Animator);
            pauseAnimatorStateSave = null;

            if (beforeSelectedTab >= 0 && beforeSelectedTab < UEditorWindow.GetNumTabs(this))
            {
                UEditorWindow.SetSelectedTab(this, beforeSelectedTab);
            }

            EditorApplication.delayCall += () =>
            {
                if (VA == null || VA.IsEditError)
                    CloseOtherWindows();
            };

            GC.Collect();

            if (!VA.UAw.GetLinkedWithTimeline())
                Selection.activeObject = VA.UAw.GetActiveRootGameObject();

            VA.UAw.ForceRefresh();

            CloseOtherWindows();

            InternalEditorUtility.RepaintAllViews();
        }

        public bool IsShowSceneGizmo()
        {
            if (Tools.current == Tool.View) return false;
            if (VA.UAw.GetPlaying()) return false;
            return true;
        }

        public bool UpdatePlayingAnimation()
        {
            if (VA.GetPlayingAnimationInfo(out var playingAnimationInfos))
            {
                PlayingAnimationInfos = playingAnimationInfos;
                return true;
            }
            else if (PlayingAnimationInfos != null)
            {
                PlayingAnimationInfos = null;
                return true;
            }
            return false;
        }

        public bool IsContainsSelectionAnimationClip(AnimationClip clip)
        {
            var index = ArrayUtility.IndexOf(ClipSelectorTreeView.AnimationClips, clip);
            return index >= 0;
        }
        public void MoveChangeSelectionAnimationClip(int move)
        {
            ClipSelectorTreeView.OffsetSelection(move);
        }

        private void SetGameObject()
        {
            bool updated = false;
            var go = VA.UAw?.GetActiveRootGameObject();
            if (go != GameObject)
            {
                GameObject = go;
                updated = true;
            }
            var ap = VA.UAw?.GetActiveAnimationPlayer();
            if (ap is Animator)
            {
                var apa = ap as Animator;
                if (Animator != apa)
                {
                    Animator = apa;
                    updated = true;
                }
                Animation = null;
            }
            else if (ap is Animation)
            {
                var apa = ap as Animation;
                if (Animation != apa)
                {
                    Animation = apa;
                    updated = true;
                }
                Animator = null;
            }
            else
            {
                if (Animation != null)
                {
                    Animation = null;
                    updated = true;
                }
                if (Animator != null)
                {
                    Animator = null;
                    updated = true;
                }
            }
            if (updated)
            {
                #region ClipSelector
                UpdateClipSelectorTree();
                #region LastSelectAnimationClip
                if (GameObject != null)
                {
                    if (!EditorApplication.isPlaying && !VA.UAw.GetLinkedWithTimeline())
                    {
                        if (GameObject.TryGetComponent<VeryAnimationSaveSettings>(out var saveSettings))
                        {
                            if (IsContainsSelectionAnimationClip(saveSettings.lastSelectAnimationClip))
                            {
                                VA.UAw.SetSelectionAnimationClip(saveSettings.lastSelectAnimationClip);
                                ClipSelectorTreeView.UpdateSelectedIds();
                            }
                        }
                    }
                }
                #endregion
                #endregion

                Repaint();
            }
        }
        private void ClearGameObject()
        {
            bool updated = GameObject != null;
            GameObject = null;
            Animator = null;
            Animation = null;
            if (updated)
            {
                #region ClipSelector
                UpdateClipSelectorTree();
                #endregion

                Repaint();
            }
        }

        #region Undo
        private void UndoRedoPerformed()
        {
            checkGuiLayoutUpdate = true;

            InternalEditorUtility.RepaintAllViews();
        }
        #endregion
    }
}
