using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;

#if VERYANIMATION_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif

namespace VeryAnimation
{
#if VERYANIMATION_TIMELINE
    internal class UTimelineWindow
    {
        private readonly Func<EditorWindow> dg_get_instance;
        private Func<object> dg_get_state;
        private Func<object> dg_get_treeView;
        protected Func<object, object> dg_get_m_LockTracker;

        public UTimelineWindowTimeControl TimelineWindowTimeControl { get; protected set; }
        public UTimelineState TimelineState { get; protected set; }
        public UTrackAsset TrackAsset { get; protected set; }
        public UAnimationTrack AnimationTrack { get; protected set; }
        public UAnimationPlayableAsset AnimationPlayableAsset { get; protected set; }
        public UTimelineTreeViewGUI TimelineTreeViewGUI { get; protected set; }
        public UTimelineAnimationUtilities TimelineAnimationUtilities { get; protected set; }
        public UEditorGUIUtility EditorGUIUtility { get; protected set; }

        public UTimelineWindow()
        {
            GetTimelineAssembly(out Assembly asmTimelineEditor, out Assembly asmTimelineEngine);

            var timelineWindowType = asmTimelineEditor.GetType("UnityEditor.Timeline.TimelineWindow");
            Assert.IsNotNull(dg_get_instance = (Func<EditorWindow>)Delegate.CreateDelegate(typeof(Func<EditorWindow>), null, timelineWindowType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetGetMethod()));
            Assert.IsNotNull(dg_get_m_LockTracker = EditorCommon.CreateGetFieldDelegate<object>(timelineWindowType.GetField("m_LockTracker", BindingFlags.NonPublic | BindingFlags.Instance)));

            TimelineWindowTimeControl = new UTimelineWindowTimeControl(asmTimelineEditor, asmTimelineEngine);
            TimelineState = new UTimelineState();
            TrackAsset = new UTrackAsset();
            AnimationTrack = new UAnimationTrack();
            AnimationPlayableAsset = new UAnimationPlayableAsset();
            TimelineTreeViewGUI = new UTimelineTreeViewGUI();
            TimelineAnimationUtilities = new UTimelineAnimationUtilities(asmTimelineEditor, asmTimelineEngine);
            EditorGUIUtility = new UEditorGUIUtility();
        }

        public class UTimelineState //UWindowState
        {
            public UISequenceState ISequenceState { get; private set; }

            //protected Func<PlayableDirector> dg_get_currentDirector;
            protected Func<bool> dg_get_recording;
            protected Action<bool> dg_set_recording;
            protected Func<bool> dg_get_previewMode;
            protected Action<bool> dg_set_previewMode;
            protected Action<bool> dg_set_rebuildGraph;
            protected Func<TrackAsset, bool> dg_get_IsArmedForRecord;
            protected Action<bool> dg_SetPlaying;
            protected Action dg_EvaluateImmediate;
            protected Action dg_Refresh;

            private Func<object> dg_get_editSequence;

            public UTimelineState()
            {
                ISequenceState = new UISequenceState();
            }

            public object GetEditSequence(object instance)
            {
                if (instance == null) return null;
                if (dg_get_editSequence == null || dg_get_editSequence.Target != instance)
                    dg_get_editSequence = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, instance.GetType().GetProperty("editSequence").GetGetMethod());
                return dg_get_editSequence();
            }

            public virtual PlayableDirector GetCurrentDirector(object instance)
            {
                if (instance == null) return null;
                return ISequenceState.GetDirector(GetEditSequence(instance));
            }

