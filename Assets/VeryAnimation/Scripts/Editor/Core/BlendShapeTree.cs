using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VeryAnimation
{
    [Serializable]
    internal class BlendShapeTree
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        private enum BlendShapeMode
        {
            Slider,
            List,
            Icon,
            Total,
        }
        private static readonly string[] BlendShapeModeString =
        {
            BlendShapeMode.Slider.ToString(),
            BlendShapeMode.List.ToString(),
            BlendShapeMode.Icon.ToString(),
        };
        private BlendShapeMode blendShapeMode;

        #region Tree
        [System.Diagnostics.DebuggerDisplay("{blendShapeName}")]
        private class BlendShapeInfo
        {
            public string blendShapeName;
        }
        private class BlendShapeNode
        {
            public string name;
            public bool foldout;
            public BlendShapeInfo[] infoList;
        }
        private class BlendShapeRootNode : BlendShapeNode
        {
            public SkinnedMeshRenderer renderer;
            public Mesh mesh;
            public string[] blendShapeNames;
        }
        private readonly List<BlendShapeRootNode> blendShapeNodes;
        private readonly Dictionary<BlendShapeNode, int> blendShapeGroupTreeTable;

        [SerializeField]
        private float[] blendShapeGroupValues;

        private bool blendShapeMirrorName;
        #endregion

        #region List
        private ReorderableList blendShapeSetListReorderableList;
        #endregion

        #region Icon
        private const int IconTextureSize = 256;
        private bool iconUpdate;
        private bool iconShowName;
        private float iconSize;

        private enum IconCameraMode
        {
            forward,
            back,
            up,
            down,
            right,
            left,
        }
        private IconCameraMode iconCameraMode;

        private enum IconCameraBounds
        {
            allRenderers,
            focusChangedRenderers,
            onlyRrenderersWithChanges,
        }
        private IconCameraBounds iconCameraBounds;
        #endregion

        #region GUIStyle
        private GUIStyle guiStyleIconButton;
        private GUIStyle guiStyleNameLabelCenter;
        private GUIStyle guiStyleNameLabelRight;

        private void GUIStyleReady()
        {
            #region GUIStyle
            guiStyleIconButton ??= new GUIStyle(GUI.skin.button);
            guiStyleIconButton.margin = new RectOffset(0, 0, 0, 0);
            guiStyleIconButton.overflow = new RectOffset(0, 0, 0, 0);
            guiStyleIconButton.padding = new RectOffset(0, 0, 0, 0);
            guiStyleNameLabelCenter ??= new GUIStyle(EditorStyles.whiteLargeLabel);
            guiStyleNameLabelCenter.alignment = TextAnchor.LowerCenter;
            guiStyleNameLabelRight ??= new GUIStyle(EditorStyles.whiteLargeLabel);
            guiStyleNameLabelRight.alignment = TextAnchor.LowerRight;
            #endregion
        }
        #endregion

        public BlendShapeTree()
        {
            if (VAW == null || VAW.GameObject == null)
                return;

            #region BlendShapeNode
            {
                blendShapeNodes = new List<BlendShapeRootNode>();
                foreach (var renderer in VAW.GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount <= 0)
                        continue;
                    var root = new BlendShapeRootNode
                    {
                        renderer = renderer,
                        mesh = renderer.sharedMesh,
                        name = renderer.gameObject.name,
                        infoList = new BlendShapeInfo[renderer.sharedMesh.blendShapeCount],
                        blendShapeNames = new string[renderer.sharedMesh.blendShapeCount + 1]
                    };
                    root.blendShapeNames[0] = "[none]";
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        root.infoList[i] = new BlendShapeInfo()
                        {
                            blendShapeName = renderer.sharedMesh.GetBlendShapeName(i),
                        };
                        root.blendShapeNames[i + 1] = renderer.sharedMesh.GetBlendShapeName(i);
                    }
                    blendShapeNodes.Add(root);
                }

                {
                    blendShapeGroupTreeTable = new Dictionary<BlendShapeNode, int>();
                    int counter = 0;
                    void AddTable(BlendShapeNode mg)
                    {
                        blendShapeGroupTreeTable.Add(mg, counter++);
                    }

                    foreach (var node in blendShapeNodes)
                    {
                        AddTable(node);
                    }

                    blendShapeGroupValues = new float[blendShapeGroupTreeTable.Count];
                }
            }
            #endregion

            iconUpdate = true;
        }

        public void LoadEditorPref()
        {
            blendShapeMode = (BlendShapeMode)EditorPrefs.GetInt("VeryAnimation_BlendShapeMode", 0);
            blendShapeMirrorName = EditorPrefs.GetBool("VeryAnimation_Control_BlendShapeMirrorName", false);
            iconShowName = EditorPrefs.GetBool("VeryAnimation_Control_BlendShapeSetIconShowName", true);
            iconSize = EditorPrefs.GetFloat("VeryAnimation_Control_BlendShapeSetIconSize", 100f);
            iconCameraMode = (IconCameraMode)EditorPrefs.GetInt("VeryAnimation_BlendShapeSetIconCameraMode", 0);
            iconCameraBounds = (IconCameraBounds)EditorPrefs.GetInt("VeryAnimation_BlendShapeSetIconCameraBounds", (int)IconCameraBounds.focusChangedRenderers);
        }
        public void SaveEditorPref()
        {
            EditorPrefs.SetInt("VeryAnimation_BlendShapeMode", (int)blendShapeMode);
            EditorPrefs.SetBool("VeryAnimation_Control_BlendShapeMirrorName", blendShapeMirrorName);
            EditorPrefs.SetBool("VeryAnimation_Control_BlendShapeSetIconShowName", iconShowName);
            EditorPrefs.SetFloat("VeryAnimation_Control_BlendShapeSetIconSize", iconSize);
            EditorPrefs.SetInt("VeryAnimation_BlendShapeSetIconCameraMode", (int)iconCameraMode);
            EditorPrefs.SetInt("VeryAnimation_BlendShapeSetIconCameraBounds", (int)iconCameraBounds);
        }

        public void BlendShapeTreeToolbarGUI()
        {
            EditorGUI.BeginChangeCheck();
            var m = (BlendShapeMode)GUILayout.Toolbar((int)blendShapeMode, BlendShapeModeString, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                blendShapeMode = m;
            }
        }

        public void BlendShapeTreeSettingsMesh()
        {
            var menu = new GenericMenu();
            menu.AddItem(Language.GetContent(Language.Help.BlendShapeMirrorName), blendShapeMirrorName, () =>
            {
                blendShapeMirrorName = !blendShapeMirrorName;
            });
            menu.AddItem(Language.GetContent(Language.Help.BlendShapeMirrorAutomap), false, () =>
            {
                VAW.VA.BlendShapeMirrorAutomap();
                InternalEditorUtility.RepaintAllViews();
            });
            menu.AddItem(Language.GetContent(Language.Help.BlendShapeMirrorClear), false, () =>
            {
                VAW.VA.BlendShapeMirrorInitialize();
                InternalEditorUtility.RepaintAllViews();
            });
            menu.ShowAsContext();
        }

        public bool IsHaveBlendShapeNodes()
        {
            return blendShapeNodes != null && blendShapeNodes.Count > 0;
        }

        public void BlendShapeTreeGUI()
        {
            var e = Event.current;

            GUIStyleReady();

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            if (blendShapeMode == BlendShapeMode.Slider)
            {
                #region Slider
                const int IndentWidth = 15;

                #region SetBlendShapeFoldout
                static void SetBlendShapeFoldout(BlendShapeNode mg, bool foldout)
                {
                    mg.foldout = foldout;
                }
                #endregion

                var mgRoot = blendShapeNodes;

                #region Top
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All", GUILayout.Width(100)))
                    {
                        if (Shortcuts.IsKeyControl(e) || e.shift)
                        {
                            var combineGoList = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                            var combineVirtualList = new HashSet<HumanBodyBones>();
                            if (VAW.VA.SelectionHumanVirtualBones != null)
                                combineVirtualList.UnionWith(VAW.VA.SelectionHumanVirtualBones);
                            var combineBindings = new HashSet<EditorCurveBinding>(VAW.VA.UAw.GetCurveSelection());
                            foreach (var root in mgRoot)
                            {
                                if (root.renderer != null && root.renderer.gameObject != null)
                                    combineGoList.Add(root.renderer.gameObject);
                                if (root.infoList != null && root.infoList.Length > 0)
                                {
                                    foreach (var info in root.infoList)
                                        combineBindings.Add(VAW.VA.AnimationCurveBindingBlendShape(root.renderer, info.blendShapeName));
                                }
                            }
                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                            VAW.VA.SetAnimationWindowSynchroSelection(combineBindings.ToArray());
                        }
                        else
                        {
                            var combineGoList = new List<GameObject>();
                            var combineBindings = new List<EditorCurveBinding>();
                            foreach (var root in mgRoot)
                            {
                                if (root.renderer != null && root.renderer.gameObject != null)
                                    combineGoList.Add(root.renderer.gameObject);
                                if (root.infoList != null && root.infoList.Length > 0)
                                {
                                    foreach (var info in root.infoList)
                                        combineBindings.Add(VAW.VA.AnimationCurveBindingBlendShape(root.renderer, info.blendShapeName));
                                }
                            }
                            VAW.VA.SelectGameObjects(combineGoList.ToArray());
                            VAW.VA.SetAnimationWindowSynchroSelection(combineBindings.ToArray());
                        }
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Reset All", VAW.GuiStyleDropDown, GUILayout.Width(100)))
                    {
                        var menu = new GenericMenu();
                        {
                            if (VAW.VA.BlendShapeWeightSave.IsEnablePrefabWeight())
                            {
                                menu.AddItem(Language.GetContent(Language.Help.EditorPosePrefab), false, () =>
                                {
                                    Undo.RecordObject(VAE, "Prefab Pose");
                                    for (int i = 0; i < blendShapeGroupValues.Length; i++)
                                        blendShapeGroupValues[i] = 0f;
                                    foreach (var root in mgRoot)
                                    {
                                        if (root.infoList != null && root.infoList.Length > 0)
                                        {
                                            foreach (var info in root.infoList)
                                                VAW.VA.SetAnimationValueBlendShapeIfNotOriginal(root.renderer, info.blendShapeName, VAW.VA.BlendShapeWeightSave.GetPrefabWeight(root.renderer, info.blendShapeName));
                                        }
                                    }
                                });
                            }
                            {
                                menu.AddItem(Language.GetContent(Language.Help.EditorPoseStart), false, () =>
                                {
                                    Undo.RecordObject(VAE, "Edit Start Pose");
                                    for (int i = 0; i < blendShapeGroupValues.Length; i++)
                                        blendShapeGroupValues[i] = 0f;
                                    foreach (var root in mgRoot)
                                    {
                                        if (root.infoList != null && root.infoList.Length > 0)
                                        {
                                            foreach (var info in root.infoList)
                                                VAW.VA.SetAnimationValueBlendShapeIfNotOriginal(root.renderer, info.blendShapeName, VAW.VA.BlendShapeWeightSave.GetOriginalWeight(root.renderer, info.blendShapeName));
                                        }
                                    }
                                });
                            }
                        }
                        menu.ShowAsContext();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion

                EditorGUILayout.Space();

                #region BlendShape
                BlendShapeRootNode rootNode = null;
                int RowCount = 0;
                void BlendShapeTreeNodeGUI(BlendShapeNode mg, int level, int brotherMaxLevel)
                {
                    const int FoldoutWidth = 22;
                    const int FoldoutSpace = 17;
                    const int FloatFieldWidth = 44;
                    var indentSpace = IndentWidth * level;
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    {
                        {
                            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(FoldoutWidth));
                            EditorGUI.BeginChangeCheck();
                            mg.foldout = EditorGUI.Foldout(rect, mg.foldout, "", true);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (e.alt)
                                    SetBlendShapeFoldout(mg, mg.foldout);
                            }
                        }
                        if (GUILayout.Button(new GUIContent(mg.name, mg.name), GUILayout.Width(VAW.EditorSettings.SettingEditorNameFieldWidth)))
                        {
                            if (Shortcuts.IsKeyControl(e) || e.shift)
                            {
                                var combineGoList = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                                var combineVirtualList = new HashSet<HumanBodyBones>();
                                if (VAW.VA.SelectionHumanVirtualBones != null)
                                    combineVirtualList.UnionWith(VAW.VA.SelectionHumanVirtualBones);
                                var combineBindings = new HashSet<EditorCurveBinding>(VAW.VA.UAw.GetCurveSelection());
                                if (rootNode.renderer != null && rootNode.renderer.gameObject != null)
                                    combineGoList.Add(rootNode.renderer.gameObject);
                                if (rootNode.infoList != null && rootNode.infoList.Length > 0)
                                {
                                    foreach (var info in rootNode.infoList)
                                        combineBindings.Add(VAW.VA.AnimationCurveBindingBlendShape(rootNode.renderer, info.blendShapeName));
                                }
                                VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                                VAW.VA.SetAnimationWindowSynchroSelection(combineBindings.ToArray());
                            }
                            else
                            {
                                var combineGoList = new List<GameObject>();
                                var combineBindings = new List<EditorCurveBinding>();
                                if (rootNode.renderer != null && rootNode.renderer.gameObject != null)
                                    combineGoList.Add(rootNode.renderer.gameObject);
                                if (rootNode.infoList != null && rootNode.infoList.Length > 0)
                                {
                                    foreach (var info in rootNode.infoList)
                                        combineBindings.Add(VAW.VA.AnimationCurveBindingBlendShape(rootNode.renderer, info.blendShapeName));
                                }
                                VAW.VA.SelectGameObjects(combineGoList.ToArray());
                                VAW.VA.SetAnimationWindowSynchroSelection(combineBindings.ToArray());
                            }
                        }
                        {
                            GUILayout.Space(FoldoutSpace);
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            var value = GUILayout.HorizontalSlider(blendShapeGroupValues[blendShapeGroupTreeTable[mg]], 0f, 100f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAE, "Change BlendShape Group");
                                blendShapeGroupValues[blendShapeGroupTreeTable[mg]] = value;
                                if (mg.infoList != null && mg.infoList.Length > 0)
                                {
                                    foreach (var info in mg.infoList)
                                    {
                                        VAW.VA.SetAnimationValueBlendShape(rootNode.renderer, info.blendShapeName, value);
                                    }
                                }
                            }
                        }
                        {
                            var width = FloatFieldWidth + IndentWidth * Math.Max(0, brotherMaxLevel);
                            EditorGUI.BeginChangeCheck();
                            var value = EditorGUILayout.FloatField(blendShapeGroupValues[blendShapeGroupTreeTable[mg]], GUILayout.Width(width));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAE, "Change BlendShape Group");
                                blendShapeGroupValues[blendShapeGroupTreeTable[mg]] = value;
                                if (mg.infoList != null && mg.infoList.Length > 0)
                                {
                                    foreach (var info in mg.infoList)
                                    {
                                        VAW.VA.SetAnimationValueBlendShape(rootNode.renderer, info.blendShapeName, value);
                                    }
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    if (mg.foldout)
                    {
                        if (mg.infoList != null && mg.infoList.Length > 0)
                        {
                            #region BlendShape
                            foreach (var info in mg.infoList)
                            {
                                var blendShapeValue = VAW.VA.GetAnimationValueBlendShape(rootNode.renderer, info.blendShapeName);
                                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                                {
                                    EditorGUILayout.GetControlRect(false, GUILayout.Width(indentSpace + FoldoutWidth));
                                    if (GUILayout.Button(new GUIContent(info.blendShapeName, info.blendShapeName), GUILayout.Width(VAW.EditorSettings.SettingEditorNameFieldWidth)))
                                    {
                                        if (Shortcuts.IsKeyControl(e) || e.shift)
                                        {
                                            var combineGoList = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                                            var combineVirtualList = new HashSet<HumanBodyBones>();
                                            if (VAW.VA.SelectionHumanVirtualBones != null)
                                                combineVirtualList.UnionWith(VAW.VA.SelectionHumanVirtualBones);
                                            var combineBindings = new HashSet<EditorCurveBinding>(VAW.VA.UAw.GetCurveSelection());
                                            if (rootNode.renderer != null && rootNode.renderer.gameObject != null)
                                                combineGoList.Add(rootNode.renderer.gameObject);
                                            combineBindings.Add(VAW.VA.AnimationCurveBindingBlendShape(rootNode.renderer, info.blendShapeName));
                                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                                            VAW.VA.SetAnimationWindowSynchroSelection(combineBindings.ToArray());
                                        }
                                        else
                                        {
                                            if (rootNode.renderer != null && rootNode.renderer.gameObject != null)
                                                VAW.VA.SelectGameObject(rootNode.renderer.gameObject);
                                            VAW.VA.SetAnimationWindowSynchroSelection(new EditorCurveBinding[] { VAW.VA.AnimationCurveBindingBlendShape(rootNode.renderer, info.blendShapeName) });
                                        }
                                    }
                                }
                                {
                                    var mirrorName = VAW.VA.GetMirrorBlendShape(rootNode.renderer, info.blendShapeName);
                                    if (!string.IsNullOrEmpty(mirrorName))
                                    {
                                        if (GUILayout.Button(new GUIContent("", string.Format("Mirror: '{0}'", mirrorName)), VAW.GuiStyleMirrorButton, GUILayout.Width(VAW.MirrorTex.width), GUILayout.Height(VAW.MirrorTex.height)))
                                        {
                                            if (rootNode.renderer != null && rootNode.renderer.gameObject != null)
                                                VAW.VA.SelectGameObject(rootNode.renderer.gameObject);
                                            VAW.VA.SetAnimationWindowSynchroSelection(new EditorCurveBinding[] { VAW.VA.AnimationCurveBindingBlendShape(rootNode.renderer, mirrorName) });
                                        }
                                    }
                                    else
                                    {
                                        GUILayout.Space(FoldoutSpace);
                                    }
                                    if (blendShapeMirrorName)
                                    {
                                        var mirrorIndex = EditorCommon.ArrayIndexOf(rootNode.blendShapeNames, mirrorName);
                                        EditorGUI.BeginChangeCheck();
                                        mirrorIndex = EditorGUILayout.Popup(mirrorIndex, rootNode.blendShapeNames);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            string newMirrorName = mirrorIndex > 0 ? rootNode.blendShapeNames[mirrorIndex] : null;
                                            if (info.blendShapeName == newMirrorName)
                                                newMirrorName = null;
                                            VAW.VA.ChangeBlendShapeMirror(rootNode.renderer, info.blendShapeName, newMirrorName);
                                            if (!string.IsNullOrEmpty(newMirrorName))
                                                VAW.VA.ChangeBlendShapeMirror(rootNode.renderer, newMirrorName, info.blendShapeName);
                                        }
                                    }
                                }
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var value2 = GUILayout.HorizontalSlider(blendShapeValue, 0f, 100f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        VAW.VA.SetAnimationValueBlendShape(rootNode.renderer, info.blendShapeName, value2);
                                    }
                                }
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var value2 = EditorGUILayout.FloatField(blendShapeValue, GUILayout.Width(FloatFieldWidth));
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        VAW.VA.SetAnimationValueBlendShape(rootNode.renderer, info.blendShapeName, value2);
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            #endregion
                        }
                    }
                }

                {
                    int maxLevel = 0;
                    foreach (var root in mgRoot)
                    {
                        if (root.renderer != null && root.mesh != null && root.renderer.sharedMesh == root.mesh)
                        {
                            if (root.foldout)
                                maxLevel = Math.Max(maxLevel, 1);
                        }
                    }
                    foreach (var root in mgRoot)
                    {
                        if (root.renderer != null && root.mesh != null && root.renderer.sharedMesh == root.mesh)
                        {
                            rootNode = root;
                            BlendShapeTreeNodeGUI(root, 1, maxLevel);
                        }
                    }
                }
                #endregion
                #endregion
            }
            else if (blendShapeMode == BlendShapeMode.List)
            {
                #region List
                if (e.type == EventType.Layout)
                {
                    UpdateBlendShapeSetListReorderableList();
                }
                blendShapeSetListReorderableList?.DoLayoutList();
                #endregion
            }
            else if (blendShapeMode == BlendShapeMode.Icon)
            {
                #region Icon
                if (e.type == EventType.Layout)
                {
                    UpdateBlendShapeSetIcon();
                }
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        EditorGUI.BeginChangeCheck();
                        iconCameraMode = (IconCameraMode)EditorGUILayout.EnumPopup(iconCameraMode, EditorStyles.toolbarDropDown, GUILayout.Width(80f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            iconUpdate = true;
                        }
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        iconCameraBounds = (IconCameraBounds)EditorGUILayout.EnumPopup(iconCameraBounds, EditorStyles.toolbarDropDown, GUILayout.Width(200f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            iconUpdate = true;
                        }
                    }
                    EditorGUILayout.Space();
                    iconShowName = GUILayout.Toggle(iconShowName, "Show Name", EditorStyles.toolbarButton);
                    EditorGUILayout.Space();
                    iconSize = EditorGUILayout.Slider(iconSize, 32f, IconTextureSize);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();
                if (VAW.VA.blendShapeSetList.Count > 0)
                {
                    float areaWidth = VAE.position.width - 16f;
                    int countX = Math.Max(1, Mathf.FloorToInt(areaWidth / iconSize));
                    int countY = Mathf.CeilToInt(VAW.VA.blendShapeSetList.Count / (float)countX);
                    for (int i = 0; i < countY; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int j = 0; j < countX; j++)
                        {
                            var index = i * countX + j;
                            if (index >= VAW.VA.blendShapeSetList.Count) break;
                            var rect = EditorGUILayout.GetControlRect(false, iconSize, guiStyleIconButton, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                            if (GUI.Button(rect, VAW.VA.blendShapeSetList[index].icon, guiStyleIconButton))
                            {
                                var poseTemplate = VAW.VA.blendShapeSetList[index].poseTemplate;
                                if (Shortcuts.IsKeyControl(e) || e.shift)
                                    VAW.VA.LoadPoseTemplate(poseTemplate, VeryAnimation.PoseFlags.BlendShape, false, true);
                                else
                                    VAW.VA.LoadPoseTemplate(poseTemplate, VeryAnimation.PoseFlags.BlendShape);
                            }
                            if (iconShowName)
                            {
                                var size = guiStyleNameLabelCenter.CalcSize(new GUIContent(VAW.VA.blendShapeSetList[index].poseTemplate.name));
                                if (size.x < rect.width)
                                    EditorGUI.DropShadowLabel(rect, VAW.VA.blendShapeSetList[index].poseTemplate.name, guiStyleNameLabelCenter);
                                else
                                    EditorGUI.DropShadowLabel(rect, VAW.VA.blendShapeSetList[index].poseTemplate.name, guiStyleNameLabelRight);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("List is Empty", EditorStyles.centeredGreyMiniLabel);
                }
                #endregion
            }
            EditorGUILayout.EndVertical();
        }

        private void UpdateBlendShapeSetListReorderableList()
        {
            if (blendShapeSetListReorderableList != null)
                return;

            blendShapeSetListReorderableList = new ReorderableList(VAW.VA.blendShapeSetList, typeof(PoseTemplate), true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    float x = rect.x;
                    {
                        const float ButtonWidth = 100f;
                        #region Add
                        {
                            var r = rect;
                            r.width = ButtonWidth;
                            if (GUI.Button(r, Language.GetContent(Language.Help.BlendShapeTemplate), EditorStyles.toolbarDropDown))
                            {
                                var blendShapeTemplates = new Dictionary<string, string>();
                                {
                                    var guids = AssetDatabase.FindAssets("t:blendshapetemplate");
                                    for (int i = 0; i < guids.Length; i++)
                                    {
                                        var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                                        var name = path["Assets/".Length..];
                                        blendShapeTemplates.Add(name, path);
                                    }
                                }

                                var menu = new GenericMenu();
                                {
                                    menu.AddItem(new GUIContent("All"), false, () =>
                                    {
                                        Undo.RecordObject(VAW, "Template BlendShape");
                                        {
                                            var basePoseTemplate = ScriptableObject.CreateInstance<PoseTemplate>();
                                            VAW.VA.SavePoseTemplate(basePoseTemplate, VeryAnimation.PoseFlags.BlendShape);
                                            for (int i = 0; i < basePoseTemplate.blendShapeValues.Length; i++)
                                            {
                                                for (int j = 0; j < basePoseTemplate.blendShapeValues[i].weights.Length; j++)
                                                    basePoseTemplate.blendShapeValues[i].weights[j] = 0f;
                                            }
                                            for (int i = 0; i < basePoseTemplate.blendShapeValues.Length; i++)
                                            {
                                                var renderer = VAW.GameObject.transform.Find(basePoseTemplate.blendShapePaths[i]);
                                                if (renderer == null)
                                                    continue;
                                                for (int j = 0; j < basePoseTemplate.blendShapeValues[i].weights.Length; j++)
                                                {
                                                    var poseTemplate = ScriptableObject.Instantiate(basePoseTemplate);
                                                    poseTemplate.name = renderer.name + "/" + basePoseTemplate.blendShapeValues[i].names[j];
                                                    poseTemplate.blendShapeValues[i].weights[j] = 100f;
                                                    VAW.VA.blendShapeSetList.Add(new VeryAnimation.BlendShapeSet()
                                                    {
                                                        poseTemplate = poseTemplate,
                                                    });
                                                }
                                            }
                                            ScriptableObject.DestroyImmediate(basePoseTemplate);
                                        }
                                        iconUpdate = true;
                                    });
                                    menu.AddSeparator("");
                                    {
                                        var enu = blendShapeTemplates.GetEnumerator();
                                        while (enu.MoveNext())
                                        {
                                            var value = enu.Current.Value;
                                            menu.AddItem(new GUIContent("Template/" + enu.Current.Key), false, () =>
                                            {
                                                var blendShapeTemplate = AssetDatabase.LoadAssetAtPath<BlendShapeTemplate>(value);
                                                if (blendShapeTemplate != null)
                                                {
                                                    Undo.RecordObject(VAW, "Template BlendShape");
                                                    foreach (var template in blendShapeTemplate.list)
                                                    {
                                                        var set = new VeryAnimation.BlendShapeSet
                                                        {
                                                            poseTemplate = template.GetPoseTemplate()
                                                        };
                                                        VAW.VA.blendShapeSetList.Add(set);
                                                    }
                                                    iconUpdate = true;
                                                }
                                            });
                                        }
                                    }
                                }
                                menu.ShowAsContext();
                            }
                        }
                        #endregion
                        #region Clear
                        {
                            var r = rect;
                            r.xMin += ButtonWidth;
                            r.width = ButtonWidth;
                            if (GUI.Button(r, "Clear", EditorStyles.toolbarButton))
                            {
                                Undo.RecordObject(VAW, "Clear BlendShape");
                                VAW.VA.blendShapeSetList.Clear();
                            }
                        }
                        #endregion
                        #region Save as
                        {
                            var r = rect;
                            r.width = ButtonWidth;
                            r.x = rect.xMax - r.width;
                            if (GUI.Button(r, Language.GetContent(Language.Help.BlendShapeSaveAs), EditorStyles.toolbarButton))
                            {
                                string path = EditorUtility.SaveFilePanel("Save as BlendShape Template", VAE.TemplateSaveDefaultDirectory, string.Format("{0}_BlendShape.asset", VAW.GameObject.name), "asset");
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
                                        var blendShapeTemplate = ScriptableObject.CreateInstance<BlendShapeTemplate>();
                                        {
                                            foreach (var set in VAW.VA.blendShapeSetList)
                                            {
                                                blendShapeTemplate.Add(set.poseTemplate);
                                            }
                                        }
                                        try
                                        {
                                            VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
                                            AssetDatabase.CreateAsset(blendShapeTemplate, path);
                                        }
                                        finally
                                        {
                                            VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
                                        }
                                        VAE.Focus();
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
            };
            blendShapeSetListReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= VAW.VA.blendShapeSetList.Count)
                    return;

                float x = rect.x;
                {
                    const float Rate = 0.7f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (index == blendShapeSetListReorderableList.index)
                    {
                        EditorGUI.BeginChangeCheck();
                        var text = EditorGUI.TextField(r, VAW.VA.blendShapeSetList[index].poseTemplate.name);
                        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(text))
                        {
                            Undo.RecordObject(VAW.VA.blendShapeSetList[index].poseTemplate, "Change set name");
                            VAW.VA.blendShapeSetList[index].poseTemplate.name = text;
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(r, VAW.VA.blendShapeSetList[index].poseTemplate.name);
                    }
                }
                {
                    const float Rate = 0.15f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (GUI.Button(r, Language.GetContent(Language.Help.BlendShapeAddButton)))
                    {
                        var poseTemplate = VAW.VA.blendShapeSetList[index].poseTemplate;
                        VAW.VA.LoadPoseTemplate(poseTemplate, VeryAnimation.PoseFlags.BlendShape, false, true);
                    }
                }
                {
                    const float Rate = 0.15f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (GUI.Button(r, Language.GetContent(Language.Help.BlendShapeSetButton)))
                    {
                        var poseTemplate = VAW.VA.blendShapeSetList[index].poseTemplate;
                        VAW.VA.LoadPoseTemplate(poseTemplate, VeryAnimation.PoseFlags.BlendShape);
                    }
                }
            };
            blendShapeSetListReorderableList.onAddCallback = list =>
            {
                Undo.RecordObject(VAW, "Add BlendShape Set");

                var poseTemplate = ScriptableObject.CreateInstance<PoseTemplate>();
                VAW.VA.SavePoseTemplate(poseTemplate, VeryAnimation.PoseFlags.BlendShape);
                {
                    var name = string.Format("Set {0}", VAW.VA.blendShapeSetList.Count);
                    float max = 0f;
                    for (int i = 0; i < poseTemplate.blendShapeValues.Length; i++)
                    {
                        for (int j = 0; j < poseTemplate.blendShapeValues[i].weights.Length; j++)
                        {
                            if (poseTemplate.blendShapeValues[i].weights[j] > max)
                            {
                                var renderer = VAW.GameObject.transform.Find(poseTemplate.blendShapePaths[i]);
                                if (renderer != null)
                                {
                                    name = renderer.name + "/" + poseTemplate.blendShapeValues[i].names[j];
                                    max = poseTemplate.blendShapeValues[i].weights[j];
                                }
                            }
                        }
                    }
                    poseTemplate.name = name;
                }
                VAW.VA.blendShapeSetList.Add(new VeryAnimation.BlendShapeSet()
                {
                    poseTemplate = poseTemplate,
                });
                iconUpdate = true;
                EditorApplication.delayCall += () =>
                {
                    blendShapeSetListReorderableList.index = VAW.VA.blendShapeSetList.Count - 1;
                    VAE.Repaint();
                };
            };
            blendShapeSetListReorderableList.onRemoveCallback = list =>
            {
                Undo.RecordObject(VAW, "Remove BlendShape Set");
                VAW.VA.blendShapeSetList.RemoveAt(list.index);
                if (list.index >= list.count)
                    list.index = list.count - 1;
            };
        }

        private void UpdateBlendShapeSetIcon()
        {
            if (!iconUpdate)
                return;
            iconUpdate = false;

            if (VAW.VA.blendShapeSetList == null || VAW.VA.blendShapeSetList.Count <= 0)
                return;

            VAW.VA.TransformPoseSave.ResetDefaultTransform();
            VAW.VA.BlendShapeWeightSave.ResetDefaultWeight();

            var gameObject = VAW.UEditorUtility.InstantiateForAnimatorPreview(VAW.GameObject);
            gameObject.hideFlags |= HideFlags.HideAndDontSave;
            gameObject.transform.SetParent(null);
#if UNITY_2022_3_OR_NEWER
            gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
#endif
            gameObject.transform.localScale = Vector3.one;
            EditorCommon.DisableOtherBehaviors(gameObject);

            if (gameObject.TryGetComponent<Animator>(out var animator))
            {
                animator.enabled = true;
                animator.Rebind();
                animator.enabled = false;
            }

            int blankLayer;
            {
                for (blankLayer = 31; blankLayer > 0; blankLayer--)
                {
                    if (string.IsNullOrEmpty(LayerMask.LayerToName(blankLayer)))
                        break;
                }
                if (blankLayer < 0)
                    blankLayer = 31;
            }
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                renderer.gameObject.layer = blankLayer;
            }
            var renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(renderer => renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0).ToArray();
            foreach (var renderer in renderers)
            {
                renderer.updateWhenOffscreen = true;
                renderer.forceMatrixRecalculationPerRender = true;
            }

            Dictionary<Renderer, bool> allRenderersEnabled = new();
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                allRenderersEnabled.Add(renderer, renderer.enabled);
            }

            {
                var iconTexture = new RenderTexture(IconTextureSize, IconTextureSize, 16, RenderTextureFormat.ARGB32);
                {
                    iconTexture.hideFlags |= HideFlags.HideAndDontSave;
                    iconTexture.Create();
                }
                var blankColors = new Color32[IconTextureSize * IconTextureSize];
                var cameraObject = new GameObject();
                {
                    cameraObject.hideFlags |= HideFlags.HideAndDontSave;
                    cameraObject.transform.SetParent(gameObject.transform);
                }
                var camera = cameraObject.AddComponent<Camera>();
                {
                    camera.targetTexture = iconTexture;
                    camera.clearFlags = CameraClearFlags.Color;
                    camera.backgroundColor = Color.clear;
                    camera.cullingMask = 1 << blankLayer;
                }

                Mesh bakeMesh = new();
                {
                    bakeMesh.hideFlags |= HideFlags.HideAndDontSave;
                }

                Dictionary<Renderer, Vector3[]> defaultVertices = new();
                var vertices = new List<Vector3>();
                if (iconCameraBounds != IconCameraBounds.allRenderers)
                {
                    foreach (var renderer in renderers)
                    {
                        if (renderer.sharedMesh == null)
                            continue;

                        renderer.BakeMesh(bakeMesh);
                        bakeMesh.GetVertices(vertices);

                        defaultVertices.Add(renderer, vertices.ToArray());
                    }
                }

                foreach (var set in VAW.VA.blendShapeSetList)
                {
                    Bounds bounds = new();

                    switch (iconCameraBounds)
                    {
                        case IconCameraBounds.allRenderers:
                        case IconCameraBounds.focusChangedRenderers:
                            foreach (var pair in allRenderersEnabled)
                            {
                                pair.Key.enabled = pair.Value;
                            }
                            break;
                        case IconCameraBounds.onlyRrenderersWithChanges:
                            foreach (var pair in allRenderersEnabled)
                            {
                                pair.Key.enabled = false;
                            }
                            break;
                    }

                    if (set.poseTemplate.blendShapePaths != null && set.poseTemplate.blendShapeValues != null)
                    {
                        foreach (var renderer in renderers)
                        {
                            var path = AnimationUtility.CalculateTransformPath(renderer.transform, gameObject.transform);
                            var index = EditorCommon.ArrayIndexOf(set.poseTemplate.blendShapePaths, path);
                            if (index < 0)
                                continue;

                            for (int i = 0; i < set.poseTemplate.blendShapeValues[index].names.Length; i++)
                            {
                                var sindex = renderer.sharedMesh.GetBlendShapeIndex(set.poseTemplate.blendShapeValues[index].names[i]);
                                if (sindex < 0 || sindex >= renderer.sharedMesh.blendShapeCount) continue;
                                renderer.SetBlendShapeWeight(sindex, set.poseTemplate.blendShapeValues[index].weights[i]);
                            }

                            switch (iconCameraBounds)
                            {
                                case IconCameraBounds.allRenderers:
                                    {
                                        if (Mathf.Approximately(bounds.size.sqrMagnitude, 0f))
                                            bounds = renderer.bounds;
                                        else
                                            bounds.Encapsulate(renderer.bounds);
                                    }
                                    break;
                                case IconCameraBounds.focusChangedRenderers:
                                case IconCameraBounds.onlyRrenderersWithChanges:
                                    {
                                        renderer.BakeMesh(bakeMesh);
                                        bakeMesh.GetVertices(vertices);
                                        var bakeVertices = defaultVertices[renderer];

                                        if (bakeVertices != null && vertices.Count == bakeVertices.Length)
                                        {
                                            for (int i = 0; i < vertices.Count; i++)
                                            {
                                                if (vertices[i] != bakeVertices[i])
                                                {
                                                    if (Mathf.Approximately(bounds.size.sqrMagnitude, 0f))
                                                        bounds = renderer.bounds;
                                                    else
                                                        bounds.Encapsulate(renderer.bounds);
                                                    renderer.enabled = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                    {
                        var transform = camera.transform;
                        var sizeMax = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                        switch (iconCameraMode)
                        {
                            case IconCameraMode.forward:
                                {
                                    var rot = Quaternion.AngleAxis(180f, Vector3.up);
                                    transform.localRotation = rot;
                                    sizeMax = Mathf.Max(bounds.size.x, bounds.size.y);
                                    transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z) - transform.forward;
                                }
                                break;
                            case IconCameraMode.back:
                                {
                                    transform.localRotation = Quaternion.identity;
                                    sizeMax = Mathf.Max(bounds.size.x, bounds.size.y);
                                    transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z) - transform.forward;
                                }
                                break;
                            case IconCameraMode.up:
                                {
                                    var rot = Quaternion.AngleAxis(90f, Vector3.right);
                                    transform.localRotation = rot;
                                    sizeMax = Mathf.Max(bounds.size.x, bounds.size.z);
                                    transform.localPosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) - transform.forward;
                                }
                                break;
                            case IconCameraMode.down:
                                {
                                    var rot = Quaternion.AngleAxis(-90f, Vector3.right);
                                    transform.localRotation = rot;
                                    sizeMax = Mathf.Max(bounds.size.x, bounds.size.z);
                                    transform.localPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - transform.forward;
                                }
                                break;
                            case IconCameraMode.right:
                                {
                                    var rot = Quaternion.AngleAxis(-90f, Vector3.up);
                                    transform.localRotation = rot;
                                    sizeMax = Mathf.Max(bounds.size.y, bounds.size.z);
                                    transform.localPosition = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z) - transform.forward;
                                }
                                break;
                            case IconCameraMode.left:
                                {
                                    var rot = Quaternion.AngleAxis(90f, Vector3.up);
                                    transform.localRotation = rot;
                                    sizeMax = Mathf.Max(bounds.size.y, bounds.size.z);
                                    transform.localPosition = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z) - transform.forward;
                                }
                                break;
                        }
                        camera.orthographic = true;
                        camera.orthographicSize = sizeMax * 0.5f;
                        camera.nearClipPlane = 0.0001f;
                        camera.farClipPlane = 1f + sizeMax * 10f;
                    }

                    camera.Render();
                    {
                        RenderTexture save = RenderTexture.active;
                        RenderTexture.active = iconTexture;
                        if (set.icon == null)
                        {
                            set.icon = new Texture2D(iconTexture.width, iconTexture.height, TextureFormat.ARGB32, iconTexture.useMipMap);
                            set.icon.hideFlags |= HideFlags.HideAndDontSave;
                        }
                        if (bounds.size.sqrMagnitude > 0f)
                            set.icon.ReadPixels(new Rect(0, 0, iconTexture.width, iconTexture.height), 0, 0);
                        else
                            set.icon.SetPixels32(blankColors);
                        set.icon.Apply();
                        RenderTexture.active = save;
                    }
                }

                Mesh.DestroyImmediate(bakeMesh);
                GameObject.DestroyImmediate(cameraObject);
                iconTexture.Release();
                RenderTexture.DestroyImmediate(iconTexture);
            }

            GameObject.DestroyImmediate(gameObject);

            VAW.VA.SetUpdateSampleAnimation();
        }
    }
}
