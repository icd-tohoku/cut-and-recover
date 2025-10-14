using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace VeryAnimation
{
    internal class SynchronizeAnimation : IDisposable
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        private class SynchronizeObject : IDisposable
        {
            public GameObject gameObject;
            public Animator animator;
            public Animation animation;
            public AnimationClip clip;
            public AnimationClipValueSave animationClipValueSave;
            public TransformPoseSave transformPoseSave;
            public PlayableGraph m_PlayableGraph;
            public AnimationClipPlayable m_AnimationClipPlayable;
            public Playable m_AnimationMotionXToDeltaPlayable;
            public Playable m_AnimationOffsetPlayable;
#if VERYANIMATION_ANIMATIONRIGGING
            public VeryAnimationRigBuilder m_VARigBuilder;
            public RigBuilder m_RigBuilder;
#endif
            private readonly bool saveFireEvents;

            public SynchronizeObject(Animator animator, AnimationClip clip)
            {
                this.animator = animator;
                gameObject = animator.gameObject;
                animationClipValueSave = new AnimationClipValueSave(gameObject, clip);
                transformPoseSave = new TransformPoseSave(gameObject);
                this.clip = clip;

                #region BuildPlayableGraph
                {
                    m_PlayableGraph = PlayableGraph.Create("SynchronizeObject." + gameObject.name);
                    m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                    m_AnimationClipPlayable = AnimationClipPlayable.Create(m_PlayableGraph, clip);
                    m_AnimationClipPlayable.SetApplyPlayableIK(false);
                    m_AnimationClipPlayable.SetApplyFootIK(false);
                    {
                        var uAnimationClipPlayable = new UAnimationClipPlayable();
                        uAnimationClipPlayable.SetRemoveStartOffset(m_AnimationClipPlayable, true);
                    }
                    Playable rootPlayable = m_AnimationClipPlayable;

#if VERYANIMATION_ANIMATIONRIGGING
                    m_VARigBuilder = gameObject.GetComponent<VeryAnimationRigBuilder>();
                    m_RigBuilder = gameObject.GetComponent<RigBuilder>();
                    if (m_VARigBuilder != null && m_RigBuilder != null)
                    {
                        m_VARigBuilder.StartPreview();
                        m_RigBuilder.StartPreview();
                        rootPlayable = m_VARigBuilder.BuildPreviewGraph(m_PlayableGraph, rootPlayable);
                        rootPlayable = m_RigBuilder.BuildPreviewGraph(m_PlayableGraph, rootPlayable);
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
                            var uAnimationOffsetPlayable = new UAnimationOffsetPlayable();
                            m_AnimationOffsetPlayable = uAnimationOffsetPlayable.Create(m_PlayableGraph, transformPoseSave.StartLocalPosition, transformPoseSave.StartLocalRotation, 1);
                            m_AnimationOffsetPlayable.SetInputWeight(0, 1f);
                            m_PlayableGraph.Connect(rootPlayable, 0, m_AnimationOffsetPlayable, 0);
                            rootPlayable = m_AnimationOffsetPlayable;
                        }
                        {
                            var uAnimationMotionXToDeltaPlayable = new UAnimationMotionXToDeltaPlayable();
                            m_AnimationMotionXToDeltaPlayable = uAnimationMotionXToDeltaPlayable.Create(m_PlayableGraph);
                            uAnimationMotionXToDeltaPlayable.SetAbsoluteMotion(m_AnimationMotionXToDeltaPlayable, true);
                            m_AnimationMotionXToDeltaPlayable.SetInputWeight(0, 1f);
                            m_PlayableGraph.Connect(rootPlayable, 0, m_AnimationMotionXToDeltaPlayable, 0);
                            rootPlayable = m_AnimationMotionXToDeltaPlayable;
                        }
                    }

                    var playableOutput = AnimationPlayableOutput.Create(m_PlayableGraph, "Animation", animator);
                    playableOutput.SetSourcePlayable(rootPlayable);
                }
                #endregion
                #region Save
                saveFireEvents = animator.fireEvents;
                animator.fireEvents = false;
                #endregion
            }
            public SynchronizeObject(Animation animation, AnimationClip clip)
            {
                this.animation = animation;
                gameObject = animation.gameObject;
                animationClipValueSave = new AnimationClipValueSave(gameObject, clip);
                transformPoseSave = new TransformPoseSave(gameObject);
                this.clip = clip;
            }
            ~SynchronizeObject()
            {
                Assert.IsFalse(m_PlayableGraph.IsValid());
            }
            public void Dispose()
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
                if (m_PlayableGraph.IsValid())
                    m_PlayableGraph.Destroy();

                if (animationClipValueSave != null)
                {
                    animationClipValueSave.LoadProperty();
                    animationClipValueSave.Dispose();
                    animationClipValueSave = null;
                }
                if (transformPoseSave != null)
                {
                    transformPoseSave.ResetOriginalTransform();
                    transformPoseSave = null;
                }

                #region Load
                if (animator != null)
                    animator.fireEvents = saveFireEvents;
                #endregion
            }
        }
        private List<SynchronizeObject> synchronizeObjects;
        private float currentTime;

        public SynchronizeAnimation()
        {
            synchronizeObjects = new List<SynchronizeObject>();

            void AddGameObject(GameObject go)
            {
                bool CheckHideFlags(Transform t)
                {
                    if ((t.gameObject.hideFlags & (HideFlags.HideAndDontSave | HideFlags.NotEditable)) != 0)
                        return false;

                    if (t.parent != null)
                        return CheckHideFlags(t.parent);
                    else
                        return true;
                }

                foreach (var animator in go.GetComponentsInChildren<Animator>(true))
                {
                    if (animator == null || animator == VAW.Animator)
                        continue;
                    if (!animator.gameObject.activeInHierarchy || !animator.enabled || !animator.hasTransformHierarchy)
                        continue;
                    if (!CheckHideFlags(animator.transform))
                        continue;
                    #region Clip
                    AnimationClip clip = null;
                    {
                        var saveSettings = animator.GetComponent<VeryAnimationSaveSettings>();
                        if (saveSettings != null && saveSettings.lastSelectAnimationClip != null)
                        {
                            if (ArrayUtility.Contains(AnimationUtility.GetAnimationClips(animator.gameObject), saveSettings.lastSelectAnimationClip))
                                clip = saveSettings.lastSelectAnimationClip;
                        }
                        if (clip == null)
                        {
                            var ac = EditorCommon.GetAnimatorController(animator);
                            if (ac != null && ac.layers.Length > 0)
                            {
                                var state = ac.layers[0].stateMachine.defaultState;
                                if (state != null)
                                {
                                    if (state.motion is UnityEditor.Animations.BlendTree)
                                    {
                                        void FindBlendTree(BlendTree blendTree)
                                        {
                                            if (blendTree.children == null) return;
                                            var children = blendTree.children;
                                            for (int i = 0; i < children.Length; i++)
                                            {
                                                if (children[i].motion is UnityEditor.Animations.BlendTree)
                                                {
                                                    FindBlendTree(children[i].motion as UnityEditor.Animations.BlendTree);
                                                }
                                                else if (children[i].motion is AnimationClip)
                                                {
                                                    clip = children[i].motion as AnimationClip;
                                                }
                                                else
                                                {
                                                    Debug.LogWarningFormat("<color=blue>[Very Animation]</color>unknown support type {0}", children[i].motion);
                                                }
                                                if (clip != null) break;
                                            }
                                            blendTree.children = children;
                                        }

                                        FindBlendTree(state.motion as UnityEditor.Animations.BlendTree);
                                    }
                                    else if (state.motion is AnimationClip)
                                    {
                                        clip = state.motion as AnimationClip;
                                    }
                                    else if (state.motion != null)
                                    {
                                        Debug.LogWarningFormat("<color=blue>[Very Animation]</color>unknown support type {0}", state.motion);
                                    }
                                }
                            }
                            if (clip != null)
                            {
                                var owc = animator.runtimeAnimatorController as AnimatorOverrideController;
                                if (owc != null)
                                {
                                    clip = owc[clip];
                                }
                            }
                        }
                    }
                    if (clip == null)
                        continue;
                    #endregion

                    synchronizeObjects.Add(new SynchronizeObject(animator, clip));
                }
                foreach (var animation in go.GetComponentsInChildren<Animation>(true))
                {
                    if (animation == null || animation == VAW.Animation)
                        continue;
                    if (!animation.gameObject.activeInHierarchy || !animation.enabled)
                        continue;
                    if (!CheckHideFlags(animation.transform))
                        continue;
                    #region Clip
                    AnimationClip clip = null;
                    {
                        var saveSettings = animation.GetComponent<VeryAnimationSaveSettings>();
                        if (saveSettings != null && saveSettings.lastSelectAnimationClip != null)
                        {
                            if (ArrayUtility.Contains(AnimationUtility.GetAnimationClips(animation.gameObject), saveSettings.lastSelectAnimationClip))
                                clip = saveSettings.lastSelectAnimationClip;
                        }
                        if (clip == null)
                            clip = animation.clip;
                    }
                    if (clip == null)
                        continue;
                    #endregion

                    synchronizeObjects.Add(new SynchronizeObject(animation, clip));
                }
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var scene = PrefabStageUtility.GetCurrentPrefabStage().scene;
                if (scene.isLoaded)
                {
                    foreach (var go in scene.GetRootGameObjects())
                        AddGameObject(go);
                }
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                    {
                        foreach (var go in scene.GetRootGameObjects())
                            AddGameObject(go);
                    }
                }
            }
        }
        ~SynchronizeAnimation()
        {
            Assert.IsNull(synchronizeObjects);
        }
        public void Dispose()
        {
            if (synchronizeObjects != null)
            {
                foreach (var data in synchronizeObjects)
                {
                    data.Dispose();
                }
                synchronizeObjects = null;
            }
        }

        public void SetTime(float time)
        {
            currentTime = time;
            foreach (var data in synchronizeObjects)
            {
                SampleAnimation(data);
            }
        }

        private void SampleAnimation(SynchronizeObject data)
        {
            if (data == null || data.clip == null)
                return;

            data.animationClipValueSave.Load();
            data.transformPoseSave.ResetOriginalTransform();

            if (data.animator != null)
            {
                #region Animator
                #region Playable
                if (data.m_AnimationClipPlayable.IsValid())
                {
                    data.m_AnimationClipPlayable.SetTime(currentTime);
                    if (data.m_PlayableGraph.IsValid())
                    {
#if VERYANIMATION_ANIMATIONRIGGING
                        if (data.m_RigBuilder != null)
                            data.m_RigBuilder.UpdatePreviewGraph(data.m_PlayableGraph);
#endif
                        data.m_PlayableGraph.Evaluate();
                    }
                }
                #endregion
                #endregion
            }
            else if (data.animation != null)
            {
                #region Animation
                data.clip.SampleAnimation(data.gameObject, currentTime);
                #endregion
            }
        }
        public void UpdateSameClip(AnimationClip clip)
        {
            foreach (var data in synchronizeObjects)
            {
                if (data == null || data.clip != clip)
                    continue;
                SampleAnimation(data);
            }
        }
    }
}
