using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations;


#if VERYANIMATION_TIMELINE
using UnityEngine.Timeline;
#endif

namespace VeryAnimation
{
    internal class UAnimationWindow
    {
        protected VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        protected Func<object, IList> dg_get_s_AnimationWindows;
        protected Func<object, object> dg_get_m_AnimEditor;
        protected Func<object, object> dg_get_m_LockTracker;
        protected MethodInfo mi_OnSelectionChange;
        protected MethodInfo mi_EditSequencerClip;

        protected class UAnimEditor
        {
            protected PropertyInfo pi_selection;
            protected PropertyInfo pi_selectedItem;
            private readonly PropertyInfo pi_triggerFraming;
            private readonly MethodInfo mi_SwitchBetweenCurvesAndDopesheet;
            private readonly MethodInfo mi_UpdateSelectedKeysToCurveEditor;
            private readonly Func<object, object> dg_get_m_State;
            private Func<object> dg_get_selection;
            private Func<object> dg_get_selectedItem;
            private Func<object> dg_get_curveEditor;

            public UAnimEditor(Assembly asmUnityEditor)
            {
                var animEditorType = asmUnityEditor.GetType("UnityEditor.AnimEditor");
                pi_selection = animEditorType.GetProperty("selection");
                Assert.IsNotNull(pi_selectedItem = animEditorType.GetProperty("selection"));
                Assert.IsNotNull(pi_triggerFraming = animEditorType.GetProperty("triggerFraming", BindingFlags.NonPublic | BindingFlags.Instance));
                Assert.IsNotNull(mi_SwitchBetweenCurvesAndDopesheet = animEditorType.GetMethod("SwitchBetweenCurvesAndDopesheet", BindingFlags.NonPublic | BindingFlags.Instance));
                Assert.IsNotNull(mi_UpdateSelectedKeysToCurveEditor = animEditorType.GetMethod("UpdateSelectedKeysToCurveEditor", BindingFlags.NonPublic | BindingFlags.Instance));
                Assert.IsNotNull(dg_get_m_State = EditorCommon.CreateGetFieldDelegate<object>(animEditorType.GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance)));
            }

            public object GetAnimationWindowState(object instance)
            {
                if (instance == null) return null;
                return dg_get_m_State(instance);
            }

