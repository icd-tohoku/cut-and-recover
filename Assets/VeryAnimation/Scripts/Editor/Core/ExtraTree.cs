using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VeryAnimation
{
    [Serializable]
    internal class ExtraTree
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        public ExtraTree()
        {
        }

        public void LoadEditorPref()
        {
        }
        public void SaveEditorPref()
        {
        }

        public void ExtraTreeToolbarGUI()
        {

        }
        public void ExtraTreeGUI()
        {
            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = GUILayout.Toggle(VAW.VA.extraOptionsCollision, Language.GetContent(Language.Help.EditorExtraCollision), EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Collision");
                        VAW.VA.extraOptionsCollision = flag;
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
                {
                    EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || VAW.VA.UAw.GetLinkedWithTimeline());
                    EditorGUI.BeginChangeCheck();
                    var flag = GUILayout.Toggle(VAW.VA.extraOptionsSynchronizeAnimation, Language.GetContent(Language.Help.EditorExtraSynchronizeAnimation), EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Synchronize Animation");
                        VAW.VA.extraOptionsSynchronizeAnimation = flag;
                        VAW.VA.SetSynchronizeAnimation(VAW.VA.extraOptionsSynchronizeAnimation);
                        InternalEditorUtility.RepaintAllViews();
                    }
                    EditorGUI.EndDisabledGroup();
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = GUILayout.Toggle(VAW.VA.extraOptionsOnionSkin, Language.GetContent(Language.Help.EditorExtraOnionSkin), EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Onion Skin");
                        VAW.VA.extraOptionsOnionSkin = flag;
                        VAW.VA.OnionSkin.Update();
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
                {
                    EditorGUI.BeginDisabledGroup(!VAW.VA.IsHuman);
                    EditorGUI.BeginChangeCheck();
                    var flag = GUILayout.Toggle(VAW.VA.extraOptionsRootTrail, Language.GetContent(Language.Help.EditorExtraRootTrail), EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Root Trail");
                        VAW.VA.extraOptionsRootTrail = flag;
                        InternalEditorUtility.RepaintAllViews();
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
        }
    }
}
