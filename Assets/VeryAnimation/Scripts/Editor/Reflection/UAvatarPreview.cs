using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace VeryAnimation
{
    internal class UAvatarPreview : IDisposable
    {
        public object Instance { get; private set; }

        public Action onAvatarChange;

        private readonly FieldInfo fi_m_PreviewDir;
        private readonly FieldInfo fi_m_ZoomFactor;
        private readonly Action<object, int> dg_set_fps;
        private readonly Func<object, AnimationClip> dg_get_m_SourcePreviewMotion;
        private readonly Func<bool> dg_get_IKOnFeet;
        private readonly Func<Animator> dg_get_Animator;
        private readonly Func<bool> dg_get_ShowIKOnFeetButton;
        private readonly Action<bool> dg_set_ShowIKOnFeetButton;
        private readonly Func<GameObject> dg_get_PreviewObject;
        private readonly Func<ModelImporterAnimationType> dg_get_animationClipType;
        private readonly Action dg_DoPreviewSettings;
        private readonly Action<Rect, GUIStyle> dg_DoAvatarPreview;
        private readonly Action dg_OnDestroy;
        private readonly Action<GameObject> dg_SetPreview;
        private readonly PropertyInfo pi_OnAvatarChangeFunc;

        private readonly UTimeControl uTimeControl;

        private PlayableGraph m_PlayableGraph;
        private AnimationLayerMixerPlayable m_AnimationLayerMixerPlayable;
        private AnimationClipPlayable[] m_AnimationClipPlayables;
        private Playable m_AnimationMotionXToDeltaPlayable;
        private Playable m_AnimationOffsetPlayable;
        private UAnimationOffsetPlayable m_UAnimationOffsetPlayable;
        private UAnimationMotionXToDeltaPlayable m_UAnimationMotionXToDeltaPlayable;
        private UAnimationClipPlayable m_UAnimationClipPlayable;
        private AvatarMask blankAvatarMask;
        private int loopCount;
#if VERYANIMATION_ANIMATIONRIGGING
        private VeryAnimationRigBuilder m_VARigBuilder;
        private RigBuilder m_RigBuilder;
#endif

        private GameObject gameObject;
        private GameObject originalGameObject;
        private Animator animator;
        private Animation animation;
        private Animator originalAnimator;

        private AnimationClipValueSave animationClipValueSave;
        private TransformPoseSave transformPoseSave;

        private readonly Dictionary<AnimatorStateMachine, AnimationClip> layerClips;

        private readonly GUIStyle guiStylePreButton = "toolbarbutton";

        private class UAvatarPreviewSelection
        {
            private readonly Func<ModelImporterAnimationType, GameObject> dg_get_GetPreview;

            public UAvatarPreviewSelection(Assembly asmUnityEditor)
            {
                var avatarPreviewSelectionType = asmUnityEditor.GetType("UnityEditor.AvatarPreviewSelection");
                Assert.IsNotNull(dg_get_GetPreview = (Func<ModelImporterAnimationType, GameObject>)Delegate.CreateDelegate(typeof(Func<ModelImporterAnimationType, GameObject>), null, avatarPreviewSelectionType.GetMethod("GetPreview", BindingFlags.Public | BindingFlags.Static)));
            }

            public GameObject GetPreview(ModelImporterAnimationType type)
            {
                return dg_get_GetPreview(type);
            }
        }
        private readonly UAvatarPreviewSelection uAvatarPreviewSelection;

        public const string EditorPrefs2D = "AvatarpreviewCustom2D";
        public const string EditorPrefsApplyRootMotion = "AvatarpreviewCustomApplyRootMotion";
        public const string EditorPrefsARConstraint = "AvatarpreviewCustomARConstraint";
        public const string EditorPrefsAnimatorControllerLayers = "AvatarpreviewCustomAnimatorControllerLayers";

        public UAvatarPreview(AnimationClip clip, GameObject gameObject, Dictionary<AnimatorStateMachine, AnimationClip> layerClips = null)
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var avatarPreviewType = asmUnityEditor.GetType("UnityEditor.AvatarPreview");
            Assert.IsNotNull(Instance = Activator.CreateInstance(avatarPreviewType, new object[] { null, clip }));
            Assert.IsNotNull(fi_m_PreviewDir = avatarPreviewType.GetField("m_PreviewDir", BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNotNull(fi_m_ZoomFactor = avatarPreviewType.GetField("m_ZoomFactor", BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNotNull(dg_set_fps = EditorCommon.CreateSetFieldDelegate<int>(avatarPreviewType.GetField("fps")));
            Assert.IsNotNull(dg_get_m_SourcePreviewMotion = EditorCommon.CreateGetFieldDelegate<AnimationClip>(avatarPreviewType.GetField("m_SourcePreviewMotion", BindingFlags.NonPublic | BindingFlags.Instance)));
            Assert.IsNotNull(dg_get_IKOnFeet = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), Instance, avatarPreviewType.GetProperty("IKOnFeet").GetGetMethod()));
            Assert.IsNotNull(dg_get_Animator = (Func<Animator>)Delegate.CreateDelegate(typeof(Func<Animator>), Instance, avatarPreviewType.GetProperty("Animator").GetGetMethod()));
            Assert.IsNotNull(dg_get_ShowIKOnFeetButton = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), Instance, avatarPreviewType.GetProperty("ShowIKOnFeetButton").GetGetMethod()));
            Assert.IsNotNull(dg_set_ShowIKOnFeetButton = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), Instance, avatarPreviewType.GetProperty("ShowIKOnFeetButton").GetSetMethod()));
            Assert.IsNotNull(dg_get_PreviewObject = (Func<GameObject>)Delegate.CreateDelegate(typeof(Func<GameObject>), Instance, avatarPreviewType.GetProperty("PreviewObject").GetGetMethod()));
            Assert.IsNotNull(dg_get_animationClipType = (Func<ModelImporterAnimationType>)Delegate.CreateDelegate(typeof(Func<ModelImporterAnimationType>), Instance, avatarPreviewType.GetProperty("animationClipType").GetGetMethod()));
            Assert.IsNotNull(dg_DoPreviewSettings = (Action)Delegate.CreateDelegate(typeof(Action), Instance, avatarPreviewType.GetMethod("DoPreviewSettings")));
            Assert.IsNotNull(dg_DoAvatarPreview = (Action<Rect, GUIStyle>)Delegate.CreateDelegate(typeof(Action<Rect, GUIStyle>), Instance, avatarPreviewType.GetMethod("DoAvatarPreview")));
            Assert.IsNotNull(dg_OnDestroy = (Action)Delegate.CreateDelegate(typeof(Action), Instance, avatarPreviewType.GetMethod("OnDisable")));
            Assert.IsNotNull(dg_SetPreview = (Action<GameObject>)Delegate.CreateDelegate(typeof(Action<GameObject>), Instance, avatarPreviewType.GetMethod("SetPreview", BindingFlags.NonPublic | BindingFlags.Instance)));
            Assert.IsNotNull(pi_OnAvatarChangeFunc = avatarPreviewType.GetProperty("OnAvatarChangeFunc"));

            this.layerClips = layerClips;

            {
                var fi_timeControl = avatarPreviewType.GetField("timeControl");
                uTimeControl = new UTimeControl(fi_timeControl.GetValue(Instance))
                {
                    StartTime = 0f,
                    StopTime = GetStopTime(),
                    CurrentTime = 0f
                };
            }
            uAvatarPreviewSelection = new UAvatarPreviewSelection(asmUnityEditor);

            pi_OnAvatarChangeFunc.SetValue(Instance, Delegate.CreateDelegate(pi_OnAvatarChangeFunc.PropertyType, this, GetType().GetMethod("OnAvatarChangeFunc", BindingFlags.NonPublic | BindingFlags.Instance)), null);
            dg_set_fps(Instance, (int)clip.frameRate);
            {
                var logEnabled = Debug.unityLogger.logEnabled;
                Debug.unityLogger.logEnabled = false;
                try
                {
                    dg_SetPreview(gameObject);
                }
                finally
                {
                    Debug.unityLogger.logEnabled = logEnabled;
                }
            }
            SetTime(uTimeControl.CurrentTime);

            AnimationUtility.onCurveWasModified += OnCurveWasModified;
        }
        ~UAvatarPreview()
        {
            AnimationUtility.onCurveWasModified -= OnCurveWasModified;
            Assert.IsFalse(m_PlayableGraph.IsValid());
        }

        public void Dispose()
        {
            DestroyController();

            AnimationUtility.onCurveWasModified -= OnCurveWasModified;
            pi_OnAvatarChangeFunc.SetValue(Instance, null, null);
            {
                var logEnabled = Debug.unityLogger.logEnabled;
                Debug.unityLogger.logEnabled = false;
                try
                {
                    dg_SetPreview(null);
                }
                finally
                {
                    Debug.unityLogger.logEnabled = logEnabled;
                }
            }
            dg_OnDestroy();
        }

        private void OnAvatarChangeFunc()
        {
            onAvatarChange?.Invoke();
            Reset();
            DestroyController();
            InitController();
        }

        private void InitController()
        {
            gameObject = dg_get_PreviewObject();
            if (gameObject == null) return;
            originalGameObject = uAvatarPreviewSelection.GetPreview(dg_get_animationClipType());

            animator = dg_get_Animator();
            animation = gameObject.GetComponent<Animation>();
            if (originalGameObject != null)
            {
                originalAnimator = originalGameObject.GetComponent<Animator>();
                transformPoseSave = new TransformPoseSave(originalGameObject);
                transformPoseSave.ChangeStartTransform();
                transformPoseSave.ChangeTransformReference(gameObject);
            }
            else
            {
                originalAnimator = null;
                transformPoseSave = new TransformPoseSave(gameObject);
            }

            var clip = dg_get_m_SourcePreviewMotion(Instance);

            animationClipValueSave = new AnimationClipValueSave(gameObject, clip, layerClips?.Values.ToArray());

            if (clip.legacy || Instance == null || !((UnityEngine.Object)animator != (UnityEngine.Object)null))
            {
                if (animation != null)
                    animation.enabled = false;  //If vaw.animation.enabled, it is not updated during execution. bug?
            }
            else
            {
                animator.fireEvents = false;
                animator.applyRootMotion = EditorPrefs.GetBool(EditorPrefsApplyRootMotion, false);

                animator.enabled = true;
                UnityEditor.Animations.AnimatorController.SetAnimatorController(animator, null);

                m_PlayableGraph = PlayableGraph.Create("Avatar Preview PlayableGraph");
                m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                m_UAnimationClipPlayable ??= new UAnimationClipPlayable();
                blankAvatarMask = new AvatarMask();
                blankAvatarMask.hideFlags |= HideFlags.HideAndDontSave;

                var isLayersflag = EditorPrefs.GetBool(EditorPrefsAnimatorControllerLayers, false);
                var ac = EditorCommon.GetAnimatorController(originalAnimator);
                if (layerClips != null && ac != null && isLayersflag)
                {
                    var layers = ac.layers;
                    var stopTime = GetStopTime();
                    m_AnimationLayerMixerPlayable = AnimationLayerMixerPlayable.Create(m_PlayableGraph, layerClips.Count);
                    m_AnimationClipPlayables = new AnimationClipPlayable[layers.Length];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        layerClips.TryGetValue(layers[i].stateMachine, out AnimationClip lclip);
                        if (lclip == null)
                            continue;

                        m_AnimationClipPlayables[i] = AnimationClipPlayable.Create(m_PlayableGraph, lclip);
                        m_AnimationClipPlayables[i].SetApplyPlayableIK(false);
                        m_AnimationClipPlayables[i].SetApplyFootIK(IsIKOnFeet);

                        m_UAnimationClipPlayable.SetRemoveStartOffset(m_AnimationClipPlayables[i], true);
                        if (Mathf.Approximately(stopTime, lclip.length))
                        {
                            m_UAnimationClipPlayable.SetOverrideLoopTime(m_AnimationClipPlayables[i], true);
                            m_UAnimationClipPlayable.SetLoopTime(m_AnimationClipPlayables[i], true);
                        }

                        m_AnimationLayerMixerPlayable.ConnectInput(i, m_AnimationClipPlayables[i], 0);
                        m_AnimationLayerMixerPlayable.SetInputWeight(i, i == 0 ? 1f : layers[i].defaultWeight);
                        m_AnimationLayerMixerPlayable.SetLayerAdditive((uint)i, layers[i].blendingMode == UnityEditor.Animations.AnimatorLayerBlendingMode.Additive);
                        if (layers[i].avatarMask != null)
                            m_AnimationLayerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, layers[i].avatarMask);
                        else
                            m_AnimationLayerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, blankAvatarMask);
                    }
                }
                else
                {
                    m_AnimationLayerMixerPlayable = AnimationLayerMixerPlayable.Create(m_PlayableGraph, 1);
                    m_AnimationClipPlayables = new AnimationClipPlayable[1];
                    m_AnimationClipPlayables[0] = AnimationClipPlayable.Create(m_PlayableGraph, clip);
                    m_AnimationClipPlayables[0].SetApplyPlayableIK(false);
                    m_AnimationClipPlayables[0].SetApplyFootIK(IsIKOnFeet);
                    m_UAnimationClipPlayable.SetRemoveStartOffset(m_AnimationClipPlayables[0], true);
                    m_UAnimationClipPlayable.SetOverrideLoopTime(m_AnimationClipPlayables[0], true);
                    m_UAnimationClipPlayable.SetLoopTime(m_AnimationClipPlayables[0], true);

                    m_AnimationLayerMixerPlayable.ConnectInput(0, m_AnimationClipPlayables[0], 0);
                    m_AnimationLayerMixerPlayable.SetInputWeight(0, 1f);
                }
                Playable rootPlayable = m_AnimationLayerMixerPlayable;

