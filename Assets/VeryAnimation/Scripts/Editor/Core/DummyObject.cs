using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace VeryAnimation
{
    internal class DummyObject : IDisposable
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        public GameObject GameObject { get; private set; }
        public Animator Animator { get; private set; }
        public Animation Animation { get; private set; }
        public GameObject[] Bones { get; private set; }
        public Dictionary<GameObject, int> BoneDictionary { get; private set; }
        public GameObject[] HumanoidBones { get; private set; }
        public Transform HumanoidHipsTransform { get; private set; }
        public HumanPoseHandler HumanPoseHandler { get; private set; }
        public VeryAnimationEditAnimator VaEdit { get; private set; }

        private GameObject sourceObject;
        private List<Material> createdMaterials;
        private MaterialPropertyBlock materialPropertyBlock;
        private UnityEditor.Animations.AnimatorController tmpAnimatorController;
        private AnimatorControllerLayer tmpAnimatorControllerLayer;
        private AnimatorState tmpAnimationState;

        private TransformPoseSave.SaveData m_SetTransformRootSave;
        private Vector3 m_OffsetPosition = Vector3.zero;
        private Quaternion m_OffsetRotation = Quaternion.identity;
        private bool m_RemoveStartOffset;
        private bool m_ApplyIK;

        private PlayableGraph m_PlayableGraph;
        private AnimationClipPlayable m_AnimationClipPlayable;
        private Playable m_AnimationMotionXToDeltaPlayable;
        private Playable m_AnimationOffsetPlayable;
        private UAnimationOffsetPlayable m_UAnimationOffsetPlayable;
        private UAnimationMotionXToDeltaPlayable m_UAnimationMotionXToDeltaPlayable;
        private UAnimationClipPlayable m_UAnimationClipPlayable;
        private bool m_AnimatesRootTransform;
        private bool m_RequiresOffsetPlayable;
        private bool m_RequiresMotionXPlayable;
        private bool m_UsesAbsoluteMotion;

        private Dictionary<Renderer, Renderer> rendererDictionary;

        private struct ResetOriginalTransformCache
        {
            public Transform transform;
            public TransformPoseSave.SaveData saveData;
        }
        private ResetOriginalTransformCache[] resetOriginalTransformCaches;
        private struct ResetOriginalBlendShapeCache
        {
            public SkinnedMeshRenderer meshRenderer;
            public int index;
            public float saveWeight;
        }
        private ResetOriginalBlendShapeCache[] resetOriginalBlendShapeCaches;

        private static readonly int ShaderID_Color = Shader.PropertyToID("_Color");
        private static readonly int ShaderID_FaceColor = Shader.PropertyToID("_FaceColor");

        ~DummyObject()
        {
            Assert.IsTrue(VaEdit == null);
            Assert.IsTrue(GameObject == null);
        }

        public void Initialize(GameObject sourceObject)
        {
            Dispose();

            this.sourceObject = sourceObject;

            GameObject = VAW.UEditorUtility.InstantiateForAnimatorPreview(sourceObject);
            GameObject.hideFlags |= HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            GameObject.name = sourceObject.name + "_Dummy";
            EditorCommon.DisableOtherBehaviors(GameObject);

            Animator = GameObject.GetComponent<Animator>();
            if (Animator != null)
            {
                if (sourceObject.GetComponent<Animator>() != null)
                {
                    Animator.enabled = true;
                    Animator.fireEvents = false;
                    Animator.updateMode = AnimatorUpdateMode.Normal;
                    Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    UnityEditor.Animations.AnimatorController.SetAnimatorController(Animator, null);
                }
                else
                {
                    Animator.DestroyImmediate(Animator);
                    Animator = null;
                }
            }
            Animation = GameObject.GetComponent<Animation>();
            if (Animation != null)
            {
                if (sourceObject.GetComponent<Animation>() != null)
                {
                    Animation.enabled = true;
                }
                else
                {
                    Animation.DestroyImmediate(Animation);
                    Animation = null;
                }
            }

            UpdateBones();

            #region rendererDictionary
            {
                rendererDictionary = new Dictionary<Renderer, Renderer>();
                var sourceRenderers = sourceObject.GetComponentsInChildren<Renderer>(true);
                var objectRenderers = GameObject.GetComponentsInChildren<Renderer>(true);
                foreach (var sr in sourceRenderers)
                {
                    if (sr == null)
                        continue;
                    var spath = AnimationUtility.CalculateTransformPath(sr.transform, sourceObject.transform);
                    var index = ArrayUtility.FindIndex(objectRenderers, (x) => AnimationUtility.CalculateTransformPath(x.transform, GameObject.transform) == spath);
                    Assert.IsTrue(index >= 0);
                    if (index >= 0 && !rendererDictionary.ContainsKey(objectRenderers[index]))
                        rendererDictionary.Add(objectRenderers[index], sr);
                }
            }
            #endregion

            #region ResetOriginalCache
            {
                var list = new List<ResetOriginalTransformCache>();
                for (int i = 0; i < VAW.VA.Bones.Length; i++)
                {
                    if (VAW.VA.Bones[i] == null)
                        continue;
                    var save = VAW.VA.TransformPoseSave.GetOriginalTransform(VAW.VA.Bones[i].transform);
                    if (save == null) continue;
                    list.Add(new ResetOriginalTransformCache()
                    {
                        transform = Bones[i].transform,
                        saveData = save,
                    });
                }
                resetOriginalTransformCaches = list.ToArray();
            }
            {
                var list = new List<ResetOriginalBlendShapeCache>();
                foreach (var pair in rendererDictionary)
                {
                    var renderer = pair.Key as SkinnedMeshRenderer;
                    if (renderer == null || renderer.sharedMesh == null)
                        continue;
                    var sourceRenderer = pair.Value as SkinnedMeshRenderer;
                    if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
                        continue;
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        var name = renderer.sharedMesh.GetBlendShapeName(i);
                        if (!VAW.VA.BlendShapeWeightSave.IsHaveOriginalWeight(sourceRenderer, name))
                            continue;
                        var weight = VAW.VA.BlendShapeWeightSave.GetOriginalWeight(sourceRenderer, name);
                        list.Add(new ResetOriginalBlendShapeCache()
                        {
                            meshRenderer = renderer,
                            index = i,
                            saveWeight = weight,
                        });
                    }
                }
                resetOriginalBlendShapeCaches = list.ToArray();
            }
            #endregion

            #region UpdateWhenOffscreen
            {
                var objectRenderers = GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in objectRenderers)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
            #endregion

            UpdateState();

            SetTransformOutside();
        }

        public void Dispose()
        {
            if (m_PlayableGraph.IsValid())
                m_PlayableGraph.Destroy();
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                UnityEditor.Animations.AnimatorController.SetAnimatorController(Animator, null);
            }
            if (tmpAnimatorController != null)
            {
                {
                    var layerCount = tmpAnimatorController.layers.Length;
                    for (int i = 0; i < layerCount; i++)
                        tmpAnimatorController.RemoveLayer(0);
                }
                UnityEditor.Animations.AnimatorController.DestroyImmediate(tmpAnimatorController);
                tmpAnimatorController = null;
            }
            tmpAnimatorControllerLayer = null;
            tmpAnimationState = null;

            RevertTransparent();

            Animator = null;
            Animation = null;
            if (VaEdit != null)
            {
                Component.DestroyImmediate(VaEdit);
                VaEdit = null;
            }
            if (GameObject != null)
            {
                GameObject.DestroyImmediate(GameObject);
                GameObject = null;
            }
            sourceObject = null;
        }

        public void SetTransformOrigin()
        {
            GameObject.transform.SetParent(null);
            ResetOriginal();

#if UNITY_2022_3_OR_NEWER
            GameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
            GameObject.transform.localPosition = Vector3.zero;
            GameObject.transform.localRotation = Quaternion.identity;
#endif
            GameObject.transform.localScale = Vector3.one;
            m_SetTransformRootSave = new TransformPoseSave.SaveData(GameObject.transform);

            SetOffset(Vector3.zero, Quaternion.identity);
        }
        public void SetTransformStart()
        {
            GameObject.transform.SetParent(sourceObject.transform.parent);
            ResetOriginal();

            GameObject.transform.SetPositionAndRotation(VAW.VA.TransformPoseSave.StartPosition, VAW.VA.TransformPoseSave.StartRotation);
            GameObject.transform.localScale = VAW.VA.TransformPoseSave.StartLocalScale;
            m_SetTransformRootSave = new TransformPoseSave.SaveData(GameObject.transform);

            SetOffset(VAW.VA.TransformPoseSave.StartLocalPosition, VAW.VA.TransformPoseSave.StartLocalRotation);
        }
        public void SetTransformOutside()
        {
            GameObject.transform.SetParent(null);
            //ResetOriginal();  Waste

            GameObject.transform.SetPositionAndRotation(new Vector3(10000f, 10000f, 10000f), Quaternion.identity);
            GameObject.transform.localScale = Vector3.one;
            m_SetTransformRootSave = new TransformPoseSave.SaveData(GameObject.transform);

            SetOffset(Vector3.zero, Quaternion.identity);
        }
        public void ResetTranformRoot()
        {
            m_SetTransformRootSave?.LoadLocal(GameObject.transform);
        }
        private void ResetOriginal()
        {
            foreach (var cache in resetOriginalTransformCaches)
            {
                cache.saveData.LoadLocal(cache.transform);
            }
            foreach (var cache in resetOriginalBlendShapeCaches)
            {
                if (cache.meshRenderer.GetBlendShapeWeight(cache.index) != cache.saveWeight)
                    cache.meshRenderer.SetBlendShapeWeight(cache.index, cache.saveWeight);
            }
        }

        public void ChangeTransparent()
        {
            RevertTransparent();

            createdMaterials = new List<Material>();
            foreach (var pair in rendererDictionary)
            {
                bool changeShader = pair.Key is SkinnedMeshRenderer;
                if (!changeShader && pair.Key is MeshRenderer)
                {
                    changeShader = true;
                    foreach (var comp in pair.Key.GetComponents<Component>())
                    {
                        if (comp.GetType().Name.StartsWith("TextMesh"))
                        {
                            changeShader = false;
                            break;
                        }
                    }
                }
                if (changeShader)
                {
                    var shader = Shader.Find("Very Animation/OnionSkin-1pass");
                    if (GraphicsSettings.currentRenderPipeline == null) //Built-in
                    {
                        shader = Shader.Find("Very Animation/OnionSkin-2pass");
                    }
                    var materials = new Material[pair.Key.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var keyMat = pair.Key.sharedMaterials[i];
                        if (keyMat == null) continue;
                        Material mat;
                        {
                            mat = new Material(shader);
                            mat.hideFlags |= HideFlags.HideAndDontSave;
                            #region SetTexture
                            {
                                void SetTexture(string name)
                                {
                                    if (mat.mainTexture == null && keyMat.HasProperty(name))
                                        mat.mainTexture = keyMat.GetTexture(name);
                                }
                                SetTexture("_MainTex");
                                SetTexture("_BaseColorMap");    //HDRP
                                SetTexture("_BaseMap");         //LWRP
                                if (mat.mainTexture == null)
                                {
                                    foreach (var name in keyMat.GetTexturePropertyNames())
                                    {
                                        SetTexture(name);
                                    }
                                }
                            }
                            #endregion
                            createdMaterials.Add(mat);
                        }
                        materials[i] = mat;
                    }
                    pair.Key.sharedMaterials = materials;
                }
                else
                {
                    var materials = new Material[pair.Key.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var keyMat = pair.Key.sharedMaterials[i];
                        if (keyMat == null) continue;
                        Material mat;
                        {
                            mat = Material.Instantiate<Material>(keyMat);
                            mat.hideFlags |= HideFlags.HideAndDontSave;
                            createdMaterials.Add(mat);
                        }
                        materials[i] = mat;
                    }
                    pair.Key.sharedMaterials = materials;
                }
            }
        }
        public void RevertTransparent()
        {
            if (createdMaterials != null)
            {
                foreach (var mat in createdMaterials)
                {
                    if (mat != null)
                        Material.DestroyImmediate(mat);
                }
                createdMaterials = null;

                foreach (var pair in rendererDictionary)
                {
                    if (pair.Key == null || pair.Value == null)
                        continue;
                    if (pair.Key.sharedMaterials != pair.Value.sharedMaterials)
                        pair.Key.sharedMaterials = pair.Value.sharedMaterials;
                }
            }
        }
        public void SetTransparentRenderQueue(int renderQueue)
        {
            if (createdMaterials == null)
                return;
            foreach (var mat in createdMaterials)
            {
                mat.renderQueue = renderQueue;
            }
        }

        public void SetColor(Color color)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetColor(ShaderID_Color, color);
            materialPropertyBlock.SetColor(ShaderID_FaceColor, color);
            foreach (var pair in rendererDictionary)
            {
                var renderer = pair.Key;
                if (renderer == null)
                    continue;
                renderer.SetPropertyBlock(materialPropertyBlock);
            }
        }
        public void ResetColor()
        {
            materialPropertyBlock = null;
            foreach (var pair in rendererDictionary)
            {
                var renderer = pair.Key;
                if (renderer == null)
                    continue;
                renderer.SetPropertyBlock(materialPropertyBlock);
            }
        }

        public void AddEditComponent()
        {
            Assert.IsNull(VaEdit);
            VaEdit = GameObject.AddComponent<VeryAnimationEditAnimator>();
            VaEdit.hideFlags |= HideFlags.HideAndDontSave;
        }
        public void RemoveEditComponent()
        {
            if (VaEdit != null)
            {
                Component.DestroyImmediate(VaEdit);
                VaEdit = null;
            }
        }

        public void RendererDestroyImmediate()
        {
            foreach (var pair in rendererDictionary)
            {
                if (pair.Key == null) continue;
                Renderer.DestroyImmediate(pair.Key);
            }
            rendererDictionary.Clear();
            resetOriginalBlendShapeCaches = new ResetOriginalBlendShapeCache[0];
        }

        public void RendererForceUpdate()
        {
            //It is necessary to avoid situations where only display is not updated.
            foreach (var pair in rendererDictionary)
            {
                if (pair.Key == null || pair.Value == null) continue;
                pair.Key.enabled = !pair.Key.enabled;
                pair.Key.enabled = !pair.Key.enabled;
            }
        }

        private void SetOffset(Vector3 position, Quaternion rotation)
        {
            if (m_OffsetPosition == position && m_OffsetRotation == rotation)
                return;
            m_OffsetPosition = position;
            m_OffsetRotation = rotation;
            if (m_AnimationOffsetPlayable.IsValid())
            {
                m_UAnimationOffsetPlayable.SetPosition(m_AnimationOffsetPlayable, m_OffsetPosition);
                m_UAnimationOffsetPlayable.SetRotation(m_AnimationOffsetPlayable, m_OffsetRotation);
            }
        }
        public void SetRemoveStartOffset(bool enable)
        {
            if (m_RemoveStartOffset == enable)
                return;
            m_RemoveStartOffset = enable;
            if (m_AnimationClipPlayable.IsValid())
            {
                m_UAnimationClipPlayable.SetRemoveStartOffset(m_AnimationClipPlayable, m_RemoveStartOffset);
            }
        }
        public void SetApplyIK(bool enable)
        {
            if (m_ApplyIK == enable)
                return;
            m_ApplyIK = enable;
            if (m_AnimationClipPlayable.IsValid())
            {
                m_AnimationClipPlayable.SetApplyPlayableIK(m_ApplyIK);
            }
        }

        public void UpdateState()
        {
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i] == null || VAW.VA.Bones[i] == null) continue;
                if (Bones[i].activeSelf != VAW.VA.Bones[i].activeSelf)
                    Bones[i].SetActive(VAW.VA.Bones[i].activeSelf);
            }
            foreach (var pair in rendererDictionary)
            {
                if (pair.Key == null || pair.Value == null) continue;
                if (pair.Key.enabled != pair.Value.enabled)
                    pair.Key.enabled = pair.Value.enabled;
            }
            if (!GameObject.activeSelf)
                GameObject.SetActive(true);
        }

        public void SampleAnimation(AnimationClip clip, float time)
        {
            ResetTranformRoot();

            VAW.VA.UpdateSyncEditorCurveClip();

            if (Animator != null)
            {
                PlayableGraphReady(clip);

                m_AnimationClipPlayable.SetTime(time);
                m_PlayableGraph.Evaluate();
            }
            else if (Animation != null)
            {
                SampleAnimationLegacy(clip, time);
            }
        }
        public void SampleAnimationLegacy(AnimationClip clip, float time)
        {
            ResetTranformRoot();

            PlayableGraphReady(clip);
            #region Offset
            if (m_AnimationOffsetPlayable.IsValid())
            {
#if UNITY_2022_3_OR_NEWER
                GameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
                GameObject.transform.localPosition = Vector3.zero;
                GameObject.transform.localRotation = Quaternion.identity;
#endif
            }
            #endregion

            VAW.VA.UpdateSyncEditorCurveClip();

            if (Animator != null)
            {
                #region Initialize
                if (tmpAnimatorController == null)
                {
                    tmpAnimatorController = new UnityEditor.Animations.AnimatorController
                    {
                        name = "Very Animation Temporary Controller"
                    };
                    tmpAnimatorController.hideFlags |= HideFlags.HideAndDontSave;
                    {
                        tmpAnimatorControllerLayer = new AnimatorControllerLayer
                        {
                            name = "Very Animation Layer"
                        };
                        {
                            tmpAnimatorControllerLayer.stateMachine = new AnimatorStateMachine
                            {
                                name = tmpAnimatorControllerLayer.name
                            };
                            tmpAnimatorControllerLayer.stateMachine.hideFlags |= HideFlags.HideAndDontSave;
                            {
                                tmpAnimationState = new AnimatorState();
                                tmpAnimationState.hideFlags |= HideFlags.HideAndDontSave;
                                tmpAnimationState.name = "Animation";
                                tmpAnimatorControllerLayer.stateMachine.states = new ChildAnimatorState[]
                                {
                                    new()
                                    {
                                        state = tmpAnimationState,
                                    },
                                };
                            }
                        }
                        tmpAnimatorController.layers = new AnimatorControllerLayer[] { tmpAnimatorControllerLayer };
                    }
                }
                if (Animator.runtimeAnimatorController != tmpAnimatorController)
                    UnityEditor.Animations.AnimatorController.SetAnimatorController(Animator, tmpAnimatorController);
                #endregion

                #region Settings
                if (tmpAnimatorControllerLayer.iKPass != m_ApplyIK)
                {
                    tmpAnimatorControllerLayer.iKPass = m_ApplyIK;
                    tmpAnimatorController.layers = new AnimatorControllerLayer[] { tmpAnimatorControllerLayer };
                }
                if (tmpAnimationState.motion != clip)
                    tmpAnimationState.motion = clip;
                #endregion

                if (!Animator.isInitialized)
                    Animator.Rebind();

                if (m_ApplyIK)
                {
                    float normalizedTime;
                    {
                        AnimationClipSettings animationClipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                        var totalTime = animationClipSettings.stopTime - animationClipSettings.startTime;
                        var ttime = time;
                        if (ttime > 0f && ttime >= totalTime)
                            ttime = totalTime - 0.0001f;
                        normalizedTime = totalTime == 0.0 ? 0.0f : (float)((ttime - animationClipSettings.startTime) / (totalTime));
                    }
                    Animator.Play(tmpAnimationState.nameHash, 0, normalizedTime);
                    Animator.Update(0f);
                }
                else
                {
                    if (Animator.applyRootMotion)
                        Animator.applyRootMotion = false;

                    clip.SampleAnimation(GameObject, time);

                    if (Animator.applyRootMotion != VAW.Animator.applyRootMotion)
                        Animator.applyRootMotion = VAW.Animator.applyRootMotion;
                }
            }
            else if (Animation != null)
            {
                WrapMode? beforeWrapMode = null;
                try
                {
                    if (clip.wrapMode != WrapMode.Default)
                    {
                        beforeWrapMode = clip.wrapMode;
                        clip.wrapMode = WrapMode.Default;
                    }

                    clip.SampleAnimation(GameObject, time);
                }
                finally
                {
                    if (beforeWrapMode.HasValue)
                    {
                        clip.wrapMode = beforeWrapMode.Value;
                    }
                }
            }

            #region Offset
            if (m_AnimationOffsetPlayable.IsValid())
            {
#if UNITY_2022_3_OR_NEWER
                GameObject.transform.SetLocalPositionAndRotation((m_OffsetRotation * GameObject.transform.localPosition) + m_OffsetPosition, m_OffsetRotation * GameObject.transform.localRotation);
#else
                GameObject.transform.localPosition = (m_OffsetRotation * GameObject.transform.localPosition) + m_OffsetPosition;
                GameObject.transform.localRotation = m_OffsetRotation * GameObject.transform.localRotation;
#endif
            }
            #endregion
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

        private void UpdateBones()
        {
            #region Humanoid
            if (VAW.VA.IsHuman && Animator != null)
            {
                if (!Animator.isInitialized)
                    Animator.Rebind();

                HumanoidBones = new GameObject[HumanTrait.BoneCount];
                for (int bone = 0; bone < HumanTrait.BoneCount; bone++)
                {
                    var t = Animator.GetBoneTransform((HumanBodyBones)bone);
                    if (t != null)
                    {
                        HumanoidBones[bone] = t.gameObject;
                    }
                }
                HumanoidHipsTransform = HumanoidBones[(int)HumanBodyBones.Hips].transform;
                HumanPoseHandler = new HumanPoseHandler(Animator.avatar, VAW.VA.UAnimator.GetAvatarRoot(Animator));
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
            }
            else
            {
                HumanoidBones = null;
                HumanoidHipsTransform = null;
                HumanPoseHandler = null;
            }
            #endregion
            #region bones
            Bones = EditorCommon.GetHierarchyGameObject(GameObject).ToArray();
            BoneDictionary = new Dictionary<GameObject, int>(Bones.Length);
            for (int i = 0; i < Bones.Length; i++)
            {
                BoneDictionary.Add(Bones[i], i);
            }
            #endregion
        }

        private void PlayableGraphReady(AnimationClip clip)
        {
            if (Animator == null)
                return;

            UnityEditor.Animations.AnimatorController.SetAnimatorController(Animator, null);

            bool animatesRootTransform = Animator.applyRootMotion;
            bool requiresOffsetPlayable = VAW.VA.RootMotionBoneIndex >= 0;
            bool requiresMotionXPlayable = animatesRootTransform;
            bool usesAbsoluteMotion = true;
            if (VAW.VA.UAw.GetLinkedWithTimeline())
            {
#if VERYANIMATION_TIMELINE
                VAW.VA.UAw.GetTimelineAnimationTrackInfo(out animatesRootTransform, out requiresMotionXPlayable, out usesAbsoluteMotion);
                requiresOffsetPlayable = requiresMotionXPlayable;
#else
                Assert.IsTrue(false);
#endif
            }

            if (m_PlayableGraph.IsValid())
            {
                if (m_AnimationClipPlayable.IsValid())
                {
                    if (m_AnimationClipPlayable.GetAnimationClip() == clip &&
                        m_AnimatesRootTransform == animatesRootTransform &&
                        m_RequiresOffsetPlayable == requiresOffsetPlayable &&
                        m_RequiresMotionXPlayable == requiresMotionXPlayable &&
                        m_UsesAbsoluteMotion == usesAbsoluteMotion)
                    {
                        return;
                    }
                }
                m_PlayableGraph.Destroy();
            }
            m_AnimatesRootTransform = animatesRootTransform;
            m_RequiresOffsetPlayable = requiresOffsetPlayable;
            m_RequiresMotionXPlayable = requiresMotionXPlayable;
            m_UsesAbsoluteMotion = usesAbsoluteMotion;

            m_PlayableGraph = PlayableGraph.Create(GameObject.name + "_DummyObject");
            m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            m_AnimationClipPlayable = AnimationClipPlayable.Create(m_PlayableGraph, clip);
            m_AnimationClipPlayable.SetApplyPlayableIK(m_ApplyIK);
            m_AnimationClipPlayable.SetApplyFootIK(false);
            m_UAnimationClipPlayable ??= new UAnimationClipPlayable();
            m_UAnimationClipPlayable.SetRemoveStartOffset(m_AnimationClipPlayable, m_RemoveStartOffset);
            m_UAnimationClipPlayable.SetOverrideLoopTime(m_AnimationClipPlayable, true);
            m_UAnimationClipPlayable.SetLoopTime(m_AnimationClipPlayable, false);

            Playable rootPlayable = m_AnimationClipPlayable;

            if (m_AnimatesRootTransform)
            {
                if (m_RequiresOffsetPlayable)
                {
                    m_UAnimationOffsetPlayable ??= new UAnimationOffsetPlayable();
                    m_AnimationOffsetPlayable = m_UAnimationOffsetPlayable.Create(m_PlayableGraph, m_OffsetPosition, m_OffsetRotation, 1);
                    m_AnimationOffsetPlayable.SetInputWeight(0, 1f);
                    m_PlayableGraph.Connect(rootPlayable, 0, m_AnimationOffsetPlayable, 0);
                    rootPlayable = m_AnimationOffsetPlayable;
                }
                if (m_RequiresMotionXPlayable)
                {
                    m_UAnimationMotionXToDeltaPlayable ??= new UAnimationMotionXToDeltaPlayable();
                    m_AnimationMotionXToDeltaPlayable = m_UAnimationMotionXToDeltaPlayable.Create(m_PlayableGraph);
                    m_UAnimationMotionXToDeltaPlayable.SetAbsoluteMotion(m_AnimationMotionXToDeltaPlayable, m_UsesAbsoluteMotion);
                    m_AnimationMotionXToDeltaPlayable.SetInputWeight(0, 1f);
                    m_PlayableGraph.Connect(rootPlayable, 0, m_AnimationMotionXToDeltaPlayable, 0);
                    rootPlayable = m_AnimationMotionXToDeltaPlayable;
                }
            }

            var playableOutput = AnimationPlayableOutput.Create(m_PlayableGraph, "Animation", Animator);
            playableOutput.SetSourcePlayable(rootPlayable);
        }
    }
}
