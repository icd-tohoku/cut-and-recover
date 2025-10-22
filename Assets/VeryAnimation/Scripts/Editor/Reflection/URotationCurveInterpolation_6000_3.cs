#if UNITY_6000_3_OR_NEWER
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class URotationCurveInterpolation_6000_3 : URotationCurveInterpolation
    {
        public URotationCurveInterpolation_6000_3()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var rotationCurveInterpolationType = asmUnityEditor.GetType("UnityEditor.AnimationWindowBuiltin.RotationCurveInterpolation");
            Assert.IsNotNull(mi_SetInterpolation = rotationCurveInterpolationType.GetMethod("SetInterpolation", BindingFlags.NonPublic | BindingFlags.Static));
        }

        public override void SetInterpolation(AnimationClip clip, EditorCurveBinding[] curveBindings, Mode newInterpolationMode)
        {
            mi_SetInterpolation.Invoke(null, new object[] { clip, curveBindings, (int)newInterpolationMode });
        }
    }
}
#endif