#if VERYANIMATION_ANIMATIONRIGGING
                if (gameObject.TryGetComponent<VeryAnimationRigBuilder>(out m_VARigBuilder))
                {
                    RigBuilder.DestroyImmediate(m_VARigBuilder);
                    m_VARigBuilder = null;
                }
                if (gameObject.TryGetComponent<RigBuilder>(out m_RigBuilder))
                {
                    RigBuilder.DestroyImmediate(m_RigBuilder);
                    m_RigBuilder = null;
                }
                if (originalGameObject != null)
                {
                    var rigBuilder = originalGameObject.GetComponent<RigBuilder>();
                    if (rigBuilder != null && rigBuilder.isActiveAndEnabled && rigBuilder.enabled)
                    {
                        var layers = new List<GameObject>();
                        foreach (var layer in rigBuilder.layers)
                        {
                            if (layer.rig == null || !layer.active)
                                continue;
                            if (!layer.rig.TryGetComponent<Rig>(out var originalRig))
                                continue;

                            Transform GetPreviewTransform(Transform t)
                            {
                                if (t == null) return null;
                                var path = AnimationUtility.CalculateTransformPath(t, originalGameObject.transform);
                                return gameObject.transform.Find(path);
                            }
                            var previewT = GetPreviewTransform(originalRig.transform);
                            if (previewT == null)
                                continue;
                            var newRig = GameObject.Instantiate<GameObject>(originalRig.gameObject, previewT.parent);
                            newRig.name = previewT.name;
                            GameObject.DestroyImmediate(previewT.gameObject);
                            AnimationRigging.ReplaceConstraintTransformReference(gameObject, newRig.GetComponent<Rig>(), originalGameObject, originalRig);
                            layers.Add(newRig);
                        }
                        if (layers.Count > 0)
                        {
                            m_VARigBuilder = gameObject.AddComponent<VeryAnimationRigBuilder>();
                            m_RigBuilder = gameObject.GetComponent<RigBuilder>();
                            foreach (var layer in layers)
                            {
                                var rig = layer.GetComponent<Rig>();
                                #region RemoveEffector
                                {
                                    var effectors = rig.effectors as List<RigEffectorData>;
                                    effectors.Clear();
                                }
                                #endregion

                                var rigLayer = new RigLayer(rig);   //version 0.3.2
                                m_RigBuilder.layers.Add(rigLayer);
                            }
                        }
                    }
                    if (m_VARigBuilder != null && m_RigBuilder != null)
                    {
                        m_VARigBuilder.enabled = m_RigBuilder.enabled = EditorPrefs.GetBool(EditorPrefsARConstraint);
                        if (m_RigBuilder.enabled)
                        {
                            m_VARigBuilder.StartPreview();
                            m_RigBuilder.StartPreview();
                            rootPlayable = m_VARigBuilder.BuildPreviewGraph(m_PlayableGraph, rootPlayable);
                            rootPlayable = m_RigBuilder.BuildPreviewGraph(m_PlayableGraph, rootPlayable);
                        }
                    }
                }
