using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VeryAnimation
{
    internal class EditorSettings
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        public Language.LanguageType SettingLanguageType { get; private set; }
        public bool SettingComponentSaveSettings { get; private set; }
        public float SettingBoneButtonSize { get; private set; }
        public Color SettingBoneNormalColor { get; private set; }
        public Color SettingBoneActiveColor { get; private set; }
        public bool SettingBoneMuscleLimit { get; private set; }
        public enum SkeletonType
        {
            None,
            Line,
            Lines,
            Mesh,
        }
        private static readonly string[] SkeletonTypeString =
        {
            SkeletonType.None.ToString(),
            SkeletonType.Line.ToString(),
            SkeletonType.Lines.ToString(),
            SkeletonType.Mesh.ToString(),
        };

        public SkeletonType SettingsSkeletonFKType { get; private set; }
        public Color SettingSkeletonFKColor { get; private set; }
        public SkeletonType SettingsSkeletonIKType { get; private set; }
        public Color SettingSkeletonIKColor { get; private set; }
        public Color SettingRootMotionColor { get; private set; }
        public float SettingIKTargetSize { get; private set; }
        public Color SettingIKTargetNormalColor { get; private set; }
        public Color SettingIKTargetActiveColor { get; private set; }
        public enum EditorWindowStyle
        {
            Floating,
            Docking,
        }
        private static readonly string[] EditorWindowStyleString =
        {
            EditorWindowStyle.Floating.ToString(),
            EditorWindowStyle.Docking.ToString(),
        };
        public EditorWindowStyle SettingEditorWindowStyle { get; private set; }
        public float SettingEditorNameFieldWidth { get; private set; }
        public bool SettingHierarchyExpandSelectObject { get; private set; }
        public enum PropertyStyle
        {
            Default,
            Filter,
        }
        private GUIContent[] PropertyStyleString = null;
        public PropertyStyle SettingPropertyStyle { get; private set; }
        public bool SettingAutorunFrameAll { get; private set; }
        public bool SettingGenericMirrorScale { get; private set; }
        public bool SettingGenericMirrorName { get; private set; }
        public string SettingGenericMirrorNameDifferentCharacters { get; private set; }
        public bool SettingGenericMirrorNameIgnoreCharacter { get; private set; }
        public string SettingGenericMirrorNameIgnoreCharacterString { get; private set; }
        public bool SettingBlendShapeMirrorName { get; private set; }
        public string SettingBlendShapeMirrorNameDifferentCharacters { get; private set; }
        public enum OnionSkinMode
        {
            Keyframes,
            Frames,
        }
        private static readonly string[] OnionSkinModeStrings =
        {
            OnionSkinMode.Keyframes.ToString(),
            OnionSkinMode.Frames.ToString(),
        };
        public OnionSkinMode SettingExtraOnionSkinMode { get; private set; }
        public int SettingExtraOnionSkinFrameIncrement { get; private set; }
        public int SettingExtraOnionSkinNextCount { get; private set; }
        public Color SettingExtraOnionSkinNextColor { get; private set; }
        private static readonly Color DefaultOnionSkinNextColor = new(0.6039216f, 0.9529412f, 0.282353f, 0.5f);
        public float SettingExtraOnionSkinNextMinAlpha { get; private set; }
        private const float DefaultOnionSkinNextMinAlpha = 0.15f;
        public int SettingExtraOnionSkinPrevCount { get; private set; }
        public Color SettingExtraOnionSkinPrevColor { get; private set; }
        private static readonly Color DefaultOnionSkinPrevColor = new(0.8588235f, 0.2431373f, 0.1137255f, 0.5f);
        public float SettingExtraOnionSkinPrevMinAlpha { get; private set; }
        private const float DefaultOnionSkinPrevMinAlpha = 0.15f;
        public Color SettingExtraRootTrailColor { get; private set; }
        private static readonly Color DefaultRootTrailColor = new(1f, 0.5f, 0.5f, 0.5f);

        private bool componentFoldout;
        private bool gizmosFoldout;
        private bool gizmosBoneFoldout;
        private bool gizmosSkeletonFoldout;
        private bool gizmosIkFoldout;
        private bool editorWindowFoldout;
        private bool controlWindowFoldout;
        private bool controlWindowHierarchyFoldout;
        private bool animationWindowFoldout;
        private bool mirrorFoldout;
        private bool mirrorAutomapFoldout;
        private bool extraFoldout;
        private bool extraOnionSkinningFoldout;
        private bool extraRootTrailFoldout;

        #region RestartOnly
        private EditorWindowStyle settingEditorWindowStyleBefore;
        #endregion

        public EditorSettings()
        {
            SettingLanguageType = (Language.LanguageType)EditorPrefs.GetInt("VeryAnimation_LanguageType", 0);
            SettingComponentSaveSettings = EditorPrefs.GetBool("VeryAnimation_ComponentSaveSettings", true);
            SettingBoneButtonSize = EditorPrefs.GetFloat("VeryAnimation_BoneButtonSize", 16f);
            SettingBoneNormalColor = GetEditorPrefsColor("VeryAnimation_BoneNormalColor", Color.white);
            SettingBoneActiveColor = GetEditorPrefsColor("VeryAnimation_BoneActiveColor", Color.yellow);
            SettingBoneMuscleLimit = EditorPrefs.GetBool("VeryAnimation_BoneMuscleLimit", true);
            SettingsSkeletonFKType = (SkeletonType)EditorPrefs.GetInt("VeryAnimation_SkeletonType", (int)SkeletonType.Lines);
            SettingSkeletonFKColor = GetEditorPrefsColor("VeryAnimation_SkeletonColor", Color.green);
            SettingsSkeletonIKType = (SkeletonType)EditorPrefs.GetInt("VeryAnimation_SkeletonIKType", (int)SkeletonType.Line);
            SettingSkeletonIKColor = GetEditorPrefsColor("VeryAnimation_SkeletonIKColor", Color.magenta);
            SettingRootMotionColor = GetEditorPrefsColor("VeryAnimation_RootMotionColor", Color.cyan);
            SettingIKTargetSize = EditorPrefs.GetFloat("VeryAnimation_IKTargetSize", 0.15f);
            SettingIKTargetNormalColor = GetEditorPrefsColor("VeryAnimation_IKTargetNormalColor", new Color(1f, 1f, 1f, 0.5f));
            SettingIKTargetActiveColor = GetEditorPrefsColor("VeryAnimation_IKTargetActiveColor", new Color(1f, 0.92f, 0.016f, 0.5f));
            SettingEditorWindowStyle = (EditorWindowStyle)EditorPrefs.GetInt("VeryAnimation_EditorWindowStyle", (int)EditorWindowStyle.Docking);
            SettingEditorNameFieldWidth = EditorPrefs.GetFloat("VeryAnimation_EditorNameFieldWidth", 180f);
            SettingHierarchyExpandSelectObject = EditorPrefs.GetBool("VeryAnimation_HierarchyExpandSelectObject", true);
            SettingPropertyStyle = (PropertyStyle)EditorPrefs.GetInt("VeryAnimation_PropertyStyle", 1);
            SettingAutorunFrameAll = EditorPrefs.GetBool("VeryAnimation_AutorunFrameAll", true);
            SettingGenericMirrorScale = EditorPrefs.GetBool("VeryAnimation_GenericMirrorScale", false);
            SettingGenericMirrorName = EditorPrefs.GetBool("VeryAnimation_GenericMirrorName", true);
            SettingGenericMirrorNameDifferentCharacters = EditorPrefs.GetString("VeryAnimation_GenericMirrorNameDifferentCharacters", "Left,Right,Hidari,Migi,L,R");
            SettingGenericMirrorNameIgnoreCharacter = EditorPrefs.GetBool("VeryAnimation_GenericMirrorNameIgnoreCharacter", false);
            SettingGenericMirrorNameIgnoreCharacterString = EditorPrefs.GetString("VeryAnimation_GenericMirrorNameIgnoreCharacterString", ".");
            SettingBlendShapeMirrorName = EditorPrefs.GetBool("VeryAnimation_BlendShapeMirrorName", true);
            SettingBlendShapeMirrorNameDifferentCharacters = EditorPrefs.GetString("VeryAnimation_BlendShapeMirrorNameDifferentCharacters", "Left,Right,Hidari,Migi,L,R");
            SettingExtraOnionSkinMode = (OnionSkinMode)EditorPrefs.GetInt("VeryAnimation_ExtraOnionSkinMode", 0);
            SettingExtraOnionSkinFrameIncrement = EditorPrefs.GetInt("VeryAnimation_ExtraOnionSkinFrameIncrement", 1);
            SettingExtraOnionSkinNextCount = EditorPrefs.GetInt("VeryAnimation_ExtraOnionSkinNextCount", 2);
            SettingExtraOnionSkinNextColor = GetEditorPrefsColor("VeryAnimation_ExtraOnionSkinNextColor", DefaultOnionSkinNextColor);
            SettingExtraOnionSkinNextMinAlpha = EditorPrefs.GetFloat("VeryAnimation_ExtraOnionSkinNextMinAlpha", DefaultOnionSkinNextMinAlpha);
            SettingExtraOnionSkinPrevCount = EditorPrefs.GetInt("VeryAnimation_ExtraOnionSkinPrevCount", 2);
            SettingExtraOnionSkinPrevColor = GetEditorPrefsColor("VeryAnimation_ExtraOnionSkinPrevColor", DefaultOnionSkinPrevColor);
            SettingExtraOnionSkinPrevMinAlpha = EditorPrefs.GetFloat("VeryAnimation_ExtraOnionSkinPrevMinAlpha", DefaultOnionSkinPrevMinAlpha);
            SettingExtraRootTrailColor = GetEditorPrefsColor("VeryAnimation_ExtraRootTrailColor", DefaultRootTrailColor);

            if (SettingPropertyStyle > PropertyStyle.Filter)
                SettingPropertyStyle = PropertyStyle.Filter;

            Language.SetLanguage(SettingLanguageType);
        }
        public void Reset()
        {
            EditorPrefs.SetInt("VeryAnimation_LanguageType", (int)(SettingLanguageType = (Language.LanguageType)0));
            EditorPrefs.SetBool("VeryAnimation_ComponentSaveSettings", SettingComponentSaveSettings = true);
            EditorPrefs.SetFloat("VeryAnimation_BoneButtonSize", SettingBoneButtonSize = 16f);
            SetEditorPrefsColor("VeryAnimation_BoneNormalColor", SettingBoneNormalColor = Color.white);
            SetEditorPrefsColor("VeryAnimation_BoneActiveColor", SettingBoneActiveColor = Color.yellow);
            EditorPrefs.SetBool("VeryAnimation_BoneMuscleLimit", SettingBoneMuscleLimit = true);
            EditorPrefs.SetInt("VeryAnimation_SkeletonType", (int)(SettingsSkeletonFKType = SkeletonType.Lines));
            SetEditorPrefsColor("VeryAnimation_SkeletonColor", SettingSkeletonFKColor = Color.green);
            EditorPrefs.SetInt("VeryAnimation_SkeletonIKType", (int)(SettingsSkeletonIKType = SkeletonType.Line));
            SetEditorPrefsColor("VeryAnimation_SkeletonIKColor", SettingSkeletonIKColor = Color.magenta);
            SetEditorPrefsColor("VeryAnimation_RootMotionColor", SettingRootMotionColor = Color.cyan);
            EditorPrefs.SetFloat("VeryAnimation_IKTargetSize", SettingIKTargetSize = 0.15f);
            SetEditorPrefsColor("VeryAnimation_IKTargetNormalColor", SettingIKTargetNormalColor = new Color(1f, 1f, 1f, 0.5f));
            SetEditorPrefsColor("VeryAnimation_IKTargetActiveColor", SettingIKTargetActiveColor = new Color(1f, 0.92f, 0.016f, 0.5f));
            EditorPrefs.SetInt("VeryAnimation_EditorWindowStyle", (int)(SettingEditorWindowStyle = EditorWindowStyle.Docking));
            EditorPrefs.SetFloat("VeryAnimation_EditorNameFieldWidth", SettingEditorNameFieldWidth = 180f);
            EditorPrefs.SetBool("VeryAnimation_HierarchyExpandSelectObject", SettingHierarchyExpandSelectObject = true);
            EditorPrefs.SetInt("VeryAnimation_PropertyStyle", (int)(SettingPropertyStyle = (PropertyStyle)1));
            EditorPrefs.SetBool("VeryAnimation_AutorunFrameAll", SettingAutorunFrameAll = true);
            EditorPrefs.SetBool("VeryAnimation_GenericMirrorScale", SettingGenericMirrorScale = false);
            EditorPrefs.SetBool("VeryAnimation_GenericMirrorName", SettingGenericMirrorName = true);
            EditorPrefs.SetString("VeryAnimation_GenericMirrorNameDifferentCharacters", SettingGenericMirrorNameDifferentCharacters = "Left,Right,Hidari,Migi,L,R");
            EditorPrefs.SetBool("VeryAnimation_GenericMirrorNameIgnoreCharacter", SettingGenericMirrorNameIgnoreCharacter = false);
            EditorPrefs.SetString("VeryAnimation_GenericMirrorNameIgnoreCharacterString", SettingGenericMirrorNameIgnoreCharacterString = ".");
            EditorPrefs.SetBool("VeryAnimation_BlendShapeMirrorName", SettingBlendShapeMirrorName = true);
            EditorPrefs.SetString("VeryAnimation_BlendShapeMirrorNameDifferentCharacters", SettingBlendShapeMirrorNameDifferentCharacters = "Left,Right,Hidari,Migi,L,R");
            EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinMode", (int)(SettingExtraOnionSkinMode = (OnionSkinMode)0));
            EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinFrameIncrement", SettingExtraOnionSkinFrameIncrement = 1);
            EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinNextCount", SettingExtraOnionSkinNextCount = 2);
            SetEditorPrefsColor("VeryAnimation_ExtraOnionSkinNextColor", SettingExtraOnionSkinNextColor = DefaultOnionSkinNextColor);
            EditorPrefs.SetFloat("VeryAnimation_ExtraOnionSkinNextMinAlpha", SettingExtraOnionSkinNextMinAlpha = DefaultOnionSkinNextMinAlpha);
            EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinPrevCount", SettingExtraOnionSkinPrevCount = 2);
            SetEditorPrefsColor("VeryAnimation_ExtraOnionSkinPrevColor", SettingExtraOnionSkinPrevColor = DefaultOnionSkinPrevColor);
            EditorPrefs.SetFloat("VeryAnimation_ExtraOnionSkinPrevMinAlpha", SettingExtraOnionSkinPrevMinAlpha = DefaultOnionSkinPrevMinAlpha);
            SetEditorPrefsColor("VeryAnimation_ExtraRootTrailColor", SettingExtraRootTrailColor = DefaultRootTrailColor);

            Language.SetLanguage(SettingLanguageType);
            VAW.VA.SetUpdateSampleAnimation();
            VAW.VA.SetAnimationWindowSynchroSelection();
            VAW.VA.SetAnimationWindowRefresh(VeryAnimation.AnimationWindowStateRefreshType.Everything);
            InternalEditorUtility.RepaintAllViews();
        }

        public void Initialize()
        {
            Release();

            #region RestartOnly
            settingEditorWindowStyleBefore = SettingEditorWindowStyle;
            #endregion

            UpdateGUIContentStrings();

            Language.OnLanguageChanged += UpdateGUIContentStrings;
        }
        public void Release()
        {
            Language.OnLanguageChanged -= UpdateGUIContentStrings;
        }

        private void UpdateGUIContentStrings()
        {
            PropertyStyleString = new GUIContent[]
            {
                Language.GetContent(Language.Help.SettingsPropertyStyle_Default),
                Language.GetContent(Language.Help.SettingsPropertyStyle_Filter),
            };
        }

        public void SettingsGUI()
        {
            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Language");
                    EditorGUI.BeginChangeCheck();
                    SettingLanguageType = (Language.LanguageType)GUILayout.Toolbar((int)SettingLanguageType, Language.LanguageTypeString, EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetInt("VeryAnimation_LanguageType", (int)(SettingLanguageType));
                        Language.SetLanguage(SettingLanguageType);
                        InternalEditorUtility.RepaintAllViews();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                componentFoldout = EditorGUILayout.Foldout(componentFoldout, "Component", true);
                if (componentFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        #region settingComponentSaveSettings
                        {
                            EditorGUI.BeginChangeCheck();
                            SettingComponentSaveSettings = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SettingsSaveSettings), SettingComponentSaveSettings);
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetBool("VeryAnimation_ComponentSaveSettings", SettingComponentSaveSettings);
                            }
                        }
                        #endregion
                    }
                    EditorGUI.indentLevel--;
                }
                gizmosFoldout = EditorGUILayout.Foldout(gizmosFoldout, "Gizmos", true);
                if (gizmosFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        gizmosBoneFoldout = EditorGUILayout.Foldout(gizmosBoneFoldout, "Bone", true);
                        if (gizmosBoneFoldout)
                        {
                            EditorGUI.indentLevel++;
                            {
                                #region Button Size
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingBoneButtonSize = EditorGUILayout.Slider("Button Size", SettingBoneButtonSize, 1f, 32f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetFloat("VeryAnimation_BoneButtonSize", SettingBoneButtonSize);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                                #region Button Normal Color
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingBoneNormalColor = EditorGUILayout.ColorField("Button Normal Color", SettingBoneNormalColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        SetEditorPrefsColor("VeryAnimation_BoneNormalColor", SettingBoneNormalColor);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                                #region Button Active Color
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingBoneActiveColor = EditorGUILayout.ColorField("Button Active Color", SettingBoneActiveColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        SetEditorPrefsColor("VeryAnimation_BoneActiveColor", SettingBoneActiveColor);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                                #region MuscleLimit
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingBoneMuscleLimit = EditorGUILayout.Toggle("Muscle Limit Gizmo", SettingBoneMuscleLimit);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetBool("VeryAnimation_BoneMuscleLimit", SettingBoneMuscleLimit);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                        gizmosSkeletonFoldout = EditorGUILayout.Foldout(gizmosSkeletonFoldout, "Skeleton", true);
                        if (gizmosSkeletonFoldout)
                        {
                            EditorGUI.indentLevel++;
                            {
                                #region FK
                                EditorGUILayout.LabelField("FK");
                                {
                                    EditorGUI.indentLevel++;
                                    #region SkeletonType
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUI.BeginChangeCheck();
                                        EditorGUILayout.PrefixLabel("Preview Type");
                                        SettingsSkeletonFKType = (SkeletonType)GUILayout.Toolbar((int)SettingsSkeletonFKType, SkeletonTypeString, EditorStyles.miniButton);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            EditorPrefs.SetInt("VeryAnimation_SkeletonType", (int)SettingsSkeletonFKType);
                                            InternalEditorUtility.RepaintAllViews();
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    #endregion
                                    #region Skeleton Color
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        SettingSkeletonFKColor = EditorGUILayout.ColorField("Preview Color", SettingSkeletonFKColor);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            SetEditorPrefsColor("VeryAnimation_SkeletonColor", SettingSkeletonFKColor);
                                            InternalEditorUtility.RepaintAllViews();
                                        }
                                    }
                                    #endregion
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                #region IK
                                EditorGUILayout.LabelField(new GUIContent("IK", "Foot IK and Animation Rigging"));
                                {
                                    EditorGUI.indentLevel++;
                                    #region SkeletonType
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUI.BeginChangeCheck();
                                        EditorGUILayout.PrefixLabel("Preview Type");
                                        SettingsSkeletonIKType = (SkeletonType)GUILayout.Toolbar((int)SettingsSkeletonIKType, SkeletonTypeString, EditorStyles.miniButton);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            EditorPrefs.SetInt("VeryAnimation_SkeletonIKType", (int)SettingsSkeletonIKType);
                                            InternalEditorUtility.RepaintAllViews();
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    #endregion
                                    #region Skeleton Color
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        SettingSkeletonIKColor = EditorGUILayout.ColorField("Preview Color", SettingSkeletonIKColor);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            SetEditorPrefsColor("VeryAnimation_SkeletonIKColor", SettingSkeletonIKColor);
                                            InternalEditorUtility.RepaintAllViews();
                                        }
                                    }
                                    #endregion
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                #region RootMotion Color
                                EditorGUILayout.LabelField("Root Motion");
                                {
                                    EditorGUI.indentLevel++;
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        SettingRootMotionColor = EditorGUILayout.ColorField("Preview Color", SettingRootMotionColor);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            SetEditorPrefsColor("VeryAnimation_RootMotionColor", SettingRootMotionColor);
                                            InternalEditorUtility.RepaintAllViews();
                                        }
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                        gizmosIkFoldout = EditorGUILayout.Foldout(gizmosIkFoldout, "IK", true);
                        if (gizmosIkFoldout)
                        {
                            EditorGUI.indentLevel++;
                            {
                                #region IK Target Size
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingIKTargetSize = EditorGUILayout.Slider("Button Size", SettingIKTargetSize, 0.01f, 1f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetFloat("VeryAnimation_IKTargetSize", SettingIKTargetSize);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                                #region IK Target Normal Color
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingIKTargetNormalColor = EditorGUILayout.ColorField("Button Normal Color", SettingIKTargetNormalColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        SetEditorPrefsColor("VeryAnimation_IKTargetNormalColor", SettingIKTargetNormalColor);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                                #region IK Target Active Color
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingIKTargetActiveColor = EditorGUILayout.ColorField("Button Active Color", SettingIKTargetActiveColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        SetEditorPrefsColor("VeryAnimation_IKTargetActiveColor", SettingIKTargetActiveColor);
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                editorWindowFoldout = EditorGUILayout.Foldout(editorWindowFoldout, "Editor Window", true);
                if (editorWindowFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        #region Window Style
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PrefixLabel("Window Style");
                        SettingEditorWindowStyle = (EditorWindowStyle)GUILayout.Toolbar((int)SettingEditorWindowStyle, EditorWindowStyleString, EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetInt("VeryAnimation_EditorWindowStyle", (int)SettingEditorWindowStyle);
                        }
                        EditorGUILayout.EndHorizontal();
                        #endregion
                    }
                    {
                        #region NameFieldWidth
                        {
                            EditorGUI.BeginChangeCheck();
                            SettingEditorNameFieldWidth = EditorGUILayout.Slider("Name Field Width", SettingEditorNameFieldWidth, 50f, 500f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetFloat("VeryAnimation_EditorNameFieldWidth", SettingEditorNameFieldWidth);
                                InternalEditorUtility.RepaintAllViews();
                            }
                        }
                        #endregion
                    }
                    EditorGUI.indentLevel--;
                }
                controlWindowFoldout = EditorGUILayout.Foldout(controlWindowFoldout, "Control Window", true);
                if (controlWindowFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        controlWindowHierarchyFoldout = EditorGUILayout.Foldout(controlWindowHierarchyFoldout, "Hierarchy", true);
                        if (controlWindowHierarchyFoldout)
                        {
                            EditorGUI.indentLevel++;
                            {
                                #region ExpandSelectObject
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingHierarchyExpandSelectObject = EditorGUILayout.Toggle(new GUIContent("Expand select object"), SettingHierarchyExpandSelectObject);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetBool("VeryAnimation_HierarchyExpandSelectObject", SettingHierarchyExpandSelectObject);
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                animationWindowFoldout = EditorGUILayout.Foldout(animationWindowFoldout, "Animation Window", true);
                if (animationWindowFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        #region Property Style
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PrefixLabel("Property Style");
                        SettingPropertyStyle = (PropertyStyle)GUILayout.Toolbar((int)SettingPropertyStyle, PropertyStyleString, EditorStyles.miniButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetInt("VeryAnimation_PropertyStyle", (int)SettingPropertyStyle);
                            VAW.VA.SetAnimationWindowSynchroSelection();
                        }
                        EditorGUILayout.EndHorizontal();
                        #endregion
                    }
                    {
                        #region AutorunFrameAll
                        EditorGUI.BeginChangeCheck();
                        SettingAutorunFrameAll = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SettingsAutorunFrameAll), SettingAutorunFrameAll);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_AutorunFrameAll", SettingAutorunFrameAll);
                        }
                        #endregion
                    }
                    EditorGUI.indentLevel--;
                }
                mirrorFoldout = EditorGUILayout.Foldout(mirrorFoldout, "Mirror", true);
                if (mirrorFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        EditorGUI.BeginChangeCheck();
                        SettingGenericMirrorScale = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SettingsMirrorScale), SettingGenericMirrorScale);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VeryAnimation_GenericMirrorScale", SettingGenericMirrorScale);
                        }
                    }

                    mirrorAutomapFoldout = EditorGUILayout.Foldout(mirrorAutomapFoldout, "Automap", true);
                    if (mirrorAutomapFoldout)
                    {
                        EditorGUI.indentLevel++;
                        {
                            EditorGUILayout.LabelField("Generic");
                            EditorGUI.indentLevel++;
                            {
                                #region settingGenericMirrorName
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingGenericMirrorName = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SettingsSearchByName), SettingGenericMirrorName);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetBool("VeryAnimation_GenericMirrorName", SettingGenericMirrorName);
                                    }
                                    if (SettingGenericMirrorName)
                                    {
                                        EditorGUI.indentLevel++;
                                        #region settingGenericMirrorNameDifferentCharacters
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            SettingGenericMirrorNameDifferentCharacters = EditorGUILayout.TextField(new GUIContent("Characters", "Different Characters"), SettingGenericMirrorNameDifferentCharacters);
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                EditorPrefs.SetString("VeryAnimation_GenericMirrorNameDifferentCharacters", SettingGenericMirrorNameDifferentCharacters);
                                            }
                                        }
                                        #endregion
                                        #region settingGenericMirrorNameIgnoreCharacter
                                        {
                                            EditorGUILayout.BeginHorizontal();
                                            {
                                                EditorGUI.BeginChangeCheck();
                                                SettingGenericMirrorNameIgnoreCharacter = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.SettingsIgnoreUpToTheSpecifiedCharacter), SettingGenericMirrorNameIgnoreCharacter);
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    EditorPrefs.SetBool("VeryAnimation_GenericMirrorNameIgnoreCharacter", SettingGenericMirrorNameIgnoreCharacter);
                                                }
                                            }
                                            if (SettingGenericMirrorNameIgnoreCharacter)
                                            {
                                                EditorGUI.BeginChangeCheck();
                                                SettingGenericMirrorNameIgnoreCharacterString = EditorGUILayout.TextField(SettingGenericMirrorNameIgnoreCharacterString, GUILayout.Width(100));
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    EditorPrefs.SetString("VeryAnimation_GenericMirrorNameIgnoreCharacterString", SettingGenericMirrorNameIgnoreCharacterString);
                                                }
                                            }
                                            EditorGUILayout.EndHorizontal();
                                        }
                                        #endregion
                                        EditorGUI.indentLevel--;
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;

                            EditorGUILayout.LabelField("Blend Shape");
                            EditorGUI.indentLevel++;
                            {
                                #region settingBlendShapeMirrorName
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingBlendShapeMirrorName = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SettingsSearchByName), SettingBlendShapeMirrorName);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetBool("VeryAnimation_BlendShapeMirrorName", SettingBlendShapeMirrorName);
                                    }
                                    if (SettingBlendShapeMirrorName)
                                    {
                                        EditorGUI.indentLevel++;
                                        #region settingBlendShapeMirrorNameDifferentCharacters
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            SettingBlendShapeMirrorNameDifferentCharacters = EditorGUILayout.TextField(new GUIContent("Characters", "Different Characters"), SettingBlendShapeMirrorNameDifferentCharacters);
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                EditorPrefs.SetString("VeryAnimation_BlendShapeMirrorNameDifferentCharacters", SettingBlendShapeMirrorNameDifferentCharacters);
                                            }
                                        }
                                        #endregion
                                        EditorGUI.indentLevel--;
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
                extraFoldout = EditorGUILayout.Foldout(extraFoldout, "Extra functions", true);
                if (extraFoldout)
                {
                    EditorGUI.indentLevel++;
                    {
                        #region SynchronizeAnimation
                        {
                            var enable = !EditorApplication.isPlaying && !VAW.VA.UAw.GetLinkedWithTimeline();
                            EditorGUI.BeginDisabledGroup(!enable);
                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUILayout.ToggleLeft(Language.GetContent(Language.Help.SettingsSynchronizeAnimation), VAW.VA.extraOptionsSynchronizeAnimation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Synchronize Animation");
                                VAW.VA.extraOptionsSynchronizeAnimation = flag;
                                VAW.VA.SetSynchronizeAnimation(VAW.VA.extraOptionsSynchronizeAnimation);
                                InternalEditorUtility.RepaintAllViews();
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        #endregion
                        #region OnionSkin
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var flag = EditorGUILayout.ToggleLeft("", VAW.VA.extraOptionsOnionSkin, GUILayout.Width(28f));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Onion Skin");
                                        VAW.VA.extraOptionsOnionSkin = flag;
                                        VAW.VA.OnionSkin.Update();
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                {
                                    var saveLevel = EditorGUI.indentLevel;
                                    EditorGUI.indentLevel = 0;
                                    extraOnionSkinningFoldout = EditorGUILayout.Foldout(extraOnionSkinningFoldout, Language.GetContent(Language.Help.SettingsOnionSkin), true);
                                    EditorGUI.indentLevel = saveLevel;
                                    if (!VAW.VA.extraOptionsOnionSkin)
                                        extraOnionSkinningFoldout = false;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            if (extraOnionSkinningFoldout)
                            {
                                EditorGUI.indentLevel++;
                                #region settingExtraOnionSkinMode
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.PrefixLabel("Mode");
                                    EditorGUI.BeginChangeCheck();
                                    SettingExtraOnionSkinMode = (OnionSkinMode)GUILayout.Toolbar((int)SettingExtraOnionSkinMode, OnionSkinModeStrings, EditorStyles.miniButton);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinMode", (int)(SettingExtraOnionSkinMode));
                                        VAW.VA.OnionSkin.Update();
                                        SceneView.RepaintAll();
                                    }
                                    EditorGUILayout.EndHorizontal();

                                    EditorGUI.indentLevel++;
                                    #region settingExtraOnionSkinFrameIncrement
                                    if (SettingExtraOnionSkinMode == OnionSkinMode.Frames)
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        SettingExtraOnionSkinFrameIncrement = EditorGUILayout.IntSlider("Frame Increment", SettingExtraOnionSkinFrameIncrement, 1, 60);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinFrameIncrement", SettingExtraOnionSkinFrameIncrement);
                                            VAW.VA.OnionSkin.Update();
                                            SceneView.RepaintAll();
                                        }
                                    }
                                    #endregion
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                #region Next
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingExtraOnionSkinNextCount = EditorGUILayout.IntSlider("Next", SettingExtraOnionSkinNextCount, 0, 10);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinNextCount", SettingExtraOnionSkinNextCount);
                                        VAW.VA.OnionSkin.Update();
                                        SceneView.RepaintAll();
                                    }
                                    EditorGUI.indentLevel++;
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.PrefixLabel(new GUIContent("Color", "Near Color + Far Alpha"));
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            SettingExtraOnionSkinNextColor = EditorGUILayout.ColorField(SettingExtraOnionSkinNextColor, GUILayout.Width(80f));
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                SetEditorPrefsColor("VeryAnimation_ExtraOnionSkinNextColor", SettingExtraOnionSkinNextColor);
                                                VAW.VA.OnionSkin.Update();
                                                SceneView.RepaintAll();
                                            }
                                        }
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            SettingExtraOnionSkinNextMinAlpha = EditorGUILayout.Slider(SettingExtraOnionSkinNextMinAlpha, 0f, 1f);
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                EditorPrefs.SetFloat("VeryAnimation_ExtraOnionSkinNextMinAlpha", SettingExtraOnionSkinNextMinAlpha);
                                                VAW.VA.OnionSkin.Update();
                                                SceneView.RepaintAll();
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                #region Prev
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingExtraOnionSkinPrevCount = EditorGUILayout.IntSlider("Previous", SettingExtraOnionSkinPrevCount, 0, 10);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        EditorPrefs.SetInt("VeryAnimation_ExtraOnionSkinPrevCount", SettingExtraOnionSkinPrevCount);
                                        VAW.VA.OnionSkin.Update();
                                        SceneView.RepaintAll();
                                    }
                                    EditorGUI.indentLevel++;
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.PrefixLabel(new GUIContent("Color", "Near Color + Far Alpha"));
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            SettingExtraOnionSkinPrevColor = EditorGUILayout.ColorField(SettingExtraOnionSkinPrevColor, GUILayout.Width(80f));
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                SetEditorPrefsColor("VeryAnimation_ExtraOnionSkinPrevColor", SettingExtraOnionSkinPrevColor);
                                                VAW.VA.OnionSkin.Update();
                                                SceneView.RepaintAll();
                                            }
                                        }
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            SettingExtraOnionSkinPrevMinAlpha = EditorGUILayout.Slider(SettingExtraOnionSkinPrevMinAlpha, 0f, 1f);
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                EditorPrefs.SetFloat("VeryAnimation_ExtraOnionSkinPrevMinAlpha", SettingExtraOnionSkinPrevMinAlpha);
                                                VAW.VA.OnionSkin.Update();
                                                SceneView.RepaintAll();
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                #endregion
                                EditorGUI.indentLevel--;
                            }
                        }
                        #endregion
                        #region RootTrail
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var flag = EditorGUILayout.ToggleLeft("", VAW.VA.extraOptionsRootTrail, GUILayout.Width(28f));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Root Trail");
                                        VAW.VA.extraOptionsRootTrail = flag;
                                        InternalEditorUtility.RepaintAllViews();
                                    }
                                }
                                EditorGUI.BeginDisabledGroup(!VAW.VA.IsHuman);
                                {
                                    var saveLevel = EditorGUI.indentLevel;
                                    EditorGUI.indentLevel = 0;
                                    extraRootTrailFoldout = EditorGUILayout.Foldout(extraRootTrailFoldout, Language.GetContent(Language.Help.SettingsRootTrail), true);
                                    EditorGUI.indentLevel = saveLevel;
                                    if (!VAW.VA.extraOptionsRootTrail)
                                        extraRootTrailFoldout = false;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            if (extraRootTrailFoldout)
                            {
                                EditorGUI.indentLevel++;
                                {
                                    EditorGUI.BeginChangeCheck();
                                    SettingExtraRootTrailColor = EditorGUILayout.ColorField("Color", SettingExtraRootTrailColor);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        SetEditorPrefsColor("VeryAnimation_ExtraRootTrailColor", SettingExtraRootTrailColor);
                                        SceneView.RepaintAll();
                                    }
                                }
                                EditorGUI.indentLevel--;
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        #endregion
                    }
                    EditorGUI.indentLevel--;
                }

                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Reset"))
                    {
                        Reset();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }

                #region RestartOnly
                if (settingEditorWindowStyleBefore != SettingEditorWindowStyle)
                {
                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.SettingsRestartOnly), MessageType.Warning);
                }
                #endregion
            }
            EditorGUILayout.EndVertical();
        }

        public float GetSkeletonTypeLinesRadius(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position) * (SettingBoneButtonSize / 200f);
        }

        private Color GetEditorPrefsColor(string name, Color defcolor)
        {
            return new Color(EditorPrefs.GetFloat(name + "_r", defcolor.r),
                            EditorPrefs.GetFloat(name + "_g", defcolor.g),
                            EditorPrefs.GetFloat(name + "_b", defcolor.b),
                            EditorPrefs.GetFloat(name + "_a", defcolor.a));
        }
        private void SetEditorPrefsColor(string name, Color color)
        {
            EditorPrefs.SetFloat(name + "_r", color.r);
            EditorPrefs.SetFloat(name + "_g", color.g);
            EditorPrefs.SetFloat(name + "_b", color.b);
            EditorPrefs.SetFloat(name + "_a", color.a);
        }
    }
}
