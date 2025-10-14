using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal partial class VeryAnimation
    {
        public enum ToolMode
        {
            Copy,
            Trim,
            Add,
            Combine,
            CreateNewClip,
            CreateNewKeyframe,
            BakeIK,
            AnimationRigging,
            HumanoidIK,
            RootMotion,
            ParameterRelatedCurves,
            RotationCurveInterpolation,
            KeyframeReduction,
            EnsureQuaternionContinuity,
            Cleanup,
            FixErrors,
            AdditiveReferencePose,
            AnimCompression,
            Export,
        }
        public ToolMode toolMode;
        public bool toolsHelp = true;
        public int toolCopy_FirstFrame;
        public int toolCopy_LastFrame;
        public int toolCopy_WriteFrame;
        public int toolTrim_FirstFrame;
        public int toolTrim_LastFrame;
        public AnimationClip toolAdd_Clip;
        public AnimationClip toolCombine_Clip;
        public enum CreateNewClipMode
        {
            Blank,
            Duplicate,
            Mirror,
            Result,
        }
        public CreateNewClipMode toolCreateNewClip_Mode;
        public int toolCreateNewClip_FirstFrame;
        public int toolCreateNewClip_LastFrame;
        public int toolCreateNewKeyframe_FirstFrame;
        public int toolCreateNewKeyframe_LastFrame;
        public int toolCreateNewKeyframe_IntervalFrame = 6;
        public bool toolCreateNewKeyframe_AnimatorRootT = true;
        public bool toolCreateNewKeyframe_AnimatorRootQ = true;
        public bool toolCreateNewKeyframe_AnimatorMuscle = true;
        public bool toolCreateNewKeyframe_AnimatorTDOF;
        public bool toolCreateNewKeyframe_TransformPosition;
        public bool toolCreateNewKeyframe_TransformRotation = true;
        public bool toolCreateNewKeyframe_TransformScale;
        public enum BakeIKMode
        {
            Simple,
            Interpolation,
        }
        public BakeIKMode toolBakeIK_Mode;
        public int toolBakeIK_FirstFrame;
        public int toolBakeIK_LastFrame;
        public int toolAnimationRigging_FirstFrame;
        public int toolAnimationRigging_LastFrame;
#pragma warning disable 0649
        public bool toolAnimationRigging_ChangeRigWeight;
        public float toolAnimationRigging_RigWeight = 1f;
        public bool toolAnimationRigging_RootMotionCancel;
        public bool toolAnimationRigging_ChangeConstraintWeight = true;
        public float toolAnimationRigging_ConstraintWeight = 1f;
#pragma warning restore 0649
        public bool toolHumanoidIK_Hand;
        public bool toolHumanoidIK_Foot = true;
        public int toolHumanoidIK_FirstFrame;
        public int toolHumanoidIK_LastFrame;
        public enum RootMotionMode
        {
            MotionCurves,
            RootCurves,
        }
        public RootMotionMode toolRootMotion_Mode;
        public bool toolCleanup_RemoveRoot;
        public bool toolCleanup_RemoveIK;
        public bool toolCleanup_RemoveTDOF;
        public bool toolCleanup_RemoveMotion;
        public bool toolCleanup_RemoveFinger;
        public bool toolCleanup_RemoveEyes;
        public bool toolCleanup_RemoveJaw;
        public bool toolCleanup_RemoveToes;
        public bool toolCleanup_RemoveTransformPosition;
        public bool toolCleanup_RemoveTransformRotation;
        public bool toolCleanup_RemoveTransformScale;
        public bool toolCleanup_RemoveBlendShape;
        public bool toolCleanup_RemoveObjectReference;
        public bool toolCleanup_RemoveEvent;
        public bool toolCleanup_RemoveMissing = true;
        public bool toolCleanup_RemoveHumanoidConflict = true;
        public bool toolCleanup_RemoveRootMotionConflict = true;
        public bool toolCleanup_RemoveUnnecessary = true;
        public bool toolCleanup_RemoveAvatarMaskDisable;
        public AvatarMask toolCleanup_RemoveAvatarMask;
        public enum RotationCurveInterpolationMode
        {
            Quaternion,
            EulerAngles,
        };
        public RotationCurveInterpolationMode toolRotationInterpolation_Mode;
        public float toolKeyframeReduction_RotationError = 0.5f;
        public float toolKeyframeReduction_PositionError = 0.5f;
        public float toolKeyframeReduction_ScaleAndOthersError = 0.5f;
        public bool toolKeyframeReduction_EnableHumanoid = true;
        public bool toolKeyframeReduction_EnableHumanoidRootAndIKGoal = true;
        public bool toolKeyframeReduction_EnableGeneric = true;
        public bool toolKeyframeReduction_EnableOther = true;
        public bool toolAdditiveReferencePose_Has;
        public AnimationClip toolAdditiveReferencePose_Clip;
        public float toolAdditiveReferencePose_Time;
        public bool toolAnimCompression_Compressed;
        public bool toolAnimCompression_UseHighQualityCurve = true;
        public bool toolExport_ActiveOnly = true;
        public bool toolExport_Mesh = true;
        public enum ExportAnimationMode
        {
            None,
            CurrentClip,
            AllClips,
        };
        public ExportAnimationMode toolExport_AnimationMode = ExportAnimationMode.CurrentClip;
        public bool toolExport_BakeFootIK = true;
#pragma warning disable 0649
        public bool toolExport_BakeAnimationRigging;
#pragma warning restore 0649

#pragma warning disable 0414
        private bool toolBakeIK_AnimatorIKFoldout = true;
        private bool toolBakeIK_OriginalIKFoldout = true;
        private bool toolAnimationRigging_AnimatorIKFoldout = true;
#pragma warning restore 0414

        private static readonly GUIContent[] CreateNewClipModeStrings =
        {
            new(CreateNewClipMode.Blank.ToString()),
            new(CreateNewClipMode.Duplicate.ToString()),
            new(CreateNewClipMode.Mirror.ToString()),
            new(CreateNewClipMode.Result.ToString()),
        };
        private static readonly GUIContent[] BakeIKModeStrings =
        {
            new("Simple"),
            new("Interpolation"),
        };
        private static readonly GUIContent[] RootMotionModeStrings =
        {
            new("Motion Curves", "MotionT, MotionQ"),
            new("Root Curves", "RootT, RootQ"),
        };

        private class ParameterRelatedData
        {
            public string propertyName;
            public int parameterIndex;
            public bool enableAnimationCurve;
            public bool enableAnimatorParameter;
        }
        private List<ParameterRelatedData> toolParameterRelatedCurve_DataList;
        private bool toolParameterRelatedCurve_Update;
        private ReorderableList toolParameterRelatedCurve_List;

        public void ToolsGUI()
        {
            if (CurrentClip == null) return;
            var clip = CurrentClip;
            var e = Event.current;

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    var mode = (ToolMode)EditorGUILayout.EnumPopup(toolMode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Tool Mode");
                        toolMode = mode;
                    }
                }
                {
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), toolsHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        Undo.RecordObject(VAW, "Change Tool Help");
                        toolsHelp = !toolsHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel++;
            if (toolsHelp)
            {
                EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpToolsCopy + (int)toolMode), MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space();
            }
            if (toolMode == ToolMode.Copy)
            {
                #region Copy
                if (UAnimationClipEditor != null)
                {
                    float firstFrame = toolCopy_FirstFrame;
                    float lastFrame = toolCopy_LastFrame;
                    float additivePoseframe = 0.0f;
                    UAnimationClipEditor.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                    if (changedStart)
                    {
                        Undo.RecordObject(VAW, "Change First Frame");
                        toolCopy_FirstFrame = Mathf.RoundToInt(firstFrame);
                    }
                    if (changedStop)
                    {
                        Undo.RecordObject(VAW, "Change Last Frame");
                        toolCopy_LastFrame = Mathf.RoundToInt(lastFrame);
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Language.GetContent(Language.Help.ToolsCopyWriteFrame), GUILayout.Width(132));
                    {
                        EditorGUI.BeginChangeCheck();
                        var frame = EditorGUILayout.IntField(toolCopy_WriteFrame, GUILayout.Width(64));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Write Frame");
                            toolCopy_WriteFrame = Math.Max(frame, 0);
                        }
                        if (GUILayout.Button(new GUIContent("Current", "Set current frame"), EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(15)))
                        {
                            Undo.RecordObject(VAW, "Change Write Frame");
                            toolCopy_WriteFrame = UAw.GetCurrentFrame();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("to", GUILayout.Width(32));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField((toolCopy_WriteFrame + (toolCopy_LastFrame - toolCopy_FirstFrame)).ToString(), GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Copy"))
                    {
                        ToolsCopy(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.Trim)
            {
                #region Trim
                if (UAnimationClipEditor != null)
                {
                    float firstFrame = toolTrim_FirstFrame;
                    float lastFrame = toolTrim_LastFrame;
                    float additivePoseframe = 0.0f;
                    UAnimationClipEditor.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                    if (changedStart)
                    {
                        Undo.RecordObject(VAW, "Change First Frame");
                        toolTrim_FirstFrame = Mathf.RoundToInt(firstFrame);
                    }
                    if (changedStop)
                    {
                        Undo.RecordObject(VAW, "Change Last Frame");
                        toolTrim_LastFrame = Mathf.RoundToInt(lastFrame);
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Trim"))
                    {
                        ToolsTrim(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.Add)
            {
                #region Add
                {
                    EditorGUI.BeginChangeCheck();
                    var addClip = EditorGUILayout.ObjectField("Add Clip", toolAdd_Clip, typeof(AnimationClip), true) as AnimationClip;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Add Clip");
                        toolAdd_Clip = addClip;
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUI.BeginDisabledGroup(toolAdd_Clip == null);
                    if (GUILayout.Button("Add"))
                    {
                        ToolsAdd(clip);
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.Combine)
            {
                #region Combine
                {
                    EditorGUI.BeginChangeCheck();
                    var combineClip = EditorGUILayout.ObjectField("Combine Clip", toolCombine_Clip, typeof(AnimationClip), true) as AnimationClip;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Combine Clip");
                        toolCombine_Clip = combineClip;
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUI.BeginDisabledGroup(toolCombine_Clip == null);
                    if (GUILayout.Button("Combine"))
                    {
                        ToolsCombine(clip);
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.CreateNewClip)
            {
                #region CreateNewClip
                {
                    {
                        EditorGUI.BeginChangeCheck();
                        var mode = (CreateNewClipMode)GUILayout.Toolbar((int)toolCreateNewClip_Mode, CreateNewClipModeStrings, EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Create New Clip Mode");
                            toolCreateNewClip_Mode = mode;
                        }
                        if (toolsHelp)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.ToolsCreateNewClipBlank + (int)toolCreateNewClip_Mode), MessageType.Info);
                        }
                        EditorGUILayout.Space();
                    }
                    if (UAnimationClipEditorTotal != null &&
                        toolCreateNewClip_Mode == CreateNewClipMode.Result)
                    {
                        float firstFrame = toolCreateNewClip_FirstFrame;
                        float lastFrame = toolCreateNewClip_LastFrame;
                        float additivePoseframe = 0.0f;
                        UAnimationClipEditorTotal.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                        if (changedStart)
                        {
                            Undo.RecordObject(VAW, "Change First Frame");
                            toolCreateNewClip_FirstFrame = Mathf.RoundToInt(firstFrame);
                        }
                        if (changedStop)
                        {
                            Undo.RecordObject(VAW, "Change Last Frame");
                            toolCreateNewClip_LastFrame = Mathf.RoundToInt(lastFrame);
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Create"))
                    {
                        var name = clip.name;
                        if (toolCreateNewClip_Mode == CreateNewClipMode.Mirror)
                            name += " (mirror)";
                        else if (toolCreateNewClip_Mode == CreateNewClipMode.Result)
                            name += " (result)";
                        var assetPath = string.Format("{0}/{1}.anim", EditorCommon.GetAssetPath(clip), name);
                        var uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                        string path = EditorUtility.SaveFilePanel("Create new animation clip", Path.GetDirectoryName(uniquePath), Path.GetFileName(uniquePath), "anim");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (!path.StartsWith(Application.dataPath))
                            {
                                EditorCommon.SaveInsideAssetsFolderDisplayDialog();
                            }
                            else
                            {
                                path = FileUtil.GetProjectRelativePath(path);
                                ToolsCreateNewClip(path);
                            }
                        }
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.CreateNewKeyframe)
            {
                #region CreateNewKeyframe
                if (UAnimationClipEditor != null)
                {
                    float firstFrame = toolCreateNewKeyframe_FirstFrame;
                    float lastFrame = toolCreateNewKeyframe_LastFrame;
                    float additivePoseframe = 0.0f;
                    UAnimationClipEditor.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                    if (changedStart)
                    {
                        Undo.RecordObject(VAW, "Change First Frame");
                        toolCreateNewKeyframe_FirstFrame = Mathf.RoundToInt(firstFrame);
                    }
                    if (changedStop)
                    {
                        Undo.RecordObject(VAW, "Change Last Frame");
                        toolCreateNewKeyframe_LastFrame = Mathf.RoundToInt(lastFrame);
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Language.GetContent(Language.Help.ToolsCreateNewKeyframeIntervalFrame), GUILayout.Width(132));
                    {
                        EditorGUI.BeginChangeCheck();
                        var frame = EditorGUILayout.IntField(toolCreateNewKeyframe_IntervalFrame, GUILayout.Width(64));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Interval Frame");
                            toolCreateNewKeyframe_IntervalFrame = Math.Max(frame, 1);
                        }
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(string.Format("{0} Second", GetFrameTime(toolCreateNewKeyframe_IntervalFrame)), VAW.GuiStyleMiddleRightGreyMiniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                if (IsHuman)
                {
                    EditorGUILayout.LabelField(new GUIContent("Animator", "Humanoid"));
                    EditorGUI.indentLevel++;
                    {
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("RootT", toolCreateNewKeyframe_AnimatorRootT);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_AnimatorRootT = flag;
                            }
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("RootQ", toolCreateNewKeyframe_AnimatorRootQ);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_AnimatorRootQ = flag;
                            }
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("Muscle", toolCreateNewKeyframe_AnimatorMuscle);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_AnimatorMuscle = flag;
                            }
                        }
                        if (HumanoidHasTDoF)
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("TDOF", toolCreateNewKeyframe_AnimatorTDOF);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_AnimatorTDOF = flag;
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                {
                    EditorGUILayout.LabelField(new GUIContent("Transform", "Generic"));
                    EditorGUI.indentLevel++;
                    {
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("Position", toolCreateNewKeyframe_TransformPosition);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_TransformPosition = flag;
                            }
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("Rotation", toolCreateNewKeyframe_TransformRotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_TransformRotation = flag;
                            }
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft("Scale", toolCreateNewKeyframe_TransformScale);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Flag");
                                toolCreateNewKeyframe_TransformScale = flag;
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                bool disable = (SelectionBones == null || SelectionBones.Count == 0);
                {
                    EditorGUI.BeginDisabledGroup(disable);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Apply"))
                    {
                        ToolsCreateNewKeyframe(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }
                #endregion
            }
            else if (toolMode == ToolMode.BakeIK)
            {
                #region BakeIK
                if (UAnimationClipEditor != null)
                {
                    float firstFrame = toolBakeIK_FirstFrame;
                    float lastFrame = toolBakeIK_LastFrame;
                    float additivePoseframe = 0.0f;
                    UAnimationClipEditor.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                    if (changedStart)
                    {
                        Undo.RecordObject(VAW, "Change First Frame");
                        toolBakeIK_FirstFrame = Mathf.RoundToInt(firstFrame);
                    }
                    if (changedStop)
                    {
                        Undo.RecordObject(VAW, "Change Last Frame");
                        toolBakeIK_LastFrame = Mathf.RoundToInt(lastFrame);
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var mode = (BakeIKMode)GUILayout.Toolbar((int)toolBakeIK_Mode, BakeIKModeStrings, EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Root Motion Mode");
                        toolBakeIK_Mode = mode;
                    }
                    if (toolsHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.ToolsBakeIKSimple + (int)toolBakeIK_Mode), MessageType.Info);
                    }
                    EditorGUILayout.Space();
                }
                bool disable = !animatorIK.ikData.Any(data => data.enable) && !originalIK.ikData.Any(data => data.enable);
                {
                    #region AnimatorIK
                    if (IsHuman && animatorIK.ikData != null)
                    {
                        toolBakeIK_AnimatorIKFoldout = EditorGUILayout.Foldout(toolBakeIK_AnimatorIKFoldout, "Animator IK", true);
                        if (toolBakeIK_AnimatorIKFoldout)
                        {
                            EditorGUI.indentLevel++;
                            for (int index = 0; index < animatorIK.ikData.Length; index++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginChangeCheck();
                                    EditorGUILayout.ToggleLeft(AnimatorIKCore.IKTargetStrings[index], animatorIK.ikData[index].enable, GUILayout.Width(160f));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        animatorIK.ChangeTargetIK((AnimatorIKCore.IKTarget)index);
                                    }
                                }
                                {
                                    var label = "Sync(";
                                    switch (animatorIK.ikData[index].defaultSyncType)
                                    {
                                        case AnimatorIKCore.AnimatorIKData.SyncType.Skeleton:
                                            label += "Skeleton";
                                            break;
                                        case AnimatorIKCore.AnimatorIKData.SyncType.SceneObject:
                                            label += "Scene Object";
                                            break;
                                        case AnimatorIKCore.AnimatorIKData.SyncType.HumanoidIK:
                                            label += "Humanoid IK";
                                            break;
                                        case AnimatorIKCore.AnimatorIKData.SyncType.AnimationRigging:
                                            label += "Animation Rigging";
                                            break;
                                        default:
                                            break;
                                    }
                                    label += ")";
                                    EditorGUILayout.LabelField(label, VAW.GuiStyleMiddleRightMiniLabel);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    #endregion
                    #region OriginalIK
                    if (originalIK.ikData != null)
                    {
                        toolBakeIK_OriginalIKFoldout = EditorGUILayout.Foldout(toolBakeIK_OriginalIKFoldout, "Original IK", true);
                        if (toolBakeIK_OriginalIKFoldout)
                        {
                            EditorGUI.indentLevel++;
                            for (int index = 0; index < originalIK.ikData.Count; index++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginChangeCheck();
                                    EditorGUILayout.ToggleLeft(originalIK.ikData[index].name, originalIK.ikData[index].enable, GUILayout.Width(160f));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        originalIK.ChangeTargetIK(index);
                                    }
                                }
                                {
                                    var label = "Sync(";
                                    switch (originalIK.ikData[index].defaultSyncType)
                                    {
                                        case OriginalIKCore.OriginalIKData.SyncType.Skeleton:
                                            label += "Skeleton";
                                            break;
                                        case OriginalIKCore.OriginalIKData.SyncType.SceneObject:
                                            label += "Scene Object";
                                            break;
                                        default:
                                            break;
                                    }
                                    label += ")";
                                    EditorGUILayout.LabelField(label, VAW.GuiStyleMiddleRightMiniLabel);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    #endregion
                }
                EditorGUI.BeginDisabledGroup(disable);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Apply"))
                {
                    ToolsGenarateBakeIK(clip);
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                #endregion
            }
            else if (toolMode == ToolMode.AnimationRigging)
            {
                #region AnimationRigging
#if VERYANIMATION_ANIMATIONRIGGING
                EditorGUI.BeginDisabledGroup(!AnimationRigging.IsValid);
                if (UAnimationClipEditorTotal != null)
                {
                    float firstFrame = toolAnimationRigging_FirstFrame;
                    float lastFrame = toolAnimationRigging_LastFrame;
                    float additivePoseframe = 0.0f;
                    UAnimationClipEditorTotal.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                    if (changedStart)
                    {
                        Undo.RecordObject(VAW, "Change First Frame");
                        toolAnimationRigging_FirstFrame = Mathf.RoundToInt(firstFrame);
                    }
                    if (changedStop)
                    {
                        Undo.RecordObject(VAW, "Change Last Frame");
                        toolAnimationRigging_LastFrame = Mathf.RoundToInt(lastFrame);
                    }
                }
                bool constraintDisable = !animatorIK.ikData.Any(data => data.enable && data.rigConstraint != null);
                EditorGUILayout.LabelField("Rig");
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft("Rig Weight", toolAnimationRigging_ChangeRigWeight);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Settings");
                            toolAnimationRigging_ChangeRigWeight = flag;
                        }
                    }
                    if (toolAnimationRigging_ChangeRigWeight)
                    {
                        EditorGUI.BeginChangeCheck();
                        var value = EditorGUILayout.Slider(toolAnimationRigging_RigWeight, 0f, 1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Settings");
                            toolAnimationRigging_RigWeight = value;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.LabelField("Constraint");
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(constraintDisable);
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsAnimationRiggingRootMotionCancel), toolAnimationRigging_RootMotionCancel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Settings");
                        toolAnimationRigging_RootMotionCancel = flag;
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft("Constraint Weight", toolAnimationRigging_ChangeConstraintWeight);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Settings");
                            toolAnimationRigging_ChangeConstraintWeight = flag;
                        }
                    }
                    if (toolAnimationRigging_ChangeConstraintWeight)
                    {
                        EditorGUI.BeginChangeCheck();
                        var value = EditorGUILayout.Slider(toolAnimationRigging_ConstraintWeight, 0f, 1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Settings");
                            toolAnimationRigging_ConstraintWeight = value;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();
                {
                    #region AnimatorIK
                    if (IsHuman && animatorIK.ikData != null)
                    {
                        toolAnimationRigging_AnimatorIKFoldout = EditorGUILayout.Foldout(toolAnimationRigging_AnimatorIKFoldout, "Animator IK", true);
                        if (toolAnimationRigging_AnimatorIKFoldout)
                        {
                            EditorGUI.indentLevel++;
                            for (int index = 0; index < animatorIK.ikData.Length; index++)
                            {
                                var data = animatorIK.ikData[index];
                                if (data.rigConstraint == null)
                                    continue;
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginChangeCheck();
                                    EditorGUILayout.ToggleLeft("", data.enable, GUILayout.Width(64f));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        animatorIK.ChangeTargetIK((AnimatorIKCore.IKTarget)index);
                                    }
                                }
                                {
                                    if (GUILayout.Button(AnimatorIKCore.IKTargetStrings[index]))
                                    {
                                        SelectGameObject(data.rigConstraint.gameObject);
                                        {
                                            var list = new List<EditorCurveBinding>();
                                            {
                                                list.AddRange(animatorIK.GetAnimationRiggingConstraintBindings((AnimatorIKCore.IKTarget)index));
                                                list.Add(data.rigConstraintWeight);
                                            }
                                            SetAnimationWindowSynchroSelection(list.ToArray());
                                        }
                                    }
                                }
                                {
                                    var label = "Sync(";
                                    switch (animatorIK.ikData[index].defaultSyncType)
                                    {
                                        case AnimatorIKCore.AnimatorIKData.SyncType.Skeleton:
                                            label += "Skeleton";
                                            break;
                                        case AnimatorIKCore.AnimatorIKData.SyncType.SceneObject:
                                            label += "Scene Object";
                                            break;
                                        case AnimatorIKCore.AnimatorIKData.SyncType.HumanoidIK:
                                            label += "Humanoid IK";
                                            break;
                                        case AnimatorIKCore.AnimatorIKData.SyncType.AnimationRigging:
                                            label += "Animation Rigging";
                                            break;
                                        default:
                                            break;
                                    }
                                    label += "), Write on Update(";
                                    switch ((AnimatorIKCore.IKTarget)index)
                                    {
                                        case AnimatorIKCore.IKTarget.Head:
                                            if (data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                            {
                                                label += "Target";
                                            }
                                            if (!data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                            {
                                                label += "[Nothing]";
                                            }
                                            break;
                                        case AnimatorIKCore.IKTarget.LeftHand:
                                        case AnimatorIKCore.IKTarget.RightHand:
                                        case AnimatorIKCore.IKTarget.LeftFoot:
                                        case AnimatorIKCore.IKTarget.RightFoot:
                                            if (data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget) &&
                                                data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingHint))
                                            {
                                                label += "Target, Hint";
                                            }
                                            else if (data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                            {
                                                label += "Target";
                                            }
                                            else if (data.writeFlags.HasFlag(AnimatorIKCore.AnimatorIKData.WriteFlags.AnimationRiggingHint))
                                            {
                                                label += "Hint";
                                            }
                                            else
                                            {
                                                label += "[Nothing]";
                                            }
                                            break;
                                    }
                                    label += ")";
                                    EditorGUILayout.LabelField(label, VAW.GuiStyleMiddleRightMiniLabel);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    #endregion
                }
                EditorGUI.indentLevel--;
                EditorGUI.BeginDisabledGroup(!toolAnimationRigging_ChangeRigWeight && constraintDisable);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Clear"))
                {
                    ToolsClearAnimationRigging(clip);
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Genarate"))
                {
                    ToolsGenarateAnimationRigging(clip);
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndDisabledGroup();
#endif
                #endregion
            }
            else if (toolMode == ToolMode.HumanoidIK)
            {
                #region HumanoidIK
                EditorGUI.BeginDisabledGroup(!IsHuman || !clip.isHumanMotion);
                if (UAnimationClipEditor != null)
                {
                    float firstFrame = toolHumanoidIK_FirstFrame;
                    float lastFrame = toolHumanoidIK_LastFrame;
                    float additivePoseframe = 0.0f;
                    UAnimationClipEditor.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
                    if (changedStart)
                    {
                        Undo.RecordObject(VAW, "Change First Frame");
                        toolHumanoidIK_FirstFrame = Mathf.RoundToInt(firstFrame);
                    }
                    if (changedStop)
                    {
                        Undo.RecordObject(VAW, "Change Last Frame");
                        toolHumanoidIK_LastFrame = Mathf.RoundToInt(lastFrame);
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Hand IK", toolHumanoidIK_Hand);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Curve setting");
                        toolHumanoidIK_Hand = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Foot IK", toolHumanoidIK_Foot);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Curve setting");
                        toolHumanoidIK_Foot = flag;
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(!IsHuman || !clip.isHumanMotion || (!toolHumanoidIK_Hand && !toolHumanoidIK_Foot));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Clear"))
                {
                    ToolsClearHumanoidIK(clip);
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Genarate"))
                {
                    ToolsGenarateHumanoidIK(clip);
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                if (!IsHuman || !clip.isHumanMotion)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpToolsHumanoidWarning), MessageType.Warning);
                }
                #endregion
            }
            else if (toolMode == ToolMode.RootMotion)
            {
                #region RootMotion
                {
                    EditorGUI.BeginChangeCheck();
                    var mode = (RootMotionMode)GUILayout.Toolbar((int)toolRootMotion_Mode, RootMotionModeStrings, EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Root Motion Mode");
                        toolRootMotion_Mode = mode;
                    }
                }
                if (toolsHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.ToolsRootMotionMotionCurves + (int)toolRootMotion_Mode), MessageType.Info);
                }
                EditorGUILayout.Space();
                if (toolRootMotion_Mode == RootMotionMode.MotionCurves)
                {
                    #region MotionCurves
                    var disable = VAW.Animator == null;
                    EditorGUI.BeginDisabledGroup(disable);

                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();
                        var index = -1;
                        if (SelectionMotionTool)
                        {
                            if (Tools.current != Tool.View)
                            {
                                if (CurrentTool() == Tool.Move) index = 0;
                                else index = 1;
                            }
                        }
                        EditorGUI.BeginChangeCheck();
                        index = GUILayout.Toolbar(index, VAW.GuiContentMoveRotateTools);
                        if (EditorGUI.EndChangeCheck())
                        {
                            LastTool = index switch
                            {
                                0 => Tool.Move,
                                _ => Tool.Rotate,
                            };
                            SelectMotionTool();
                        }
                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                    }

                    GUILayout.Space(24f);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Clear"))
                    {
                        ToolsRootMotionMotionClear(clip);
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Genarate"))
                    {
                        ToolsRootMotionMotionGenerate(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();
                    if (VAW.Animator == null)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpToolsNotAnimatorWarning), MessageType.Warning);
                    }
                    #endregion
                }
                else if (toolRootMotion_Mode == RootMotionMode.RootCurves)
                {
                    #region RootCurves
                    var disable = IsHuman || RootMotionBoneIndex < 0 || VAW.Animator == null;
                    EditorGUI.BeginDisabledGroup(disable);

                    GUILayout.Space(24f);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Clear"))
                    {
                        ToolsRootMotionRootClear(clip);
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Genarate"))
                    {
                        ToolsRootMotionRootGenerate(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();
                    if (disable)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpToolsGenericAndRootNodeWarning), MessageType.Warning);
                    }
                    #endregion
                }
                #endregion
            }
            else if (toolMode == ToolMode.ParameterRelatedCurves)
            {
                #region ParameterRelatedCurves
                if (e.type == EventType.Layout)
                {
                    ParameterRelatedCurveUpdateList();
                }
                toolParameterRelatedCurve_List?.DoLayoutList();
                if (clip.legacy || VAW.Animator == null)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpToolsNotAnimatorWarning), MessageType.Warning);
                }
                #endregion
            }
            else if (toolMode == ToolMode.RotationCurveInterpolation)
            {
                #region RotationCurveInterpolation
                {
                    EditorGUI.BeginChangeCheck();
                    var mode = (RotationCurveInterpolationMode)EditorGUILayout.EnumPopup("Interpolation", toolRotationInterpolation_Mode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Rotation Curve Interpolation setting");
                        toolRotationInterpolation_Mode = mode;
                    }
                }
                if (toolRotationInterpolation_Mode == RotationCurveInterpolationMode.EulerAngles)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpToolsRotationCurveInterpolationEulerAnglesWarning), MessageType.Warning);
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Convert"))
                    {
                        ToolsRotationCurveInterpolation(clip, toolRotationInterpolation_Mode);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.KeyframeReduction)
            {
                #region KeyframeReduction
                {
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Rotation Error", GUILayout.Width(150f));
                            EditorGUI.BeginChangeCheck();
                            var param = EditorGUILayout.FloatField(toolKeyframeReduction_RotationError, GUILayout.Width(100f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                                toolKeyframeReduction_RotationError = param;
                            }
                        }
                        EditorGUILayout.Space();
                        {
                            if (GUILayout.Button("Reset"))
                            {
                                Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                                toolKeyframeReduction_RotationError = 0.5f;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Position Error", GUILayout.Width(150f));
                            EditorGUI.BeginChangeCheck();
                            var param = EditorGUILayout.FloatField(toolKeyframeReduction_PositionError, GUILayout.Width(100f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                                toolKeyframeReduction_PositionError = param;
                            }
                        }
                        EditorGUILayout.Space();
                        {
                            if (GUILayout.Button("Reset"))
                            {
                                Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                                toolKeyframeReduction_PositionError = 0.5f;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Scale and Others Error", GUILayout.Width(150f));
                            EditorGUI.BeginChangeCheck();
                            var param = EditorGUILayout.FloatField(toolKeyframeReduction_ScaleAndOthersError, GUILayout.Width(100f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                                toolKeyframeReduction_ScaleAndOthersError = param;
                            }
                        }
                        EditorGUILayout.Space();
                        {
                            if (GUILayout.Button("Reset"))
                            {
                                Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                                toolKeyframeReduction_ScaleAndOthersError = 0.5f;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft(new GUIContent("Humanoid Curves", "Animator Type"), toolKeyframeReduction_EnableHumanoid);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                            toolKeyframeReduction_EnableHumanoid = flag;
                        }
                    }
                    if (toolKeyframeReduction_EnableHumanoid)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsKeyframeReductionRootAndIKGoalCurves), toolKeyframeReduction_EnableHumanoidRootAndIKGoal);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                            toolKeyframeReduction_EnableHumanoidRootAndIKGoal = flag;
                        }
                        EditorGUI.indentLevel--;
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft(new GUIContent("Generic Curves", "Transform Type"), toolKeyframeReduction_EnableGeneric);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                            toolKeyframeReduction_EnableGeneric = flag;
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft(new GUIContent("Other Curves", "Anything Type"), toolKeyframeReduction_EnableOther);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Keyframe Reduction setting");
                            toolKeyframeReduction_EnableOther = flag;
                        }
                    }
                    {
                        EditorGUI.BeginDisabledGroup(!toolKeyframeReduction_EnableHumanoid && !toolKeyframeReduction_EnableGeneric && !toolKeyframeReduction_EnableOther);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space();
                        if (GUILayout.Button("Reduction"))
                        {
                            ToolsKeyframeReduction(clip);
                        }
                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.EndDisabledGroup();
                    }
                }
                #endregion
            }
            else if (toolMode == ToolMode.EnsureQuaternionContinuity)
            {
                #region EnsureQuaternionContinuity
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Execute"))
                    {
                        ToolsEnsureQuaternionContinuity(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.Cleanup)
            {
                #region Cleanup
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorRootCurves), toolCleanup_RemoveRoot);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveRoot = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorIKGoalCurves), toolCleanup_RemoveIK);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveIK = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorTDOFCurves), toolCleanup_RemoveTDOF);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveTDOF = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorMotionCurves), toolCleanup_RemoveMotion);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveMotion = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorFingerCurves), toolCleanup_RemoveFinger);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveFinger = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorEyeCurves), toolCleanup_RemoveEyes);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveEyes = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorJawCurve), toolCleanup_RemoveJaw);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveJaw = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimatorToeCurves), toolCleanup_RemoveToes);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveToes = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveTransformPositionCurves), toolCleanup_RemoveTransformPosition);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveTransformPosition = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveTransformRotationCurves), toolCleanup_RemoveTransformRotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveTransformRotation = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveTransformScaleCurves), toolCleanup_RemoveTransformScale);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveTransformScale = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveBlendShapeCurves), toolCleanup_RemoveBlendShape);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveBlendShape = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveObjectReferenceCurves), toolCleanup_RemoveObjectReference);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveObjectReference = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAnimationEvents), toolCleanup_RemoveEvent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveEvent = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveMissingCurves), toolCleanup_RemoveMissing);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveMissing = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveHumanoidandGenericconflictCurves), toolCleanup_RemoveHumanoidConflict);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveHumanoidConflict = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveRootmotionconflictCurves), toolCleanup_RemoveRootMotionConflict);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveRootMotionConflict = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveUnnecessaryCurves), toolCleanup_RemoveUnnecessary);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveUnnecessary = flag;
                    }
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.ToolsCleanupRemoveAvatarMaskDisable), toolCleanup_RemoveAvatarMaskDisable);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Cleanup setting");
                        toolCleanup_RemoveAvatarMaskDisable = flag;
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        var mask = EditorGUILayout.ObjectField(toolCleanup_RemoveAvatarMask, typeof(AvatarMask), false) as AvatarMask;
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Cleanup setting");
                            toolCleanup_RemoveAvatarMask = mask;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUI.BeginDisabledGroup(!toolCleanup_RemoveRoot && !toolCleanup_RemoveIK && !toolCleanup_RemoveTDOF && !toolCleanup_RemoveMotion &&
                                                    !toolCleanup_RemoveFinger && !toolCleanup_RemoveEyes && !toolCleanup_RemoveJaw && !toolCleanup_RemoveToes &&
                                                    !toolCleanup_RemoveTransformPosition && !toolCleanup_RemoveTransformRotation && !toolCleanup_RemoveTransformScale && !toolCleanup_RemoveBlendShape &&
                                                    !toolCleanup_RemoveObjectReference && !toolCleanup_RemoveEvent && !toolCleanup_RemoveMissing && !toolCleanup_RemoveHumanoidConflict && !toolCleanup_RemoveRootMotionConflict && !toolCleanup_RemoveUnnecessary && !toolCleanup_RemoveAvatarMaskDisable);
                    if (GUILayout.Button("Cleanup"))
                    {
                        ToolsCleanup(clip);
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.FixErrors)
            {
                #region FixErrors
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Fix"))
                    {
                        ToolsFixErrors(clip);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.AdditiveReferencePose)
            {
                #region AdditiveReferencePose
                {
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.ToggleLeft("Has Additive Reference Pose", toolAdditiveReferencePose_Has);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change AdditiveReferencePose setting");
                            toolAdditiveReferencePose_Has = flag;
                        }
                    }
                    EditorGUI.BeginDisabledGroup(!toolAdditiveReferencePose_Has);
                    EditorGUI.indentLevel++;
                    {
                        EditorGUI.BeginChangeCheck();
                        var refClip = EditorGUILayout.ObjectField("Clip", toolAdditiveReferencePose_Clip, typeof(AnimationClip), true) as AnimationClip;
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change AdditiveReferencePose setting");
                            toolAdditiveReferencePose_Clip = refClip;
                        }
                    }
                    if (toolAdditiveReferencePose_Clip != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        var refTime = EditorGUILayout.Slider("Time", toolAdditiveReferencePose_Time, 0f, toolAdditiveReferencePose_Clip.length);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change AdditiveReferencePose setting");
                            toolAdditiveReferencePose_Time = refTime;
                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(toolAdditiveReferencePose_Has && toolAdditiveReferencePose_Clip == null);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Set Current Clip"))
                    {
                        ToolsAdditiveReferencePose(clip, false);
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Set All Clips"))
                    {
                        ToolsAdditiveReferencePose(clip, true);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }
                #endregion
            }
            else if (toolMode == ToolMode.AnimCompression)
            {
                #region AnimCompression
                {
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.Toggle(Language.GetContent(Language.Help.ToolsAnimCompressionCompressed), toolAnimCompression_Compressed);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Anim Compression setting");
                            toolAnimCompression_Compressed = flag;
                            if (toolAnimCompression_Compressed)
                                toolAnimCompression_UseHighQualityCurve = true;
                        }
                    }
                    {
                        EditorGUI.BeginDisabledGroup(clip.legacy);
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.Toggle(Language.GetContent(Language.Help.ToolsAnimCompressionUseHighQualityCurve), toolAnimCompression_UseHighQualityCurve);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Anim Compression setting");
                            toolAnimCompression_UseHighQualityCurve = flag;
                            if (!toolAnimCompression_UseHighQualityCurve)
                                toolAnimCompression_Compressed = false;
                        }
                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Set Current Clip"))
                    {
                        ToolsAnimCompression(clip, false);
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Set All Clips"))
                    {
                        ToolsAnimCompression(clip, true);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            else if (toolMode == ToolMode.Export)
            {
                #region Export
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.Toggle("Active Only", toolExport_ActiveOnly);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Export setting");
                        toolExport_ActiveOnly = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.Toggle("Export Mesh", toolExport_Mesh);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Export setting");
                        toolExport_Mesh = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var mode = (ExportAnimationMode)EditorGUILayout.EnumPopup("Export Animation", toolExport_AnimationMode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Export setting");
                        toolExport_AnimationMode = mode;
                    }
                }
                if (toolExport_AnimationMode != ExportAnimationMode.None)
                {
                    EditorGUILayout.LabelField("Bake");
                    EditorGUI.indentLevel++;
                    if (IsHuman)
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.Toggle(new GUIContent("Foot IK", "Activates feet IK bake."), toolExport_BakeFootIK);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Bake Flag");
                            toolExport_BakeFootIK = flag;
                        }
                    }
#if VERYANIMATION_ANIMATIONRIGGING
                    {
                        bool disableBake = !AnimationRigging.IsValid;
                        EditorGUI.BeginDisabledGroup(disableBake);
                        EditorGUI.BeginChangeCheck();
                        var flag = EditorGUILayout.Toggle("Animation Rigging", toolExport_BakeAnimationRigging);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Bake Flag");
                            toolExport_BakeAnimationRigging = flag;
                        }
                        EditorGUI.EndDisabledGroup();
                    }
#endif
                    EditorGUI.indentLevel--;
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Export"))
                    {
                        ToolsExport();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        public void DuplicateAndReplace()
        {
            var assetPath = string.Format("{0}/{1}.anim", EditorCommon.GetAssetPath(CurrentClip), CurrentClip.name);
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            string path = EditorUtility.SaveFilePanel("Duplicate", Path.GetDirectoryName(uniquePath), Path.GetFileName(uniquePath), "anim");
            if (string.IsNullOrEmpty(path))
                return;
            if (!path.StartsWith(Application.dataPath))
            {
                EditorCommon.SaveInsideAssetsFolderDisplayDialog();
                return;
            }
            else
            {
                try
                {
                    VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
                    path = FileUtil.GetProjectRelativePath(path);
                    AssetDatabase.CreateAsset(AnimationClip.Instantiate(CurrentClip), path);
                    var newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                    #region Extra
                    if (EditorUtility.DisplayDialog("Extra", Language.GetText(Language.Help.AnimationClipDuplicateandReplaceDialog), "Yes", "No"))
                    {
                        ToolsReductionCurve(newClip);

                        ToolsRotationCurveInterpolation(newClip, RotationCurveInterpolationMode.Quaternion);
                    }
                    #endregion

                    bool replaced = false;
                    if (UAw.GetLinkedWithTimeline())
                    {
                        #region Timeline
#if VERYANIMATION_TIMELINE
                        var timelineClip = UAw.GetTimelineAnimationClip();
                        if (timelineClip == CurrentClip)
                        {
                            UAw.SetTimelineAnimationClip(newClip, "Duplicate and Replace");
                            UAw.EditSequencerClip(UAw.GetTimelineClip());
                            replaced = true;
                        }
#else
                        Assert.IsTrue(false);
#endif
                        #endregion
                    }
                    else
                    {
                        #region Animator
                        if (VAW.Animator != null && VAW.Animator.runtimeAnimatorController != null)
                        {
                            var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                            #region AnimatorOverrideController
                            if (VAW.Animator.runtimeAnimatorController is AnimatorOverrideController)
                            {
                                var owc = VAW.Animator.runtimeAnimatorController as AnimatorOverrideController;
                                {
                                    List<KeyValuePair<AnimationClip, AnimationClip>> srcList = new();
                                    owc.GetOverrides(srcList);
                                    List<KeyValuePair<AnimationClip, AnimationClip>> dstList = new();
                                    bool changed = false;
                                    foreach (var pair in srcList)
                                    {
                                        if (pair.Key == CurrentClip || pair.Value == CurrentClip)
                                            changed = true;
                                        dstList.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key != CurrentClip ? pair.Key : newClip,
                                                                                                    pair.Value != CurrentClip ? pair.Value : newClip));
                                    }
                                    if (changed)
                                    {
                                        owc.ApplyOverrides(dstList);
                                        replaced = true;
                                    }
                                }
                            }
                            #endregion
                            #region AnimatorControllerLayer
                            if (ac != null)
                            {
                                var layers = ac.layers;
                                for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                                {
                                    if (animationMode == AnimationMode.Layers)
                                    {
                                        if (layerIndex != animationLayerIndex)
                                            continue;
                                    }

                                    void ReplaceStateMachine(AnimatorStateMachine stateMachine)
                                    {
                                        foreach (var state in stateMachine.states)
                                        {
                                            var motion = state.state.motion;
                                            if (layers[layerIndex].syncedLayerIndex >= 0)
                                                motion = layers[layerIndex].GetOverrideMotion(state.state);

                                            if (motion is UnityEditor.Animations.BlendTree)
                                            {
                                                Undo.RecordObject(state.state, "Duplicate and Replace");
                                                void ReplaceBlendTree(UnityEditor.Animations.BlendTree blendTree)
                                                {
                                                    if (blendTree.children == null) return;
                                                    Undo.RecordObject(blendTree, "Duplicate and Replace");
                                                    var children = blendTree.children;
                                                    for (int i = 0; i < children.Length; i++)
                                                    {
                                                        if (children[i].motion is UnityEditor.Animations.BlendTree)
                                                        {
                                                            ReplaceBlendTree(children[i].motion as UnityEditor.Animations.BlendTree);
                                                        }
                                                        else
                                                        {
                                                            if (children[i].motion == CurrentClip)
                                                            {
                                                                children[i].motion = newClip;
                                                                replaced = true;
                                                            }
                                                        }
                                                    }
                                                    blendTree.children = children;
                                                }

                                                ReplaceBlendTree(motion as UnityEditor.Animations.BlendTree);
                                            }
                                            else
                                            {
                                                if (motion == CurrentClip)
                                                {
                                                    if (layerIndex > 0 && layers[layerIndex].syncedLayerIndex >= 0)
                                                    {
                                                        Undo.RecordObject(ac, "Duplicate and Replace");
                                                        layers[layerIndex].SetOverrideMotion(state.state, newClip);
                                                    }
                                                    else
                                                    {
                                                        Undo.RecordObject(state.state, "Duplicate and Replace");
                                                        state.state.motion = newClip;
                                                    }
                                                    replaced = true;
                                                }
                                            }
                                        }
                                        foreach (var childStateMachine in stateMachine.stateMachines)
                                        {
                                            ReplaceStateMachine(childStateMachine.stateMachine);
                                        }
                                    }

                                    if (layers[layerIndex].syncedLayerIndex >= 0)
                                        ReplaceStateMachine(layers[layers[layerIndex].syncedLayerIndex].stateMachine);
                                    else
                                        ReplaceStateMachine(layers[layerIndex].stateMachine);
                                }
                                if (replaced)
                                    ac.layers = layers;
                            }
                            #endregion
                        }
                        #endregion
                        #region Animation
                        if (VAW.Animation != null)
                        {
                            Undo.RecordObject(VAW.Animation, "Duplicate and Replace");
                            bool changed = false;
                            var animations = AnimationUtility.GetAnimationClips(VAW.GameObject);
                            for (int i = 0; i < animations.Length; i++)
                            {
                                if (animations[i] == CurrentClip)
                                {
                                    animations[i] = newClip;
                                    changed = true;
                                }
                            }
                            if (VAW.Animation.clip == CurrentClip)
                            {
                                VAW.Animation.clip = newClip;
                                changed = true;
                            }
                            if (changed)
                            {
                                AnimationUtility.SetAnimationClips(VAW.Animation, animations);
                                replaced = true;
                            }
                        }
                        #endregion

                        if (replaced)
                            SetCurrentClip(newClip);
                    }

                    if (!replaced)
                        Debug.LogWarningFormat(Language.GetText(Language.Help.LogAnimationClipReferenceReplaceError), newClip);

                    ClearEditorCurveCache();
                    OnHierarchyWindowChanged();
                    SetUpdateSampleAnimation(true, true);
                    UAw.ForceRefresh();
                }
                finally
                {
                    VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
                }
            }
        }

        private void ToolsReset()
        {
            var lastFrame = GetLastFrame();
            var totalLastFrame = Mathf.RoundToInt(GetTotalClipLength() * CurrentClip.frameRate);
            toolCreateNewClip_FirstFrame = 0;
            toolCreateNewClip_LastFrame = totalLastFrame;
#if VERYANIMATION_TIMELINE
            if (UAw.GetLinkedWithTimeline())
            {
                var timelineClip = UAw.GetTimelineClip();
                if (timelineClip != null)
                {
                    var start = Mathf.RoundToInt((float)timelineClip.start * UAw.GetTimelineFrameRate());
                    var end = Mathf.RoundToInt((float)timelineClip.end * UAw.GetTimelineFrameRate());
                    toolCreateNewClip_FirstFrame = start;
                    toolCreateNewClip_LastFrame = end;
                }
            }
#endif
            toolCreateNewKeyframe_FirstFrame = 0;
            toolCreateNewKeyframe_LastFrame = lastFrame;
            toolBakeIK_FirstFrame = 0;
            toolBakeIK_LastFrame = lastFrame;
            toolHumanoidIK_FirstFrame = 0;
            toolHumanoidIK_LastFrame = lastFrame;
            toolAnimationRigging_FirstFrame = 0;
            toolAnimationRigging_LastFrame = totalLastFrame;
            toolCopy_FirstFrame = 0;
            toolCopy_LastFrame = lastFrame;
            toolCopy_WriteFrame = lastFrame + 1;
            toolTrim_FirstFrame = 0;
            toolTrim_LastFrame = lastFrame;
            if (CurrentClip != null)
            {
                var so = new SerializedObject(CurrentClip);
                {
                    var animationClipSettings = so.FindProperty("m_AnimationClipSettings");
                    var clip = animationClipSettings.FindPropertyRelative("m_AdditiveReferencePoseClip").objectReferenceValue;
                    toolAdditiveReferencePose_Clip = clip as AnimationClip;
                    toolAdditiveReferencePose_Time = animationClipSettings.FindPropertyRelative("m_AdditiveReferencePoseTime").floatValue;
                    toolAdditiveReferencePose_Has = animationClipSettings.FindPropertyRelative("m_HasAdditiveReferencePose").boolValue;
                }
                toolAnimCompression_Compressed = so.FindProperty("m_Compressed").boolValue;
                toolAnimCompression_UseHighQualityCurve = so.FindProperty("m_UseHighQualityCurve").boolValue;
            }

            ToolsParameterRelatedCurveReset();
        }
        private void ToolsParameterRelatedCurveReset()
        {
            toolParameterRelatedCurve_DataList = null;
            toolParameterRelatedCurve_Update = true;
            toolParameterRelatedCurve_List = null;
        }

        private void ParameterRelatedCurveUpdateList()
        {
            if (!toolParameterRelatedCurve_Update)
                return;

            void UpdateEnableFlagAll()
            {
                if (toolParameterRelatedCurve_DataList == null) return;
                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac == null) return;
                var parameters = ac.parameters;
                foreach (var data in toolParameterRelatedCurve_DataList)
                {
                    var binding = AnimationCurveBindingAnimatorCustom(data.propertyName);
                    var curve = GetEditorCurveCache(binding);
                    data.enableAnimationCurve = curve != null;
                    data.parameterIndex = ArrayUtility.FindIndex(parameters, (x) => x.name == data.propertyName);
                    data.enableAnimatorParameter = data.parameterIndex >= 0 && parameters[data.parameterIndex].type == UnityEngine.AnimatorControllerParameterType.Float;
                }
                SetUpdateSampleAnimation();
                SetAnimationWindowRefresh(AnimationWindowStateRefreshType.Everything);
            }
            void UpdateEnableFlag(int index)
            {
                if (toolParameterRelatedCurve_DataList == null) return;
                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac == null) return;
                var parameters = ac.parameters;
                {
                    var data = toolParameterRelatedCurve_DataList[index];
                    var binding = AnimationCurveBindingAnimatorCustom(data.propertyName);
                    var curve = GetEditorCurveCache(binding);
                    data.enableAnimationCurve = curve != null;
                    data.parameterIndex = ArrayUtility.FindIndex(parameters, (x) => x.name == data.propertyName);
                    data.enableAnimatorParameter = data.parameterIndex >= 0 && parameters[data.parameterIndex].type == UnityEngine.AnimatorControllerParameterType.Float;
                }
                SetUpdateSampleAnimation();
                SetAnimationWindowRefresh(AnimationWindowStateRefreshType.Everything);
            }

            if (toolParameterRelatedCurve_DataList == null)
                toolParameterRelatedCurve_DataList = new List<ParameterRelatedData>();
            else
                toolParameterRelatedCurve_DataList.Clear();
            {
                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac != null)
                {
                    var parameters = ac.parameters;
                    foreach (var binding in AnimationUtility.GetCurveBindings(CurrentClip))
                    {
                        if (binding.type != typeof(Animator)) continue;
                        if (IsAnimatorReservedPropertyName(binding.propertyName)) continue;
                        bool ready = false;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].type != UnityEngine.AnimatorControllerParameterType.Float) continue;
                            if (binding.propertyName == parameters[i].name)
                            {
                                toolParameterRelatedCurve_DataList.Add(new ParameterRelatedData()
                                {
                                    propertyName = binding.propertyName,
                                    parameterIndex = i,
                                    enableAnimationCurve = true,
                                    enableAnimatorParameter = true
                                });
                                ready = true;
                                break;
                            }
                        }
                        if (!ready)
                        {
                            toolParameterRelatedCurve_DataList.Add(new ParameterRelatedData()
                            {
                                propertyName = binding.propertyName,
                                parameterIndex = -1,
                                enableAnimationCurve = true,
                                enableAnimatorParameter = false,
                            });
                        }
                    }
                }
            }
            toolParameterRelatedCurve_List = new ReorderableList(toolParameterRelatedCurve_DataList, typeof(ParameterRelatedData), false, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    float x = rect.x;
                    {
                        const float Rate = 0.4f;
                        var r = rect;
                        r.x = x;
                        r.width = rect.width * Rate;
                        x += r.width;
                        EditorGUI.LabelField(r, "Name", VAW.GuiStyleCenterAlignLabel);
                    }
                    {
                        const float Rate = 0.2f;
                        var r = rect;
                        r.x = x;
                        r.width = rect.width * Rate;
                        x += r.width;
                        EditorGUI.LabelField(r, new GUIContent("Curve", "Animation Curve"), VAW.GuiStyleCenterAlignLabel);
                    }
                    {
                        const float Rate = 0.2f;
                        var r = rect;
                        r.x = x;
                        r.width = rect.width * Rate;
                        x += r.width;
                        EditorGUI.LabelField(r, new GUIContent("Parameter", "Animator Controller Parameter"), VAW.GuiStyleCenterAlignLabel);
                    }
                    {
                        const float Rate = 0.2f;
                        var r = rect;
                        r.x = x;
                        r.width = rect.width * Rate;
                        x += r.width;
                        EditorGUI.LabelField(r, "Value", VAW.GuiStyleCenterAlignLabel);
                    }
                }
            };
            toolParameterRelatedCurve_List.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= toolParameterRelatedCurve_DataList.Count)
                    return;

                EditorGUI.BeginDisabledGroup((CurrentClip.hideFlags & HideFlags.NotEditable) != HideFlags.None);

                float x = rect.x;
                {
                    const float Rate = 0.4f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (index == toolParameterRelatedCurve_List.index)
                    {
                        EditorGUI.BeginChangeCheck();
                        var text = EditorGUI.TextField(r, toolParameterRelatedCurve_DataList[index].propertyName);
                        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(text))
                        {
                            if (ToolsCommonBefore(CurrentClip, "Change Parameter Related Curve"))
                            {
                                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                                if (ac != null)
                                {
                                    {
                                        var origText = text;
                                        if (IsAnimatorReservedPropertyName(text))
                                            text += " 0";
                                        text = ac.MakeUniqueParameterName(text);
                                        if (origText != text)
                                        {
                                            Debug.LogWarningFormat(Language.GetText(Language.Help.LogParameterRelatedCurveNameChanged), origText, text);
                                        }
                                    }
                                    Undo.RecordObject(VAW, "Change Parameter Related Curve");
                                    Undo.RecordObject(ac, "Change Parameter Related Curve");
                                    {
                                        var binding = AnimationCurveBindingAnimatorCustom(toolParameterRelatedCurve_DataList[index].propertyName);
                                        var curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);
                                        if (curve != null)
                                        {
                                            AnimationUtility.SetEditorCurve(CurrentClip, binding, null);
                                            binding = AnimationCurveBindingAnimatorCustom(text);
                                            AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);
                                        }
                                    }
                                    {
                                        var parameters = ac.parameters;
                                        int paramIndex = toolParameterRelatedCurve_DataList[index].parameterIndex;
                                        if (paramIndex >= 0 && paramIndex < parameters.Length)
                                        {
                                            parameters[paramIndex].name = text;
                                            ac.parameters = parameters;
                                        }
                                    }
                                    toolParameterRelatedCurve_DataList[index].propertyName = text;
                                    UpdateEnableFlag(index);
                                }
                            }
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(r, toolParameterRelatedCurve_DataList[index].propertyName);
                    }
                }
                {
                    const float Rate = 0.2f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (toolParameterRelatedCurve_DataList[index].enableAnimationCurve)
                        EditorGUI.LabelField(r, "Ready", VAW.GuiStyleCenterAlignLabel);
                    else
                        EditorGUI.LabelField(r, "Missing", VAW.GuiStyleCenterAlignYellowLabel);
                }
                {
                    const float Rate = 0.2f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (toolParameterRelatedCurve_DataList[index].enableAnimatorParameter)
                        EditorGUI.LabelField(r, "Ready", VAW.GuiStyleCenterAlignLabel);
                    else
                        EditorGUI.LabelField(r, "Missing", VAW.GuiStyleCenterAlignYellowLabel);
                }
                {
                    const float Rate = 0.2f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (!toolParameterRelatedCurve_DataList[index].enableAnimationCurve || !toolParameterRelatedCurve_DataList[index].enableAnimatorParameter)
                    {
                        if (GUI.Button(r, "Fix"))
                        {
                            if (ToolsCommonBefore(CurrentClip, "Fix Parameter Related Curve"))
                            {
                                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                                if (ac != null)
                                {
                                    Undo.RecordObject(VAW, "Fix Parameter Related Curve");
                                    Undo.RecordObject(ac, "Fix Parameter Related Curve");
                                    if (!toolParameterRelatedCurve_DataList[index].enableAnimationCurve)
                                    {
                                        var binding = AnimationCurveBindingAnimatorCustom(toolParameterRelatedCurve_DataList[index].propertyName);
                                        var curve = new AnimationCurve();
                                        SetKeyframeTangentModeLinear(curve, curve.AddKey(0f, 0f));
                                        SetKeyframeTangentModeLinear(curve, curve.AddKey(CurrentClip.length, 1f));
                                        AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);
                                    }
                                    if (!toolParameterRelatedCurve_DataList[index].enableAnimatorParameter)
                                    {
                                        {
                                            var parameters = ac.parameters;
                                            var paramIndex = ArrayUtility.FindIndex(parameters, (d) => d.name == toolParameterRelatedCurve_DataList[index].propertyName);
                                            if (paramIndex >= 0 && paramIndex < parameters.Length)
                                            {
                                                ac.RemoveParameter(paramIndex);
                                            }
                                        }
                                        ac.AddParameter(toolParameterRelatedCurve_DataList[index].propertyName, UnityEngine.AnimatorControllerParameterType.Float);
                                    }
                                    UpdateEnableFlag(index);
                                }
                            }
                        }
                    }
                    else if (UAvatarPreview != null)
                    {
                        var binding = AnimationCurveBindingAnimatorCustom(toolParameterRelatedCurve_DataList[index].propertyName);
                        var curve = GetEditorCurveCache(binding);
                        if (curve != null)
                        {
                            var value = curve.Evaluate(UAvatarPreview.GetTime());
                            EditorGUI.LabelField(r, value.ToString("F2"), VAW.GuiStyleCenterAlignLabel);
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();
            };
            toolParameterRelatedCurve_List.onSelectCallback = list =>
            {
                UpdateEnableFlagAll();
                EditorApplication.delayCall += () =>
                {
                    SetAnimationWindowSynchroSelection(new EditorCurveBinding[] { AnimationCurveBindingAnimatorCustom(toolParameterRelatedCurve_DataList[list.index].propertyName) });
                    VAW.Repaint();
                };
            };
            toolParameterRelatedCurve_List.onAddCallback = list =>
            {
                if (!ToolsCommonBefore(CurrentClip, "Add Parameter Related Curve")) return;

                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                if (ac == null)
                    return;

                Undo.RecordObject(VAW, "Add Parameter Related Curve");
                {
                    var name = ac.MakeUniqueParameterName("New Parameter");
                    var data = new ParameterRelatedData() { propertyName = name };
                    {
                        var binding = AnimationCurveBindingAnimatorCustom(name);
                        var curve = new AnimationCurve();
                        SetKeyframeTangentModeLinear(curve, curve.AddKey(0f, 0f));
                        SetKeyframeTangentModeLinear(curve, curve.AddKey(CurrentClip.length, 1f));
                        AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);
                    }
                    {
                        ac.AddParameter(name, UnityEngine.AnimatorControllerParameterType.Float);
                    }
                    toolParameterRelatedCurve_DataList.Add(data);
                }
                toolParameterRelatedCurve_Update = true;
                EditorApplication.delayCall += () =>
                {
                    toolParameterRelatedCurve_List.index = toolParameterRelatedCurve_DataList.Count - 1;
                    SetAnimationWindowSynchroSelection(new EditorCurveBinding[] { AnimationCurveBindingAnimatorCustom(toolParameterRelatedCurve_DataList[toolParameterRelatedCurve_List.index].propertyName) });
                    VAW.Repaint();
                };

                ToolsCommonAfter();
                InternalEditorUtility.RepaintAllViews();
            };
            toolParameterRelatedCurve_List.onCanAddCallback = list =>
            {
                var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                return (ac != null);
            };
            toolParameterRelatedCurve_List.onRemoveCallback = list =>
            {
                if (!ToolsCommonBefore(CurrentClip, "Add Parameter Related Curve")) return;

                Undo.RecordObject(VAW, "Add Parameter Related Curve");
                {
                    var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                    if (ac != null)
                    {
                        var data = toolParameterRelatedCurve_DataList[list.index];
                        {
                            var binding = AnimationCurveBindingAnimatorCustom(data.propertyName);
                            AnimationUtility.SetEditorCurve(CurrentClip, binding, null);
                        }
                        {
                            var parameters = ac.parameters;
                            data.parameterIndex = ArrayUtility.FindIndex(parameters, (x) => x.name == data.propertyName);
                            if (data.parameterIndex >= 0)
                                ac.RemoveParameter(data.parameterIndex);
                        }
                    }
                }
                toolParameterRelatedCurve_DataList.RemoveAt(list.index);
                toolParameterRelatedCurve_Update = true;

                ToolsCommonAfter();
                InternalEditorUtility.RepaintAllViews();
            };

            UpdateEnableFlagAll();
            toolParameterRelatedCurve_Update = false;
        }

        private void ToolsCurvesWasModifiedStoppedSmoothTangents(float beginTime, float endTime)
        {
            foreach (var pair in curvesWasModifiedStopped)
            {
                if (pair.Value.deleted != AnimationUtility.CurveModifiedType.CurveModified)
                    continue;
                var curve = GetEditorCurveCache(pair.Value.binding);
                if (curve == null)
                    continue;

                for (int i = 0; i < curve.length; i++)
                {
                    if (curve[i].time >= beginTime && curve[i].time <= endTime)
                        curve.SmoothTangents(i, 0f);
                }
                SetEditorCurveCache(pair.Value.binding, curve);
            }
        }

        private bool ToolsCommonBefore(AnimationClip clip, string undoName)
        {
            if (!BeginChangeAnimationCurve(clip, undoName))
                return false;

            UAw.ClearKeySelections();
            ClearEditorCurveCache();
            SetOnCurveWasModifiedStop(true);

            return true;
        }
        private void ToolsCommonAfter()
        {
            ResetOnCurveWasModifiedStop();
            UpdateSyncEditorCurveClip();
            curvesWasModified?.Clear();
            humanoidFootIK?.Clear();
            ClearEditorCurveCache();
            SetUpdateSampleAnimation(true, true);
            SetAnimationWindowSynchroSelection();
            ResetUpdateIKtargetAll();
            SetSynchroIKtargetAll();
            UAw.ForceRefresh();
        }

        private void ToolsReductionCurve(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Reduction Curve")) return;

            try
            {
                bool allWriteDefaults = true;
                if (UAw.GetLinkedWithTimeline())
                {
                    allWriteDefaults = false;
                }
                else
                {
                    ActionAllAnimatorState(clip, (animatorState) =>
                    {
                        if (!animatorState.writeDefaultValues)
                            allWriteDefaults = false;
                    });
                }

                #region It is not necessary if AnimatorState.writeDefaultValues is enabled
                if (allWriteDefaults)
                {
                    const float eps = 0.0001f;

                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    int progressIndex = 0;
                    int progressTotal = bindings.Length;

                    bool[] doneFlags = new bool[bindings.Length];
                    for (int k = 0; k < bindings.Length; k++)
                    {
                        EditorUtility.DisplayProgressBar("Reduction Curve", string.IsNullOrEmpty(bindings[k].path) ? bindings[k].propertyName : bindings[k].path, progressIndex++ / (float)progressTotal);
                        if (doneFlags[k]) continue;
                        doneFlags[k] = true;
                        var curve = AnimationUtility.GetEditorCurve(clip, bindings[k]);
                        if (curve == null) continue;
                        var t = GetTransformFromPath(bindings[k].path);
                        if (t == null) continue;
                        if (bindings[k].type == typeof(Animator))
                        {
                            #region Animator
                            if (GetMuscleIndexFromCurveBinding(bindings[k]) >= 0)
                            {
                                bool remove = true;
                                for (float time = 0f; time <= clip.length; time += 1f / clip.frameRate)
                                {
                                    if (Mathf.Abs(curve.Evaluate(time)) >= eps)
                                    {
                                        remove = false;
                                        break;
                                    }
                                }
                                if (remove)
                                {
                                    AnimationUtility.SetEditorCurve(clip, bindings[k], null);
                                }
                            }
                            #endregion
                        }
                        else if (bindings[k].type == typeof(Transform))
                        {
                            #region Transform
                            string[] TypeNames =
                            {
                                "m_LocalPosition",
                                "m_LocalRotation",
                                "m_LocalScale",
                                "localEulerAnglesRaw",
                            };
                            int type = -1;
                            for (int i = 0; i < TypeNames.Length; i++)
                            {
                                if (bindings[k].propertyName.StartsWith(TypeNames[i]))
                                {
                                    type = i;
                                    break;
                                }
                            }
                            var boneIndex = GetBoneIndexFromCurveBinding(bindings[k]);
                            if (type >= 0 && boneIndex >= 0)
                            {
                                var save = TransformPoseSave.GetOriginalTransform(Bones[boneIndex].transform);
                                if (save != null)
                                {
                                    int dofCount = type == 1 ? 4 : 3;
                                    bool remove = true;
                                    int[] indexes = new int[dofCount];
                                    for (int dof = 0; dof < dofCount; dof++)
                                    {
                                        indexes[dof] = ArrayUtility.FindIndex(bindings, (x) => x.type == bindings[k].type && x.path == bindings[k].path &&
                                                                                                x.propertyName == TypeNames[type] + DofIndex2String[dof]);
                                        if (indexes[dof] >= 0)
                                            doneFlags[indexes[dof]] = true;
                                        if (remove && indexes[dof] >= 0)
                                        {
                                            curve = AnimationUtility.GetEditorCurve(clip, bindings[indexes[dof]]);
                                            if (curve != null)
                                            {
                                                float saveValue = 0f;
                                                switch (type)
                                                {
                                                    case 0: saveValue = save.localPosition[dof]; break;
                                                    case 1: saveValue = save.localRotation[dof]; break;
                                                    case 2: saveValue = save.localScale[dof]; break;
                                                    case 3: saveValue = save.localRotation.eulerAngles[dof]; break;
                                                }
                                                for (float time = 0f; time <= clip.length; time += 1f / clip.frameRate)
                                                {
                                                    if (Mathf.Abs(curve.Evaluate(time) - saveValue) >= eps)
                                                    {
                                                        remove = false;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (remove)
                                    {
                                        foreach (var index in indexes)
                                        {
                                            if (index >= 0)
                                                AnimationUtility.SetEditorCurve(clip, bindings[index], null);
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                        else if (bindings[k].type == typeof(SkinnedMeshRenderer))
                        {
                            #region SkinnedMeshRenderer
                            var renderer = t.GetComponent<SkinnedMeshRenderer>();
                            if (renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                            {
                                if (IsBlendShapePropertyName(bindings[k].propertyName))
                                {
                                    var name = PropertyName2BlendShapeName(bindings[k].propertyName);
                                    if (BlendShapeWeightSave.IsHaveOriginalWeight(renderer, name))
                                    {
                                        var weight = BlendShapeWeightSave.GetOriginalWeight(renderer, name);
                                        bool remove = true;
                                        for (float time = 0f; time <= clip.length; time += 1f / clip.frameRate)
                                        {
                                            if (Mathf.Abs(curve.Evaluate(time) - weight) >= eps)
                                            {
                                                remove = false;
                                                break;
                                            }
                                        }
                                        if (remove)
                                        {
                                            AnimationUtility.SetEditorCurve(clip, bindings[k], null);
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                #region Optional bone
                {
                    void RemoveMuscleCurve(HumanBodyBones hi)
                    {
                        for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                        {
                            var mi = HumanTrait.MuscleFromBone((int)hi, dofIndex);
                            if (mi < 0) continue;
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMuscle(mi), null);
                        }
                    }
                    if (!IsHuman || !HumanoidHasLeftHand)
                    {
                        for (var hi = HumanBodyBones.LeftThumbProximal; hi <= HumanBodyBones.LeftLittleDistal; hi++)
                            RemoveMuscleCurve(hi);
                    }
                    if (!IsHuman || !HumanoidHasRightHand)
                    {
                        for (var hi = HumanBodyBones.RightThumbProximal; hi <= HumanBodyBones.RightLittleDistal; hi++)
                            RemoveMuscleCurve(hi);
                    }
                    if (!IsHuman || !HumanoidHasTDoF)
                    {
                        for (int tdofIndex = 0; tdofIndex < (int)AnimatorTDOFIndex.Total; tdofIndex++)
                        {
                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorTDOF((AnimatorTDOFIndex)tdofIndex, dofIndex), null);
                        }
                    }
                    void RemoveNoneMuscleCurve(HumanBodyBones hi)
                    {
                        if (!IsHuman || HumanoidBones[(int)hi] == null)
                            RemoveMuscleCurve(hi);
                    }
                    RemoveNoneMuscleCurve(HumanBodyBones.LeftEye);
                    RemoveNoneMuscleCurve(HumanBodyBones.RightEye);
                    RemoveNoneMuscleCurve(HumanBodyBones.Jaw);
                    RemoveNoneMuscleCurve(HumanBodyBones.LeftToes);
                    RemoveNoneMuscleCurve(HumanBodyBones.RightToes);
                }
                #endregion

                #region GenericRootMotion
                if (!IsHuman)
                {
                    if (RootMotionBoneIndex >= 0)
                    {
                        if (IsHaveAnimationCurveTransformPosition(0))
                        {
                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingTransformPosition(0, dofIndex), null);
                        }
                        if (GetHaveAnimationCurveTransformRotationMode(0) != URotationCurveInterpolation.Mode.Undefined)
                        {
                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingTransformRotation(0, dofIndex, URotationCurveInterpolation.Mode.RawEuler), null);
                            for (int dofIndex = 0; dofIndex < 4; dofIndex++)
                                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingTransformRotation(0, dofIndex, URotationCurveInterpolation.Mode.RawQuaternions), null);
                        }
                    }
                    else
                    {
                        if (IsHaveAnimationCurveAnimatorRootT())
                        {
                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootT[dofIndex], null);
                        }
                        if (IsHaveAnimationCurveAnimatorRootQ())
                        {
                            for (int dofIndex = 0; dofIndex < 4; dofIndex++)
                                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootQ[dofIndex], null);
                        }
                    }
                }
                #endregion
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private bool ToolsFixOverRotationCurve(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Fix Over Rotation Curve")) return false;

            try
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                int progressIndex = 0;
                int progressTotal = bindings.Length;
                #region CurveBindings
                foreach (var binding in bindings)
                {
                    EditorUtility.DisplayProgressBar("Fix Over Rotation Curve", string.IsNullOrEmpty(binding.path) ? binding.propertyName : binding.path, progressIndex++ / (float)progressTotal);
                    if (!IsTransformRotationCurveBinding(binding) || URotationCurveInterpolation.GetModeFromCurveData(binding) != URotationCurveInterpolation.Mode.RawEuler) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null) continue;
                    bool update = false;
                    for (int i = 1; i < curve.length; i++)
                    {
                        var power = curve[i].value - curve[i - 1].value;
                        if (Mathf.Abs(power) < 180f) continue;
                        var time = UAw.SnapToFrame(Mathf.Lerp(curve[i].time, curve[i - 1].time, 0.5f), clip.frameRate);
                        if (Mathf.Approximately(time, curve[i].time) || Mathf.Approximately(time, curve[i - 1].time)) continue;
                        AddKeyframe(curve, time, curve.Evaluate(time));
                        update = true;
                        i = 0;
                    }
                    if (update)
                    {
                        AnimationUtility.SetEditorCurve(clip, binding, curve);
                        Debug.LogWarningFormat(Language.GetText(Language.Help.LogFixOverRotationCurve), binding.path, binding.propertyName);
                    }
                }
                #endregion
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();

            return true;
        }

        private struct IKDataSave
        {
            public IKDataSave(AnimatorIKCore.AnimatorIKData ikData)
            {
                position = ikData.position;
                rotation = ikData.rotation;
                worldPosition = ikData.WorldPosition;
                worldRotation = ikData.WorldRotation;
                swivelRotation = ikData.swivelRotation;
                swivelPosition = ikData.swivelPosition;
            }
            public IKDataSave(OriginalIKCore.OriginalIKData ikData)
            {
                position = ikData.position;
                rotation = ikData.rotation;
                worldPosition = ikData.WorldPosition;
                worldRotation = ikData.WorldRotation;
                swivelRotation = ikData.swivel;
                swivelPosition = ikData.WorldPosition;
            }
            public readonly void Set(AnimatorIKCore.AnimatorIKData ikData)
            {
                if (ikData.spaceType == AnimatorIKCore.AnimatorIKData.SpaceType.Parent)
                {
                    ikData.position = position;
                    ikData.rotation = rotation;
                }
                else
                {
                    ikData.WorldPosition = worldPosition;
                    ikData.WorldRotation = worldRotation;
                }
                ikData.swivelRotation = swivelRotation;
                ikData.swivelPosition = swivelPosition;
            }
            public readonly void Set(OriginalIKCore.OriginalIKData ikData)
            {
                if (ikData.spaceType == OriginalIKCore.OriginalIKData.SpaceType.Parent)
                {
                    ikData.position = position;
                    ikData.rotation = rotation;
                }
                else
                {
                    ikData.WorldPosition = worldPosition;
                    ikData.WorldRotation = worldRotation;
                }
                ikData.swivel = swivelRotation;
            }

            public static IKDataSave Lerp(IKDataSave a, IKDataSave b, float t)
            {
                var ikDataSave = new IKDataSave()
                {
                    position = Vector3.Lerp(a.position, b.position, t),
                    rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
                    worldPosition = Vector3.Lerp(a.worldPosition, b.worldPosition, t),
                    worldRotation = Quaternion.Slerp(a.worldRotation, b.worldRotation, t),
                    swivelPosition = Vector3.Lerp(a.swivelPosition, b.swivelPosition, t),
                };
                if (Mathf.Abs(a.swivelRotation - b.swivelRotation) > 180f)
                {
                    var aSwivel = a.swivelRotation;
                    if (aSwivel < 0f) aSwivel += 360f;
                    var bSwivel = b.swivelRotation;
                    if (bSwivel < 0f) bSwivel += 360f;
                    ikDataSave.swivelRotation = Mathf.Lerp(aSwivel, bSwivel, t);
                    while (ikDataSave.swivelRotation < -180f || ikDataSave.swivelRotation > 180f)
                    {
                        if (ikDataSave.swivelRotation > 180f)
                            ikDataSave.swivelRotation -= 360f;
                        else if (ikDataSave.swivelRotation < -180f)
                            ikDataSave.swivelRotation += 360f;
                    }
                }
                else
                {
                    ikDataSave.swivelRotation = Mathf.Lerp(a.swivelRotation, b.swivelRotation, t);
                }
                return ikDataSave;
            }

            public Vector3 position;
            public Quaternion rotation;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public float swivelRotation;
            public Vector3 swivelPosition;
        }
        private void ToolsGenarateBakeIK(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Bake IK")) return;

            if (clip != CurrentClip) return;

            var saveCurrentTime = UAw.GetCurrentTime();
            try
            {
                var beginTime = UAw.SnapToFrame(toolBakeIK_FirstFrame >= 0 ? toolBakeIK_FirstFrame / clip.frameRate : 0f, clip.frameRate);
                var endTime = UAw.SnapToFrame(toolBakeIK_LastFrame >= 0 ? toolBakeIK_LastFrame / clip.frameRate : clip.length, clip.frameRate);
                int progressIndex = 0;
                int progressTotal = 2;

                #region AnimatorIK
                EditorUtility.DisplayProgressBar("Bake IK", "Animator IK", progressIndex++ / (float)progressTotal);
                if (IsHuman && animatorIK.ikData.Any(data => data.enable))
                {
                    #region Save
                    IKDataSave[,] ikDataAllSave = null;
                    IKDataSave[] ikDataBeginSave = null, ikDataEndSave = null;
                    if (toolBakeIK_Mode == BakeIKMode.Simple)
                    {
                        ikDataAllSave = new IKDataSave[animatorIK.ikData.Length, GetLastFrame() + 1];
                        for (int frame = toolBakeIK_FirstFrame; frame <= toolBakeIK_LastFrame; frame++)
                        {
                            var time = UAw.GetFrameTime(frame, clip);
                            SetCurrentTimeAndSampleAnimation(time);
                            for (var index = 0; index < animatorIK.ikData.Length; index++)
                            {
                                if (!animatorIK.ikData[index].enable) continue;
                                animatorIK.SynchroSet((AnimatorIKCore.IKTarget)index);
                                ikDataAllSave[index, frame] = new IKDataSave(animatorIK.ikData[index]);
                            }
                        }
                    }
                    else if (toolBakeIK_Mode == BakeIKMode.Interpolation)
                    {
                        ikDataBeginSave = new IKDataSave[animatorIK.ikData.Length];
                        ikDataEndSave = new IKDataSave[animatorIK.ikData.Length];
                        SetCurrentTimeAndSampleAnimation(beginTime);
                        for (var index = 0; index < animatorIK.ikData.Length; index++)
                        {
                            if (!animatorIK.ikData[index].enable) continue;
                            animatorIK.SynchroSet((AnimatorIKCore.IKTarget)index);
                            ikDataBeginSave[index] = new IKDataSave(animatorIK.ikData[index]);
                        }
                        SetCurrentTimeAndSampleAnimation(endTime);
                        for (var index = 0; index < animatorIK.ikData.Length; index++)
                        {
                            if (!animatorIK.ikData[index].enable) continue;
                            animatorIK.SynchroSet((AnimatorIKCore.IKTarget)index);
                            ikDataEndSave[index] = new IKDataSave(animatorIK.ikData[index]);
                        }
                    }
                    #endregion

                    ResetAnimatorRootCorrection();
                    for (int frame = toolBakeIK_FirstFrame; frame <= toolBakeIK_LastFrame; frame++)
                    {
                        var time = UAw.GetFrameTime(frame, clip);

                        SetCurrentTimeAndSampleAnimation(time);

                        if (toolBakeIK_Mode == BakeIKMode.Simple)
                        {
                            #region Simple
                            for (var index = 0; index < animatorIK.ikData.Length; index++)
                            {
                                if (!animatorIK.ikData[index].enable) continue;
                                ikDataAllSave[index, frame].Set(animatorIK.ikData[index]);
                                SetUpdateIKtargetAnimatorIK((AnimatorIKCore.IKTarget)index);
                            }
                            #endregion
                        }
                        else if (toolBakeIK_Mode == BakeIKMode.Interpolation)
                        {
                            #region Interpolation
                            float rate = 0f;
                            if (toolBakeIK_LastFrame - toolBakeIK_FirstFrame > 0)
                                rate = (frame - toolBakeIK_FirstFrame) / (float)(toolBakeIK_LastFrame - toolBakeIK_FirstFrame);

                            for (var index = 0; index < animatorIK.ikData.Length; index++)
                            {
                                if (!animatorIK.ikData[index].enable) continue;
                                var ikDataSave = IKDataSave.Lerp(ikDataBeginSave[index], ikDataEndSave[index], rate);
                                ikDataSave.Set(animatorIK.ikData[index]);
                                SetUpdateIKtargetAnimatorIK((AnimatorIKCore.IKTarget)index);
                            }
                            #endregion
                        }

                        EnableAnimatorRootCorrection(time, time, time);
                        UpdateAnimatorRootCorrection();
                        animatorIK.UpdateIK(false);
                        ResetAnimatorRootCorrection();
                        ResetUpdateIKtargetAll();
                    }
                    for (int frame = toolBakeIK_FirstFrame; frame <= toolBakeIK_LastFrame; frame++)
                    {
                        var time = UAw.GetFrameTime(frame, clip);
                        EnableAnimatorRootCorrection(time, time, time);
                    }
                    UpdateAnimatorRootCorrection();
                    ResetAnimatorRootCorrection();
                }
                #endregion
                #region OriginalIK
                EditorUtility.DisplayProgressBar("Bake IK", "Original IK", progressIndex++ / (float)progressTotal);
                if (originalIK.ikData.Any(data => data.enable))
                {
                    #region Save
                    IKDataSave[,] ikDataAllSave = null;
                    IKDataSave[] ikDataBeginSave = null, ikDataEndSave = null;
                    if (toolBakeIK_Mode == BakeIKMode.Simple)
                    {
                        ikDataAllSave = new IKDataSave[originalIK.ikData.Count, GetLastFrame() + 1];
                        for (int frame = toolBakeIK_FirstFrame; frame <= toolBakeIK_LastFrame; frame++)
                        {
                            var time = UAw.GetFrameTime(frame, clip);
                            SetCurrentTimeAndSampleAnimation(time);
                            for (var index = 0; index < originalIK.ikData.Count; index++)
                            {
                                if (!originalIK.ikData[index].enable) continue;
                                originalIK.SynchroSet(index);
                                ikDataAllSave[index, frame] = new IKDataSave(originalIK.ikData[index]);
                            }
                        }
                    }
                    else if (toolBakeIK_Mode == BakeIKMode.Interpolation)
                    {
                        ikDataBeginSave = new IKDataSave[originalIK.ikData.Count];
                        ikDataEndSave = new IKDataSave[originalIK.ikData.Count];
                        SetCurrentTimeAndSampleAnimation(beginTime);
                        for (var index = 0; index < originalIK.ikData.Count; index++)
                        {
                            if (!originalIK.ikData[index].enable) continue;
                            originalIK.SynchroSet(index);
                            ikDataBeginSave[index] = new IKDataSave(originalIK.ikData[index]);
                        }
                        SetCurrentTimeAndSampleAnimation(endTime);
                        for (var index = 0; index < originalIK.ikData.Count; index++)
                        {
                            if (!originalIK.ikData[index].enable) continue;
                            originalIK.SynchroSet(index);
                            ikDataEndSave[index] = new IKDataSave(originalIK.ikData[index]);
                        }
                    }
                    #endregion

                    for (int frame = toolBakeIK_FirstFrame; frame <= toolBakeIK_LastFrame; frame++)
                    {
                        var time = UAw.GetFrameTime(frame, clip);

                        SetCurrentTimeAndSampleAnimation(time);

                        if (toolBakeIK_Mode == BakeIKMode.Simple)
                        {
                            #region Simple
                            for (var index = 0; index < originalIK.ikData.Count; index++)
                            {
                                if (!originalIK.ikData[index].enable) continue;
                                ikDataAllSave[index, frame].Set(originalIK.ikData[index]);
                                SetUpdateIKtargetOriginalIK(index);
                            }
                            #endregion
                        }
                        else if (toolBakeIK_Mode == BakeIKMode.Interpolation)
                        {
                            #region Interpolation
                            float rate = 0f;
                            if (toolBakeIK_LastFrame - toolBakeIK_FirstFrame > 0)
                                rate = (frame - toolBakeIK_FirstFrame) / (float)(toolBakeIK_LastFrame - toolBakeIK_FirstFrame);

                            for (var index = 0; index < originalIK.ikData.Count; index++)
                            {
                                if (!originalIK.ikData[index].enable) continue;
                                var ikDataSave = IKDataSave.Lerp(ikDataBeginSave[index], ikDataEndSave[index], rate);
                                ikDataSave.Set(originalIK.ikData[index]);
                                SetUpdateIKtargetOriginalIK(index);
                            }
                            #endregion
                        }

                        originalIK.UpdateIK();
                        ResetUpdateIKtargetAll();
                    }
                }
                #endregion

                ToolsCurvesWasModifiedStoppedSmoothTangents(beginTime, endTime);
            }
            finally
            {
                SetCurrentTime(saveCurrentTime);
                ResetUpdateIKtargetAll();
                SetSynchroIKtargetAll();
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsGenarateAnimationRigging(AnimationClip clip)
        {
#if VERYANIMATION_ANIMATIONRIGGING
            if (!ToolsCommonBefore(clip, "AnimationRigging")) return;

            Assert.IsTrue(clip == CurrentClip);

            List<EditorCurveBinding> afterSyncBindings = new();

            var saveTime = CurrentTime;
            var saveRigLayerActive = VAW.VA.AnimationRigging.RigLayer.active;
            try
            {
                VAW.VA.AnimationRigging.RigLayer.active = false;

                SetCurrentTime(UAw.GetFrameTime(toolAnimationRigging_FirstFrame, clip));
                var firstTime = UAw.GetFrameTime(toolAnimationRigging_FirstFrame, clip);
                var lastTime = UAw.GetFrameTime(toolAnimationRigging_LastFrame, clip);

                if (toolAnimationRigging_RootMotionCancel)
                {
                    var root = VAW.GameObject.transform;

                    SetCurrentTimeAndSampleAnimation(0f);
#if UNITY_2022_3_OR_NEWER
                    root.GetPositionAndRotation(out var zeroPosition, out var zeroRotation);
#else
                    var zeroPosition = root.position;
                    var zeroRotation = root.rotation;
#endif
                    for (int frame = toolAnimationRigging_FirstFrame; frame <= toolAnimationRigging_LastFrame; frame++)
                    {
                        EditorUtility.DisplayProgressBar("Genarate AnimationRigging Curves", frame.ToString(), (frame - toolAnimationRigging_FirstFrame) / (float)(toolAnimationRigging_LastFrame - toolAnimationRigging_FirstFrame));

                        var time = UAw.GetFrameTime(frame, clip);
                        SetCurrentTimeAndSampleAnimation(time);
                        var position = AnimationRigging.ArRig.transform.parent.worldToLocalMatrix.MultiplyPoint3x4(zeroPosition);
                        var rotation = Quaternion.Inverse(AnimationRigging.ArRig.transform.parent.rotation) * zeroRotation;

                        for (var target = 0; target < animatorIK.ikData.Length; target++)
                        {
                            var data = animatorIK.ikData[target];
                            if (!data.enable || data.rigConstraint == null)
                                continue;

                            var boneIndex = BonesIndexOf(animatorIK.ikData[target].rigConstraint.gameObject);
                            SetAnimationValueTransformPosition(boneIndex, position, time);
                            SetAnimationValueTransformRotation(boneIndex, rotation, time);
                        }
                    }
                    for (var target = 0; target < animatorIK.ikData.Length; target++)
                    {
                        var data = animatorIK.ikData[target];
                        if (!data.enable || data.rigConstraint == null)
                            continue;

                        var boneIndex = BonesIndexOf(animatorIK.ikData[target].rigConstraint.gameObject);
                        for (int dof = 0; dof < 3; dof++)
                            afterSyncBindings.Add(AnimationCurveBindingTransformPosition(boneIndex, dof));
                        for (int dof = 0; dof < 4; dof++)
                            afterSyncBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                        for (int dof = 0; dof < 3; dof++)
                            afterSyncBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));
                    }
                }

                for (int frame = toolAnimationRigging_FirstFrame; frame <= toolAnimationRigging_LastFrame; frame++)
                {
                    EditorUtility.DisplayProgressBar("Genarate AnimationRigging Curves", frame.ToString(), (frame - toolAnimationRigging_FirstFrame) / (float)(toolAnimationRigging_LastFrame - toolAnimationRigging_FirstFrame));

                    var time = UAw.GetFrameTime(frame, clip);
                    SetCurrentTimeAndSampleAnimation(time);

                    for (var target = 0; target < animatorIK.ikData.Length; target++)
                    {
                        var data = animatorIK.ikData[target];
                        if (!data.enable || data.rigConstraint == null)
                            continue;

                        var syncFlags = AnimatorIKCore.SynchroSetFlags.None;
                        {
                            if (data.defaultSyncType == AnimatorIKCore.AnimatorIKData.SyncType.SceneObject)
                            {
                                syncFlags |= AnimatorIKCore.SynchroSetFlags.SceneObject;
                            }
                            if (data.defaultSyncType == AnimatorIKCore.AnimatorIKData.SyncType.HumanoidIK)
                            {
                                syncFlags |= AnimatorIKCore.SynchroSetFlags.HumanoidIK;
                            }
                        }
                        animatorIK.SynchroSet((AnimatorIKCore.IKTarget)target, syncFlags);

                        animatorIK.WriteAnimationRiggingConstraint((AnimatorIKCore.IKTarget)target, time);
                    }
                }
                for (var target = 0; target < animatorIK.ikData.Length; target++)
                {
                    var data = animatorIK.ikData[target];
                    if (!data.enable || data.rigConstraint == null)
                        continue;

                    var bindings = animatorIK.GetAnimationRiggingConstraintBindings((AnimatorIKCore.IKTarget)target);
                    afterSyncBindings.AddRange(bindings);
                }

                ToolsCurvesWasModifiedStoppedSmoothTangents(UAw.GetFrameTime(toolAnimationRigging_FirstFrame, clip), UAw.GetFrameTime(toolAnimationRigging_LastFrame, clip));

                if (toolAnimationRigging_ChangeRigWeight)
                {
                    var path = AnimationUtility.CalculateTransformPath(AnimationRigging.ArRig.transform, VAW.GameObject.transform);
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, AnimationRigging.ArRig.GetType(), "m_Weight");
                    var curve = GetAnimationCurveCustomProperty(binding);
                    var firstIndex = SetKeyframe(curve, firstTime, toolAnimationRigging_RigWeight);
                    var lastIndex = SetKeyframe(curve, lastTime, toolAnimationRigging_RigWeight);
                    SetKeyframeTangentFlat(curve, firstIndex);
                    SetKeyframeTangentFlat(curve, lastIndex);
                    SetAnimationCurveCustomProperty(binding, curve);
                    afterSyncBindings.Add(binding);
                }

                if (toolAnimationRigging_ChangeConstraintWeight)
                {
                    for (var target = 0; target < animatorIK.ikData.Length; target++)
                    {
                        var data = animatorIK.ikData[target];
                        if (!data.enable || data.rigConstraint == null)
                            continue;

                        var path = AnimationUtility.CalculateTransformPath(data.rigConstraint.transform, VAW.GameObject.transform);
                        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, data.rigConstraint.GetType(), "m_Weight");
                        var curve = GetAnimationCurveCustomProperty(binding);
                        var firstIndex = SetKeyframe(curve, firstTime, toolAnimationRigging_ConstraintWeight);
                        var lastIndex = SetKeyframe(curve, lastTime, toolAnimationRigging_ConstraintWeight);
                        SetKeyframeTangentFlat(curve, firstIndex);
                        SetKeyframeTangentFlat(curve, lastIndex);
                        SetAnimationCurveCustomProperty(binding, curve);
                        afterSyncBindings.Add(binding);
                    }
                }
            }
            finally
            {
                VAW.VA.AnimationRigging.RigLayer.active = saveRigLayerActive;
                SetCurrentTime(saveTime);
                EditorUtility.ClearProgressBar();
            }

            EditorApplication.delayCall += () =>
            {
                SetAnimationWindowSynchroSelection(afterSyncBindings.ToArray());
            };

            ToolsCommonAfter();
#endif
        }
        private void ToolsClearAnimationRigging(AnimationClip clip)
        {
#if VERYANIMATION_ANIMATIONRIGGING
            if (!ToolsCommonBefore(clip, "Clear AnimationRigging")) return;

            {
                var beginTime = UAw.SnapToFrame(toolAnimationRigging_FirstFrame >= 0 ? toolAnimationRigging_FirstFrame / clip.frameRate : 0f, clip.frameRate);
                var endTime = UAw.SnapToFrame(toolAnimationRigging_LastFrame >= 0 ? toolAnimationRigging_LastFrame / clip.frameRate : clip.length, clip.frameRate);
                float halfFrameTime = (0.5f / clip.frameRate) - 0.0001f;

                void Clear(EditorCurveBinding binding)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null) return;
                    for (int i = curve.length - 1; i >= 0; i--)
                    {
                        if (curve[i].time >= beginTime - halfFrameTime && curve[i].time <= endTime + halfFrameTime)
                        {
                            curve.RemoveKey(i);
                        }
                    }
                    AnimationUtility.SetEditorCurve(clip, binding, curve.length > 0 ? curve : null);
                }

                if (toolAnimationRigging_ChangeRigWeight)
                {
                    var path = AnimationUtility.CalculateTransformPath(AnimationRigging.ArRig.transform, VAW.GameObject.transform);
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, AnimationRigging.ArRig.GetType(), "m_Weight");
                    Clear(binding);
                }

                if (toolAnimationRigging_RootMotionCancel)
                {
                    for (var target = 0; target < animatorIK.ikData.Length; target++)
                    {
                        var data = animatorIK.ikData[target];
                        if (!data.enable || data.rigConstraint == null)
                            continue;

                        var boneIndex = BonesIndexOf(data.rigConstraint.gameObject);
                        for (int dof = 0; dof < 3; dof++)
                            Clear(AnimationCurveBindingTransformPosition(boneIndex, dof));
                        for (int dof = 0; dof < 4; dof++)
                            Clear(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                        for (int dof = 0; dof < 3; dof++)
                            Clear(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));
                    }
                }
                if (toolAnimationRigging_ChangeConstraintWeight)
                {
                    for (var target = 0; target < animatorIK.ikData.Length; target++)
                    {
                        var data = animatorIK.ikData[target];
                        if (!data.enable || data.rigConstraint == null)
                            continue;

                        var path = AnimationUtility.CalculateTransformPath(data.rigConstraint.transform, VAW.GameObject.transform);
                        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, data.rigConstraint.GetType(), "m_Weight");
                        Clear(binding);
                    }
                }

                for (var target = 0; target < animatorIK.ikData.Length; target++)
                {
                    var data = animatorIK.ikData[target];
                    if (!data.enable || data.rigConstraint == null)
                        continue;

                    var bindings = animatorIK.GetAnimationRiggingConstraintBindings((AnimatorIKCore.IKTarget)target);
                    foreach (var binding in bindings)
                    {
                        Clear(binding);
                    }
                }
            }

            ToolsCommonAfter();
#endif
        }
        private void ToolsClearHumanoidIK(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Clear IK Keyframe")) return;

            {
                var beginTime = UAw.SnapToFrame(toolHumanoidIK_FirstFrame >= 0 ? toolHumanoidIK_FirstFrame / clip.frameRate : 0f, clip.frameRate);
                var endTime = UAw.SnapToFrame(toolHumanoidIK_LastFrame >= 0 ? toolHumanoidIK_LastFrame / clip.frameRate : clip.length, clip.frameRate);
                float halfFrameTime = (0.5f / clip.frameRate) - 0.0001f;
                for (var ikIndex = (AnimatorIKIndex)0; ikIndex < AnimatorIKIndex.Total; ikIndex++)
                {
                    if (ikIndex == AnimatorIKIndex.LeftHand || ikIndex == AnimatorIKIndex.RightHand)
                    {
                        if (!toolHumanoidIK_Hand) continue;
                    }
                    else if (ikIndex == AnimatorIKIndex.LeftFoot || ikIndex == AnimatorIKIndex.RightFoot)
                    {
                        if (!toolHumanoidIK_Foot) continue;
                    }
                    else
                    {
                        continue;
                    }
                    void ClearHumanoidIK(EditorCurveBinding binding)
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        if (curve == null) return;
                        for (int i = curve.length - 1; i >= 0; i--)
                        {
                            if (curve[i].time >= beginTime - halfFrameTime && curve[i].time <= endTime + halfFrameTime)
                            {
                                curve.RemoveKey(i);
                            }
                        }
                        AnimationUtility.SetEditorCurve(clip, binding, curve.length > 0 ? curve : null);
                    }
                    for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                        ClearHumanoidIK(AnimationCurveBindingAnimatorIkT(ikIndex, dofIndex));
                    for (int dofIndex = 0; dofIndex < 4; dofIndex++)
                        ClearHumanoidIK(AnimationCurveBindingAnimatorIkQ(ikIndex, dofIndex));
                }
            }

            ToolsCommonAfter();
        }
        private void ToolsGenarateHumanoidIK(AnimationClip clip)
        {
            if (!IsHuman || !clip.isHumanMotion) return;
            if (!ToolsCommonBefore(clip, "Genarate IK Keyframe")) return;

            Assert.IsTrue(clip == CurrentClip);

            List<EditorCurveBinding> afterSyncBindings = new();

            var saveTime = CurrentTime;
            try
            {
                EditorUtility.DisplayProgressBar("Genarate IK Keyframe", "", 0f);
                var beginTime = UAw.SnapToFrame(toolHumanoidIK_FirstFrame >= 0 ? toolHumanoidIK_FirstFrame / clip.frameRate : 0f, clip.frameRate);
                var endTime = UAw.SnapToFrame(toolHumanoidIK_LastFrame >= 0 ? toolHumanoidIK_LastFrame / clip.frameRate : clip.length, clip.frameRate);

                SetCurrentTime(beginTime);

                float halfFrameTime = (0.5f / clip.frameRate) - 0.0001f;
                for (var ikIndex = (AnimatorIKIndex)0; ikIndex < AnimatorIKIndex.Total; ikIndex++)
                {
                    EditorUtility.DisplayProgressBar("Genarate IK Keyframe", string.Format("{0} / {1}", (int)ikIndex, (int)AnimatorIKIndex.Total), (int)ikIndex / (float)AnimatorIKIndex.Total);
                    if (ikIndex == AnimatorIKIndex.LeftHand || ikIndex == AnimatorIKIndex.RightHand)
                    {
                        if (!toolHumanoidIK_Hand) continue;
                    }
                    else if (ikIndex == AnimatorIKIndex.LeftFoot || ikIndex == AnimatorIKIndex.RightFoot)
                    {
                        if (!toolHumanoidIK_Foot) continue;
                    }
                    else
                    {
                        continue;
                    }

                    AnimationCurve[] ikTCurves = new AnimationCurve[3];
                    AnimationCurve[] ikQCurves = new AnimationCurve[4];
                    {
                        for (int dofIndex = 0; dofIndex < ikTCurves.Length; dofIndex++)
                        {
                            ikTCurves[dofIndex] = AnimationUtility.GetEditorCurve(clip, AnimationCurveBindingAnimatorIkT((AnimatorIKIndex)ikIndex, dofIndex));
                            if (ikTCurves[dofIndex] == null)
                                ikTCurves[dofIndex] = new AnimationCurve();
                            else
                            {
                                for (int i = ikTCurves[dofIndex].length - 1; i >= 0; i--)
                                {
                                    if (ikTCurves[dofIndex][i].time >= beginTime - halfFrameTime && ikTCurves[dofIndex][i].time <= endTime + halfFrameTime)
                                    {
                                        ikTCurves[dofIndex].RemoveKey(i);
                                    }
                                }
                            }
                        }
                        for (int dofIndex = 0; dofIndex < ikQCurves.Length; dofIndex++)
                        {
                            ikQCurves[dofIndex] = AnimationUtility.GetEditorCurve(clip, AnimationCurveBindingAnimatorIkQ((AnimatorIKIndex)ikIndex, dofIndex));
                            if (ikQCurves[dofIndex] == null)
                                ikQCurves[dofIndex] = new AnimationCurve();
                            else
                            {
                                for (int i = ikQCurves[dofIndex].length - 1; i >= 0; i--)
                                {
                                    if (ikQCurves[dofIndex][i].time >= beginTime - halfFrameTime && ikQCurves[dofIndex][i].time <= endTime + halfFrameTime)
                                    {
                                        ikQCurves[dofIndex].RemoveKey(i);
                                    }
                                }
                            }
                        }
                    }
                    Skeleton.SetApplyIK(false);
                    Skeleton.SetTransformStart();
                    var locaToWorldRotation = TransformPoseSave.StartRotation;
                    var worldToLocalMatrix = TransformPoseSave.StartMatrix.inverse;
                    var humanScale = Skeleton.Animator.humanScale;
                    var leftFeetBottomHeight = Skeleton.Animator.leftFeetBottomHeight;
                    var rightFeetBottomHeight = Skeleton.Animator.rightFeetBottomHeight;
                    var postLeftHand = GetHumanoidAvatarPostRotation(HumanBodyBones.LeftHand);
                    var postRightHand = GetHumanoidAvatarPostRotation(HumanBodyBones.RightHand);
                    var postLeftFoot = GetHumanoidAvatarPostRotation(HumanBodyBones.LeftFoot);
                    var postRightFoot = GetHumanoidAvatarPostRotation(HumanBodyBones.RightFoot);
                    var humanoidIndex = AnimatorIKIndex2HumanBodyBones[(int)ikIndex];
                    var t = Skeleton.HumanoidBones[(int)humanoidIndex].transform;
                    var positionTable = new Dictionary<float, Vector3>();
                    var rotationTable = new Dictionary<float, Quaternion>();
                    #region KeyInfoTable
                    {
                        var keyTimes = GetHumanoidKeyframeTimeList(clip, AnimatorIKIndex2HumanBodyBones[(int)ikIndex]);
                        foreach (var time in keyTimes)
                        {
                            Skeleton.SampleAnimation(clip, time);
                            positionTable.Add(time, t.position);
                            rotationTable.Add(time, t.rotation);
                        }
                    }
                    #endregion
                    for (int frame = toolHumanoidIK_FirstFrame; frame <= toolHumanoidIK_LastFrame; frame++)
                    {
                        var time = UAw.GetFrameTime(frame, clip);
                        Skeleton.SampleAnimation(clip, time);
                        Vector3 position;
                        Quaternion rotation;
                        {
                            Vector3 positionL = Vector3.zero, positionR = Vector3.zero;
                            float nearL = float.MinValue, nearR = float.MaxValue;
                            foreach (var pair in positionTable)
                            {
                                if (pair.Key <= time && pair.Key > nearL)
                                {
                                    positionL = pair.Value;
                                    nearL = pair.Key;
                                }
                                if (pair.Key >= time && pair.Key < nearR)
                                {
                                    positionR = pair.Value;
                                    nearR = pair.Key;
                                }
                            }
                            var rate = nearR - nearL != 0f ? (time - nearL) / (nearR - nearL) : 0f;
                            position = Vector3.Lerp(positionL, positionR, rate);
                        }
                        {
                            Quaternion rotationL = Quaternion.identity, rotationR = Quaternion.identity;
                            float nearL = float.MinValue, nearR = float.MaxValue;
                            foreach (var pair in rotationTable)
                            {
                                if (pair.Key <= time && pair.Key > nearL)
                                {
                                    rotationL = pair.Value;
                                    nearL = pair.Key;
                                }
                                if (pair.Key >= time && pair.Key < nearR)
                                {
                                    rotationR = pair.Value;
                                    nearR = pair.Key;
                                }
                            }
                            var rate = nearR - nearL != 0f ? (time - nearL) / (nearR - nearL) : 0f;
                            rotation = Quaternion.Slerp(rotationL, rotationR, rate);
                        }
                        var rootT = GetAnimationValueAnimatorRootT(time);
                        var rootQ = GetAnimationValueAnimatorRootQ(time);

                        Vector3 ikT = position;
                        Quaternion ikQ = rotation;
                        {
                            Quaternion post = Quaternion.identity;
                            switch ((AnimatorIKIndex)ikIndex)
                            {
                                case AnimatorIKIndex.LeftHand: post = postLeftHand; break;
                                case AnimatorIKIndex.RightHand: post = postRightHand; break;
                                case AnimatorIKIndex.LeftFoot: post = postLeftFoot; break;
                                case AnimatorIKIndex.RightFoot: post = postRightFoot; break;
                            }
                            #region IkT
                            if (ikIndex == AnimatorIKIndex.LeftFoot || ikIndex == AnimatorIKIndex.RightFoot)
                            {
                                Vector3 add = Vector3.zero;
                                switch ((AnimatorIKIndex)ikIndex)
                                {
                                    case AnimatorIKIndex.LeftFoot: add.x += leftFeetBottomHeight; break;
                                    case AnimatorIKIndex.RightFoot: add.x += rightFeetBottomHeight; break;
                                }
                                ikT += (rotation * post) * add;
                            }
                            ikT = worldToLocalMatrix.MultiplyPoint3x4(ikT) - (rootT * humanScale);
                            ikT = Quaternion.Inverse(rootQ) * ikT;
                            ikT *= 1f / humanScale;
                            #endregion
                            #region IkQ
                            ikQ = Quaternion.Inverse(locaToWorldRotation * rootQ) * (rotation * post);
                            #endregion
                        }
                        for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                        {
                            AddKeyframe(ikTCurves[dofIndex], time, ikT[dofIndex]);
                        }
                        for (int dofIndex = 0; dofIndex < 4; dofIndex++)
                        {
                            AddKeyframe(ikQCurves[dofIndex], time, ikQ[dofIndex]);
                        }
                    }
                    for (int dofIndex = 0; dofIndex < ikTCurves.Length; dofIndex++)
                    {
                        var binding = AnimationCurveBindingAnimatorIkT(ikIndex, dofIndex);
                        AnimationUtility.SetEditorCurve(clip, binding, ikTCurves[dofIndex]);
                        afterSyncBindings.Add(binding);
                    }
                    for (int dofIndex = 0; dofIndex < ikQCurves.Length; dofIndex++)
                    {
                        var binding = AnimationCurveBindingAnimatorIkQ(ikIndex, dofIndex);
                        AnimationUtility.SetEditorCurve(clip, binding, ikQCurves[dofIndex]);
                        afterSyncBindings.Add(binding);
                    }
                }
            }
            finally
            {
                SetCurrentTime(saveTime);
                EditorUtility.ClearProgressBar();
            }

            EditorApplication.delayCall += () =>
            {
                SetAnimationWindowSynchroSelection(afterSyncBindings.ToArray());
            };

            ToolsCommonAfter();
        }
        private void ToolsRootMotionMotionClear(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Clear Generic Root Motion Keyframe")) return;

            {
                for (int dof = 0; dof < 3; dof++)
                    AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[dof], null);
                for (int dof = 0; dof < 4; dof++)
                    AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionQ[dof], null);
            }

            ToolsCommonAfter();
        }
        private void ToolsRootMotionMotionGenerate(AnimationClip clip)
        {
            if (VAW.Animator == null) return;
            if (!ToolsCommonBefore(clip, "Genarate Generic Root Motion Keyframe")) return;

            var afterSyncBindings = new List<EditorCurveBinding>();

            if (IsHuman)
            {
                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[0], GetAnimationCurveAnimatorRootT(0));
                AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[2], GetAnimationCurveAnimatorRootT(2));
                {
                    var curve = GetAnimationCurveAnimatorRootT(1);
                    for (int i = 0; i < curve.length; i++)
                    {
                        var key = curve[i];
                        key.value -= 1f;
                        curve.MoveKey(i, key);
                    }
                    AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[1], curve);
                }
                for (int dof = 0; dof < 4; dof++)
                    AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionQ[dof], GetAnimationCurveAnimatorRootQ(dof));

                afterSyncBindings.AddRange(AnimationCurveBindingAnimatorMotionT);
                afterSyncBindings.AddRange(AnimationCurveBindingAnimatorMotionQ);
            }
            else
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Genarate Generic Root Motion Keyframe", "", 0f);
                    AnimationCurve[] rootT = new AnimationCurve[3];
                    AnimationCurve[] rootQ = new AnimationCurve[4];
                    {
                        for (int dof = 0; dof < 3; dof++)
                            rootT[dof] = new AnimationCurve();
                        for (int dof = 0; dof < 4; dof++)
                            rootQ[dof] = new AnimationCurve();
                    }
                    Skeleton.SetApplyIK(false);
                    Skeleton.SetTransformOrigin();
                    var rootTransform = Skeleton.Bones[RootMotionBoneIndex >= 0 ? RootMotionBoneIndex : 0].transform;

                    Skeleton.SampleAnimation(clip, 0f);