#endif

                if (animator.applyRootMotion)
                {
                    bool hasRootMotionBone = false;
                    if (animator.isHuman)
                        hasRootMotionBone = true;
                    else
                    {
                        var uAvatar = new UAvatar();
                        var genericRootMotionBonePath = uAvatar.GetGenericRootMotionBonePath(animator.avatar);
                        hasRootMotionBone = !string.IsNullOrEmpty(genericRootMotionBonePath);
                    }
                    if (hasRootMotionBone)
                    {
                        m_UAnimationOffsetPlayable ??= new UAnimationOffsetPlayable();
                        m_AnimationOffsetPlayable = m_UAnimationOffsetPlayable.Create(m_PlayableGraph, transformPoseSave.StartLocalPosition, transformPoseSave.StartLocalRotation, 1);
                        m_AnimationOffsetPlayable.SetInputWeight(0, 1f);
                        m_PlayableGraph.Connect(rootPlayable, 0, m_AnimationOffsetPlayable, 0);
                        rootPlayable = m_AnimationOffsetPlayable;
                    }
                    {
                        m_UAnimationMotionXToDeltaPlayable ??= new UAnimationMotionXToDeltaPlayable();
                        m_AnimationMotionXToDeltaPlayable = m_UAnimationMotionXToDeltaPlayable.Create(m_PlayableGraph);
                        m_UAnimationMotionXToDeltaPlayable.SetAbsoluteMotion(m_AnimationMotionXToDeltaPlayable, true);
                        m_AnimationMotionXToDeltaPlayable.SetInputWeight(0, 1f);
                        m_PlayableGraph.Connect(rootPlayable, 0, m_AnimationMotionXToDeltaPlayable, 0);
                        rootPlayable = m_AnimationMotionXToDeltaPlayable;
                    }
                }

                var playableOutput = AnimationPlayableOutput.Create(m_PlayableGraph, "Animation", animator);
                playableOutput.SetSourcePlayable(rootPlayable);
            }

            dg_set_ShowIKOnFeetButton(animator != null && animator.isHuman && clip.isHumanMotion);

            ForceUpdate();
        }
        private void DestroyController()
        {
#if VERYANIMATION_ANIMATIONRIGGING
            if (m_RigBuilder != null)
            {
                m_RigBuilder.StopPreview();
                m_RigBuilder = null;
            }
            if (m_VARigBuilder != null)
            {
                m_VARigBuilder.StopPreview();
                m_VARigBuilder = null;
            }
#endif
            if (blankAvatarMask != null)
            {
                AvatarMask.DestroyImmediate(blankAvatarMask);
                blankAvatarMask = null;
            }
            if (m_PlayableGraph.IsValid())
                m_PlayableGraph.Destroy();
            if (animationClipValueSave != null)
            {
                animationClipValueSave.Dispose();
                animationClipValueSave = null;
            }
        }

        private void OnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType deleted)
        {
            if (Instance == null) return;
            if (clip != dg_get_m_SourcePreviewMotion(Instance)) return;

            Reset();
        }

        public void OnPreviewSettings()
        {
            var clip = dg_get_m_SourcePreviewMotion(Instance);

            if (!clip.legacy && animator != null)
            {
                var flag = EditorPrefs.GetBool(EditorPrefsApplyRootMotion, false);
                EditorGUI.BeginChangeCheck();
                flag = GUILayout.Toggle(flag, new GUIContent("Root Motion", "Apply Root Motion"), guiStylePreButton);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(EditorPrefsApplyRootMotion, flag);
                    Reset();
                    OnAvatarChangeFunc();
                }
            }
