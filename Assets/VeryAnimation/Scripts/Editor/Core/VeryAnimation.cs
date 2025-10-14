//#define Enable_Profiler

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace VeryAnimation
{
    [Serializable]
    internal partial class VeryAnimation
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        #region Reflection
        public UAnimationWindow UAw { get; private set; }
        public UAvatar UAvatar { get; private set; }
        public UAnimator UAnimator { get; private set; }
        public UEditorWindow UEditorWindow { get; private set; }
        public UAnimatorControllerTool UAnimatorControllerTool { get; private set; }
        public UAnimationUtility UAnimationUtility { get; private set; }
        public UAnimationWindowUtility UAnimationWindowUtility { get; private set; }
        public UCurveUtility UCurveUtility { get; private set; }
        public URotationCurveInterpolation URotationCurveInterpolation { get; private set; }
        public USceneView USceneView { get; private set; }
        public UAnimationMode UAnimationMode { get; private set; }

        public UAvatarPreview UAvatarPreview { get; private set; }
        public UAnimationClipEditor UAnimationClipEditor { get; private set; }

        public UAnimationClipEditor UAnimationClipEditorTotal { get; private set; }

#if UNITY_6000_3_OR_NEWER
        public UAnimationWindow_6000_3 UAw_6000_3 { get; private set; }
        public URotationCurveInterpolation_6000_3 URotationCurveInterpolation_6000_3 { get; private set; }
#endif
#if UNITY_2023_1_OR_NEWER
        public UAnimationWindow_2023_1 UAw_2023_1 { get; private set; }
        public UAnimationWindowUtility_2023_1 UAnimationWindowUtility_2023_1 { get; private set; }
        public UEditorWindow_2023_1 UEditorWindow_2023_1 { get; private set; }
#endif
        #endregion

        #region Core
        public MusclePropertyName MusclePropertyName { get; private set; }
#if VERYANIMATION_ANIMATIONRIGGING
        public AnimationRigging AnimationRigging { get; private set; }
#endif
        public AnimatorIKCore animatorIK;
        public OriginalIKCore originalIK;
        private SynchronizeAnimation synchronizeAnimation;
        #endregion

        public bool IsEdit { get; private set; }

        #region Selection
        public List<GameObject> SelectionGameObjects { get; private set; }
        public List<int> SelectionBones { get; private set; }
        public GameObject SelectionActiveGameObject { get { return SelectionGameObjects != null && SelectionGameObjects.Count > 0 ? SelectionGameObjects[0] : null; } }
        public int SelectionActiveBone { get { return SelectionBones != null && SelectionBones.Count > 0 ? SelectionBones[0] : -1; } }
        public List<HumanBodyBones> SelectionHumanVirtualBones { get; private set; }
        public bool SelectionMotionTool { get; private set; }
        #endregion

        #region Cache
        public class MirrorBoneData
        {
            public int rootBoneIndex;
            public bool[] positionTangentInverse;
            public bool[] rotationTangentInverse;
            public bool[] eulerAnglesTangentInverse;
            public bool[] scaleTangentInverse;
        }
        public TransformPoseSave TransformPoseSave { get; private set; }
        public BlendShapeWeightSave BlendShapeWeightSave { get; private set; }
        public Renderer[] Renderers { get; private set; }
        public bool IsHuman { get; private set; }
        public bool AnimatorApplyRootMotion { get; private set; }
        public Avatar AnimatorAvatar { get; private set; }
        public Transform AnimatorAvatarRoot { get; private set; }
        public GameObject[] Bones { get; private set; }
        public Dictionary<GameObject, int> BoneDictionary { get; private set; }
        public GameObject[] HumanoidBones { get; private set; }
        public int[] ParentBoneIndexes { get; private set; }
        public int[] BoneHierarchyLevels { get; private set; }
        public int BoneHierarchyMaxLevel { get; private set; }
        public int[] MirrorBoneIndexes { get; private set; }
        public MirrorBoneData[] MirrorBoneDatas { get; private set; }
        public Dictionary<SkinnedMeshRenderer, Dictionary<string, string>> MirrorBlendShape { get; private set; }
        public HumanBodyBones[] BoneIndex2humanoidIndex { get; private set; }
        public int[] HumanoidIndex2boneIndex { get; private set; }
        public bool[] HumanoidConflict { get; private set; }
        public string[] BonePaths { get; private set; }
        public Dictionary<string, int> BonePathDictionary { get; private set; }
        public UAvatar.Transform[] BoneDefaultPose { get; private set; }
        public TransformPoseSave.SaveData[] BoneSaveTransforms { get; private set; }
        public TransformPoseSave.SaveData[] BoneSaveOriginalTransforms { get; private set; }
        public HumanPose SaveHumanPose { get; private set; }
        public UAvatar.MuscleLimit[] HumanoidMuscleLimit { get; private set; }
        public bool HumanoidHasLeftHand { get; private set; }
        public bool HumanoidHasRightHand { get; private set; }
        public bool HumanoidHasTDoF { get; private set; }
        public Quaternion HumanoidPreHipRotationInverse { get; private set; }
        public Quaternion HumanoidPostHipRotation { get; private set; }
        public float HumanoidLeftLowerLegLengthSq { get; private set; }
        public bool[] HumanoidMuscleContains { get; private set; }
        public HumanPoseHandler HumanPoseHandler { get; private set; }
        public bool IsHumanAvatarReady { get; private set; }
        public int RootMotionBoneIndex { get; private set; }
        public Vector3 HumanWorldRootPositionCache { get; set; }
        public Quaternion HumanWorldRootRotationCache { get; set; }
        public Vector3 AnimatorWorldRootPositionCache { get; set; }
        public Quaternion AnimatorWorldRootRotationCache { get; set; }
        public Bounds GameObjectBounds { get; set; }
        public bool PrefabMode { get; set; }
        public DummyObject Skeleton { get; private set; }
        public OnionSkin OnionSkin { get; private set; }
        #endregion

        #region Current
        public AnimationClip CurrentClip { get; private set; }
        public float CurrentTime { get; private set; }
        public bool CurrentLinkedWithTimeline { get; private set; }
        public Dictionary<AnimatorStateMachine, AnimationClip> CurrentLayerClips { get; private set; }
        #endregion
        #region Before
        private bool beforePlaying;
        private AnimationClip beforeClip;
        private float beforeTime;
        private float beforeLength;
        private Tool beforeCurrentTool;
        private bool beforeShowSceneGizmo;
        private EditorWindow beforeMouseOverWindow;
        private EditorWindow beforeFocusedWindow;
        private bool beforeEnableHumanoidFootIK;
        private bool beforeEnableAnimationRiggingIK;
#pragma warning disable 0414
        private bool beforeRemoveStartOffset;
        private Vector3 beforeOffsetPosition;
        private Quaternion beforeOffsetRotation;
        private AnimationClipSettings beforeAnimationClipSettings;
#pragma warning restore 0414
        #endregion

        #region Refresh
        public enum AnimationWindowStateRefreshType
        {
            None,
            CurvesOnly,
            Everything,
        }
        private AnimationWindowStateRefreshType animationWindowRefresh;
        public void SetAnimationWindowRefresh(AnimationWindowStateRefreshType type)
        {
            if (type > animationWindowRefresh)
                animationWindowRefresh = type;
        }
        private bool updateSampleAnimation;
        private bool updatePoseFixAnimation;
        private bool updateStartTransform;
        private bool updateSaveForce;
        #endregion

        #region AnimationWindow
        private List<EditorCurveBinding> animationWindowFilterBindings;
        private bool animationWindowSynchroSelection;
        private List<EditorCurveBinding> animationWindowSynchroSelectionBindings;
        #endregion

        #region CopyPaste
        private enum CopyDataType
        {
            None = -1,
            SelectionPose,
            FullPose,
            AnimatorIKTarget,
            OriginalIKTarget,
        }
        private CopyDataType copyDataType = CopyDataType.None;

        private PoseTemplate copyPaste;

        private class CopyAnimatorIKTargetData
        {
            public AnimatorIKCore.IKTarget ikTarget;
            public bool autoRotation;
            public AnimatorIKCore.AnimatorIKData.SpaceType spaceType;
            public GameObject parent;
            public Vector3 position;
            public Quaternion rotation;
            public float swivelRotation;
        }
        private CopyAnimatorIKTargetData[] copyAnimatorIKTargetData;

        private class CopyOriginalIKTargetData
        {
            public int ikTarget;
            public bool autoRotation;
            public OriginalIKCore.OriginalIKData.SpaceType spaceType;
            public GameObject parent;
            public Vector3 position;
            public Quaternion rotation;
            public float swivel;
        }
        private CopyOriginalIKTargetData[] copyOriginalIKTargetData;
        #endregion

        #region EditorWindow
        public bool optionsClampMuscle;
        public bool optionsAutoFootIK;
        public bool optionsMirror;
        public enum RootCorrectionMode
        {
            Disable,
            Single,
            Full,
            Total
        }
        public RootCorrectionMode rootCorrectionMode;

        public bool extraOptionsCollision;
        public bool extraOptionsSynchronizeAnimation;
        public bool extraOptionsOnionSkin;
        public bool extraOptionsRootTrail;
        #endregion

        #region ControlWindow
        public List<VeryAnimationSaveSettings.SelectionData> selectionSetList;
        #endregion

        #region AnimationWindow
        private EditorWindow autoLockedAnimationWindow;
#if VERYANIMATION_TIMELINE
        private EditorWindow autoLockedTimelineWindow;
#endif
        #endregion

        public void OnEnable()
        {
            IsEdit = false;
            animationMode = AnimationMode.Single;
#if UNITY_6000_3_OR_NEWER
            UAw = UAw_2023_1 = UAw_6000_3 = new UAnimationWindow_6000_3();
            UAnimationWindowUtility = UAnimationWindowUtility_2023_1 = new UAnimationWindowUtility_2023_1();
            UEditorWindow = UEditorWindow_2023_1 = new UEditorWindow_2023_1();
            URotationCurveInterpolation = URotationCurveInterpolation_6000_3 = new URotationCurveInterpolation_6000_3();
#elif UNITY_2023_1_OR_NEWER
            UAw = UAw_2023_1 = new UAnimationWindow_2023_1();
            UAnimationWindowUtility = UAnimationWindowUtility_2023_1 = new UAnimationWindowUtility_2023_1();
            UEditorWindow = UEditorWindow_2023_1 = new UEditorWindow_2023_1();
            URotationCurveInterpolation = new URotationCurveInterpolation();
#else
            UAw = new UAnimationWindow();
            UAnimationWindowUtility = new UAnimationWindowUtility();
            UEditorWindow = new UEditorWindow();
            URotationCurveInterpolation = new URotationCurveInterpolation();
#endif
            UAvatar = new UAvatar();
            UAnimator = new UAnimator();
            UAnimatorControllerTool = new UAnimatorControllerTool();
            UAnimationUtility = new UAnimationUtility();
            UCurveUtility = new UCurveUtility();
            USceneView = new USceneView();
            UAnimationMode = new UAnimationMode();

            MusclePropertyName = new MusclePropertyName();
#if VERYANIMATION_ANIMATIONRIGGING
            AnimationRigging = new AnimationRigging();
#endif

            LastTool = Tools.current;

            CreateEditorCurveBindingPropertyNames();

            OnBoneShowFlagsUpdated += UpdateSkeletonShowBoneList;

            InternalEditorUtility.RepaintAllViews();
        }
        public void OnDisable()
        {
            OnBoneShowFlagsUpdated -= UpdateSkeletonShowBoneList;
        }
        public void OnDestroy()
        {
        }

        public void Initialize()
        {
            Release();

            UpdateCurrentInfo();

            StopAllRecording();

            IsEdit = true;

            #region AutoLock
            {
                autoLockedAnimationWindow = null;
#if VERYANIMATION_TIMELINE
                autoLockedTimelineWindow = null;
#endif
                if (UAw.GetLinkedWithTimeline())
                {
#if VERYANIMATION_TIMELINE
                    if (!UAw.UTimelineWindow.GetLock(UAw.UTimelineWindow.Instance))
                    {
                        UAw.UTimelineWindow.SetLock(UAw.UTimelineWindow.Instance, true);
                        autoLockedTimelineWindow = UAw.UTimelineWindow.Instance;
                    }
#endif
                }
                else
                {
                    if (!UAw.GetLock(UAw.Instance))
                    {
                        UAw.SetLock(UAw.Instance, true);
                        autoLockedAnimationWindow = UAw.Instance;
                    }
                }
            }
            #endregion

            beforeCurrentTool = LastTool = Tools.current;
            beforeShowSceneGizmo = false;
            beforeMouseOverWindow = null;
            beforeFocusedWindow = null;
            beforeEnableHumanoidFootIK = false;
            beforeEnableAnimationRiggingIK = false;
            beforeRemoveStartOffset = UAw.GetRemoveStartOffset();
            beforeOffsetPosition = Vector3.zero;
            beforeOffsetRotation = Quaternion.identity;
            beforeAnimationClipSettings = null;

            #region Animator
            UnityEditor.Animations.AnimatorController ac = null;
            if (VAW.Animator != null)
            {
                if (!VAW.Animator.isInitialized)
                    VAW.Animator.Rebind();
                ac = EditorCommon.GetAnimatorController(VAW.Animator);
            }
            #endregion

            #region AnimationWindow
            animationWindowFilterBindings = null;
            animationWindowSynchroSelectionBindings = new List<EditorCurveBinding>();
            #endregion

            UpdateBones(true);

            #region PreviewDefaultSettings
            {
                #region AvatarpreviewShowIK
                if (UAw.GetLinkedWithTimeline())
                {
#if VERYANIMATION_TIMELINE
                    EditorPrefs.SetBool("AvatarpreviewShowIK", UAw.GetTimelineAnimationApplyFootIK());
#else
                    Assert.IsTrue(false);
#endif
                }
                else if (VAW.Animator != null && ac != null && ac.layers.Length > 0)
                {
                    bool enable = false;
                    if (EditorApplication.isPlaying)
                    {
                        var state = VAW.Animator.GetCurrentAnimatorStateInfo(0);
                        var index = ArrayUtility.FindIndex(ac.layers[0].stateMachine.states, (x) => x.state.nameHash == state.shortNameHash);
                        if (index >= 0)
                            enable = ac.layers[0].stateMachine.states[index].state.iKOnFeet;
                    }
                    else
                    {
                        foreach (var layer in ac.layers)
                        {
                            bool FindMotion(Motion motion)
                            {
                                if (motion != null)
                                {
                                    if (motion is UnityEditor.Animations.BlendTree)
                                    {
                                        var blendTree = motion as UnityEditor.Animations.BlendTree;
                                        foreach (var c in blendTree.children)
                                        {
                                            if (FindMotion(c.motion))
                                                return true;
                                        }
                                    }
                                    else
                                    {
                                        if (motion == CurrentClip)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                return false;
                            }

                            foreach (var state in layer.stateMachine.states)
                            {
                                if (FindMotion(state.state.motion))
                                {
                                    enable = state.state.iKOnFeet;
                                    break;
                                }
                            }
                        }
                    }
                    EditorPrefs.SetBool("AvatarpreviewShowIK", enable);
                }
                else
                {
                    EditorPrefs.SetBool("AvatarpreviewShowIK", false);
                }
                #endregion
                if (VAW.Animator != null)
                {
                    EditorPrefs.SetBool(UAvatarPreview.EditorPrefsApplyRootMotion, UAw.GetLinkedWithTimeline() || VAW.Animator.applyRootMotion);
#if VERYANIMATION_ANIMATIONRIGGING
                    EditorPrefs.SetBool(UAvatarPreview.EditorPrefsARConstraint, AnimationRigging != null && AnimationRigging.IsValid);
#endif
                }
            }
            #endregion

            SelectGameObjectEvent();

            #region gameObjectBounds
            {
                bool first = true;
                var bounds = new Bounds();
                foreach (var renderer in VAW.GameObject.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;
                    if (first)
                    {
                        bounds = renderer.bounds;
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
                GameObjectBounds = bounds;
            }
            #endregion

            PrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

            InitializeAnimatorRootCorrection();
            InitializeHumanoidFootIK();
            InitializeAnimationPlayable();
            InitializeHandPoseSetList();
            InitializeBlendShapeSetList();

            selectionSetList = new List<VeryAnimationSaveSettings.SelectionData>();

            ResetOnCurveWasModifiedStop();

            if (EditorApplication.isPlaying &&
                VAW.PlayingAnimationInfos != null)
            {
                #region SetCurrentClipAndTime
                UpdateAnimationLayersInfo();
                CurrentLayerClips.Clear();
                foreach (var animationInfo in VAW.PlayingAnimationInfos)
                {
                    if (animationInfo.stateMachine != null)
                    {
                        CurrentLayerClips.Add(animationInfo.stateMachine, animationInfo.clip);
                    }
                }

                var info = VAW.PlayingAnimationInfos[0];
                UAw.SetSelectionAnimationClip(info.clip);
                var time = 0f;
                if (info.length > 0f)
                    time = (info.time / info.length) * info.clip.length;
                SetCurrentTime(time);
                UpdateCurrentInfo();
                if (VAW.Animator != null)
                {
                    var t = VAW.GameObject.transform;
                    SetCurrentTimeAndSampleAnimation(time);
                    var offsetRotation = TransformPoseSave.OriginalRotation * Quaternion.Inverse(t.rotation);
                    var offsetMatrix = TransformPoseSave.OriginalMatrix * t.worldToLocalMatrix;
                    SetCurrentTimeAndSampleAnimation(0f);
                    t.SetPositionAndRotation(offsetMatrix.MultiplyPoint3x4(TransformPoseSave.OriginalPosition), offsetRotation * TransformPoseSave.OriginalRotation);
                    TransformPoseSave = null;
                    UpdateBones(false);
                    SetCurrentTimeAndSampleAnimation(time);
                }
                #endregion
            }

            SetUpdateSampleAnimation();
            updateStartTransform = UAw.GetLinkedWithTimeline();

            #region Layers
            {
                ResetAnimationMode();
                Language.OnLanguageChanged += UpdateAnimationModeString;
            }
            #endregion

            Undo.undoRedoPerformed += UndoRedoPerformed;
            AnimationUtility.onCurveWasModified += OnCurveWasModified;
            EditorApplication.hierarchyChanged += OnHierarchyWindowChanged;
        }
        public void Release()
        {
            UpdateSyncEditorCurveClip();

            VAW.SetClipSelectorLayerIndex(-1);
            Language.OnLanguageChanged -= UpdateAnimationModeString;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            AnimationUtility.onCurveWasModified -= OnCurveWasModified;
            EditorApplication.hierarchyChanged -= OnHierarchyWindowChanged;

            StopRecording();

            IsEdit = false;
            SelectionGameObjects = null;
            SelectionBones = null;
            SelectionHumanVirtualBones = null;
            SelectionMotionTool = false;
            Renderers = null;
            IsHuman = false;
            AnimatorApplyRootMotion = false;
            AnimatorAvatar = null;
            AnimatorAvatarRoot = null;
            Bones = null;
            BoneDictionary = null;
            HumanoidBones = null;
            ParentBoneIndexes = null;
            BoneHierarchyLevels = null;
            BoneHierarchyMaxLevel = 0;
            MirrorBoneIndexes = null;
            MirrorBoneDatas = null;
            MirrorBlendShape = null;
            BoneIndex2humanoidIndex = null;
            HumanoidIndex2boneIndex = null;
            HumanoidConflict = null;
            BonePaths = null;
            BonePathDictionary = null;
            BoneDefaultPose = null;
            BoneSaveTransforms = null;
            BoneSaveOriginalTransforms = null;
            HumanoidMuscleLimit = null;
            HumanoidMuscleContains = null;
            HumanPoseHandler = null;
            IsHumanAvatarReady = false;
            PrefabMode = false;

            beforePlaying = false;
            beforeClip = null;
            beforeTime = 0f;
            beforeLength = 0f;

            animationWindowRefresh = AnimationWindowStateRefreshType.None;
            updateSampleAnimation = false;
            updatePoseFixAnimation = false;
            updateStartTransform = false;
            updateSaveForce = false;
            animationWindowFilterBindings = null;
            animationWindowSynchroSelection = false;
            animationWindowSynchroSelectionBindings = null;

            copyDataType = CopyDataType.None;
            copyPaste = null;
            copyAnimatorIKTargetData = null;
            copyOriginalIKTargetData = null;

            boneShowFlags = null;

            editorCurveCacheClip = null;
            editorCurveCacheDic = null;
            editorCurveDelayWriteDic = null;
            editorCurveWasModifiedDic = null;

            if (UAnimationClipEditor != null)
            {
                UAnimationClipEditor.Dispose();
                UAnimationClipEditor = null;
            }
            if (UAnimationClipEditorTotal != null)
            {
                UAnimationClipEditorTotal.Dispose();
                UAnimationClipEditorTotal = null;
            }
            if (UAvatarPreview != null)
            {
                UAvatarPreview.Dispose();
                UAvatarPreview = null;
            }

            animatorIK?.Release();   //Not to be null
            originalIK?.Release();   //Not to be null
#if VERYANIMATION_ANIMATIONRIGGING
            AnimationRigging?.Release();   //Not to be null
#endif
            if (synchronizeAnimation != null)
            {
                synchronizeAnimation.Dispose();
                synchronizeAnimation = null;
            }
            if (Skeleton != null)
            {
                Skeleton.Dispose();
                Skeleton = null;
            }
            if (OnionSkin != null)
            {
                OnionSkin.Dispose();
                OnionSkin = null;
            }

            curvesWasModified.Clear();

            selectionSetList = null;

            ReleaseAnimatorRootCorrection();
            ReleaseHumanoidFootIK();
            ReleaseCollision();
            ReleaseAnimationPlayable();
            ReleaseHandPoseSetList();
            ReleaseBlendShapeSetList();

            #region OriginalSave
            if (TransformPoseSave != null)
            {
                TransformPoseSave.ResetOriginalTransform();
                TransformPoseSave.ResetRootOriginalTransform();
                TransformPoseSave = null;
            }
            BlendShapeWeightSave?.ResetOriginalWeight();
            #endregion

            DisableCustomTools();

            if (UAw != null && UAw.GetLinkedWithTimeline())
            {
                UAw.StartPreviewing();
            }

            #region AutoLock
            if (UAw != null && autoLockedAnimationWindow != null)
            {
                UAw.SetLock(autoLockedAnimationWindow, false);
                autoLockedAnimationWindow = null;
            }
#if VERYANIMATION_TIMELINE
            if (UAw != null && autoLockedTimelineWindow != null)
            {
                Selection.activeObject = UAw.UTimelineWindow.GetCurrentDirector();

                UAw.UTimelineWindow.SetLock(autoLockedTimelineWindow, false);
                autoLockedTimelineWindow = null;
            }
#endif
            #endregion
        }

        private bool StartRecording()
        {
            bool result = true;
            if (!UAw.GetRecording())
            {
                if (!UAw.GetCanRecord() && UAw.GetPreviewing())
                {
                }
                else
                {
#if VERYANIMATION_TIMELINE
                    if (UAw.GetLinkedWithTimeline() && !UAw.IsTimelineArmedForRecord())
                    {
                        UAw.SetTimelineRecording(false);
                        result = UAw.StartRecording();
                    }
                    else
#endif
                    {
                        result = UAw.StartRecording();
                    }
                }
            }

            #region Unusual error
            if (!result)
            {
                Debug.LogError(Language.GetText(Language.Help.LogAnimationWindowRecordingStartError));
            }
            #endregion

            return result;
        }
        private void StopAllRecording()
        {
            UAw.CleanAnimationModeEvents();

            StopRecording();

            UAw.StopAllRecording();
        }
        public void StopRecording()
        {
            if (UAw == null) return;

            UAw.OnSelectionChange();    //Added to be sure to call StopPreview

#if VERYANIMATION_TIMELINE
            var preview = UAw.GetTimelinePreviewMode();
            UAw.SetTimelineRecording(false);
#endif
            UAw.StopRecording();

#if VERYANIMATION_TIMELINE
            if (preview)
                UAw.SetTimelinePreviewMode(true);
#endif
        }

        public void SetCurrentClip(AnimationClip clip)
        {
            UAw.SetSelectionAnimationClip(clip);
            CurrentClip = UAw.GetSelectionAnimationClip();
            if (UAw.GetPlaying())
                SetCurrentTime(0f);

            if (animationMode == AnimationMode.Layers)
            {
                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac != null && CurrentLayerClips != null)
                {
                    var layers = ac.layers;
                    if (animationLayerIndex >= 0 && animationLayerIndex < layers.Length)
                    {
                        if (!CurrentLayerClips.ContainsKey(layers[animationLayerIndex].stateMachine))
                            CurrentLayerClips.Add(layers[animationLayerIndex].stateMachine, CurrentClip);
                        else
                            CurrentLayerClips[layers[animationLayerIndex].stateMachine] = CurrentClip;
                    }
                }
            }
        }
        public void SetCurrentTime(float time)
        {
            UAw.SetCurrentTime(time);
            CurrentTime = UAw.GetCurrentTime();
        }
        public void UpdateCurrentInfo()
        {
            CurrentClip = UAw.GetSelectionAnimationClip();
            CurrentTime = UAw.GetCurrentTime();
            CurrentLinkedWithTimeline = UAw.GetLinkedWithTimeline();
        }

        public bool IsEditError
        {
            get
            {
                return !IsEdit || IsError;
            }
        }
        public bool IsError
        {
            get
            {
                return GetErrorCode < 0;
            }
        }
        public int GetErrorCode
        {
            get
            {
                if (UAw == null || UAw.Instance == null || !UAw.HasFocus() || CurrentClip == null)
                    return -1;
                if (VAW == null || VAW.GameObject == null || (VAW.Animator == null && VAW.Animation == null))
                    return -2;
                if (VAW.Animator != null && !VAW.Animator.hasTransformHierarchy)
                    return -3;
                if (Application.isPlaying && VAW.Animator != null && VAW.Animator.runtimeAnimatorController == null)
                    return -4;
                if (Application.isPlaying && VAW.Animation != null)
                    return -5;
                if (IsEdit && VAW.GameObject != UAw.GetActiveRootGameObject())
                    return -6;
                if (IsEdit && VAW.Animator != null && AnimatorApplyRootMotion != VAW.Animator.applyRootMotion)
                    return -7;
                if (IsEdit && VAW.Animator != null && AnimatorAvatar != VAW.Animator.avatar)
                    return -8;
                if (IsEdit && VAW.Animator != null && IsHuman && !IsHumanAvatarReady)
                    return -9;
                if (IsEdit && CurrentLinkedWithTimeline != UAw.GetLinkedWithTimeline())
                    return -10;
                if (IsEdit && (Bones == null || boneShowFlags == null || Bones.Length != boneShowFlags.Length))
                    return -11;
                if (!UAw.GetLinkedWithTimeline())
                {
                    if (!VAW.GameObject.activeInHierarchy)
                        return -110;
                    if (!IsEdit && VAW.Animator != null && VAW.Animator.runtimeAnimatorController != null && (VAW.Animator.runtimeAnimatorController.hideFlags & (HideFlags.DontSave | HideFlags.NotEditable)) != 0)
                        return -112;
                }
#if VERYANIMATION_TIMELINE
                else
                {
                    if (!IsEdit && !VAW.GameObject.activeInHierarchy)
                        return -120;
                    if (!UAw.GetTimelineTrackAssetEditable())
                        return -121;
                    if (Application.isPlaying)
                        return -122;
                    var currentDirector = UAw.GetTimelineCurrentDirector();
                    if (currentDirector != null)
                    {
                        if (!currentDirector.gameObject.activeInHierarchy)
                            return -123;
                        if (!currentDirector.enabled)
                            return -124;
                    }
                    if (!UAw.GetTimelineHasFocus())
                        return -125;
                }
#endif
                if (IsEdit && PrefabMode != (PrefabStageUtility.GetCurrentPrefabStage() != null))
                    return -140;
                if (VAW.UPrefabStage.GetAutoSave(PrefabStageUtility.GetCurrentPrefabStage()))
                    return -141;
                if (PrefabStageUtility.GetCurrentPrefabStage() != null &&
                    !EditorCommon.IsAncestorObject(UAw.GetActiveRootGameObject(), PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot))
                    return -142;
                if (IsEdit && VeryAnimationEditorWindow.instance == null)
                    return -1000;
                if (IsEdit && VeryAnimationControlWindow.instance == null)
                    return -1001;
                return 0;
            }
        }

        #region Update
        public void OnInspectorUpdate()
        {
            if (IsEditError) return;

            #region AnimationWindowRefresh
            if (animationWindowRefresh != AnimationWindowStateRefreshType.None)
            {
                if (animationWindowRefresh == AnimationWindowStateRefreshType.CurvesOnly)
                {

                }
                else if (animationWindowRefresh == AnimationWindowStateRefreshType.Everything)
                {
                    UAw.ForceRefresh();
                }
                animationWindowRefresh = AnimationWindowStateRefreshType.None;
            }
            #endregion

            #region SettingChange
            {
                var footIK = IsEnableUpdateHumanoidFootIK();
                var arIK = false;
#if VERYANIMATION_ANIMATIONRIGGING
                arIK = AnimationRigging.IsValid;
#endif
                if (beforeEnableHumanoidFootIK != footIK ||
                    beforeEnableAnimationRiggingIK != arIK)
                {
                    beforeEnableHumanoidFootIK = footIK;
                    beforeEnableAnimationRiggingIK = arIK;
                    UpdateSkeletonShowBoneList();
                }
            }
            if (UAw.GetLinkedWithTimeline())
            {
#if VERYANIMATION_TIMELINE
                {
                    var removeStartOffset = UAw.GetRemoveStartOffset();
                    if (beforeRemoveStartOffset != removeStartOffset)
                    {
                        beforeRemoveStartOffset = removeStartOffset;
                        updateStartTransform = true;
                    }
                }
                {
                    UAw.GetTimelineRootMotionOffsets(out Vector3 offsetPosition, out Quaternion offsetRotation);
                    if (beforeOffsetPosition != offsetPosition || beforeOffsetRotation != offsetRotation)
                    {
                        beforeOffsetPosition = offsetPosition;
                        beforeOffsetRotation = offsetRotation;
                        updateStartTransform = true;
                    }
                }
                {
                    var animationClipSettings = AnimationUtility.GetAnimationClipSettings(CurrentClip);
                    if (beforeAnimationClipSettings == null ||
                        beforeAnimationClipSettings.keepOriginalPositionXZ != animationClipSettings.keepOriginalPositionXZ ||
                        beforeAnimationClipSettings.keepOriginalPositionY != animationClipSettings.keepOriginalPositionY ||
                        beforeAnimationClipSettings.keepOriginalOrientation != animationClipSettings.keepOriginalOrientation ||
                        beforeAnimationClipSettings.orientationOffsetY != animationClipSettings.orientationOffsetY ||
                        beforeAnimationClipSettings.level != animationClipSettings.level)
                    {
                        beforeAnimationClipSettings = animationClipSettings;
                        updateStartTransform = true;
                    }
                }
#else
                Assert.IsTrue(false);
#endif
            }
            #endregion
        }
        public void Update()
        {
            UpdateCurrentInfo();

            if (IsEditError) return;

#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("****VeryAnimation.Update");
#endif

            bool awForceRefresh = false;

            UpdateSyncEditorCurveClip();

            #region SnapToFrame
            if (!UAw.GetPlaying())
            {
                var snapTime = UAw.SnapToFrame(CurrentTime, CurrentClip.frameRate);
                if (CurrentTime != snapTime)
                {
                    SetCurrentTime(snapTime);
                }
            }
            #endregion

            #region RecordingChange
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("RecordingChange");
#endif
            {
                if (!StartRecording())
                {
                    EditorApplication.delayCall += () =>
                    {
                        VAW.Release();
                    };
                    return;
                }
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region PlayingChange
            if (beforePlaying != UAw.GetPlaying())
            {
                SetUpdateSampleAnimation();
                beforePlaying = UAw.GetPlaying();
                UAw.Repaint();
            }
            #endregion

            #region ClipOrLengthChange
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("ClipChange");
#endif
            {
                if (CurrentClip != null && (CurrentClip != beforeClip || beforeLength != CurrentClip.length || UAvatarPreview == null || UAnimationClipEditor == null || UAnimationClipEditorTotal == null))
                {
                    TransformPoseSave?.ResetOriginalTransform();
                    BlendShapeWeightSave?.ResetOriginalWeight();
                    {
                        if (UAnimationClipEditor != null)
                        {
                            UAnimationClipEditor.Dispose();
                            UAnimationClipEditor = null;
                        }
                        if (UAvatarPreview != null)
                        {
                            var previewDir = UAvatarPreview.PreviewDir;
                            var zoomFactor = UAvatarPreview.ZoomFactor;
                            var playing = UAvatarPreview.Playing;
                            UAvatarPreview.Dispose();
                            UAvatarPreview = new UAvatarPreview(CurrentClip, VAW.GameObject, animationMode == AnimationMode.Layers ? CurrentLayerClips : null);
                            UAvatarPreview.SetTime(CurrentTime);
                            UAvatarPreview.PreviewDir = previewDir;
                            UAvatarPreview.ZoomFactor = zoomFactor;
                            UAvatarPreview.Playing = playing;
                        }
                        else
                        {
                            UAvatarPreview = new UAvatarPreview(CurrentClip, VAW.GameObject, animationMode == AnimationMode.Layers ? CurrentLayerClips : null);
                            UAvatarPreview.SetTime(CurrentTime);
                        }
                        UAvatarPreview.onAvatarChange += () =>
                        {
                            TransformPoseSave?.ResetOriginalTransform();
                            BlendShapeWeightSave?.ResetOriginalWeight();
                            SetUpdateSampleAnimation();
                        };
                        UAnimationClipEditor = new UAnimationClipEditor(CurrentClip, UAvatarPreview);
                    }
                    {
                        if (UAnimationClipEditorTotal != null)
                        {
                            UAnimationClipEditorTotal.Dispose();
                            UAnimationClipEditorTotal = null;
                        }
                        var timeLength = CurrentClip.length;
                        {
                            if (UAw.GetLinkedWithTimeline())
                            {
#if VERYANIMATION_TIMELINE
                                var director = UAw.GetTimelineCurrentDirector();
                                timeLength = (float)director.duration;
#endif
                            }
                            else if (animationMode == AnimationMode.Layers)
                            {
                                timeLength = GetTotalClipLength();
                            }
                        }
                        UAnimationClipEditorTotal = new UAnimationClipEditor(timeLength, CurrentClip.frameRate);
                    }
                    ClearEditorCurveCache();
                    SetUpdateSampleAnimation(true, true);
                    SetSynchroIKtargetAll();
                    SetAnimationWindowSynchroSelection();
                    SetSynchronizeAnimation(extraOptionsSynchronizeAnimation);
                    beforeClip = CurrentClip;
                    beforeTime = -1f;
                    beforeLength = CurrentClip.length;
                    ToolsReset();

                    #region Layers
                    if (animationMode == AnimationMode.Layers)
                    {
                        var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                        if (ac != null)
                        {
                            var layers = ac.layers;
                            if (animationLayerIndex >= 0 && animationLayerIndex < layers.Length)
                            {
                                var layerClips = GetLayerAnimationClips(animationLayerIndex);
                                CurrentLayerClips.TryGetValue(layers[animationLayerIndex].stateMachine, out AnimationClip clip);
                                if (!layerClips.Contains(clip))
                                {
                                    CurrentLayerClips.Remove(layers[animationLayerIndex].stateMachine);
                                    for (int i = 0; i < layers.Length; i++)
                                    {
                                        layerClips = GetLayerAnimationClips(i);
                                        if (layerClips.Contains(CurrentClip))
                                        {
                                            SetAnimationLayerIndex(i);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region TimeChange
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("TimeChange");
#endif
            {
                if (CurrentTime != beforeTime)
                {
                    if (!UAw.GetPlaying())
                    {
                        SetUpdateSampleAnimation();
                        SetSynchroIKtargetAll();
                        if (UAvatarPreview != null &&
                            !UAvatarPreview.Playing)
                        {
                            UAvatarPreview.SetTime(CurrentTime);
                        }
                    }
                    UAnimationClipEditorTotal?.SetDummyCursorTime(CurrentTime, CurrentClip.frameRate);
                    synchronizeAnimation?.SetTime(CurrentTime);
                    beforeTime = CurrentTime;
                }
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region GizmoChange
            if (beforeShowSceneGizmo != VAW.IsShowSceneGizmo())
            {
                if (!VAW.IsShowSceneGizmo())
                {
                    OnionSkin.Dispose();
                }
                SetUpdateSampleAnimation();
                SetSynchroIKtargetAll();
                beforeShowSceneGizmo = VAW.IsShowSceneGizmo();
            }
            #endregion

            #region ToolChange
            if (Tools.current == Tool.View)
            {
                if (beforeCurrentTool != Tools.current)
                {
                    beforeCurrentTool = Tools.current;
                    VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
                }
            }
            else if (Tools.current == Tool.None)
            {
                if (beforeCurrentTool != LastTool)
                {
                    SetAnimationWindowSynchroSelection();
                    beforeCurrentTool = LastTool;
                    VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
                }
            }
            else
            {
                if (beforeCurrentTool != Tools.current)
                {
                    SetAnimationWindowSynchroSelection();
                    beforeCurrentTool = Tools.current;
                    EnableCustomTools(Tool.None);
                    VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
                }
            }
            #endregion

            #region WindowChange
            {
                var mouseOverWindow = EditorWindow.mouseOverWindow;
                var focusedWindow = EditorWindow.focusedWindow;
                if (mouseOverWindow != beforeMouseOverWindow || focusedWindow != beforeFocusedWindow)
                {
                    #region SceneView
                    if (mouseOverWindow is SceneView)
                    {
                        if (Tools.current != Tool.View)
                            EnableCustomTools(Tool.None);
                    }
                    #endregion

                    #region AnimationWindow
                    if (mouseOverWindow == UAw.Instance || focusedWindow == UAw.Instance)
                    {
                        editorCurveCacheDirty = true;
                    }
                    #endregion

                    beforeMouseOverWindow = mouseOverWindow;
                    beforeFocusedWindow = focusedWindow;
                }
            }
            #endregion

            #region AnimationWindow
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("AnimationWindow");
#endif
            if (animationWindowSynchroSelection)
            {
                var animationWindowRefreshDone = UAw.IsDoneRefresh();

                #region Disable AnimationWindow Filter
                if (animationWindowRefreshDone)
                {
                    if (VAW.EditorSettings.SettingPropertyStyle == EditorSettings.PropertyStyle.Filter)
                    {
                        if (UAw.GetFilterBySelection())
                        {
                            UAw.SetFilterBySelection(false);
                            animationWindowRefreshDone = UAw.IsDoneRefresh();
                        }
                    }
                }
                #endregion

                if (animationWindowRefreshDone)
                {
                    if (EditorWindow.focusedWindow != UAw.Instance)
                    {
                        SelectGameObjectEvent();    //UpdateSelection

                        #region SyncSelection
                        var syncBindings = new List<EditorCurveBinding>(animationWindowSynchroSelectionBindings);
                        {
                            void AddGeneric(Tool currentTool, int boneIndex)
                            {
                                switch (currentTool)
                                {
                                    case Tool.Move:
                                        for (int dof = 0; dof < 3; dof++)
                                            syncBindings.Add(AnimationCurveBindingTransformPosition(boneIndex, dof));
                                        break;
                                    case Tool.Rotate:
                                        for (int dof = 0; dof < 3; dof++)
                                            syncBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.Baked));
                                        for (int dof = 0; dof < 3; dof++)
                                            syncBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.NonBaked));
                                        for (int dof = 0; dof < 4; dof++)
                                            syncBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                                        for (int dof = 0; dof < 3; dof++)
                                            syncBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));
                                        break;
                                    case Tool.Scale:
                                        for (int dof = 0; dof < 3; dof++)
                                            syncBindings.Add(AnimationCurveBindingTransformScale(boneIndex, dof));
                                        break;
                                }
                            }

                            Tool tool = CurrentTool();

                            #region Humanoid
                            if (IsHuman)
                            {
                                void AddMuscle(HumanBodyBones humanoidIndex)
                                {
                                    switch (tool)
                                    {
                                        case Tool.Move:
                                            if (HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] != null)
                                            {
                                                for (int dof = 0; dof < 3; dof++)
                                                    syncBindings.Add(AnimationCurveBindingAnimatorTDOF(HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index, dof));
                                            }
                                            break;
                                        case Tool.Rotate:
                                            for (int dof = 0; dof < 3; dof++)
                                            {
                                                var muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, dof);
                                                if (muscleIndex < 0) continue;
                                                syncBindings.Add(AnimationCurveBindingAnimatorMuscle(muscleIndex));
                                            }
                                            break;
                                    }
                                }
                                {
                                    foreach (var go in SelectionGameObjects)
                                    {
                                        HumanBodyBones humanoidIndex;
                                        if (VAW.GameObject == go)
                                        {
                                            switch (tool)
                                            {
                                                case Tool.Move:
                                                    foreach (var binding in AnimationCurveBindingAnimatorRootT)
                                                        syncBindings.Add(binding);
                                                    break;
                                                case Tool.Rotate:
                                                    foreach (var binding in AnimationCurveBindingAnimatorRootQ)
                                                        syncBindings.Add(binding);
                                                    break;
                                            }
                                        }
                                        else if ((humanoidIndex = HumanoidBonesIndexOf(go)) >= 0)
                                        {
                                            AddMuscle(humanoidIndex);
                                        }
                                    }
                                }
                                if (SelectionHumanVirtualBones != null)
                                {
                                    foreach (var humanoidIndex in SelectionHumanVirtualBones)
                                    {
                                        AddMuscle(humanoidIndex);
                                    }
                                }
                                #region AnimatorIK
                                if (animatorIK.ikTargetSelect != null)
                                {
                                    foreach (var ikTarget in animatorIK.ikTargetSelect)
                                    {
                                        var data = animatorIK.ikData[(int)ikTarget];
                                        if (!data.enable) continue;
                                        switch (ikTarget)
                                        {
                                            case AnimatorIKCore.IKTarget.Head:
                                                if (data.headWeight > 0f)
                                                {
                                                    AddMuscle(HumanBodyBones.Head);
                                                    AddMuscle(HumanBodyBones.Neck);
                                                }
                                                if (data.eyesWeight > 0f)
                                                {
                                                    AddMuscle(HumanBodyBones.LeftEye);
                                                    AddMuscle(HumanBodyBones.RightEye);
                                                }
                                                break;
                                            case AnimatorIKCore.IKTarget.LeftHand:
                                                AddMuscle(HumanBodyBones.LeftHand);
                                                AddMuscle(HumanBodyBones.LeftLowerArm);
                                                AddMuscle(HumanBodyBones.LeftUpperArm);
                                                if (data.enableShoulder)
                                                    AddMuscle(HumanBodyBones.LeftShoulder);
                                                break;
                                            case AnimatorIKCore.IKTarget.RightHand:
                                                AddMuscle(HumanBodyBones.RightHand);
                                                AddMuscle(HumanBodyBones.RightLowerArm);
                                                AddMuscle(HumanBodyBones.RightUpperArm);
                                                if (data.enableShoulder)
                                                    AddMuscle(HumanBodyBones.RightShoulder);
                                                break;
                                            case AnimatorIKCore.IKTarget.LeftFoot:
                                                AddMuscle(HumanBodyBones.LeftFoot);
                                                AddMuscle(HumanBodyBones.LeftLowerLeg);
                                                AddMuscle(HumanBodyBones.LeftUpperLeg);
                                                AddMuscle(HumanBodyBones.LeftToes);
                                                break;
                                            case AnimatorIKCore.IKTarget.RightFoot:
                                                AddMuscle(HumanBodyBones.RightFoot);
                                                AddMuscle(HumanBodyBones.RightLowerLeg);
                                                AddMuscle(HumanBodyBones.RightUpperLeg);
                                                AddMuscle(HumanBodyBones.RightToes);
                                                break;
                                        }
#if VERYANIMATION_ANIMATIONRIGGING
                                        #region AnimationRigging
                                        switch (ikTarget)
                                        {
                                            case AnimatorIKCore.IKTarget.Head:
                                                {
                                                    var constraint = data.rigConstraint as MultiAimConstraint;
                                                    if (constraint != null)
                                                    {
                                                        if (data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                                        {
                                                            foreach (var item in constraint.data.sourceObjects)
                                                            {
                                                                var boneIndex = BonesIndexOf(item.transform.gameObject);
                                                                if (boneIndex >= 0)
                                                                {
                                                                    AddGeneric(Tool.Move, boneIndex);
                                                                }
                                                            }
                                                        }
                                                        syncBindings.Add(data.rigConstraintWeight);
                                                    }
                                                }
                                                break;
                                            case AnimatorIKCore.IKTarget.LeftHand:
                                            case AnimatorIKCore.IKTarget.RightHand:
                                            case AnimatorIKCore.IKTarget.LeftFoot:
                                            case AnimatorIKCore.IKTarget.RightFoot:
                                                {
                                                    var constraint = data.rigConstraint as TwoBoneIKConstraint;
                                                    if (constraint != null)
                                                    {
                                                        if (constraint.data.target != null &&
                                                            data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                                        {
                                                            var boneIndex = BonesIndexOf(constraint.data.target.gameObject);
                                                            if (boneIndex >= 0)
                                                            {
                                                                AddGeneric(Tool.Move, boneIndex);
                                                                AddGeneric(Tool.Rotate, boneIndex);
                                                            }
                                                        }
                                                        if (constraint.data.hint != null &&
                                                            data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingHint))
                                                        {
                                                            var boneIndex = BonesIndexOf(constraint.data.hint.gameObject);
                                                            if (boneIndex >= 0)
                                                                AddGeneric(Tool.Move, boneIndex);
                                                        }
                                                        syncBindings.Add(data.rigConstraintWeight);
                                                    }
                                                }
                                                break;
                                        }
                                        #endregion
#endif
                                    }
                                }
                                #endregion
                            }
                            #endregion
                            #region Generic
                            {
                                if (SelectionBones != null)
                                {
                                    foreach (var boneIndex in SelectionBones)
                                    {
                                        AddGeneric(tool, boneIndex);
                                    }
                                }
                                #region OriginalIK
                                if (originalIK.ikTargetSelect != null)
                                {
                                    foreach (var ikTarget in originalIK.ikTargetSelect)
                                    {
                                        if (ikTarget < 0 || ikTarget >= originalIK.ikData.Count) continue;
                                        if (!originalIK.ikData[ikTarget].enable) continue;
                                        for (int i = 0; i < originalIK.ikData[ikTarget].joints.Count; i++)
                                        {
                                            var boneIndex = BonesIndexOf(originalIK.ikData[ikTarget].joints[i].bone);
                                            if (boneIndex >= 0)
                                                AddGeneric(tool, boneIndex);
                                        }
                                    }
                                }
                                #endregion
                            }
                            #endregion
                            #region Motion
                            if (SelectionMotionTool)
                            {
                                switch (tool)
                                {
                                    case Tool.Move:
                                        foreach (var binding in AnimationCurveBindingAnimatorMotionT)
                                            syncBindings.Add(binding);
                                        break;
                                    case Tool.Rotate:
                                        foreach (var binding in AnimationCurveBindingAnimatorMotionQ)
                                            syncBindings.Add(binding);
                                        break;
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region PropertyFilterByBindings
                        if (VAW.EditorSettings.SettingPropertyStyle == EditorSettings.PropertyStyle.Filter)
                        {
                            if (syncBindings.Count > 0)
                            {
                                var filterBindings = new HashSet<EditorCurveBinding>(syncBindings);
                                void AddTransformPositionBindings(int boneIndex)
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingTransformPosition(boneIndex, dof));
                                }
                                void AddTransformRotationBindings(int boneIndex)
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.Baked));
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.NonBaked));
                                    for (int dof = 0; dof < 4; dof++)
                                        filterBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));
                                }
                                void AddTransformScaleBindings(int boneIndex)
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingTransformScale(boneIndex, dof));
                                }
                                void AddAnimatorIKBindings()
                                {
                                    void AddHumanoidBindings(HumanBodyBones humanoidIndex)
                                    {
                                        if (HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] != null)
                                        {
                                            for (int dof = 0; dof < 3; dof++)
                                                filterBindings.Add(AnimationCurveBindingAnimatorTDOF(HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index, dof));
                                        }
                                        for (int dof = 0; dof < 3; dof++)
                                        {
                                            var muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, dof);
                                            if (muscleIndex < 0) continue;
                                            filterBindings.Add(AnimationCurveBindingAnimatorMuscle(muscleIndex));
                                        }
                                    }
                                    for (int i = 0; i < animatorIK.ikData.Length; i++)
                                    {
                                        var data = animatorIK.ikData[i];
                                        if (!data.enable)
                                            continue;
                                        if (data.spaceType == AnimatorIKCore.AnimatorIKData.SpaceType.Local)
                                            continue;
                                        switch ((AnimatorIKCore.IKTarget)i)
                                        {
                                            case AnimatorIKCore.IKTarget.Head:
                                                if (data.headWeight > 0f)
                                                {
                                                    AddHumanoidBindings(HumanBodyBones.Head);
                                                    AddHumanoidBindings(HumanBodyBones.Neck);
                                                }
                                                if (data.eyesWeight > 0f)
                                                {
                                                    AddHumanoidBindings(HumanBodyBones.LeftEye);
                                                    AddHumanoidBindings(HumanBodyBones.RightEye);
                                                }
                                                break;
                                            case AnimatorIKCore.IKTarget.LeftHand:
                                                AddHumanoidBindings(HumanBodyBones.LeftHand);
                                                AddHumanoidBindings(HumanBodyBones.LeftLowerArm);
                                                AddHumanoidBindings(HumanBodyBones.LeftUpperArm);
                                                if (data.enableShoulder)
                                                    AddHumanoidBindings(HumanBodyBones.LeftShoulder);
                                                break;
                                            case AnimatorIKCore.IKTarget.RightHand:
                                                AddHumanoidBindings(HumanBodyBones.RightHand);
                                                AddHumanoidBindings(HumanBodyBones.RightLowerArm);
                                                AddHumanoidBindings(HumanBodyBones.RightUpperArm);
                                                if (data.enableShoulder)
                                                    AddHumanoidBindings(HumanBodyBones.RightShoulder);
                                                break;
                                            case AnimatorIKCore.IKTarget.LeftFoot:
                                                AddHumanoidBindings(HumanBodyBones.LeftFoot);
                                                AddHumanoidBindings(HumanBodyBones.LeftLowerLeg);
                                                AddHumanoidBindings(HumanBodyBones.LeftUpperLeg);
                                                AddHumanoidBindings(HumanBodyBones.LeftToes);
                                                break;
                                            case AnimatorIKCore.IKTarget.RightFoot:
                                                AddHumanoidBindings(HumanBodyBones.RightFoot);
                                                AddHumanoidBindings(HumanBodyBones.RightLowerLeg);
                                                AddHumanoidBindings(HumanBodyBones.RightUpperLeg);
                                                AddHumanoidBindings(HumanBodyBones.RightToes);
                                                break;
                                        }
#if VERYANIMATION_ANIMATIONRIGGING
                                        #region AnimationRigging
                                        switch ((AnimatorIKCore.IKTarget)i)
                                        {
                                            case AnimatorIKCore.IKTarget.Head:
                                                {
                                                    var constraint = data.rigConstraint as MultiAimConstraint;
                                                    if (constraint != null)
                                                    {
                                                        if (data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                                        {
                                                            foreach (var item in constraint.data.sourceObjects)
                                                            {
                                                                var boneIndex = BonesIndexOf(item.transform.gameObject);
                                                                if (boneIndex >= 0)
                                                                {
                                                                    AddTransformPositionBindings(boneIndex);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                break;
                                            case AnimatorIKCore.IKTarget.LeftHand:
                                            case AnimatorIKCore.IKTarget.RightHand:
                                            case AnimatorIKCore.IKTarget.LeftFoot:
                                            case AnimatorIKCore.IKTarget.RightFoot:
                                                {
                                                    var constraint = data.rigConstraint as TwoBoneIKConstraint;
                                                    if (constraint != null)
                                                    {
                                                        if (constraint.data.target != null &&
                                                            data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                                        {
                                                            var boneIndex = BonesIndexOf(constraint.data.target.gameObject);
                                                            if (boneIndex >= 0)
                                                            {
                                                                AddTransformPositionBindings(boneIndex);
                                                                AddTransformRotationBindings(boneIndex);
                                                            }
                                                        }
                                                        if (constraint.data.hint != null &&
                                                            data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingHint))
                                                        {
                                                            var boneIndex = BonesIndexOf(constraint.data.hint.gameObject);
                                                            if (boneIndex >= 0)
                                                                AddTransformPositionBindings(boneIndex);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                        #endregion
#endif
                                    }
                                }
                                void AddOriginalIKBindings(int boneIndex = -1)
                                {
                                    void AddGenericBindings(int index)
                                    {
                                        for (int dof = 0; dof < 3; dof++)
                                            filterBindings.Add(AnimationCurveBindingTransformRotation(index, dof, URotationCurveInterpolation.Mode.Baked));
                                        for (int dof = 0; dof < 3; dof++)
                                            filterBindings.Add(AnimationCurveBindingTransformRotation(index, dof, URotationCurveInterpolation.Mode.NonBaked));
                                        for (int dof = 0; dof < 4; dof++)
                                            filterBindings.Add(AnimationCurveBindingTransformRotation(index, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                                        for (int dof = 0; dof < 3; dof++)
                                            filterBindings.Add(AnimationCurveBindingTransformRotation(index, dof, URotationCurveInterpolation.Mode.RawEuler));
                                    }

                                    bool IsChildIK(int rootBoneIndex, int index)
                                    {
                                        for (int i = 0; i < Bones[index].transform.childCount; i++)
                                        {
                                            var cIndex = BonesIndexOf(Bones[index].transform.GetChild(i).gameObject);
                                            if (rootBoneIndex == cIndex)
                                                return true;
                                            if (IsChildIK(rootBoneIndex, cIndex))
                                                return true;
                                        }
                                        return false;
                                    }
                                    for (int ikTarget = 0; ikTarget < originalIK.ikData.Count; ikTarget++)
                                    {
                                        var data = originalIK.ikData[ikTarget];
                                        if (!data.enable)
                                            continue;
                                        if (data.spaceType == OriginalIKCore.OriginalIKData.SpaceType.Local)
                                            continue;
                                        if (boneIndex >= 0 && !IsChildIK(data.rootBoneIndex, boneIndex))
                                            continue;
                                        for (int i = 0; i < data.joints.Count; i++)
                                        {
                                            var jBoneIndex = BonesIndexOf(data.joints[i].bone);
                                            if (jBoneIndex >= 0)
                                                AddGenericBindings(jBoneIndex);
                                        }
                                    }
                                }
                                void AddRootTBindings()
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingAnimatorRootT[dof]);
                                }
                                void AddRootQBindings()
                                {
                                    for (int dof = 0; dof < 4; dof++)
                                        filterBindings.Add(AnimationCurveBindingAnimatorRootQ[dof]);
                                }
                                void AddMotionTBindings()
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                        filterBindings.Add(AnimationCurveBindingAnimatorMotionT[dof]);
                                }
                                void AddMotionQBindings()
                                {
                                    for (int dof = 0; dof < 4; dof++)
                                        filterBindings.Add(AnimationCurveBindingAnimatorMotionQ[dof]);
                                }
                                void AddFootIKBindings()
                                {
                                    if (IsEnableUpdateHumanoidFootIK())
                                    {
                                        for (var ik = AnimatorIKIndex.LeftFoot; ik <= AnimatorIKIndex.RightFoot; ik++)
                                        {
                                            for (int dof = 0; dof < 3; dof++)
                                                filterBindings.Add(AnimationCurveBindingAnimatorIkT(ik, dof));
                                            for (int dof = 0; dof < 4; dof++)
                                                filterBindings.Add(AnimationCurveBindingAnimatorIkQ(ik, dof));
                                        }
                                    }
                                }
                                void AddMuscleBindings(HumanBodyBones humanoidIndex)
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                    {
                                        var mi = HumanTrait.MuscleFromBone((int)humanoidIndex, dof);
                                        if (mi < 0)
                                            continue;
                                        filterBindings.Add(AnimationCurveBindingAnimatorMuscle(mi));
                                    }
                                }
                                void AddTDofBindings(HumanBodyBones humanoidIndex)
                                {
                                    if (HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] != null)
                                    {
                                        var tdof = HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index;
                                        for (int dof = 0; dof < 3; dof++)
                                            filterBindings.Add(AnimationCurveBindingAnimatorTDOF(tdof, dof));
                                    }
                                }
                                #region Bone binding
                                foreach (var binding in syncBindings)
                                {
                                    var boneIndex = GetBoneIndexFromCurveBinding(binding);
                                    if (IsHuman && binding.type == typeof(Animator))
                                    {
                                        #region Humanoid
                                        if (boneIndex == 0)
                                        {
                                            AddRootTBindings();
                                            AddRootQBindings();
                                            AddFootIKBindings();
                                        }
                                        else if (boneIndex > 0 && BoneIndex2humanoidIndex[boneIndex] >= 0)
                                        {
                                            var humanoidIndex = BoneIndex2humanoidIndex[boneIndex];
                                            AddMuscleBindings(humanoidIndex);
                                            AddTDofBindings(humanoidIndex);
                                            if (rootCorrectionMode != RootCorrectionMode.Disable)
                                            {
                                                if (IsAnimatorRootCorrectionBone(humanoidIndex))
                                                {
                                                    AddRootTBindings();
                                                    AddRootQBindings();
                                                    AddFootIKBindings();
                                                }
                                            }
                                        }
                                        else if (boneIndex < 0) //virtual bone
                                        {
                                            var muscleIndex = GetMuscleIndexFromCurveBinding(binding);
                                            if (muscleIndex >= 0)
                                            {
                                                var humanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(muscleIndex);
                                                AddMuscleBindings(humanoidIndex);
                                                AddTDofBindings(humanoidIndex);
                                                if (rootCorrectionMode != RootCorrectionMode.Disable)
                                                {
                                                    if (IsAnimatorRootCorrectionBone(humanoidIndex))
                                                    {
                                                        AddRootTBindings();
                                                        AddRootQBindings();
                                                        AddFootIKBindings();
                                                    }
                                                }
                                            }
                                        }
                                        AddAnimatorIKBindings();
                                        AddOriginalIKBindings();
                                        #endregion
                                    }
                                    else if (boneIndex >= 0)
                                    {
                                        #region Generic & Legacy
                                        AddTransformPositionBindings(boneIndex);
                                        AddTransformRotationBindings(boneIndex);
                                        AddTransformScaleBindings(boneIndex);
                                        AddOriginalIKBindings(boneIndex);
                                        #endregion
                                    }
                                }
                                #endregion
                                #region Mirror binding
                                if (optionsMirror)
                                {
                                    var mirrorBindings = new List<EditorCurveBinding>();
                                    foreach (var binding in filterBindings)
                                    {
                                        var mbinding = GetMirrorAnimationCurveBinding(binding);
                                        if (!mbinding.HasValue)
                                            continue;

                                        mirrorBindings.Add(mbinding.Value);
                                    }
                                    foreach (var binding in mirrorBindings)
                                    {
                                        filterBindings.Add(binding);
                                    }
                                }
                                #endregion
                                #region MotionTool
                                if (SelectionMotionTool)
                                {
                                    AddMotionTBindings();
                                    AddMotionQBindings();
                                }
                                #endregion
                                #region Path
                                {
                                    var allBindings = AnimationUtility.GetCurveBindings(CurrentClip).ToList();
                                    allBindings.AddRange(AnimationUtility.GetObjectReferenceCurveBindings(CurrentClip));
                                    foreach (var binding in allBindings)
                                    {
                                        var boneIndex = GetBoneIndexFromCurveBinding(binding);
                                        if (boneIndex < 0)
                                            continue;
                                        if (!SelectionBones.Contains(boneIndex))
                                            continue;

                                        if (binding.type == typeof(Animator) &&
                                            IsAnimatorReservedPropertyName(binding.propertyName))
                                            continue;

                                        filterBindings.Add(binding);
                                    }
                                }
                                #endregion
                                animationWindowFilterBindings = filterBindings.ToList();
                            }
                            else
                            {
                                animationWindowFilterBindings = null;
                            }
                        }
                        else
                        {
                            animationWindowFilterBindings = null;
                        }
                        #endregion

                        UAw.PropertySortOrFilterByBindings(animationWindowFilterBindings);
                        UAw.SynchroCurveSelection(animationWindowSynchroSelectionBindings.Count == 0 ? syncBindings : animationWindowSynchroSelectionBindings);
                    }
                    animationWindowSynchroSelection = false;
                    animationWindowSynchroSelectionBindings.Clear();
                }
                else
                {
                    UAw.Repaint();
                }
            }

#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region TimelineTimeLink
            if (UAw.GetLinkedWithTimeline())
            {
#if VERYANIMATION_TIMELINE
                UAnimationClipEditorTotal?.SetDummyCursorTime((float)UAw.GetTimelineCurrentDirector().time, UAw.GetTimelineFrameRate());
#endif
            }
            #endregion

            AnimationWindowSampleAnimationOverride(true);

            #region CurveChange Step1
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("CurveChange Step1");
#endif
            bool rootUpdated = false;
            if (curvesWasModified.Count > 0)
            {
                SetOnCurveWasModifiedStop(true);
                foreach (var pair in curvesWasModified)
                {
                    #region CheckConflictCurve
                    if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveModified)
                    {
                        if (pair.Value.binding.type == typeof(Transform))
                        {
                            var boneIndex = GetBoneIndexFromCurveBinding(pair.Value.binding);
                            if (boneIndex >= 0)
                            {
                                if (IsHuman && HumanoidConflict[boneIndex])
                                {
                                    EditorCommon.ShowNotification("Conflict");
                                    Debug.LogErrorFormat(Language.GetText(Language.Help.LogGenericCurveHumanoidConflictError), Skeleton.Bones[boneIndex].name);
                                    SetEditorCurveCache(pair.Value.binding, null);
                                    continue;
                                }
                                else if (RootMotionBoneIndex >= 0 && boneIndex == 0 &&
                                        (IsTransformPositionCurveBinding(pair.Value.binding) || IsTransformRotationCurveBinding(pair.Value.binding)))
                                {
                                    EditorCommon.ShowNotification("Conflict");
                                    Debug.LogErrorFormat(Language.GetText(Language.Help.LogGenericCurveRootConflictError), Skeleton.Bones[boneIndex].name);
                                    SetEditorCurveCache(pair.Value.binding, null);
                                    continue;
                                }
                            }
                        }
                    }
                    #endregion

                    #region EditorOptions - rootCorrectionMode
                    if (IsHuman && rootCorrectionMode != RootCorrectionMode.Disable)
                    {
                        #region DisableAnimatorRootCorrection
                        if (IsAnimatorRootCurveBinding(pair.Value.binding))
                        {
                            if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveModified)
                            {
                                DisableAnimatorRootCorrection();
                            }
                            else if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveDeleted)
                            {
                                DisableAnimatorRootCorrection();
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region UpdateIK
                    {
                        AnimatorTDOFIndex tdofIndex;
                        int muscleIndex;
                        if (IsAnimatorRootCurveBinding(pair.Value.binding))
                        {
                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                            {
                                if (Mathf.Approximately(CurrentTime, curve[keyIndex].time))
                                {
                                    SetUpdateIKtargetAll();
                                    if (!rootUpdated && pair.Value.beforeCurve != null)
                                    {
                                        var valueNow = curve.Evaluate(CurrentTime);
                                        var valueBefore = pair.Value.beforeCurve.Evaluate(CurrentTime);
                                        rootUpdated = !Mathf.Approximately(valueNow, valueBefore);
                                    }
                                }
                            });
                        }
                        else if ((tdofIndex = GetTDOFIndexFromCurveBinding(pair.Value.binding)) >= 0)
                        {
                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                            {
                                if (Mathf.Approximately(CurrentTime, curve[keyIndex].time))
                                {
                                    SetUpdateIKtargetTdofIndex(tdofIndex);
                                }
                            });
                        }
                        else if ((muscleIndex = GetMuscleIndexFromCurveBinding(pair.Value.binding)) >= 0)
                        {
                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                            {
                                if (Mathf.Approximately(CurrentTime, curve[keyIndex].time))
                                {
                                    SetUpdateIKtargetMuscle(muscleIndex);
                                }
                            });
                        }
                        else if (IsTransformPositionCurveBinding(pair.Value.binding) ||
                                IsTransformRotationCurveBinding(pair.Value.binding) ||
                                IsTransformScaleCurveBinding(pair.Value.binding))
                        {
                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                            {
                                if (Mathf.Approximately(CurrentTime, curve[keyIndex].time))
                                {
                                    var boneIndex = GetBoneIndexFromCurveBinding(pair.Value.binding);
                                    SetUpdateIKtargetBone(boneIndex);
                                }
                            });
                        }
                    }
                    #endregion
                }
                SetOnCurveWasModifiedStop(false);
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region IK
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("IK");
#endif
            if (GetUpdateIKtargetAll() && !updatePoseFixAnimation)
            {
                if (IsHuman)
                {
                    #region Humanoid
                    if (animatorIK.GetUpdateIKtargetAll())
                    {
                        EnableAnimatorRootCorrection(CurrentTime, CurrentTime, CurrentTime);
                        UpdateAnimatorRootCorrection();
                        animatorIK.UpdateIK(rootUpdated);
                    }
                    #endregion
                }
                originalIK.UpdateIK();
                SetUpdateSampleAnimation();
            }
            else if (GetSynchroIKtargetAll())
            {
                SetUpdateSampleAnimation();
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region CurveChange Step2
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("CurveChange Step2");
#endif
            if (curvesWasModified.Count > 0 && ((IsHuman && optionsClampMuscle) || optionsMirror) && !updatePoseFixAnimation)
            {
                SetOnCurveWasModifiedStop(true);
                foreach (var pair in curvesWasModified)
                {
                    if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveModified)
                    {
                        #region EditorOptions - clampMuscle
                        if ((IsHuman && optionsClampMuscle))
                        {
                            if (GetMuscleIndexFromCurveBinding(pair.Value.binding) >= 0)
                            {
                                AnimationCurve changedCurve = null;
                                ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                {
                                    var key = curve[keyIndex];
                                    var clampValue = Mathf.Clamp(key.value, -1f, 1f);
                                    if (key.value != clampValue)
                                    {
                                        key.value = clampValue;
                                        curve.MoveKey(keyIndex, key);
                                        changedCurve = curve;
                                    }
                                });
                                if (changedCurve != null)
                                {
                                    SetEditorCurveCache(pair.Value.binding, changedCurve);
                                }
                            }
                        }
                        #endregion

                        #region EditorOptions - mirrorEnable
                        if (optionsMirror)
                        {
                            var mbinding = GetMirrorAnimationCurveBinding(pair.Value.binding);
                            if (mbinding.HasValue)
                            {
                                var hash = GetEditorCurveBindingHashCode(mbinding.Value);
                                if (!curvesWasModified.ContainsKey(hash))
                                {
                                    bool updated = false;
                                    var boneIndex = GetBoneIndexFromCurveBinding(pair.Value.binding);
                                    var mcurve = GetEditorCurveCache(mbinding.Value);
                                    if (mcurve == null)
                                    {
                                        #region CreateMirrorCurves
                                        if (IsTransformPositionCurveBinding(pair.Value.binding))
                                        {
                                            SetAnimationValueTransformPosition(MirrorBoneIndexes[boneIndex], GetAnimationValueTransformPosition(MirrorBoneIndexes[boneIndex]));
                                        }
                                        else if (IsTransformRotationCurveBinding(pair.Value.binding))
                                        {
                                            var mode = GetHaveAnimationCurveTransformRotationMode(boneIndex);
                                            var mmode = GetHaveAnimationCurveTransformRotationMode(MirrorBoneIndexes[boneIndex]);
                                            if (mmode == URotationCurveInterpolation.Mode.Undefined)
                                            {
                                                SetAnimationValueTransformRotation(MirrorBoneIndexes[boneIndex], GetAnimationValueTransformRotation(MirrorBoneIndexes[boneIndex]));
                                            }
                                            else if (mode == URotationCurveInterpolation.Mode.RawQuaternions && mmode == URotationCurveInterpolation.Mode.RawEuler)
                                            {
                                                EditorCurveBinding[] convertBindings = new EditorCurveBinding[3];
                                                for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                {
                                                    convertBindings[dofIndex] = mbinding.Value;
                                                    convertBindings[dofIndex].propertyName = EditorCurveBindingTransformRotationPropertyNames[(int)URotationCurveInterpolation.Mode.RawEuler][dofIndex];
                                                    RemoveEditorCurveCache(convertBindings[dofIndex]);
                                                }
                                                URotationCurveInterpolation.SetInterpolation(CurrentClip, convertBindings, URotationCurveInterpolation.Mode.NonBaked);
                                            }
                                            else if (mode == URotationCurveInterpolation.Mode.RawEuler && mmode == URotationCurveInterpolation.Mode.RawQuaternions)
                                            {
                                                {
                                                    EditorCurveBinding[] convertBindings = new EditorCurveBinding[3];
                                                    for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                    {
                                                        convertBindings[dofIndex] = mbinding.Value;
                                                        convertBindings[dofIndex].propertyName = EditorCurveBindingTransformRotationPropertyNames[(int)URotationCurveInterpolation.Mode.NonBaked][dofIndex];
                                                        RemoveEditorCurveCache(convertBindings[dofIndex]);
                                                    }
                                                    URotationCurveInterpolation.SetInterpolation(CurrentClip, convertBindings, URotationCurveInterpolation.Mode.RawEuler);
                                                }
                                            }
                                        }
                                        else if (IsTransformScaleCurveBinding(pair.Value.binding))
                                        {
                                            SetAnimationValueTransformScale(MirrorBoneIndexes[boneIndex], GetAnimationValueTransformScale(MirrorBoneIndexes[boneIndex]));
                                        }
                                        else
                                        {
                                            var curve = GetEditorCurveCache(pair.Value.binding);
                                            SetEditorCurveCache(mbinding.Value, new AnimationCurve(curve.keys));
                                        }
                                        mcurve = GetEditorCurveCache(mbinding.Value);
                                        #endregion
                                    }
                                    if (mcurve != null)
                                    {
                                        #region RemoveMirrorCurveKeyframe
                                        ActionBeforeChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                        {
                                            var index = FindKeyframeAtTime(mcurve, curve[keyIndex].time);
                                            if (index >= 0)
                                            {
                                                mcurve.RemoveKey(index);
                                                updated = true;
                                            }
                                        });
                                        #endregion

                                        #region UpdateMirrorSyncro
                                        void UpdateMirrorSyncro()
                                        {
                                            if (IsHuman)
                                            {
                                                SetSynchroIKtargetMuscle(GetMuscleIndexFromCurveBinding(mbinding.Value));
                                                SetSynchroIKtargetTdofIndex(GetTDOFIndexFromCurveBinding(mbinding.Value));
                                            }
                                            SetSynchroIKtargetBone(GetBoneIndexFromCurveBinding(mbinding.Value));
                                        }
                                        #endregion

                                        AnimatorTDOFIndex tdofIndex;
                                        if (GetIkTIndexFromCurveBinding(pair.Value.binding) >= 0 ||
                                            GetIkQIndexFromCurveBinding(pair.Value.binding) >= 0)
                                        {
                                            #region IK
                                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                            {
                                                AddHumanoidFootIK(curve[keyIndex].time);
                                            });
                                            #endregion
                                        }
                                        else if ((tdofIndex = GetTDOFIndexFromCurveBinding(pair.Value.binding)) >= 0)
                                        {
                                            #region TDOF
                                            var mtdofIndex = AnimatorTDOFMirrorIndexes[(int)tdofIndex];
                                            if (mtdofIndex != AnimatorTDOFIndex.None)
                                            {
                                                var dof = GetDOFIndexFromCurveBinding(pair.Value.binding);
                                                var mirrorScale = HumanBonesAnimatorTDOFIndex[(int)AnimatorTDOFIndex2HumanBodyBones[(int)mtdofIndex]].mirror;
                                                ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                                {
                                                    var key = curve[keyIndex];
                                                    key.value *= mirrorScale[dof];
                                                    key.inTangent *= mirrorScale[dof];
                                                    key.outTangent *= mirrorScale[dof];
                                                    SetKeyframe(mcurve, key);
                                                    updated = true;
                                                });
                                            }
                                            #endregion
                                        }
                                        else if (IsTransformPositionCurveBinding(pair.Value.binding))
                                        {
                                            #region Position
                                            LoadTmpCurvesFullDof(mbinding.Value, 3);
                                            LoadTmpSubCurvesFullDof(pair.Value.binding, 3);
                                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                            {
                                                var position = GetAnimationValueTransformPosition(boneIndex, curve[keyIndex].time);
                                                var mposition = GetMirrorBoneLocalPosition(boneIndex, position);
                                                for (int dof = 0; dof < 3; dof++)
                                                {
                                                    if (tmpCurves.curves[dof] == null || tmpCurves.subCurves[dof] == null)
                                                        continue;
                                                    var mkeyIndex = FindKeyframeAtTime(tmpCurves.subCurves[dof], curve[keyIndex].time);
                                                    if (mkeyIndex >= 0)
                                                    {
                                                        var key = tmpCurves.subCurves[dof][mkeyIndex];
                                                        key.value = mposition[dof];
                                                        if (MirrorBoneDatas[MirrorBoneIndexes[boneIndex]].positionTangentInverse[dof])
                                                        {
                                                            key.inTangent *= -1f;
                                                            key.outTangent *= -1f;
                                                        }
                                                        SetKeyframe(tmpCurves.curves[dof], key);
                                                    }
                                                    else
                                                    {
                                                        SetKeyframe(tmpCurves.curves[dof], curve[keyIndex].time, mposition[dof]);
                                                    }
                                                }
                                                updated = true;
                                            });
                                            if (updated)
                                            {
                                                for (int dof = 0; dof < 3; dof++)
                                                    SetEditorCurveCache(tmpCurves.bindings[dof], tmpCurves.curves[dof]);
                                                updated = false;
                                                UpdateMirrorSyncro();
                                            }
                                            tmpCurves.Clear();
                                            #endregion
                                        }
                                        else if (IsTransformRotationCurveBinding(pair.Value.binding))
                                        {
                                            #region Rotation
                                            if (mbinding.Value.propertyName.StartsWith(URotationCurveInterpolation.PrefixForInterpolation[(int)URotationCurveInterpolation.Mode.RawQuaternions]))
                                            {
                                                LoadTmpCurvesFullDof(mbinding.Value, 4);
                                                LoadTmpSubCurvesFullDof(pair.Value.binding, 4);
                                                ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                                {
                                                    var localRotation = GetAnimationValueTransformRotation(boneIndex, curve[keyIndex].time);
                                                    var mlocalRotation = GetMirrorBoneLocalRotation(boneIndex, localRotation);
                                                    mlocalRotation = FixReverseRotationQuaternion(tmpCurves.curves, curve[keyIndex].time, mlocalRotation);
                                                    for (int dof = 0; dof < 4; dof++)
                                                    {
                                                        if (tmpCurves.curves[dof] == null || tmpCurves.subCurves[dof] == null)
                                                            continue;
                                                        var mkeyIndex = FindKeyframeAtTime(tmpCurves.subCurves[dof], curve[keyIndex].time);
                                                        if (mkeyIndex >= 0)
                                                        {
                                                            var key = tmpCurves.subCurves[dof][mkeyIndex];
                                                            key.value = mlocalRotation[dof];
                                                            if (MirrorBoneDatas[MirrorBoneIndexes[boneIndex]].rotationTangentInverse[dof])
                                                            {
                                                                key.inTangent *= -1f;
                                                                key.outTangent *= -1f;
                                                            }
                                                            SetKeyframe(tmpCurves.curves[dof], key);
                                                        }
                                                        else
                                                        {
                                                            SetKeyframe(tmpCurves.curves[dof], curve[keyIndex].time, mlocalRotation[dof]);
                                                        }
                                                    }
                                                    updated = true;
                                                });
                                                if (updated)
                                                {
                                                    for (int dof = 0; dof < 4; dof++)
                                                        SetEditorCurveCache(tmpCurves.bindings[dof], tmpCurves.curves[dof]);
                                                    updated = false;
                                                    UpdateMirrorSyncro();
                                                }
                                                tmpCurves.Clear();
                                            }
                                            else if (mbinding.Value.propertyName.StartsWith(URotationCurveInterpolation.PrefixForInterpolation[(int)URotationCurveInterpolation.Mode.RawEuler]))
                                            {
                                                LoadTmpCurvesFullDof(mbinding.Value, 3);
                                                LoadTmpSubCurvesFullDof(pair.Value.binding, 3);
                                                ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                                {
                                                    var localRotation = GetAnimationValueTransformRotation(boneIndex, curve[keyIndex].time);
                                                    var mlocalRotation = GetMirrorBoneLocalRotation(boneIndex, localRotation);
                                                    var meulerAngles = FixReverseRotationEuler(tmpCurves.curves, curve[keyIndex].time, mlocalRotation.eulerAngles);
                                                    for (int dof = 0; dof < 3; dof++)
                                                    {
                                                        if (tmpCurves.curves[dof] == null || tmpCurves.subCurves[dof] == null)
                                                            continue;
                                                        var mkeyIndex = FindKeyframeAtTime(tmpCurves.subCurves[dof], curve[keyIndex].time);
                                                        if (mkeyIndex >= 0)
                                                        {
                                                            var key = tmpCurves.subCurves[dof][mkeyIndex];
                                                            key.value = meulerAngles[dof];
                                                            if (MirrorBoneDatas[MirrorBoneIndexes[boneIndex]].eulerAnglesTangentInverse[dof])
                                                            {
                                                                key.inTangent *= -1f;
                                                                key.outTangent *= -1f;
                                                            }
                                                            SetKeyframe(tmpCurves.curves[dof], key);
                                                        }
                                                        else
                                                        {
                                                            SetKeyframe(tmpCurves.curves[dof], curve[keyIndex].time, meulerAngles[dof]);
                                                        }
                                                    }
                                                    updated = true;
                                                });
                                                if (updated)
                                                {
                                                    for (int dof = 0; dof < 3; dof++)
                                                        SetEditorCurveCache(tmpCurves.bindings[dof], tmpCurves.curves[dof]);
                                                    updated = false;
                                                    UpdateMirrorSyncro();
                                                }
                                                tmpCurves.Clear();
                                            }
                                            #endregion
                                        }
                                        else if (IsTransformScaleCurveBinding(pair.Value.binding))
                                        {
                                            #region Scale
                                            LoadTmpCurvesFullDof(mbinding.Value, 3);
                                            LoadTmpSubCurvesFullDof(pair.Value.binding, 3);
                                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                            {
                                                var scale = GetAnimationValueTransformScale(boneIndex, curve[keyIndex].time);
                                                var mscale = GetMirrorBoneLocalScale(boneIndex, scale);
                                                for (int dof = 0; dof < 3; dof++)
                                                {
                                                    if (tmpCurves.curves[dof] == null || tmpCurves.subCurves[dof] == null)
                                                        continue;
                                                    var mkeyIndex = FindKeyframeAtTime(tmpCurves.subCurves[dof], curve[keyIndex].time);
                                                    if (mkeyIndex >= 0)
                                                    {
                                                        var key = tmpCurves.subCurves[dof][mkeyIndex];
                                                        key.value = mscale[dof];
                                                        if (MirrorBoneDatas[MirrorBoneIndexes[boneIndex]].scaleTangentInverse[dof])
                                                        {
                                                            key.inTangent *= -1f;
                                                            key.outTangent *= -1f;
                                                        }
                                                        SetKeyframe(tmpCurves.curves[dof], key);
                                                    }
                                                    else
                                                    {
                                                        SetKeyframe(tmpCurves.curves[dof], curve[keyIndex].time, mscale[dof]);
                                                    }
                                                }
                                                updated = true;
                                            });
                                            if (updated)
                                            {
                                                for (int dof = 0; dof < 3; dof++)
                                                    SetEditorCurveCache(tmpCurves.bindings[dof], tmpCurves.curves[dof]);
                                                updated = false;
                                                UpdateMirrorSyncro();
                                            }
                                            tmpCurves.Clear();
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Other (As it is)
                                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                                            {
                                                var key = curve[keyIndex];
                                                SetKeyframe(mcurve, key);
                                                updated = true;
                                            });
                                            #endregion
                                        }
                                        if (updated)
                                        {
                                            SetEditorCurveCache(mbinding.Value, mcurve);
                                            updated = false;
                                            UpdateMirrorSyncro();
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    else if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveDeleted)
                    {
                        #region EditorOptions - mirrorEnable
                        if (optionsMirror)
                        {
                            var mbinding = GetMirrorAnimationCurveBinding(pair.Value.binding);
                            if (mbinding.HasValue)
                            {
                                var hash = GetEditorCurveBindingHashCode(mbinding.Value);
                                if (!curvesWasModified.ContainsKey(hash))
                                {
                                    SetEditorCurveCache(mbinding.Value, null);
                                }
                            }
                        }
                        #endregion
                    }
                }
                SetOnCurveWasModifiedStop(false);
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region CurveChange Step3
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("CurveChange Step3");
#endif
            if (curvesWasModified.Count > 0)
            {
                foreach (var pair in curvesWasModified)
                {
                    #region EditorOptions - rootCorrectionMode
                    if (IsHuman && rootCorrectionMode != RootCorrectionMode.Disable)
                    {
                        #region EnableAnimatorRootCorrection
                        {
                            bool updatedMuscle = false;
                            {
                                var muscleIndex = GetMuscleIndexFromCurveBinding(pair.Value.binding);
                                if (muscleIndex >= 0)
                                {
                                    var humanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(muscleIndex);
                                    if (IsAnimatorRootCorrectionBone(humanoidIndex))
                                    {
                                        updatedMuscle = true;
                                    }
                                }
                            }
                            if (updatedMuscle ||
                                GetTDOFIndexFromCurveBinding(pair.Value.binding) >= 0)
                            {
                                if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveModified)
                                {
                                    void ChangedKeyframe(AnimationCurve curve, int keyIndex)
                                    {
                                        EnableAnimatorRootCorrection(curve, keyIndex);
                                    }
                                    ActionCurrentChangedKeyframes(pair.Value, ChangedKeyframe);
                                    ActionBeforeChangedKeyframes(pair.Value, ChangedKeyframe);
                                }
                                else if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveDeleted)
                                {
                                    EnableAnimatorRootCorrection(CurrentTime, 0f, CurrentClip.length);
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region HumanoidFootIK
                    if (IsEnableUpdateHumanoidFootIK())
                    {
                        if (IsAnimatorRootCurveBinding(pair.Value.binding) ||
                            GetMuscleIndexFromCurveBinding(pair.Value.binding) >= 0 ||
                            GetTDOFIndexFromCurveBinding(pair.Value.binding) >= 0)
                        {
                            ActionCurrentChangedKeyframes(pair.Value, (curve, keyIndex) =>
                            {
                                AddHumanoidFootIK(curve[keyIndex].time);
                            });
                        }
                    }
                    #endregion

                    #region CurveCreated
                    if (pair.Value.beforeCurve == null)
                    {
                        SetUpdateSampleAnimation(true, true);
                        SetAnimationWindowSynchroSelection();
                    }
                    #endregion
                    #region CurveDeleted
                    if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveDeleted)
                    {
                        SetUpdateSampleAnimation(true, true);
                        SetAnimationWindowSynchroSelection();
                    }
                    #endregion
                }

                SetUpdateSampleAnimation();
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region UpdateAnimation
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("UpdateAnimation");
#endif
            bool updateAnimation = false;
            {
                #region updateStartTransform
                if (updateStartTransform)
                {
                    if (UAw.GetLinkedWithTimeline())
                    {
#if VERYANIMATION_TIMELINE
                        var saveTime = CurrentTime;
                        SetCurrentTime(0f);
                        {
                            SampleAnimation();
                            {
                                if (UAw.GetTimelineRootMotionOffsets(out Vector3 offsetPosition, out Quaternion offsetRotation))
                                {
#if UNITY_2022_3_OR_NEWER
                                    VAW.GameObject.transform.SetLocalPositionAndRotation(offsetPosition, offsetRotation);
#else
                                    VAW.GameObject.transform.localPosition = offsetPosition;
                                    VAW.GameObject.transform.localRotation = offsetRotation;
#endif
                                }
                            }
                            TransformPoseSave.ChangeStartTransform();
                        }
                        SetCurrentTime(saveTime);
                        updateAnimation = true;
                        SetSynchroIKtargetAll();
#else
                        Assert.IsTrue(false);
#endif
                    }
                    updateStartTransform = false;
                }
                #endregion
                #region updateSampleAnimation
                if (updateSampleAnimation)
                {
                    TransformPoseSave.ResetOriginalTransform();
                    BlendShapeWeightSave.ResetOriginalWeight();
                    UpdateAnimatorRootCorrection();
#if Enable_Profiler
                    UnityEngine.Profiling.Profiler.BeginSample("UpdateCollisionData");
#endif
                    UpdateCollision();
#if Enable_Profiler
                    UnityEngine.Profiling.Profiler.EndSample();
#endif
                    updateAnimation = true;
                }
                #endregion
                #region FootIK
                if (UpdateHumanoidFootIK())
                {
                    updateAnimation = true;
                }
                #endregion

                if (updateAnimation)
                {
                    #region Save
                    {
                        updateSaveForce |= curvesWasModified.Count > 0;
                        SaveAnimatorRootCorrection(updateSaveForce);
                        SaveCollision(updateSaveForce);
                        updateSaveForce = false;
                    }
                    #endregion

                    SampleAnimation();

                    UpdateSynchroIKSet();
                    OnionSkin.Update();

                    UAvatarPreview?.Reset();
                    synchronizeAnimation?.UpdateSameClip(CurrentClip);

                    #region Cache
                    HumanWorldRootPositionCache = GetHumanWorldRootPosition();
                    HumanWorldRootRotationCache = GetHumanWorldRootRotation();
                    AnimatorWorldRootPositionCache = GetAnimatorWorldMotionPosition();
                    AnimatorWorldRootRotationCache = GetAnimatorWorldMotionRotation();
                    #endregion
                }
                updateSampleAnimation = false;

                EndChangeAnimationCurve();
            }

#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region CurveCache
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("CurveCache");
#endif
            if (editorCurveCacheDirty)
            {
                var bindings = AnimationUtility.GetCurveBindings(CurrentClip);
                foreach (var binding in bindings)
                {
                    GetEditorCurveCache(binding);
                    if (binding.type == typeof(Transform) &&
                        binding.propertyName == EditorCurveBindingTransformRotationPropertyNames[(int)URotationCurveInterpolation.Mode.RawQuaternions][0])
                    {
                        var tmpBinding = binding;
                        foreach (var propertyName in EditorCurveBindingTransformRotationPropertyNames[(int)URotationCurveInterpolation.Mode.NonBaked])
                        {
                            tmpBinding.propertyName = propertyName;
                            GetEditorCurveCache(tmpBinding);
                        }
                    }
                }
                editorCurveCacheDirty = false;
            }
            else
            {
                foreach (var pair in curvesWasModified)
                {
                    if (pair.Value.deleted == AnimationUtility.CurveModifiedType.CurveModified &&
                        !IsContainsEditorCurveCache(pair.Value.binding))
                    {
                        GetEditorCurveCache(pair.Value.binding);
                    }
                }
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region Repaint
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("Repaint");
#endif
            if (updateAnimation)
            {
                VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.Edit);
                if (UAw.IsShowCurveEditor())
                    SetAnimationWindowRefresh(AnimationWindowStateRefreshType.CurvesOnly);
                if (EditorApplication.isPlaying && EditorApplication.isPaused) //Is there a bug that will not be updated while pausing? Therefore, it forcibly updates it.
                    RendererForceUpdate();
            }
            else
            {
                if (EditorApplication.isPlaying && EditorApplication.isPaused && UAw.GetPlaying())  //Is there a bug that will not be updated while pausing? Therefore, it forcibly updates it.
                    RendererForceUpdate();
            }
            if (awForceRefresh)
            {
                UAw.ForceRefresh();
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            #region Clear
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.BeginSample("Clear");
#endif
            {
                curvesWasModified.Clear();  //Do it last
                updatePoseFixAnimation = false;
                ResetUpdateIKtargetAll();
                ResetSynchroIKtargetAll();
            }
#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            #endregion

            UpdateSyncEditorCurveClip();

#if Enable_Profiler
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

        private void UpdateBones(bool initialize)
        {
            if (UAw.GetLinkedWithTimeline())
            {
#if VERYANIMATION_TIMELINE
                UAw.UTimelineWindow.Refresh();
#endif
            }
            else
            {
                UAw.DestroyPlayableGraph();
            }
            animationPlayable?.Release();

            #region Reload
            List<GameObject> reloadShowBones = null;
            List<GameObject> reloadWriteLockBones = null;
            Dictionary<GameObject, GameObject> reloadMirrorBonePaths = null;
            VeryAnimationSaveSettings.AnimatorIKData[] reloadAnimatorIKData = null;
            VeryAnimationSaveSettings.OriginalIKData[] reloadOriginalIKData = null;
            if (!initialize)
            {
                reloadShowBones = GetReloadBoneShowFlags();
                reloadWriteLockBones = GetReloadBoneWriteLockFlags();
                reloadMirrorBonePaths = GetReloadBonesMirror();
                if (animatorIK != null)
                    reloadAnimatorIKData = animatorIK.SaveIKSaveSettings();
                if (originalIK != null)
                    reloadOriginalIKData = originalIK.SaveIKSaveSettings();
            }
            #endregion

            #region OriginalSave
            if (TransformPoseSave != null)
            {
                TransformPoseSave.ResetOriginalTransform();
                TransformPoseSave.ResetRootOriginalTransform();
                TransformPoseSave = null;
            }
            TransformPoseSave = new TransformPoseSave(VAW.GameObject);
            TransformPoseSave.CreateExtraTransforms();

            BlendShapeWeightSave?.ResetOriginalWeight();
            BlendShapeWeightSave = new BlendShapeWeightSave(VAW.GameObject);
            BlendShapeWeightSave.CreateExtraValues();
            #endregion

            Renderers = VAW.GameObject.GetComponentsInChildren<Renderer>(true);
            IsHuman = VAW.Animator != null && VAW.Animator.isHuman;
            AnimatorApplyRootMotion = VAW.Animator != null && VAW.Animator.applyRootMotion;
#pragma warning disable IDE0031
            AnimatorAvatar = VAW.Animator != null ? VAW.Animator.avatar : null;
#pragma warning restore IDE0031
            AnimatorAvatarRoot = VAW.Animator != null ? UAnimator.GetAvatarRoot(VAW.Animator) : null;
            #region Humanoid
            if (IsHuman)
            {
                if (!VAW.Animator.isInitialized)
                    VAW.Animator.Rebind();

                HumanoidBones = new GameObject[HumanTrait.BoneCount];
                HumanoidMuscleLimit = new UAvatar.MuscleLimit[HumanTrait.BoneCount];
                HumanoidMuscleContains = new bool[HumanTrait.MuscleCount];
                for (int bone = 0; bone < HumanTrait.BoneCount; bone++)
                {
                    var t = VAW.Animator.GetBoneTransform((HumanBodyBones)bone);
                    if (t != null)
                    {
                        HumanoidBones[bone] = t.gameObject;
                    }
                    HumanoidMuscleLimit[bone] = UAvatar.GetMuscleLimitNonError(AnimatorAvatar, (HumanBodyBones)bone);
                }
                HumanoidHasLeftHand = UAvatar.GetHasLeftHand(AnimatorAvatar);
                HumanoidHasRightHand = UAvatar.GetHasRightHand(AnimatorAvatar);
                HumanoidHasTDoF = UAvatar.GetHasTDoF(AnimatorAvatar);
                HumanoidPreHipRotationInverse = Quaternion.Inverse(GetHumanoidAvatarPreRotation(HumanBodyBones.Hips));
                HumanoidPostHipRotation = GetHumanoidAvatarPostRotation(HumanBodyBones.Hips);
                HumanoidLeftLowerLegLengthSq = Mathf.Pow(UAvatar.GetAxisLength(AnimatorAvatar, (int)HumanBodyBones.LeftLowerLeg), 2f);
                for (int mi = 0; mi < HumanTrait.MuscleCount; mi++)
                {
                    bool flag = false;
                    var humanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(mi);
                    if (humanoidIndex >= 0)
                    {
                        if (humanoidIndex >= HumanBodyBones.LeftThumbProximal && humanoidIndex <= HumanBodyBones.LeftLittleDistal && HumanoidHasLeftHand)
                            flag = true;
                        else if (humanoidIndex >= HumanBodyBones.RightThumbProximal && humanoidIndex <= HumanBodyBones.RightLittleDistal && HumanoidHasRightHand)
                            flag = true;
                        else
                            flag = HumanoidBones[(int)humanoidIndex] != null || HumanVirtualBones[(int)humanoidIndex] != null;
                    }
                    HumanoidMuscleContains[mi] = flag;
                }
                HumanPoseHandler = new HumanPoseHandler(AnimatorAvatar, AnimatorAvatarRoot);
                #region Avoiding Unity's bug
                {
                    //Hips You need to call SetHumanPose once if there is a scale in the top. Otherwise, the result of GetHumanPose becomes abnormal.
                    var hp = new HumanPose()
                    {
                        bodyPosition = new Vector3(0f, 1f, 0f),
                        bodyRotation = Quaternion.identity,
                        muscles = new float[HumanTrait.MuscleCount],
                    };
                    HumanPoseHandler.SetHumanPose(ref hp);
                }
                #endregion

                #region CheckHumanoid
                {
                    IsHumanAvatarReady = true;
                    var humanoidBonesSet = UAvatar.GetHumanoidBonesSet(AnimatorAvatar);
                    void CheckHumanoidBone(HumanBodyBones humanoidIndex)
                    {
                        if (humanoidBonesSet[(int)humanoidIndex] && HumanoidBones[(int)humanoidIndex] == null)
                        {
                            IsHumanAvatarReady = false;
                            Debug.LogErrorFormat(Language.GetText(Language.Help.LogAvatarHumanoidSetBoneError), humanoidIndex);
                        }
                    }
                    for (var humanoidIndex = HumanBodyBones.Hips; humanoidIndex <= HumanBodyBones.Jaw; humanoidIndex++)
                        CheckHumanoidBone(humanoidIndex);
                    CheckHumanoidBone(HumanBodyBones.UpperChest);
                    if (HumanoidHasLeftHand)
                    {
                        for (var humanoidIndex = HumanBodyBones.LeftThumbProximal; humanoidIndex <= HumanBodyBones.LeftLittleDistal; humanoidIndex++)
                            CheckHumanoidBone(humanoidIndex);
                    }
                    if (HumanoidHasRightHand)
                    {
                        for (var humanoidIndex = HumanBodyBones.RightThumbProximal; humanoidIndex <= HumanBodyBones.RightLittleDistal; humanoidIndex++)
                            CheckHumanoidBone(humanoidIndex);
                    }
                }
                #endregion
            }
            else
            {
                HumanoidBones = null;
                HumanoidMuscleLimit = null;
                HumanoidHasLeftHand = false;
                HumanoidHasRightHand = false;
                HumanoidHasTDoF = false;
                HumanoidPreHipRotationInverse = Quaternion.identity;
                HumanoidPostHipRotation = Quaternion.identity;
                HumanoidLeftLowerLegLengthSq = 0f;
                HumanoidMuscleContains = null;
                HumanPoseHandler = null;
                IsHumanAvatarReady = false;
            }
            #endregion
            #region bones
            Bones = EditorCommon.GetHierarchyGameObject(VAW.GameObject).ToArray();
            BoneDictionary = new Dictionary<GameObject, int>(Bones.Length);
            for (int i = 0; i < Bones.Length; i++)
            {
                BoneDictionary.Add(Bones[i], i);
            }
            #endregion
            #region boneIndex2humanoidIndex, humanoidIndex2boneIndex
            if (IsHuman)
            {
                BoneIndex2humanoidIndex = new HumanBodyBones[Bones.Length];
                for (int i = 0; i < Bones.Length; i++)
                    BoneIndex2humanoidIndex[i] = (HumanBodyBones)EditorCommon.ArrayIndexOf(HumanoidBones, Bones[i]);
                HumanoidIndex2boneIndex = new int[HumanTrait.BoneCount];
                for (int i = 0; i < HumanoidBones.Length; i++)
                    HumanoidIndex2boneIndex[i] = EditorCommon.ArrayIndexOf(Bones, HumanoidBones[i]);
            }
            else
            {
                BoneIndex2humanoidIndex = null;
                HumanoidIndex2boneIndex = null;
            }
            #endregion
            #region bonePaths, bonePathDic, boneDefaultPose, boneSaveTransforms, boneSaveOriginalTransforms
            BonePaths = new string[Bones.Length];
            BonePathDictionary = new Dictionary<string, int>(BonePaths.Length);
            BoneSaveTransforms = new TransformPoseSave.SaveData[Bones.Length];
            BoneSaveOriginalTransforms = new TransformPoseSave.SaveData[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                BonePaths[i] = AnimationUtility.CalculateTransformPath(Bones[i].transform, VAW.GameObject.transform);
                if (!BonePathDictionary.ContainsKey(BonePaths[i]))
                {
                    BonePathDictionary.Add(BonePaths[i], i);
                }
                else
                {
                    Debug.LogWarningFormat(Language.GetText(Language.Help.LogMultipleGameObjectsWithSameName), BonePaths[i]);
                }
                BoneSaveTransforms[i] = TransformPoseSave.GetBindTransform(Bones[i].transform);
                if (BoneSaveTransforms[i] == null)
                    BoneSaveTransforms[i] = TransformPoseSave.GetPrefabTransform(Bones[i].transform);
                if (BoneSaveTransforms[i] == null)
                    BoneSaveTransforms[i] = TransformPoseSave.GetOriginalTransform(Bones[i].transform);
                Assert.IsNotNull(BoneSaveTransforms[i]);
                BoneSaveOriginalTransforms[i] = TransformPoseSave.GetOriginalTransform(Bones[i].transform);
                Assert.IsNotNull(BoneSaveOriginalTransforms[i]);
            }
            if (AnimatorAvatar != null)
            {
                BoneDefaultPose = new UAvatar.Transform[Bones.Length];
                var defaultPose = UAvatar.GetDefaultPose(AnimatorAvatar);
                foreach (var pair in defaultPose)
                {
                    if (BonePathDictionary.TryGetValue(pair.Key, out int boneIndex))
                        BoneDefaultPose[boneIndex] = pair.Value;
                }
            }
            else
            {
                BoneDefaultPose = null;
            }
            if (IsHuman)
            {
                var humanPose = new HumanPose();
                GetSceneObjectHumanPose(ref humanPose);
                SaveHumanPose = humanPose;
            }
            #endregion
            #region rootMotionBoneIndex
            RootMotionBoneIndex = -1;
            if (VAW.Animator != null)
            {
                if (VAW.Animator.isHuman)
                {
                    RootMotionBoneIndex = 0;
                }
                else
                {
                    var genericRootMotionBonePath = UAvatar.GetGenericRootMotionBonePath(AnimatorAvatar);
                    if (!string.IsNullOrEmpty(genericRootMotionBonePath))
                    {
                        if (BonePathDictionary.TryGetValue(genericRootMotionBonePath, out int boneIndex))
                        {
                            RootMotionBoneIndex = boneIndex;
                        }
                    }
                }
            }
            #endregion
            #region parentBone
            {
                ParentBoneIndexes = new int[Bones.Length];
                for (int i = 0; i < Bones.Length; i++)
                {
                    if (Bones[i].transform.parent != null)
                        ParentBoneIndexes[i] = BonesIndexOf(Bones[i].transform.parent.gameObject);
                    else
                        ParentBoneIndexes[i] = -1;
                }
            }
            #endregion
            #region boneHierarchyLevels
            {
                BoneHierarchyLevels = new int[Bones.Length];
                BoneHierarchyMaxLevel = 0;
                for (int i = 0; i < Bones.Length; i++)
                {
                    var parentIndex = ParentBoneIndexes[i];
                    while (parentIndex >= 0)
                    {
                        parentIndex = ParentBoneIndexes[parentIndex];
                        BoneHierarchyLevels[i]++;
                    }
                    BoneHierarchyMaxLevel = Math.Max(BoneHierarchyMaxLevel, BoneHierarchyLevels[i]);
                }
            }
            #endregion
            #region humanoidConflict
            if (IsHuman)
            {
                HumanoidConflict = new bool[Bones.Length];
                void SetHumanoidConflict(int index)
                {
                    if (index < 0) return;
                    HumanoidConflict[index] = true;
                    if (ParentBoneIndexes[index] >= 0)
                        SetHumanoidConflict(ParentBoneIndexes[index]);
                }

                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.Head]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.Jaw]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftHand]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftThumbDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftIndexDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftMiddleDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftRingDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftLittleDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightHand]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightThumbDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightIndexDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightMiddleDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightRingDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightLittleDistal]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftFoot]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightFoot]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftToes]);
                SetHumanoidConflict(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightToes]);
                foreach (var index in HumanoidIndex2boneIndex)
                {
                    if (index >= 0)
                        HumanoidConflict[index] = true;
                }
            }
            else
            {
                HumanoidConflict = null;
            }
            #endregion
            #region BlendShapeConflictCheck
            foreach (var renderer in Renderers)
            {
                var smRenderer = renderer as SkinnedMeshRenderer;
                if (smRenderer == null)
                    continue;
                var mesh = smRenderer.sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0) continue;
                var list = new List<string>(mesh.blendShapeCount);
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    var name = mesh.GetBlendShapeName(i);
                    if (list.Contains(name))
                        Debug.LogWarningFormat(Language.GetText(Language.Help.LogMultipleBlendShapesWithSameName), mesh.name, name);
                    list.Add(name);
                }
            }
            #endregion

            #region AnimationRigging
