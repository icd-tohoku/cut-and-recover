using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimatorControllerTool
    {
        private readonly Func<object, object> dg_get_tool;

        public Type animtorControllerToolLayerSettingsWindowType;

        public UAnimatorControllerTool()
        {
            var path = InternalEditorUtility.GetEditorAssemblyPath().Replace("UnityEditor.dll", "UnityEditor.Graphs.dll");
            var asmUnityEditor = Assembly.LoadFrom(path);
            var animatorControllerToolType = asmUnityEditor.GetType("UnityEditor.Graphs.AnimatorControllerTool");
            {
                var fi_tool = animatorControllerToolType.GetField("tool", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(dg_get_tool = EditorCommon.CreateGetFieldDelegate<object>(fi_tool));
            }
            animtorControllerToolLayerSettingsWindowType = asmUnityEditor.GetType("UnityEditor.Graphs.LayerSettingsWindow");
        }

        public EditorWindow Instance => (EditorWindow)dg_get_tool(null);
    }
}
