//#define Enable_Profiler

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if Enable_Profiler
using UnityEngine.Profiling;
#endif

#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace VeryAnimation
{
    [Serializable]
    internal class VeryAnimationEditorWindow : EditorWindow
    {
        public static VeryAnimationEditorWindow instance;

        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        #region GUI
        private bool editorExtraFoldout = true;
        private bool editorPoseFoldout = true;
        private bool editorBlendPoseFoldout = true;
        private bool editorMuscleFoldout = true;
        private bool editorHandPoseFoldout = true;
        private bool editorBlendShapeFoldout = true;
        private bool editorSelectionFoldout = true;

        private bool editorExtraVisible = true;
        private bool editorPoseVisible = true;
        private bool editorBlendPoseVisible = true;
        private bool editorMuscleVisible = true;
        private bool editorHandPoseVisible = true;
        private bool editorBlendShapeVisible = true;
        private bool editorSelectionVisible = true;

        public bool EditorSelectionOnScene { get; private set; }

        private bool editorExtraGroupHelp;
        private bool editorPoseGroupHelp;
        private bool editorBlendPoseGroupHelp;
        private bool editorMuscleGroupHelp;
        private bool editorHandPoseGroupHelp;
        private bool editorBlendShapeGroupHelp;
        private bool editorSelectionGroupHelp;
        #endregion

        #region Strings
        private readonly GUIContent[] RootCorrectionModeString = new GUIContent[(int)VeryAnimation.RootCorrectionMode.Total];
        #endregion

        #region Core
        [SerializeField]
        private ExtraTree extraTree;
        [SerializeField]
        private PoseTree poseTree;
        [SerializeField]
        private BlendPoseTree blendPoseTree;
        [SerializeField]
        private MuscleGroupTree muscleGroupTree;
        [SerializeField]
        private HandPoseTree handPoseTree;
        [SerializeField]
        private BlendShapeTree blendShapeTree;
        #endregion

        private bool initialized;

        private Vector2 editorScrollPosition;
        private Rect rangePinningDropDownButtonRect;

        public string TemplateSaveDefaultDirectory { get; set; }

        void OnEnable()
        {
            if (VAW == null || VAW.VA == null) return;

            instance = this;

            TemplateSaveDefaultDirectory = Application.dataPath;

            UpdateRootCorrectionModeString();
            Language.OnLanguageChanged += UpdateRootCorrectionModeString;

            titleContent = new GUIContent("VA Editor");
        }
        void OnDisable()
        {
            if (VAW == null || VAW.VA == null) return;

            Release();

            instance = null;
        }

        public void Initialize()
        {
            Release();

            extraTree = new ExtraTree();
            poseTree = new PoseTree();
            blendPoseTree = new BlendPoseTree();
            muscleGroupTree = new MuscleGroupTree();
            handPoseTree = new HandPoseTree();
            blendShapeTree = new BlendShapeTree();

            #region EditorPref
            {
                editorExtraFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_Extra", false);
                editorPoseFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_Pose", true);
                editorBlendPoseFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_BlendPose", false);
                editorMuscleFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_Muscle", false);
                editorHandPoseFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_HandPose", true);
                editorBlendShapeFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_BlendShape", true);
                editorSelectionFoldout = EditorPrefs.GetBool("VeryAnimation_Editor_Selection", true);

                editorExtraVisible = EditorPrefs.GetBool("VeryAnimation_Editor_ExtraVisible", true);
                editorPoseVisible = EditorPrefs.GetBool("VeryAnimation_Editor_PoseVisible", true);
                editorBlendPoseVisible = EditorPrefs.GetBool("VeryAnimation_Editor_BlendPoseVisible", false);
                editorMuscleVisible = EditorPrefs.GetBool("VeryAnimation_Editor_MuscleVisible", false);
                editorHandPoseVisible = EditorPrefs.GetBool("VeryAnimation_Editor_HandPoseVisible", true);
                editorBlendShapeVisible = EditorPrefs.GetBool("VeryAnimation_Editor_BlendShapeVisible", true);
                editorSelectionVisible = EditorPrefs.GetBool("VeryAnimation_Editor_SelectionVisible", true);

                EditorSelectionOnScene = EditorPrefs.GetBool("VeryAnimation_Editor_Selection_OnScene", false);

                VAW.VA.optionsClampMuscle = EditorPrefs.GetBool("VeryAnimation_ClampMuscle", false);
                VAW.VA.optionsAutoFootIK = EditorPrefs.GetBool("VeryAnimation_AutoFootIK", false);
                VAW.VA.optionsMirror = EditorPrefs.GetBool("VeryAnimation_MirrorEnable", false);
                VAW.VA.rootCorrectionMode = (VeryAnimation.RootCorrectionMode)EditorPrefs.GetInt("VeryAnimation_RootCorrectionMode", (int)VeryAnimation.RootCorrectionMode.Single);
                VAW.VA.extraOptionsCollision = EditorPrefs.GetBool("VeryAnimation_ExtraCollision", false);
                VAW.VA.extraOptionsSynchronizeAnimation = EditorPrefs.GetBool("VeryAnimation_ExtraSynchronizeAnimation", false);
                VAW.VA.extraOptionsOnionSkin = EditorPrefs.GetBool("VeryAnimation_ExtraOnionSkin", false);
                VAW.VA.extraOptionsRootTrail = EditorPrefs.GetBool("VeryAnimation_ExtraRootTrail", false);

                extraTree.LoadEditorPref();
                poseTree.LoadEditorPref();
                blendPoseTree.LoadEditorPref();
                muscleGroupTree.LoadEditorPref();
                handPoseTree.LoadEditorPref();
                blendShapeTree.LoadEditorPref();
            }
            #endregion

            initialized = true;
        }
        private void Release()
        {
            if (!initialized) return;

            #region EditorPref
            {
                EditorPrefs.SetBool("VeryAnimation_Editor_Extra", editorExtraFoldout);
                EditorPrefs.SetBool("VeryAnimation_Editor_Pose", editorPoseFoldout);
                EditorPrefs.SetBool("VeryAnimation_Editor_BlendPose", editorBlendPoseFoldout);
                EditorPrefs.SetBool("VeryAnimation_Editor_Muscle", editorMuscleFoldout);
                EditorPrefs.SetBool("VeryAnimation_Editor_HandPose", editorHandPoseFoldout);
                EditorPrefs.SetBool("VeryAnimation_Editor_BlendShape", editorBlendShapeFoldout);
                EditorPrefs.SetBool("VeryAnimation_Editor_Selection", editorSelectionFoldout);

                EditorPrefs.SetBool("VeryAnimation_Editor_ExtraVisible", editorExtraVisible);
                EditorPrefs.SetBool("VeryAnimation_Editor_PoseVisible", editorPoseVisible);
                EditorPrefs.SetBool("VeryAnimation_Editor_BlendPoseVisible", editorBlendPoseVisible);
                EditorPrefs.SetBool("VeryAnimation_Editor_MuscleVisible", editorMuscleVisible);
                EditorPrefs.SetBool("VeryAnimation_Editor_HandPoseVisible", editorHandPoseVisible);
                EditorPrefs.SetBool("VeryAnimation_Editor_BlendShapeVisible", editorBlendShapeVisible);
                EditorPrefs.SetBool("VeryAnimation_Editor_SelectionVisible", editorSelectionVisible);

                EditorPrefs.SetBool("VeryAnimation_ClampMuscle", VAW.VA.optionsClampMuscle);
                EditorPrefs.SetBool("VeryAnimation_AutoFootIK", VAW.VA.optionsAutoFootIK);
                EditorPrefs.SetBool("VeryAnimation_MirrorEnable", VAW.VA.optionsMirror);
                EditorPrefs.SetInt("VeryAnimation_RootCorrectionMode", (int)VAW.VA.rootCorrectionMode);
                EditorPrefs.SetBool("VeryAnimation_ExtraCollision", VAW.VA.extraOptionsCollision);
                EditorPrefs.SetBool("VeryAnimation_ExtraSynchronizeAnimation", VAW.VA.extraOptionsSynchronizeAnimation);
                EditorPrefs.SetBool("VeryAnimation_ExtraOnionSkin", VAW.VA.extraOptionsOnionSkin);
                EditorPrefs.SetBool("VeryAnimation_ExtraRootTrail", VAW.VA.extraOptionsRootTrail);

                extraTree.SaveEditorPref();
                poseTree.SaveEditorPref();
                blendPoseTree.SaveEditorPref();
                muscleGroupTree.SaveEditorPref();
                handPoseTree.SaveEditorPref();
                blendShapeTree.SaveEditorPref();
            }
            #endregion
        }

        void OnInspectorUpdate()
        {
            if (VAW == null || VAW.VA == null || !VAW.Initialized || VeryAnimationControlWindow.instance == null)
            {
                Close();
                return;
            }
        }

        void OnGUI()
        {
            if (VAW.VA == null || VAW.VA.IsEditError || !VAW.GuiStyleReady)
                return;

#if Enable_Profiler
            Profiler.BeginSample("****VeryAnimationEditorWindow.OnGUI");
#endif
            Event e = Event.current;

            #region Event
            switch (e.type)
            {
                case EventType.KeyDown:
                    if (focusedWindow == this)
                        VAW.VA.HotKeys();
                    break;
                case EventType.MouseUp:
                    SceneView.RepaintAll();
                    break;
            }
            VAW.VA.Commands();
            #endregion

            #region ToolBar
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (editorExtraVisible)
                {
                    editorExtraFoldout = GUILayout.Toggle(editorExtraFoldout, "Extra", EditorStyles.toolbarButton);
                }
                if (editorPoseVisible)
                {
                    editorPoseFoldout = GUILayout.Toggle(editorPoseFoldout, "Pose", EditorStyles.toolbarButton);
                }
                if (editorBlendPoseVisible)
                {
                    editorBlendPoseFoldout = GUILayout.Toggle(editorBlendPoseFoldout, "Blend Pose", EditorStyles.toolbarButton);
                }
                if (VAW.VA.IsHuman && editorMuscleVisible)
                {
                    editorMuscleFoldout = GUILayout.Toggle(editorMuscleFoldout, "Muscle Group", EditorStyles.toolbarButton);
                }
                if (VAW.VA.IsHuman && VAW.VA.HumanoidHasLeftHand && VAW.VA.HumanoidHasRightHand && editorHandPoseVisible)
                {
                    editorHandPoseFoldout = GUILayout.Toggle(editorHandPoseFoldout, "Hand Pose", EditorStyles.toolbarButton);
                }
                if (blendShapeTree.IsHaveBlendShapeNodes() && editorBlendShapeVisible)
                {
                    editorBlendShapeFoldout = GUILayout.Toggle(editorBlendShapeFoldout, "Blend Shape", EditorStyles.toolbarButton);
                }
                if (editorSelectionVisible)
                {
                    editorSelectionFoldout = GUILayout.Toggle(editorSelectionFoldout, "Selection", EditorStyles.toolbarButton);
                }
                {
                    if (EditorGUILayout.DropdownButton(VAW.UEditorGUI.GetTitleSettingsIcon(), FocusType.Passive, VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        GenericMenu menu = new();
                        menu.AddItem(new GUIContent("Extra"), editorExtraVisible, () =>
                        {
                            editorExtraVisible = !editorExtraVisible;
                            editorExtraFoldout = editorExtraVisible;
                            if (!editorExtraVisible)
                            {
                                VAW.VA.extraOptionsCollision = editorExtraVisible;
                                VAW.VA.extraOptionsSynchronizeAnimation = editorExtraVisible;
                                VAW.VA.SetSynchronizeAnimation(VAW.VA.extraOptionsSynchronizeAnimation);
                                VAW.VA.extraOptionsOnionSkin = editorExtraVisible;
                                VAW.VA.OnionSkin.Update();
                                VAW.VA.extraOptionsRootTrail = editorExtraVisible;
                            }
                        });
                        menu.AddItem(new GUIContent("Pose"), editorPoseVisible, () => { editorPoseVisible = !editorPoseVisible; editorPoseFoldout = editorPoseVisible; });
                        menu.AddItem(new GUIContent("Blend Pose"), editorBlendPoseVisible, () => { editorBlendPoseVisible = !editorBlendPoseVisible; editorBlendPoseFoldout = editorBlendPoseVisible; });
                        if (VAW.VA.IsHuman)
                            menu.AddItem(new GUIContent("Muscle Group"), editorMuscleVisible, () => { editorMuscleVisible = !editorMuscleVisible; editorMuscleFoldout = editorMuscleVisible; });
                        if (VAW.VA.IsHuman && VAW.VA.HumanoidHasLeftHand && VAW.VA.HumanoidHasRightHand)
                            menu.AddItem(new GUIContent("Hand Pose"), editorHandPoseVisible, () => { editorHandPoseVisible = !editorHandPoseVisible; editorHandPoseFoldout = editorHandPoseVisible; });
                        if (blendShapeTree.IsHaveBlendShapeNodes())
                            menu.AddItem(new GUIContent("Blend Shape"), editorBlendShapeVisible, () => { editorBlendShapeVisible = !editorBlendShapeVisible; editorBlendShapeFoldout = editorBlendShapeVisible; });
                        menu.AddItem(new GUIContent("Selection"), editorSelectionVisible, () => { editorSelectionVisible = !editorSelectionVisible; editorSelectionFoldout = editorSelectionVisible; });
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            if (VAW.VA.IsHuman)
                HumanoidEditorGUI();
            else
                GenericEditorGUI();

#if Enable_Profiler
            Profiler.EndSample();
#endif
        }

        private void HumanoidEditorGUI()
        {
            Event e = Event.current;
            #region Tools
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Options", EditorStyles.miniLabel, GUILayout.Width(48f));
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(VAW.VA.optionsClampMuscle, Language.GetContent(Language.Help.EditorOptionsClamp), EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(this, "Change Clamp");
                            VAW.VA.optionsClampMuscle = flag;
                        }
                    }
                    {
                        var help = Language.Help.EditorOptionsFootIK_2018_3;
                        if (VAW.VA.UAw.GetLinkedWithTimeline())
                        {
#if VERYANIMATION_TIMELINE
                            EditorGUI.BeginDisabledGroup(true);
                            GUILayout.Toggle(VAW.VA.UAw.GetTimelineAnimationApplyFootIK(), Language.GetContent(help), EditorStyles.miniButton);
                            EditorGUI.EndDisabledGroup();
#endif
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            var flag = GUILayout.Toggle(VAW.VA.optionsAutoFootIK, Language.GetContent(help), EditorStyles.miniButton);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(this, "Change Foot IK");
                                VAW.VA.optionsAutoFootIK = flag;
                                VAW.VA.SetUpdateSampleAnimation();
                                VAW.VA.SetSynchroIKtargetAll();
                                VAW.VA.SetAnimationWindowSynchroSelection();
                            }
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(VAW.VA.optionsMirror, Language.GetContent(Language.Help.EditorOptionsMirror), EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(this, "Change Mirror");
                            VAW.VA.optionsMirror = flag;
                            VAW.VA.SetAnimationWindowSynchroSelection();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(VAW.VA.RootMotionBoneIndex >= 0 && VAW.VA.IsWriteLockBone(VAW.VA.RootMotionBoneIndex));
                    EditorGUILayout.LabelField(Language.GetContent(Language.Help.EditorRootCorrection), EditorStyles.miniLabel, GUILayout.Width(88f));
                    {
                        EditorGUI.BeginChangeCheck();
                        var mode = (VeryAnimation.RootCorrectionMode)GUILayout.Toolbar((int)VAW.VA.rootCorrectionMode, RootCorrectionModeString, EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(this, "Change Root Correction Mode");
                            VAW.VA.rootCorrectionMode = mode;
                            VAW.VA.SetAnimationWindowSynchroSelection();
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            editorScrollPosition = EditorGUILayout.BeginScrollView(editorScrollPosition);

            EditorGUI_ExtraGUI();

            EditorGUI_PoseGUI();

            EditorGUI_BlendPoseGUI();

            EditorGUI_MuscleGroupGUI();

            EditorGUI_HandPoseGUI();

            EditorGUI_BlendShapeGUI();

            EditorGUI_SelectionGUI();

            EditorGUILayout.EndScrollView();
        }
        private void GenericEditorGUI()
        {
            #region Tools
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Options", GUILayout.Width(52f));
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(VAW.VA.optionsMirror, Language.GetContent(Language.Help.EditorOptionsMirror), EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(this, "Change Mirror");
                            VAW.VA.optionsMirror = flag;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion

            editorScrollPosition = EditorGUILayout.BeginScrollView(editorScrollPosition);

            EditorGUI_ExtraGUI();

            EditorGUI_PoseGUI();

            EditorGUI_BlendPoseGUI();

            EditorGUI_BlendShapeGUI();

            EditorGUI_SelectionGUI();

            EditorGUILayout.EndScrollView();
        }
        private void EditorGUI_ExtraGUI()
        {
            if (editorExtraFoldout && editorExtraVisible)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    editorExtraFoldout = EditorGUILayout.Foldout(editorExtraFoldout, "Extra", true, VAW.GuiStyleBoldFoldout);
                }
                {
                    EditorGUILayout.Space();
                    extraTree.ExtraTreeToolbarGUI();
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorExtraGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorExtraGroupHelp = !editorExtraGroupHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (editorExtraGroupHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpExtra), MessageType.Info);
                }

                extraTree.ExtraTreeGUI();
            }
        }
        private void EditorGUI_PoseGUI()
        {
            if (editorPoseFoldout && editorPoseVisible)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    editorPoseFoldout = EditorGUILayout.Foldout(editorPoseFoldout, "Pose", true, VAW.GuiStyleBoldFoldout);
                }
                {
                    EditorGUILayout.Space();
                    poseTree.PoseTreeToolbarGUI();
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorPoseGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorPoseGroupHelp = !editorPoseGroupHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (editorPoseGroupHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpPose), MessageType.Info);
                }

                poseTree.PoseTreeGUI();
            }
        }
        private void EditorGUI_BlendPoseGUI()
        {
            if (editorBlendPoseFoldout && editorBlendPoseVisible)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    editorBlendPoseFoldout = EditorGUILayout.Foldout(editorBlendPoseFoldout, "Blend Pose", true, VAW.GuiStyleBoldFoldout);
                }
                {
                    EditorGUILayout.Space();
                    blendPoseTree.BlendPoseTreeToolbarGUI();
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorBlendPoseGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorBlendPoseGroupHelp = !editorBlendPoseGroupHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (editorBlendPoseGroupHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpBlendPose), MessageType.Info);
                }

                blendPoseTree.BlendPoseTreeGUI();
            }
        }
        private void EditorGUI_MuscleGroupGUI()
        {
            if (editorMuscleFoldout && editorMuscleVisible)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    editorMuscleFoldout = EditorGUILayout.Foldout(editorMuscleFoldout, "Muscle Group", true, VAW.GuiStyleBoldFoldout);
                }
                {
                    EditorGUILayout.Space();
                    muscleGroupTree.MuscleGroupToolbarGUI();
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorMuscleGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorMuscleGroupHelp = !editorMuscleGroupHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (editorMuscleGroupHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpMuscleGroup), MessageType.Info);
                }

                muscleGroupTree.MuscleGroupTreeGUI();
            }
        }
        private void EditorGUI_HandPoseGUI()
        {
            if (!VAW.VA.IsHuman || !VAW.VA.HumanoidHasLeftHand || !VAW.VA.HumanoidHasRightHand)
                return;

            if (editorHandPoseFoldout && editorHandPoseVisible)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    editorHandPoseFoldout = EditorGUILayout.Foldout(editorHandPoseFoldout, "Hand Pose", true, VAW.GuiStyleBoldFoldout);
                }
                {
                    EditorGUILayout.Space();
                    handPoseTree.HandPoseToolbarGUI();
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorHandPoseGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorHandPoseGroupHelp = !editorHandPoseGroupHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (editorHandPoseGroupHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpHandPose), MessageType.Info);
                }

                handPoseTree.HandPoseTreeGUI();
            }
        }
        private void EditorGUI_BlendShapeGUI()
        {
            if (!blendShapeTree.IsHaveBlendShapeNodes())
                return;

            if (editorBlendShapeFoldout && editorBlendShapeVisible)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    editorBlendShapeFoldout = EditorGUILayout.Foldout(editorBlendShapeFoldout, "Blend Shape", true, VAW.GuiStyleBoldFoldout);
                }
                {
                    EditorGUILayout.Space();
                    blendShapeTree.BlendShapeTreeToolbarGUI();
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorBlendShapeGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorBlendShapeGroupHelp = !editorBlendShapeGroupHelp;
                    }
                    if (EditorGUILayout.DropdownButton(VAW.UEditorGUI.GetTitleSettingsIcon(), FocusType.Passive, VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        blendShapeTree.BlendShapeTreeSettingsMesh();
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (editorBlendShapeGroupHelp)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpBlendShape), MessageType.Info);
                }

                blendShapeTree.BlendShapeTreeGUI();
            }
        }
        public void EditorGUI_SelectionGUI(bool onScene = false)
        {
            const int FoldoutSpace = 17;
            const int FloatFieldWidth = 44;

            if (editorSelectionFoldout && editorSelectionVisible && onScene == EditorSelectionOnScene)
            {
                EditorGUILayout.BeginHorizontal();
                if (!onScene)
                {
                    editorSelectionFoldout = EditorGUILayout.Foldout(editorSelectionFoldout, "Selection", true, VAW.GuiStyleBoldFoldout);
                }
                if (VAW.VA.SelectionActiveGameObject != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(VAW.VA.SelectionActiveGameObject, typeof(GameObject), false);
                    EditorGUI.EndDisabledGroup();
                }
                else if (VAW.VA.animatorIK.IKActiveTarget != AnimatorIKCore.IKTarget.None && VAW.VA.animatorIK.ikData[(int)VAW.VA.animatorIK.IKActiveTarget].enable)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Animator IK: " + AnimatorIKCore.IKTargetStrings[(int)VAW.VA.animatorIK.IKActiveTarget]);
                    EditorGUI.EndDisabledGroup();
                }
                else if (VAW.VA.originalIK.IKActiveTarget >= 0 && VAW.VA.originalIK.ikData[VAW.VA.originalIK.IKActiveTarget].enable)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Original IK: " + VAW.VA.originalIK.ikData[VAW.VA.originalIK.IKActiveTarget].name);
                    EditorGUI.EndDisabledGroup();
                }
                else if (VAW.VA.SelectionHumanVirtualBones != null && VAW.VA.SelectionHumanVirtualBones.Count > 0)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Virtual: " + VAW.VA.SelectionHumanVirtualBones[0].ToString());
                    EditorGUI.EndDisabledGroup();
                }
                else if (VAW.VA.SelectionMotionTool)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Motion");
                    EditorGUI.EndDisabledGroup();
                }
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), editorSelectionGroupHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        editorSelectionGroupHelp = !editorSelectionGroupHelp;
                        VAW.editorWindowSelectionRect.size = Vector2.zero;
                    }
                    if (EditorGUILayout.DropdownButton(VAW.UEditorGUI.GetTitleSettingsIcon(), FocusType.Passive, VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        GenericMenu menu = new();
                        menu.AddItem(Language.GetContent(Language.Help.EditorMenuOnScene), EditorSelectionOnScene, () =>
                        {
                            EditorSelectionOnScene = !EditorSelectionOnScene;
                            EditorPrefs.SetBool("VeryAnimation_Editor_Selection_OnScene", EditorSelectionOnScene);
                            VAW.editorWindowSelectionRect.size = Vector2.zero;
                            Repaint();
                            SceneView.RepaintAll();
                        });
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.EndHorizontal();
                {
                    if (editorSelectionGroupHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpSelection), MessageType.Info);
                    }

                    EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
                    {
                        int RowCount = 0;
                        var humanoidIndex = VAW.VA.SelectionGameObjectHumanoidIndex();
                        var boneIndex = VAW.VA.SelectionActiveBone;
                        if (VAW.VA.IsHuman && (humanoidIndex >= 0 || boneIndex == VAW.VA.RootMotionBoneIndex))
                        {
                            #region Humanoid
                            if (humanoidIndex == HumanBodyBones.Hips)
                            {
                                EditorGUILayout.LabelField(Language.GetText(Language.Help.SelectionHip), VAW.GuiStyleCenterAlignLabel);
                            }
                            else if (humanoidIndex > HumanBodyBones.Hips || VAW.VA.SelectionActiveGameObject == VAW.GameObject)
                            {
                                EditorGUILayout.BeginHorizontal();
                                #region Mirror
                                var mirrorIndex = humanoidIndex >= 0 && VAW.VA.HumanoidIndex2boneIndex[(int)humanoidIndex] >= 0 ? VAW.VA.MirrorBoneIndexes[VAW.VA.HumanoidIndex2boneIndex[(int)humanoidIndex]] : -1;
                                if (GUILayout.Button(Language.GetContentFormat(Language.Help.SelectionMirror, (mirrorIndex >= 0 ? string.Format("From '{0}'", VAW.VA.Bones[mirrorIndex].name) : "From self")), GUILayout.Width(100)))
                                {
                                    VAW.VA.SetSelectionMirror();
                                }
                                #endregion
                                EditorGUILayout.Space();
                                #region Reset
                                if (GUILayout.Button("Reset All", VAW.GuiStyleDropDown, GUILayout.Width(100)))
                                {
                                    ResetAllSelectionHumanoidMenu(true, true, true);
                                }
                                #endregion
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }
                            if (boneIndex == VAW.VA.RootMotionBoneIndex)
                            {
                                #region Root
                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Root T", "Root Position\nRootT * Animator.humanScale = Position"), GUILayout.Width(64)))
                                    {
                                        VAW.VA.LastTool = Tool.Move;
                                        VAW.VA.SelectGameObject(VAW.GameObject);
                                    }
                                    EditorGUI.BeginChangeCheck();
                                    var rootT = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueAnimatorRootT());
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        VAW.VA.SetAnimationValueAnimatorRootT(rootT);
                                    }
                                    if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                    {
                                        ResetAllSelectionHumanoidMenu(true, false, false);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Root Q", "Root Rotation (Quaternion)"), GUILayout.Width(64)))
                                    {
                                        VAW.VA.LastTool = Tool.Rotate;
                                        VAW.VA.SelectGameObject(VAW.GameObject);
                                    }
                                    EditorGUI.BeginChangeCheck();
                                    var quat = VAW.VA.GetAnimationValueAnimatorRootQ();
                                    var rotation = new Vector4(quat.x, quat.y, quat.z, quat.w);
                                    rotation = EditorGUILayout.Vector4Field("", rotation);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        if (rotation.sqrMagnitude > 0f)
                                        {
                                            rotation.Normalize();
                                            quat.x = rotation.x;
                                            quat.y = rotation.y;
                                            quat.z = rotation.z;
                                            quat.w = rotation.w;
                                            VAW.VA.SetAnimationValueAnimatorRootQ(quat);
                                        }
                                    }
                                    if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                    {
                                        ResetAllSelectionHumanoidMenu(false, true, false);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Position", "Local Position"), GUILayout.Width(64)))
                                    {
                                        VAW.VA.LastTool = Tool.Move;
                                        VAW.VA.SelectGameObject(VAW.GameObject);
                                    }
                                    EditorGUI.BeginChangeCheck();
                                    var position = VAW.VA.GetHumanWorldRootPosition();
                                    position = VAW.VA.TransformPoseSave.StartMatrix.inverse.MultiplyPoint3x4(position);
                                    position = EditorGUILayout.Vector3Field("", position);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        position = VAW.VA.TransformPoseSave.StartMatrix.MultiplyPoint3x4(position);
                                        VAW.VA.SetAnimationValueAnimatorRootT(VAW.VA.GetHumanLocalRootPosition(position));
                                    }
                                    if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                    {
                                        ResetAllSelectionHumanoidMenu(true, false, false);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Rotation", "Local Rotation (Euler)"), GUILayout.Width(64)))
                                    {
                                        VAW.VA.LastTool = Tool.Rotate;
                                        VAW.VA.SelectGameObject(VAW.GameObject);
                                    }
                                    EditorGUI.BeginChangeCheck();
                                    var rootQ = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueAnimatorRootQ().eulerAngles);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        VAW.VA.SetAnimationValueAnimatorRootQ(Quaternion.Euler(rootQ));
                                    }
                                    if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                    {
                                        ResetAllSelectionHumanoidMenu(false, true, false);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion
                            }
                            else if (humanoidIndex > HumanBodyBones.Hips)
                            {
                                #region Muscle
                                if (VAW.muscleRotationSliderIds == null || VAW.muscleRotationSliderIds.Length != 3)
                                    VAW.muscleRotationSliderIds = new int[3];
                                for (int i = 0; i < VAW.muscleRotationSliderIds.Length; i++)
                                    VAW.muscleRotationSliderIds[i] = -1;
                                for (int i = 0; i < 3; i++)
                                {
                                    var muscleIndex = HumanTrait.MuscleFromBone((int)humanoidIndex, i);
                                    if (muscleIndex < 0) continue;
                                    var muscleValue = VAW.VA.GetAnimationValueAnimatorMuscle(muscleIndex);
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent(VAW.VA.MusclePropertyName.Names[muscleIndex], VAW.VA.MusclePropertyName.Names[muscleIndex]), GUILayout.Width(VAW.EditorSettings.SettingEditorNameFieldWidth)))
                                    {
                                        VAW.VA.LastTool = Tool.Rotate;
                                        VAW.VA.SelectHumanoidBones(new HumanBodyBones[] { humanoidIndex });
                                        VAW.VA.SetAnimationWindowSynchroSelection(new EditorCurveBinding[] { VAW.VA.AnimationCurveBindingAnimatorMuscle(muscleIndex) });
                                    }
                                    {
                                        var mmuscleIndex = VAW.VA.GetMirrorMuscleIndex(muscleIndex);
                                        if (mmuscleIndex >= 0)
                                        {
                                            if (GUILayout.Button(new GUIContent("", string.Format("Mirror: '{0}'", VAW.VA.MusclePropertyName.Names[mmuscleIndex])), VAW.GuiStyleMirrorButton, GUILayout.Width(VAW.MirrorTex.width), GUILayout.Height(VAW.MirrorTex.height)))
                                            {
                                                var mhumanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(mmuscleIndex);
                                                VAW.VA.SelectHumanoidBones(new HumanBodyBones[] { mhumanoidIndex });
                                                VAW.VA.SetAnimationWindowSynchroSelection(new EditorCurveBinding[] { VAW.VA.AnimationCurveBindingAnimatorMuscle(mmuscleIndex) });
                                            }
                                        }
                                        else
                                        {
                                            GUILayout.Space(FoldoutSpace);
                                        }
                                    }
                                    {
                                        var saveBackgroundColor = GUI.backgroundColor;
                                        switch (i)
                                        {
                                            case 0: GUI.backgroundColor = Handles.xAxisColor; break;
                                            case 1: GUI.backgroundColor = Handles.yAxisColor; break;
                                            case 2: GUI.backgroundColor = Handles.zAxisColor; break;
                                        }
                                        EditorGUI.BeginChangeCheck();
                                        muscleValue = GUILayout.HorizontalSlider(muscleValue, -1f, 1f);
                                        VAW.muscleRotationSliderIds[i] = VAW.UEditorGUIUtility.GetLastControlID();
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            foreach (var mi in VAW.VA.SelectionGameObjectsMuscleIndex(i))
                                            {
                                                VAW.VA.SetAnimationValueAnimatorMuscle(mi, muscleValue);
                                            }
                                        }
                                        GUI.backgroundColor = saveBackgroundColor;
                                    }
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var value2 = EditorGUILayout.FloatField(muscleValue, GUILayout.Width(FloatFieldWidth));
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            foreach (var mi in VAW.VA.SelectionGameObjectsMuscleIndex(i))
                                            {
                                                VAW.VA.SetAnimationValueAnimatorMuscleIfNotOriginal(mi, value2);
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion

                                #region Rotation
                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Rotation", "Local Rotation"), GUILayout.Width(64)))
                                    {
                                        VAW.VA.LastTool = Tool.Rotate;
                                        VAW.VA.SelectHumanoidBones(new HumanBodyBones[] { humanoidIndex });
                                    }
                                    {
                                        var muscleIndex0 = HumanTrait.MuscleFromBone((int)humanoidIndex, 0);
                                        var muscleIndex1 = HumanTrait.MuscleFromBone((int)humanoidIndex, 1);
                                        var muscleIndex2 = HumanTrait.MuscleFromBone((int)humanoidIndex, 2);
                                        var euler = new Vector3(VAW.VA.Muscle2EulerAngle(muscleIndex0, VAW.VA.GetAnimationValueAnimatorMuscle(muscleIndex0)),
                                                                VAW.VA.Muscle2EulerAngle(muscleIndex1, VAW.VA.GetAnimationValueAnimatorMuscle(muscleIndex1)),
                                                                VAW.VA.Muscle2EulerAngle(muscleIndex2, VAW.VA.GetAnimationValueAnimatorMuscle(muscleIndex2)));
                                        EditorGUI.BeginChangeCheck();
                                        euler = EditorGUILayout.Vector3Field("", euler);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < 3; i++)
                                            {
                                                var muscleValue = VAW.VA.EulerAngle2Muscle(HumanTrait.MuscleFromBone((int)humanoidIndex, i), euler[i]);
                                                foreach (var mi in VAW.VA.SelectionGameObjectsMuscleIndex(i))
                                                {
                                                    VAW.VA.SetAnimationValueAnimatorMuscle(mi, muscleValue);
                                                }
                                            }
                                        }
                                    }
                                    if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                    {
                                        ResetAllSelectionHumanoidMenu(false, true, false);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion

                                #region Position(TDOF)
                                if (VAW.VA.HumanoidHasTDoF && VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)humanoidIndex] != null)
                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Position", "Translation DoF"), GUILayout.Width(64)))
                                    {
                                        VAW.VA.LastTool = Tool.Move;
                                        VAW.VA.SelectHumanoidBones(new HumanBodyBones[] { humanoidIndex });
                                    }
                                    EditorGUI.BeginChangeCheck();
                                    var tdof = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueAnimatorTDOF(VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)humanoidIndex].index));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        foreach (var hi in VAW.VA.SelectionGameObjectsHumanoidIndex())
                                        {
                                            if (VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)hi] == null) continue;
                                            VAW.VA.SetAnimationValueAnimatorTDOF(VeryAnimation.HumanBonesAnimatorTDOFIndex[(int)hi].index, tdof);
                                        }
                                    }
                                    if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                    {
                                        ResetAllSelectionHumanoidMenu(true, false, false);
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion

                            }
                            #endregion
                        }
                        else if (boneIndex >= 0)
                        {
                            #region Generic
                            if (VAW.VA.IsHuman && VAW.VA.HumanoidConflict[boneIndex])
                            {
                                EditorGUILayout.LabelField(Language.GetText(Language.Help.SelectionHumanoidConflict), VAW.GuiStyleCenterAlignLabel);
                            }
                            else
                            {
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    #region Mirror
                                    if (GUILayout.Button(Language.GetContentFormat(Language.Help.SelectionMirror, (VAW.VA.MirrorBoneIndexes[boneIndex] >= 0 ? string.Format("From '{0}'", VAW.VA.Bones[VAW.VA.MirrorBoneIndexes[boneIndex]].name) : "From self")), GUILayout.Width(100)))
                                    {
                                        VAW.VA.SetSelectionMirror();
                                    }
                                    #endregion
                                    EditorGUILayout.Space();
                                    #region Reset
                                    if (GUILayout.Button("Reset All", VAW.GuiStyleDropDown, GUILayout.Width(100)))
                                    {
                                        ResetAllSelectionGenericMenu(true, true, true);
                                    }
                                    #endregion
                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUILayout.Space();
                                {
                                    #region Position
                                    {
                                        EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                        if (GUILayout.Button("Position", GUILayout.Width(64)))
                                        {
                                            VAW.VA.LastTool = Tool.Move;
                                            VAW.VA.SelectGameObject(VAW.VA.Bones[boneIndex]);
                                        }
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            var localPosition = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueTransformPosition(boneIndex));
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                foreach (var bi in VAW.VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                                {
                                                    VAW.VA.SetAnimationValueTransformPosition(bi, localPosition);
                                                }
                                            }
                                        }
                                        if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                        {
                                            ResetAllSelectionGenericMenu(true, false, false);
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    #endregion
                                    #region Rotation
                                    {
                                        EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                        if (GUILayout.Button("Rotation", GUILayout.Width(64)))
                                        {
                                            VAW.VA.LastTool = Tool.Rotate;
                                            VAW.VA.SelectGameObject(VAW.VA.Bones[boneIndex]);
                                        }
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            var localEulerAngles = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueTransformRotation(boneIndex).eulerAngles);
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                foreach (var bi in VAW.VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                                {
                                                    VAW.VA.SetAnimationValueTransformRotation(bi, Quaternion.Euler(localEulerAngles));
                                                }
                                            }
                                        }
                                        if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                        {
                                            ResetAllSelectionGenericMenu(false, true, false);
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    #endregion
                                    #region Scale
                                    {
                                        EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                        if (GUILayout.Button("Scale", GUILayout.Width(64)))
                                        {
                                            VAW.VA.LastTool = Tool.Scale;
                                            VAW.VA.SelectGameObject(VAW.VA.Bones[boneIndex]);
                                        }
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            var localScale = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueTransformScale(boneIndex));
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                foreach (var bi in VAW.VA.SelectionGameObjectsOtherHumanoidBoneIndex())
                                                {
                                                    VAW.VA.SetAnimationValueTransformScale(bi, localScale);
                                                }
                                            }
                                        }
                                        if (GUILayout.Button("Reset", VAW.GuiStyleDropDown, GUILayout.Width(64)))
                                        {
                                            ResetAllSelectionGenericMenu(false, false, true);
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                        }
                        else if (VAW.VA.SelectionMotionTool)
                        {
                            #region Motion
                            {
                                EditorGUILayout.BeginHorizontal();
                                #region Mirror
                                if (GUILayout.Button(Language.GetContentFormat(Language.Help.SelectionMirror, "From self"), GUILayout.Width(100)))
                                {
                                    VAW.VA.SetSelectionMirror();
                                }
                                #endregion
                                EditorGUILayout.Space();
                                #region Reset
                                if (GUILayout.Button("Reset All", GUILayout.Width(100)))
                                {
                                    VAW.VA.SetSelectionEditStart(false, false, false);
                                }
                                #endregion
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }
                            {
                                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                if (GUILayout.Button(new GUIContent("Position", "MotionT"), GUILayout.Width(64)))
                                {
                                    VAW.VA.LastTool = Tool.Move;
                                    VAW.VA.SelectMotionTool();
                                }
                                EditorGUI.BeginChangeCheck();
                                var motionT = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueAnimatorMotionT());
                                if (EditorGUI.EndChangeCheck())
                                {
                                    VAW.VA.SetAnimationValueAnimatorMotionT(motionT);
                                }
                                if (GUILayout.Button("Reset", GUILayout.Width(64f)))
                                {
                                    VAW.VA.SetAnimationValueAnimatorMotionTIfNotOriginal(Vector3.zero);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            {
                                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                if (GUILayout.Button(new GUIContent("Rotation", "MotionQ"), GUILayout.Width(64)))
                                {
                                    VAW.VA.LastTool = Tool.Rotate;
                                    VAW.VA.SelectMotionTool();
                                }
                                EditorGUI.BeginChangeCheck();
                                var motionQ = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueAnimatorMotionQ().eulerAngles);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    VAW.VA.SetAnimationValueAnimatorMotionQ(Quaternion.Euler(motionQ));
                                }
                                if (GUILayout.Button("Reset", GUILayout.Width(64f)))
                                {
                                    VAW.VA.SetAnimationValueAnimatorMotionQIfNotOriginal(Quaternion.identity);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            #endregion
                        }
                        else if (VAW.VA.animatorIK.IKActiveTarget != AnimatorIKCore.IKTarget.None)
                        {
                            VAW.VA.animatorIK.SelectionGUI();
                        }
                        else if (VAW.VA.originalIK.IKActiveTarget >= 0)
                        {
                            VAW.VA.originalIK.SelectionGUI();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(Language.GetText(Language.Help.SelectionNothingisselected), VAW.GuiStyleCenterAlignLabel);
                        }
#if VERYANIMATION_ANIMATIONRIGGING
                        if (VAW.VA.AnimationRigging.IsValid && boneIndex >= 0)
                        {
                            if (VAW.VA.Bones[boneIndex].TryGetComponent<Rig>(out var rig))
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.Space();
                                    EditorGUILayout.LabelField(new GUIContent("Animation Rigging", rig.ToString()), EditorStyles.centeredGreyMiniLabel);
                                    EditorGUILayout.Space();
                                    EditorGUILayout.EndHorizontal();
                                }

                                #region Weight
                                {
                                    EditorCurveBinding[] GetEditorCurveBindings()
                                    {
                                        var list = new List<EditorCurveBinding>();
                                        foreach (var bi in VAW.VA.SelectionBones)
                                        {
                                            if (!VAW.VA.Bones[bi].TryGetComponent<Rig>(out var rig))
                                                continue;
                                            var path = AnimationUtility.CalculateTransformPath(VAW.VA.Bones[bi].transform, VAW.GameObject.transform);
                                            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, rig.GetType(), "m_Weight");
                                            list.Add(binding);
                                        }
                                        return list.ToArray();
                                    }

                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Weight", "Rig.weight"), GUILayout.Width(128)))
                                    {
                                        VAW.VA.SetAnimationWindowSynchroSelection(GetEditorCurveBindings());
                                    }
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var weight = EditorGUILayout.Slider(rig.weight, 0f, 1f);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            var bindings = GetEditorCurveBindings();
                                            foreach (var binding in bindings)
                                            {
                                                VAW.VA.SetAnimationValueCustomProperty(binding, weight);
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion

                                EditorGUILayout.EndVertical();
                            }
                            if (VAW.VA.Bones[boneIndex].TryGetComponent<IRigConstraint>(out var rigConstraint))
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.Space();
                                    EditorGUILayout.LabelField(new GUIContent("Animation Rigging", rigConstraint.ToString()), EditorStyles.centeredGreyMiniLabel);
                                    EditorGUILayout.Space();
                                    EditorGUILayout.EndHorizontal();
                                }

                                #region Weight
                                {
                                    EditorCurveBinding[] GetEditorCurveBindings()
                                    {
                                        var list = new List<EditorCurveBinding>();
                                        foreach (var bi in VAW.VA.SelectionBones)
                                        {
                                            if (!VAW.VA.Bones[bi].TryGetComponent<IRigConstraint>(out var constraint))
                                                continue;
                                            var path = AnimationUtility.CalculateTransformPath(VAW.VA.Bones[bi].transform, VAW.GameObject.transform);
                                            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, constraint.GetType(), "m_Weight");
                                            list.Add(binding);
                                        }
                                        return list.ToArray();
                                    }

                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    if (GUILayout.Button(new GUIContent("Weight", "IRigConstraint.weight"), GUILayout.Width(128)))
                                    {
                                        VAW.VA.SetAnimationWindowSynchroSelection(GetEditorCurveBindings());
                                    }
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var weight = EditorGUILayout.Slider(rigConstraint.weight, 0f, 1f);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            var bindings = GetEditorCurveBindings();
                                            foreach (var binding in bindings)
                                            {
                                                VAW.VA.SetAnimationValueCustomProperty(binding, weight);
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                #endregion

                                {
                                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                    EditorGUILayout.Space();
                                    if (GUILayout.Button(Language.GetContent(Language.Help.SelectionRangePinning)))
                                    {
                                        var popupPosition = GUIUtility.GUIToScreenPoint(rangePinningDropDownButtonRect.center);
                                        var window = CreateInstance<VeryAnimationRangePinningWindow>();
                                        popupPosition.x -= window.position.size.x / 2f;
                                        window.ShowAsDropDown(new Rect(popupPosition, Vector2.zero), window.position.size);
                                    }
                                    if (Event.current.type == EventType.Repaint)
                                        rangePinningDropDownButtonRect = GUILayoutUtility.GetLastRect();
                                    EditorGUILayout.Space();
                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
#endif
                    }
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void UpdateRootCorrectionModeString()
        {
            for (int i = 0; i < (int)VeryAnimation.RootCorrectionMode.Total; i++)
            {
                RootCorrectionModeString[i] = new GUIContent(Language.GetContent(Language.Help.EditorRootCorrectionDisable + i));
            }
        }

        private void ResetAllSelectionHumanoidMenu(bool position, bool rotation, bool scale)
        {
            GenericMenu menu = new();
            {
                if (VAW.VA.IsHuman)
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseHumanoidReset), false, () =>
                    {
                        Undo.RecordObject(this, "Humanoid Pose");
                        VAW.VA.SetSelectionHumanoidDefault(position, rotation);
                    });
                }
                if (VAW.VA.TransformPoseSave.IsEnableHumanDescriptionTransforms())
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseAvatarConfiguration), false, () =>
                    {
                        Undo.RecordObject(this, "Avatar Configuration Pose");
                        VAW.VA.SetSelectionHumanoidAvatarConfiguration(position, rotation);
                    });
                }
                if (VAW.VA.TransformPoseSave.IsEnableTPoseTransform())
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseTPose), false, () =>
                    {
                        Undo.RecordObject(this, "T Pose");
                        VAW.VA.SetSelectionHumanoidTPose(position, rotation);
                    });
                }
                if (VAW.VA.TransformPoseSave.IsEnableBindTransform())
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseBind), false, () =>
                    {
                        Undo.RecordObject(this, "Bind Pose");
                        VAW.VA.SetSelectionBindPose(position, rotation, scale);
                    });
                }
                if (VAW.VA.TransformPoseSave.IsEnablePrefabTransform())
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPosePrefab), false, () =>
                    {
                        Undo.RecordObject(this, "Prefab Pose");
                        VAW.VA.SetSelectionPrefabPose(position, rotation, scale);
                    });
                }
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseStart), false, () =>
                    {
                        Undo.RecordObject(this, "Edit Start Pose");
                        VAW.VA.SetSelectionEditStart(position, rotation, scale);
                    });
                }
            }
            menu.ShowAsContext();
        }
        private void ResetAllSelectionGenericMenu(bool position, bool rotation, bool scale)
        {
            GenericMenu menu = new();
            {
                if (VAW.VA.TransformPoseSave.IsEnableBindTransform())
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseBind), false, () =>
                    {
                        Undo.RecordObject(this, "Bind Pose");
                        VAW.VA.SetSelectionBindPose(position, rotation, scale);
                    });
                }
                if (VAW.VA.TransformPoseSave.IsEnablePrefabTransform())
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPosePrefab), false, () =>
                    {
                        Undo.RecordObject(this, "Prefab Pose");
                        VAW.VA.SetSelectionPrefabPose(position, rotation, scale);
                    });
                }
                {
                    menu.AddItem(Language.GetContent(Language.Help.EditorPoseStart), false, () =>
                    {
                        Undo.RecordObject(this, "Edit Start Pose");
                        VAW.VA.SetSelectionEditStart(position, rotation, scale);
                    });
                }
            }
            menu.ShowAsContext();
        }

        public void PoseQuickSave(int index)
        {
            poseTree.QuickSave(index);
        }
        public void PoseQuickLoad(int index)
        {
            poseTree.QuickLoad(index);
        }

        public static void ForceRepaint()
        {
            if (instance == null) return;
            instance.Repaint();
        }
    }
}
