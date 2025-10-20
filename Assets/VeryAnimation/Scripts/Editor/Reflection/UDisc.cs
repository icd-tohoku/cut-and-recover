using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UDisc
    {
        private readonly Func<object, float> dg_get_s_RotationDist;

        public UDisc()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var discType = asmUnityEditor.GetType("UnityEditorInternal.Disc");
            Assert.IsNotNull(dg_get_s_RotationDist = EditorCommon.CreateGetFieldDelegate<float>(discType.GetField("s_RotationDist", BindingFlags.NonPublic | BindingFlags.Static)));
        }

        public float GetRotationDist()
        {
            return dg_get_s_RotationDist(null);
        }
    }
}