#if VERYANIMATION_ANIMATIONRIGGING
            if (m_RigBuilder != null)
            {
                var flag = EditorPrefs.GetBool(EditorPrefsARConstraint, false);
                EditorGUI.BeginChangeCheck();
                flag = GUILayout.Toggle(flag, new GUIContent("Animation Rigging", "Animation Rigging Constraint"), guiStylePreButton);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(EditorPrefsARConstraint, flag);
                    Reset();
                    OnAvatarChangeFunc();
                }
            }
#endif
            if (layerClips != null && originalAnimator != null)
            {
                var ac = EditorCommon.GetAnimatorController(originalAnimator);
                if (ac != null)
                {
                    var flag = EditorPrefs.GetBool(EditorPrefsAnimatorControllerLayers, false);
                    EditorGUI.BeginChangeCheck();
                    flag = GUILayout.Toggle(flag, new GUIContent("Layers", "Animator Controller Layers"), guiStylePreButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool(EditorPrefsAnimatorControllerLayers, flag);
                        Reset();
                        OnAvatarChangeFunc();
                    }
                }
            }
            GUILayout.Space(20);
            dg_DoPreviewSettings();
        }

        public void OnGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                #region TimeControl
                {
                    var beforePlaying = uTimeControl.IsPlaying;
                    var beforeCurrentTime = uTimeControl.CurrentTime;
                    uTimeControl.Update();
                    if (uTimeControl.IsPlaying && uTimeControl.CurrentTime < beforeCurrentTime)
                    {
                        loopCount++;
                    }
                    if (beforePlaying != uTimeControl.IsPlaying)
                    {
                        uTimeControl.IsPlaying = beforePlaying;
                    }
                }
                #endregion

                {
                    var clip = dg_get_m_SourcePreviewMotion(Instance);
                    uTimeControl.IsLoop = true;
                    uTimeControl.StartTime = 0f;
                    uTimeControl.StopTime = GetStopTime();
                    dg_set_fps(Instance, (int)clip.frameRate);
                    if (!clip.legacy && animator != null)
                    {
                        dg_set_ShowIKOnFeetButton(animator.isHuman && clip.isHumanMotion);

                        if (m_PlayableGraph.IsValid())
                        {
                            if (m_AnimationOffsetPlayable.IsValid())
                            {
                                m_UAnimationOffsetPlayable.SetPosition(m_AnimationOffsetPlayable, transformPoseSave.StartPosition);
                                m_UAnimationOffsetPlayable.SetRotation(m_AnimationOffsetPlayable, transformPoseSave.StartRotation);
                            }

                            var isLayersflag = EditorPrefs.GetBool(EditorPrefsAnimatorControllerLayers, false);
                            if (layerClips != null && originalAnimator != null && isLayersflag)
                            {
                                var ac = EditorCommon.GetAnimatorController(originalAnimator);
                                if (ac != null)
                                {
                                    var layers = ac.layers;
                                    var count = Math.Min(m_AnimationLayerMixerPlayable.GetInputCount(), layers.Length);
                                    for (int i = 0; i < count; i++)
                                    {
                                        m_AnimationLayerMixerPlayable.SetInputWeight(i, i == 0 ? 1f : layers[i].defaultWeight);
                                        m_AnimationLayerMixerPlayable.SetLayerAdditive((uint)i, layers[i].blendingMode == UnityEditor.Animations.AnimatorLayerBlendingMode.Additive);
                                        if (layers[i].avatarMask != null)
                                            m_AnimationLayerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, layers[i].avatarMask);
                                        else
                                            m_AnimationLayerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, blankAvatarMask);
                                    }
                                }
                            }
                            for (int i = 0; i < m_AnimationClipPlayables.Length; i++)
                            {
                                if (!m_AnimationClipPlayables[i].IsValid())
                                    continue;
                                m_AnimationClipPlayables[i].SetApplyFootIK(IsIKOnFeet);
                                var time = uTimeControl.CurrentTime;
                                if (Mathf.Approximately(uTimeControl.StopTime, m_AnimationClipPlayables[i].GetAnimationClip().length))
                                    time += loopCount * uTimeControl.StopTime;
                                m_AnimationClipPlayables[i].SetTime(time);
                            }
#if VERYANIMATION_ANIMATIONRIGGING
                            if (m_RigBuilder != null && m_RigBuilder.enabled && m_RigBuilder.layers.Count > 0)
                            {
                                m_RigBuilder.UpdatePreviewGraph(m_PlayableGraph);
                            }
#endif
                            m_PlayableGraph.Evaluate();
                        }
                    }
                    else if (animation != null)
                    {
                        dg_set_ShowIKOnFeetButton(false);
                        clip.SampleAnimation(gameObject, uTimeControl.CurrentTime);
                    }
                }
            }

            dg_DoAvatarPreview(r, background);

            if (animator != null && animator.applyRootMotion && transformPoseSave != null)
            {
                var rect = r;
                rect.yMin = rect.yMax - 40f;
                rect.yMax -= 15f;
                var invRot = Quaternion.Inverse(transformPoseSave.OriginalRotation);
                var pos = invRot * (gameObject.transform.position - transformPoseSave.OriginalPosition);
                var rot = (invRot * gameObject.transform.rotation).eulerAngles;
                EditorGUI.DropShadowLabel(rect, string.Format("Root Motion Position {0}\nRoot Motion Rotation {1}", pos, rot));
            }
        }

        public void SetTime(float time)
        {
            uTimeControl.CurrentTime = 0f;
            uTimeControl.NextCurrentTime = time;

            Reset();
        }
        public float GetTime()
        {
            return uTimeControl.CurrentTime;
        }
        public void SetCurrentTimeOnly(float time)
        {
            uTimeControl.CurrentTime = time;
        }

        public void SetApplyRootMotion(bool flag)
        {
            if (EditorPrefs.GetBool(EditorPrefsApplyRootMotion, false) != flag)
            {
                EditorPrefs.SetBool(EditorPrefsApplyRootMotion, flag);
                Reset();
                OnAvatarChangeFunc();
            }
        }

        public void Reset()
        {
            {
                var time = uTimeControl.CurrentTime + (uTimeControl.GetDeltaTimeSet() ? uTimeControl.DeltaTime : 0f);
                uTimeControl.CurrentTime = 0f;
                uTimeControl.NextCurrentTime = time;
            }

            animationClipValueSave?.Load();
            transformPoseSave?.ResetOriginalTransform();

            loopCount = 0;
        }

        public void ForceUpdate()
        {
            var clip = dg_get_m_SourcePreviewMotion(Instance);
            if (!clip.legacy && animator != null)
            {
                if (m_PlayableGraph.IsValid())
                {
                    for (int i = 0; i < m_AnimationClipPlayables.Length; i++)
                    {
                        if (!m_AnimationClipPlayables[i].IsValid())
                            continue;
                        m_AnimationClipPlayables[i].SetTime(uTimeControl.CurrentTime);
                    }
                    m_PlayableGraph.Evaluate();
                }
            }
            else if (animation != null)
            {
                clip.SampleAnimation(gameObject, uTimeControl.CurrentTime);
            }
        }

        public Vector2 PreviewDir
        {
            get
            {
                return (Vector2)fi_m_PreviewDir.GetValue(Instance);
            }
            set
            {
                fi_m_PreviewDir.SetValue(Instance, value);
            }
        }

        public float ZoomFactor
        {
            get
            {
                return (float)fi_m_ZoomFactor.GetValue(Instance);
            }
            set
            {
                fi_m_ZoomFactor.SetValue(Instance, value);
            }
        }

        public bool Playing
        {
            get
            {
                return uTimeControl.IsPlaying;
            }
            set
            {
                uTimeControl.IsPlaying = value;
            }
        }

        private bool IsIKOnFeet { get { return dg_get_ShowIKOnFeetButton() && dg_get_IKOnFeet(); } }

        private float GetStopTime()
        {
            var clip = dg_get_m_SourcePreviewMotion(Instance);
            var stopTime = clip.length;

            var isLayersflag = EditorPrefs.GetBool(EditorPrefsAnimatorControllerLayers, false);
            if (layerClips != null && isLayersflag)
            {
                foreach (var item in layerClips)
                {
                    if (item.Value == null)
                        continue;
                    stopTime = Mathf.Max(stopTime, item.Value.length);
                }
            }
            return stopTime;
        }
    }
}