#if VERYANIMATION_ANIMATIONRIGGING
            {
                AnimationRigging ??= new AnimationRigging();
                AnimationRigging.Initialize();
            }
#endif
            #endregion

            #region calcObject
            {
                Skeleton?.Dispose();
                Skeleton = new DummyObject();
                Skeleton.Initialize(VAW.GameObject);
                Skeleton.AddEditComponent();
                Skeleton.RendererDestroyImmediate();
                Skeleton.SetTransformStart();
            }
            #endregion

            #region AnimatorIK
            {
                animatorIK ??= new AnimatorIKCore();
                animatorIK.Initialize();
            }
            #endregion

            #region OriginalIK
            {
                originalIK ??= new OriginalIKCore();
                originalIK.Initialize();
            }
            #endregion

            #region OnionSkin
            {
                OnionSkin?.Dispose();
                OnionSkin = new OnionSkin();
            }
            #endregion

            #region Preview
            if (UAvatarPreview != null)
            {
                UAvatarPreview.Dispose();
                UAvatarPreview = null;
            }
            #endregion

            IKUpdateBones();

            if (initialize)
            {
                InitializeBoneFlags();
                BonesMirrorAutomap();
                BlendShapeMirrorAutomap();
            }
            else
            {
                SetReloadBoneShowFlags(reloadShowBones);
                SetReloadBoneWriteLockFlags(reloadWriteLockBones);
                SetReloadBonesMirror(reloadMirrorBonePaths);
                animatorIK.LoadIKSaveSettings(reloadAnimatorIKData);
                originalIK.LoadIKSaveSettings(reloadOriginalIKData);
            }
            UpdateSkeletonShowBoneList();
        }

        public void BonesMirrorInitialize()
        {
            MirrorBoneIndexes = new int[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                MirrorBoneIndexes[i] = -1;

                #region Humanoid
                if (IsHuman)
                {
                    var humanoidIndex = BoneIndex2humanoidIndex[i];
                    if (humanoidIndex >= 0)
                    {
                        var mhi = HumanBodyMirrorBones[(int)humanoidIndex];
                        if (mhi >= 0)
                        {
                            MirrorBoneIndexes[i] = BonesIndexOf(HumanoidBones[(int)mhi]);
                        }
                    }
                }
                #endregion
            }

            UpdateBonesMirrorOther();
        }
        public void BonesMirrorAutomap()
        {
            BonesMirrorInitialize();

            #region Name
            if (VAW.EditorSettings.SettingGenericMirrorName)
            {
                var boneLRIgnorePaths = new string[Bones.Length];
                {
                    {
                        var splits = !string.IsNullOrEmpty(VAW.EditorSettings.SettingGenericMirrorNameDifferentCharacters) ? VAW.EditorSettings.SettingGenericMirrorNameDifferentCharacters.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries) : new string[0];
                        for (int i = 0; i < Bones.Length; i++)
                        {
                            boneLRIgnorePaths[i] = BonePaths[i];
                            foreach (var split in splits)
                            {
                                boneLRIgnorePaths[i] = Regex.Replace(boneLRIgnorePaths[i], split, "*", RegexOptions.IgnoreCase);
                            }
                        }
                    }
                    if (VAW.EditorSettings.SettingGenericMirrorNameIgnoreCharacter && !string.IsNullOrEmpty(VAW.EditorSettings.SettingGenericMirrorNameIgnoreCharacterString))
                    {
                        for (int i = 0; i < Bones.Length; i++)
                        {
                            var splits = boneLRIgnorePaths[i].Split(new string[] { "/" }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (splits.Length <= 0) continue;
                            for (int j = 0; j < splits.Length; j++)
                            {
                                var index = splits[j].IndexOf(VAW.EditorSettings.SettingGenericMirrorNameIgnoreCharacterString);
                                if (index < 0) continue;
                                splits[j] = splits[j][(index + 1)..];
                            }
                            boneLRIgnorePaths[i] = string.Join("/", splits);
                        }
                    }
                }
                {
                    bool[] doneFlag = new bool[Bones.Length];
                    for (int i = 0; i < Bones.Length; i++)
                    {
                        if (doneFlag[i])
                            continue;
                        doneFlag[i] = true;

                        if (MirrorBoneIndexes[i] < 0)
                        {
                            for (int j = 0; j < Bones.Length; j++)
                            {
                                if (i == j || boneLRIgnorePaths[i] != boneLRIgnorePaths[j])
                                    continue;
                                if (IsHuman)
                                {
                                    if (BoneIndex2humanoidIndex[j] >= 0)
                                        continue;
                                }
                                var rootIndex = GetMirrorRootNode(i, j);
                                if (rootIndex < 0)
                                    continue;
                                MirrorBoneIndexes[i] = j;
                                MirrorBoneIndexes[MirrorBoneIndexes[i]] = i;
                                doneFlag[MirrorBoneIndexes[i]] = true;
                                break;
                            }
                        }
                    }
                }
            }
            #endregion

            UpdateBonesMirrorOther();
        }
        private Dictionary<GameObject, GameObject> GetReloadBonesMirror()
        {
            var reload = new Dictionary<GameObject, GameObject>();
            for (int i = 0; i < MirrorBoneIndexes.Length; i++)
            {
                if (MirrorBoneIndexes[i] < 0)
                    continue;
                if (Bones[i] == null || Bones[MirrorBoneIndexes[i]] == null)
                    continue;
                reload.Add(Bones[i], Bones[MirrorBoneIndexes[i]]);
            }
            return reload;
        }
        private void SetReloadBonesMirror(Dictionary<GameObject, GameObject> reload)
        {
            BonesMirrorInitialize();
            foreach (var pair in reload)
            {
                int boneIndex = BonesIndexOf(pair.Key);
                if (boneIndex >= 0)
                {
                    int mirrorBoneIndex = BonesIndexOf(pair.Value);
                    if (mirrorBoneIndex >= 0)
                    {
                        MirrorBoneIndexes[boneIndex] = mirrorBoneIndex;
                    }
                }
            }
            UpdateBonesMirrorOther();
        }
        private void UpdateBonesMirrorOther()
        {
            #region Other
            MirrorBoneDatas = new MirrorBoneData[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                if (MirrorBoneIndexes[i] < 0)
                    continue;
                MirrorBoneDatas[i] = new MirrorBoneData
                {
                    rootBoneIndex = GetMirrorRootNode(i, MirrorBoneIndexes[i])
                };

                #region position
                {
                    MirrorBoneDatas[i].positionTangentInverse = new bool[3];
                    var zeroPosition = BoneSaveTransforms[i].localPosition;
                    var mirrorZeroPosition = GetMirrorBoneLocalPosition(i, zeroPosition);
                    for (int dof = 0; dof < 3; dof++)
                    {
                        var plusPosition = zeroPosition;
                        plusPosition[dof] += 1f;
                        var mirrorPlusPosition = GetMirrorBoneLocalPosition(i, plusPosition);
                        MirrorBoneDatas[i].positionTangentInverse[dof] = Math.Sign((mirrorPlusPosition[dof] - mirrorZeroPosition[dof]) * (plusPosition[dof] - zeroPosition[dof])) < 0;
                    }
                }
                #endregion
                #region rotation
                {
                    MirrorBoneDatas[i].rotationTangentInverse = new bool[4];
                    var zeroRotation = BoneSaveTransforms[i].localRotation;
                    var mirrorZeroRotation = GetMirrorBoneLocalRotation(i, zeroRotation);
                    for (int dof = 0; dof < 4; dof++)
                    {
                        var plusRotation = zeroRotation;
                        {
                            plusRotation[dof] += 1f * Mathf.Deg2Rad;
                            var tmp = new Vector4(plusRotation.x, plusRotation.y, plusRotation.z, plusRotation.w).normalized;
                            plusRotation = new Quaternion(tmp.x, tmp.y, tmp.z, tmp.w);
                        }
                        var mirrorPlusRotation = GetMirrorBoneLocalRotation(i, plusRotation);
                        MirrorBoneDatas[i].rotationTangentInverse[dof] = Math.Sign((mirrorPlusRotation[dof] - mirrorZeroRotation[dof]) * (plusRotation[dof] - zeroRotation[dof])) < 0;
                    }
                }
                #endregion
                #region eulerAngles
                {
                    MirrorBoneDatas[i].eulerAnglesTangentInverse = new bool[3];
                    var zeroRotation = BoneSaveTransforms[i].localRotation;
                    var mirrorZeroRotation = GetMirrorBoneLocalRotation(i, BoneSaveTransforms[i].localRotation);
                    for (int dof = 0; dof < 3; dof++)
                    {
                        var plusRotation = zeroRotation;
                        {
                            var add = Vector3.zero;
                            add[dof] += 1f;
                            plusRotation = Quaternion.Euler(add) * plusRotation;
                        }
                        var mirrorPlusRotation = GetMirrorBoneLocalRotation(i, plusRotation);

                        static Vector3 ToEulerAngles(Quaternion rot)
                        {
                            var euler = rot.eulerAngles;
                            for (int k = 0; k < 3; k++)
                            {
                                if (euler[k] > 180f)
                                    euler[k] = euler[k] - 360f;
                            }
                            return euler;
                        }
                        var zeroRotationE = ToEulerAngles(zeroRotation);
                        var mirrorZeroRotationE = ToEulerAngles(mirrorZeroRotation);
                        var plusRotationE = ToEulerAngles(plusRotation);
                        var mirrorPlusRotationE = ToEulerAngles(mirrorPlusRotation);
                        MirrorBoneDatas[i].eulerAnglesTangentInverse[dof] = Math.Sign((mirrorPlusRotationE[dof] - mirrorZeroRotationE[dof]) * (plusRotationE[dof] - zeroRotationE[dof])) < 0;
                    }
                }
                #endregion
                #region scale
                {
                    MirrorBoneDatas[i].scaleTangentInverse = new bool[3];
                    var zeroScale = BoneSaveTransforms[i].localScale;
                    var mirrorZeroScale = GetMirrorBoneLocalScale(i, zeroScale);
                    for (int dof = 0; dof < 3; dof++)
                    {
                        var plusScale = zeroScale;
                        plusScale[dof] += 1f;
                        var mirrorPlusScale = GetMirrorBoneLocalScale(i, plusScale);
                        MirrorBoneDatas[i].scaleTangentInverse[dof] = Math.Sign((mirrorPlusScale[dof] - mirrorZeroScale[dof]) * (plusScale[dof] - zeroScale[dof])) < 0;
                    }
                }
                #endregion
            }
            #endregion
        }
        private int GetMirrorRootNode(int b1, int b2)
        {
            if (b1 < 0 || b2 < 0)
                return -1;

            var b1s = b1;
            while (ParentBoneIndexes[b1s] >= 0)
            {
                var b2s = b2;
                while (ParentBoneIndexes[b2s] >= 0)
                {
                    if (ParentBoneIndexes[b1s] == ParentBoneIndexes[b2s])
                    {
                        return ParentBoneIndexes[b1s];
                    }
                    b2s = ParentBoneIndexes[b2s];
                }
                b1s = ParentBoneIndexes[b1s];
            }
            return -1;
        }
        public void ChangeBonesMirror(int boneIndex, int mirrorBoneIndex)
        {
            void ActionChildren(int bi, int mbi)
            {
                if (boneIndex < 0)
                    return;

                MirrorBoneIndexes[bi] = mbi;

                #region ParentCheck
                if (MirrorBoneIndexes[bi] >= 0)
                {
                    var index = bi;
                    while (ParentBoneIndexes[index] >= 0)
                    {
                        if (mbi == ParentBoneIndexes[index])
                        {
                            MirrorBoneIndexes[bi] = -1;
                            break;
                        }
                        index = ParentBoneIndexes[index];
                    }
                }
                if (MirrorBoneIndexes[bi] >= 0)
                {
                    var index = mbi;
                    while (ParentBoneIndexes[index] >= 0)
                    {
                        if (bi == ParentBoneIndexes[index])
                        {
                            MirrorBoneIndexes[bi] = -1;
                            break;
                        }
                        index = ParentBoneIndexes[index];
                    }
                }
                #endregion

                #region RootCheck
                if (MirrorBoneIndexes[bi] >= 0)
                {
                    if (GetMirrorRootNode(bi, mbi) < 0)
                    {
                        MirrorBoneIndexes[bi] = -1;
                    }
                }
                #endregion

                if (MirrorBoneIndexes[bi] >= 0)
                {
                    MirrorBoneIndexes[MirrorBoneIndexes[bi]] = bi;
                    {
                        var t = Bones[bi].transform;
                        var mt = Bones[MirrorBoneIndexes[bi]].transform;
                        if (t.childCount == mt.childCount)
                        {
                            for (int i = 0; i < t.childCount; i++)
                            {
                                var ci = BonesIndexOf(t.GetChild(i).gameObject);
                                var mci = BonesIndexOf(mt.GetChild(i).gameObject);
                                ActionChildren(ci, mci);
                            }
                        }
                    }
                }
            }

            ActionChildren(boneIndex, mirrorBoneIndex);

            UpdateBonesMirrorOther();
        }

        public void BlendShapeMirrorInitialize()
        {
            MirrorBlendShape = new Dictionary<SkinnedMeshRenderer, Dictionary<string, string>>();
        }
        public void BlendShapeMirrorAutomap()
        {
            BlendShapeMirrorInitialize();

            if (VAW.EditorSettings.SettingBlendShapeMirrorName)
            {
                foreach (var renderer in VAW.GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount <= 0)
                        continue;
                    var nameTable = new Dictionary<string, string>();
                    {
                        var nameLRIgnorePaths = new string[renderer.sharedMesh.blendShapeCount];
                        {
                            var splits = !string.IsNullOrEmpty(VAW.EditorSettings.SettingBlendShapeMirrorNameDifferentCharacters) ? VAW.EditorSettings.SettingBlendShapeMirrorNameDifferentCharacters.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries) : new string[0];
                            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                            {
                                nameLRIgnorePaths[i] = renderer.sharedMesh.GetBlendShapeName(i);
                                foreach (var split in splits)
                                {
                                    nameLRIgnorePaths[i] = Regex.Replace(nameLRIgnorePaths[i], split, "*", RegexOptions.IgnoreCase);
                                }
                            }
                        }
                        bool[] doneFlag = new bool[renderer.sharedMesh.blendShapeCount];
                        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                        {
                            if (doneFlag[i]) continue;
                            doneFlag[i] = true;
                            var nameI = renderer.sharedMesh.GetBlendShapeName(i);
                            if (nameTable.ContainsKey(nameI))
                                continue;
                            for (int j = 0; j < renderer.sharedMesh.blendShapeCount; j++)
                            {
                                if (i == j || nameLRIgnorePaths[i] != nameLRIgnorePaths[j])
                                    continue;
                                var nameJ = renderer.sharedMesh.GetBlendShapeName(j);
                                if (nameI == nameJ || nameTable.ContainsKey(nameJ))
                                    continue;
                                nameTable.Add(nameI, nameJ);
                                nameTable.Add(nameJ, nameI);
                                doneFlag[j] = true;
                                break;
                            }
                        }
                    }
                    MirrorBlendShape.Add(renderer, nameTable);
                }
            }
        }
        public void ChangeBlendShapeMirror(SkinnedMeshRenderer renderer, string name, string mirrorName)
        {
            if (MirrorBlendShape.TryGetValue(renderer, out Dictionary<string, string> nameTable))
            {
                if (string.IsNullOrEmpty(mirrorName))
                {
                    nameTable.Remove(name);
                }
                else
                {
                    nameTable[name] = mirrorName;
                }
            }
            else if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
            {
                nameTable = new Dictionary<string, string>
                {
                    { name, mirrorName }
                };
                MirrorBlendShape.Add(renderer, nameTable);
            }
        }

        public void OnHierarchyWindowChanged()
        {
            if (IsEditError) return;

            bool changed = false;
            List<GameObject> list = EditorCommon.GetHierarchyGameObject(VAW.GameObject);
            if (Bones.Length != list.Count)
            {
                changed = true;
            }
            if (!changed)
            {
                for (int i = 0; i < Bones.Length; i++)
                {
                    if (Bones[i] != list[i] ||
                        BonePaths[i] != AnimationUtility.CalculateTransformPath(Bones[i].transform, VAW.GameObject.transform))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                StopRecording();

                Selection.activeObject = null;
                SelectGameObjectEvent();

                UpdateBones(false);

                OnHierarchyUpdated?.Invoke();

                SetUpdateSampleAnimation();
                SetSynchroIKtargetAll();
                EditorApplication.delayCall += () =>
                {
                    SetUpdateSampleAnimation();
                    SetSynchroIKtargetAll();
                };
            }
        }
        #endregion

        #region SampleAnimation
        [Flags]
        public enum EditObjectFlag
        {
            Skeleton = (1 << 0),
            SceneObject = (1 << 1),
            All = Skeleton | SceneObject,
        }
        public void SetUpdateSampleAnimation(bool full = false, bool playableGraphRebuild = false)
        {
            updateSampleAnimation = true;

            if (full)
            {
                updateSaveForce = true;
            }

            if (playableGraphRebuild)
            {
                if (UAw.GetLinkedWithTimeline())
                {
#if VERYANIMATION_TIMELINE
                    UAw.UTimelineWindow.Refresh();
#endif
                }
                else
                {
                    UAw.DestroyPlayableGraph();
                }
                animationPlayable?.Release();
            }
        }
        public void SetCurrentTimeAndSampleAnimation(float time, EditObjectFlag flags = EditObjectFlag.All)
        {
            SetCurrentTime(time);
            SampleAnimation(flags);
        }
        public void SampleAnimation(EditObjectFlag flags = EditObjectFlag.All)
        {
            UpdateSyncEditorCurveClip();

            if (flags.HasFlag(EditObjectFlag.SceneObject))
            {
                TransformPoseSave.ResetRootStartTransform();

                if (UAw.GetLinkedWithTimeline())
                {
#if VERYANIMATION_TIMELINE
                    UAw.UTimelineWindow.EvaluateImmediate();
#endif
                }
                else
                {
                    if (VAW.Animator != null)
                    {
                        #region AnimationWindowPlayable
                        var playableGraph = UAw.GetPlayableGraph();
                        if (!playableGraph.IsValid())
                        {
                            UAw.ResampleAnimation();
                            playableGraph = UAw.GetPlayableGraph();
                        }
                        if (playableGraph.IsValid())
                        {
                            var output = playableGraph.GetOutput(0);
                            if (output.IsOutputValid())
                            {
                                var sourcePlayable = output.GetSourcePlayable();

                                #region DisconnectCandidateClipPlayable
                                {
                                    var candidateClipPlayable = UAw.GetCandidateClipPlayable();
                                    if (candidateClipPlayable.IsValid())
                                    {
                                        for (int i = 0; i < candidateClipPlayable.GetOutputCount(); i++)
                                        {
                                            var mixerPlayable = candidateClipPlayable.GetOutput(i);
                                            if (mixerPlayable.IsValid())
                                            {
                                                for (int j = 0; j < mixerPlayable.GetInputCount(); j++)
                                                {
                                                    var playable = mixerPlayable.GetInput(j);
                                                    if (playable.Equals(candidateClipPlayable))
                                                    {
                                                        playableGraph.Disconnect(mixerPlayable, j);
                                                        mixerPlayable.SetInputCount(mixerPlayable.GetInputCount() - 1);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                #region InsertAnimationOffsetPlayable
                                if (!animationPlayable.animationOffsetPlayable.IsValid())
                                {
                                    if (RootMotionBoneIndex >= 0 &&
                                        sourcePlayable.GetPlayableType() == animationPlayable.uAnimationMotionXToDeltaPlayable.PlayableType &&
                                        sourcePlayable.GetInputCount() > 0 && sourcePlayable.GetInput(0).GetPlayableType() != animationPlayable.uAnimationOffsetPlayable.PlayableType)
                                    {
                                        animationPlayable.animationOffsetPlayable = animationPlayable.uAnimationOffsetPlayable.Create(playableGraph, TransformPoseSave.StartLocalPosition, TransformPoseSave.StartLocalRotation, 1);
                                        animationPlayable.animationOffsetPlayable.SetInputWeight(0, 1);
                                        for (int i = 0; i < sourcePlayable.GetInputCount(); i++)
                                        {
                                            var playable = sourcePlayable.GetInput(i);
                                            playableGraph.Disconnect(sourcePlayable, i);
                                            playableGraph.Connect(playable, 0, animationPlayable.animationOffsetPlayable, i);
                                        }
                                        playableGraph.Connect(animationPlayable.animationOffsetPlayable, 0, sourcePlayable, 0);
                                    }
                                }
                                #endregion

                                #region Layers
                                if (animationMode == AnimationMode.Layers)
                                {
                                    var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                                    if (ac != null)
                                    {
                                        var layers = ac.layers;
                                        var mixerPlayable = UAw.GetLayerMixerPlayable();
                                        if (mixerPlayable.IsValid())
                                        {
                                            const int DefaultPoseOffset = 1;
                                            if (mixerPlayable.GetInputCount() != DefaultPoseOffset + layers.Length)
                                            {
                                                for (int i = 0; i < mixerPlayable.GetInputCount(); i++)
                                                    mixerPlayable.DisconnectInput(i);
                                                mixerPlayable.SetInputCount(DefaultPoseOffset + layers.Length);

                                                var clipPlayable = UAw.GetClipPlayable();

                                                #region DefaultPose
                                                {
                                                    UAnimationMode.RevertPropertyModificationsForGameObject(VAW.GameObject);

                                                    var bindings = new HashSet<EditorCurveBinding>(UAnimationUtility.GetAnimationStreamBindings(VAW.GameObject));
                                                    for (int i = 0; i < layers.Length; i++)
                                                    {
                                                        CurrentLayerClips.TryGetValue(layers[i].stateMachine, out AnimationClip clip);
                                                        if (clip == null)
                                                            continue;
                                                        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                                                        {
                                                            bindings.Add(binding);
                                                        }
                                                    }

                                                    {
                                                        if (defaultPoseClip != null)
                                                        {
                                                            AnimationClip.DestroyImmediate(defaultPoseClip);
                                                            defaultPoseClip = null;
                                                        }
                                                        defaultPoseClip = new AnimationClip() { name = "VA DefaultPose" };
                                                        defaultPoseClip.hideFlags |= HideFlags.HideAndDontSave;

                                                        CreateDefaultCurves(defaultPoseClip, bindings.ToArray());
                                                    }

                                                    animationPlayable.defaultPosePlayable = AnimationClipPlayable.Create(playableGraph, defaultPoseClip);
                                                    animationPlayable.defaultPosePlayable.SetApplyFootIK(false);
                                                    animationPlayable.defaultPosePlayable.SetTime(clipPlayable.GetTime());

                                                    mixerPlayable.ConnectInput(0, animationPlayable.defaultPosePlayable, 0, 1f);
                                                }
                                                #endregion

                                                for (int i = 0; i < layers.Length; i++)
                                                {
                                                    CurrentLayerClips.TryGetValue(layers[i].stateMachine, out AnimationClip clip);
                                                    if (clip == null)
                                                        continue;

                                                    AnimationClipPlayable playable;
                                                    if (i != animationLayerIndex)
                                                    {
                                                        playable = AnimationClipPlayable.Create(playableGraph, clip);
                                                        playable.SetTime(clipPlayable.GetTime());
                                                    }
                                                    else
                                                    {
                                                        playable = clipPlayable;
                                                    }
                                                    mixerPlayable.ConnectInput(DefaultPoseOffset + i, playable, 0);
                                                }
                                            }

                                            for (int i = 0; i < layers.Length; i++)
                                            {
                                                mixerPlayable.SetInputWeight(DefaultPoseOffset + i, i == 0 ? 1f : layers[i].defaultWeight);
                                                mixerPlayable.SetLayerAdditive((uint)(DefaultPoseOffset + i), layers[i].blendingMode == UnityEditor.Animations.AnimatorLayerBlendingMode.Additive);
                                                if (layers[i].avatarMask != null)
                                                {
                                                    mixerPlayable.SetLayerMaskFromAvatarMask((uint)(DefaultPoseOffset + i), layers[i].avatarMask);
                                                }
                                                else
                                                {
                                                    mixerPlayable.SetLayerMaskFromAvatarMask((uint)(DefaultPoseOffset + i), blankAvatarMask);
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                if (IsHuman)
                                {
                                    int defaultPoseOffset = 0;
                                    if (animationPlayable.defaultPosePlayable.IsValid())
                                    {
                                        defaultPoseOffset++;
                                    }
                                    else
                                    {
                                        var defaultPosePlayable = UAw.GetDefaultPosePlayable();
                                        if (defaultPosePlayable.IsValid())
                                        {
                                            defaultPoseOffset++;
                                        }
                                    }

                                    #region FootIK
                                    {
                                        var mixerPlayable = UAw.GetLayerMixerPlayable();
                                        if (mixerPlayable.IsValid())
                                        {
                                            for (int i = defaultPoseOffset; i < mixerPlayable.GetInputCount(); i++)
                                            {
                                                var playable = mixerPlayable.GetInput(i);
                                                if (playable.IsValid() &&
                                                    playable.GetPlayableType() == typeof(AnimationClipPlayable))
                                                {
                                                    ((AnimationClipPlayable)playable).SetApplyFootIK(optionsAutoFootIK);
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }

                                UAw.ResampleAnimation();
                            }
                        }
                        #endregion
                    }
                    else if (VAW.Animation != null)
                    {
                        WrapMode? beforeWrapMode = null;
                        try
                        {
                            if (CurrentClip.wrapMode != WrapMode.Default)
                            {
                                beforeWrapMode = CurrentClip.wrapMode;
                                CurrentClip.wrapMode = WrapMode.Default;
                            }
                            CurrentClip.SampleAnimation(VAW.GameObject, CurrentTime);
                        }
                        finally
                        {
                            if (beforeWrapMode.HasValue)
                            {
                                CurrentClip.wrapMode = beforeWrapMode.Value;
                            }
                        }
                    }
                }
            }

            #region DummyObject
            if (flags.HasFlag(EditObjectFlag.Skeleton))
            {
                Skeleton.SetApplyIK(false);
                Skeleton.SetTransformStart();
                Skeleton.SampleAnimation(CurrentClip, CurrentTime);
            }
            #endregion
        }
        public void SampleAnimationLegacy(EditObjectFlag flags = EditObjectFlag.All)
        {
            UpdateSyncEditorCurveClip();

            if (flags.HasFlag(EditObjectFlag.SceneObject))
            {
                TransformPoseSave.ResetRootStartTransform();

                if (UAw.GetLinkedWithTimeline())
                {
#if VERYANIMATION_TIMELINE
                    UAw.UTimelineWindow.EvaluateImmediate();
#endif
                }
                else
                {
                    if (VAW.Animator != null)
                    {
                        var changedRootMotion = VAW.Animator.applyRootMotion;
                        if (changedRootMotion)
                        {
                            VAW.Animator.applyRootMotion = false;
                        }

                        if (!VAW.Animator.isInitialized)
                            VAW.Animator.Rebind();
                        CurrentClip.SampleAnimation(VAW.GameObject, CurrentTime);

                        if (changedRootMotion)
                        {
                            VAW.Animator.applyRootMotion = true;
                        }
                    }
                    else if (VAW.Animation != null)
                    {
                        WrapMode? beforeWrapMode = null;
                        try
                        {
                            if (CurrentClip.wrapMode != WrapMode.Default)
                            {
                                beforeWrapMode = CurrentClip.wrapMode;
                                CurrentClip.wrapMode = WrapMode.Default;
                            }
                            CurrentClip.SampleAnimation(VAW.GameObject, CurrentTime);
                        }
                        finally
                        {
                            if (beforeWrapMode.HasValue)
                            {
                                CurrentClip.wrapMode = beforeWrapMode.Value;
                            }
                        }
                    }
                }
            }

            #region DummyObject
            if (flags.HasFlag(EditObjectFlag.Skeleton))
            {
                Skeleton.SetApplyIK(false);
                Skeleton.SetTransformStart();
                Skeleton.SampleAnimationLegacy(CurrentClip, CurrentTime);
            }
            #endregion
        }

        public void AnimationWindowSampleAnimationOverride(bool callUpdate)
        {
            if (UAw.GetLinkedWithTimeline())
                return;

            var playableGraph = UAw.GetPlayableGraph();
            if (playableGraph.IsValid())
            {
                var output = playableGraph.GetOutput(0);
                if (output.IsOutputValid())
                {
                    var sourcePlayable = output.GetSourcePlayable();

                    bool update = false;

                    #region CheckDisconnectCandidateClipPlayable
                    if (!update)
                    {
                        var candidateClipPlayable = UAw.GetCandidateClipPlayable();
                        if (candidateClipPlayable.IsValid())
                        {
                            for (int i = 0; i < candidateClipPlayable.GetOutputCount(); i++)
                            {
                                var mixerPlayable = candidateClipPlayable.GetOutput(i);
                                if (mixerPlayable.IsValid())
                                {
                                    update = true;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion

                    #region CheckDisableDefaultPosePlayableFootIK
                    if (!update)
                    {
                        var defaultPosePlayable = UAw.GetDefaultPosePlayable();
                        if (defaultPosePlayable.IsValid())
                        {
                            if (defaultPosePlayable.GetApplyFootIK())
                                update = true;
                        }
                    }
                    #endregion

                    #region CheckAnimationOffsetPlayable
                    if (!update)
                    {
                        if (sourcePlayable.GetPlayableType() == animationPlayable.uAnimationMotionXToDeltaPlayable.PlayableType &&
                             sourcePlayable.GetInputCount() > 0 && sourcePlayable.GetInput(0).GetPlayableType() != animationPlayable.uAnimationOffsetPlayable.PlayableType)
                        {
                            update = true;
                        }
                    }
                    #endregion

                    if (callUpdate && !update)
                    {
                        #region Layers
                        if (animationMode == AnimationMode.Layers)
                        {
                            var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                            if (ac != null &&
                                UAnimatorControllerTool.Instance != null &&
                                UEditorWindow.GetEditorWindows(UAnimatorControllerTool.animtorControllerToolLayerSettingsWindowType) != null)
                            {
                                var layers = ac.layers;
                                var mixerPlayable = UAw.GetLayerMixerPlayable();
                                if (mixerPlayable.IsValid())
                                {
                                    for (int i = 0; i < layers.Length; i++)
                                    {
                                        if (i >= mixerPlayable.GetInputCount())
                                            break;
                                        mixerPlayable.SetInputWeight(i, i == 0 ? 1f : layers[i].defaultWeight);
                                        mixerPlayable.SetLayerAdditive((uint)i, layers[i].blendingMode == UnityEditor.Animations.AnimatorLayerBlendingMode.Additive);
                                        if (layers[i].avatarMask != null)
                                        {
                                            mixerPlayable.SetLayerMaskFromAvatarMask((uint)i, layers[i].avatarMask);
                                        }
                                        else
                                        {
                                            mixerPlayable.SetLayerMaskFromAvatarMask((uint)i, blankAvatarMask);
                                        }
                                    }
                                }
                                update = true;
                            }
                        }
                        #endregion
                    }

                    if (update)
                    {
                        SetUpdateSampleAnimation();
                    }
                }
            }

        }
        #endregion

        #region HumanPose
        public void GetSkeletonHumanPose(ref HumanPose humanPose)
        {
            if (!IsHuman) return;

            var handler = Skeleton.HumanPoseHandler;
            var t = Skeleton.GameObject.transform;

            var save = new TransformPoseSave.SaveData(t);
#if UNITY_2022_3_OR_NEWER
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
#endif
            t.localScale = Vector3.one;
            handler.GetHumanPose(ref humanPose);
            save.LoadLocal(t);
        }
        public void GetSceneObjectHumanPose(ref HumanPose humanPose)
        {
            if (!IsHuman) return;

            var handler = HumanPoseHandler;
            var t = VAW.GameObject.transform;

            var save = new TransformPoseSave.SaveData(t);
#if UNITY_2022_3_OR_NEWER
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
#endif
            t.localScale = Vector3.one;
            handler.GetHumanPose(ref humanPose);
            save.LoadLocal(t);
        }
        #endregion

        #region HotKey
        public void HotKeys()
        {
            if (VAW.UEditorGUI.IsEditingTextField())
                return;

            Event e = Event.current;

            void KeyCommmon()
            {
                VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
                e.Use();
            }

            #region Exit edit
            if (!Shortcuts.IsKeyControl(e) && !e.alt && !e.shift && e.keyCode == KeyCode.Escape)
            {
                EditorApplication.delayCall += () =>
                {
                    VAW.Release();
                };
                KeyCommmon();
            }
            #endregion
            #region Pose Quick Save1
            else if (Shortcuts.IsPoseQuickSave1(e))
            {
                VAE.PoseQuickSave(0);
                KeyCommmon();
            }
            #endregion
            #region Pose Quick Load1
            else if (Shortcuts.IsPoseQuickLoad1(e))
            {
                VAE.PoseQuickLoad(0);
                KeyCommmon();
            }
            #endregion
            #region Change Clamp
            else if (Shortcuts.IsChangeClamp(e))
            {
                Undo.RecordObject(VAW, "Change Clamp");
                optionsClampMuscle = !optionsClampMuscle;
                KeyCommmon();
            }
            #endregion
            #region Change Foot IK
            else if (Shortcuts.IsChangeFootIK(e))
            {
                Undo.RecordObject(VAW, "Change Foot IK");
                optionsAutoFootIK = !optionsAutoFootIK;
                SetUpdateSampleAnimation();
                SetSynchroIKtargetAll();
                SetAnimationWindowSynchroSelection();
                KeyCommmon();
            }
            #endregion
            #region Change Mirror
            else if (Shortcuts.IsChangeMirror(e))
            {
                Undo.RecordObject(VAW, "Change Mirror");
                optionsMirror = !optionsMirror;
                SetAnimationWindowSynchroSelection();
                KeyCommmon();
            }
            #endregion
            #region Change Root Correction Mode
            else if (Shortcuts.IsChangeRootCorrectionMode(e))
            {
                Undo.RecordObject(VAW, "Change Root Correction Mode");
                rootCorrectionMode = (RootCorrectionMode)((int)(++rootCorrectionMode) % ((int)RootCorrectionMode.Full + 1));
                SetAnimationWindowSynchroSelection();
                KeyCommmon();
            }
            #endregion
            #region Change selection bone IK
            else if (Shortcuts.IsChangeSelectionBoneIK(e))
            {
                IKChangeSelection();
                SetAnimationWindowSynchroSelection();
                KeyCommmon();
            }
            #endregion
            #region Force refresh
            else if (Shortcuts.IsForceRefresh(e))
            {
                SetUpdateSampleAnimation(true, true);
                SetSynchroIKtargetAll();
                UAw.ForceRefresh();
                SetAnimationWindowSynchroSelection();
                KeyCommmon();
            }
            #endregion
            #region Next animation clip
            else if (Shortcuts.IsNextAnimationClip(e))
            {
                if (!UAw.GetLinkedWithTimeline())
                {
                    VAW.MoveChangeSelectionAnimationClip(1);
                    KeyCommmon();
                }
            }
            #endregion
            #region Previous animation clip
            else if (Shortcuts.IsPreviousAnimationClip(e))
            {
                if (!UAw.GetLinkedWithTimeline())
                {
                    VAW.MoveChangeSelectionAnimationClip(-1);
                    KeyCommmon();
                }
            }
            #endregion
            #region AddInbetween
            else if (Shortcuts.IsAddInbetween(e))
            {
                AddRemoveInbetween(1);
                KeyCommmon();
            }
            #endregion
            #region RemoveInbetween
            else if (Shortcuts.IsRemoveInbetween(e))
            {
                AddRemoveInbetween(-1);
                KeyCommmon();
            }
            #endregion
            #region Hide select bones
            else if (Shortcuts.IsHideSelectBones(e))
            {
                Undo.RecordObject(VAW, "Change Show Flag");
                if (SelectionBones != null)
                {
                    foreach (var boneIndex in SelectionBones)
                    {
                        boneShowFlags[boneIndex] = false;
                    }
                }
                OnBoneShowFlagsUpdated.Invoke();
                KeyCommmon();
            }
            #endregion
            #region Show select bones
            else if (Shortcuts.IsShowSelectBones(e))
            {
                Undo.RecordObject(VAW, "Change Show Flag");
                if (SelectionBones != null)
                {
                    foreach (var boneIndex in SelectionBones)
                    {
                        boneShowFlags[boneIndex] = true;
                    }
                }
                OnBoneShowFlagsUpdated.Invoke();
                KeyCommmon();
            }
            #endregion
            #region Preview/Change playing
            else if (Shortcuts.IsPreviewChangePlaying(e))
            {
                if (UAvatarPreview != null)
                {
                    UAvatarPreview.Playing = !UAvatarPreview.Playing;
                    if (UAvatarPreview.Playing)
                        UAvatarPreview.SetTime(0f);
                    else
                        UAvatarPreview.SetTime(UAw.GetCurrentTime());
                }
                KeyCommmon();
            }
            #endregion
            #region Add IK - Level / Direction
            else if (Shortcuts.IsAddIKLevel(e))
            {
                if (originalIK.IKActiveTarget >= 0)
                {
                    Undo.RecordObject(VAW, "Change Original IK Data");
                    for (int i = 0; i < originalIK.ikTargetSelect.Length; i++)
                    {
                        originalIK.ChangeTypeSetting(originalIK.ikTargetSelect[i], 1);
                    }
                }
                KeyCommmon();
            }
            #endregion
            #region Sub IK - Level / Direction
            else if (Shortcuts.IsSubIKLevel(e))
            {
                if (originalIK.IKActiveTarget >= 0)
                {
                    Undo.RecordObject(VAW, "Change Original IK Data");
                    for (int i = 0; i < originalIK.ikTargetSelect.Length; i++)
                    {
                        originalIK.ChangeTypeSetting(originalIK.ikTargetSelect[i], -1);
                    }
                }
                KeyCommmon();
            }
            #endregion
            #region Change playing
            else if (Shortcuts.IsAnimationChangePlaying(e))
            {
                UAw.PlayingChange();
                KeyCommmon();
            }
            #endregion
            #region Switch between curves and dope sheet
            else if (Shortcuts.IsAnimationSwitchBetweenCurvesAndDopeSheet(e))
            {
                UAw.SwitchBetweenCurvesAndDopesheet();
                EditorApplication.delayCall += () =>
                {
                    SetAnimationWindowSynchroSelection();
                };
                KeyCommmon();
            }
            #endregion
            #region Add keyframe
            else if (Shortcuts.IsAddKeyframe(e))
            {
                var tool = CurrentTool();
                if (IsHuman)
                {
                    foreach (var humanoidIndex in SelectionGameObjectsHumanoidIndex())
                    {
                        switch (tool)
                        {
                            case Tool.Move:
                                if (HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] != null)
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                    {
                                        var curve = GetAnimationCurveAnimatorTDOF(HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index, dof);
                                        if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                            SetAnimationCurveAnimatorTDOF(HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index, dof, curve);
                                    }
                                }
                                break;
                            case Tool.Rotate:
                                for (int dof = 0; dof < 3; dof++)
                                {
                                    var muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, dof);
                                    if (muscleIndex >= 0)
                                    {
                                        var curve = GetAnimationCurveAnimatorMuscle(muscleIndex);
                                        if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                            SetAnimationCurveAnimatorMuscle(muscleIndex, curve);
                                    }
                                }
                                break;
                        }
                    }
                    if (SelectionGameObjectsIndexOf(VAW.GameObject) >= 0)
                    {
                        switch (tool)
                        {
                            case Tool.Move:
                                for (int dof = 0; dof < 3; dof++)
                                {
                                    var curve = GetAnimationCurveAnimatorRootT(dof);
                                    if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                        SetAnimationCurveAnimatorRootT(dof, curve);
                                }
                                break;
                            case Tool.Rotate:
                                for (int dof = 0; dof < 4; dof++)
                                {
                                    var curve = GetAnimationCurveAnimatorRootQ(dof);
                                    if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                        SetAnimationCurveAnimatorRootQ(dof, curve);
                                }
                                break;
                        }
                    }
                    if (animatorIK.ikTargetSelect != null)
                    {
                        foreach (var ikTarget in animatorIK.ikTargetSelect)
                        {
                            SetUpdateIKtargetAnimatorIK(ikTarget);
                        }
                    }
                }
                foreach (var boneIndex in SelectionGameObjectsOtherHumanoidBoneIndex())
                {
                    switch (tool)
                    {
                        case Tool.Move:
                            for (int dof = 0; dof < 3; dof++)
                            {
                                var curve = GetAnimationCurveTransformPosition(boneIndex, dof);
                                if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                    SetAnimationCurveTransformPosition(boneIndex, dof, curve);
                            }
                            break;
                        case Tool.Rotate:
                            {
                                //Rotation is difficult to handle depending on the mode, so be sure to write it down.
                                var mode = GetHaveAnimationCurveTransformRotationMode(boneIndex);
                                if (mode == URotationCurveInterpolation.Mode.Undefined)
                                    mode = URotationCurveInterpolation.Mode.RawQuaternions;
                                if (mode == URotationCurveInterpolation.Mode.RawQuaternions)
                                {
                                    for (int dof = 0; dof < 4; dof++)
                                    {
                                        var curve = GetAnimationCurveTransformRotation(boneIndex, dof, mode);
                                        AddInbetweenKeyframe(curve, CurrentTime);
                                        SetAnimationCurveTransformRotation(boneIndex, dof, mode, curve);
                                    }
                                }
                                else
                                {
                                    for (int dof = 0; dof < 3; dof++)
                                    {
                                        var curve = GetAnimationCurveTransformRotation(boneIndex, dof, mode);
                                        AddInbetweenKeyframe(curve, CurrentTime);
                                        SetAnimationCurveTransformRotation(boneIndex, dof, mode, curve);
                                    }
                                }
                            }
                            break;
                        case Tool.Scale:
                            for (int dof = 0; dof < 3; dof++)
                            {
                                var curve = GetAnimationCurveTransformScale(boneIndex, dof);
                                if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                    SetAnimationCurveTransformScale(boneIndex, dof, curve);
                            }
                            break;
                    }
                }
                if (originalIK.ikTargetSelect != null)
                {
                    foreach (var ikTarget in originalIK.ikTargetSelect)
                    {
                        SetUpdateIKtargetOriginalIK(ikTarget);
                    }
                }
                if (SelectionMotionTool)
                {
                    switch (tool)
                    {
                        case Tool.Move:
                            for (int dof = 0; dof < 3; dof++)
                            {
                                var curve = GetAnimationCurveAnimatorMotionT(dof);
                                if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                    SetAnimationCurveAnimatorMotionT(dof, curve);
                            }
                            break;
                        case Tool.Rotate:
                            for (int dof = 0; dof < 4; dof++)
                            {
                                var curve = GetAnimationCurveAnimatorMotionQ(dof);
                                if (AddInbetweenKeyframe(curve, CurrentTime) >= 0)
                                    SetAnimationCurveAnimatorMotionQ(dof, curve);
                            }
                            break;
                    }
                }
                KeyCommmon();
            }
            #endregion
            #region Move to next frame
            else if (Shortcuts.IsMoveToNextFrame(e))
            {
                UAw.MoveToNextFrame();
                KeyCommmon();
            }
            #endregion
            #region Move to previous frame
            else if (Shortcuts.IsMoveToPrevFrame(e))
            {
                UAw.MoveToPrevFrame();
                KeyCommmon();
            }
            #endregion
            #region Move to next keyframe
            else if (Shortcuts.IsMoveToNextKeyframe(e))
            {
                UAw.MoveToNextKeyframe();
                KeyCommmon();
            }
            #endregion
            #region Move to previous keyframe
            else if (Shortcuts.IsMoveToPreviousKeyframe(e))
            {
                UAw.MoveToPreviousKeyframe();
                KeyCommmon();
            }
            #endregion
            #region Move to first keyframe
            else if (Shortcuts.IsMoveToFirstKeyframe(e))
            {
                UAw.MoveToFirstKeyframe();
                KeyCommmon();
            }
            #endregion
            #region Move to last keyframe
            else if (Shortcuts.IsMoveToLastKeyframe(e))
            {
                UAw.MoveToLastKeyframe();
                KeyCommmon();
            }
            #endregion
        }
        public void Commands()
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.ValidateCommand:
                    {
                        if (e.commandName == "Cut" ||
                            e.commandName == "Copy" ||
                            e.commandName == "Paste" ||
                            e.commandName == "SelectAll" ||
                            e.commandName == "FrameSelected" ||
                            e.commandName == "FrameSelectedWithLock" ||
                            e.commandName == "Delete" ||
                            e.commandName == "SoftDelete" ||
                            e.commandName == "Duplicate")
                        {
                            e.Use();
                        }
                    }
                    break;
                case EventType.ExecuteCommand:
                    {
                        if (e.commandName == "Cut" ||
                            e.commandName == "Delete" ||
                            e.commandName == "SoftDelete" ||
                            e.commandName == "Duplicate")
                        {
                            e.Use();
                        }
                        else if (e.commandName == "Copy")
                        {
                            if (CommandCopy())
                                e.Use();
                        }
                        else if (e.commandName == "Paste")
                        {
                            if (CommandPaste())
                                e.Use();
                        }
                        else if (e.commandName == "SelectAll")
                        {
                            if (CommandSelectAll())
                                e.Use();
                        }
                        else if (e.commandName == "FrameSelected")
                        {
                            if (CommandFrameSelected(false))
                                e.Use();
                        }
                        else if (e.commandName == "FrameSelectedWithLock")
                        {
                            if (CommandFrameSelected(true))
                                e.Use();
                        }
                    }
                    break;
            }
        }
        private bool CommandCopy()
        {
            if (copyPaste != null)
            {
                GameObject.DestroyImmediate(copyPaste);
                copyPaste = null;
            }
            copyAnimatorIKTargetData = null;
            copyOriginalIKTargetData = null;

            if (SelectionActiveGameObject != null)
            {
                copyPaste = ScriptableObject.CreateInstance<PoseTemplate>();
                SaveSelectionPoseTemplate(copyPaste);
                copyDataType = CopyDataType.SelectionPose;
            }
            else if (animatorIK.ikTargetSelect != null && animatorIK.ikTargetSelect.Length > 0)
            {
                copyAnimatorIKTargetData = new CopyAnimatorIKTargetData[animatorIK.ikTargetSelect.Length];
                for (int i = 0; i < animatorIK.ikTargetSelect.Length; i++)
                {
                    var index = (int)animatorIK.ikTargetSelect[i];
                    var data = animatorIK.ikData[index];
                    copyAnimatorIKTargetData[i] = new CopyAnimatorIKTargetData()
                    {
                        ikTarget = (AnimatorIKCore.IKTarget)index,
                        autoRotation = data.autoRotation,
                        spaceType = data.spaceType,
                        parent = data.parent,
                        position = data.position,
                        rotation = data.rotation,
                        swivelRotation = data.swivelRotation,
                    };
                }
                copyDataType = CopyDataType.AnimatorIKTarget;
            }
            else if (originalIK.ikTargetSelect != null && originalIK.ikTargetSelect.Length > 0)
            {
                copyOriginalIKTargetData = new CopyOriginalIKTargetData[originalIK.ikTargetSelect.Length];
                for (int i = 0; i < originalIK.ikTargetSelect.Length; i++)
                {
                    var index = originalIK.ikTargetSelect[i];
                    var data = originalIK.ikData[index];
                    copyOriginalIKTargetData[i] = new CopyOriginalIKTargetData()
                    {
                        ikTarget = index,
                        autoRotation = data.autoRotation,
                        spaceType = data.spaceType,
                        parent = data.parent,
                        position = data.position,
                        rotation = data.rotation,
                        swivel = data.swivel,
                    };
                }
                copyDataType = CopyDataType.OriginalIKTarget;
            }
            else
            {
                copyPaste = ScriptableObject.CreateInstance<PoseTemplate>();
                SavePoseTemplate(copyPaste);
                copyDataType = CopyDataType.FullPose;
            }
            return true;
        }
        private bool CommandPaste()
        {
            switch (copyDataType)
            {
                case CopyDataType.None:
                    break;
                case CopyDataType.SelectionPose:
                    if (copyPaste != null)
                    {
                        LoadPoseTemplate(copyPaste, PoseFlags.All, true);
                    }
                    break;
                case CopyDataType.FullPose:
                    if (copyPaste != null)
                    {
                        LoadPoseTemplate(copyPaste);
                    }
                    break;
                case CopyDataType.AnimatorIKTarget:
                    if (copyAnimatorIKTargetData != null)
                    {
                        Undo.RecordObject(VAW, "Paste");
                        foreach (var copyData in copyAnimatorIKTargetData)
                        {
                            var index = (int)copyData.ikTarget;
                            if (index < 0 || index >= animatorIK.ikData.Length)
                                continue;
                            var data = animatorIK.ikData[index];
                            {
                                data.autoRotation = copyData.autoRotation;
                                data.spaceType = copyData.spaceType;
                                data.parent = copyData.parent;
                                data.position = copyData.position;
                                data.rotation = copyData.rotation;
                                data.swivelRotation = copyData.swivelRotation;
                            }
                            animatorIK.UpdateOptionPosition(copyData.ikTarget);
                            animatorIK.UpdateSwivelPosition(copyData.ikTarget);
                            SetUpdateIKtargetAnimatorIK(copyData.ikTarget);
                        }
                    }
                    break;
                case CopyDataType.OriginalIKTarget:
                    if (copyOriginalIKTargetData != null)
                    {
                        Undo.RecordObject(VAW, "Paste");
                        foreach (var copyData in copyOriginalIKTargetData)
                        {
                            var index = copyData.ikTarget;
                            if (index < 0 || index >= originalIK.ikData.Count)
                                continue;
                            var data = originalIK.ikData[index];
                            {
                                data.autoRotation = copyData.autoRotation;
                                data.spaceType = copyData.spaceType;
                                data.parent = copyData.parent;
                                data.position = copyData.position;
                                data.rotation = copyData.rotation;
                                data.swivel = copyData.swivel;
                            }
                            SetUpdateIKtargetOriginalIK(copyData.ikTarget);
                        }
                    }
                    break;
                default:
                    break;
            }
            return true;
        }
        private bool CommandSelectAll()
        {
            var selectObjects = new List<GameObject>(Bones.Length);
            for (int i = 0; i < Bones.Length; i++)
            {
                if (!IsShowBone(i)) continue;
                selectObjects.Add(Bones[i]);
            }
            var selectVirtual = new List<HumanBodyBones>(HumanVirtualBones.Length);
            for (int i = 0; i < HumanVirtualBones.Length; i++)
            {
                if (!IsShowVirtualBone((HumanBodyBones)i)) continue;
                selectVirtual.Add((HumanBodyBones)i);
            }
            SelectGameObjects(selectObjects.ToArray(), selectVirtual.ToArray());
            return true;
        }
        private bool CommandFrameSelected(bool withLock)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return false;
            var bounds = GetSelectionBounds(0.333f);
            USceneView.SetViewIsLockedToObject(sceneView, withLock);
            sceneView.FixNegativeSize();
            USceneView.Frame(sceneView, bounds, EditorApplication.isPlaying);

            return true;
        }
        #endregion

        #region SelectionGameObject
        public void SelectGameObjectEvent(bool selectionChangedClear = false)
        {
            bool selectionChanged = false;
            if (selectionChangedClear)
            {
                selectionChanged |= SelectionGameObjects == null;
                if (!selectionChanged)
                    selectionChanged = SelectionGameObjects.Count != Selection.gameObjects.Length;
                if (!selectionChanged && SelectionGameObjects.Count > 0)
                    selectionChanged = SelectionGameObjects[0] != Selection.activeGameObject;
                if (!selectionChanged)
                {
                    foreach (var gameObject in Selection.gameObjects)
                    {
                        if (!SelectionGameObjects.Contains(gameObject))
                        {
                            selectionChanged = true;
                            break;
                        }
                    }
                }
            }

            #region selectionGameObjects
            {
                SelectionGameObjects ??= new List<GameObject>();
                SelectionGameObjects.Clear();
                SelectionGameObjects.AddRange(Selection.gameObjects);
                if (Selection.activeGameObject != null)
                {
                    SelectionGameObjects.Remove(Selection.activeGameObject);
                    SelectionGameObjects.Insert(0, Selection.activeGameObject);
                }
            }
            #endregion

            #region selectionBones
            {
                SelectionBones ??= new List<int>();
                SelectionBones.Clear();
                foreach (var go in SelectionGameObjects)
                {
                    var boneIndex = BonesIndexOf(go);
                    if (boneIndex < 0) continue;
                    SelectionBones.Add(boneIndex);
                }
            }
            #endregion

            #region CheckParentChange
            {
                bool changed = false;
                foreach (var boneIndex in SelectionBones)
                {
                    var parent = Bones[boneIndex].transform.parent;
                    int parentBoneIndex = -1;
                    if (parent != null)
                        parentBoneIndex = BonesIndexOf(parent.gameObject);
                    if (ParentBoneIndexes[boneIndex] != parentBoneIndex)
                    {
                        changed = true;
                        break;
                    }
                }
                if (changed)
                {
                    OnHierarchyWindowChanged();
                }
            }
            #endregion

            #region SceneWindow
            VAW.editorWindowSelectionRect.size = Vector2.zero;
            #endregion

            if (selectionChangedClear && selectionChanged && SelectionGameObjects.Count > 0)
            {
                SelectionHumanVirtualBones = null;
                SelectionMotionTool = false;
                ClearIkTargetSelect();
                SetUpdateSampleAnimation();
                SetAnimationWindowSynchroSelection();
                VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
            }
        }
        public void SelectGameObjectMouseDrag(GameObject[] go, HumanBodyBones[] virtualBones, AnimatorIKCore.IKTarget[] animatorIKTarget, int[] originalIKTarget)
        {
            Undo.RecordObject(VAW, "Change Selection");
            Selection.objects = go;
            SelectionHumanVirtualBones = virtualBones != null ? new List<HumanBodyBones>(virtualBones) : null;
            SelectionMotionTool = false;
            animatorIK.ikTargetSelect = animatorIKTarget;
            animatorIK.OnSelectionChange();
            originalIK.ikTargetSelect = originalIKTarget;
            originalIK.OnSelectionChange();
            SetAnimationWindowSynchroSelection();
        }
        public void SelectGameObjectPlusKey(GameObject go)
        {
            var select = new List<GameObject>();
            if (go != null)
                select.Add(go);
            var selectVirtual = new List<HumanBodyBones>();
            var e = Event.current;
            if (e.alt)
            {
                if (go != null)
                {
                    var boneIndex = BonesIndexOf(go);
                    ActionAllBoneChildren(boneIndex, (ci) =>
                    {
                        select.Add(Bones[ci]);
                    });
                    ActionAllVirtualBoneChildren(boneIndex, (cvhi) =>
                    {
                        selectVirtual.Add(cvhi);
                    });
                }
            }
            if (Shortcuts.IsKeyControl(e) || e.shift)
            {
                SelectionHumanVirtualBones?.AddRange(SelectionHumanVirtualBones);
                if (SelectionGameObjects != null)
                {
                    foreach (var o in SelectionGameObjects)
                    {
                        if (select.Contains(o))
                            select.Remove(o);
                        else
                            select.Add(o);
                    }
                }
            }
            if (go != null && select.Contains(go))
                Selection.activeGameObject = go;
            SelectGameObjects(select.ToArray(), selectVirtual.ToArray());
        }
        public void SelectGameObject(GameObject go)
        {
            Undo.RecordObject(VAW, "Change Selection");
            Selection.objects = new UnityEngine.Object[] { go };
            SelectionHumanVirtualBones = null;
            SelectionMotionTool = false;
            ClearIkTargetSelect();
            SetUpdateSampleAnimation();
            SetAnimationWindowSynchroSelection();
            VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
        }
        public void SelectGameObjects(GameObject[] go, HumanBodyBones[] virtualBones = null)
        {
            Undo.RecordObject(VAW, "Change Selection");
            Selection.objects = go;
            SelectionHumanVirtualBones = virtualBones != null ? new List<HumanBodyBones>(virtualBones) : null;
            SelectionMotionTool = false;
            ClearIkTargetSelect();
            SetUpdateSampleAnimation();
            SetAnimationWindowSynchroSelection();
            VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
        }
        public void SelectVirtualBonePlusKey(HumanBodyBones humanoidIndex)
        {
            if (HumanoidIndex2boneIndex[(int)humanoidIndex] >= 0)
                return;

            var select = new List<GameObject>();
            var selectVirtual = new List<HumanBodyBones>
            {
                humanoidIndex
            };
            var e = Event.current;
            if (e.alt)
            {
                void VirtualNeck()
                {
                    int boneIndex;
                    if (HumanoidIndex2boneIndex[(int)HumanBodyBones.Neck] >= 0)
                        boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.Neck];
                    else
                    {
                        selectVirtual.Add(HumanBodyBones.Neck);
                        boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.Head];
                    }
                    select.Add(Bones[boneIndex]);
                    ActionAllBoneChildren(boneIndex, (ci) =>
                    {
                        select.Add(Bones[ci]);
                    });
                }
                void VirtualLeftShoulder()
                {
                    int boneIndex;
                    if (HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftShoulder] >= 0)
                        boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftShoulder];
                    else
                    {
                        selectVirtual.Add(HumanBodyBones.LeftShoulder);
                        boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftUpperArm];
                    }
                    select.Add(Bones[boneIndex]);
                    ActionAllBoneChildren(boneIndex, (ci) =>
                    {
                        select.Add(Bones[ci]);
                    });
                }
                void VirtualRightShoulder()
                {
                    int boneIndex;
                    if (HumanoidIndex2boneIndex[(int)HumanBodyBones.RightShoulder] >= 0)
                        boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.RightShoulder];
                    else
                    {
                        selectVirtual.Add(HumanBodyBones.RightShoulder);
                        boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.RightUpperArm];
                    }
                    select.Add(Bones[boneIndex]);
                    ActionAllBoneChildren(boneIndex, (ci) =>
                    {
                        select.Add(Bones[ci]);
                    });
                }
                switch (humanoidIndex)
                {
                    case HumanBodyBones.Chest:
                        selectVirtual.Add(HumanBodyBones.UpperChest);
                        VirtualNeck();
                        VirtualLeftShoulder();
                        VirtualRightShoulder();
                        break;
                    case HumanBodyBones.Neck:
                        VirtualNeck();
                        break;
                    case HumanBodyBones.LeftShoulder:
                        VirtualLeftShoulder();
                        break;
                    case HumanBodyBones.RightShoulder:
                        VirtualRightShoulder();
                        break;
                    case HumanBodyBones.UpperChest:
                        VirtualNeck();
                        VirtualLeftShoulder();
                        VirtualRightShoulder();
                        break;
                    default:
                        Assert.IsTrue(false);
                        break;
                }
            }
            if (Shortcuts.IsKeyControl(e) || e.shift)
            {
                SelectionGameObjects?.AddRange(SelectionGameObjects);
                if (SelectionHumanVirtualBones != null)
                {
                    foreach (var h in SelectionHumanVirtualBones)
                    {
                        if (selectVirtual.Contains(h))
                            selectVirtual.Remove(h);
                        else
                            selectVirtual.Add(h);
                    }
                }
            }
            SelectGameObjects(select.ToArray(), selectVirtual.ToArray());
        }
        public void SelectHumanoidBones(HumanBodyBones[] bones)
        {
            var goList = new List<GameObject>();
            var virtualList = new List<HumanBodyBones>();
            foreach (var hi in bones)
            {
                if (hi < 0)
                    goList.Add(VAW.GameObject);
                else if (HumanoidBones[(int)hi] != null)
                    goList.Add(HumanoidBones[(int)hi]);
                else if (HumanVirtualBones[(int)hi] != null)
                    virtualList.Add(hi);
            }
            Undo.RecordObject(VAW, "Change Selection");
            Selection.objects = goList.ToArray();
            SelectionHumanVirtualBones = virtualList;
            SelectionMotionTool = false;
            ClearIkTargetSelect();
            SetUpdateSampleAnimation();
            SetAnimationWindowSynchroSelection();
            VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
        }
        public void SelectAnimatorIKTargetPlusKey(AnimatorIKCore.IKTarget ikTarget)
        {
            var select = new List<AnimatorIKCore.IKTarget>
            {
                ikTarget
            };
            var e = Event.current;
            if (e != null && (Shortcuts.IsKeyControl(e) || e.shift))
            {
                if (animatorIK.ikTargetSelect != null)
                {
                    select = new List<AnimatorIKCore.IKTarget>(animatorIK.ikTargetSelect);
                    if (EditorCommon.ArrayContains(animatorIK.ikTargetSelect, ikTarget))
                        select.Remove(ikTarget);
                    else
                        select.Add(ikTarget);
                }
            }
            SelectIKTargets(select.ToArray(), null);
        }
        public void SelectOriginalIKTargetPlusKey(int ikTarget)
        {
            var select = new List<int>
            {
                ikTarget
            };
            var e = Event.current;
            if (e != null && (Shortcuts.IsKeyControl(e) || e.shift))
            {
                if (originalIK.ikTargetSelect != null)
                {
                    select = new List<int>(originalIK.ikTargetSelect);
                    if (EditorCommon.ArrayContains(originalIK.ikTargetSelect, ikTarget))
                        select.Remove(ikTarget);
                    else
                        select.Add(ikTarget);
                }
            }
            SelectIKTargets(null, select.ToArray());
        }
        public void SelectIKTargets(AnimatorIKCore.IKTarget[] animatorIKTargets, int[] originalIKTargets)
        {
            Undo.RecordObject(VAW, "Change Selection");
            Selection.activeGameObject = null;
            SelectionHumanVirtualBones = null;
            SelectionMotionTool = false;
            animatorIK.ikTargetSelect = animatorIKTargets;
            animatorIK.OnSelectionChange();
            originalIK.ikTargetSelect = originalIKTargets;
            originalIK.OnSelectionChange();
            SetUpdateSampleAnimation();
            SetAnimationWindowSynchroSelection();
            VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
        }
        public void SelectMotionTool()
        {
            Undo.RecordObject(VAW, "Change Selection");
            Selection.activeGameObject = null;
            SelectionHumanVirtualBones = null;
            SelectionMotionTool = true;
            ClearIkTargetSelect();
            SetUpdateSampleAnimation();
            SetAnimationWindowSynchroSelection();
            VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
        }
        public void SetAnimationWindowSynchroSelection(EditorCurveBinding[] bindings = null)
        {
            animationWindowSynchroSelection = true;
            animationWindowSynchroSelectionBindings.Clear();
            if (bindings != null)
            {
                foreach (var binding in bindings)
                {
                    if (!animationWindowSynchroSelectionBindings.Contains(binding))
                        animationWindowSynchroSelectionBindings.Add(binding);
                }
            }
        }

        public int SelectionGameObjectsIndexOf(GameObject go)
        {
            if (SelectionGameObjects != null)
            {
                for (int i = 0; i < SelectionGameObjects.Count; i++)
                {
                    if (SelectionGameObjects[i] == go)
                        return i;
                }
            }
            return -1;
        }
        public bool SelectionGameObjectsContains(HumanBodyBones humanIndex)
        {
            if (SelectionBones != null)
            {
                foreach (var boneIndex in SelectionBones)
                {
                    if (BoneIndex2humanoidIndex[boneIndex] == humanIndex)
                        return true;
                }
            }
            if (SelectionHumanVirtualBones != null)
            {
                foreach (var vb in SelectionHumanVirtualBones)
                {
                    if (vb == humanIndex)
                        return true;
                }
            }
            return false;
        }
        public HumanBodyBones SelectionGameObjectHumanoidIndex()
        {
            var humanoidIndex = HumanoidBonesIndexOf(SelectionActiveGameObject);
            if (humanoidIndex < 0 && SelectionHumanVirtualBones != null && SelectionHumanVirtualBones.Count > 0)
                humanoidIndex = SelectionHumanVirtualBones[0];
            return humanoidIndex;
        }
        public List<HumanBodyBones> SelectionGameObjectsHumanoidIndex()
        {
            var list = new List<HumanBodyBones>();
            if (IsHuman)
            {
                if (SelectionBones != null)
                {
                    foreach (var boneIndex in SelectionBones)
                    {
                        var humanoidIndex = BoneIndex2humanoidIndex[boneIndex];
                        if (humanoidIndex < 0) continue;
                        list.Add(humanoidIndex);
                    }
                }
                if (SelectionHumanVirtualBones != null)
                {
                    foreach (var humanoidIndex in SelectionHumanVirtualBones)
                    {
                        if (humanoidIndex < 0) continue;
                        list.Add(humanoidIndex);
                    }
                }
            }
            return list;
        }
        public bool IsSelectionGameObjectsHumanoidIndexContains(HumanBodyBones humanoidIndex)
        {
            if (IsHuman)
            {
                if (SelectionBones != null)
                {
                    foreach (var boneIndex in SelectionBones)
                    {
                        if (BoneIndex2humanoidIndex[boneIndex] == humanoidIndex)
                            return true;
                    }
                }
                if (SelectionHumanVirtualBones != null)
                {
                    foreach (var hi in SelectionHumanVirtualBones)
                    {
                        if (hi == humanoidIndex)
                            return true;
                    }
                }
            }
            return false;
        }
        public List<int> SelectionGameObjectsOtherHumanoidBoneIndex()
        {
            var list = new List<int>();
            if (IsHuman)
            {
                if (SelectionBones != null)
                {
                    foreach (var boneIndex in SelectionBones)
                    {
                        if (boneIndex == RootMotionBoneIndex ||
                            BoneIndex2humanoidIndex[boneIndex] >= 0) continue;
                        list.Add(boneIndex);
                    }
                }
            }
            else
            {
                if (SelectionBones != null)
                {
                    list.AddRange(SelectionBones);
                }
            }
            return list;
        }
        public List<int> SelectionGameObjectsMuscleIndex(int dofIndex = -1)
        {
            var list = new List<int>();
            var humanoidIndexs = SelectionGameObjectsHumanoidIndex();
            if (dofIndex < 0)
            {
                foreach (var humanoidIndex in humanoidIndexs)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, j);
                        if (muscleIndex < 0) continue;
                        list.Add(muscleIndex);
                    }
                }
            }
            else if (dofIndex >= 0 && dofIndex <= 2)
            {
                foreach (var humanoidIndex in humanoidIndexs)
                {
                    int muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, dofIndex);
                    if (muscleIndex < 0) continue;
                    list.Add(muscleIndex);
                }
            }
            return list;
        }
        public float GetSelectionSuppressPowerRate()
        {
            int maxLevel = 1;
            if (SelectionBones != null)
            {
                foreach (var boneIndex in SelectionBones)
                {
                    int level = 1;
                    var bi = boneIndex;
                    while (ParentBoneIndexes[bi] >= 0)
                    {
                        if (SelectionBones.Contains(ParentBoneIndexes[bi]))
                            level++;
                        bi = ParentBoneIndexes[bi];
                    }
                    maxLevel = Math.Max(maxLevel, level);
                }
            }
            if (IsHuman)
            {
                foreach (var humanoidIndex in SelectionGameObjectsHumanoidIndex())
                {
                    int level = 1;
                    var hi = humanoidIndex;
                    var parentHi = (HumanBodyBones)HumanTrait.GetParentBone((int)hi);
                    while (parentHi >= 0)
                    {
                        if (IsSelectionGameObjectsHumanoidIndexContains(parentHi))
                            level++;
                        hi = parentHi;
                        parentHi = (HumanBodyBones)HumanTrait.GetParentBone((int)hi);
                    }
                    maxLevel = Math.Max(maxLevel, level);
                }
            }

            if (maxLevel > 1)
            {
                return 1f / maxLevel;
            }
            else
            {
                return 1f;
            }
        }

        public int BonesIndexOf(GameObject go)
        {
            if (BoneDictionary != null && go != null)
            {
                if (BoneDictionary.TryGetValue(go, out int boneIndex))
                {
                    return boneIndex;
                }
            }
            return -1;
        }
        public HumanBodyBones HumanoidBonesIndexOf(GameObject go)
        {
            if (go == null || !IsHuman) return (HumanBodyBones)(-1);
            if (HumanoidBones != null)
            {
                var boneIndex = BonesIndexOf(go);
                if (boneIndex >= 0)
                {
                    return BoneIndex2humanoidIndex[boneIndex];
                }
            }
            return (HumanBodyBones)(-1);
        }
        #endregion

        #region Bounds
        public Bounds GetSelectionBounds(float sizeAdjustment = 0f)
        {
            bool done = false;
            var bounds = new Bounds(Skeleton.GameObject.transform.position, GameObjectBounds.size * sizeAdjustment);
            void SetBounds(Vector3 pos)
            {
                if (!done)
                {
                    bounds.center = pos;
                    done = true;
                }
                else
                {
                    bounds.Encapsulate(pos);
                }
            }
            #region Bone
            if (SelectionBones != null)
            {
                foreach (var boneIndex in SelectionBones)
                {
                    if (IsHuman && boneIndex == 0) continue;
                    SetBounds(Skeleton.Bones[boneIndex].transform.position);
                }
            }
            #endregion
            if (IsHuman)
            {
                #region Root
                if (SelectionGameObjectsIndexOf(VAW.GameObject) >= 0)
                {
                    SetBounds(HumanWorldRootPositionCache);
                }
                #endregion
                #region VirtualBone
                if (SelectionHumanVirtualBones != null)
                {
                    foreach (var virtualBone in SelectionHumanVirtualBones)
                    {
                        SetBounds(GetHumanVirtualBonePosition(virtualBone));
                    }
                }
                #endregion
                #region AnimatorIK
                if (animatorIK.IKActiveTarget != AnimatorIKCore.IKTarget.None)
                {
                    foreach (var ikTarget in animatorIK.ikTargetSelect)
                    {
                        SetBounds(animatorIK.ikData[(int)ikTarget].WorldPosition);
                    }
                }
                #endregion
            }
            #region OriginalIK
            if (originalIK.IKActiveTarget >= 0)
            {
                foreach (var ikTarget in originalIK.ikTargetSelect)
                {
                    SetBounds(originalIK.ikData[ikTarget].WorldPosition);
                }
            }
            #endregion
            return bounds;
        }
        public Quaternion GetSelectionBoundsRotation()
        {
            Quaternion rotation = Quaternion.identity;
            var posList = new List<Vector3>();
            #region Bone
            if (SelectionBones != null)
            {
                foreach (var boneIndex in SelectionBones)
                {
                    if (IsHuman && boneIndex == 0) continue;
                    posList.Add(Skeleton.Bones[boneIndex].transform.position);
                    rotation = Skeleton.Bones[boneIndex].transform.rotation;
                }
            }
            #endregion
            if (IsHuman)
            {
                #region Root
                if (SelectionGameObjectsIndexOf(VAW.GameObject) >= 0)
                {
                    posList.Add(HumanWorldRootPositionCache);
                    rotation = HumanWorldRootRotationCache;
                }
                #endregion
                #region VirtualBone
                if (SelectionHumanVirtualBones != null)
                {
                    foreach (var virtualBone in SelectionHumanVirtualBones)
                    {
                        posList.Add(GetHumanVirtualBonePosition(virtualBone));
                        rotation = GetHumanVirtualBoneRotation(virtualBone);
                    }
                }
                #endregion
                #region AnimatorIK
                if (animatorIK.IKActiveTarget != AnimatorIKCore.IKTarget.None)
                {
                    foreach (var ikTarget in animatorIK.ikTargetSelect)
                    {
                        posList.Add(animatorIK.ikData[(int)ikTarget].WorldPosition);
                        rotation = animatorIK.ikData[(int)ikTarget].WorldRotation;
                    }
                }
                #endregion
            }
            #region OriginalIK
            if (originalIK.IKActiveTarget >= 0)
            {
                foreach (var ikTarget in originalIK.ikTargetSelect)
                {
                    posList.Add(originalIK.ikData[ikTarget].WorldPosition);
                    rotation = originalIK.ikData[ikTarget].WorldRotation;
                }
            }
            #endregion

            if (posList.Count > 0)
            {
                var center = GetSelectionBounds().center;
                Vector3 normal = Vector3.zero;
                {
                    for (int i = 0; i < posList.Count; i++)
                    {
                        for (int j = 0; j < posList.Count; j++)
                        {
                            if (i == j) continue;
                            var cross = Vector3.Cross(posList[i] - center, posList[j] - center);
                            var dot = Vector3.Dot(normal, cross);
                            if (dot < 0f)
                                cross = -cross;
                            normal += cross;
                        }
                    }
                    var vecActive = posList[0] - center;
                    vecActive.Normalize();
                    if (vecActive.sqrMagnitude > 0f)
                    {
                        normal.Normalize();
                        if (normal.sqrMagnitude > 0f)
                        {
                            rotation = Quaternion.LookRotation(normal);
                        }
                        else
                        {
                            rotation = Quaternion.LookRotation(vecActive);
                            normal = rotation * Vector3.up;
                        }
                        var angle = Vector3.SignedAngle(rotation * Vector3.right, vecActive, normal);
                        var rightRotation = Quaternion.AngleAxis(angle, normal);
                        rotation = rightRotation * rotation;
                    }
                }
            }
            return rotation;
        }
        #endregion

        #region BoneFlags (ShowBone, WriteLock)
        public bool[] boneShowFlags;
        public bool[] boneWriteLockFlags;
        public List<int> SkeletonFKShowBoneList { get; private set; }
        public bool[] SkeletonFKShowBoneFlag { get; private set; }
        public List<Vector2Int> SkeletonIKShowBoneList { get; private set; }
        public int BoneShowCount { get; private set; }

        private void InitializeBoneFlags()
        {
            boneShowFlags = new bool[Bones.Length];
            boneWriteLockFlags = new bool[Bones.Length];
            if (IsHuman)
            {
                ActionBoneShowFlagsHumanoidBody((index) =>
                {
                    boneShowFlags[index] = true;
                });
            }
            else
            {
                bool done = false;
                ActionBoneShowFlagsHaveWeight((index) =>
                {
                    boneShowFlags[index] = true;
                    done = true;
                });
                if (!done)
                {
                    ActionBoneShowFlagsHaveRendererParent((index) =>
                    {
                        boneShowFlags[index] = true;
                        done = true;
                    });
                }
                if (!done)
                {
                    ActionBoneShowFlagsHaveRenderer((index) =>
                    {
                        boneShowFlags[index] = true;
                        done = true;
                    });
                }
                if (!done)
                {
                    ActionBoneShowFlagsAll((index) =>
                    {
                        boneShowFlags[index] = true;
                        done = true;
                    });
                }
                {
                    if (RootMotionBoneIndex >= 0)
                    {
                        boneShowFlags[RootMotionBoneIndex] = true;
                        boneShowFlags[0] = false;
                    }
                    else if (boneShowFlags.Length > 0)
                    {
                        boneShowFlags[0] = true;
                    }
                }
            }
            {
                var animators = VAW.GameObject.GetComponentsInChildren<Animator>(true);
                foreach (var animator in animators)
                {
                    if (animator == null || animator == VAW.Animator)
                        continue;
                    void HideFlag(int bi)
                    {
                        if (bi < 0) return;
                        boneShowFlags[bi] = false;
                        for (int i = 0; i < Bones[bi].transform.childCount; i++)
                        {
                            HideFlag(BonesIndexOf(Bones[bi].transform.GetChild(i).gameObject));
                        }
                    }

                    HideFlag(BonesIndexOf(animator.gameObject));
                }
            }
            OnBoneShowFlagsUpdated.Invoke();
        }
        private List<GameObject> GetReloadBoneShowFlags()
        {
            var reload = new List<GameObject>();
            for (int i = 0; i < boneShowFlags.Length; i++)
            {
                if (!boneShowFlags[i] || Bones[i] == null)
                    continue;
                reload.Add(Bones[i]);
            }
            return reload;
        }
        private void SetReloadBoneShowFlags(List<GameObject> reload)
        {
            boneShowFlags = new bool[Bones.Length];
            foreach (var go in reload)
            {
                int boneIndex = BonesIndexOf(go);
                if (boneIndex < 0)
                    continue;
                boneShowFlags[boneIndex] = true;
            }
            OnBoneShowFlagsUpdated.Invoke();
        }
        private List<GameObject> GetReloadBoneWriteLockFlags()
        {
            var reload = new List<GameObject>();
            for (int i = 0; i < boneWriteLockFlags.Length; i++)
            {
                if (!boneWriteLockFlags[i] || Bones[i] == null)
                    continue;
                reload.Add(Bones[i]);
            }
            return reload;
        }
        private void SetReloadBoneWriteLockFlags(List<GameObject> reload)
        {
            boneWriteLockFlags = new bool[Bones.Length];
            foreach (var go in reload)
            {
                int boneIndex = BonesIndexOf(go);
                if (boneIndex < 0)
                    continue;
                boneWriteLockFlags[boneIndex] = true;
            }
        }


        public void ActionBoneShowFlagsAll(Action<int> action)
        {
            if (boneShowFlags == null) return;
            for (int i = 0; i < boneShowFlags.Length; i++)
                action(i);
        }
        public void ActionBoneShowFlagsHumanoidBody(Action<int> action)
        {
            action(0);    //Root
            for (int i = (int)HumanBodyBones.LeftUpperLeg; i <= (int)HumanBodyBones.RightToes; i++)
            {
                var boneIndex = HumanoidIndex2boneIndex[i];
                if (boneIndex < 0) continue;
                action(boneIndex);
            }
            {
                var boneIndex = HumanoidIndex2boneIndex[(int)HumanBodyBones.UpperChest];
                if (boneIndex >= 0)
                    action(boneIndex);
            }
        }
        public void ActionBoneShowFlagsHumanoidFace(Action<int> action)
        {
            for (int i = (int)HumanBodyBones.LeftEye; i <= (int)HumanBodyBones.Jaw; i++)
            {
                var boneIndex = HumanoidIndex2boneIndex[i];
                if (boneIndex < 0) continue;
                action(boneIndex);
            }
        }
        public void ActionBoneShowFlagsHumanoidLeftHand(Action<int> action)
        {
            for (int i = (int)HumanBodyBones.LeftThumbProximal; i <= (int)HumanBodyBones.LeftLittleDistal; i++)
            {
                var boneIndex = HumanoidIndex2boneIndex[i];
                if (boneIndex < 0) continue;
                action(boneIndex);
            }
        }
        public void ActionBoneShowFlagsHumanoidRightHand(Action<int> action)
        {
            for (int i = (int)HumanBodyBones.RightThumbProximal; i <= (int)HumanBodyBones.RightLittleDistal; i++)
            {
                var boneIndex = HumanoidIndex2boneIndex[i];
                if (boneIndex < 0) continue;
                action(boneIndex);
            }
        }
        public void ActionBoneShowFlagsHaveWeight(Action<int> action)
        {
            if (Renderers == null) return;
            foreach (var renderer in Renderers)
            {
                if (renderer == null) continue;
                if (renderer is SkinnedMeshRenderer)
                {
                    var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                    var mesh = skinnedMeshRenderer.sharedMesh;
                    if (mesh != null)
                    {
                        var meshBones = skinnedMeshRenderer.bones;
                        var list = new Dictionary<int, int>();
                        void SetBoneIndex(int index)
                        {
                            if (index < 0 || index >= meshBones.Length)
                                return;
                            if (list.ContainsKey(index))
                                return;
                            if (meshBones[index] != null)
                                list.Add(index, BonesIndexOf(meshBones[index].gameObject));
                            else
                                list.Add(index, -1);
                        }
                        foreach (var boneWeight in mesh.boneWeights)
                        {
                            if (boneWeight.weight0 > 0f)
                                SetBoneIndex(boneWeight.boneIndex0);
                            if (boneWeight.weight1 > 0f)
                                SetBoneIndex(boneWeight.boneIndex1);
                            if (boneWeight.weight2 > 0f)
                                SetBoneIndex(boneWeight.boneIndex2);
                            if (boneWeight.weight3 > 0f)
                                SetBoneIndex(boneWeight.boneIndex3);
                        }
                        foreach (var pair in list)
                        {
                            if (pair.Value >= 0)
                                action(pair.Value);
                        }
                    }
                }
            }
        }
        public void ActionBoneShowFlagsHaveRenderer(Action<int> action)
        {
            if (Renderers == null) return;
            foreach (var renderer in Renderers)
            {
                if (renderer == null) continue;
                var boneIndex = BonesIndexOf(renderer.transform.gameObject);
                if (boneIndex >= 0)
                    action(boneIndex);
            }
        }
        public void ActionBoneShowFlagsHaveRendererParent(Action<int> action)
        {
            if (Renderers == null) return;
            foreach (var renderer in Renderers)
            {
                if (renderer == null) continue;
                var parent = renderer.transform.parent;
                if (parent == null) continue;
                var boneIndex = BonesIndexOf(parent.gameObject);
                if (boneIndex >= 0)
                    action(boneIndex);
            }
        }

        public bool IsShowBone(int boneIndex)
        {
            if (boneIndex < 0 || boneIndex >= Bones.Length || Bones[boneIndex] == null || !boneShowFlags[boneIndex])
                return false;
            if (IsHuman)
            {
                if (animatorIK.IsIKBone(BoneIndex2humanoidIndex[boneIndex]) != AnimatorIKCore.IKTarget.None)
                    return false;
            }
            if (originalIK.IsIKBone(boneIndex) >= 0)
                return false;
            return true;
        }
        public bool IsShowVirtualBone(HumanBodyBones humanoidIndex)
        {
            if (!IsHuman)
                return false;
            if (HumanoidBones[(int)humanoidIndex] != null || HumanVirtualBones[(int)humanoidIndex] == null)
                return false;
            {
                var ikIndex = animatorIK.IsIKBone(humanoidIndex);
                if (ikIndex >= 0 && ikIndex < AnimatorIKCore.IKTarget.Total)
                {
                    if (animatorIK.ikData[(int)ikIndex].enable)
                        return false;
                }
            }
            {
                var phi = GetHumanVirtualBoneParentBone(humanoidIndex);
                if (phi < 0 || HumanoidIndex2boneIndex[(int)phi] < 0) return false;
                if (!IsShowBone(HumanoidIndex2boneIndex[(int)phi])) return false;
            }
            return true;
        }
        public Action OnHierarchyUpdated;
        public Action OnBoneShowFlagsUpdated;
        public void UpdateSkeletonShowBoneList()
        {
            if (IsEditError) return;

            SkeletonFKShowBoneFlag = new bool[Bones.Length];

            void SetParentFlags(int boneIndex, bool flag)
            {
                if (ParentBoneIndexes[boneIndex] < 0 || ParentBoneIndexes[ParentBoneIndexes[boneIndex]] < 0) return;
                SkeletonFKShowBoneFlag[ParentBoneIndexes[boneIndex]] = flag;
                SetParentFlags(ParentBoneIndexes[boneIndex], flag);
            }

            for (int i = 0; i < Bones.Length; i++)
            {
                SkeletonFKShowBoneFlag[i] = boneShowFlags[i] && ParentBoneIndexes[i] >= 0;
                if (SkeletonFKShowBoneFlag[i])
                    SetParentFlags(i, true);
            }
            {
                if (SkeletonFKShowBoneList == null)
                    SkeletonFKShowBoneList = new List<int>(SkeletonFKShowBoneFlag.Length);
                else
                    SkeletonFKShowBoneList.Clear();
                for (int i = 0; i < SkeletonFKShowBoneFlag.Length; i++)
                {
                    if (SkeletonFKShowBoneFlag[i])
                        SkeletonFKShowBoneList.Add(i);
                }
            }
            {
                if (SkeletonIKShowBoneList == null)
                    SkeletonIKShowBoneList = new List<Vector2Int>(SkeletonFKShowBoneFlag.Length);
                else
                    SkeletonIKShowBoneList.Clear();
                {
                    void AddBone(int boneIndex)
                    {
                        if (!SkeletonFKShowBoneFlag[boneIndex])
                            return;
                        SkeletonIKShowBoneList.Add(new Vector2Int(boneIndex, -1));
                    }
                    if (IsEnableUpdateHumanoidFootIK())
                    {
                        AddBone(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftLowerLeg]);
                        AddBone(HumanoidIndex2boneIndex[(int)HumanBodyBones.LeftFoot]);
                        AddBone(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightLowerLeg]);
                        AddBone(HumanoidIndex2boneIndex[(int)HumanBodyBones.RightFoot]);
                    }
                }
            }
            BoneShowCount = boneShowFlags.Count(n => n);
        }
        public bool IsWriteLockBone(HumanBodyBones humanoidIndex)
        {
            var boneIndex = HumanoidIndex2boneIndex[(int)humanoidIndex];
            if (boneIndex < 0)
            {
                if (HumanVirtualBones[(int)humanoidIndex] != null)
                {
                    for (int i = 0; i < HumanVirtualBones[(int)humanoidIndex].Length; i++)
                    {
                        if (IsWriteLockBone(HumanVirtualBones[(int)humanoidIndex][i].boneA) ||
                            IsWriteLockBone(HumanVirtualBones[(int)humanoidIndex][i].boneB))
                            return true;
                    }
                }
                return false;
            }
            else
            {
                return IsWriteLockBone(boneIndex);
            }
        }
        public bool IsWriteLockBone(int boneIndex)
        {
            if (boneIndex < 0)
                return false;
            else
                return boneWriteLockFlags[boneIndex];
        }
        #endregion

        #region UnityTool
        public Tool LastTool { get; set; }

        public void EnableCustomTools(Tool t)
        {
            if (Tools.current != Tool.None)
            {
                LastTool = Tools.current;
                Tools.current = t;
            }
        }
        public void DisableCustomTools()
        {
            if (LastTool != Tool.None)
            {
                Tools.current = LastTool;
                LastTool = Tool.None;
            }
        }
        public Tool CurrentTool()
        {
            Tool tool = LastTool;
            var humanoidIndex = SelectionGameObjectHumanoidIndex();
            if (animatorIK.IKActiveTarget != AnimatorIKCore.IKTarget.None)
            {
                tool = Tool.Rotate;
            }
            else if (originalIK.IKActiveTarget >= 0)
            {
                tool = Tool.Rotate;
            }
            else if (IsHuman && SelectionActiveBone >= 0 && SelectionActiveBone == RootMotionBoneIndex)
            {
                if (LastTool == Tool.Move) tool = Tool.Move;
                else tool = Tool.Rotate;
            }
            else if (humanoidIndex >= 0)
            {
                switch (LastTool)
                {
                    case Tool.Move:
                        if (!HumanoidHasTDoF || HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] == null ||
                            Skeleton.HumanoidBones[(int)humanoidIndex] == null || Skeleton.HumanoidBones[(int)HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].parent] == null)
                        {
                            tool = Tool.Rotate;
                        }
                        break;
                    default:
                        tool = Tool.Rotate;
                        break;
                }
            }
            else if (SelectionMotionTool)
            {
                if (LastTool == Tool.Move) tool = Tool.Move;
                else tool = Tool.Rotate;
            }
            else
            {
                switch (LastTool)
                {
                    case Tool.Move:
                    case Tool.Scale:
                        break;
                    default:
                        tool = Tool.Rotate;
                        break;
                }
            }
            return tool;
        }
        #endregion

        #region AnimationGUI
        #region GUI
        private bool guiAnimationLoopFoldout;
        private bool guiAnimationWarningFoldout = true;
        private bool guiClipSelectorEditFoldout = false;

        public void AnimationToolbarGUI()
        {
            if (UAw.GetLinkedWithTimeline())
            {
                EditorGUILayout.LabelField("Timeline Animation Playable Asset", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(VAW.Animator == null || VAW.Animator.runtimeAnimatorController == null);
                EditorGUI.BeginChangeCheck();
                var mode = (AnimationMode)GUILayout.Toolbar((int)animationMode, AnimationModeString, EditorStyles.miniButton);
                if (EditorGUI.EndChangeCheck())
                {
                    UAw.StopPreviewing();

                    animationMode = mode;
                    if (animationMode == AnimationMode.Layers)
                        SetAnimationLayerIndex(0);
                    else
                        SetAnimationLayerIndex(-1);
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        public void AnimationGUI()
        {
            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                #region Animatable
                if (VAW.Animator != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Animator", GUILayout.Width(116));
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(VAW.Animator, typeof(Animator), false, GUILayout.Width(180));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();

                    if (animationMode == AnimationMode.Layers)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Animator Controller", GUILayout.Width(116));
                        GUILayout.FlexibleSpace();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(VAW.Animator.runtimeAnimatorController, typeof(Animator), false, GUILayout.Width(180));
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();

                        {
                            EditorGUI.BeginChangeCheck();
                            var index = GUILayout.Toolbar(animationLayerIndex, AnimationLayersString);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UAw.StopPreviewing();

                                SetAnimationLayerIndex(Mathf.Clamp(index, 0, AnimationLayersString.Length - 1));
                            }
                        }
                    }
                }
                else if (VAW.Animation != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Animation", GUILayout.Width(116));
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(VAW.Animation, typeof(Animation), false, GUILayout.Width(180));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion

                #region Animation Clip
                {
                    if (UAw.GetLinkedWithTimeline())
                    {
#if VERYANIMATION_TIMELINE
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.LabelField("Animation Clip", GUILayout.Width(116));
                        GUILayout.FlexibleSpace();
                        var isReadOnly = CurrentClip != null && (CurrentClip.hideFlags & HideFlags.NotEditable) != HideFlags.None;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(CurrentClip, typeof(AnimationClip), false, GUILayout.Width(isReadOnly ? 98 : 180));
                        EditorGUI.EndDisabledGroup();
                        if (isReadOnly)
                            EditorGUILayout.LabelField("(Read-Only)", GUILayout.Width(78));
                        EditorGUILayout.EndHorizontal();
#else
                        Assert.IsTrue(false);
#endif
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        guiClipSelectorEditFoldout = EditorGUILayout.Foldout(guiClipSelectorEditFoldout, "Animation Clip", true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetAnimationLayerIndex(animationLayerIndex);
                        }
                        GUILayout.FlexibleSpace();
                        var isReadOnly = CurrentClip != null && (CurrentClip.hideFlags & HideFlags.NotEditable) != HideFlags.None;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(CurrentClip, typeof(AnimationClip), false, GUILayout.Width(isReadOnly ? 98 : 180));
                        EditorGUI.EndDisabledGroup();
                        if (isReadOnly)
                            EditorGUILayout.LabelField("(Read-Only)", GUILayout.Width(78));
                        EditorGUILayout.EndHorizontal();
                        if (guiClipSelectorEditFoldout)
                        {
                            #region ClipSelector
                            {
                                VAW.ClipSelectorTreeView.UpdateSelectedIds();
                                {
                                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                                    EditorGUI.indentLevel++;
                                    VAW.ClipSelectorTreeView.searchString = VAW.ClipSelectorTreeSearchField.OnToolbarGUI(VAW.ClipSelectorTreeView.searchString);
                                    EditorGUI.indentLevel--;
                                    EditorGUILayout.EndHorizontal();
                                }
                                {
                                    var rect = EditorGUILayout.GetControlRect(false, VAW.position.height * 0.4f);
                                    VAW.ClipSelectorTreeView.OnGUI(rect);
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                if (CurrentClip != null)
                {
                    AnimationClipSettings animationClipSettings = AnimationUtility.GetAnimationClipSettings(CurrentClip);
                    bool hasMotionCurves = UAnimationUtility.HasMotionCurves(CurrentClip);
                    bool hasRootCurves = UAnimationUtility.HasRootCurves(CurrentClip);
                    var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                    EditorGUI.indentLevel++;
                    if ((CurrentClip.hideFlags & HideFlags.NotEditable) != 0)
                    {
                        EditorGUILayout.BeginHorizontal(VAW.GuiStyleSkinBox);
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationclipisReadOnly), MessageType.Warning);
                        {
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.Space();
                            if (GUILayout.Button("Duplicate and Replace"))
                            {
                                DuplicateAndReplace();
                            }
                            EditorGUILayout.Space();
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        if (VAW.Animator != null && VAW.Animator.isHuman && CurrentClip.isHumanMotion)
                            EditorGUILayout.LabelField("Humanoid Motion");
                        else if (!CurrentClip.legacy)
                            EditorGUILayout.LabelField("Generic Motion");
                        else
                            EditorGUILayout.LabelField("Legacy Motion");
                        #region Loop
                        if (CurrentClip.isLooping && !CurrentClip.legacy)
                        {
                            guiAnimationLoopFoldout = EditorGUILayout.Foldout(guiAnimationLoopFoldout, "Loop", true);
                            EditorGUI.indentLevel++;
                            if (guiAnimationLoopFoldout)
                            {
                                if (VAW.Animator != null && !VAW.Animator.isInitialized)
                                    VAW.Animator.Rebind();
                                var info = VAW.UMuscleClipQualityInfo.GetMuscleClipQualityInfo(CurrentClip, 0f, CurrentClip.length);
                                var hasRootCurve = UAnimationUtility.HasRootCurves(CurrentClip) || UAnimationUtility.HasMotionCurves(CurrentClip);
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    if (animationClipSettings.loopBlend)
                                        EditorGUILayout.LabelField(new GUIContent("Loop Pose", "Loop Blend"), GUILayout.Width(160f));
                                    else
                                        EditorGUILayout.LabelField("Loop", GUILayout.Width(160f));
                                    GUILayout.FlexibleSpace();
                                    if (hasRootCurve && CurrentClip.isHumanMotion)
                                    {
                                        EditorGUILayout.LabelField("loop match", VAW.GuiStyleMiddleRightMiniLabel, GUILayout.Width(90f));
                                        var rect = EditorGUILayout.GetControlRect(false, 16f, GUILayout.Width(16f));
                                        if (animationClipSettings.loopBlend)
                                        {
                                            if (info.loop < 0.33f)
                                                GUI.DrawTexture(rect, VAW.RedLightTex);
                                            else if (info.loop < 0.66f)
                                                GUI.DrawTexture(rect, VAW.OrangeLightTex);
                                            else
                                                GUI.DrawTexture(rect, VAW.GreenLightTex);
                                        }
                                        else
                                        {
                                            if (info.loop < 0.66f)
                                                GUI.DrawTexture(rect, VAW.RedLightTex);
                                            else if (info.loop < 0.99f)
                                                GUI.DrawTexture(rect, VAW.OrangeLightTex);
                                            else
                                                GUI.DrawTexture(rect, VAW.GreenLightTex);
                                        }
                                        GUI.DrawTexture(rect, VAW.LightRimTex);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                if (hasRootCurve)
                                {
                                    void LoopMatchGUI(string name, float value, bool bake)
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.LabelField(name, GUILayout.Width(160f));
                                        EditorGUILayout.Space();
                                        if (bake)
                                        {
                                            EditorGUILayout.LabelField("loop match", VAW.GuiStyleMiddleRightMiniLabel, GUILayout.Width(90f));
                                            var rect = EditorGUILayout.GetControlRect(false, 16f, GUILayout.Width(16f));
                                            if (animationClipSettings.loopBlend)
                                            {
                                                if (value < 0.33f)
                                                    GUI.DrawTexture(rect, VAW.RedLightTex);
                                                else if (value < 0.66f)
                                                    GUI.DrawTexture(rect, VAW.OrangeLightTex);
                                                else
                                                    GUI.DrawTexture(rect, VAW.GreenLightTex);
                                            }
                                            else
                                            {
                                                if (value < 0.66f)
                                                    GUI.DrawTexture(rect, VAW.RedLightTex);
                                                else if (value < 0.99f)
                                                    GUI.DrawTexture(rect, VAW.OrangeLightTex);
                                                else
                                                    GUI.DrawTexture(rect, VAW.GreenLightTex);
                                            }
                                            GUI.DrawTexture(rect, VAW.LightRimTex);
                                        }
                                        else
                                        {
                                            EditorGUILayout.LabelField("root motion", VAW.GuiStyleMiddleRightMiniLabel, GUILayout.Width(90f));
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    LoopMatchGUI("Loop Orientation", info.loopOrientation, animationClipSettings.loopBlendOrientation);
                                    LoopMatchGUI("Loop Position (Y)", info.loopPositionY, animationClipSettings.loopBlendPositionY);
                                    LoopMatchGUI("Loop Position (XZ)", info.loopPositionXZ, animationClipSettings.loopBlendPositionXZ);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                        #endregion
                        #region Warning
                        {
                            int count = 0;
                            {
                                if (animationClipSettings.loopTime && animationClipSettings.loopBlend) count++;
                                if (!animationClipSettings.keepOriginalPositionY && animationClipSettings.heightFromFeet && !IsHaveAnimationCurveAnimatorIkT(VeryAnimation.AnimatorIKIndex.LeftFoot) && !IsHaveAnimationCurveAnimatorIkT(VeryAnimation.AnimatorIKIndex.RightFoot)) count++;
                                if (hasRootCurves && !hasMotionCurves)
                                {
                                    if (!animationClipSettings.keepOriginalOrientation || !animationClipSettings.keepOriginalPositionXZ || !animationClipSettings.keepOriginalPositionY ||
                                        animationClipSettings.level != 0f || animationClipSettings.orientationOffsetY != 0f)
                                        count++;
                                }
                                if (UAw.GetLinkedWithTimeline())
                                {
#if VERYANIMATION_TIMELINE
                                    if (UAw.GetTimelineAnimationPlayableAssetHasRootTransforms() && UAw.GetRemoveStartOffset())
                                        count++;
#else
                                    Assert.IsTrue(false);
#endif
                                }
                            }
                            if (count > 0)
                            {
                                guiAnimationWarningFoldout = EditorGUILayout.Foldout(guiAnimationWarningFoldout, "Warning", true);
                                EditorGUI.indentLevel++;
                                if (guiAnimationWarningFoldout)
                                {
                                    if (animationClipSettings.loopTime && animationClipSettings.loopBlend)
                                    {
                                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationClipSettingsLoopPoseisenabled), MessageType.Warning);
                                    }
                                    if (!animationClipSettings.keepOriginalPositionY && animationClipSettings.heightFromFeet && !IsHaveAnimationCurveAnimatorIkT(VeryAnimation.AnimatorIKIndex.LeftFoot) && !IsHaveAnimationCurveAnimatorIkT(VeryAnimation.AnimatorIKIndex.RightFoot))
                                    {
                                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationClipSettingsRootTransformPositionYisFeet), MessageType.Warning);
                                    }
                                    if (hasRootCurves && !hasMotionCurves)
                                    {
                                        if (!animationClipSettings.keepOriginalOrientation || !animationClipSettings.keepOriginalPositionXZ || !animationClipSettings.keepOriginalPositionY ||
                                            animationClipSettings.level != 0f || animationClipSettings.orientationOffsetY != 0f)
                                        {
                                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationClipSettingsBasedUponisnotOriginal), MessageType.Warning);
                                        }
                                    }
                                    if (UAw.GetLinkedWithTimeline())
                                    {
#if VERYANIMATION_TIMELINE
                                        if (UAw.GetTimelineAnimationPlayableAssetHasRootTransforms() && UAw.GetRemoveStartOffset())
                                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationTrackSettingRemoveStartOffsetsEnable), MessageType.Warning);
#else
                                        Assert.IsTrue(false);
#endif
                                    }
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                        #endregion
                        #region Error
                        {
                            int count = 0;
                            {
                                if (animationClipSettings.cycleOffset != 0f) count++;
                                if (animationClipSettings.mirror) count++;
                                if (!AnimatorApplyRootMotion && hasRootCurves && !hasMotionCurves &&
                                    (!animationClipSettings.loopBlendOrientation || !animationClipSettings.loopBlendPositionXZ || !animationClipSettings.loopBlendPositionY)) count++;

                                if (!UAw.GetLinkedWithTimeline())
                                {
                                    if (ac != null)
                                    {
                                        if (animationMode == AnimationMode.Layers)
                                        {
                                            if (ac.layers[0].blendingMode != UnityEditor.Animations.AnimatorLayerBlendingMode.Override)
                                                count++;
                                        }
                                    }
                                }
                            }
                            if (count > 0)
                            {
                                EditorGUILayout.LabelField("Error");
                                EditorGUI.indentLevel++;
                                {
                                    if (animationClipSettings.cycleOffset != 0f)
                                    {
                                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationClipSettingsCycleOffsetisnot0), MessageType.Error);
                                    }
                                    if (animationClipSettings.mirror)
                                    {
                                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationClipSettingsMirrorisenabled), MessageType.Error);
                                    }
                                    if (!AnimatorApplyRootMotion && hasRootCurves && !hasMotionCurves &&
                                        (!animationClipSettings.loopBlendOrientation || !animationClipSettings.loopBlendPositionXZ || !animationClipSettings.loopBlendPositionY))
                                    {
                                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimationClipSettingsBakeIntoPoseDisableRootMotion), MessageType.Error);
                                    }
                                    if (!UAw.GetLinkedWithTimeline())
                                    {
                                        if (ac != null)
                                        {
                                            if (animationMode == AnimationMode.Layers)
                                            {
                                                if (ac.layers[0].blendingMode != UnityEditor.Animations.AnimatorLayerBlendingMode.Override)
                                                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimatorControllerLayer0NotOverride), MessageType.Error);
                                            }
                                        }
                                    }
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                        #endregion
                    }
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
        }

        #region AnimationMode
        public enum AnimationMode
        {
            Single,
            Layers,
            Total
        }
        public AnimationMode animationMode;

        private readonly GUIContent[] AnimationModeString = new GUIContent[(int)AnimationMode.Total];

        private void ResetAnimationMode()
        {
            animationMode = AnimationMode.Single;
            SetAnimationLayerIndex(-1);
            UpdateAnimationModeString();
        }
        private void UpdateAnimationModeString()
        {
            for (int i = 0; i < (int)AnimationMode.Total; i++)
            {
                AnimationModeString[i] = new GUIContent(Language.GetContent(Language.Help.AnimationModeSingle + i));
            }
        }
        #endregion

        #region Layers
        private int animationLayerIndex;

        private GUIContent[] AnimationLayersString;

        public void CheckChangeLayersSettings()
        {
            if (animationMode == AnimationMode.Layers && AnimationLayersString != null)
            {
                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac != null)
                {
                    var layers = ac.layers;
                    if (layers.Length != AnimationLayersString.Length)
                    {
                        SetAnimationLayerIndex(Mathf.Clamp(animationLayerIndex, 0, layers.Length - 1));
                    }
                }
            }
        }

        private void UpdateAnimationLayersInfo()
        {
            var ac = EditorCommon.GetAnimatorController(VAW.Animator);
            if (ac != null)
            {
                var layers = ac.layers;

                CurrentLayerClips ??= new Dictionary<AnimatorStateMachine, AnimationClip>();

                if (animationMode == AnimationMode.Layers)
                {
                    foreach (var item in CurrentLayerClips.Where(x => x.Key == null).ToList())
                        CurrentLayerClips.Remove(item.Key);

                    for (int i = 0; i < layers.Length; i++)
                    {
                        var layerClips = GetLayerAnimationClips(i);
                        CurrentLayerClips.TryGetValue(layers[i].stateMachine, out AnimationClip clip);
                        if (!layerClips.Contains(clip))
                        {
                            if (layerClips.Length > 0)
                                clip = layerClips[0];
                            if (!CurrentLayerClips.ContainsKey(layers[i].stateMachine))
                                CurrentLayerClips.Add(layers[i].stateMachine, clip);
                            else
                                CurrentLayerClips[layers[i].stateMachine] = clip;
                        }
                    }
                }

                AnimationLayersString = new GUIContent[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    CurrentLayerClips.TryGetValue(layers[i].stateMachine, out AnimationClip clip);
                    AnimationLayersString[i] = new GUIContent(layers[i].name, string.Format("Clip\t\t{0}\nWeight\t{1}\nMask\t\t{2}\nBlending\t{3}",
                                                                                                clip != null ? clip.name : "",
                                                                                                i == 0 ? 1f : layers[i].defaultWeight,
                                                                                                layers[i].avatarMask != null ? layers[i].avatarMask.name : "",
                                                                                                layers[i].blendingMode));
                }
            }
        }

        private void SetAnimationLayerIndex(int layerIndex)
        {
            UpdateAnimationLayersInfo();

            animationLayerIndex = layerIndex;

            if (animationMode == AnimationMode.Layers)
            {
                var layerClips = GetLayerAnimationClips(layerIndex);
                if (layerClips.Length == 0)
                {
                    animationLayerIndex = 0;
                    EditorCommon.ShowNotification("No clip");
                    Debug.LogErrorFormat(Language.GetText(Language.Help.LogAnimationLayerNoClip), AnimationLayersString[layerIndex].text);
                }

                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac != null)
                {
                    var layers = ac.layers;
                    if (CurrentLayerClips != null)
                    {
                        CurrentLayerClips.TryGetValue(layers[animationLayerIndex].stateMachine, out AnimationClip clip);
                        SetCurrentClip(clip);
                    }
                }
            }

            VAW.SetClipSelectorLayerIndex(animationMode == AnimationMode.Layers ? animationLayerIndex : -1);

            #region Preview
            if (UAvatarPreview != null)
            {
                UAvatarPreview.Dispose();
                UAvatarPreview = null;
            }
            #endregion

            SetUpdateSampleAnimation(false, true);
        }
        #endregion
        #endregion
        #endregion

        #region PreviewGUI
        public void PreviewGUI()
        {
            if (UAvatarPreview != null)
            {
                {
                    EditorGUILayout.BeginHorizontal("preToolbar", GUILayout.Height(17f));
                    GUILayout.FlexibleSpace();
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (CurrentClip != null)
                        GUI.Label(lastRect, CurrentClip.name, "preToolbar2");
                    UAvatarPreview.OnPreviewSettings();
                    EditorGUILayout.EndHorizontal();
                }
                if (UAvatarPreview.Playing)
                {
                    VAW.Repaint();
                }
                else
                {
                    if (Event.current.type == EventType.Repaint)
                        UAvatarPreview.ForceUpdate();
                }

                {
                    var rect = EditorGUILayout.GetControlRect(false, 0);
                    rect.height = Math.Max(VAW.position.height - rect.y, 0);
                    UAvatarPreview.OnGUI(rect, "preBackground");
                }
            }
        }
        #endregion

        #region SynchronizeAnimation
        public void SetSynchronizeAnimation(bool enable)
        {
            if (EditorApplication.isPlaying)
                return;
            if (UAw.GetLinkedWithTimeline())
                return;

            if (enable && synchronizeAnimation == null)
            {
                synchronizeAnimation = new SynchronizeAnimation();
                synchronizeAnimation.SetTime(CurrentTime);
            }
            else if (!enable && synchronizeAnimation != null)
            {
                synchronizeAnimation.Dispose();
                synchronizeAnimation = null;
            }
        }
        #endregion

        #region SaveSettings
        public void LoadSaveSettings()
        {
            if (VAW.GameObject == null)
                return;
            if (!VAW.GameObject.TryGetComponent<VeryAnimationSaveSettings>(out var saveSettings))
                return;

            #region bones
            if (saveSettings.bonePaths != null && saveSettings.bonePaths.Length > 0)
            {
                #region WriteLock
                if (saveSettings.writeLockBones != null && saveSettings.writeLockBones.Length > 0)
                {
                    for (int i = 0; i < boneWriteLockFlags.Length; i++)
                        boneWriteLockFlags[i] = false;
                    foreach (var index in saveSettings.writeLockBones)
                    {
                        if (index < 0 || index >= saveSettings.bonePaths.Length) continue;
                        var boneIndex = GetBoneIndexFromPath(saveSettings.bonePaths[index]);
                        if (boneIndex < 0) continue;
                        boneWriteLockFlags[boneIndex] = true;
                    }
                }
                #endregion
                #region Show
                if (saveSettings.showBones != null && saveSettings.showBones.Length > 0)
                {
                    for (int i = 0; i < boneShowFlags.Length; i++)
                        boneShowFlags[i] = false;
                    foreach (var index in saveSettings.showBones)
                    {
                        if (index < 0 || index >= saveSettings.bonePaths.Length) continue;
                        var boneIndex = GetBoneIndexFromPath(saveSettings.bonePaths[index]);
                        if (boneIndex < 0) continue;
                        boneShowFlags[boneIndex] = true;
                    }
                }
                #endregion
                #region Foldout
                if (saveSettings.foldoutBones != null && saveSettings.foldoutBones.Length > 0)
                {
                    if (VeryAnimationControlWindow.instance != null)
                    {
                        VeryAnimationControlWindow.instance.CollapseAll();
                        foreach (var index in saveSettings.foldoutBones)
                        {
                            if (index < 0 || index >= saveSettings.bonePaths.Length) continue;
                            var boneIndex = GetBoneIndexFromPath(saveSettings.bonePaths[index]);
                            if (boneIndex < 0) continue;
                            VeryAnimationControlWindow.instance.SetExpand(Bones[boneIndex], true);
                        }
                    }
                }
                #endregion
                #region MirrorBone
                if (saveSettings.mirrorBones != null && saveSettings.mirrorBones.Length > 0)
                {
                    BonesMirrorInitialize();
                    for (int i = 0; i < saveSettings.mirrorBones.Length; i++)
                    {
                        var bi = saveSettings.mirrorBones[i];
                        if (bi < 0 || bi >= saveSettings.bonePaths.Length) continue;
                        var boneIndex = GetBoneIndexFromPath(saveSettings.bonePaths[i]);
                        var mboneIndex = GetBoneIndexFromPath(saveSettings.bonePaths[bi]);
                        if (boneIndex < 0 || mboneIndex < 0 || boneIndex == mboneIndex) continue;
                        MirrorBoneIndexes[boneIndex] = mboneIndex;
                    }
                    UpdateBonesMirrorOther();
                }
                #endregion
            }
            #endregion
            #region MirrorBlendShape
            if (saveSettings.mirrorBlendShape != null && saveSettings.mirrorBlendShape.Length > 0)
            {
                BlendShapeMirrorInitialize();
                var renderers = VAW.GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var data in saveSettings.mirrorBlendShape)
                {
                    if (renderers.Contains(data.renderer))
                    {
                        for (int i = 0; i < data.names.Length && i < data.mirrorNames.Length; i++)
                        {
                            if (i < 0 || i >= data.mirrorNames.Length) continue;
                            ChangeBlendShapeMirror(data.renderer, data.names[i], data.mirrorNames[i]);
                        }
                    }
                }
            }
            #endregion

            animatorIK.LoadIKSaveSettings(saveSettings.animatorIkData);
            originalIK.LoadIKSaveSettings(saveSettings.originalIkData);

            #region SelectionSet
            selectionSetList = LoadSelectionSaveSettings(saveSettings.selectionData).ToList();
            #endregion

            #region Animation
            if (!EditorApplication.isPlaying && !UAw.GetLinkedWithTimeline())
            {
                if (saveSettings.lastSelectAnimationClip != null)
                {
                    if (VAW.IsContainsSelectionAnimationClip(saveSettings.lastSelectAnimationClip))
                        UAw.SetSelectionAnimationClip(saveSettings.lastSelectAnimationClip);
                }
            }
            #endregion

            #region HandPoseSet
            handPoseSetList.Clear();
            if (saveSettings.handPoseList != null && saveSettings.handPoseList.Length > 0)
            {
                foreach (var set in saveSettings.handPoseList)
                {
                    var poseTemplate = ScriptableObject.CreateInstance<PoseTemplate>();
                    poseTemplate.name = set.name;
                    {
                        poseTemplate.musclePropertyNames = new string[set.musclePropertyNames.Length];
                        set.musclePropertyNames.CopyTo(poseTemplate.musclePropertyNames, 0);
                        poseTemplate.muscleValues = new float[set.muscleValues.Length];
                        set.muscleValues.CopyTo(poseTemplate.muscleValues, 0);
                    }
                    poseTemplate.isHuman = true;
                    string[] rightMusclePropertyNames = new string[poseTemplate.musclePropertyNames.Length];
                    for (int i = 0; i < poseTemplate.musclePropertyNames.Length; i++)
                    {
                        rightMusclePropertyNames[i] = poseTemplate.musclePropertyNames[i].Replace("Left", "Right");
                    }
                    handPoseSetList.Add(new HandPoseSet()
                    {
                        poseTemplate = poseTemplate,
                        leftMusclePropertyNames = poseTemplate.musclePropertyNames,
                        rightMusclePropertyNames = rightMusclePropertyNames,
                    });
                }
            }
            #endregion

            #region BlendShapeSet
            blendShapeSetList.Clear();
            if (saveSettings.blendShapeList != null && saveSettings.blendShapeList.Length > 0)
            {
                foreach (var set in saveSettings.blendShapeList)
                {
                    var poseTemplate = ScriptableObject.CreateInstance<PoseTemplate>();
                    {
                        poseTemplate.name = set.name;
                        poseTemplate.blendShapePaths = set.blendShapePaths.ToArray();
                        poseTemplate.blendShapeValues = new PoseTemplate.BlendShapeData[set.blendShapeValues.Length];
                        for (int i = 0; i < set.blendShapeValues.Length; i++)
                        {
                            poseTemplate.blendShapeValues[i] = new PoseTemplate.BlendShapeData()
                            {
                                names = set.blendShapeValues[i].names.ToArray(),
                                weights = set.blendShapeValues[i].weights.ToArray(),
                            };
                        }
                    }
                    blendShapeSetList.Add(new BlendShapeSet()
                    {
                        poseTemplate = poseTemplate,
                    });
                }
            }
            #endregion
        }
        public void SaveSaveSettings()
        {
            if (VAW.GameObject == null)
                return;
            if (!VAW.GameObject.TryGetComponent<VeryAnimationSaveSettings>(out var saveSettings))
                return;

            #region bones
            {
                saveSettings.bonePaths = new string[BonePaths.Length];
                BonePaths.CopyTo(saveSettings.bonePaths, 0);
                #region WriteLock
                if (boneWriteLockFlags != null && Bones != null && boneWriteLockFlags.Length == Bones.Length)
                {
                    var list = new List<int>();
                    for (int i = 0; i < boneWriteLockFlags.Length; i++)
                    {
                        if (Bones[i] == null || !boneWriteLockFlags[i]) continue;
                        list.Add(i);
                    }
                    saveSettings.writeLockBones = list.ToArray();
                }
                #endregion
                #region Show
                if (boneShowFlags != null && Bones != null && boneShowFlags.Length == Bones.Length)
                {
                    var list = new List<int>();
                    for (int i = 0; i < boneShowFlags.Length; i++)
                    {
                        if (Bones[i] == null || !boneShowFlags[i]) continue;
                        list.Add(i);
                    }
                    saveSettings.showBones = list.ToArray();
                }
                #endregion
                #region Foldout
                if (VeryAnimationControlWindow.instance != null)
                {
                    var list = new List<int>();
                    VeryAnimationControlWindow.instance.ActionAllExpand((go) =>
                    {
                        var boneIndex = BonesIndexOf(go);
                        if (boneIndex >= 0)
                            list.Add(boneIndex);
                    });
                    saveSettings.foldoutBones = list.ToArray();
                }
                #endregion
                #region MirrorBone
                if (MirrorBoneIndexes != null && Bones != null && MirrorBoneIndexes.Length == Bones.Length)
                {
                    var list = new int[MirrorBoneIndexes.Length];
                    for (int i = 0; i < MirrorBoneIndexes.Length; i++)
                    {
                        list[i] = -1;
                        if (i != MirrorBoneIndexes[i])
                            list[i] = MirrorBoneIndexes[i];
                    }
                    saveSettings.mirrorBones = list;
                }
                #endregion
            }
            #endregion
            #region MirrorBlendShape
            if (MirrorBlendShape != null)
            {
                var list = new List<VeryAnimationSaveSettings.MirrorBlendShape>(MirrorBlendShape.Count);
                foreach (var pair in MirrorBlendShape)
                {
                    var data = new VeryAnimationSaveSettings.MirrorBlendShape()
                    {
                        renderer = pair.Key,
                        names = pair.Value.Keys.ToArray(),
                        mirrorNames = pair.Value.Values.ToArray(),
                    };
                    list.Add(data);
                }
                saveSettings.mirrorBlendShape = list.ToArray();
            }
            #endregion

            saveSettings.animatorIkData = animatorIK.SaveIKSaveSettings();
            saveSettings.originalIkData = originalIK.SaveIKSaveSettings();

            #region SelectionSet
            saveSettings.selectionData = SaveSelectionSaveSettings();
            #endregion

            #region Animation
            saveSettings.lastSelectAnimationClip = UAw.GetSelectionAnimationClip();
            #endregion

            #region HandPoseSet
            saveSettings.handPoseList = new VeryAnimationSaveSettings.HandPoseSet[handPoseSetList != null ? handPoseSetList.Count : 0];
            if (handPoseSetList != null)
            {
                for (int i = 0; i < handPoseSetList.Count; i++)
                {
                    if (handPoseSetList[i].poseTemplate == null)
                        continue;
                    handPoseSetList[i].SetLeft();
                    var srcPoseTemplate = handPoseSetList[i].poseTemplate;
                    var set = new VeryAnimationSaveSettings.HandPoseSet()
                    {
                        name = srcPoseTemplate.name,
                    };
                    {
                        var muscleDic = new Dictionary<string, float>();
                        if (srcPoseTemplate.musclePropertyNames != null && MusclePropertyName.PropertyNames != null)
                        {
                            var beginMuscle = HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftThumbProximal, 2);
                            var endMuscle = HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftLittleDistal, 2);
                            for (int muscle = beginMuscle; muscle <= endMuscle; muscle++)
                            {
                                var index = ArrayUtility.IndexOf(srcPoseTemplate.musclePropertyNames, MusclePropertyName.PropertyNames[muscle]);
                                if (index < 0) continue;
                                muscleDic.Add(srcPoseTemplate.musclePropertyNames[index], srcPoseTemplate.muscleValues[index]);
                            }
                        }
                        set.musclePropertyNames = muscleDic.Keys.ToArray();
                        set.muscleValues = muscleDic.Values.ToArray();
                    }
                    saveSettings.handPoseList[i] = set;
                }
            }
            #endregion

            #region BlendShapeSet
            saveSettings.blendShapeList = new VeryAnimationSaveSettings.BlendShapeSet[blendShapeSetList != null ? blendShapeSetList.Count : 0];
            if (blendShapeSetList != null)
            {
                for (int i = 0; i < blendShapeSetList.Count; i++)
                {
                    if (blendShapeSetList[i].poseTemplate == null)
                        continue;
                    var set = new VeryAnimationSaveSettings.BlendShapeSet
                    {
                        name = blendShapeSetList[i].poseTemplate.name,
                        blendShapePaths = blendShapeSetList[i].poseTemplate.blendShapePaths.ToArray(),
                        blendShapeValues = new VeryAnimationSaveSettings.BlendShapeSet.BlendShapeData[blendShapeSetList[i].poseTemplate.blendShapeValues.Length]
                    };
                    for (int j = 0; j < blendShapeSetList[i].poseTemplate.blendShapeValues.Length; j++)
                    {
                        set.blendShapeValues[j] = new VeryAnimationSaveSettings.BlendShapeSet.BlendShapeData()
                        {
                            names = blendShapeSetList[i].poseTemplate.blendShapeValues[j].names.ToArray(),
                            weights = blendShapeSetList[i].poseTemplate.blendShapeValues[j].weights.ToArray(),
                        };
                    }
                    saveSettings.blendShapeList[i] = set;
                }
            }
            #endregion
        }

        public VeryAnimationSaveSettings.SelectionData[] LoadSelectionSaveSettings(VeryAnimationSaveSettings.SelectionData[] selectionData)
        {
            var list = new List<VeryAnimationSaveSettings.SelectionData>();
            if (selectionData != null)
            {
                foreach (var data in selectionData)
                {
                    var newData = new VeryAnimationSaveSettings.SelectionData()
                    {
                        name = data.name,
                    };
                    {
                        var bones = new List<GameObject>();
                        if (data.bones != null)
                        {
                            foreach (var bone in data.bones)
                            {
                                if (bone == null) continue;
                                bones.Add(bone);
                            }
                            if (data.bonePaths != null && data.bones.Length == data.bonePaths.Length)
                            {
                                bones.Clear();
                                foreach (var bonePath in data.bonePaths)
                                {
                                    var bone = VAW.GameObject.transform.Find(bonePath);
                                    if (bone == null) continue;
                                    bones.Add(bone.gameObject);
                                }
                            }
                        }
                        newData.bones = bones.ToArray();
                    }
                    if (IsHuman)
                    {
                        var vbones = new List<HumanBodyBones>();
                        if (data.virtualBones != null)
                        {
                            foreach (var vbone in data.virtualBones)
                            {
                                if (vbone < 0 || vbone >= HumanBodyBones.LastBone || HumanoidBones[(int)vbone] != null) continue;
                                vbones.Add(vbone);
                            }
                        }
                        newData.virtualBones = vbones.ToArray();
                    }
                    list.Add(newData);
                }
            }
            return list.ToArray();
        }
        public VeryAnimationSaveSettings.SelectionData[] SaveSelectionSaveSettings()
        {
            if (selectionSetList == null)
                return new VeryAnimationSaveSettings.SelectionData[0];
            var array = selectionSetList.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                array[i].bonePaths = new string[array[i].bones.Length];
                for (int j = 0; j < array[i].bones.Length; j++)
                {
                    if (array[i].bones[j] == null)
                        continue;
                    array[i].bonePaths[j] = AnimationUtility.CalculateTransformPath(array[i].bones[j].transform, VAW.GameObject.transform);
                }
            }
            return array;
        }

        #endregion

        #region Etc
        public void ActionAllBoneChildren(int boneIndex, Action<int> action)
        {
            var t = Bones[boneIndex].transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var childIndex = BonesIndexOf(t.GetChild(i).gameObject);
                if (childIndex < 0) continue;
                action.Invoke(childIndex);
                ActionAllBoneChildren(childIndex, action);
            }
        }
        public void ActionAllVirtualBoneChildren(int boneIndex, Action<HumanBodyBones> action)
        {
            if (!IsHuman) return;
            bool Check(HumanBodyBones hi)
            {
                void Invoke(HumanBodyBones hhi)
                {
                    if (HumanoidBones[(int)hhi] == null)
                        action.Invoke(hhi);
                }
                switch (hi)
                {
                    case HumanBodyBones.Hips:
                    case HumanBodyBones.Spine:
                        Invoke(HumanBodyBones.Chest);
                        Invoke(HumanBodyBones.Neck);
                        Invoke(HumanBodyBones.LeftShoulder);
                        Invoke(HumanBodyBones.RightShoulder);
                        Invoke(HumanBodyBones.UpperChest);
                        return true;
                    case HumanBodyBones.Chest:
                        Invoke(HumanBodyBones.Neck);
                        Invoke(HumanBodyBones.LeftShoulder);
                        Invoke(HumanBodyBones.RightShoulder);
                        Invoke(HumanBodyBones.UpperChest);
                        return true;
                    case HumanBodyBones.UpperChest:
                        Invoke(HumanBodyBones.Neck);
                        Invoke(HumanBodyBones.LeftShoulder);
                        Invoke(HumanBodyBones.RightShoulder);
                        return true;
                }
                return false;
            }

            if (Check(BoneIndex2humanoidIndex[boneIndex]))
                return;
            var t = Bones[boneIndex].transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var childIndex = BonesIndexOf(t.GetChild(i).gameObject);
                if (childIndex < 0) continue;
                if (Check(BoneIndex2humanoidIndex[childIndex]))
                    return;
                ActionAllVirtualBoneChildren(childIndex, action);
            }
        }

        public Type GetBoneType(int boneIndex)
        {
            if (IsHuman && (VAW.GameObject == Bones[boneIndex] || BoneIndex2humanoidIndex[boneIndex] >= 0))
            {
                return typeof(Animator);
            }
            else if (RootMotionBoneIndex >= 0 && RootMotionBoneIndex == boneIndex)
            {
                return typeof(Animator);
            }
            else
            {
                if (Bones[boneIndex].TryGetComponent<Renderer>(out var renderer))
                    return renderer.GetType();
                else
                    return typeof(Transform);
            }
        }

        private void RendererForceUpdate()
        {
            if (Renderers == null) return;
            //It is necessary to avoid situations where only display is not updated.
            foreach (var renderer in Renderers)
            {
                if (renderer == null) continue;
                renderer.enabled = !renderer.enabled;
                renderer.enabled = !renderer.enabled;
            }
        }
        #endregion

        #region Undo
        private void UndoRedoPerformed()
        {
            if (IsEditError) return;

            UpdateSkeletonShowBoneList();
            ToolsParameterRelatedCurveReset();

            SetUpdateSampleAnimation(true);
            SetSynchroIKtargetAll();
            SetAnimationWindowSynchroSelection();

            VAW.VA?.SetSynchronizeAnimation(VAW.VA.extraOptionsSynchronizeAnimation);
            VAW.VA?.OnionSkin?.Update();
        }
        #endregion
    }
}
