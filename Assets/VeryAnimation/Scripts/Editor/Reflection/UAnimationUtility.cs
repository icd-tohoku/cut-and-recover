using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimationUtility
    {
        private readonly Func<GameObject, EditorCurveBinding[]> dg_GetAnimationStreamBindings;
        private readonly Func<AnimationClip, bool> dg_HasMotionCurves;
        private readonly Func<AnimationClip, bool> dg_HasRootCurves;
        private readonly Action<AnimationCurve, int> dg_UpdateTangentsFromModeSurrounding;
        private readonly Action<AnimationClip, EditorCurveBinding, AnimationCurve, bool> dg_Internal_SetEditorCurve;
        private readonly Action<AnimationClip> dg_Internal_SyncEditorCurves;
        private readonly Action<AnimationClip, EditorCurveBinding, AnimationUtility.CurveModifiedType> dg_Internal_InvokeOnCurveWasModified;
        private readonly Func<AnimationCurve, float, int> dg_AddInbetweenKey;
        private readonly Action<AnimationClip, AnimationClipSettings> dg_SetAnimationClipSettingsNoDirty;

        public UAnimationUtility()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var animationUtilityType = asmUnityEditor.GetType("UnityEditor.AnimationUtility");
            Assert.IsNotNull(dg_GetAnimationStreamBindings = (Func<GameObject, EditorCurveBinding[]>)Delegate.CreateDelegate(typeof(Func<GameObject, EditorCurveBinding[]>), null, animationUtilityType.GetMethod("GetAnimationStreamBindings", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_HasMotionCurves = (Func<AnimationClip, bool>)Delegate.CreateDelegate(typeof(Func<AnimationClip, bool>), null, animationUtilityType.GetMethod("HasMotionCurves", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_HasRootCurves = (Func<AnimationClip, bool>)Delegate.CreateDelegate(typeof(Func<AnimationClip, bool>), null, animationUtilityType.GetMethod("HasRootCurves", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_UpdateTangentsFromModeSurrounding = (Action<AnimationCurve, int>)Delegate.CreateDelegate(typeof(Action<AnimationCurve, int>), null, animationUtilityType.GetMethod("UpdateTangentsFromModeSurrounding", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_Internal_SetEditorCurve = (Action<AnimationClip, EditorCurveBinding, AnimationCurve, bool>)Delegate.CreateDelegate(typeof(Action<AnimationClip, EditorCurveBinding, AnimationCurve, bool>), null, animationUtilityType.GetMethod("Internal_SetEditorCurve", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_Internal_SyncEditorCurves = (Action<AnimationClip>)Delegate.CreateDelegate(typeof(Action<AnimationClip>), null, animationUtilityType.GetMethod("SyncEditorCurves", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_Internal_InvokeOnCurveWasModified = (Action<AnimationClip, EditorCurveBinding, AnimationUtility.CurveModifiedType>)Delegate.CreateDelegate(typeof(Action<AnimationClip, EditorCurveBinding, AnimationUtility.CurveModifiedType>), null, animationUtilityType.GetMethod("Internal_InvokeOnCurveWasModified", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_AddInbetweenKey = (Func<AnimationCurve, float, int>)Delegate.CreateDelegate(typeof(Func<AnimationCurve, float, int>), null, animationUtilityType.GetMethod("AddInbetweenKey", BindingFlags.NonPublic | BindingFlags.Static)));
            Assert.IsNotNull(dg_SetAnimationClipSettingsNoDirty = (Action<AnimationClip, AnimationClipSettings>)Delegate.CreateDelegate(typeof(Action<AnimationClip, AnimationClipSettings>), null, animationUtilityType.GetMethod("SetAnimationClipSettingsNoDirty", BindingFlags.NonPublic | BindingFlags.Static)));
        }

        public EditorCurveBinding[] GetAnimationStreamBindings(GameObject root)
        {
            return dg_GetAnimationStreamBindings(root);
        }

        public bool HasMotionCurves(AnimationClip clip)
        {
            return dg_HasMotionCurves(clip);
        }

        public bool HasRootCurves(AnimationClip clip)
        {
            return dg_HasRootCurves(clip);
        }

        public void UpdateTangentsFromModeSurrounding(AnimationCurve curve, int index)
        {
            dg_UpdateTangentsFromModeSurrounding(curve, index);
        }

        public void Internal_SetEditorCurve(AnimationClip clip, EditorCurveBinding binding, AnimationCurve curve, bool syncEditorCurve)
        {
            dg_Internal_SetEditorCurve(clip, binding, curve, syncEditorCurve);
        }
        public void Internal_SyncEditorCurves(AnimationClip clip)
        {
            dg_Internal_SyncEditorCurves(clip);
        }
        public void Internal_InvokeOnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            dg_Internal_InvokeOnCurveWasModified(clip, binding, type);
        }

        public int AddInbetweenKey(AnimationCurve curve, float time)
        {
            return dg_AddInbetweenKey(curve, time);
        }

        public void SetAnimationClipSettingsNoDirty(AnimationClip clip, AnimationClipSettings clipSettings)
        {
            dg_SetAnimationClipSettingsNoDirty(clip, clipSettings);
        }
    }
}
