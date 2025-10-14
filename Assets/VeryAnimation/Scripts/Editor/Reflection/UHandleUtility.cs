using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UHandleUtility
    {
        private readonly Action dg_ApplyWireMaterial;

        public UHandleUtility()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var handleUtilityType = asmUnityEditor.GetType("UnityEditor.HandleUtility");

            Assert.IsNotNull(dg_ApplyWireMaterial = (Action)Delegate.CreateDelegate(typeof(Action), null, handleUtilityType.GetMethod("ApplyWireMaterial", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { }, null)));
        }

        public void ApplyWireMaterial()
        {
            dg_ApplyWireMaterial();
        }
    }
}
