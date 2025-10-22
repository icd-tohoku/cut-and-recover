using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimationWindowUtility
    {
        protected MethodInfo mi_IsNodeLeftOverCurve;
        protected MethodInfo mi_GetNextKeyframeTime;
        protected MethodInfo mi_GetPreviousKeyframeTime;

        public UAnimationWindowUtility()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var animationWindowUtilityType = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowUtility");
            Assert.IsNotNull(mi_IsNodeLeftOverCurve = animationWindowUtilityType.GetMethod("IsNodeLeftOverCurve", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(mi_GetNextKeyframeTime = animationWindowUtilityType.GetMethod("GetNextKeyframeTime", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(mi_GetPreviousKeyframeTime = animationWindowUtilityType.GetMethod("GetPreviousKeyframeTime", BindingFlags.Public | BindingFlags.Static));
        }

        public virtual bool IsNodeLeftOverCurve(object state, object node)
        {
            return (bool)mi_IsNodeLeftOverCurve.Invoke(null, new object[] { node });
        }

        public float GetNextKeyframeTime(Array curves, float currentTime, float frameRate)
        {
            return (float)mi_GetNextKeyframeTime.Invoke(null, new object[] { curves, currentTime, frameRate });
        }
        public float GetPreviousKeyframeTime(Array curves, float currentTime, float frameRate)
        {
            return (float)mi_GetPreviousKeyframeTime.Invoke(null, new object[] { curves, currentTime, frameRate });
        }
    }
}
