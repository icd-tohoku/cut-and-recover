using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UEditorGUI
    {
        private readonly Func<bool> dg_IsEditingTextField;
        private readonly Func<GUIContent> dg_titleSettingsIcon;
        private readonly Func<GUIContent> dg_helpIcon;

        public UEditorGUI()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());

            var editorGUIType = asmUnityEditor.GetType("UnityEditor.EditorGUI");
            Assert.IsNotNull(dg_IsEditingTextField = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), null, editorGUIType.GetMethod("IsEditingTextField", BindingFlags.NonPublic | BindingFlags.Static)));

            var gUIContentsType = asmUnityEditor.GetType("UnityEditor.EditorGUI+GUIContents");
            Assert.IsNotNull(dg_titleSettingsIcon = (Func<GUIContent>)Delegate.CreateDelegate(typeof(Func<GUIContent>), null, gUIContentsType.GetProperty("titleSettingsIcon", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true)));
            Assert.IsNotNull(dg_helpIcon = (Func<GUIContent>)Delegate.CreateDelegate(typeof(Func<GUIContent>), null, gUIContentsType.GetProperty("helpIcon", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true)));
        }

        public bool IsEditingTextField()
        {
            return dg_IsEditingTextField();
        }

        public GUIContent GetTitleSettingsIcon()
        {
            return dg_titleSettingsIcon();
        }
        public GUIContent GetHelpIcon()
        {
            return dg_helpIcon();
        }
    }
}
