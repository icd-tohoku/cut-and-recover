#if UNITY_2023_1_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace VeryAnimation
{
    internal class UEditorWindow_2023_1 : UEditorWindow
    {
        private readonly Func<List<EditorWindow>> dg_activeEditorWindows;

        public UEditorWindow_2023_1()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            var editorWindowType = asmUnityEditor.GetType("UnityEditor.EditorWindow");

            //Unity 2023.1.17f1
            var pi_activeEditorWindows = editorWindowType.GetProperty("activeEditorWindows", BindingFlags.NonPublic | BindingFlags.Static);
            if (pi_activeEditorWindows != null)
            {
                dg_activeEditorWindows = (Func<List<EditorWindow>>)Delegate.CreateDelegate(typeof(Func<List<EditorWindow>>), null, pi_activeEditorWindows.GetGetMethod(true));
            }
        }

        public override IList GetActiveEditorWindows()
        {
            if (dg_activeEditorWindows != null)
                return dg_activeEditorWindows();
            else
                return base.GetActiveEditorWindows();
        }
    }
}
#endif