            public object GetSelection(object instance)
            {
                if (instance == null) return null;
                if (dg_get_selection == null || dg_get_selection.Target != instance)
                    dg_get_selection = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, pi_selection.GetGetMethod());
                return dg_get_selection();
            }
            public object GetSelectedItem(object instance)
            {
                if (instance == null) return null;
                if (dg_get_selectedItem == null || dg_get_selectedItem.Target != instance)
                    dg_get_selectedItem = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, pi_selectedItem.GetGetMethod());
                return dg_get_selectedItem();
            }
            public void SetTriggerFraming(object instance)
            {
                if (instance == null) return;
                pi_triggerFraming.SetValue(instance, true, null);
            }

            public void SwitchBetweenCurvesAndDopesheet(object instance)
            {
                if (instance == null) return;
                mi_SwitchBetweenCurvesAndDopesheet.Invoke(instance, null);
            }

            public void UpdateSelectedKeysToCurveEditor(object instance)
            {
                if (instance == null) return;
                mi_UpdateSelectedKeysToCurveEditor.Invoke(instance, null);
            }

            public object GetCurveEditor(object instance)
            {
                if (instance == null) return null;
                if (dg_get_curveEditor == null || dg_get_curveEditor.Target != instance)
                    dg_get_curveEditor = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, instance.GetType().GetProperty("curveEditor", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));
                return dg_get_curveEditor();
            }
        }
        protected class UCurveEditor
        {
            private Func<bool> dg_get_hasSelection;
            private Action dg_ClearSelection;

            public UCurveEditor(Assembly _)
            {
            }

            public bool HasSelection(object instance)
            {
                if (instance == null) return false;
                if (dg_get_hasSelection == null || dg_get_hasSelection.Target != instance)
                    dg_get_hasSelection = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("hasSelection", BindingFlags.Public | BindingFlags.Instance).GetGetMethod());
                return dg_get_hasSelection();
            }
            public void ClearSelection(object instance)
            {
                if (instance == null) return;
                if (dg_ClearSelection == null || dg_ClearSelection.Target != instance)
                    dg_ClearSelection = (Action)Delegate.CreateDelegate(typeof(Action), instance, instance.GetType().GetMethod("ClearSelection", BindingFlags.NonPublic | BindingFlags.Instance));
                dg_ClearSelection();
            }
        }
        protected class UAnimationWindowState
        {
            protected Type animationWindowStateType;
            protected PropertyInfo pi_refresh;
            protected MethodInfo mi_ForceRefresh;
            protected MethodInfo mi_CurveWasModified;
            protected MethodInfo mi_SelectKey;
            protected MethodInfo mi_ClearKeySelections;
            protected MethodInfo mi_ClearHierarchySelection;
            protected MethodInfo mi_SelectHierarchyItem;
            protected MethodInfo mi_UnSelectHierarchyItem;
            protected MethodInfo mi_SnapToFrame;
            protected MethodInfo mi_TimeToFrameRound;
            protected MethodInfo mi_StartRecording;
            protected MethodInfo mi_StopRecording;
            protected MethodInfo mi_StartPlayback;
            protected MethodInfo mi_StopPlayback;
            protected MethodInfo mi_StartPreview;
            protected MethodInfo mi_StopPreview;
            protected Func<object, bool> dg_get_showCurveEditor;
            protected Func<object, object> dg_get_hierarchyData;
            protected Func<object, bool> dg_get_linkedWithSequencer;
            protected Func<object, IList> dg_get_m_ActiveCurvesCache;
            protected Action<object, IList> dg_set_m_ActiveCurvesCache;
            protected Func<object, IList> dg_get_m_dopelinesCache;
            protected Action<object, IList> dg_set_m_dopelinesCache;
            protected Action<object, EditorCurveBinding?> dg_set_m_lastAddedCurveBinding;
            protected Func<object> dg_get_controlInterface;
            protected Func<GameObject> dg_get_activeRootGameObject;
            protected Func<Component> dg_get_activeAnimationPlayer;
            protected Func<bool> dg_get_playing;
            protected Func<bool> dg_get_recording;
            protected Func<bool> dg_get_previewing;
            protected Func<bool> dg_get_canPreview;
            protected Func<int> dg_get_currentFrame;
            protected Action<int> dg_set_currentFrame;
            protected Func<float> dg_get_currentTime;
            protected Action<float> dg_set_currentTime;
            protected Func<IList> dg_get_allCurves;
            protected Func<IList> dg_get_activeCurves;
            protected Func<IList> dg_get_dopelines;
            protected Func<IEnumerable> dg_get_selectedKeyHashes;
            protected Func<float, float, float> dg_SnapToFrame;
            protected Func<float, int> dg_TimeToFrameRound;
            protected Func<AnimationClip> dg_get_activeAnimationClip;
            protected Action<AnimationClip> dg_set_activeAnimationClip;
            protected Action<object, IList> dg_set_m_AllCurvesCache;
            protected Func<bool> dg_get_filterBySelection;
            protected Action<bool> dg_set_filterBySelection;

            public UAnimationWindowState(Assembly asmUnityEditor)
            {
                Assert.IsNotNull(animationWindowStateType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowState"));
                Assert.IsNotNull(pi_refresh = animationWindowStateType.GetProperty("refresh"));
                mi_ForceRefresh = animationWindowStateType.GetMethod("ForceRefresh");
                Assert.IsNotNull(mi_CurveWasModified = animationWindowStateType.GetMethod("CurveWasModified", BindingFlags.Instance | BindingFlags.NonPublic));
                Assert.IsNotNull(mi_SelectKey = animationWindowStateType.GetMethod("SelectKey"));
                Assert.IsNotNull(mi_ClearKeySelections = animationWindowStateType.GetMethod("ClearKeySelections"));
                Assert.IsNotNull(mi_ClearHierarchySelection = animationWindowStateType.GetMethod("ClearHierarchySelection"));
                Assert.IsNotNull(mi_SelectHierarchyItem = animationWindowStateType.GetMethod("SelectHierarchyItem", new Type[] { typeof(int), typeof(bool), typeof(bool) }));
                Assert.IsNotNull(mi_UnSelectHierarchyItem = animationWindowStateType.GetMethod("UnSelectHierarchyItem", new Type[] { typeof(int) }));
                Assert.IsNotNull(mi_SnapToFrame = animationWindowStateType.GetMethod("SnapToFrame", new Type[] { typeof(float), typeof(float) }));
                Assert.IsNotNull(mi_TimeToFrameRound = animationWindowStateType.GetMethod("TimeToFrameRound"));
                mi_StartRecording = animationWindowStateType.GetMethod("StartRecording");
                mi_StopRecording = animationWindowStateType.GetMethod("StopRecording");
                mi_StartPlayback = animationWindowStateType.GetMethod("StartPlayback");
                mi_StopPlayback = animationWindowStateType.GetMethod("StopPlayback");
                mi_StartPreview = animationWindowStateType.GetMethod("StartPreview");
                mi_StopPreview = animationWindowStateType.GetMethod("StopPreview");
                Assert.IsNotNull(dg_get_showCurveEditor = EditorCommon.CreateGetFieldDelegate<bool>(animationWindowStateType.GetField("showCurveEditor")));
                Assert.IsNotNull(dg_get_hierarchyData = EditorCommon.CreateGetFieldDelegate<object>(animationWindowStateType.GetField("hierarchyData")));
                Assert.IsNotNull(dg_get_linkedWithSequencer = EditorCommon.CreateGetFieldDelegate<bool>(animationWindowStateType.GetField("linkedWithSequencer")));
                Assert.IsNotNull(dg_get_m_ActiveCurvesCache = EditorCommon.CreateGetFieldDelegate<IList>(animationWindowStateType.GetField("m_ActiveCurvesCache", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_set_m_ActiveCurvesCache = EditorCommon.CreateSetFieldDelegate<IList>(animationWindowStateType.GetField("m_ActiveCurvesCache", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_get_m_dopelinesCache = EditorCommon.CreateGetFieldDelegate<IList>(animationWindowStateType.GetField("m_dopelinesCache", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_set_m_dopelinesCache = EditorCommon.CreateSetFieldDelegate<IList>(animationWindowStateType.GetField("m_dopelinesCache", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_set_m_lastAddedCurveBinding = EditorCommon.CreateSetFieldDelegate<EditorCurveBinding?>(animationWindowStateType.GetField("m_lastAddedCurveBinding", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_set_m_AllCurvesCache = EditorCommon.CreateSetFieldDelegate<IList>(animationWindowStateType.GetField("m_AllCurvesCache", BindingFlags.NonPublic | BindingFlags.Instance)));
            }

            public enum RefreshType
            {
                None,
                CurvesOnly,
                Everything,
            }

            public bool GetShowCurveEditor(object instance)
            {
                if (instance == null) return false;
                return dg_get_showCurveEditor(instance);
            }
            public object GetHierarchyData(object instance)
            {
                if (instance == null) return null;
                return dg_get_hierarchyData(instance);
            }
            public bool GetLinkedWithSequencer(object instance)
            {
                if (instance == null) return false;
                return dg_get_linkedWithSequencer(instance);
            }
            public virtual object GetControlInterface(object instance)
            {
                if (instance == null) return null;
                if (dg_get_controlInterface == null || dg_get_controlInterface.Target != instance)
                    dg_get_controlInterface = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, instance.GetType().GetProperty("controlInterface").GetGetMethod());
                return dg_get_controlInterface();
            }
            public GameObject GetActiveRootGameObject(object instance)
            {
                if (instance == null) return null;
                if (dg_get_activeRootGameObject == null || dg_get_activeRootGameObject.Target != instance)
                    dg_get_activeRootGameObject = (Func<GameObject>)Delegate.CreateDelegate(typeof(Func<GameObject>), instance, instance.GetType().GetProperty("activeRootGameObject").GetGetMethod());
                return dg_get_activeRootGameObject();
            }
            public Component GetActiveAnimationPlayer(object instance)
            {
                if (instance == null) return null;
                if (dg_get_activeAnimationPlayer == null || dg_get_activeAnimationPlayer.Target != instance)
                    dg_get_activeAnimationPlayer = (Func<Component>)Delegate.CreateDelegate(typeof(Func<Component>), instance, instance.GetType().GetProperty("activeAnimationPlayer").GetGetMethod());
                return dg_get_activeAnimationPlayer();
            }
            public RefreshType GetRefresh(object instance)
            {
                if (instance == null) return RefreshType.None;
                return (RefreshType)pi_refresh.GetValue(instance, null);
            }
            public bool GetPlaying(object instance)
            {
                if (instance == null) return false;
                if (dg_get_playing == null || dg_get_playing.Target != instance)
                    dg_get_playing = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("playing").GetGetMethod());
                return dg_get_playing();
            }
            public bool GetRecording(object instance)
            {
                if (instance == null) return false;
                if (dg_get_recording == null || dg_get_recording.Target != instance)
                    dg_get_recording = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("recording").GetGetMethod());
                return dg_get_recording();
            }
            public int GetCurrentFrame(object instance)
            {
                if (instance == null) return 0;
                if (dg_get_currentFrame == null || dg_get_currentFrame.Target != instance)
                    dg_get_currentFrame = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), instance, instance.GetType().GetProperty("currentFrame").GetGetMethod());
                return dg_get_currentFrame();
            }
            public void SetCurrentFrame(object instance, int value)
            {
                if (instance == null) return;
                if (dg_set_currentFrame == null || dg_set_currentFrame.Target != instance)
                    dg_set_currentFrame = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), instance, instance.GetType().GetProperty("currentFrame").GetSetMethod());
                dg_set_currentFrame(value);
            }
            public float GetCurrentTime(object instance)
            {
                if (instance == null) return 0f;
                if (dg_get_currentTime == null || dg_get_currentTime.Target != instance)
                    dg_get_currentTime = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), instance, instance.GetType().GetProperty("currentTime").GetGetMethod());
                return dg_get_currentTime();
            }
            public void SetCurrentTime(object instance, float value)
            {
                if (instance == null) return;
                if (dg_set_currentTime == null || dg_set_currentTime.Target != instance)
                    dg_set_currentTime = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), instance, instance.GetType().GetProperty("currentTime").GetSetMethod());
                dg_set_currentTime(value);
            }
            public IList GetAllCurves(object instance)
            {
                if (instance == null) return null;
                if (dg_get_allCurves == null || dg_get_allCurves.Target != instance)
                    dg_get_allCurves = (Func<IList>)Delegate.CreateDelegate(typeof(Func<IList>), instance, instance.GetType().GetProperty("allCurves").GetGetMethod());
                return dg_get_allCurves();
            }
            public IList GetActiveCurves(object instance)
            {
                if (instance == null) return null;
                //Cache Hit
                var list = dg_get_m_ActiveCurvesCache.Invoke(instance);
                if (list != null)
                    return list;
                //Cache Miss
                if (dg_get_activeCurves == null || dg_get_activeCurves.Target != instance)
                    dg_get_activeCurves = (Func<IList>)Delegate.CreateDelegate(typeof(Func<IList>), instance, instance.GetType().GetProperty("activeCurves").GetGetMethod());
                list = dg_get_activeCurves();
                dg_set_m_ActiveCurvesCache(instance, null);  //Cache Clear
                return list;
            }
            public IList GetDopelines(object instance)
            {
                if (instance == null) return null;
                //Cache Hit
                var list = dg_get_m_dopelinesCache(instance);
                if (list != null)
                    return list;
                //Cache Miss
                if (dg_get_dopelines == null || dg_get_dopelines.Target != instance)
                    dg_get_dopelines = (Func<IList>)Delegate.CreateDelegate(typeof(Func<IList>), instance, instance.GetType().GetProperty("dopelines").GetGetMethod());
                list = dg_get_dopelines();
                dg_set_m_dopelinesCache(instance, null);  //Cache Clear
                return list;
            }
            public virtual void ClearCache(object instance)
            {
                if (instance == null) return;
                dg_set_m_ActiveCurvesCache(instance, null);  //Cache Clear
                dg_set_m_dopelinesCache(instance, null);  //Cache Clear
                dg_set_m_AllCurvesCache(instance, null);  //Cache Clear
            }
            public void ClearLastAddedCurveBinding(object instance)
            {
                if (instance == null) return;
                dg_set_m_lastAddedCurveBinding(instance, null);
            }

            public IEnumerable GetSelectedKeyHashes(object instance)
            {
                if (instance == null) return null;
                if (dg_get_selectedKeyHashes == null || dg_get_selectedKeyHashes.Target != instance)
                    dg_get_selectedKeyHashes = (Func<IEnumerable>)Delegate.CreateDelegate(typeof(Func<IEnumerable>), instance, instance.GetType().GetProperty("selectedKeyHashes", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
                return dg_get_selectedKeyHashes();
            }

            public virtual void ForceRefresh(object instance)
            {
                if (instance == null) return;
                mi_ForceRefresh.Invoke(instance, null);
            }
            public void SelectKey(object instance, object keyframe)
            {
                if (instance == null) return;
                mi_SelectKey.Invoke(instance, new object[] { keyframe });
            }
            public void ClearKeySelections(object instance)
            {
                if (instance == null) return;
                mi_ClearKeySelections.Invoke(instance, null);
            }
            public void ClearHierarchySelection(object instance)
            {
                if (instance == null) return;
                mi_ClearHierarchySelection.Invoke(instance, null);
            }
            public void SelectHierarchyItem(object instance, int hierarchyNodeID, bool additive, bool triggerSceneSelectionSync)
            {
                if (instance == null) return;
                mi_SelectHierarchyItem.Invoke(instance, new object[] { hierarchyNodeID, additive, triggerSceneSelectionSync });
            }
            public void UnSelectHierarchyItem(object instance, int hierarchyNodeID)
            {
                if (instance == null) return;
                mi_UnSelectHierarchyItem.Invoke(instance, new object[] { hierarchyNodeID });
            }
            public float SnapToFrame(object instance, float time, float fps)
            {
                if (instance == null) return 0f;
                if (dg_SnapToFrame == null || dg_SnapToFrame.Target != instance)
                    dg_SnapToFrame = (Func<float, float, float>)Delegate.CreateDelegate(typeof(Func<float, float, float>), instance, mi_SnapToFrame);
                return dg_SnapToFrame(time, fps);
            }
            public int TimeToFrameRound(object instance, float time)
            {
                if (instance == null) return 0;
                if (dg_TimeToFrameRound == null || dg_TimeToFrameRound.Target != instance)
                    dg_TimeToFrameRound = (Func<float, int>)Delegate.CreateDelegate(typeof(Func<float, int>), instance, mi_TimeToFrameRound);
                return dg_TimeToFrameRound(time);
            }

            public virtual bool StartRecording(object instance)
            {
                if (instance == null) return false;
                try
                {
                    mi_StartRecording.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            }
            public virtual bool StopRecording(object instance)
            {
                if (instance == null) return false;
                try
                {
                    mi_StopRecording.Invoke(instance, null);
                    mi_StopPreview.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            }
            public virtual bool StartPlayback(object instance)
            {
                if (instance == null) return false;
                try
                {
                    mi_StartPlayback.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            }
            public virtual bool StopPlayback(object instance)
            {
                if (instance == null) return false;
                try
                {
                    mi_StopPlayback.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            }
            public virtual bool StartPreview(object instance)
            {
                if (instance == null) return false;
                try
                {
                    mi_StartPreview.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            }
            public virtual bool StopPreview(object instance)
            {
                if (instance == null) return false;
                try
                {
                    mi_StopPreview.Invoke(instance, null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return false;
                }
                return true;
            }
            public bool GetPreviewing(object instance)
            {
                if (instance == null) return false;
                if (dg_get_previewing == null || dg_get_previewing.Target != instance)
                    dg_get_previewing = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("previewing").GetGetMethod());
                return dg_get_previewing();
            }
            public bool GetCanPreview(object instance)
            {
                if (instance == null) return false;
                if (dg_get_canPreview == null || dg_get_canPreview.Target != instance)
                    dg_get_canPreview = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("canPreview").GetGetMethod());
                return dg_get_canPreview();
            }

            public AnimationClip GetActiveAnimationClip(object instance)
            {
                if (instance == null) return null;
                if (dg_get_activeAnimationClip == null || dg_get_activeAnimationClip.Target != instance)
                    dg_get_activeAnimationClip = (Func<AnimationClip>)Delegate.CreateDelegate(typeof(Func<AnimationClip>), instance, instance.GetType().GetProperty("activeAnimationClip").GetGetMethod());
                return dg_get_activeAnimationClip();
            }
            public void SetActiveAnimationClip(object instance, AnimationClip clip)
            {
                if (instance == null) return;
                if (dg_set_activeAnimationClip == null || dg_set_activeAnimationClip.Target != instance)
                    dg_set_activeAnimationClip = (Action<AnimationClip>)Delegate.CreateDelegate(typeof(Action<AnimationClip>), instance, instance.GetType().GetProperty("activeAnimationClip").GetSetMethod());
                dg_set_activeAnimationClip(clip);
            }

            public bool GetFilterBySelection(object instance)
            {
                if (instance == null) return false;
                if (dg_get_filterBySelection == null || dg_get_filterBySelection.Target != instance)
                    dg_get_filterBySelection = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("filterBySelection").GetGetMethod());
                return dg_get_filterBySelection();
            }
            public void SetFilterBySelection(object instance, bool enable)
            {
                if (instance == null) return;
                if (dg_set_filterBySelection == null || dg_set_filterBySelection.Target != instance)
                    dg_set_filterBySelection = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), instance, instance.GetType().GetProperty("filterBySelection").GetSetMethod());
                dg_set_filterBySelection(enable);
            }
        }
        protected class UAnimationWindowControl
        {
            protected MethodInfo mi_GoToNextKeyframe;
            protected MethodInfo mi_GoToPreviousKeyframe;
            protected MethodInfo mi_GoToFirstKeyframe;
            protected MethodInfo mi_GoToLastKeyframe;
            protected Func<bool> dg_get_canRecord;
            protected Func<object, PlayableGraph> dg_get_m_Graph;
            protected Func<object, AnimationClipPlayable> dg_get_m_ClipPlayable;
            protected Func<object, AnimationClipPlayable> dg_get_m_CandidateClipPlayable;
            protected Action<int> dg_ResampleAnimationHasFlag;
            protected Action dg_DestroyGraph;
            protected Func<object, AnimationClipPlayable> dg_get_m_DefaultPosePlayable;

            public UAnimationWindowControl(Assembly asmUnityEditor)
            {
                var iAnimationWindowControlType = asmUnityEditor.GetType("UnityEditorInternal.IAnimationWindowControl");
                mi_GoToNextKeyframe = iAnimationWindowControlType.GetMethod("GoToNextKeyframe", new Type[] { });
                mi_GoToPreviousKeyframe = iAnimationWindowControlType.GetMethod("GoToPreviousKeyframe", new Type[] { });
                mi_GoToFirstKeyframe = iAnimationWindowControlType.GetMethod("GoToFirstKeyframe", new Type[] { });
                mi_GoToLastKeyframe = iAnimationWindowControlType.GetMethod("GoToLastKeyframe", new Type[] { });
                var animationWindowControlType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowControl");
                if (animationWindowControlType != null)
                {
                    Assert.IsNotNull(dg_get_m_Graph = EditorCommon.CreateGetFieldDelegate<PlayableGraph>(animationWindowControlType.GetField("m_Graph", BindingFlags.NonPublic | BindingFlags.Instance)));
                    Assert.IsNotNull(dg_get_m_ClipPlayable = EditorCommon.CreateGetFieldDelegate<AnimationClipPlayable>(animationWindowControlType.GetField("m_ClipPlayable", BindingFlags.NonPublic | BindingFlags.Instance)));
                    Assert.IsNotNull(dg_get_m_CandidateClipPlayable = EditorCommon.CreateGetFieldDelegate<AnimationClipPlayable>(animationWindowControlType.GetField("m_CandidateClipPlayable", BindingFlags.NonPublic | BindingFlags.Instance)));
                    Assert.IsNotNull(dg_get_m_DefaultPosePlayable = EditorCommon.CreateGetFieldDelegate<AnimationClipPlayable>(animationWindowControlType.GetField("m_DefaultPosePlayable", BindingFlags.NonPublic | BindingFlags.Instance)));
                }
            }

            public virtual bool GetCanRecord(object instance)
            {
                if (instance == null) return false;
                if (dg_get_canRecord == null || dg_get_canRecord.Target != instance)
                    dg_get_canRecord = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("canRecord").GetGetMethod());
                return dg_get_canRecord();
            }
            public virtual void ResampleAnimation(object instance)
            {
                if (instance == null) return;
                if (dg_ResampleAnimationHasFlag == null || dg_ResampleAnimationHasFlag.Target != instance)
                    dg_ResampleAnimationHasFlag = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), instance, instance.GetType().GetMethod("ResampleAnimation", BindingFlags.NonPublic | BindingFlags.Instance));
                dg_ResampleAnimationHasFlag(0);
            }
            public void GoToNextKeyframe(object instance)
            {
                if (instance == null) return;
                mi_GoToNextKeyframe.Invoke(instance, null);
            }
            public void GoToPreviousKeyframe(object instance)
            {
                if (instance == null) return;
                mi_GoToPreviousKeyframe.Invoke(instance, null);
            }
            public void GoToFirstKeyframe(object instance)
            {
                if (instance == null) return;
                mi_GoToFirstKeyframe.Invoke(instance, null);
            }
            public void GoToLastKeyframe(object instance)
            {
                if (instance == null) return;
                mi_GoToLastKeyframe.Invoke(instance, null);
            }

            public PlayableGraph GetGraph(object instance)
            {
                if (instance == null) return new PlayableGraph();
                return dg_get_m_Graph(instance);
            }
            public void DestroyGraph(object instance)
            {
                if (instance == null) return;
                if (dg_DestroyGraph == null || dg_DestroyGraph.Target != instance)
                    dg_DestroyGraph = (Action)Delegate.CreateDelegate(typeof(Action), instance, instance.GetType().GetMethod("DestroyGraph", BindingFlags.NonPublic | BindingFlags.Instance));
                dg_DestroyGraph();
            }
            public AnimationClipPlayable GetClipPlayable(object instance)
            {
                if (instance == null) return new AnimationClipPlayable();
                return dg_get_m_ClipPlayable(instance);
            }
            public AnimationClipPlayable GetCandidateClipPlayable(object instance)
            {
                if (instance == null) return new AnimationClipPlayable();
                return dg_get_m_CandidateClipPlayable(instance);
            }

            public AnimationClipPlayable GetDefaultPosePlayable(object instance)
            {
                if (instance == null) return new AnimationClipPlayable();
                return dg_get_m_DefaultPosePlayable(instance);
            }
        }
        protected class UAnimationKeyTime
        {
            protected MethodInfo mi_Time;

            public UAnimationKeyTime(Assembly asmUnityEditor)
            {
                var animationKeyTimeType = asmUnityEditor.GetType("UnityEditorInternal.AnimationKeyTime");
                mi_Time = animationKeyTimeType.GetMethod("Time", BindingFlags.Public | BindingFlags.Static);
            }
            public object Time(float time, float frameRate)
            {
                return mi_Time.Invoke(null, new object[] { time, frameRate });
            }
        }
        protected class UAnimationWindowCurve
        {
            public Type CurveType { get; private set; }
            protected readonly Func<object, EditorCurveBinding> dg_get_m_Binding;
            protected readonly MethodInfo mi_FindKeyAtTime;

            public UAnimationWindowCurve(Assembly asmUnityEditor)
            {
                CurveType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowCurve");

                Assert.IsNotNull(dg_get_m_Binding = EditorCommon.CreateGetFieldDelegate<EditorCurveBinding>(CurveType.GetField("m_Binding", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(mi_FindKeyAtTime = CurveType.GetMethod("FindKeyAtTime"));
            }

            public EditorCurveBinding GetBinding(object instance)
            {
                if (instance == null) return new EditorCurveBinding();
                return dg_get_m_Binding(instance);
            }
            public object FindKeyAtTime(object instance, object keyTime)
            {
                if (instance == null) return null;
                return mi_FindKeyAtTime.Invoke(instance, new object[] { keyTime });
            }
        }
        protected class UAnimationWindowSelection
        {
            private Action dg_ClearCache;

            public UAnimationWindowSelection(Assembly _)
            {
            }

            public void ClearCurvesCache(object instance)
            {
                if (instance == null) return;
                if (dg_ClearCache == null || dg_ClearCache.Target != instance)
                    dg_ClearCache = (Action)Delegate.CreateDelegate(typeof(Action), instance, instance.GetType().GetMethod("ClearCache"));
                dg_ClearCache();
            }
        }
        protected class UAnimationWindowSelectionItem
        {
            private Func<AnimationClip> dg_get_animationClip;
            private Func<IList> dg_get_curves;
            protected Action<object, IList> dg_set_m_CurvesCache;
            protected Func<object, IList> dg_get_m_CurvesCache;
            private Action dg_ClearCache;
            private Func<EditorCurveBinding, Type> dg_GetEditorCurveValueType;

            public IList swapDummyCurves;   //It is only used to temporarily replace the actual curve to be displayed.

            public UAnimationWindowSelectionItem(Assembly asmUnityEditor)
            {
                var animationWindowSelectionItemType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowSelectionItem");
                if (animationWindowSelectionItemType != null)
                {
                    dg_set_m_CurvesCache = EditorCommon.CreateSetFieldDelegate<IList>(animationWindowSelectionItemType.GetField("m_CurvesCache", BindingFlags.NonPublic | BindingFlags.Instance));
                    dg_get_m_CurvesCache = EditorCommon.CreateGetFieldDelegate<IList>(animationWindowSelectionItemType.GetField("m_CurvesCache", BindingFlags.NonPublic | BindingFlags.Instance));
                }
            }

            public AnimationClip GetAnimationClip(object instance)
            {
                if (instance == null) return null;
                if (dg_get_animationClip == null || dg_get_animationClip.Target != instance)
                    dg_get_animationClip = (Func<AnimationClip>)Delegate.CreateDelegate(typeof(Func<AnimationClip>), instance, instance.GetType().GetProperty("animationClip").GetGetMethod());
                return dg_get_animationClip();
            }

            public virtual IList GetCurves(object instance)
            {
                if (instance == null) return null;
                if (dg_get_curves == null || dg_get_curves.Target != instance)
                    dg_get_curves = (Func<IList>)Delegate.CreateDelegate(typeof(Func<IList>), instance, instance.GetType().GetProperty("curves").GetGetMethod());
                return dg_get_curves();
            }
            public virtual void SetCurvesCache(object instance, IList curves)
            {
                if (instance == null) return;
                dg_set_m_CurvesCache(instance, curves);
            }
            public virtual IList GetCurvesCache(object instance)
            {
                if (instance == null) return null;
                return dg_get_m_CurvesCache(instance);
            }
            public virtual void ClearCurvesCache(object instance)
            {
                if (instance == null) return;
                if (dg_ClearCache == null || dg_ClearCache.Target != instance)
                    dg_ClearCache = (Action)Delegate.CreateDelegate(typeof(Action), instance, instance.GetType().GetMethod("ClearCache"));
                dg_ClearCache();
            }
            public virtual Type GetEditorCurveValueType(object instance, EditorCurveBinding binding)
            {
                if (instance == null) return null;
                if (dg_GetEditorCurveValueType == null || dg_GetEditorCurveValueType.Target != instance)
                    dg_GetEditorCurveValueType = (Func<EditorCurveBinding, Type>)Delegate.CreateDelegate(typeof(Func<EditorCurveBinding, Type>), instance, instance.GetType().GetMethod("GetEditorCurveValueType"));
                return dg_GetEditorCurveValueType(binding);
            }
        }
        protected class UAnimationWindowHierarchyDataSource
        {
            private readonly MethodInfo mi_FindItem;
            private readonly MethodInfo mi_UpdateData;

            public UAnimationWindowHierarchyDataSource(Assembly asmUnityEditor)
            {
                var animationWindowHierarchyDataSourceType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowHierarchyDataSource");
                Assert.IsNotNull(mi_FindItem = animationWindowHierarchyDataSourceType.GetMethod("FindItem", BindingFlags.Public | BindingFlags.Instance));
                Assert.IsNotNull(mi_UpdateData = animationWindowHierarchyDataSourceType.GetMethod("UpdateData", BindingFlags.Public | BindingFlags.Instance));
            }

            public object FindItem(object instance, int id)
            {
                if (instance == null) return null;
                return mi_FindItem.Invoke(instance, new object[] { id });
            }

            public void UpdateData(object instance)
            {
                if (instance == null) return;
                mi_UpdateData.Invoke(instance, null);
            }
        }
        protected class UAnimationWindowHierarchyNode
        {
            private readonly Func<object, IList> dg_get_curves;

            public UAnimationWindowHierarchyNode(Assembly asmUnityEditor)
            {
                var animationWindowHierarchyNodeType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowHierarchyNode");
                Assert.IsNotNull(dg_get_curves = EditorCommon.CreateGetFieldDelegate<IList>(animationWindowHierarchyNodeType.GetField("curves", BindingFlags.Public | BindingFlags.Instance)));
            }

            public IList GetCurves(object instance)
            {
                if (instance == null) return null;
                return dg_get_curves(instance);
            }
        }
        protected class UDopeLine
        {
            private readonly PropertyInfo pi_curves;
            private readonly PropertyInfo pi_hierarchyNodeID;

            public UDopeLine(Assembly asmUnityEditor)
            {
                var dopeLineType = asmUnityEditor.GetType("UnityEditorInternal.DopeLine");
                Assert.IsNotNull(pi_curves = dopeLineType.GetProperty("curves"));
                Assert.IsNotNull(pi_hierarchyNodeID = dopeLineType.GetProperty("hierarchyNodeID"));
            }

            public IList GetCurves(object instance)
            {
                if (instance == null) return null;
                return (IList)pi_curves.GetValue(instance, null);
            }
            public int GetHierarchyNodeID(object instance)
            {
                if (instance == null) return -1;
                return (int)pi_hierarchyNodeID.GetValue(instance, null);
            }
        }

        protected UEditorWindow uEditorWindow;
        protected UAnimationWindowUtility uAnimationWindowUtility;
        protected UAnimEditor uAnimEditor;
        protected UCurveEditor uCurveEditor;
        protected UAnimationWindowState uAnimationWindowState;
        protected UAnimationWindowControl uAnimationWindowControl;
        protected UAnimationKeyTime uAnimationKeyTime;
        protected UAnimationWindowCurve uAnimationWindowCurve;
        protected UAnimationWindowSelection uAnimationWindowSelection;
        protected UAnimationWindowSelectionItem uAnimationWindowSelectionItem;
        protected UAnimationWindowHierarchyDataSource uAnimationWindowHierarchyDataSource;
        protected UAnimationWindowHierarchyNode uAnimationWindowHierarchyNode;
        protected UDopeLine uDopeLine;
        protected UAnimationMode uAnimationMode;
        protected UEditorGUIUtility uEditorGUIUtility;
#if VERYANIMATION_TIMELINE
        public UTimelineWindow UTimelineWindow { get; protected set; }
        protected object AnimationTimeWindowControlInstance
        {
            get
            {
                var awc = AnimationWindowControlInstance;
                if (awc != null && awc.GetType() == UTimelineWindow.TimelineWindowTimeControl.ControlType)
                    return awc;
                return null;
            }
        }
#endif

        protected object AnimEditorInstance
        {
            get
            {
                var aw = Instance;
                if (aw == null) return null;
                return dg_get_m_AnimEditor(aw);
            }
        }
        public object AnimationWindowStateInstance
        {
            get
            {
                return uAnimEditor.GetAnimationWindowState(AnimEditorInstance);
            }
        }
        protected object AnimationWindowControlInstance
        {
            get
            {
                return uAnimationWindowState.GetControlInterface(AnimationWindowStateInstance);
            }
        }

        protected object Selection
        {
            get
            {
                var ae = AnimEditorInstance;
                var si = uAnimEditor.GetSelection(ae);
                if (si == null)
                {
                    if (!HasFocus() && Instance != null)
                    {
                        Instance.Focus();
                    }
                    si = uAnimEditor.GetSelection(ae);
                    if (si == null)
                        return null;
                }
                return si;
            }
        }
        protected object SelectedItem
        {
            get
            {
                var ae = AnimEditorInstance;
                var si = uAnimEditor.GetSelectedItem(ae);
                if (si == null)
                {
                    if (!HasFocus() && Instance != null)
                    {
                        Instance.Focus();
                    }
                    si = uAnimEditor.GetSelectedItem(ae);
                    if (si == null)
                        return null;
                }
                return si;
            }
        }

        public UAnimationWindow()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var animationWindowType = asmUnityEditor.GetType("UnityEditor.AnimationWindow");

            Assert.IsNotNull(dg_get_s_AnimationWindows = EditorCommon.CreateGetFieldDelegate<IList>(animationWindowType.GetField("s_AnimationWindows", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_get_m_AnimEditor = EditorCommon.CreateGetFieldDelegate<object>(animationWindowType.GetField("m_AnimEditor", BindingFlags.NonPublic | BindingFlags.Instance)));
            Assert.IsNotNull(dg_get_m_LockTracker = EditorCommon.CreateGetFieldDelegate<object>(animationWindowType.GetField("m_LockTracker", BindingFlags.NonPublic | BindingFlags.Instance)));
            Assert.IsNotNull(mi_OnSelectionChange = animationWindowType.GetMethod("OnSelectionChange", BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNotNull(mi_EditSequencerClip = animationWindowType.GetMethod("EditSequencerClip", BindingFlags.NonPublic | BindingFlags.Instance));

            uEditorWindow = new UEditorWindow();
            uAnimationWindowUtility = new UAnimationWindowUtility();
            uAnimEditor = new UAnimEditor(asmUnityEditor);
            uCurveEditor = new UCurveEditor(asmUnityEditor);
            uAnimationWindowState = new UAnimationWindowState(asmUnityEditor);
            uAnimationWindowControl = new UAnimationWindowControl(asmUnityEditor);
            uAnimationKeyTime = new UAnimationKeyTime(asmUnityEditor);
            uAnimationWindowCurve = new UAnimationWindowCurve(asmUnityEditor);
            uAnimationWindowSelection = new UAnimationWindowSelection(asmUnityEditor);
            uAnimationWindowSelectionItem = new UAnimationWindowSelectionItem(asmUnityEditor);
            uAnimationWindowHierarchyDataSource = new UAnimationWindowHierarchyDataSource(asmUnityEditor);
            uAnimationWindowHierarchyNode = new UAnimationWindowHierarchyNode(asmUnityEditor);
            uDopeLine = new UDopeLine(asmUnityEditor);
            uAnimationMode = new UAnimationMode();
            uEditorGUIUtility = new UEditorGUIUtility();
#if VERYANIMATION_TIMELINE
            UTimelineWindow = new UTimelineWindow();
#endif
        }

        public EditorWindow Instance
        {
            get
            {
                EditorWindow result = null;
                {
                    var list = dg_get_s_AnimationWindows(null);
                    if (list.Count > 0)
                        result = list[0] as EditorWindow;
                }
                return result;
            }
        }

        public GameObject GetActiveRootGameObject()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
#if VERYANIMATION_TIMELINE
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var bindingObject = UTimelineWindow.TimelineWindowTimeControl.GetGenericBinding(atwc);
                    if (bindingObject != null)
                    {
                        if (bindingObject is GameObject)
                        {
                            return bindingObject as GameObject;
                        }
                        else if (bindingObject is Animator)
                        {
                            var animator = bindingObject as Animator;
                            return animator.gameObject;
                        }
                    }
                }
#endif
                return null;
            }
            else
            {
                return uAnimationWindowState.GetActiveRootGameObject(aws);
            }
        }
        public Component GetActiveAnimationPlayer()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
#if VERYANIMATION_TIMELINE
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var bindingObject = UTimelineWindow.TimelineWindowTimeControl.GetGenericBinding(atwc);
                    if (bindingObject != null)
                    {
                        if (bindingObject is GameObject)
                        {
                            var gameObject = bindingObject as GameObject;
                            return gameObject.GetComponent<Animator>();
                        }
                        else if (bindingObject is Animator)
                        {
                            return bindingObject as Animator;
                        }
                    }
                }
#endif
                return null;
            }
            else
            {
                return uAnimationWindowState.GetActiveAnimationPlayer(aws);
            }
        }

        public virtual AnimationClip GetSelectionAnimationClip()
        {
            if (Instance == null) return null;
            return uAnimationWindowState.GetActiveAnimationClip(AnimationWindowStateInstance);
        }
        public virtual void SetSelectionAnimationClip(AnimationClip animationClip)
        {
            if (Instance == null) return;
            if (GetSelectionAnimationClip() == animationClip) return;

            var aws = AnimationWindowStateInstance;
            bool playing = uAnimationWindowState.GetPlaying(aws);
            float currentTime = uAnimationWindowState.GetCurrentTime(aws);
            {
                uAnimationWindowState.SetActiveAnimationClip(aws, animationClip);
            }
            uAnimationWindowState.SetCurrentTime(aws, currentTime);
            if (playing)
                uAnimationWindowState.StartPlayback(aws);

            ForceRefresh();
        }

        public void CleanAnimationModeEvents()
        {
            //Added to infer that there may be an error due to remaining actions for deleted windows.
            {
                var onStart = uAnimationMode.GetOnAnimationRecordingStart();
                if (onStart != null)
                {
                    bool changed = false;
                    {
                        var delegates = onStart.GetInvocationList();
                        foreach (var del in delegates)
                        {
                            if (del.Target == null)
                            {
                                onStart -= (Action)del;
                                changed = true;
                            }
                            else
                            {
                                var type = del.Target.GetType();
                                var fi = type.GetField("m_Panel", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (fi != null)
                                {
                                    var panel = fi.GetValue(del.Target);
                                    if (panel == null)
                                    {
                                        onStart -= (Action)del;
                                        changed = true;
                                    }
                                    else if (panel.GetType().GetProperty("visualTree").GetValue(panel) == null)
                                    {
                                        onStart -= (Action)del;
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                    if (changed)
                        uAnimationMode.SetOnAnimationRecordingStart(onStart);
                }
            }
            {
                var onStop = uAnimationMode.GetOnAnimationRecordingStop();
                if (onStop != null)
                {
                    bool changed = false;
                    {
                        var delegates = onStop.GetInvocationList();
                        foreach (var del in delegates)
                        {
                            if (del.Target == null)
                            {
                                onStop -= (Action)del;
                                changed = true;
                            }
                            else
                            {
                                var type = del.Target.GetType();
                                var fi = type.GetField("m_Panel", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (fi != null)
                                {
                                    var panel = fi.GetValue(del.Target);
                                    if (panel == null)
                                    {
                                        onStop -= (Action)del;
                                        changed = true;
                                    }
                                    else if (panel.GetType().GetProperty("visualTree").GetValue(panel) == null)
                                    {
                                        onStop -= (Action)del;
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                    if (changed)
                        uAnimationMode.SetOnAnimationRecordingStop(onStop);
                }
            }
        }

        public void StopAllRecording()
        {
            var list = dg_get_s_AnimationWindows(null);
            foreach (var aw in list)
            {
                if (aw == null) continue;

                var ae = dg_get_m_AnimEditor(aw);
                if (ae == null) continue;

                var aws = uAnimEditor.GetAnimationWindowState(ae);
                if (aws == null) continue;

                if (uAnimationWindowState.GetRecording(aws))
                {
                    uAnimationWindowState.StopRecording(aws);
                }
                else if (uAnimationWindowState.GetPreviewing(aws))
                {
                    uAnimationWindowState.StopPreview(aws);
                }
            }
        }
        public void StopRecording()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetRecording(aws))
            {
                uAnimationWindowState.StopRecording(aws);
            }
            else if (uAnimationWindowState.GetPreviewing(aws))
            {
                uAnimationWindowState.StopPreview(aws);
            }
        }
        public bool StartRecording()
        {
            var aws = AnimationWindowStateInstance;
            if (GetCanRecord())
            {
                if (!uAnimationWindowState.GetRecording(aws))
                {
                    if (!uAnimationWindowState.StartRecording(aws))
                        return false;
                }
            }
            else if (GetCanPreview())
            {
                if (!uAnimationWindowState.GetPreviewing(aws))
                {
                    if (!uAnimationWindowState.StartPreview(aws))
                        return false;
                }
            }
            return true;
        }
        public bool GetCanRecord()
        {
            return uAnimationWindowControl.GetCanRecord(AnimationWindowControlInstance);
        }
        public bool GetRecording()
        {
            return uAnimationWindowState.GetRecording(AnimationWindowStateInstance);
        }
        public void StartPreviewing()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetPreviewing(aws))
                return;
            uAnimationWindowState.StartPreview(aws);
        }
        public void StopPreviewing()
        {
            var aws = AnimationWindowStateInstance;
            if (!uAnimationWindowState.GetPreviewing(aws))
                return;
            uAnimationWindowState.StopPreview(aws);
        }
        public bool GetCanPreview()
        {
            return uAnimationWindowState.GetCanPreview(AnimationWindowStateInstance);
        }
        public bool GetPreviewing()
        {
            return uAnimationWindowState.GetPreviewing(AnimationWindowStateInstance);
        }

        public void PlayingChange()
        {
            var aws = AnimationWindowStateInstance;
            if (!HasFocus())
                Instance.Focus();
            var playing = uAnimationWindowState.GetPlaying(aws);
            playing = !playing;
            if (playing)
                uAnimationWindowState.StartPlayback(aws);
            else
                uAnimationWindowState.StopPlayback(aws);
        }
        public bool GetPlaying()
        {
            return uAnimationWindowState.GetPlaying(AnimationWindowStateInstance);
        }

        public int GetCurrentFrame()
        {
            return uAnimationWindowState.GetCurrentFrame(AnimationWindowStateInstance);
        }
        public void SetCurrentFrame(int frame)
        {
            uAnimationWindowState.SetCurrentFrame(AnimationWindowStateInstance, frame);
            Repaint();
        }
        public void MoveFrame(int add)
        {
            var clip = GetSelectionAnimationClip();
            var time = SnapToFrame(GetCurrentTime(), clip.frameRate);
            var addTime = GetFrameTime(add, clip);
            SetCurrentTime(time + addTime);
        }
        public int GetLastFrame(AnimationClip clip)
        {
            return Mathf.RoundToInt(clip.length * clip.frameRate);
        }
        public float GetFrameTime(int frame, AnimationClip clip)
        {
            return SnapToFrame(frame * (1f / clip.frameRate), clip.frameRate);
        }

        public float GetCurrentTime()
        {
            return uAnimationWindowState.GetCurrentTime(AnimationWindowStateInstance);
        }
        public void SetCurrentTime(float time)
        {
            time = SnapToFrame(time, GetSelectionAnimationClip().frameRate);
            uAnimationWindowState.SetCurrentTime(AnimationWindowStateInstance, time);
            Repaint();
        }

        public float SnapToFrame(float time, float fps)
        {
            return uAnimationWindowState.SnapToFrame(AnimationWindowStateInstance, time, fps);
        }
        public int TimeToFrameRound(float time)
        {
            return uAnimationWindowState.TimeToFrameRound(AnimationWindowStateInstance, time);
        }

        public void MoveToNextFrame()
        {
            MoveFrame(1);
        }
        public void MoveToPrevFrame()
        {
            MoveFrame(-1);
        }
        public virtual void MoveToNextKeyframe()
        {
            uAnimationWindowControl.GoToNextKeyframe(AnimationWindowControlInstance);
            Repaint();
        }
        public virtual void MoveToPreviousKeyframe()
        {
            uAnimationWindowControl.GoToPreviousKeyframe(AnimationWindowControlInstance);
            Repaint();
        }
        public virtual void MoveToFirstKeyframe()
        {
            uAnimationWindowControl.GoToFirstKeyframe(AnimationWindowControlInstance);
            Repaint();
        }
        public virtual void MoveToLastKeyframe()
        {
            uAnimationWindowControl.GoToLastKeyframe(AnimationWindowControlInstance);
            Repaint();
        }

        public void SwitchBetweenCurvesAndDopesheet()
        {
            var ae = AnimEditorInstance;
            uAnimEditor.SwitchBetweenCurvesAndDopesheet(ae);
            if (VAW.EditorSettings.SettingAutorunFrameAll)
            {
                uAnimEditor.SetTriggerFraming(ae);
            }
            Repaint();
        }
        public bool IsShowCurveEditor()
        {
            return uAnimationWindowState.GetShowCurveEditor(AnimationWindowStateInstance);
        }

        public void ClearKeySelections()
        {
            var ae = AnimEditorInstance;
            var aws = AnimationWindowStateInstance;
            if (ae == null || aws == null)
                return;
            if (IsShowCurveEditor())
            {
                var curveEditor = uAnimEditor.GetCurveEditor(ae);
                if (curveEditor != null)
                {
                    if (uCurveEditor.HasSelection(curveEditor))
                    {
                        uCurveEditor.ClearSelection(curveEditor);
                        Repaint();
                    }
                }
            }
            else
            {
                var list = uAnimationWindowState.GetSelectedKeyHashes(aws);
                if (list != null)
                {
                    var e = list.GetEnumerator();
                    if (e.MoveNext())
                    {
                        uAnimationWindowState.ClearKeySelections(aws);
                        Repaint();
                    }
                }
            }
        }

        public virtual void PropertySortOrFilterByBindings(List<EditorCurveBinding> bindings)
        {
            var aws = AnimationWindowStateInstance;
            var sl = Selection;
            var si = SelectedItem;
            if (aws == null || sl == null || si == null)
                return;
            var hierarchyData = uAnimationWindowState.GetHierarchyData(aws);
            if (hierarchyData == null)
                return;

            uAnimationWindowState.ClearCache(aws);
            if (bindings != null && bindings.Count > 0)
            {
                var selectionItemCurves = uAnimationWindowSelectionItem.GetCurves(si);
                uAnimationWindowSelectionItem.swapDummyCurves = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(uAnimationWindowCurve.CurveType));
                {
                    foreach (var curve in selectionItemCurves)
                    {
                        var binding = uAnimationWindowCurve.GetBinding(curve);
                        if (!bindings.Contains(binding))
                            continue;
                        uAnimationWindowSelectionItem.swapDummyCurves.Add(curve);
                    }
                }
                uAnimationWindowSelectionItem.SetCurvesCache(si, uAnimationWindowSelectionItem.swapDummyCurves);
                {
                    if (sl != si)   //Unity 2017.4 or earlier
                        uAnimationWindowSelection.ClearCurvesCache(sl);
                    uAnimationWindowHierarchyDataSource.UpdateData(hierarchyData);
                }
                uAnimationWindowSelectionItem.SetCurvesCache(si, selectionItemCurves);
            }
            else
            {
                uAnimationWindowSelectionItem.swapDummyCurves = null;
                uAnimationWindowHierarchyDataSource.UpdateData(hierarchyData);
            }

            Repaint();
        }
        public bool IsSelectedItemCurvesDummySwapped => uAnimationWindowSelectionItem.swapDummyCurves != null;
        public bool ContainsSelectedItemCurvesDummySwapped(EditorCurveBinding binding)
        {
            if (uAnimationWindowSelectionItem.swapDummyCurves == null)
                return false;
            foreach (var curve in uAnimationWindowSelectionItem.swapDummyCurves)
            {
                var cbinding = uAnimationWindowCurve.GetBinding(curve);
                if (cbinding == binding)
                    return true;
                //RawQuaternions are displayed as NonBaked in the AnimationWindow, so special care is required.
                if (cbinding.type == typeof(Transform) && binding.type == typeof(Transform) && cbinding.path == binding.path &&
                    cbinding.propertyName.StartsWith(URotationCurveInterpolation.PrefixForInterpolation[(int)URotationCurveInterpolation.Mode.NonBaked]) &&
                    binding.propertyName.StartsWith(URotationCurveInterpolation.PrefixForInterpolation[(int)URotationCurveInterpolation.Mode.RawQuaternions]))
                    return true;
            }
            return false;
        }
        public void ClearSelectedItemCurvesDummySwapped()
        {
            if (uAnimationWindowSelectionItem.swapDummyCurves == null)
                return;
            uAnimationWindowSelectionItem.swapDummyCurves = null;
            ForceRefresh();
        }

        public void SynchroCurveSelection(List<EditorCurveBinding> bindings)
        {
            var ae = AnimEditorInstance;
            var aws = AnimationWindowStateInstance;
            if (ae == null || aws == null)
                return;

            uAnimationWindowState.ClearKeySelections(aws);
            uAnimationWindowState.ClearHierarchySelection(aws);
            uAnimationWindowState.ClearLastAddedCurveBinding(aws);

            if (bindings.Count > 0)
            {
                var animationKeyTime = uAnimationKeyTime.Time(GetCurrentTime(), GetSelectionAnimationClip().frameRate);
                foreach (object dopeline in uAnimationWindowState.GetDopelines(aws))
                {
                    foreach (var curve in uDopeLine.GetCurves(dopeline))
                    {
                        var cbinding = uAnimationWindowCurve.GetBinding(curve);
                        foreach (var binding in bindings)
                        {
                            if (binding == cbinding)
                            {
                                uAnimationWindowState.SelectHierarchyItem(aws, uDopeLine.GetHierarchyNodeID(dopeline), true, false);
                                var keyframe = uAnimationWindowCurve.FindKeyAtTime(curve, animationKeyTime);
                                if (keyframe != null)
                                    uAnimationWindowState.SelectKey(aws, keyframe);
                                break;
                            }
                        }
                    }
                }
                if (IsShowCurveEditor())
                {
                    uAnimEditor.UpdateSelectedKeysToCurveEditor(ae);
                }
            }

            if (VAW.EditorSettings.SettingAutorunFrameAll)
            {
                uAnimEditor.SetTriggerFraming(ae);
            }

            Repaint();
        }

        public List<EditorCurveBinding> GetCurveSelection()
        {
            var list = new List<EditorCurveBinding>();
            var activeCurves = uAnimationWindowState.GetActiveCurves(AnimationWindowStateInstance);
            if (activeCurves == null)
                return list;
            foreach (var curve in activeCurves)
            {
                var cbinding = uAnimationWindowCurve.GetBinding(curve);
                list.Add(cbinding);
            }
            return list;
        }

        public List<EditorCurveBinding> GetMissingCurveBindings()
        {
            var aws = AnimationWindowStateInstance;
            List<EditorCurveBinding> list = new();
            var hierarchyData = uAnimationWindowState.GetHierarchyData(aws);
            foreach (object dopeline in uAnimationWindowState.GetDopelines(aws))
            {
                var hierarchyNodeID = uDopeLine.GetHierarchyNodeID(dopeline);
                var windowHierarchyNode = uAnimationWindowHierarchyDataSource.FindItem(hierarchyData, hierarchyNodeID);
                if (windowHierarchyNode == null) continue;
                if (uAnimationWindowUtility.IsNodeLeftOverCurve(aws, windowHierarchyNode))
                {
                    var curves = uAnimationWindowHierarchyNode.GetCurves(windowHierarchyNode);
                    if (curves == null) continue;
                    foreach (var curve in curves)
                    {
                        if (curve == null) continue;
                        var binding = uAnimationWindowCurve.GetBinding(curve);
                        list.Add(binding);
                    }
                }
            }
            return list;
        }

        public void GetNearKeyframeTimes(float[] nextTimes, float[] prevTimes)
        {
            var aws = AnimationWindowStateInstance;
            Array curves;
            {
                var list = uAnimationWindowState.GetAllCurves(aws);
                curves = Array.CreateInstance(uAnimationWindowCurve.CurveType, list.Count);
                list.CopyTo(curves, 0);
            }
            var frameRate = GetSelectionAnimationClip().frameRate;
            if (nextTimes != null)
            {
                var time = GetCurrentTime();
                for (int i = 0; i < nextTimes.Length; i++)
                {
                    nextTimes[i] = uAnimationWindowUtility.GetNextKeyframeTime(curves, time, frameRate);
                    if (time != nextTimes[i])
                        time = nextTimes[i];
                    else
                        nextTimes[i] = -1f;
                }
            }
            if (prevTimes != null)
            {
                var time = GetCurrentTime();
                for (int i = 0; i < prevTimes.Length; i++)
                {
                    prevTimes[i] = uAnimationWindowUtility.GetPreviousKeyframeTime(curves, time, frameRate);
                    if (time != prevTimes[i])
                        time = prevTimes[i];
                    else
                        prevTimes[i] = -1f;
                }
            }
        }

        public bool IsDoneRefresh()
        {
            var refresh = uAnimationWindowState.GetRefresh(AnimationWindowStateInstance);
            return refresh == UAnimationWindowState.RefreshType.None;
        }
        public void ForceRefresh()
        {
            uAnimationWindowState.ForceRefresh(AnimationWindowStateInstance);
            Repaint();
        }

        public void ResampleAnimation()
        {
            uAnimationWindowControl.ResampleAnimation(AnimationWindowControlInstance);
        }

        public void Repaint()
        {
            if (!HasFocus())
                return;

            var list = dg_get_s_AnimationWindows(null);
            if (list.Count > 0)
            {
                (list[0] as EditorWindow).Repaint();
                #region OtherAnimationWindows
                if (list.Count > 1)
                {
                    var clip = GetSelectionAnimationClip();
                    for (int i = 1; i < list.Count; i++)
                    {
                        var ew = list[i] as EditorWindow;
                        if (uEditorWindow.HasFocus(ew))
                        {
                            var ae = dg_get_m_AnimEditor(ew);
                            var si = uAnimEditor.GetSelectedItem(ae);
                            if (si != null)
                            {
                                var sclip = uAnimationWindowSelectionItem.GetAnimationClip(si);
                                if (clip == sclip)
                                {
                                    var aws = uAnimEditor.GetAnimationWindowState(ae);
                                    uAnimationWindowState.ForceRefresh(aws);
                                    ew.Repaint();
                                }
                            }
                        }
                    }
                }
                #endregion
            }
        }

        public bool HasFocus()
        {
            return uEditorWindow.HasFocus(Instance);
        }

        public void Close()
        {
            if (Instance == null)
                return;
            Instance.Close();
        }

        public virtual bool GetLock(EditorWindow aw)
        {
            return uEditorGUIUtility.uEditorLockTracker.GetLock(dg_get_m_LockTracker(aw));
        }
        public virtual void SetLock(EditorWindow aw, bool flag)
        {
            uEditorGUIUtility.uEditorLockTracker.SetLock(dg_get_m_LockTracker(aw), flag);
        }

        public virtual bool GetFilterBySelection()
        {
            return uAnimationWindowState.GetFilterBySelection(AnimationWindowStateInstance);
        }
        public virtual void SetFilterBySelection(bool enable)
        {
            uAnimationWindowState.SetFilterBySelection(AnimationWindowStateInstance, enable);
        }

        public void OnSelectionChange()
        {
            if (Instance == null) return;
            mi_OnSelectionChange.Invoke(Instance, null);
        }

        public bool GetRemoveStartOffset()
        {
#if VERYANIMATION_TIMELINE
            if (GetLinkedWithTimeline())
                return GetTimelineAnimationRemoveStartOffset();
#endif
            return false;
        }

        public PlayableGraph GetPlayableGraph()
        {
            return uAnimationWindowControl.GetGraph(AnimationWindowControlInstance);
        }
        public void DestroyPlayableGraph()
        {
            uAnimationWindowControl.DestroyGraph(AnimationWindowControlInstance);
        }
        public AnimationClipPlayable GetClipPlayable()
        {
            return uAnimationWindowControl.GetClipPlayable(AnimationWindowControlInstance);
        }
        public AnimationClipPlayable GetCandidateClipPlayable()
        {
            return uAnimationWindowControl.GetCandidateClipPlayable(AnimationWindowControlInstance);
        }
        public AnimationClipPlayable GetDefaultPosePlayable()
        {
            return uAnimationWindowControl.GetDefaultPosePlayable(AnimationWindowControlInstance);
        }
        public AnimationLayerMixerPlayable GetLayerMixerPlayable()
        {
            var playable = GetClipPlayable().GetOutput(0);
            if (playable.IsValid())
            {
                while (playable.GetPlayableType() != typeof(AnimationLayerMixerPlayable))
                    playable = playable.GetOutput(0);
                return (AnimationLayerMixerPlayable)playable;
            }
            else
            {
                return AnimationLayerMixerPlayable.Null;
            }
        }

        public bool GetLinkedWithTimeline()
        {
#if VERYANIMATION_TIMELINE
            return uAnimationWindowState.GetLinkedWithSequencer(AnimationWindowStateInstance);
#else
            return false;
#endif
        }
#if VERYANIMATION_TIMELINE
        public bool GetTimelineTrackAssetEditable()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var trackAsset = UTimelineWindow.TimelineWindowTimeControl.GetTrackAsset(atwc);
                    if (trackAsset != null && !trackAsset.muted)
                    {
                        var locked = UTimelineWindow.TrackAsset.GetLocked(trackAsset);
                        if (!locked)
                            return true;
                    }
                }
            }
            return false;
        }
        public bool GetTimelineHasFocus()
        {
            return uEditorWindow.HasFocus(UTimelineWindow.Instance);
        }

        public bool GetTimelineRecording()
        {
            return UTimelineWindow.GetRecording();
        }
        public void SetTimelineRecording(bool enable)
        {
            UTimelineWindow.SetRecording(enable);
        }

        public bool GetTimelinePreviewMode()
        {
            return UTimelineWindow.GetPreviewMode();
        }
        public void SetTimelinePreviewMode(bool enable)
        {
            UTimelineWindow.SetPreviewMode(enable);
        }

        public void SetTimelinePlaying(bool enable)
        {
            UTimelineWindow.SetPlaying(enable);
        }

        public AnimationClip GetTimelineAnimationClip()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    return UTimelineWindow.TimelineWindowTimeControl.GetAnimationClip(atwc);
                }
            }
            return null;
        }
        public void SetTimelineAnimationClip(AnimationClip clip, string undoName = null)
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    UTimelineWindow.TimelineWindowTimeControl.SetAnimationClip(atwc, clip, undoName);
                }
            }
        }

        public void GetTimelineAnimationTrackInfo(out bool animatesRootTransform, out bool requiresMotionXPlayable, out bool usesAbsoluteMotion)
        {
            animatesRootTransform = false;
            requiresMotionXPlayable = false;
            usesAbsoluteMotion = false;

            var animtionTrack = GetTimelineAnimationTrack(true);
            if (animtionTrack == null)
                return;
            var go = GetActiveRootGameObject();

            animatesRootTransform = UTimelineWindow.AnimationTrack.AnimatesRootTransform(animtionTrack);
            var mode = UTimelineWindow.AnimationTrack.GetOffsetMode(animtionTrack, go, animatesRootTransform);
            requiresMotionXPlayable = UTimelineWindow.AnimationTrack.RequiresMotionXPlayable(animtionTrack, mode, go);
            usesAbsoluteMotion = UTimelineWindow.AnimationTrack.UsesAbsoluteMotion(animtionTrack, mode);
        }
        public bool GetTimelineRootMotionOffsets(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            var animtionTrack = GetTimelineAnimationTrack(true);

            //Track Offsets
            {
                if (animtionTrack == null)
                    return false;

                var hasRootTransforms = UTimelineWindow.AnimationTrack.AnimatesRootTransform(animtionTrack);
                if (!hasRootTransforms)
                    return false;

                if (animtionTrack.trackOffset == TrackOffset.Auto || animtionTrack.trackOffset == TrackOffset.ApplyTransformOffsets)
                {
                    position = animtionTrack.position;
                    rotation = animtionTrack.rotation;
                }
                else if (animtionTrack.trackOffset == TrackOffset.ApplySceneOffsets)
                {
                    position = UTimelineWindow.AnimationTrack.GetSceneOffsetPosition(animtionTrack);
                    rotation = UTimelineWindow.AnimationTrack.GetSceneOffsetRotation(animtionTrack);
                }
            }
            //Clip Offsets
            {
                var animationPlayableAsset = GetTimelineAnimationPlayableAsset();
                if (animationPlayableAsset != null)
                {
                    position += rotation * animationPlayableAsset.position;
                    rotation *= animationPlayableAsset.rotation;
                }
                else
                {
                    position += rotation * animtionTrack.infiniteClipOffsetPosition;
                    rotation *= animtionTrack.infiniteClipOffsetRotation;
                }
            }

            return true;
        }

        public int GetTimelineFrame()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var state = UTimelineWindow.TimelineWindowTimeControl.GetTimelineState(atwc);
                    return UTimelineWindow.TimelineState.GetFrame(state);
                }
            }
            return 0;
        }
        public void SetTimelineFrame(int frame)
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var state = UTimelineWindow.TimelineWindowTimeControl.GetTimelineState(atwc);
                    UTimelineWindow.TimelineState.SetFrame(state, frame);
                }
            }
        }

        public float GetTimelineFrameRate()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var state = UTimelineWindow.TimelineWindowTimeControl.GetTimelineState(atwc);
                    return UTimelineWindow.TimelineState.GetFrameRate(state);
                }
            }
            return 0f;
        }

        public bool IsTimelineArmedForRecord()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var awc = AnimationWindowControlInstance;
                if (awc != null && awc.GetType() == UTimelineWindow.TimelineWindowTimeControl.ControlType)
                {
                    return UTimelineWindow.TimelineWindowTimeControl.IsArmedForRecord(awc);
                }
            }
            return false;
        }

        public bool EditSequencerClip(TimelineClip timelineClip)
        {
            var sourceObject = GetActiveRootGameObject();
            object controlInterface = UTimelineWindow.TimelineAnimationUtilities.CreateTimeController(UTimelineWindow.State, timelineClip);
#pragma warning disable IDE0029
            return (bool)mi_EditSequencerClip.Invoke(Instance, new object[] { timelineClip.animationClip != null ? timelineClip.animationClip : timelineClip.curves, sourceObject, controlInterface });
#pragma warning restore IDE0029
        }

        public PlayableDirector GetTimelineCurrentDirector()
        {
            return UTimelineWindow.GetCurrentDirector();
        }

        public AnimationTrack GetTimelineAnimationTrack(bool top = false)
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    var animtionTrack = UTimelineWindow.TimelineWindowTimeControl.GetTrackAsset(atwc) as AnimationTrack;
                    if (animtionTrack != null && top)
                    {
                        while (animtionTrack.parent is AnimationTrack)
                        {
                            var track = animtionTrack.parent as AnimationTrack;
                            if (track == null)
                                break;
                            animtionTrack = track;
                        }
                    }
                    return animtionTrack;
                }
            }
            return null;
        }
        public TimelineClip GetTimelineClip()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    return UTimelineWindow.TimelineWindowTimeControl.GetTimelineClip(atwc);
                }
            }
            return null;
        }
        public AnimationPlayableAsset GetTimelineAnimationPlayableAsset()
        {
            var aws = AnimationWindowStateInstance;
            if (uAnimationWindowState.GetLinkedWithSequencer(aws))
            {
                var atwc = AnimationTimeWindowControlInstance;
                if (atwc != null)
                {
                    return UTimelineWindow.TimelineWindowTimeControl.GetPlayableAsset(atwc) as AnimationPlayableAsset;
                }
            }
            return null;
        }
        public bool GetTimelineAnimationPlayableAssetHasRootTransforms()
        {
            var animationPlayableAsset = GetTimelineAnimationPlayableAsset();
            if (animationPlayableAsset == null)
                return false;
            return UTimelineWindow.AnimationPlayableAsset.GetHasRootTransforms(animationPlayableAsset);
        }
        public virtual bool GetTimelineAnimationRemoveStartOffset()
        {
            var animationPlayableAsset = GetTimelineAnimationPlayableAsset();
            if (animationPlayableAsset != null)
                return animationPlayableAsset.removeStartOffset;
            else
                return false;
        }
        public virtual bool GetTimelineAnimationApplyFootIK()
        {
            var animationPlayableAsset = GetTimelineAnimationPlayableAsset();
            if (animationPlayableAsset != null)
                return animationPlayableAsset.applyFootIK;
            else
                return true;
        }
#endif
    }
}
