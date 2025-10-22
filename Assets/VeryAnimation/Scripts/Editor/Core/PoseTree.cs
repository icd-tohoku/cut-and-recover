using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VeryAnimation
{
    [Serializable]
    internal class PoseTree
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        public enum EditMode
        {
            All,
            Selection,
            Total
        }
        private EditMode editMode;

        private readonly GUIContent[] EditModeString = new GUIContent[(int)EditMode.Total];

        private const int QuickSaveSize = 3;
        private PoseTemplate[] quickSaves;

        public PoseTree()
        {
            UpdateEditModeString();
            Language.OnLanguageChanged += UpdateEditModeString;
        }

        public void LoadEditorPref()
        {
            editMode = (EditMode)EditorPrefs.GetInt("VeryAnimation_Editor_Pose_EditMode", 0);
        }
        public void SaveEditorPref()
        {
            EditorPrefs.SetInt("VeryAnimation_Editor_Pose_EditMode", (int)editMode);
        }

        public void PoseTreeToolbarGUI()
        {
            EditorGUI.BeginChangeCheck();
            var mode = (EditMode)GUILayout.Toolbar((int)editMode, EditModeString, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                editMode = mode;
            }
        }
        public void PoseTreeGUI()
        {
            bool IsSetDisable() => editMode == EditMode.Selection && VAW.VA.SelectionActiveBone < 0;

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(IsSetDisable());
                #region Set
                if (GUILayout.Button("Reset", VAW.GuiStyleDropDown))
                {
                    GenericMenu menu = new();
                    {
                        if (VAW.VA.IsHuman)
                        {
                            menu.AddItem(Language.GetContent(Language.Help.EditorPoseHumanoidReset), false, () =>
                            {
                                Undo.RecordObject(VAE, "Humanoid Pose");
                                switch (editMode)
                                {
                                    case EditMode.All:
                                        VAW.VA.SetPoseHumanoidDefault();
                                        break;
                                    case EditMode.Selection:
                                        VAW.VA.SetSelectionHumanoidDefault(true, true);
                                        break;
                                }
                            });
                        }
                        if (VAW.VA.TransformPoseSave.IsEnableHumanDescriptionTransforms())
                        {
                            menu.AddItem(Language.GetContent(Language.Help.EditorPoseAvatarConfiguration), false, () =>
                            {
                                Undo.RecordObject(VAE, "Avatar Configuration Pose");
                                switch (editMode)
                                {
                                    case EditMode.All:
                                        VAW.VA.SetPoseHumanoidAvatarConfiguration();
                                        break;
                                    case EditMode.Selection:
                                        VAW.VA.SetSelectionHumanoidAvatarConfiguration(true, true);
                                        break;
                                }
                            });
                        }
                        if (VAW.VA.TransformPoseSave.IsEnableTPoseTransform())
                        {
                            menu.AddItem(Language.GetContent(Language.Help.EditorPoseTPose), false, () =>
                            {
                                Undo.RecordObject(VAE, "T Pose");
                                switch (editMode)
                                {
                                    case EditMode.All:
                                        VAW.VA.SetPoseHumanoidTPose();
                                        break;
                                    case EditMode.Selection:
                                        VAW.VA.SetSelectionHumanoidTPose(true, true);
                                        break;
                                }
                            });
                        }
                        if (VAW.VA.TransformPoseSave.IsEnableBindTransform())
                        {
                            menu.AddItem(Language.GetContent(Language.Help.EditorPoseBind), false, () =>
                            {
                                Undo.RecordObject(VAE, "Bind Pose");
                                switch (editMode)
                                {
                                    case EditMode.All:
                                        VAW.VA.SetPoseBind();
                                        break;
                                    case EditMode.Selection:
                                        VAW.VA.SetSelectionBindPose(true, true, true);
                                        break;
                                }
                            });
                        }
                        if (VAW.VA.TransformPoseSave.IsEnablePrefabTransform())
                        {
                            menu.AddItem(Language.GetContent(Language.Help.EditorPosePrefab), false, () =>
                            {
                                Undo.RecordObject(VAE, "Prefab Pose");
                                switch (editMode)
                                {
                                    case EditMode.All:
                                        VAW.VA.SetPosePrefab();
                                        break;
                                    case EditMode.Selection:
                                        VAW.VA.SetSelectionPrefabPose(true, true, true);
                                        break;
                                }
                            });
                        }
                        {
                            menu.AddItem(Language.GetContent(Language.Help.EditorPoseStart), false, () =>
                            {
                                Undo.RecordObject(VAE, "Edit Start Pose");
                                switch (editMode)
                                {
                                    case EditMode.All:
                                        VAW.VA.SetPoseEditStart();
                                        break;
                                    case EditMode.Selection:
                                        VAW.VA.SetSelectionEditStart(true, true, true);
                                        break;
                                }
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
                #endregion
                #region Mirror
                if (GUILayout.Button(Language.GetContent(Language.Help.EditorPoseMirror)))
                {
                    Undo.RecordObject(VAE, "Mirror Pose");
                    switch (editMode)
                    {
                        case EditMode.All:
                            VAW.VA.SetPoseMirror();
                            break;
                        case EditMode.Selection:
                            VAW.VA.SetSelectionMirror();
                            break;
                    }
                }
                #endregion
                #region Template
                if (GUILayout.Button(Language.GetContent(Language.Help.EditorPoseTemplate), VAW.GuiStyleDropDown))
                {
                    Dictionary<string, string> poseTemplates = new();
                    {
                        var guids = AssetDatabase.FindAssets("t:posetemplate");
                        for (int i = 0; i < guids.Length; i++)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                            var name = path["Assets/".Length..];
                            poseTemplates.Add(name, path);
                        }
                    }

                    GenericMenu menu = new();
                    {
                        var enu = poseTemplates.GetEnumerator();
                        while (enu.MoveNext())
                        {
                            var value = enu.Current.Value;
                            menu.AddItem(new GUIContent(enu.Current.Key), false, () =>
                            {
                                var poseTemplate = AssetDatabase.LoadAssetAtPath<PoseTemplate>(value);
                                if (poseTemplate != null)
                                {
                                    Undo.RecordObject(VAE, "Template Pose");
                                    switch (editMode)
                                    {
                                        case EditMode.All:
                                            VAW.VA.LoadPoseTemplate(poseTemplate);
                                            break;
                                        case EditMode.Selection:
                                            VAW.VA.LoadSelectionPoseTemplate(poseTemplate);
                                            break;
                                    }
                                }
                                else
                                {
                                    Debug.LogErrorFormat(Language.GetText(Language.Help.LogFailedLoadPoseError), value);
                                }
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
                #endregion
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
                #region Save as
                if (GUILayout.Button(Language.GetContent(Language.Help.EditorPoseSaveAs)))
                {
                    string path = EditorUtility.SaveFilePanel("Save as Pose Template", VAE.TemplateSaveDefaultDirectory, string.Format("{0}.asset", VAW.VA.CurrentClip.name), "asset");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!path.StartsWith(Application.dataPath))
                        {
                            EditorCommon.SaveInsideAssetsFolderDisplayDialog();
                        }
                        else
                        {
                            VAE.TemplateSaveDefaultDirectory = Path.GetDirectoryName(path);
                            path = FileUtil.GetProjectRelativePath(path);
                            var poseTemplate = ScriptableObject.CreateInstance<PoseTemplate>();
                            VAW.VA.SavePoseTemplate(poseTemplate);
                            try
                            {
                                VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
                                AssetDatabase.CreateAsset(poseTemplate, path);
                            }
                            finally
                            {
                                VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
                            }
                            VAE.Focus();
                        }
                    }
                }
                #endregion
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Quick Load", GUILayout.Width(70));
                    EditorGUI.BeginDisabledGroup(IsSetDisable());
                    for (int i = 0; i < QuickSaveSize; i++)
                    {
                        EditorGUI.BeginDisabledGroup(quickSaves == null || i >= quickSaves.Length || quickSaves[i] == null);
                        if (GUILayout.Button((i + 1).ToString()))
                        {
                            QuickLoad(i);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Quick Save", GUILayout.Width(70));
                    for (int i = 0; i < QuickSaveSize; i++)
                    {
                        if (GUILayout.Button((i + 1).ToString()))
                        {
                            QuickSave(i);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
        }

        private void UpdateEditModeString()
        {
            for (int i = 0; i < (int)EditMode.Total; i++)
            {
                EditModeString[i] = new GUIContent(Language.GetContent(Language.Help.EditorPoseModeAll + i));
            }
        }

        public void QuickSave(int index)
        {
            Undo.RecordObject(VAE, "Quick Save");
            if (quickSaves == null || quickSaves.Length != QuickSaveSize)
                quickSaves = new PoseTemplate[QuickSaveSize];
            quickSaves[index] = ScriptableObject.CreateInstance<PoseTemplate>();
            VAW.VA.SavePoseTemplate(quickSaves[index]);
        }
        public void QuickLoad(int index)
        {
            Undo.RecordObject(VAE, "Quick Load");
            if (quickSaves != null && index < quickSaves.Length && quickSaves[index] != null)
            {
                switch (editMode)
                {
                    case EditMode.All:
                        VAW.VA.LoadPoseTemplate(quickSaves[index]);
                        break;
                    case EditMode.Selection:
                        VAW.VA.LoadSelectionPoseTemplate(quickSaves[index]);
                        break;
                }
            }
        }
    }
}