#if UNITY_2022_3_OR_NEWER
                    rootTransform.GetPositionAndRotation(out Vector3 startPosition, out Quaternion startRotation);
#else
                    var startPosition = rootTransform.position;
                    var startRotation = rootTransform.rotation;
#endif

                    var lastFrame = UAw.GetLastFrame(clip);
                    for (int frame = 0; frame <= lastFrame; frame++)
                    {
                        var time = GetFrameTime(frame);

                        Skeleton.SampleAnimation(clip, time);

                        var position = rootTransform.position - startPosition;
                        var rotation = Quaternion.Inverse(startRotation) * rootTransform.rotation;

                        for (int dof = 0; dof < 3; dof++)
                            SetKeyframe(rootT[dof], time, position[dof]);
                        for (int dof = 0; dof < 4; dof++)
                            SetKeyframe(rootQ[dof], time, rotation[dof]);
                    }
                    {
                        for (int dof = 0; dof < 3; dof++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[dof], rootT[dof]);
                        for (int dof = 0; dof < 4; dof++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionQ[dof], rootQ[dof]);
                    }

                    afterSyncBindings.AddRange(AnimationCurveBindingAnimatorMotionT);
                    afterSyncBindings.AddRange(AnimationCurveBindingAnimatorMotionQ);
                }
                finally
                {
                    Skeleton.SetTransformStart();
                    EditorUtility.ClearProgressBar();
                }
            }

            SelectMotionTool();

            EditorApplication.delayCall += () =>
            {
                SetAnimationWindowSynchroSelection(afterSyncBindings.ToArray());
            };

            ToolsCommonAfter();
        }
        private void ToolsRootMotionRootClear(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Clear Generic Root Motion Keyframe")) return;

            {
                for (int dof = 0; dof < 3; dof++)
                    AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootT[dof], null);
                for (int dof = 0; dof < 4; dof++)
                    AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootQ[dof], null);
            }

            ToolsCommonAfter();
        }
        private void ToolsRootMotionRootGenerate(AnimationClip clip)
        {
            if (IsHuman || RootMotionBoneIndex < 0) return;
            if (!ToolsCommonBefore(clip, "Genarate Generic Root Motion Keyframe")) return;

            var afterSyncBindings = new List<EditorCurveBinding>();

            try
            {
                EditorUtility.DisplayProgressBar("Genarate Generic Root Motion Keyframe", "", 0f);
                AnimationCurve[] rootT = new AnimationCurve[3];
                AnimationCurve[] rootQ = new AnimationCurve[4];
                {
                    for (int dof = 0; dof < 3; dof++)
                        rootT[dof] = new AnimationCurve();
                    for (int dof = 0; dof < 4; dof++)
                        rootQ[dof] = new AnimationCurve();
                }
                Skeleton.SetApplyIK(false);
                Skeleton.SetTransformOrigin();
                var rootTransform = Skeleton.Bones[RootMotionBoneIndex >= 0 ? RootMotionBoneIndex : 0].transform;

                Skeleton.SampleAnimation(clip, 0f);
#if UNITY_2022_3_OR_NEWER
                rootTransform.GetPositionAndRotation(out Vector3 startPosition, out Quaternion startRotation);
#else
                var startPosition = rootTransform.position;
                var startRotation = rootTransform.rotation;
#endif

                var lastFrame = UAw.GetLastFrame(clip);
                for (int frame = 0; frame <= lastFrame; frame++)
                {
                    var time = GetFrameTime(frame);

                    Skeleton.SampleAnimation(clip, time);

                    var position = rootTransform.position - startPosition;
                    var rotation = Quaternion.Inverse(startRotation) * rootTransform.rotation;

                    for (int dof = 0; dof < 3; dof++)
                        SetKeyframe(rootT[dof], time, position[dof]);
                    for (int dof = 0; dof < 4; dof++)
                        SetKeyframe(rootQ[dof], time, rotation[dof]);
                }
                {
                    for (int dof = 0; dof < 3; dof++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootT[dof], rootT[dof]);
                    for (int dof = 0; dof < 4; dof++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootQ[dof], rootQ[dof]);
                }

                afterSyncBindings.AddRange(AnimationCurveBindingAnimatorRootT);
                afterSyncBindings.AddRange(AnimationCurveBindingAnimatorRootQ);
            }
            finally
            {
                Skeleton.SetTransformStart();
                EditorUtility.ClearProgressBar();
            }

            SelectGameObject(null);

            EditorApplication.delayCall += () =>
            {
                SetAnimationWindowSynchroSelection(afterSyncBindings.ToArray());
            };

            ToolsCommonAfter();
        }
        private void ToolsCopy(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Copy Keyframe")) return;

            try
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var rbindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                var events = AnimationUtility.GetAnimationEvents(clip);
                var beginTime = UAw.SnapToFrame(toolCopy_FirstFrame / clip.frameRate, clip.frameRate);
                var endTime = UAw.SnapToFrame(toolCopy_LastFrame / clip.frameRate, clip.frameRate);
                var writeBeginTime = UAw.SnapToFrame(toolCopy_WriteFrame / clip.frameRate, clip.frameRate);
                var writeEndTime = writeBeginTime + (endTime - beginTime);
                float halfFrameTime = (0.5f / clip.frameRate) - 0.0001f;
                int progressIndex = 0;
                int progressTotal = 3;

                EditorUtility.DisplayProgressBar("Copy Keyframe", "Read", progressIndex++ / (float)progressTotal);
                if (writeBeginTime > clip.length)
                {
                    #region AddLastLinerKey
                    foreach (var binding in bindings)
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var keyIndex = FindKeyframeAtTime(curve, clip.length);
                        if (keyIndex < 0)
                        {
                            keyIndex = AddKeyframe(curve, clip.length, curve.Evaluate(clip.length));
                            AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                            if (keyIndex > 0)
                                AnimationUtility.SetKeyRightTangentMode(curve, keyIndex - 1, AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetEditorCurve(clip, binding, curve);
                        }
                    }
                    #endregion
                }

                EditorUtility.DisplayProgressBar("Copy Keyframe", "Read", progressIndex++ / (float)progressTotal);
                #region CurveBindings
                List<Keyframe>[] curveCopyKeyframes = new List<Keyframe>[bindings.Length];
                List<int>[] markKeyIndexes = new List<int>[bindings.Length];
                for (int i = 0; i < bindings.Length; i++)
                {
                    curveCopyKeyframes[i] = new List<Keyframe>();
                    markKeyIndexes[i] = new List<int>();
                    bool update = false;
                    var curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
                    if (curveCopyKeyframes[i].FindIndex((x) => Mathf.Approximately(UAw.SnapToFrame(x.time, clip.frameRate), UAw.SnapToFrame(writeBeginTime, clip.frameRate))) < 0)
                    {
                        var key = new Keyframe(writeBeginTime, curve.Evaluate(beginTime));
                        markKeyIndexes[i].Add(curveCopyKeyframes[i].Count);
                        curveCopyKeyframes[i].Add(key);
                    }
                    if (curveCopyKeyframes[i].FindIndex((x) => Mathf.Approximately(UAw.SnapToFrame(x.time, clip.frameRate), UAw.SnapToFrame(writeEndTime, clip.frameRate))) < 0)
                    {
                        var key = new Keyframe(writeEndTime, curve.Evaluate(endTime));
                        markKeyIndexes[i].Add(curveCopyKeyframes[i].Count);
                        curveCopyKeyframes[i].Add(key);
                    }
                    for (int j = 0; j < curve.length; j++)
                    {
                        if (curve[j].time >= beginTime - halfFrameTime && curve[j].time <= endTime + halfFrameTime)
                        {
                            var key = curve[j];
                            key.time = UAw.SnapToFrame(writeBeginTime + (key.time - beginTime), clip.frameRate);
                            curveCopyKeyframes[i].Add(key);
                        }
                        if (curve[j].time > writeBeginTime + halfFrameTime && curve[j].time < writeEndTime - halfFrameTime)
                        {
                            curve.RemoveKey(j--);
                            update = true;
                        }
                    }
                    {
                        void ActionAddKeyframe(int frame)
                        {
                            var setTime = UAw.GetFrameTime(frame, clip);
                            var keyIndex = FindKeyframeAtTime(curve, setTime);
                            if (keyIndex < 0)
                            {
                                AddKeyframe(curve, setTime, curve.Evaluate(setTime));
                                update = true;
                            }
                        }
                        if (toolCopy_WriteFrame < toolCopy_LastFrame)
                        {
                            ActionAddKeyframe(toolCopy_WriteFrame);
                        }
                        if (toolCopy_WriteFrame + (toolCopy_LastFrame - toolCopy_FirstFrame) > toolCopy_FirstFrame)
                        {
                            ActionAddKeyframe(toolCopy_WriteFrame + (toolCopy_LastFrame - toolCopy_FirstFrame));
                        }
                    }
                    if (update)
                        AnimationUtility.SetEditorCurve(clip, bindings[i], curve);
                }
                #endregion
                #region ObjectReferenceCurveBindings
                List<ObjectReferenceKeyframe>[] rcurveCopyKeyframes = new List<ObjectReferenceKeyframe>[rbindings.Length];
                for (int i = 0; i < rbindings.Length; i++)
                {
                    rcurveCopyKeyframes[i] = new List<ObjectReferenceKeyframe>();
                    bool update = false;
                    var keys = new List<ObjectReferenceKeyframe>(AnimationUtility.GetObjectReferenceCurve(clip, rbindings[i]));
                    for (int j = 0; j < keys.Count; j++)
                    {
                        if (keys[j].time >= beginTime - halfFrameTime && keys[j].time <= endTime + halfFrameTime)
                        {
                            var key = keys[j];
                            key.time = UAw.SnapToFrame(writeBeginTime + (key.time - beginTime), clip.frameRate);
                            rcurveCopyKeyframes[i].Add(key);
                        }
                        if (keys[j].time > writeBeginTime + halfFrameTime && keys[j].time < writeEndTime - halfFrameTime)
                        {
                            keys.RemoveAt(j--);
                            update = true;
                        }
                    }
                    if (update)
                        AnimationUtility.SetObjectReferenceCurve(clip, rbindings[i], keys.ToArray());
                }
                #endregion
                #region AnimationEvents
                List<AnimationEvent> newEvents = new();
                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i].time >= beginTime - halfFrameTime && events[i].time <= endTime + halfFrameTime)
                    {
                        var key = new AnimationEvent()
                        {
                            stringParameter = events[i].stringParameter,
                            floatParameter = events[i].floatParameter,
                            intParameter = events[i].intParameter,
                            objectReferenceParameter = events[i].objectReferenceParameter,
                            functionName = events[i].functionName,
                            time = writeBeginTime + (events[i].time - beginTime),
                            messageOptions = events[i].messageOptions,
                        };
                        newEvents.Add(key);
                    }
                    if (events[i].time < writeBeginTime - halfFrameTime || events[i].time > writeEndTime + halfFrameTime)
                    {
                        newEvents.Add(events[i]);
                    }
                }
                newEvents.Sort((x, y) =>
                {
                    if (x.time > y.time) return 1;
                    else if (x.time < y.time) return -1;
                    else return 0;
                });
                #endregion

                EditorUtility.DisplayProgressBar("Copy Keyframe", "Write", progressIndex++ / (float)progressTotal);
                #region CurveBindings
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (curveCopyKeyframes[i] == null || curveCopyKeyframes[i].Count <= 0) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
                    for (int j = 0; j < curveCopyKeyframes[i].Count; j++)
                    {
                        if (markKeyIndexes[i].Contains(j))
                        {
                            SetKeyframe(curve, curveCopyKeyframes[i][j].time, curveCopyKeyframes[i][j].value);
                        }
                        else
                        {
                            var index = SetKeyframe(curve, curveCopyKeyframes[i][j]);
                            curve.MoveKey(index, curveCopyKeyframes[i][j]);
                        }
                    }
                    AnimationUtility.SetEditorCurve(clip, bindings[i], curve);
                }
                #endregion
                #region ObjectReferenceCurveBindings
                for (int i = 0; i < rbindings.Length; i++)
                {
                    if (rcurveCopyKeyframes[i] == null || rcurveCopyKeyframes[i].Count <= 0) continue;
                    var keys = new List<ObjectReferenceKeyframe>(AnimationUtility.GetObjectReferenceCurve(clip, rbindings[i]));
                    for (int j = 0; j < rcurveCopyKeyframes[i].Count; j++)
                    {
                        var keyIndex = FindKeyframeAtTime(keys, rcurveCopyKeyframes[i][j].time);
                        if (keyIndex >= 0)
                        {
                            keys[keyIndex] = rcurveCopyKeyframes[i][j];
                        }
                        else
                        {
                            keys.Add(rcurveCopyKeyframes[i][j]);
                        }
                    }
                    AnimationUtility.SetObjectReferenceCurve(clip, rbindings[i], keys.ToArray());
                }
                #endregion
                #region AnimationEvents
                AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
                #endregion
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsTrim(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Trim Keyframe")) return;

            try
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var rbindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                var events = AnimationUtility.GetAnimationEvents(clip);
                var beginTime = UAw.SnapToFrame(toolTrim_FirstFrame / clip.frameRate, clip.frameRate);
                var endTime = UAw.SnapToFrame(toolTrim_LastFrame / clip.frameRate, clip.frameRate);
                float halfFrameTime = (0.5f / clip.frameRate) - 0.0001f;
                int progressIndex = 0;
                int progressTotal = bindings.Length * 3 + rbindings.Length + events.Length;
                {
                    AnimationCurve[] curves = new AnimationCurve[bindings.Length];
                    for (int i = 0; i < bindings.Length; i++)
                    {
                        EditorUtility.DisplayProgressBar("Trim", string.IsNullOrEmpty(bindings[i].path) ? bindings[i].propertyName : bindings[i].path, progressIndex++ / (float)progressTotal);
                        var curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
                        {
                            var keys = new List<Keyframe>();
                            for (int j = 0; j < curve.length; j++)
                            {
                                if (curve[j].time < beginTime - halfFrameTime || curve[j].time > endTime + halfFrameTime) continue;
                                var tmp = curve[j];
                                tmp.time = UAw.SnapToFrame(tmp.time - beginTime, clip.frameRate);
                                keys.Add(tmp);
                            }
                            if (keys.FindIndex((x) => Mathf.Approximately(x.time, 0f)) < 0)
                            {
                                keys.Insert(0, new Keyframe(0, curve.Evaluate(beginTime)));
                            }
                            if (keys.FindIndex((x) => Mathf.Approximately(x.time, endTime - beginTime)) < 0)
                            {
                                keys.Add(new Keyframe(endTime - beginTime, curve.Evaluate(endTime)));
                            }
                            curve.keys = keys.ToArray();
                        }
                        curves[i] = curve;
                    }
                    for (int i = 0; i < bindings.Length; i++)
                    {
                        EditorUtility.DisplayProgressBar("Trim", string.IsNullOrEmpty(bindings[i].path) ? bindings[i].propertyName : bindings[i].path, progressIndex++ / (float)progressTotal);
                        AnimationUtility.SetEditorCurve(clip, bindings[i], null);
                    }
                    for (int i = 0; i < bindings.Length; i++)
                    {
                        EditorUtility.DisplayProgressBar("Trim", string.IsNullOrEmpty(bindings[i].path) ? bindings[i].propertyName : bindings[i].path, progressIndex++ / (float)progressTotal);
                        AnimationUtility.SetEditorCurve(clip, bindings[i], curves[i]);
                    }
                }
                for (int i = 0; i < rbindings.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Trim", string.IsNullOrEmpty(rbindings[i].path) ? rbindings[i].propertyName : rbindings[i].path, progressIndex++ / (float)progressTotal);
                    var rkeys = AnimationUtility.GetObjectReferenceCurve(clip, rbindings[i]);
                    var keys = new List<ObjectReferenceKeyframe>();
                    foreach (var key in rkeys)
                    {
                        if (key.time < beginTime - halfFrameTime || key.time > endTime + halfFrameTime) continue;
                        var tmp = key;
                        tmp.time = UAw.SnapToFrame(tmp.time - beginTime, clip.frameRate);
                        keys.Add(tmp);
                    }
                    if (keys.FindIndex((x) => Mathf.Approximately(x.time, 0f)) < 0)
                    {
                        var nearIndex = FindBeforeNearKeyframeAtTime(rkeys, beginTime);
                        keys.Insert(0, new ObjectReferenceKeyframe() { time = 0f, value = rkeys[nearIndex].value });
                    }
                    AnimationUtility.SetObjectReferenceCurve(clip, rbindings[i], keys.ToArray());
                }
                {
                    List<AnimationEvent> newEvents = new(events.Length);
                    for (int i = 0; i < events.Length; i++)
                    {
                        EditorUtility.DisplayProgressBar("Trim", events[i].functionName, progressIndex++ / (float)progressTotal);
                        if (events[i].time < beginTime - halfFrameTime || events[i].time > endTime + halfFrameTime) continue;
                        var tmp = events[i];
                        tmp.time = UAw.SnapToFrame(tmp.time - beginTime, clip.frameRate);
                        newEvents.Add(tmp);
                    }
                    AnimationUtility.SetAnimationEvents(clip, newEvents.ToArray());
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsAdd(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Add Clip")) return;

            try
            {
                var addTime = clip.length + (1f / clip.frameRate);
                foreach (var binding in AnimationUtility.GetCurveBindings(toolAdd_Clip))
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    curve ??= new AnimationCurve();
                    var srcCurve = AnimationUtility.GetEditorCurve(toolAdd_Clip, binding);
                    for (int i = 0; i < srcCurve.length; i++)
                    {
                        var key = srcCurve[i];
                        key.time += addTime;
                        curve.AddKey(key);
                    }
                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                }
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(toolAdd_Clip))
                {
                    var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding).ToList();
                    curve ??= new List<ObjectReferenceKeyframe>();
                    var srcCurve = AnimationUtility.GetObjectReferenceCurve(toolAdd_Clip, binding);
                    for (int i = 0; i < srcCurve.Length; i++)
                    {
                        var key = srcCurve[i];
                        key.time += addTime;
                        curve.Add(key);
                    }
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, curve.ToArray());
                }
                {
                    var events = AnimationUtility.GetAnimationEvents(clip).ToList();
                    foreach (var ev in AnimationUtility.GetAnimationEvents(toolAdd_Clip))
                    {
                        var tmp = ev;
                        tmp.time += addTime;
                        events.Add(tmp);
                    }
                    AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsCombine(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Combine Clip")) return;

            try
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(toolCombine_Clip))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(toolCombine_Clip, binding));
                }
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(toolCombine_Clip))
                {
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, AnimationUtility.GetObjectReferenceCurve(toolCombine_Clip, binding));
                }
                {
                    var events = AnimationUtility.GetAnimationEvents(clip).ToList();
                    foreach (var ev in AnimationUtility.GetAnimationEvents(toolCombine_Clip))
                    {
                        events.Add(ev);
                    }
                    events.Sort((x, y) =>
                    {
                        if (x.time > y.time) return 1;
                        else if (x.time < y.time) return -1;
                        else return 0;
                    });
                    AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private class AnimatableBindingData
        {
            public bool needWrite;
            public AnimationCurve curve;
            public List<ObjectReferenceKeyframe> refCurve;
        }
        private void ToolsCreateNewClip(string clipPath)
        {
            VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
            var newClip = EditorCommon.CreateNewClipAtPath(clipPath);
            VeryAnimationWindow.CustomAssetModificationProcessor.Resume();

            if (!ToolsCommonBefore(newClip, "Create new clip")) return;

            var saveCurrentTime = UAw.GetCurrentTime();
            var saveApplyRootMotion = VAW.Animator != null && VAW.Animator.applyRootMotion;
#if VERYANIMATION_TIMELINE
            var saveTimelineAnimationRemoveStartOffset = UAw.GetTimelineAnimationRemoveStartOffset();
            var saveTimelineAnimationApplyFootIK = UAw.GetTimelineAnimationApplyFootIK();
#endif
            AnimationClip defaultPoseClip = null;
            void CreateDefaultPoseClip()
            {
                UAnimationMode.RevertPropertyModificationsForGameObject(VAW.GameObject);

                var allAnimatableBindings = new List<EditorCurveBinding>();
                for (int boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                    allAnimatableBindings.AddRange(AnimationUtility.GetAnimatableBindings(Bones[boneIndex], VAW.GameObject));

                defaultPoseClip = new AnimationClip() { name = "VA DefaultPose" };
                defaultPoseClip.hideFlags |= HideFlags.HideAndDontSave;

                CreateDefaultCurves(defaultPoseClip, allAnimatableBindings.ToArray());
            }

            try
            {
                int progressIndex = 0;
                int progressTotal = 1;
                EditorUtility.DisplayProgressBar("Create", clipPath, progressIndex++ / (float)progressTotal);

                var lastFrame = GetLastFrame();
                var baseClip = CurrentClip;
                if (animationMode == AnimationMode.Layers)
                {
                    var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                    if (ac != null && CurrentLayerClips != null)
                    {
                        var layers = ac.layers;
                        baseClip = null;
                        for (int i = 0; i < layers.Length; i++)
                        {
                            if (CurrentLayerClips.TryGetValue(layers[i].stateMachine, out AnimationClip lclip))
                            {
                                if (baseClip == null)
                                    baseClip = lclip;
                                lastFrame = Mathf.Max(lastFrame, UAw.GetLastFrame(lclip));
                            }
                        }
                    }
                }

                AnimationUtility.SetAnimationClipSettings(newClip, AnimationUtility.GetAnimationClipSettings(baseClip));
                {
                    newClip.frameRate = baseClip.frameRate;
                    newClip.wrapMode = baseClip.wrapMode;
                    newClip.localBounds = baseClip.localBounds;
                    newClip.legacy = baseClip.legacy;
                }

                if (toolCreateNewClip_Mode == CreateNewClipMode.Duplicate || toolCreateNewClip_Mode == CreateNewClipMode.Mirror)
                {
                    #region Duplicate
                    foreach (var binding in AnimationUtility.GetCurveBindings(baseClip))
                    {
                        AnimationUtility.SetEditorCurve(newClip, binding, AnimationUtility.GetEditorCurve(baseClip, binding));
                    }
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(baseClip))
                    {
                        AnimationUtility.SetObjectReferenceCurve(newClip, binding, AnimationUtility.GetObjectReferenceCurve(baseClip, binding));
                    }
                    AnimationUtility.SetAnimationEvents(newClip, AnimationUtility.GetAnimationEvents(baseClip));
                    #endregion
                }
                if (toolCreateNewClip_Mode == CreateNewClipMode.Mirror)
                {
                    #region Mirror
                    {
                        #region SwapMirrorCurve
                        {
                            var bindings = AnimationUtility.GetCurveBindings(newClip);
                            int GetMirrorBindingIndex(EditorCurveBinding binding)
                            {
                                var mbinding = GetMirrorAnimationCurveBinding(binding);
                                if (!mbinding.HasValue) return -1;
                                return EditorCommon.ArrayIndexOf(bindings, mbinding.Value);
                            }
                            #region CreateMirrorCurve
                            foreach (var binding in bindings)
                            {
                                var mbinding = GetMirrorAnimationCurveBinding(binding);
                                if (!mbinding.HasValue) continue;
                                if (!EditorCommon.ArrayContains(bindings, mbinding.Value))
                                {
                                    var mcurve = new AnimationCurve();
                                    void AddKey(float value)
                                    {
                                        AddKeyframe(mcurve, 0, value);
                                        AddKeyframe(mcurve, newClip.length, value);
                                    }
                                    int dofIndex = GetDOFIndexFromCurveBinding(mbinding.Value);
                                    if (mbinding.Value.type == typeof(Animator))
                                    {
                                        AddKey(0f);
                                    }
                                    else if (mbinding.Value.type == typeof(Transform))
                                    {
                                        var boneIndex = GetBoneIndexFromCurveBinding(mbinding.Value);
                                        if (IsTransformPositionCurveBinding(mbinding.Value))
                                            AddKey(BoneSaveTransforms[boneIndex].localPosition[dofIndex]);
                                        else if (IsTransformRotationCurveBinding(mbinding.Value))
                                        {
                                            if (URotationCurveInterpolation.GetModeFromCurveData(mbinding.Value) == URotationCurveInterpolation.Mode.RawEuler)
                                                AddKey(BoneSaveTransforms[boneIndex].localRotation.eulerAngles[dofIndex]);
                                            else if (URotationCurveInterpolation.GetModeFromCurveData(mbinding.Value) == URotationCurveInterpolation.Mode.RawQuaternions)
                                                AddKey(BoneSaveTransforms[boneIndex].localRotation[dofIndex]);
                                            else
                                                Assert.IsTrue(false);
                                        }
                                        else if (IsTransformScaleCurveBinding(mbinding.Value))
                                            AddKey(BoneSaveTransforms[boneIndex].localScale[dofIndex]);
                                    }
                                    else if (IsSkinnedMeshRendererBlendShapeCurveBinding(mbinding.Value))
                                    {
                                        var boneIndex = GetBoneIndexFromCurveBinding(mbinding.Value);
                                        var renderer = Bones[boneIndex].GetComponent<SkinnedMeshRenderer>();
                                        var name = PropertyName2BlendShapeName(mbinding.Value.propertyName);
                                        AddKey(BlendShapeWeightSave.GetDefaultWeight(renderer, name));
                                    }
                                    else
                                    {
                                        Assert.IsTrue(false);
                                    }
                                    AnimationUtility.SetEditorCurve(newClip, mbinding.Value, mcurve);
                                }
                            }
                            bindings = AnimationUtility.GetCurveBindings(newClip);
                            #endregion

                            #region MirrorCurve
                            {
                                void SwapCurve(int indexA, int indexB)
                                {
                                    var curveA = AnimationUtility.GetEditorCurve(newClip, bindings[indexA]);
                                    var curveB = AnimationUtility.GetEditorCurve(newClip, bindings[indexB]);
                                    AnimationUtility.SetEditorCurve(newClip, bindings[indexB], curveA);
                                    AnimationUtility.SetEditorCurve(newClip, bindings[indexA], curveB);
                                }
                                void MirrorCurve(int index)
                                {
                                    var curve = AnimationUtility.GetEditorCurve(newClip, bindings[index]);
                                    for (int i = 0; i < curve.length; i++)
                                    {
                                        var key = curve[i];
                                        key.value = -key.value;
                                        key.inTangent = -key.inTangent;
                                        key.outTangent = -key.outTangent;
                                        curve.MoveKey(i, key);
                                    }
                                    AnimationUtility.SetEditorCurve(newClip, bindings[index], curve);
                                }

                                bool[] doneFlag = new bool[bindings.Length];
                                for (int i = 0; i < bindings.Length; i++)
                                {
                                    if (doneFlag[i]) continue;
                                    doneFlag[i] = true;
                                    if (bindings[i].type == typeof(Animator))
                                    {
                                        #region Animator
                                        AnimatorIKIndex ikIndex = AnimatorIKIndex.None;
                                        AnimatorTDOFIndex tdofIndex = AnimatorTDOFIndex.None;
                                        var muscleIndex = GetMuscleIndexFromCurveBinding(bindings[i]);
                                        if (muscleIndex >= 0)
                                        {
                                            #region Muscle
                                            var mirrorMuscleIndex = GetMirrorMuscleIndex(muscleIndex);
                                            if (mirrorMuscleIndex >= 0)
                                            {
                                                var mirrorBindingIndex = GetMirrorBindingIndex(bindings[i]);
                                                doneFlag[mirrorBindingIndex] = true;
                                                SwapCurve(i, mirrorBindingIndex);
                                            }
                                            else if (muscleIndex == HumanTrait.MuscleFromBone(HumanTrait.BoneFromMuscle(muscleIndex), 0) ||
                                                    muscleIndex == HumanTrait.MuscleFromBone(HumanTrait.BoneFromMuscle(muscleIndex), 1))
                                            {
                                                MirrorCurve(i);
                                            }
                                            #endregion
                                        }
                                        else if (bindings[i].propertyName == AnimationCurveBindingAnimatorRootT[0].propertyName ||
                                                bindings[i].propertyName == AnimationCurveBindingAnimatorRootQ[1].propertyName ||
                                                bindings[i].propertyName == AnimationCurveBindingAnimatorRootQ[2].propertyName)
                                        {
                                            #region Root
                                            MirrorCurve(i);
                                            #endregion
                                        }
                                        else if ((ikIndex = GetIkTIndexFromCurveBinding(bindings[i])) != AnimatorIKIndex.None)
                                        {
                                            #region IKT
                                            var mirrorBindingIndex = GetMirrorBindingIndex(bindings[i]);
                                            if (mirrorBindingIndex >= 0)
                                            {
                                                doneFlag[mirrorBindingIndex] = true;
                                                var dofIndex = GetDOFIndexFromCurveBinding(bindings[i]);
                                                if (dofIndex == 0)
                                                {
                                                    MirrorCurve(i);
                                                }
                                                SwapCurve(i, mirrorBindingIndex);
                                                if (dofIndex == 0)
                                                {
                                                    MirrorCurve(i);
                                                }
                                            }
                                            #endregion
                                        }
                                        else if ((ikIndex = GetIkQIndexFromCurveBinding(bindings[i])) != AnimatorIKIndex.None)
                                        {
                                            #region IKQ
                                            EditorCurveBinding[] ikQBindings = new EditorCurveBinding[4];
                                            int[] bindingIndexes = new int[4];
                                            int[] mirrorBindingIndexes = new int[4];
                                            for (int dof = 0; dof < ikQBindings.Length; dof++)
                                            {
                                                ikQBindings[dof] = AnimationCurveBindingAnimatorIkQ(ikIndex, dof);
                                                bindingIndexes[dof] = ArrayUtility.FindIndex(bindings, x => x.propertyName == ikQBindings[dof].propertyName);
                                                string mirrorPropertyName = null;
                                                if (ikQBindings[dof].propertyName.IndexOf("Left") >= 0)
                                                    mirrorPropertyName = ikQBindings[dof].propertyName.Replace("Left", "Right");
                                                else if (ikQBindings[dof].propertyName.IndexOf("Right") >= 0)
                                                    mirrorPropertyName = ikQBindings[dof].propertyName.Replace("Right", "Left");
                                                Assert.IsNotNull(mirrorPropertyName);
                                                mirrorBindingIndexes[dof] = ArrayUtility.FindIndex(bindings, x => x.propertyName == mirrorPropertyName);
                                            }
                                            if (bindingIndexes[0] >= 0 && bindingIndexes[1] >= 0 && bindingIndexes[2] >= 0 && bindingIndexes[3] >= 0 &&
                                                mirrorBindingIndexes[0] >= 0 && mirrorBindingIndexes[1] >= 0 && mirrorBindingIndexes[2] >= 0 && mirrorBindingIndexes[3] >= 0)
                                            {
                                                for (int dof = 0; dof < ikQBindings.Length; dof++)
                                                {
                                                    SwapCurve(bindingIndexes[dof], mirrorBindingIndexes[QuaternionXMirrorSwapDof[dof]]);
                                                    doneFlag[bindingIndexes[dof]] = true;
                                                    doneFlag[mirrorBindingIndexes[QuaternionXMirrorSwapDof[dof]]] = true;
                                                }
                                            }
                                            #endregion
                                        }
                                        else if ((tdofIndex = GetTDOFIndexFromCurveBinding(bindings[i])) != AnimatorTDOFIndex.None)
                                        {
                                            #region TDOF
                                            var dofIndex = GetDOFIndexFromCurveBinding(bindings[i]);
                                            var mirrortdofIndex = AnimatorTDOFMirrorIndexes[(int)tdofIndex];
                                            if (mirrortdofIndex != AnimatorTDOFIndex.None)
                                            {
                                                var mirrorBindingIndex = GetMirrorBindingIndex(bindings[i]);
                                                if (mirrorBindingIndex >= 0)
                                                {
                                                    doneFlag[mirrorBindingIndex] = true;

                                                    var mirror = HumanBonesAnimatorTDOFIndex[(int)AnimatorTDOFIndex2HumanBodyBones[(int)mirrortdofIndex]].mirror;
                                                    if (mirror[dofIndex] < 0)
                                                    {
                                                        MirrorCurve(i);
                                                    }
                                                    SwapCurve(i, mirrorBindingIndex);
                                                    if (mirror[dofIndex] < 0)
                                                    {
                                                        MirrorCurve(i);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (dofIndex == 2)
                                                {
                                                    MirrorCurve(i);
                                                }
                                            }
                                            #endregion
                                        }
                                        #endregion
                                    }
                                    else if (IsSkinnedMeshRendererBlendShapeCurveBinding(bindings[i]))
                                    {
                                        #region BlendShape
                                        var boneIndex = GetBoneIndexFromCurveBinding(bindings[i]);
                                        if (boneIndex >= 0)
                                        {
                                            var mirrorBindingIndex = GetMirrorBindingIndex(bindings[i]);
                                            if (mirrorBindingIndex >= 0)
                                            {
                                                doneFlag[mirrorBindingIndex] = true;
                                                SwapCurve(i, mirrorBindingIndex);
                                            }
                                        }
                                        #endregion
                                    }
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region FullBakeKeyframe
                        {
                            var curves = new Dictionary<EditorCurveBinding, AnimationCurve>();
                            AnimationCurve GetCurve(EditorCurveBinding binding)
                            {
                                if (!curves.TryGetValue(binding, out AnimationCurve curve))
                                {
                                    curve = AnimationUtility.GetEditorCurve(newClip, binding);
                                    curve ??= new AnimationCurve();
                                    curves.Add(binding, curve);
                                }
                                return curve;
                            }

                            Quaternion[] boneWroteRotation = new Quaternion[Bones.Length];
                            Vector3[] boneWroteEuler = new Vector3[Bones.Length];
                            for (int boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                            {
                                boneWroteRotation[boneIndex] = BoneSaveOriginalTransforms[boneIndex].localRotation;
                                boneWroteEuler[boneIndex] = boneWroteRotation[boneIndex].eulerAngles;
                            }
                            for (int frame = 0; frame <= lastFrame; frame++)
                            {
                                EditorUtility.DisplayProgressBar("Frame", string.Format("{0} / {1}", frame, lastFrame), frame / (float)lastFrame);

                                var time = GetFrameTime(frame);
                                #region Generic
                                {
                                    for (int boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                                    {
                                        if (IsHuman && HumanoidConflict[boneIndex]) continue;
                                        Vector3 position;
                                        Quaternion rotation;
                                        Vector3 scale;
                                        Vector3 positionMirrorOriginal;
                                        Quaternion rotationMirrorOriginal;
                                        Vector3 scaleMirrorOriginal;
                                        var mbi = MirrorBoneIndexes[boneIndex];
                                        if (mbi >= 0)
                                        {
                                            position = GetMirrorBoneLocalPosition(mbi, GetAnimationValueTransformPosition(mbi, time));
                                            rotation = GetMirrorBoneLocalRotation(mbi, GetAnimationValueTransformRotation(mbi, time));
                                            scale = GetMirrorBoneLocalScale(mbi, GetAnimationValueTransformScale(mbi, time));
                                            positionMirrorOriginal = GetMirrorBoneLocalPosition(mbi, BoneSaveTransforms[mbi].localPosition);
                                            rotationMirrorOriginal = GetMirrorBoneLocalRotation(mbi, BoneSaveTransforms[mbi].localRotation);
                                            scaleMirrorOriginal = GetMirrorBoneLocalScale(mbi, BoneSaveTransforms[mbi].localScale);
                                        }
                                        else
                                        {
                                            position = GetMirrorBoneLocalPosition(boneIndex, GetAnimationValueTransformPosition(boneIndex, time));
                                            rotation = GetMirrorBoneLocalRotation(boneIndex, GetAnimationValueTransformRotation(boneIndex, time));
                                            scale = GetMirrorBoneLocalScale(boneIndex, GetAnimationValueTransformScale(boneIndex, time));
                                            positionMirrorOriginal = GetMirrorBoneLocalPosition(boneIndex, BoneSaveTransforms[boneIndex].localPosition);
                                            rotationMirrorOriginal = GetMirrorBoneLocalRotation(boneIndex, BoneSaveTransforms[boneIndex].localRotation);
                                            scaleMirrorOriginal = GetMirrorBoneLocalScale(boneIndex, BoneSaveTransforms[boneIndex].localScale);
                                        }
                                        bool positionMirrorDifferent = false;
                                        bool rotationMirrorDifferent = false;
                                        bool scaleMirrorDifferent = false;
                                        {
                                            positionMirrorDifferent = Mathf.Abs(positionMirrorOriginal.x - BoneSaveOriginalTransforms[boneIndex].localPosition.x) >= TransformPositionApproximatelyThreshold ||
                                                                        Mathf.Abs(positionMirrorOriginal.y - BoneSaveOriginalTransforms[boneIndex].localPosition.y) >= TransformPositionApproximatelyThreshold ||
                                                                        Mathf.Abs(positionMirrorOriginal.z - BoneSaveOriginalTransforms[boneIndex].localPosition.z) >= TransformPositionApproximatelyThreshold; ;
                                            {
                                                var eulerAngles = rotationMirrorOriginal.eulerAngles;
                                                var originalEulerAngles = BoneSaveTransforms[boneIndex].localRotation.eulerAngles;
                                                rotationMirrorDifferent = Mathf.Abs(eulerAngles.x - originalEulerAngles.x) >= TransformRotationApproximatelyThreshold ||
                                                                            Mathf.Abs(eulerAngles.y - originalEulerAngles.y) >= TransformRotationApproximatelyThreshold ||
                                                                            Mathf.Abs(eulerAngles.z - originalEulerAngles.z) >= TransformRotationApproximatelyThreshold;
                                            }
                                            scaleMirrorDifferent = Mathf.Abs(scaleMirrorOriginal.x - BoneSaveOriginalTransforms[boneIndex].localScale.x) >= TransformScaleApproximatelyThreshold ||
                                                                    Mathf.Abs(scaleMirrorOriginal.y - BoneSaveOriginalTransforms[boneIndex].localScale.y) >= TransformScaleApproximatelyThreshold ||
                                                                    Mathf.Abs(scaleMirrorOriginal.z - BoneSaveOriginalTransforms[boneIndex].localScale.z) >= TransformScaleApproximatelyThreshold;
                                        }
                                        if (IsHaveAnimationCurveTransformPosition(boneIndex) || IsHaveAnimationCurveTransformPosition(mbi) || positionMirrorDifferent)
                                        {
                                            for (int dof = 0; dof < 3; dof++)
                                            {
                                                var curve = GetCurve(AnimationCurveBindingTransformPosition(boneIndex, dof));
                                                SetKeyframe(curve, time, position[dof]);
                                            }
                                        }
                                        {
                                            var rotationMode = GetHaveAnimationCurveTransformRotationMode(boneIndex);
                                            var mrotationMode = GetHaveAnimationCurveTransformRotationMode(mbi);
                                            if (rotationMode != URotationCurveInterpolation.Mode.Undefined || mrotationMode != URotationCurveInterpolation.Mode.Undefined || rotationMirrorDifferent)
                                            {
                                                if (rotationMode == URotationCurveInterpolation.Mode.RawEuler)
                                                {
                                                    var eulerAngles = rotation.eulerAngles;
                                                    eulerAngles = FixReverseRotationEuler(eulerAngles, boneWroteEuler[boneIndex]);
                                                    boneWroteEuler[boneIndex] = eulerAngles;
                                                    for (int dof = 0; dof < 3; dof++)
                                                    {
                                                        var curve = GetCurve(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));
                                                        SetKeyframe(curve, time, eulerAngles[dof]);
                                                    }
                                                }
                                                else
                                                {
                                                    rotation = FixReverseRotationQuaternion(rotation, boneWroteRotation[boneIndex]);
                                                    boneWroteRotation[boneIndex] = rotation;
                                                    for (int dof = 0; dof < 4; dof++)
                                                    {
                                                        var curve = GetCurve(AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                                                        SetKeyframe(curve, time, rotation[dof]);
                                                    }
                                                }
                                            }
                                        }
                                        if (IsHaveAnimationCurveTransformScale(boneIndex) || IsHaveAnimationCurveTransformScale(mbi) || scaleMirrorDifferent)
                                        {
                                            if (VAW.EditorSettings.SettingGenericMirrorScale)
                                            {
                                                for (int dof = 0; dof < 3; dof++)
                                                {
                                                    var curve = GetCurve(AnimationCurveBindingTransformScale(boneIndex, dof));
                                                    SetKeyframe(curve, time, scale[dof]);
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }

                            #region SmoothTangents
                            foreach (var pair in curves)
                            {
                                var curve = pair.Value;
                                if (curve == null || curve.length <= 0) continue;

                                for (int i = 0; i < curve.length; i++)
                                    curve.SmoothTangents(i, 0f);
                                AnimationUtility.SetEditorCurve(newClip, pair.Key, curve);
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                }
                else if (toolCreateNewClip_Mode == CreateNewClipMode.Result)
                {
                    #region Result
                    //InitializeSettings
                    {
                        var settings = AnimationUtility.GetAnimationClipSettings(newClip);
                        settings.heightFromFeet = false;
                        settings.keepOriginalPositionXZ = true;
                        settings.keepOriginalPositionY = true;
                        settings.keepOriginalOrientation = true;
                        settings.loopBlendOrientation = true;
                        settings.loopBlendPositionXZ = true;
                        settings.loopBlendPositionY = true;
                        settings.mirror = false;
                        settings.loopBlend = false;
                        settings.cycleOffset = 0;
                        settings.level = 0;
                        settings.orientationOffsetY = 0;
                        settings.loopTime = false;
                        AnimationUtility.SetAnimationClipSettings(newClip, settings);
                    }

                    CreateDefaultPoseClip();

#if VERYANIMATION_TIMELINE
                    if (UAw.GetLinkedWithTimeline())
                    {
                        newClip.frameRate = UAw.GetTimelineFrameRate();
                    }
                    else
#endif
                    {
                        SampleAnimation(0f);
                        animationPlayable.uAnimationClipPlayable.SetOverrideLoopTime(UAw.GetClipPlayable(), false);
                    }

                    var animatableBindingsTable = new EditorCurveBinding[Bones.Length][];
                    for (int boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                    {
                        animatableBindingsTable[boneIndex] = AnimationUtility.GetAnimatableBindings(Bones[boneIndex], VAW.GameObject);
                    }

                    var animatableDataDic = new Dictionary<EditorCurveBinding, AnimatableBindingData>();
                    var rootT = VAW.GameObject.transform;

                    for (int frame = toolCreateNewClip_FirstFrame; frame <= toolCreateNewClip_LastFrame; frame++)
                    {
                        EditorUtility.DisplayProgressBar("Frame", string.Format("{0} / {1}", frame, toolCreateNewClip_LastFrame), (frame - toolCreateNewClip_FirstFrame) / (float)(toolCreateNewClip_LastFrame - toolCreateNewClip_FirstFrame));

                        var time = UAw.GetFrameTime(frame, newClip);
                        var writeTime = UAw.GetFrameTime(frame - toolCreateNewClip_FirstFrame, newClip);
#if VERYANIMATION_TIMELINE
                        if (UAw.GetLinkedWithTimeline())
                        {
                            UAw.SetTimelineFrame(frame);
                            SampleAnimation();
                        }
                        else
#endif
                        {
                            SetCurrentTime(time);
                            SampleAnimation();
                        }

                        for (int boneIndex = 0; boneIndex < Bones.Length; boneIndex++)
                        {
                            foreach (var binding in animatableBindingsTable[boneIndex])
                            {
                                if (!animatableDataDic.TryGetValue(binding, out var value))
                                {
                                    value = new AnimatableBindingData();
                                    animatableDataDic[binding] = value;
                                }

                                if (binding.isPPtrCurve)
                                {
                                    if (AnimationUtility.GetObjectReferenceValue(VAW.GameObject, binding, out var data))
                                    {
                                        if (!value.needWrite)
                                        {
                                            var defaultCurve = AnimationUtility.GetObjectReferenceCurve(defaultPoseClip, binding);
                                            if (defaultCurve != null)
                                            {
                                                foreach (var item in defaultCurve)
                                                {
                                                    if (item.value != data)
                                                    {
                                                        value.needWrite = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        value.refCurve ??= new List<ObjectReferenceKeyframe>();
                                        value.refCurve.Add(new ObjectReferenceKeyframe()
                                        {
                                            time = writeTime,
                                            value = data,
                                        });
                                    }
                                }
                                else
                                {
                                    if (AnimationUtility.GetFloatValue(VAW.GameObject, binding, out var data))
                                    {
                                        if (!value.needWrite)
                                        {
                                            var defaultCurve = AnimationUtility.GetEditorCurve(defaultPoseClip, binding);
                                            if (defaultCurve != null)
                                            {
                                                var defaultValue = defaultCurve.Evaluate(time);
                                                if (defaultValue != data)
                                                {
                                                    value.needWrite = true;
                                                }
                                            }
                                        }
                                        value.curve ??= new AnimationCurve();
                                        SetKeyframe(value.curve, writeTime, data);
                                    }
                                }
                            }
                        }

                        #region Humanoid
                        if (IsHuman)
                        {
#if UNITY_2022_3_OR_NEWER
                            rootT.SetLocalPositionAndRotation(TransformPoseSave.StartMatrix.inverse.MultiplyPoint3x4(rootT.position), Quaternion.Inverse(TransformPoseSave.StartRotation) * rootT.rotation);
#else
                            rootT.localPosition = TransformPoseSave.StartMatrix.inverse.MultiplyPoint3x4(rootT.position);
                            rootT.localRotation = Quaternion.Inverse(TransformPoseSave.StartRotation) * rootT.rotation;
#endif
                            HumanPose hp = new();
                            HumanPoseHandler.GetHumanPose(ref hp);

                            for (int dof = 0; dof < 3; dof++)
                            {
                                var binding = AnimationCurveBindingAnimatorRootT[dof];
                                SetKeyframe(animatableDataDic[binding].curve, writeTime, hp.bodyPosition[dof]);
                            }
                            for (int dof = 0; dof < 4; dof++)
                            {
                                var binding = AnimationCurveBindingAnimatorRootQ[dof];
                                SetKeyframe(animatableDataDic[binding].curve, writeTime, hp.bodyRotation[dof]);
                            }
                            for (int muscleIndex = 0; muscleIndex < HumanTrait.MuscleCount; muscleIndex++)
                            {
                                var binding = AnimationCurveBindingAnimatorMuscle(muscleIndex);
                                SetKeyframe(animatableDataDic[binding].curve, writeTime, hp.muscles[muscleIndex]);
                            }
                        }
                        #endregion
                    }
                    #region Humanoid
                    if (IsHuman)
                    {
                        void SetNeedWrite(EditorCurveBinding[] bindings, bool value)
                        {
                            for (int dof = 0; dof < bindings.Length; dof++)
                                animatableDataDic[bindings[dof]].needWrite = value;
                        }
                        SetNeedWrite(AnimationCurveBindingAnimatorRootT, true);
                        SetNeedWrite(AnimationCurveBindingAnimatorRootQ, true);
                        SetNeedWrite(AnimationCurveBindingAnimatorMotionT, false);
                        SetNeedWrite(AnimationCurveBindingAnimatorMotionQ, false);
                        for (int muscleIndex = 0; muscleIndex < HumanTrait.MuscleCount; muscleIndex++)
                        {
                            var binding = AnimationCurveBindingAnimatorMuscle(muscleIndex);
                            animatableDataDic[binding].needWrite = true;
                        }
                        for (int ikIndex = 0; ikIndex < (int)AnimatorIKIndex.Total; ikIndex++)
                        {
                            for (int dof = 0; dof < 3; dof++)
                            {
                                var binding = AnimationCurveBindingAnimatorIkT((AnimatorIKIndex)ikIndex, dof);
                                animatableDataDic[binding].needWrite = false;
                            }
                            for (int dof = 0; dof < 4; dof++)
                            {
                                var binding = AnimationCurveBindingAnimatorIkQ((AnimatorIKIndex)ikIndex, dof);
                                animatableDataDic[binding].needWrite = false;
                            }
                        }
                        foreach (var pair in animatableDataDic)
                        {
                            if (!pair.Value.needWrite)
                                continue;
                            if (pair.Key.type == typeof(Transform))
                            {
                                var boneIndex = GetBoneIndexFromCurveBinding(pair.Key);
                                if (boneIndex >= 0)
                                {
                                    if (HumanoidConflict[boneIndex])
                                        pair.Value.needWrite = false;
                                }
                            }
                        }
                    }
                    #endregion
                    #region Same members
                    foreach (var pair in animatableDataDic)
                    {
                        if (!pair.Value.needWrite)
                            continue;

                        var lastIndex = pair.Key.propertyName.LastIndexOf(".");
                        if (lastIndex >= 0)
                        {
                            var pName = pair.Key.propertyName[..(lastIndex + 1)];
                            foreach (var pairSub in animatableDataDic)
                            {
                                if (pairSub.Value.needWrite)
                                    continue;
                                if (pair.Key == pairSub.Key ||
                                    pair.Key.path != pairSub.Key.path)
                                    continue;
                                if (!pairSub.Key.propertyName.Contains(pName))
                                    continue;
                                pairSub.Value.needWrite = true;
                            }
                        }
                    }
                    #endregion

                    foreach (var pair in animatableDataDic)
                    {
                        if (!pair.Value.needWrite)
                            continue;

                        if (pair.Key.isPPtrCurve)
                        {
                            if (pair.Value.refCurve != null && pair.Value.refCurve.Count > 0)
                            {
                                AnimationUtility.SetObjectReferenceCurve(newClip, pair.Key, pair.Value.refCurve.ToArray());
                            }
                        }
                        else
                        {
                            if (pair.Value.curve != null && pair.Value.curve.length > 0)
                            {
                                AnimationUtility.SetEditorCurve(newClip, pair.Key, pair.Value.curve);
                            }
                        }
                    }

                    ResetAnimationMode();
                    #endregion
                }

                bool added = false;
                if (UAw.GetLinkedWithTimeline())
                {
                    #region Timeline
#if VERYANIMATION_TIMELINE
                    Undo.RecordObject(UAw.GetTimelineCurrentDirector(), "Create New Clip");
                    var animationTrack = UAw.GetTimelineAnimationTrack();
                    Undo.RecordObject(animationTrack, "Create New Clip");
                    var timelineClip = animationTrack.CreateClip(newClip);
                    timelineClip.displayName = Path.GetFileNameWithoutExtension(clipPath);
                    UAw.ForceRefresh();
                    UAw.EditSequencerClip(timelineClip);
                    var animationPlayableAsset = UAw.GetTimelineAnimationPlayableAsset();
                    animationPlayableAsset.removeStartOffset = saveTimelineAnimationRemoveStartOffset;
                    animationPlayableAsset.applyFootIK = saveTimelineAnimationApplyFootIK;
                    added = true;
#endif
                    #endregion
                }
                else
                {
                    #region Animator
                    if (VAW.Animator != null && VAW.Animator.runtimeAnimatorController != null)
                    {
                        var ac = EditorCommon.GetAnimatorController(VAW.Animator);
                        AnimationClip virtualClip = null;
                        #region AnimatorOverrideController
                        if (VAW.Animator.runtimeAnimatorController is AnimatorOverrideController)
                        {
                            var owc = VAW.Animator.runtimeAnimatorController as AnimatorOverrideController;
                            {
                                var srcList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                                owc.GetOverrides(srcList);
                                foreach (var pair in srcList)
                                {
                                    if (pair.Value == baseClip)
                                    {
                                        virtualClip = pair.Key;
                                        added = true;
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion
                        #region AnimatorControllerLayer
                        if (ac != null)
                        {
                            Undo.RecordObject(ac, "Create New Clip");
                            int findLayerIndex = 0;
                            AnimatorState srcState = null;
                            var layers = ac.layers;
                            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                            {
                                void FindStateMachine(AnimatorStateMachine stateMachine)
                                {
                                    foreach (var state in stateMachine.states)
                                    {
                                        var motion = state.state.motion;
                                        if (layers[layerIndex].syncedLayerIndex >= 0)
                                            motion = layers[layerIndex].GetOverrideMotion(state.state);

                                        if (motion is UnityEditor.Animations.BlendTree)
                                        {
                                            void FindBlendTree(UnityEditor.Animations.BlendTree blendTree)
                                            {
                                                if (blendTree.children == null) return;
                                                var children = blendTree.children;
                                                for (int i = 0; i < children.Length; i++)
                                                {
                                                    if (children[i].motion is UnityEditor.Animations.BlendTree)
                                                    {
                                                        FindBlendTree(children[i].motion as UnityEditor.Animations.BlendTree);
                                                    }
                                                    else
                                                    {
                                                        if (children[i].motion == baseClip || (virtualClip != null && children[i].motion == virtualClip))
                                                        {
                                                            findLayerIndex = layerIndex;
                                                            srcState = state.state;
                                                            break;
                                                        }
                                                    }
                                                }
                                                blendTree.children = children;
                                            }

                                            FindBlendTree(motion as UnityEditor.Animations.BlendTree);
                                        }
                                        else
                                        {
                                            if (motion == baseClip || (virtualClip != null && motion == virtualClip))
                                            {
                                                if (layers[layerIndex].syncedLayerIndex >= 0)
                                                    findLayerIndex = layers[layerIndex].syncedLayerIndex;
                                                else
                                                    findLayerIndex = layerIndex;
                                                srcState = state.state;
                                                break;
                                            }
                                        }
                                    }
                                    foreach (var childStateMachine in stateMachine.stateMachines)
                                    {
                                        FindStateMachine(childStateMachine.stateMachine);
                                    }
                                }

                                if (layers[layerIndex].syncedLayerIndex >= 0)
                                    FindStateMachine(layers[layers[layerIndex].syncedLayerIndex].stateMachine);
                                else
                                    FindStateMachine(layers[layerIndex].stateMachine);
                            }
                            var animatorState = ac.AddMotion(newClip, findLayerIndex);
                            if (srcState != null)
                            {
                                animatorState.behaviours = srcState.behaviours;
                                animatorState.transitions = srcState.transitions;
                                animatorState.mirrorParameterActive = srcState.mirrorParameterActive;
                                animatorState.cycleOffsetParameterActive = srcState.cycleOffsetParameterActive;
                                animatorState.speedParameterActive = srcState.speedParameterActive;
                                animatorState.mirrorParameter = srcState.mirrorParameter;
                                animatorState.cycleOffsetParameter = srcState.cycleOffsetParameter;
                                animatorState.speedParameter = srcState.speedParameter;
                                animatorState.tag = srcState.tag;
                                animatorState.writeDefaultValues = srcState.writeDefaultValues;
                                animatorState.iKOnFeet = srcState.iKOnFeet;
                                animatorState.mirror = srcState.mirror;
                                animatorState.cycleOffset = srcState.cycleOffset;
                                animatorState.speed = srcState.speed;
                                animatorState.motion = newClip;
                                animatorState.timeParameter = srcState.timeParameter;
                                animatorState.timeParameterActive = srcState.timeParameterActive;
                                added = true;
                            }
                        }
                        #endregion
                    }
                    #endregion
                    #region Animation
                    if (VAW.Animation != null)
                    {
                        Undo.RecordObject(VAW.Animation, "Create New Clip");
                        var animations = AnimationUtility.GetAnimationClips(VAW.GameObject);
                        ArrayUtility.Add(ref animations, newClip);
                        AnimationUtility.SetAnimationClips(VAW.Animation, animations);
                        added = true;
                    }
                    #endregion

                    if (!added)
                        Debug.LogWarningFormat(Language.GetText(Language.Help.LogAnimationClipAddError), newClip);

                    EditorGUIUtility.PingObject(newClip);

                    UAw.ForceRefresh();
                    SetCurrentClip(newClip);
                }
            }
            finally
            {
                if (defaultPoseClip != null)
                {
                    AnimationClip.DestroyImmediate(defaultPoseClip);
                    defaultPoseClip = null;
                }

                SetCurrentTime(saveCurrentTime);
                if (VAW.Animator != null && VAW.Animator.applyRootMotion != saveApplyRootMotion)
                    VAW.Animator.applyRootMotion = saveApplyRootMotion;
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsCreateNewKeyframe(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Create New Keyframe")) return;

            try
            {
                int progressIndex = 0;
                int progressTotal = 1;
                EditorUtility.DisplayProgressBar("Create New Keyframe", "", progressIndex++ / (float)progressTotal);

                bool selectRoot = SelectionBones != null && SelectionBones.Contains(0);
                var humanoidIndexes = SelectionGameObjectsHumanoidIndex();
                var boneIndexes = SelectionGameObjectsOtherHumanoidBoneIndex();
                List<float> times = new();
                {
                    int interval = 0;
                    for (int frame = toolCreateNewKeyframe_FirstFrame; frame <= toolCreateNewKeyframe_LastFrame; frame++)
                    {
                        bool set = frame == toolCreateNewKeyframe_FirstFrame || frame == toolCreateNewKeyframe_LastFrame || toolCreateNewKeyframe_IntervalFrame == 0;
                        if (!set)
                        {
                            set = ++interval >= toolCreateNewKeyframe_IntervalFrame;
                        }
                        if (!set) continue;
                        interval = 0;
                        var time = GetFrameTime(frame);
                        times.Add(time);
                    }
                }

                if (IsHuman)
                {
                    if (selectRoot)
                    {
                        if (toolCreateNewKeyframe_AnimatorRootT)
                        {
                            var saveValues = new Dictionary<float, Vector3>();
                            foreach (var time in times)
                                saveValues.Add(time, GetAnimationValueAnimatorRootT(time));
                            foreach (var pair in saveValues)
                                SetAnimationValueAnimatorRootT(pair.Value, pair.Key);
                        }
                        if (toolCreateNewKeyframe_AnimatorRootQ)
                        {
                            var saveValues = new Dictionary<float, Quaternion>();
                            foreach (var time in times)
                                saveValues.Add(time, GetAnimationValueAnimatorRootQ(time));
                            foreach (var pair in saveValues)
                                SetAnimationValueAnimatorRootQ(pair.Value, pair.Key);
                        }
                    }
                    foreach (var humanoidIndex in humanoidIndexes)
                    {
                        if (toolCreateNewKeyframe_AnimatorMuscle)
                        {
                            for (int dof = 0; dof < 3; dof++)
                            {
                                var muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, dof);
                                if (muscleIndex < 0)
                                    continue;
                                var saveValues = new Dictionary<float, float>();
                                foreach (var time in times)
                                    saveValues.Add(time, GetAnimationValueAnimatorMuscle(muscleIndex, time));
                                foreach (var pair in saveValues)
                                    SetAnimationValueAnimatorMuscle(muscleIndex, pair.Value, pair.Key);
                            }
                        }
                        if (HumanoidHasTDoF && toolCreateNewKeyframe_AnimatorTDOF)
                        {
                            if (HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] != null)
                            {
                                var tdof = HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index;
                                var saveValues = new Dictionary<float, Vector3>();
                                foreach (var time in times)
                                    saveValues.Add(time, GetAnimationValueAnimatorTDOF(tdof, time));
                                foreach (var pair in saveValues)
                                    SetAnimationValueAnimatorTDOF(tdof, pair.Value, pair.Key);
                            }
                        }
                    }
                }

                foreach (var boneIndex in boneIndexes)
                {
                    if (IsHuman && HumanoidConflict[boneIndex])
                        continue;
                    if (RootMotionBoneIndex >= 0 && boneIndex == 0)
                        continue;

                    if (toolCreateNewKeyframe_TransformPosition)
                    {
                        var saveValues = new Dictionary<float, Vector3>();
                        foreach (var time in times)
                            saveValues.Add(time, GetAnimationValueTransformPosition(boneIndex, time));
                        foreach (var pair in saveValues)
                            SetAnimationValueTransformPosition(boneIndex, pair.Value, pair.Key);
                    }
                    if (toolCreateNewKeyframe_TransformRotation)
                    {
                        var saveValues = new Dictionary<float, Quaternion>();
                        foreach (var time in times)
                            saveValues.Add(time, GetAnimationValueTransformRotation(boneIndex, time));
                        foreach (var pair in saveValues)
                            SetAnimationValueTransformRotation(boneIndex, pair.Value, pair.Key);
                    }
                    if (toolCreateNewKeyframe_TransformScale)
                    {
                        var saveValues = new Dictionary<float, Vector3>();
                        foreach (var time in times)
                            saveValues.Add(time, GetAnimationValueTransformScale(boneIndex, time));
                        foreach (var pair in saveValues)
                            SetAnimationValueTransformScale(boneIndex, pair.Value, pair.Key);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsRotationCurveInterpolation(AnimationClip clip, RotationCurveInterpolationMode rotationCurveInterpolationMode)
        {
            if (!ToolsFixOverRotationCurve(clip)) return;

            if (!ToolsCommonBefore(clip, "RotationCurveInterpolation")) return;

            try
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                int progressIndex = 0;
                int progressTotal = bindings.Length + 1;

                {
                    List<EditorCurveBinding> convertBindings = new();
                    for (int i = 0; i < bindings.Length; i++)
                    {
                        EditorUtility.DisplayProgressBar("Read", string.IsNullOrEmpty(bindings[i].path) ? bindings[i].propertyName : bindings[i].path, progressIndex++ / (float)progressTotal);
                        if (!IsTransformRotationCurveBinding(bindings[i])) continue;
                        var mode = URotationCurveInterpolation.GetModeFromCurveData(bindings[i]);
                        if (convertBindings.FindIndex((x) => x.path == bindings[i].path) < 0)
                        {
                            var boneIndex = GetBoneIndexFromCurveBinding(bindings[i]);
                            if (boneIndex >= 0)
                            {
                                switch (mode)
                                {
                                    case URotationCurveInterpolation.Mode.RawQuaternions:
                                        if (rotationCurveInterpolationMode != RotationCurveInterpolationMode.Quaternion)
                                        {
                                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                convertBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dofIndex, URotationCurveInterpolation.Mode.Baked));
                                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                convertBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dofIndex, URotationCurveInterpolation.Mode.NonBaked));
                                        }
                                        else
                                        {
                                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                convertBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dofIndex, URotationCurveInterpolation.Mode.Baked));
                                        }
                                        break;
                                    case URotationCurveInterpolation.Mode.RawEuler:
                                        if (rotationCurveInterpolationMode != RotationCurveInterpolationMode.EulerAngles)
                                        {
                                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                convertBindings.Add(AnimationCurveBindingTransformRotation(boneIndex, dofIndex, URotationCurveInterpolation.Mode.RawEuler));
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    {
                        URotationCurveInterpolation.Mode mode = (URotationCurveInterpolation.Mode)(-1);
                        switch (rotationCurveInterpolationMode)
                        {
                            case RotationCurveInterpolationMode.Quaternion: mode = URotationCurveInterpolation.Mode.NonBaked; break;
                            case RotationCurveInterpolationMode.EulerAngles: mode = URotationCurveInterpolation.Mode.RawEuler; break;
                            default: Assert.IsTrue(false); break;
                        }
                        EditorUtility.DisplayProgressBar("Convert", "", progressIndex++ / (float)progressTotal);
                        if (convertBindings.Count > 0)
                            URotationCurveInterpolation.SetInterpolation(clip, convertBindings.ToArray(), mode);
                    }
                }
                #region FixReverseRotation
                if (rotationCurveInterpolationMode == RotationCurveInterpolationMode.EulerAngles)
                {
                    bindings = AnimationUtility.GetCurveBindings(clip);
                    foreach (var binding in bindings)
                    {
                        if (!IsTransformRotationCurveBinding(binding)) continue;
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        if (FixReverseRotationEuler(curve))
                            AnimationUtility.SetEditorCurve(clip, binding, curve);
                    }
                }
                #endregion
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsKeyframeReduction(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "KeyframeReduction")) return;

            ToolsRotationCurveInterpolation(clip, RotationCurveInterpolationMode.Quaternion);

            AnimationClip tmpClip = null;
            GameObject tmpObject = null;

            try
            {
                VeryAnimationWindow.CustomAssetModificationProcessor.Pause();

                var assetPath = string.Format("{0}/{1}_tmp.dae", EditorCommon.GetAssetPath(clip), clip.name);
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                var path = Application.dataPath + assetPath["Assets".Length..];

                if (!TransformPoseSave.ResetPrefabTransform())
                    TransformPoseSave.ResetOriginalTransform();
                if (!BlendShapeWeightSave.ResetPrefabWeight())
                    BlendShapeWeightSave.ResetOriginalWeight();

                tmpClip = AnimationClip.Instantiate(clip);
                tmpClip.hideFlags |= HideFlags.HideAndDontSave;
                tmpObject = GameObject.Instantiate(VAW.GameObject);
                tmpObject.hideFlags |= HideFlags.HideAndDontSave;
                EditorCommon.DisableOtherBehaviors(tmpObject);

                #region AddOtherCurve
                var otherCurveDic = new Dictionary<EditorCurveBinding, EditorCurveBinding>();
                {
                    var bindings = AnimationUtility.GetCurveBindings(tmpClip);
                    foreach (var binding in bindings)
                    {
                        if (binding.type == typeof(Transform))
                            continue;
                        if (binding.type == typeof(Animator))
                        {
                            if (!IsHuman && (binding.propertyName.StartsWith("RootT.") || binding.propertyName.StartsWith("RootQ.")))
                            {
                                //Root
                            }
                            else if (binding.propertyName.StartsWith("MotionT.") || binding.propertyName.StartsWith("MotionQ."))
                            {
                                //Motion
                            }
                            else if (!IsAnimatorReservedPropertyName(binding.propertyName))
                            {
                                //ParameterRelatedCurves
                            }
                            else
                            {
                                continue;
                            }
                        }
                        var valueType = AnimationUtility.GetEditorCurveValueType(VAW.GameObject, binding);
                        if (valueType != typeof(float))
                            continue;
                        var curve = AnimationUtility.GetEditorCurve(tmpClip, binding);
                        if (curve == null)
                            continue;
                        AnimationUtility.SetEditorCurve(tmpClip, binding, null);
                        var add = new GameObject(binding.GetHashCode().ToString());
                        add.hideFlags |= HideFlags.HideAndDontSave;
                        add.transform.SetParent(tmpObject.transform);
                        add.transform.localScale = Vector3.zero;
                        var addBinding = new EditorCurveBinding()
                        {
                            type = typeof(Transform),
                            path = AnimationUtility.CalculateTransformPath(add.transform, tmpObject.transform),
                            propertyName = EditorCurveBindingTransformScalePropertyNames[0],
                        };
                        AnimationUtility.SetEditorCurve(tmpClip, addBinding, curve);
                        otherCurveDic.Add(binding, addBinding);
                    }
                }
                #endregion

                List<Transform> transforms;
                {
                    var tmpBones = EditorCommon.GetHierarchyGameObject(tmpObject);
                    transforms = new List<Transform>();
                    foreach (var b in tmpBones)
                        transforms.Add(b.transform);
                }

                AnimationClip[] clips = new AnimationClip[] { tmpClip };

                DaeExporter exporter = new()
                {
                    settings_activeOnly = false,
                    settings_exportMesh = false,
                    settings_iKOnFeet = false,
                    settings_animationRigging = false,
                    settings_animationType = IsHuman ? ModelImporterAnimationType.Human : (VAW.Animator != null ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.Legacy),
                    settings_motionNodePath = RootMotionBoneIndex >= 0 ? BonePaths[RootMotionBoneIndex] : null,
                };
                if (VAW.Animator != null)
                    exporter.settings_avatar = VAW.Animator.avatar;
                var result = exporter.Export(path, transforms, clips);
                if (result)
                {
                    Assert.IsTrue(exporter.exportedFiles.Count == 2);
                    try
                    {
                        AnimationClip reductionClip = null;
                        {
                            var subAssetPath = FileUtil.GetProjectRelativePath(exporter.exportedFiles[1]);
                            var importer = AssetImporter.GetAtPath(subAssetPath);
                            if (importer is ModelImporter)
                            {
                                var modelImporter = importer as ModelImporter;
                                modelImporter.importAnimation = true;
                                modelImporter.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                                modelImporter.animationRotationError = toolKeyframeReduction_RotationError;
                                modelImporter.animationPositionError = toolKeyframeReduction_PositionError;
                                modelImporter.animationScaleError = toolKeyframeReduction_ScaleAndOthersError;
                                modelImporter.SaveAndReimport();
                            }
                            reductionClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(subAssetPath);
                        }
                        Assert.IsNotNull(reductionClip);
                        {
                            Dictionary<EditorCurveBinding, AnimationCurve> curves = new();
                            var bindings = AnimationUtility.GetCurveBindings(clip);
                            foreach (var binding in bindings)
                            {
                                var srcCurve = AnimationUtility.GetEditorCurve(clip, binding);
                                if (srcCurve == null) continue;
                                var reductionCurve = AnimationUtility.GetEditorCurve(reductionClip, binding);
                                if (reductionCurve != null)
                                {
                                    #region CopyCurve
                                    if (!toolKeyframeReduction_EnableHumanoid && binding.type == typeof(Animator))
                                        continue;
                                    if (toolKeyframeReduction_EnableHumanoid && binding.type == typeof(Animator) &&
                                        !toolKeyframeReduction_EnableHumanoidRootAndIKGoal && (IsAnimatorRootCurveBinding(binding) || GetIkTIndexFromCurveBinding(binding) != AnimatorIKIndex.None || GetIkQIndexFromCurveBinding(binding) != AnimatorIKIndex.None))
                                        continue;
                                    if (!toolKeyframeReduction_EnableGeneric && binding.type == typeof(Transform))
                                        continue;
                                    if (srcCurve.length > reductionCurve.length)
                                    {
                                        if (GetRootQDofIndexFromCurveBinding(binding) >= 0 ||
                                             GetIkQIndexFromCurveBinding(binding) >= 0 ||
                                             URotationCurveInterpolation.GetModeFromCurveData(binding) == URotationCurveInterpolation.Mode.RawQuaternions)
                                        {
                                            #region Quaternion
                                            bool allClear = true;
                                            for (int dof = 0; dof < 4; dof++)
                                            {
                                                var subBinding = GetDOFIndexChangeCurveBinding(binding, dof);
                                                var subSrcCurve = AnimationUtility.GetEditorCurve(clip, subBinding);
                                                var subReductionCurve = AnimationUtility.GetEditorCurve(reductionClip, subBinding);
                                                if (subSrcCurve == null || subReductionCurve == null ||
                                                    subSrcCurve.length <= subReductionCurve.length)
                                                {
                                                    allClear = false;
                                                    break;
                                                }
                                            }
                                            if (!allClear)
                                                continue;
                                            #endregion
                                        }
                                        curves.Add(binding, reductionCurve);
                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region CopyOtherCurve
                                    if (!toolKeyframeReduction_EnableOther)
                                        continue;
                                    if (otherCurveDic.TryGetValue(binding, out EditorCurveBinding origBinding))
                                    {
                                        reductionCurve = AnimationUtility.GetEditorCurve(reductionClip, origBinding);
                                        if (reductionCurve != null)
                                        {
                                            if (srcCurve.length > reductionCurve.length)
                                            {
                                                curves.Add(binding, reductionCurve);
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                            foreach (var pair in curves)
                            {
                                AnimationUtility.SetEditorCurve(clip, pair.Key, null);
                            }
                            foreach (var pair in curves)
                            {
                                AnimationUtility.SetEditorCurve(clip, pair.Key, pair.Value);
                            }
                        }
                    }
                    finally
                    {
                        foreach (var p in exporter.exportedFiles)
                        {
                            var pTmp = FileUtil.GetProjectRelativePath(p);
                            AssetDatabase.DeleteAsset(pTmp);
                        }
                        AssetDatabase.Refresh();
                    }
                }
                #region SimpleReductionKeyframe
                if (toolKeyframeReduction_EnableOther)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (binding.type == typeof(Animator) || binding.type == typeof(Transform))
                            continue;
                        var valueType = AnimationUtility.GetEditorCurveValueType(VAW.GameObject, binding);
                        if (valueType == null || valueType == typeof(float))
                            continue;
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        if (curve == null)
                            continue;
                        bool update = false;
                        for (int i = 1; i < curve.length - 1; i++)
                        {
                            if (Mathf.Approximately(curve[i - 1].value, curve[i].value) &&
                                Mathf.Approximately(curve[i + 1].value, curve[i].value))
                            {
                                curve.RemoveKey(i--);
                                update = true;
                            }
                        }
                        if (update)
                            AnimationUtility.SetEditorCurve(clip, binding, curve);
                    }
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    {
                        var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        if (curve == null)
                            continue;
                        bool update = false;
                        var keys = new List<ObjectReferenceKeyframe>(curve);
                        for (int i = 1; i < keys.Count - 1; i++)
                        {
                            if (keys[i - 1].value == keys[i].value)
                            {
                                keys.RemoveAt(i--);
                                update = true;
                            }
                        }
                        if (update)
                            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
                    }
                }
                #endregion
            }
            finally
            {
                if (tmpClip != null)
                    AnimationClip.DestroyImmediate(tmpClip);
                if (tmpObject != null)
                    GameObject.DestroyImmediate(tmpObject);
                VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
            }

            ToolsCommonAfter();
        }
        private void ToolsEnsureQuaternionContinuity(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "EnsureQuaternionContinuity")) return;

            {
                clip.EnsureQuaternionContinuity();
            }

            ToolsCommonAfter();
        }
        private void ToolsCleanup(AnimationClip clip)
        {
            if (!ToolsCommonBefore(clip, "Cleanup")) return;

            try
            {
                void RemoveMuscleCurve(HumanBodyBones hi)
                {
                    for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                    {
                        var mi = HumanTrait.MuscleFromBone((int)hi, dofIndex);
                        if (mi < 0) continue;
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMuscle(mi), null);
                    }
                }
                void RemoveTDofCurve(AnimatorTDOFIndex tdofIndex)
                {
                    for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorTDOF(tdofIndex, dofIndex), null);
                }
                var bindings = AnimationUtility.GetCurveBindings(clip);

                int progressIndex = 0;
                int progressTotal = 17;

                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveRoot)
                {
                    for (int i = 0; i < AnimationCurveBindingAnimatorRootT.Length; i++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootT[i], null);
                    for (int i = 0; i < AnimationCurveBindingAnimatorRootQ.Length; i++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootQ[i], null);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveIK)
                {
                    for (int ikIndex = 0; ikIndex < (int)AnimatorIKIndex.Total; ikIndex++)
                    {
                        for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkT((AnimatorIKIndex)ikIndex, dofIndex), null);
                        for (int dofIndex = 0; dofIndex < 4; dofIndex++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkQ((AnimatorIKIndex)ikIndex, dofIndex), null);
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveTDOF)
                {
                    for (int tdofIndex = 0; tdofIndex < (int)AnimatorTDOFIndex.Total; tdofIndex++)
                        RemoveTDofCurve((AnimatorTDOFIndex)tdofIndex);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveMotion)
                {
                    for (int i = 0; i < AnimationCurveBindingAnimatorMotionT.Length; i++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[i], null);
                    for (int i = 0; i < AnimationCurveBindingAnimatorMotionQ.Length; i++)
                        AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionQ[i], null);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveFinger)
                {
                    for (var hi = HumanBodyBones.LeftThumbProximal; hi <= HumanBodyBones.RightLittleDistal; hi++)
                        RemoveMuscleCurve(hi);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveEyes)
                {
                    RemoveMuscleCurve(HumanBodyBones.LeftEye);
                    RemoveMuscleCurve(HumanBodyBones.RightEye);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveJaw)
                {
                    RemoveMuscleCurve(HumanBodyBones.Jaw);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveToes)
                {
                    RemoveMuscleCurve(HumanBodyBones.LeftToes);
                    RemoveMuscleCurve(HumanBodyBones.RightToes);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveTransformPosition || toolCleanup_RemoveTransformRotation || toolCleanup_RemoveTransformScale)
                {
                    foreach (var binding in bindings)
                    {
                        if (binding.type == typeof(Transform))
                        {
                            if ((toolCleanup_RemoveTransformPosition && binding.propertyName.StartsWith("m_LocalPosition.")) ||
                                (toolCleanup_RemoveTransformRotation && (binding.propertyName.StartsWith("m_LocalRotation.") || binding.propertyName.StartsWith("localEulerAngles"))) ||
                                (toolCleanup_RemoveTransformScale && binding.propertyName.StartsWith("m_LocalScale.")))
                            {
                                AnimationUtility.SetEditorCurve(clip, binding, null);
                            }
                        }
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveBlendShape)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (IsSkinnedMeshRendererBlendShapeCurveBinding(binding))
                        {
                            AnimationUtility.SetEditorCurve(clip, binding, null);
                        }
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveObjectReference)
                {
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    {
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveEvent)
                {
                    AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveMissing)
                {
                    foreach (var binding in UAw.GetMissingCurveBindings())
                    {
                        if (!binding.isPPtrCurve)
                            AnimationUtility.SetEditorCurve(clip, binding, null);
                        else
                            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveHumanoidConflict && IsHuman)
                {
                    List<string> paths = new();
                    for (int i = 0; i < Bones.Length; i++)
                    {
                        if (HumanoidConflict[i])
                        {
                            paths.Add(AnimationUtility.CalculateTransformPath(Bones[i].transform, VAW.GameObject.transform.transform));
                        }
                    }
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (binding.type != typeof(Transform)) continue;
                        if (!paths.Contains(binding.path)) continue;
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveRootMotionConflict && RootMotionBoneIndex >= 0)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (!IsTransformPositionCurveBinding(binding) && !IsTransformRotationCurveBinding(binding)) continue;
                        var boneIndex = GetBoneIndexFromCurveBinding(binding);
                        if (boneIndex == 0)
                            AnimationUtility.SetEditorCurve(clip, binding, null);
                    }
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveUnnecessary)
                {
                    ToolsReductionCurve(clip);
                }
                EditorUtility.DisplayProgressBar("Cleanup", "", progressIndex++ / (float)progressTotal);
                if (toolCleanup_RemoveAvatarMaskDisable && toolCleanup_RemoveAvatarMask != null)
                {
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root))
                    {
                        for (int i = 0; i < AnimationCurveBindingAnimatorRootT.Length; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootT[i], null);
                        for (int i = 0; i < AnimationCurveBindingAnimatorRootQ.Length; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorRootQ[i], null);
                        for (int i = 0; i < AnimationCurveBindingAnimatorMotionT.Length; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionT[i], null);
                        for (int i = 0; i < AnimationCurveBindingAnimatorMotionQ.Length; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorMotionQ[i], null);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body))
                    {
                        RemoveMuscleCurve(HumanBodyBones.UpperChest);
                        RemoveMuscleCurve(HumanBodyBones.Chest);
                        RemoveMuscleCurve(HumanBodyBones.Spine);
                        RemoveTDofCurve(AnimatorTDOFIndex.UpperChest);
                        RemoveTDofCurve(AnimatorTDOFIndex.Chest);
                        RemoveTDofCurve(AnimatorTDOFIndex.Spine);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Head))
                    {
                        RemoveMuscleCurve(HumanBodyBones.Neck);
                        RemoveMuscleCurve(HumanBodyBones.Head);
                        RemoveMuscleCurve(HumanBodyBones.LeftEye);
                        RemoveMuscleCurve(HumanBodyBones.RightEye);
                        RemoveMuscleCurve(HumanBodyBones.Jaw);
                        RemoveTDofCurve(AnimatorTDOFIndex.Neck);
                        RemoveTDofCurve(AnimatorTDOFIndex.Head);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg))
                    {
                        RemoveMuscleCurve(HumanBodyBones.LeftUpperLeg);
                        RemoveMuscleCurve(HumanBodyBones.LeftLowerLeg);
                        RemoveMuscleCurve(HumanBodyBones.LeftFoot);
                        RemoveMuscleCurve(HumanBodyBones.LeftToes);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftUpperLeg);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftLowerLeg);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftFoot);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftToes);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg))
                    {
                        RemoveMuscleCurve(HumanBodyBones.RightUpperLeg);
                        RemoveMuscleCurve(HumanBodyBones.RightLowerLeg);
                        RemoveMuscleCurve(HumanBodyBones.RightFoot);
                        RemoveMuscleCurve(HumanBodyBones.RightToes);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightUpperLeg);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightLowerLeg);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightFoot);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightToes);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm))
                    {
                        RemoveMuscleCurve(HumanBodyBones.LeftShoulder);
                        RemoveMuscleCurve(HumanBodyBones.LeftUpperArm);
                        RemoveMuscleCurve(HumanBodyBones.LeftLowerArm);
                        RemoveMuscleCurve(HumanBodyBones.LeftHand);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftShoulder);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftUpperArm);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftLowerArm);
                        RemoveTDofCurve(AnimatorTDOFIndex.LeftHand);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm))
                    {
                        RemoveMuscleCurve(HumanBodyBones.RightShoulder);
                        RemoveMuscleCurve(HumanBodyBones.RightUpperArm);
                        RemoveMuscleCurve(HumanBodyBones.RightLowerArm);
                        RemoveMuscleCurve(HumanBodyBones.RightHand);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightShoulder);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightUpperArm);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightLowerArm);
                        RemoveTDofCurve(AnimatorTDOFIndex.RightHand);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers))
                    {
                        for (var hi = HumanBodyBones.LeftThumbProximal; hi <= HumanBodyBones.LeftLittleDistal; hi++)
                            RemoveMuscleCurve(hi);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers))
                    {
                        for (var hi = HumanBodyBones.RightThumbProximal; hi <= HumanBodyBones.RightLittleDistal; hi++)
                            RemoveMuscleCurve(hi);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK))
                    {
                        for (int i = 0; i < 3; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkT(AnimatorIKIndex.LeftFoot, i), null);
                        for (int i = 0; i < 4; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkQ(AnimatorIKIndex.LeftFoot, i), null);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK))
                    {
                        for (int i = 0; i < 3; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkT(AnimatorIKIndex.RightFoot, i), null);
                        for (int i = 0; i < 4; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkQ(AnimatorIKIndex.RightFoot, i), null);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK))
                    {
                        for (int i = 0; i < 3; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkT(AnimatorIKIndex.LeftHand, i), null);
                        for (int i = 0; i < 4; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkQ(AnimatorIKIndex.LeftHand, i), null);
                    }
                    if (!toolCleanup_RemoveAvatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK))
                    {
                        for (int i = 0; i < 3; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkT(AnimatorIKIndex.RightHand, i), null);
                        for (int i = 0; i < 4; i++)
                            AnimationUtility.SetEditorCurve(clip, AnimationCurveBindingAnimatorIkQ(AnimatorIKIndex.RightHand, i), null);
                    }
                    for (int i = 0; i < toolCleanup_RemoveAvatarMask.transformCount; i++)
                    {
                        if (!toolCleanup_RemoveAvatarMask.GetTransformActive(i))
                        {
                            var path = toolCleanup_RemoveAvatarMask.GetTransformPath(i);
                            foreach (var binding in bindings)
                            {
                                if (binding.path == path)
                                    AnimationUtility.SetEditorCurve(clip, binding, null);
                            }
                        }
                    }
                }

            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsFixErrors(AnimationClip clip)
        {
            if (!ToolsFixOverRotationCurve(clip)) return;

            if (!ToolsCommonBefore(clip, "Fix Errors")) return;

            try
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);

                int progressIndex = 0;
                int progressTotal = bindings.Length;

                foreach (var binding in bindings)
                {
                    EditorUtility.DisplayProgressBar("Fix Errors", "", progressIndex++ / (float)progressTotal);

                    #region There must be at least two keyframes. If not, an Assert will occur.[AnimationUtility.GetEditorCurve]
                    if (IsTransformRotationCurveBinding(binding) && URotationCurveInterpolation.GetModeFromCurveData(binding) == URotationCurveInterpolation.Mode.RawQuaternions)
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        if (curve.length <= 1)
                        {
                            void ErrorAvoidance(float time)
                            {
                                while (curve.length < 2)
                                {
                                    var addTime = 0f;
                                    if (time != 0f) addTime = 0f;
                                    else if (clip.length != 0f) addTime = clip.length;
                                    else addTime = 1f;
                                    AddKeyframe(curve, addTime, curve.Evaluate(addTime));
                                }
                            }
                            ErrorAvoidance(0f);
                            AnimationUtility.SetEditorCurve(clip, binding, curve);
                            Debug.LogWarningFormat(Language.GetText(Language.Help.LogFixErrors), binding.path, binding.propertyName);
                        }
                    }
                    #endregion
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsAdditiveReferencePose(AnimationClip clip, bool isAllClip)
        {
            if (!ToolsCommonBefore(clip, "Additive Reference Pose")) return;

            AnimationClip[] allClips;
            if (!isAllClip)
                allClips = new AnimationClip[] { clip };
            else
                allClips = AnimationUtility.GetAnimationClips(VAW.GameObject).Distinct().ToArray();

            try
            {
                Undo.RecordObjects(allClips, "Additive Reference Pose");

                int progressIndex = 0;
                int progressTotal = allClips.Length;
                foreach (var c in allClips)
                {
                    EditorUtility.DisplayProgressBar("Additive Reference Pose", "", progressIndex++ / (float)progressTotal);

                    if ((c.hideFlags & HideFlags.NotEditable) != HideFlags.None)
                    {
                        EditorCommon.ShowNotification("Read-Only");
                        Debug.LogErrorFormat(Language.GetText(Language.Help.LogAnimationClipReadOnlyError), c.name);
                        continue;
                    }

                    if (toolAdditiveReferencePose_Has)
                    {
                        var bindings = AnimationUtility.GetCurveBindings(c);
                        var poseBindings = AnimationUtility.GetCurveBindings(toolAdditiveReferencePose_Clip);

                        var missingBindings = bindings.Where(x => !poseBindings.Contains(x)).ToArray();
                        if (missingBindings.Length > 0)
                        {
                            if (toolAdditiveReferencePose_Clip.hideFlags.HasFlag(HideFlags.NotEditable))
                            {
                                Debug.LogFormat(Language.GetText(Language.Help.LogToolsAdditiveReferencePoseMissingCurvesError), toolAdditiveReferencePose_Clip.name);
                            }
                            else
                            {
                                Undo.RecordObject(toolAdditiveReferencePose_Clip, "Additive Reference Pose");

                                foreach (var binding in missingBindings)
                                {
                                    var baseCurve = AnimationUtility.GetEditorCurve(c, binding);
                                    var curve = new AnimationCurve(new Keyframe[] { new(0f, baseCurve.Evaluate(0f)) });
                                    AnimationUtility.SetEditorCurve(toolAdditiveReferencePose_Clip, binding, curve);
                                }
                                Debug.LogFormat(Language.GetText(Language.Help.LogToolsAdditiveReferencePoseAddMissingCurves), toolAdditiveReferencePose_Clip.name);
                            }
                        }

                        AnimationUtility.SetAdditiveReferencePose(c, toolAdditiveReferencePose_Clip, toolAdditiveReferencePose_Time);
                    }
                    else
                    {
                        AnimationUtility.SetAdditiveReferencePose(c, null, 0f);
                    }

                    Debug.LogFormat(Language.GetText(Language.Help.LogToolsAdditiveReferencePoseChanged), c.name);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsAnimCompression(AnimationClip clip, bool isAllClip)
        {
            if (!ToolsCommonBefore(clip, "Anim Compression")) return;

            AnimationClip[] allClips;
            if (!isAllClip)
                allClips = new AnimationClip[] { clip };
            else
                allClips = AnimationUtility.GetAnimationClips(VAW.GameObject).Distinct().ToArray();

            try
            {
                Undo.RecordObjects(allClips, "Anim Compression");

                int progressIndex = 0;
                int progressTotal = allClips.Length;
                foreach (var c in allClips)
                {
                    EditorUtility.DisplayProgressBar("Anim Compression", "", progressIndex++ / (float)progressTotal);

                    if ((c.hideFlags & HideFlags.NotEditable) != HideFlags.None)
                    {
                        EditorCommon.ShowNotification("Read-Only");
                        Debug.LogErrorFormat(Language.GetText(Language.Help.LogAnimationClipReadOnlyError), c.name);
                        continue;
                    }

                    bool changed = false;

                    var so = new SerializedObject(c);
                    {
                        var sp = so.FindProperty("m_Compressed");
                        if (sp.boolValue != toolAnimCompression_Compressed)
                        {
                            sp.boolValue = toolAnimCompression_Compressed;
                            changed = true;
                        }
                    }
                    if (!c.legacy)
                    {
                        var sp = so.FindProperty("m_UseHighQualityCurve");
                        if (sp.boolValue != toolAnimCompression_UseHighQualityCurve)
                        {
                            sp.boolValue = toolAnimCompression_UseHighQualityCurve;
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        so.ApplyModifiedProperties();
                        Debug.LogFormat(Language.GetText(Language.Help.LogToolsAnimCompressionChanged), c.name);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ToolsCommonAfter();
        }
        private void ToolsExport()
        {
            string path = EditorUtility.SaveFilePanel("Export",
                                                        EditorCommon.GetAssetPath(CurrentClip),
                                                        VAW.GameObject.name + ".dae", "dae");
            if (string.IsNullOrEmpty(path))
                return;

            if (!TransformPoseSave.ResetPrefabTransform())
                TransformPoseSave.ResetOriginalTransform();
            if (!BlendShapeWeightSave.ResetPrefabWeight())
                BlendShapeWeightSave.ResetOriginalWeight();

            var transforms = new List<Transform>(Bones.Length);
            foreach (var b in Bones)
                transforms.Add(b.transform);

            AnimationClip[] clips = null;
            switch (toolExport_AnimationMode)
            {
                case ExportAnimationMode.None:
                    clips = null;
                    break;
                case ExportAnimationMode.CurrentClip:
                    clips = new AnimationClip[] { CurrentClip };
                    break;
                case ExportAnimationMode.AllClips:
                    clips = AnimationUtility.GetAnimationClips(VAW.GameObject).Distinct().ToArray();
                    break;
            }

            try
            {
                VeryAnimationWindow.CustomAssetModificationProcessor.Pause();

                DaeExporter exporter = new()
                {
                    settings_activeOnly = toolExport_ActiveOnly,
                    settings_exportMesh = toolExport_Mesh,
                    settings_iKOnFeet = toolExport_BakeFootIK,
                    settings_animationRigging = toolExport_BakeAnimationRigging,
                    settings_animationType = IsHuman ? ModelImporterAnimationType.Human : (VAW.Animator != null ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.Legacy),
                    settings_motionNodePath = RootMotionBoneIndex >= 0 ? BonePaths[RootMotionBoneIndex] : null,
                };
                if (VAW.Animator != null)
                    exporter.settings_avatar = VAW.Animator.avatar;
                exporter.Export(path, transforms, clips);
            }
            finally
            {
                VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
            }
            SetUpdateSampleAnimation();
            UAw.ForceRefresh();
        }
    }
}