            public bool GetRecording(object instance)
            {
                if (instance == null) return false;
                if (dg_get_recording == null || dg_get_recording.Target != instance)
                    dg_get_recording = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("recording").GetGetMethod());
                return dg_get_recording();
            }
            public void SetRecording(object instance, bool enable)
            {
                if (instance == null) return;
                if (dg_set_recording == null || dg_set_recording.Target != instance)
                    dg_set_recording = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), instance, instance.GetType().GetProperty("recording").GetSetMethod());
                try
                {
                    dg_set_recording(enable);
                }
                catch
                {
                }
            }

            public bool GetPreviewMode(object instance)
            {
                if (instance == null) return false;
                if (dg_get_previewMode == null || dg_get_previewMode.Target != instance)
                    dg_get_previewMode = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("previewMode").GetGetMethod());
                return dg_get_previewMode();
            }
            public virtual void SetPreviewMode(object instance, bool enable)
            {
                if (instance == null) return;
                if (dg_set_previewMode == null || dg_set_previewMode.Target != instance)
                    dg_set_previewMode = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), instance, instance.GetType().GetProperty("previewMode").GetSetMethod());
                dg_set_previewMode(enable);
                if (!enable)
                {
                    var mi = instance.GetType().GetMethod("SetPlaying");
                    mi.Invoke(instance, new object[] { false });
                }
                else
                {
                    if (dg_set_rebuildGraph == null || dg_set_rebuildGraph.Target != instance)
                        dg_set_rebuildGraph = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), instance, instance.GetType().GetProperty("rebuildGraph").GetSetMethod());
                    dg_set_rebuildGraph(true);
                }
            }

            public void SetPlaying(object instance, bool enable)
            {
                if (instance == null) return;
                if (dg_SetPlaying == null || dg_SetPlaying.Target != instance)
                    dg_SetPlaying = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), instance, instance.GetType().GetMethod("SetPlaying"));
                dg_SetPlaying(enable);
            }

            public void EvaluateImmediate(object instance)
            {
                if (instance == null) return;
                if (dg_EvaluateImmediate == null || dg_EvaluateImmediate.Target != instance)
                    dg_EvaluateImmediate = (Action)Delegate.CreateDelegate(typeof(Action), instance, instance.GetType().GetMethod("EvaluateImmediate"));
                dg_EvaluateImmediate();
            }
            public void Refresh(object instance)
            {
                if (instance == null) return;
                if (dg_Refresh == null || dg_Refresh.Target != instance)
                    dg_Refresh = (Action)Delegate.CreateDelegate(typeof(Action), instance, instance.GetType().GetMethod("Refresh"));
                dg_Refresh();
            }

            public virtual int GetFrame(object instance)
            {
                if (instance == null) return 0;
                return ISequenceState.GetFrame(GetEditSequence(instance));
            }
            public virtual void SetFrame(object instance, int frame)
            {
                if (instance == null) return;
                ISequenceState.SetFrame(GetEditSequence(instance), frame);
            }

            public virtual float GetFrameRate(object instance)
            {
                if (instance == null) return 0f;
                return ISequenceState.GetFrameRate(GetEditSequence(instance));
            }

            public bool IsArmedForRecord(object instance, TrackAsset track)
            {
                if (instance == null) return false;
                if (dg_get_IsArmedForRecord == null || dg_get_IsArmedForRecord.Target != instance)
                    dg_get_IsArmedForRecord = (Func<TrackAsset, bool>)Delegate.CreateDelegate(typeof(Func<TrackAsset, bool>), instance, instance.GetType().GetMethod("IsArmedForRecord"));
                return dg_get_IsArmedForRecord(track);
            }
        }
        public class UTimelineWindowTimeControl
        {
            public Type ControlType { get; protected set; }

            protected Func<object, TimelineClip> dg_get_m_Clip;
            protected Func<TrackAsset> dg_get_track;
            protected Func<object> dg_get_state;

            protected UTimelineState uTimelineState;

            public UTimelineWindowTimeControl(Assembly asmTimelineEditor, Assembly _)
            {
                ControlType = asmTimelineEditor.GetType("UnityEditor.Timeline.TimelineWindowTimeControl");
                Assert.IsNotNull(dg_get_m_Clip = EditorCommon.CreateGetFieldDelegate<TimelineClip>(ControlType.GetField("m_Clip", BindingFlags.NonPublic | BindingFlags.Instance)));

                uTimelineState = new UTimelineState();
            }

            public virtual TrackAsset GetTrackAsset(object instance)
            {
                if (instance == null) return null;
                if (dg_get_track == null || dg_get_track.Target != instance)
                    dg_get_track = (Func<TrackAsset>)Delegate.CreateDelegate(typeof(Func<TrackAsset>), instance, instance.GetType().GetProperty("track", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
                return dg_get_track();
            }
            public virtual object GetTimelineState(object instance)
            {
                if (instance == null) return null;
                if (dg_get_state == null || dg_get_state.Target != instance)
                    dg_get_state = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), null, instance.GetType().GetProperty("state", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true));
                return dg_get_state();
            }

            public TimelineClip GetTimelineClip(object instance)
            {
                if (instance == null) return null;
                return dg_get_m_Clip(instance);
            }
            public PlayableAsset GetPlayableAsset(object instance)
            {
                if (instance == null) return null;
                var clip = dg_get_m_Clip(instance);
                if (clip == null) return null;
                return clip.asset as PlayableAsset;
            }
            public UnityEngine.Object GetGenericBinding(object instance)
            {
                var currentDirector = uTimelineState.GetCurrentDirector(GetTimelineState(instance));
                if (currentDirector == null) return null;
                var trackAsset = GetTrackAsset(instance);
                while (trackAsset != null)
                {
                    foreach (var playableBinding in trackAsset.outputs)
                    {
                        var o = currentDirector.GetGenericBinding(trackAsset) as UnityEngine.Object;
                        if (o != null) return o;
                    }
                    trackAsset = trackAsset.parent as TrackAsset;
                }
                return null;
            }
            public AnimationClip GetAnimationClip(object instance)
            {
                if (instance == null) return null;
                var clip = dg_get_m_Clip(instance);
                if (clip == null) return null;
                return clip.animationClip;
            }
            public void SetAnimationClip(object instance, AnimationClip animClip, string undoName = null)
            {
                if (instance == null) return;
                var clip = dg_get_m_Clip(instance);
                if (clip == null) return;
                var animationPlayableAsset = clip.asset as AnimationPlayableAsset;
                if (animationPlayableAsset == null) return;
                if (undoName != null)
                    Undo.RecordObject(animationPlayableAsset, undoName);
                animationPlayableAsset.clip = animClip;
            }

            public bool IsArmedForRecord(object instance)
            {
                var currentDirector = uTimelineState.GetCurrentDirector(GetTimelineState(instance));
                if (currentDirector == null) return false;
                var state = GetTimelineState(instance);
                if (state == null) return false;
                var trackAsset = GetTrackAsset(instance);
                if (trackAsset == null) return false;
                return uTimelineState.IsArmedForRecord(state, trackAsset);
            }
        }
        public class UTrackAsset
        {
            protected Func<bool> dg_get_locked;

            public virtual bool GetLocked(object instance)
            {
                if (instance == null) return false;
                if (dg_get_locked == null || dg_get_locked.Target != instance)
                    dg_get_locked = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("locked").GetGetMethod());
                return dg_get_locked();
            }
        }
        public class UAnimationTrack
        {
            protected FieldInfo fi_m_ClipOffset;
            protected Func<Vector3> dg_GetSceneOffsetPosition;
            protected Func<Vector3> dg_GetSceneOffsetRotation;
            protected MethodInfo mi_RequiresMotionXPlayable;
            protected MethodInfo mi_UsesAbsoluteMotion;
            protected MethodInfo mi_GetOffsetMode;
            protected Func<bool> dg_AnimatesRootTransform;

            public IPlayable GetClipOffset(object instance)
            {
                if (fi_m_ClipOffset == null)
                    fi_m_ClipOffset = instance.GetType().GetField("m_ClipOffset", BindingFlags.NonPublic | BindingFlags.Instance);
                return (IPlayable)fi_m_ClipOffset.GetValue(instance);
            }
            public Vector3 GetSceneOffsetPosition(object instance)
            {
                if (instance == null) return Vector3.zero;
                if (dg_GetSceneOffsetPosition == null || dg_GetSceneOffsetPosition.Target != instance)
                    dg_GetSceneOffsetPosition = (Func<Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3>), instance, instance.GetType().GetProperty("sceneOffsetPosition", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
                return dg_GetSceneOffsetPosition();
            }
            public Quaternion GetSceneOffsetRotation(object instance)
            {
                if (instance == null) return Quaternion.identity;
                if (dg_GetSceneOffsetRotation == null || dg_GetSceneOffsetRotation.Target != instance)
                    dg_GetSceneOffsetRotation = (Func<Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3>), instance, instance.GetType().GetProperty("sceneOffsetRotation", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
                return Quaternion.Euler(dg_GetSceneOffsetRotation());
            }

            public bool RequiresMotionXPlayable(object instance, int mode, GameObject gameObject)
            {
                if (instance == null) return false;
                if (mi_RequiresMotionXPlayable == null)
                    mi_RequiresMotionXPlayable = instance.GetType().GetMethod("RequiresMotionXPlayable", BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)mi_RequiresMotionXPlayable.Invoke(instance, new object[] { mode, gameObject });
            }
            public bool UsesAbsoluteMotion(object instance, int mode)
            {
                if (instance == null) return false;
                if (mi_UsesAbsoluteMotion == null)
                    mi_UsesAbsoluteMotion = instance.GetType().GetMethod("UsesAbsoluteMotion", BindingFlags.NonPublic | BindingFlags.Static);
                return (bool)mi_UsesAbsoluteMotion.Invoke(null, new object[] { mode });
            }
            public int GetOffsetMode(object instance, GameObject go, bool animatesRootTransform)
            {
                if (instance == null) return -1;
                if (mi_GetOffsetMode == null)
                    mi_GetOffsetMode = instance.GetType().GetMethod("GetOffsetMode", BindingFlags.NonPublic | BindingFlags.Instance);
                return (int)mi_GetOffsetMode.Invoke(instance, new object[] { go, animatesRootTransform });
            }
            public bool AnimatesRootTransform(object instance)
            {
                if (instance == null) return false;
                if (dg_AnimatesRootTransform == null || dg_AnimatesRootTransform.Target != instance)
                    dg_AnimatesRootTransform = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetMethod("AnimatesRootTransform", BindingFlags.NonPublic | BindingFlags.Instance));
                return dg_AnimatesRootTransform();
            }
        }
        public class UAnimationPlayableAsset
        {
            private Func<bool> dg_get_hasRootTransforms;

            public UAnimationPlayableAsset()
            {
            }

            public virtual bool GetHasRootTransforms(AnimationPlayableAsset instance)
            {
                if (instance == null) return false;
                if (dg_get_hasRootTransforms == null || dg_get_hasRootTransforms.Target != (object)instance)
                    dg_get_hasRootTransforms = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("hasRootTransforms", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));
                return dg_get_hasRootTransforms();
            }
        }
        public class UTimelineTreeViewGUI
        {
            public Func<List<TrackAsset>> dg_get_selection;

            public List<TrackAsset> GetSelection(object instance)
            {
                if (instance == null) return null;
                if (dg_get_selection == null || dg_get_selection.Target != instance)
                    dg_get_selection = (Func<List<TrackAsset>>)Delegate.CreateDelegate(typeof(Func<List<TrackAsset>>), instance, instance.GetType().GetProperty("selection").GetGetMethod());
                return dg_get_selection();
            }
        }
        public class UTimelineAnimationUtilities
        {
            private readonly MethodInfo mi_CreateTimeController;

            public UTimelineAnimationUtilities(Assembly asmTimelineEditor, Assembly _)
            {
                var timelineAnimationUtilitiesType = asmTimelineEditor.GetType("UnityEditor.Timeline.TimelineAnimationUtilities");
                var methods = timelineAnimationUtilitiesType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var mi in methods)
                {
                    if (mi.Name != "CreateTimeController") continue;
                    var parameters = mi.GetParameters();
                    if (parameters.Length != 2) continue;
                    if (parameters[0].Name == "state" &&
                        parameters[1].Name == "clip")
                    {
                        mi_CreateTimeController = mi;
                        break;
                    }
                }
                if (mi_CreateTimeController == null)
                {   //Timeline 1.4.0
                    foreach (var mi in methods)
                    {
                        if (mi.Name != "CreateTimeController") continue;
                        var parameters = mi.GetParameters();
                        if (parameters.Length != 1) continue;
                        if (parameters[0].Name == "clip")
                        {
                            mi_CreateTimeController = mi;
                            break;
                        }
                    }
                }
                Assert.IsNotNull(mi_CreateTimeController);
            }

            public object CreateTimeController(object timelineState, TimelineClip clip)
            {
                if (mi_CreateTimeController.GetParameters().Length == 2)
                    return mi_CreateTimeController.Invoke(null, new object[] { timelineState, clip });
                else
                    return mi_CreateTimeController.Invoke(null, new object[] { clip });    //Timeline 1.4.0
            }
        }

        public class UISequenceState
        {
            private Func<PlayableDirector> dg_get_director;
            private Func<int> dg_get_frame;
            private Action<int> dg_set_frame;
            private Func<float> dg_get_frameRate;
            private Func<double> dg_get_frameRateDouble;

            public PlayableDirector GetDirector(object instance)
            {
                if (instance == null) return null;
                if (dg_get_director == null || dg_get_director.Target != instance)
                    dg_get_director = (Func<PlayableDirector>)Delegate.CreateDelegate(typeof(Func<PlayableDirector>), instance, instance.GetType().GetProperty("director").GetGetMethod());
                return dg_get_director();
            }

            public int GetFrame(object instance)
            {
                if (instance == null) return 0;
                if (dg_get_frame == null || dg_get_frame.Target != instance)
                    dg_get_frame = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), instance, instance.GetType().GetProperty("frame").GetGetMethod());
                return dg_get_frame();
            }
            public void SetFrame(object instance, int frame)
            {
                if (instance == null) return;
                if (dg_set_frame == null || dg_set_frame.Target != instance)
                    dg_set_frame = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), instance, instance.GetType().GetProperty("frame").GetSetMethod());
                dg_set_frame(frame);
            }

            public float GetFrameRate(object instance)
            {
                if (instance == null) return 0f;
                if (!(dg_get_frameRate != null && dg_get_frameRate.Target == instance) &&
                    !(dg_get_frameRateDouble != null && dg_get_frameRateDouble.Target == instance))
                {
                    var mi = instance.GetType().GetProperty("frameRate").GetGetMethod();
                    dg_get_frameRate = null;
                    dg_get_frameRateDouble = null;
                    if (mi.ReturnType == typeof(double))
                        dg_get_frameRateDouble = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>), instance, mi);
                    else
                        dg_get_frameRate = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), instance, mi);
                }
                if (dg_get_frameRate != null && dg_get_frameRate.Target == instance)
                    return dg_get_frameRate();
                if (dg_get_frameRateDouble != null && dg_get_frameRateDouble.Target == instance)
                    return (float)dg_get_frameRateDouble();
                return 0f;
            }
        }

        public EditorWindow Instance
        {
            get { return dg_get_instance(); }
        }

        public object State
        {
            get
            {
                if (Instance == null) return null;
                if (dg_get_state == null || dg_get_state.Target != (object)Instance)
                    dg_get_state = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), Instance, Instance.GetType().GetProperty("state").GetGetMethod());
                return dg_get_state();
            }
        }

        public PlayableDirector GetCurrentDirector()
        {
            return TimelineState.GetCurrentDirector(State);
        }

        public bool GetRecording()
        {
            return TimelineState.GetRecording(State);
        }
        public void SetRecording(bool enable)
        {
            TimelineState.SetRecording(State, enable);
        }

        public bool GetPreviewMode()
        {
            return TimelineState.GetPreviewMode(State);
        }
        public void SetPreviewMode(bool enable)
        {
            TimelineState.SetPreviewMode(State, enable);
        }

        public void SetPlaying(bool enable)
        {
            TimelineState.SetPlaying(State, enable);
        }

        public void EvaluateImmediate()
        {
            TimelineState.EvaluateImmediate(State);
        }
        public void Refresh()
        {
            TimelineState.Refresh(State);
        }

        public void Close()
        {
            if (Instance != null)
                Instance.Close();
        }

        public virtual bool GetLock(EditorWindow aw)
        {
            if (aw == null) return false;
            return EditorGUIUtility.uEditorLockTracker.GetLock(dg_get_m_LockTracker(aw));
        }
        public virtual void SetLock(EditorWindow aw, bool flag)
        {
            if (aw == null) return;
            EditorGUIUtility.uEditorLockTracker.SetLock(dg_get_m_LockTracker(aw), flag);
        }

        public TrackAsset GetSelectionTrack()
        {
            if (Instance == null) return null;
            if (dg_get_treeView == null || dg_get_treeView.Target != (object)Instance)
                dg_get_treeView = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), Instance, Instance.GetType().GetProperty("treeView").GetGetMethod());
            var treeView = dg_get_treeView();
            if (treeView == null) return null;
            var selection = TimelineTreeViewGUI.GetSelection(treeView);
            if (selection == null || selection.Count <= 0) return null;
            return selection[0];
        }

        protected void GetTimelineAssembly(out Assembly asmTimelineEditor, out Assembly asmTimelineEngine)
        {
            asmTimelineEditor = typeof(UnityEditor.Timeline.TimelineEditor).Assembly;
            asmTimelineEngine = typeof(TrackAsset).Assembly;
        }
    }
#endif
}
