using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    [Serializable]
    internal class HandPoseTree
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        private enum HandPoseMode
        {
            Slider,
            List,
            Left,
            Right,
            Total,
        }
        private static readonly string[] HandPoseModeString =
        {
            HandPoseMode.Slider.ToString(),
            HandPoseMode.List.ToString(),
            HandPoseMode.Left.ToString(),
            HandPoseMode.Right.ToString(),
        };

        private HandPoseMode handPoseMode;

        #region Tree
        private class HandPoseInfo
        {
            public HumanBodyBones hi;
            public int dof;
            public float scale = 1f;
        }
        private class HandPoseNode
        {
            public string name;
            public string mirrorName;
            public bool foldout;
            public int dof = -1;
            public HandPoseInfo[] infoList;
            public HandPoseNode[] children;
        }
        private readonly HandPoseNode handPoseNode;
        private readonly Dictionary<HandPoseNode, int> handPoseTreeTable;

        [SerializeField]
        private float[] handPoseValues;
        #endregion

        #region List
        private ReorderableList handPoseSetListReorderableList;
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

        public HandPoseTree()
        {
            if (VAW == null || VAW.GameObject == null)
                return;

            #region HandPoseNode
            {
                {
                    #region Slider
                    handPoseNode = new HandPoseNode()
                    {
                        name = HandPoseMode.Slider.ToString(),
                        children = new HandPoseNode[]
                        {
#region Left Finger
                            new() { name = "Left Finger", mirrorName = "Right Finger",
                                infoList = new HandPoseInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftThumbProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftThumbProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftThumbIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftThumbDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftIndexProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftIndexProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftIndexIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftIndexDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftMiddleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftMiddleDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftRingProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftRingProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftRingIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftRingDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLittleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLittleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftLittleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLittleDistal, dof = 2 },
                                },
                                children = new HandPoseNode[]
                                {
#region Left Thumb
                                    new() { name = "Left Thumb", mirrorName = "Right Finger/Right Thumb",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftThumbProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftThumbProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftThumbIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftThumbDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Left Index
                                    new() { name = "Left Index", mirrorName = "Right Finger/Right Index",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftIndexProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftIndexProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftIndexIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftIndexDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Left Middle
                                    new() { name = "Left Middle", mirrorName = "Right Finger/Right Middle",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftMiddleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftMiddleDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Left Ring
                                    new() { name = "Left Ring", mirrorName = "Right Finger/Right Ring",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftRingProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftRingProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftRingIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftRingDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Left Little
                                    new() { name = "Left Little", mirrorName = "Right Finger/Right Little",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftLittleProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftLittleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLittleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLittleDistal, dof = 2 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Right Finger
                            new() { name = "Right Finger", mirrorName = "Left Finger",
                                infoList = new HandPoseInfo[]
                                {
                                    new() { hi = HumanBodyBones.RightThumbProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightThumbProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightThumbIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightThumbDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightIndexProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightIndexProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightIndexIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightIndexDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightMiddleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightMiddleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightMiddleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightMiddleDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightRingProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightRingProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightRingIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightRingDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLittleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLittleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightLittleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLittleDistal, dof = 2 },
                                },
                                children = new HandPoseNode[]
                                {
#region Right Thumb
                                    new() { name = "Right Thumb", mirrorName = "Left Finger/Left Thumb",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightThumbProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightThumbProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightThumbIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightThumbDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Right Index
                                    new() { name = "Right Index", mirrorName = "Left Finger/Left Index",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightIndexProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightIndexProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightIndexIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightIndexDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Right Middle
                                    new() { name = "Right Middle", mirrorName = "Left Finger/Left Middle",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightMiddleProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightMiddleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightMiddleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightMiddleDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Right Ring
                                    new() { name = "Right Ring", mirrorName = "Left Finger/Left Ring",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightRingProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightRingProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightRingIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightRingDistal, dof = 2 },
                                        },
                                    },
#endregion
#region Right Little
                                    new() { name = "Right Little", mirrorName = "Left Finger/Left Little",
                                        infoList = new HandPoseInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightLittleProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightLittleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLittleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLittleDistal, dof = 2 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
                        },
                    };
                    #endregion
                }

                {
                    handPoseTreeTable = new Dictionary<HandPoseNode, int>();
                    int counter = 0;
                    void AddTable(HandPoseNode mg)
                    {
                        handPoseTreeTable.Add(mg, counter++);
                        if (mg.children != null)
                        {
                            foreach (var child in mg.children)
                            {
                                AddTable(child);
                            }
                        }
                    }

                    AddTable(handPoseNode);

                    handPoseValues = new float[handPoseTreeTable.Count];
                }
            }
            #endregion

            iconUpdate = true;
        }

        public void LoadEditorPref()
        {
            handPoseMode = (HandPoseMode)EditorPrefs.GetInt("VeryAnimation_HandPoseMode", 0);
            iconShowName = EditorPrefs.GetBool("VeryAnimation_Control_HandPoseSetIconShowName", true);
            iconSize = EditorPrefs.GetFloat("VeryAnimation_Control_HandPoseSetIconSize", 100f);
            iconCameraMode = (IconCameraMode)EditorPrefs.GetInt("VeryAnimation_HandPoseSetIconCameraMode", (int)IconCameraMode.up);
        }
        public void SaveEditorPref()
        {
            EditorPrefs.SetInt("VeryAnimation_HandPoseMode", (int)handPoseMode);
            EditorPrefs.SetBool("VeryAnimation_Control_HandPoseSetIconShowName", iconShowName);
            EditorPrefs.SetFloat("VeryAnimation_Control_HandPoseSetIconSize", iconSize);
            EditorPrefs.SetInt("VeryAnimation_HandPoseSetIconCameraMode", (int)iconCameraMode);
        }

        public void HandPoseToolbarGUI()
        {
            EditorGUI.BeginChangeCheck();
            var m = (HandPoseMode)GUILayout.Toolbar((int)handPoseMode, HandPoseModeString, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                handPoseMode = m;
            }
        }

        private struct MuscleValue
        {
            public int muscleIndex;
            public float value;
        }
        public void HandPoseTreeGUI()
        {
            RowCount = 0;

            var e = Event.current;

            GUIStyleReady();

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            if (handPoseMode == HandPoseMode.Slider)
            {
                #region Slider
                var mgRoot = handPoseNode;

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
                            for (int hi = (int)HumanBodyBones.LeftThumbProximal; hi <= (int)HumanBodyBones.RightLittleDistal; hi++)
                            {
                                if (VAW.VA.HumanoidBones[hi] != null)
                                    combineGoList.Add(VAW.VA.HumanoidBones[hi]);
                                else if (VeryAnimation.HumanVirtualBones[hi] != null)
                                    combineVirtualList.Add((HumanBodyBones)hi);
                            }
                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                        }
                        else
                        {
                            var combineGoList = new List<GameObject>();
                            var combineVirtualList = new List<HumanBodyBones>();
                            for (int hi = (int)HumanBodyBones.LeftThumbProximal; hi <= (int)HumanBodyBones.RightLittleDistal; hi++)
                            {
                                if (VAW.VA.HumanoidBones[hi] != null)
                                    combineGoList.Add(VAW.VA.HumanoidBones[hi]);
                                else if (VeryAnimation.HumanVirtualBones[hi] != null)
                                    combineVirtualList.Add((HumanBodyBones)hi);
                            }
                            Selection.activeGameObject = combineGoList[0];
                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                        }
                    }
                    EditorGUILayout.Space();

                    if (GUILayout.Button("Reset All", GUILayout.Width(100)))
                    {
                        Undo.RecordObject(VAE, "Reset All Muscle Group");
                        foreach (var root in mgRoot.children)
                        {
                            var muscles = new List<MuscleValue>();
                            SetHandPoseValue(root, 0f, muscles);
                            SetAnimationCurveMuscleValues(muscles);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion

                EditorGUILayout.Space();

                #region Muscle
                {
                    int maxLevel = 0;
                    foreach (var root in mgRoot.children)
                    {
                        maxLevel = Math.Max(GetTreeLevel(root, 0), maxLevel);
                    }
                    foreach (var root in mgRoot.children)
                    {
                        HandPoseTreeNodeGUI(root, 0, maxLevel);
                    }
                }
                #endregion
                #endregion
            }
            else if (handPoseMode == HandPoseMode.List)
            {
                #region List
                if (e.type == EventType.Layout)
                {
                    UpdateHandPoseSetListReorderableList();
                }
                handPoseSetListReorderableList?.DoLayoutList();
                #endregion
            }
            else if (handPoseMode == HandPoseMode.Left || handPoseMode == HandPoseMode.Right)
            {
                #region Icon
                if (e.type == EventType.Layout)
                {
                    UpdateHandPoseSetIcon();
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
                    EditorGUILayout.Space();
                    iconShowName = GUILayout.Toggle(iconShowName, "Show Name", EditorStyles.toolbarButton);
                    EditorGUILayout.Space();
                    iconSize = EditorGUILayout.Slider(iconSize, 32f, IconTextureSize);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();
                if (VAW.VA.handPoseSetList.Count > 0)
                {
                    float areaWidth = VAE.position.width - 16f;
                    int countX = Math.Max(1, Mathf.FloorToInt(areaWidth / iconSize));
                    int countY = Mathf.CeilToInt(VAW.VA.handPoseSetList.Count / (float)countX);
                    for (int i = 0; i < countY; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int j = 0; j < countX; j++)
                        {
                            var index = i * countX + j;
                            if (index >= VAW.VA.handPoseSetList.Count) break;
                            var rect = EditorGUILayout.GetControlRect(false, iconSize, guiStyleIconButton, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                            var icon = handPoseMode == HandPoseMode.Left ? VAW.VA.handPoseSetList[index].iconLeft : VAW.VA.handPoseSetList[index].iconRight;
                            if (GUI.Button(rect, icon, guiStyleIconButton))
                            {
                                var set = VAW.VA.handPoseSetList[index];
                                if (handPoseMode == HandPoseMode.Left)
                                    set.SetLeft();
                                else
                                    set.SetRight();
                                VAW.VA.LoadPoseTemplate(set.poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                                if (VAW.VA.optionsMirror)
                                {
                                    if (handPoseMode == HandPoseMode.Left)
                                        set.SetRight();
                                    else
                                        set.SetLeft();
                                    VAW.VA.LoadPoseTemplate(set.poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                                }
                            }
                            if (iconShowName)
                            {
                                var size = guiStyleNameLabelCenter.CalcSize(new GUIContent(VAW.VA.handPoseSetList[index].poseTemplate.name));
                                if (size.x < rect.width)
                                    EditorGUI.DropShadowLabel(rect, VAW.VA.handPoseSetList[index].poseTemplate.name, guiStyleNameLabelCenter);
                                else
                                    EditorGUI.DropShadowLabel(rect, VAW.VA.handPoseSetList[index].poseTemplate.name, guiStyleNameLabelRight);
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

        private void UpdateHandPoseSetListReorderableList()
        {
            if (handPoseSetListReorderableList != null)
                return;

            handPoseSetListReorderableList = new ReorderableList(VAW.VA.handPoseSetList, typeof(PoseTemplate), true, true, true, true)
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
                            if (GUI.Button(r, Language.GetContent(Language.Help.HandPoseTemplate), EditorStyles.toolbarDropDown))
                            {
                                var handPoseTemplates = new Dictionary<string, string>();
                                {
                                    var guids = AssetDatabase.FindAssets("t:handposetemplate");
                                    for (int i = 0; i < guids.Length; i++)
                                    {
                                        var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                                        var name = path["Assets/".Length..];
                                        handPoseTemplates.Add(name, path);
                                    }
                                }

                                var menu = new GenericMenu();
                                {
                                    var enu = handPoseTemplates.GetEnumerator();
                                    while (enu.MoveNext())
                                    {
                                        var value = enu.Current.Value;
                                        menu.AddItem(new GUIContent(enu.Current.Key), false, () =>
                                        {
                                            var handPoseTemplate = AssetDatabase.LoadAssetAtPath<HandPoseTemplate>(value);
                                            if (handPoseTemplate != null)
                                            {
                                                Undo.RecordObject(VAW, "Template HandPose");
                                                foreach (var template in handPoseTemplate.list)
                                                {
                                                    var poseTemplate = template.GetPoseTemplate();
                                                    string[] rightMusclePropertyNames = new string[poseTemplate.musclePropertyNames.Length];
                                                    for (int i = 0; i < poseTemplate.musclePropertyNames.Length; i++)
                                                    {
                                                        rightMusclePropertyNames[i] = poseTemplate.musclePropertyNames[i].Replace("Left", "Right");
                                                    }
                                                    VAW.VA.handPoseSetList.Add(new VeryAnimation.HandPoseSet()
                                                    {
                                                        poseTemplate = poseTemplate,
                                                        leftMusclePropertyNames = poseTemplate.musclePropertyNames,
                                                        rightMusclePropertyNames = rightMusclePropertyNames,
                                                    });
                                                }
                                                iconUpdate = true;
                                            }
                                        });
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
                                Undo.RecordObject(VAW, "Clear HandPose");
                                VAW.VA.handPoseSetList.Clear();
                            }
                        }
                        #endregion
                        #region Save as
                        {
                            var r = rect;
                            r.width = ButtonWidth;
                            r.x = rect.xMax - r.width;
                            if (GUI.Button(r, Language.GetContent(Language.Help.HandPoseSaveAs), EditorStyles.toolbarButton))
                            {
                                string path = EditorUtility.SaveFilePanel("Save as HandPose Template", VAE.TemplateSaveDefaultDirectory, string.Format("{0}_HandPose.asset", VAW.VA.CurrentClip.name), "asset");
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
                                        var handPoseTemplate = ScriptableObject.CreateInstance<HandPoseTemplate>();
                                        {
                                            foreach (var set in VAW.VA.handPoseSetList)
                                            {
                                                handPoseTemplate.Add(VAW.VA.MusclePropertyName, set.poseTemplate);
                                            }
                                        }
                                        try
                                        {
                                            VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
                                            AssetDatabase.CreateAsset(handPoseTemplate, path);
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
            handPoseSetListReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= VAW.VA.handPoseSetList.Count)
                    return;

                float x = rect.x;
                {
                    const float Rate = 0.5f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (index == handPoseSetListReorderableList.index)
                    {
                        EditorGUI.BeginChangeCheck();
                        var text = EditorGUI.TextField(r, VAW.VA.handPoseSetList[index].poseTemplate.name);
                        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(text))
                        {
                            Undo.RecordObject(VAW.VA.handPoseSetList[index].poseTemplate, "Change set name");
                            VAW.VA.handPoseSetList[index].poseTemplate.name = text;
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(r, VAW.VA.handPoseSetList[index].poseTemplate.name);
                    }
                }
                {
                    const float Rate = 0.25f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (GUI.Button(r, "Left"))
                    {
                        var set = VAW.VA.handPoseSetList[index];
                        set.SetLeft();
                        VAW.VA.LoadPoseTemplate(set.poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                        if (VAW.VA.optionsMirror)
                        {
                            set.SetRight();
                            VAW.VA.LoadPoseTemplate(set.poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                        }
                    }
                }
                {
                    const float Rate = 0.25f;
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    if (GUI.Button(r, "Right"))
                    {
                        var set = VAW.VA.handPoseSetList[index];
                        set.SetRight();
                        VAW.VA.LoadPoseTemplate(set.poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                        if (VAW.VA.optionsMirror)
                        {
                            set.SetLeft();
                            VAW.VA.LoadPoseTemplate(set.poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                        }
                    }
                }
            };
            handPoseSetListReorderableList.onAddDropdownCallback = (buttonRect, list) =>
            {
                void AddItem(bool isRight)
                {
                    Undo.RecordObject(VAW, "Add HandPose Set");

                    var poseTemplate = ScriptableObject.CreateInstance<PoseTemplate>();
                    VAW.VA.SavePoseTemplate(poseTemplate, VeryAnimation.PoseFlags.Humanoid);
                    {
                        poseTemplate.name = "Hand Pose " + list.count;
                    }
                    if (!isRight)
                    {
                        var beginMuscle = HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftThumbProximal, 2);
                        var endMuscle = HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftLittleDistal, 2);
                        var muscleDic = new Dictionary<string, float>();
                        for (int i = 0; i < poseTemplate.musclePropertyNames.Length; i++)
                        {
                            var muscleIndex = VAW.VA.GetMuscleIndexFromPropertyName(poseTemplate.musclePropertyNames[i]);
                            if (muscleIndex >= beginMuscle && muscleIndex <= endMuscle)
                                muscleDic.Add(poseTemplate.musclePropertyNames[i], poseTemplate.muscleValues[i]);
                        }
                        poseTemplate.musclePropertyNames = muscleDic.Keys.ToArray();
                        poseTemplate.muscleValues = muscleDic.Values.ToArray();

                        string[] rightMusclePropertyNames = new string[poseTemplate.musclePropertyNames.Length];
                        for (int i = 0; i < poseTemplate.musclePropertyNames.Length; i++)
                        {
                            rightMusclePropertyNames[i] = poseTemplate.musclePropertyNames[i].Replace("Left", "Right");
                        }
                        VAW.VA.handPoseSetList.Add(new VeryAnimation.HandPoseSet()
                        {
                            poseTemplate = poseTemplate,
                            leftMusclePropertyNames = poseTemplate.musclePropertyNames,
                            rightMusclePropertyNames = rightMusclePropertyNames,
                        });
                    }
                    else
                    {
                        var beginMuscle = HumanTrait.MuscleFromBone((int)HumanBodyBones.RightThumbProximal, 2);
                        var endMuscle = HumanTrait.MuscleFromBone((int)HumanBodyBones.RightLittleDistal, 2);
                        var muscleDic = new Dictionary<string, float>();
                        for (int i = 0; i < poseTemplate.musclePropertyNames.Length; i++)
                        {
                            var muscleIndex = VAW.VA.GetMuscleIndexFromPropertyName(poseTemplate.musclePropertyNames[i]);
                            if (muscleIndex >= beginMuscle && muscleIndex <= endMuscle)
                                muscleDic.Add(poseTemplate.musclePropertyNames[i], poseTemplate.muscleValues[i]);
                        }
                        poseTemplate.musclePropertyNames = muscleDic.Keys.ToArray();
                        poseTemplate.muscleValues = muscleDic.Values.ToArray();

                        string[] leftMusclePropertyNames = new string[poseTemplate.musclePropertyNames.Length];
                        for (int i = 0; i < poseTemplate.musclePropertyNames.Length; i++)
                        {
                            leftMusclePropertyNames[i] = poseTemplate.musclePropertyNames[i].Replace("Right", "Left");
                        }
                        VAW.VA.handPoseSetList.Add(new VeryAnimation.HandPoseSet()
                        {
                            poseTemplate = poseTemplate,
                            leftMusclePropertyNames = leftMusclePropertyNames,
                            rightMusclePropertyNames = poseTemplate.musclePropertyNames,
                        });
                    }
                    iconUpdate = true;
                    EditorApplication.delayCall += () =>
                    {
                        handPoseSetListReorderableList.index = VAW.VA.handPoseSetList.Count - 1;
                        VAE.Repaint();
                    };
                }

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Left"), false, () =>
                {
                    AddItem(false);
                });
                menu.AddItem(new GUIContent("Right"), false, () =>
                {
                    AddItem(true);
                });
                menu.DropDown(buttonRect);
            };
            handPoseSetListReorderableList.onRemoveCallback = list =>
            {
                Undo.RecordObject(VAW, "Remove HandPose Set");
                VAW.VA.handPoseSetList.RemoveAt(list.index);
                if (list.index >= list.count)
                    list.index = list.count - 1;
            };
        }

        private void UpdateHandPoseSetIcon()
        {
            if (!iconUpdate)
                return;
            iconUpdate = false;

            if (VAW.VA.handPoseSetList == null || VAW.VA.handPoseSetList.Count <= 0)
                return;

            if (!VAW.VA.TransformPoseSave.ResetTPoseTransform() &&
                !VAW.VA.TransformPoseSave.ResetHumanDescriptionTransforms())
                VAW.VA.TransformPoseSave.ResetDefaultTransform();

            VAW.VA.BlendShapeWeightSave.ResetDefaultWeight();

            var defaultHumanPose = new HumanPose();
            VAW.VA.GetSceneObjectHumanPose(ref defaultHumanPose);

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

            var animator = gameObject.GetComponent<Animator>();
            animator.enabled = true;
            animator.Rebind();
            animator.enabled = false;
            var humanPoseHandler = new HumanPoseHandler(animator.avatar, VAW.VA.UAnimator.GetAvatarRoot(animator));

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
            var renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(renderer => renderer != null && renderer.sharedMesh != null).ToArray();
            foreach (var renderer in renderers)
            {
                renderer.updateWhenOffscreen = true;
                renderer.forceMatrixRecalculationPerRender = true;
            }

            {
                var iconTexture = new RenderTexture(IconTextureSize, IconTextureSize, 16, RenderTextureFormat.ARGB32);
                iconTexture.hideFlags |= HideFlags.HideAndDontSave;
                iconTexture.Create();
                var cameraObject = new GameObject();
                cameraObject.hideFlags |= HideFlags.HideAndDontSave;
                var camera = cameraObject.AddComponent<Camera>();
                camera.targetTexture = iconTexture;
                camera.clearFlags = CameraClearFlags.Color;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << blankLayer;

                Bounds leftBounds = new(), rightBounds = new();
                for (int loop = 0; loop < 2; loop++)
                {
                    Bounds bounds = new();
                    void AddBounds(HumanBodyBones humanoidIndex)
                    {
                        var t = animator.GetBoneTransform(humanoidIndex);
                        if (t == null)
                            return;

                        float size = animator.humanScale * 0.05f;
                        var subBounds = new Bounds(t.position, Vector3.one * size);
                        if (Mathf.Approximately(bounds.size.sqrMagnitude, 0f))
                            bounds = subBounds;
                        else
                            bounds.Encapsulate(subBounds);
                    }
                    AddBounds(loop == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                    var beginIndex = loop == 0 ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal;
                    var endIndex = loop == 0 ? HumanBodyBones.LeftLittleDistal : HumanBodyBones.RightLittleDistal;
                    for (var index = beginIndex; index <= endIndex; index++)
                    {
                        AddBounds(index);
                    }
                    if (loop == 0)
                        leftBounds = bounds;
                    else
                        rightBounds = bounds;
                }
                for (int loop = 0; loop < 2; loop++)
                {
                    {
                        var bounds = loop == 0 ? leftBounds : rightBounds;
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
                        camera.orthographicSize = sizeMax * 0.6f;
                        camera.nearClipPlane = 0.0001f;
                        camera.farClipPlane = 1f + sizeMax * 10f;
                    }

                    foreach (var set in VAW.VA.handPoseSetList)
                    {
                        var humanPose = new HumanPose()
                        {
                            bodyPosition = defaultHumanPose.bodyPosition,
                            bodyRotation = defaultHumanPose.bodyRotation,
                            muscles = defaultHumanPose.muscles.ToArray(),
                        };
                        set.SetLeft();
                        for (int i = 0; i < set.poseTemplate.musclePropertyNames.Length; i++)
                        {
                            var leftMuscleIndex = VAW.VA.GetMuscleIndexFromPropertyName(set.poseTemplate.musclePropertyNames[i]);
                            if (leftMuscleIndex < 0)
                                continue;
                            humanPose.muscles[leftMuscleIndex] = set.poseTemplate.muscleValues[i];
                            var rightMuscleIndex = VAW.VA.GetMirrorMuscleIndex(leftMuscleIndex);
                            if (rightMuscleIndex < 0)
                                continue;
                            humanPose.muscles[rightMuscleIndex] = set.poseTemplate.muscleValues[i];
                        }
                        humanPoseHandler.SetHumanPose(ref humanPose);

                        camera.Render();
                        {
                            RenderTexture save = RenderTexture.active;
                            RenderTexture.active = iconTexture;
                            var icon = loop == 0 ? set.iconLeft : set.iconRight;
                            if (icon == null)
                            {
                                icon = new Texture2D(iconTexture.width, iconTexture.height, TextureFormat.ARGB32, iconTexture.useMipMap);
                                icon.hideFlags |= HideFlags.HideAndDontSave;
                            }
                            icon.ReadPixels(new Rect(0, 0, iconTexture.width, iconTexture.height), 0, 0);
                            icon.Apply();
                            if (loop == 0)
                                set.iconLeft = icon;
                            else
                                set.iconRight = icon;
                            RenderTexture.active = save;
                        }
                    }
                }

                GameObject.DestroyImmediate(cameraObject);
                iconTexture.Release();
                RenderTexture.DestroyImmediate(iconTexture);
            }

            GameObject.DestroyImmediate(gameObject);

            VAW.VA.SetUpdateSampleAnimation();
        }

        #region HandPoseTreeGUI
        private int RowCount = 0;
        private const int IndentWidth = 15;
        private int GetTreeLevel(HandPoseNode mg, int level)
        {
            if (mg.foldout)
            {
                if (mg.children != null && mg.children.Length > 0)
                {
                    int tmp = level;
                    foreach (var child in mg.children)
                    {
                        tmp = Math.Max(tmp, GetTreeLevel(child, level + 1));
                    }
                    level = tmp;
                }
                else if (mg.infoList != null && mg.infoList.Length > 0)
                {
                    level++;
                }
            }
            return level;
        }
        private HandPoseNode GetMirrorNode(HandPoseNode mg)
        {
            if (string.IsNullOrEmpty(mg.mirrorName))
                return null;
            var splits = mg.mirrorName.Split('/');
            HandPoseNode mirrorNode = handPoseNode;
            for (int i = 0; i < splits.Length; i++)
            {
                var index = ArrayUtility.FindIndex(mirrorNode.children, (node) => node.name == splits[i]);
                mirrorNode = mirrorNode.children[index];
            }
            Assert.IsTrue(mirrorNode.name == Path.GetFileName(mg.mirrorName));
            return mirrorNode;
        }
        private void SetHandPoseFoldout(HandPoseNode mg, bool foldout)
        {
            mg.foldout = foldout;
            if (mg.children != null)
            {
                foreach (var child in mg.children)
                {
                    SetHandPoseFoldout(child, foldout);
                }
            }
        }
        private bool ContainsHandPose(HandPoseNode mg)
        {
            if (mg.infoList != null)
            {
                foreach (var info in mg.infoList)
                {
                    var muscleIndex = HumanTrait.MuscleFromBone((int)info.hi, info.dof);
                    if (VAW.VA.HumanoidMuscleContains[muscleIndex]) return true;
                }
            }
            if (mg.children != null && mg.children.Length > 0)
            {
                foreach (var child in mg.children)
                {
                    if (ContainsHandPose(child)) return true;
                }
            }
            return false;
        }
        private void SetHandPoseValue(HandPoseNode mg, float value, List<MuscleValue> muscles)
        {
            handPoseValues[handPoseTreeTable[mg]] = value;
            if (mg.infoList != null)
            {
                foreach (var info in mg.infoList)
                {
                    var muscleIndex = HumanTrait.MuscleFromBone((int)info.hi, info.dof);
                    muscles.Add(new MuscleValue() { muscleIndex = muscleIndex, value = value * info.scale });
                }
            }
            if (mg.children != null && mg.children.Length > 0)
            {
                foreach (var child in mg.children)
                {
                    SetHandPoseValue(child, value, muscles);
                }
            }
        }
        private void SetAnimationCurveMuscleValues(List<MuscleValue> muscles)
        {
            bool[] doneFlags = null;
            for (int i = 0; i < muscles.Count; i++)
            {
                if (VAW.VA.optionsMirror)
                {
                    doneFlags ??= new bool[HumanTrait.MuscleCount];
                    var mmuscleIndex = VAW.VA.GetMirrorMuscleIndex(muscles[i].muscleIndex);
                    if (mmuscleIndex >= 0 && doneFlags[mmuscleIndex])
                        continue;
                    doneFlags[muscles[i].muscleIndex] = true;
                }
                VAW.VA.SetAnimationValueAnimatorMuscleIfNotOriginal(muscles[i].muscleIndex, muscles[i].value);
            }
        }
        private void HandPoseTreeNodeGUI(HandPoseNode mg, int level, int brotherMaxLevel)
        {
            const int FoldoutWidth = 22;
            const int FoldoutSpace = 17;
            const int FloatFieldWidth = 44;
            var indentSpace = IndentWidth * level;
            var e = Event.current;
            var mgContains = ContainsHandPose(mg);
            EditorGUI.BeginDisabledGroup(!mgContains);
            EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
            {
                {
                    EditorGUI.indentLevel = level;
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(indentSpace + FoldoutWidth));
                    EditorGUI.BeginChangeCheck();
                    mg.foldout = EditorGUI.Foldout(rect, mg.foldout, "", true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (Event.current.alt)
                            SetHandPoseFoldout(mg, mg.foldout);
                    }
                    EditorGUI.indentLevel = 0;
                }
                {
                    void SelectNodeAll(HandPoseNode node)
                    {
                        var humanoidIndexes = new HashSet<HumanBodyBones>();
                        var bindings = new HashSet<EditorCurveBinding>();
                        if (node.infoList != null && node.infoList.Length > 0)
                        {
                            foreach (var info in node.infoList)
                            {
                                humanoidIndexes.Add(info.hi);
                                var muscleIndex = HumanTrait.MuscleFromBone((int)info.hi, info.dof);
                                bindings.Add(VAW.VA.AnimationCurveBindingAnimatorMuscle(muscleIndex));
                            }
                        }
                        if (Shortcuts.IsKeyControl(e) || e.shift)
                        {
                            var combineGoList = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                            var combineVirtualList = new HashSet<HumanBodyBones>();
                            if (VAW.VA.SelectionHumanVirtualBones != null)
                                combineVirtualList.UnionWith(VAW.VA.SelectionHumanVirtualBones);
                            foreach (var hi in humanoidIndexes)
                            {
                                if (VAW.VA.HumanoidBones[(int)hi] != null)
                                    combineGoList.Add(VAW.VA.HumanoidBones[(int)hi]);
                                else if (VeryAnimation.HumanVirtualBones[(int)hi] != null)
                                    combineVirtualList.Add(hi);
                            }
                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                            bindings.UnionWith(VAW.VA.UAw.GetCurveSelection());
                            VAW.VA.SetAnimationWindowSynchroSelection(bindings.ToArray());
                        }
                        else
                        {
                            if (humanoidIndexes.Count > 0)
                            {
                                foreach (var hi in humanoidIndexes)
                                {
                                    if (VAW.VA.HumanoidBones[(int)hi] != null)
                                    {
                                        Selection.activeGameObject = VAW.VA.HumanoidBones[(int)hi];
                                        break;
                                    }
                                }
                            }
                            VAW.VA.SelectHumanoidBones(humanoidIndexes.ToArray());
                            VAW.VA.SetAnimationWindowSynchroSelection(bindings.ToArray());
                        }
                    }
                    if (GUILayout.Button(new GUIContent(mg.name, mg.name), GUILayout.Width(VAW.EditorSettings.SettingEditorNameFieldWidth)))
                    {
                        SelectNodeAll(mg);
                    }
                    if (!string.IsNullOrEmpty(mg.mirrorName))
                    {
                        if (GUILayout.Button(new GUIContent("", string.Format("Mirror: '{0}'", Path.GetFileName(mg.mirrorName))), VAW.GuiStyleMirrorButton, GUILayout.Width(VAW.MirrorTex.width), GUILayout.Height(VAW.MirrorTex.height)))
                        {
                            SelectNodeAll(GetMirrorNode(mg));
                        }
                    }
                    else
                    {
                        GUILayout.Space(FoldoutSpace);
                    }
                }
                {
                    var saveBackgroundColor = GUI.backgroundColor;
                    switch (mg.dof)
                    {
                        case 0: GUI.backgroundColor = Handles.xAxisColor; break;
                        case 1: GUI.backgroundColor = Handles.yAxisColor; break;
                        case 2: GUI.backgroundColor = Handles.zAxisColor; break;
                    }
                    EditorGUI.BeginChangeCheck();
                    var value = GUILayout.HorizontalSlider(handPoseValues[handPoseTreeTable[mg]], -1f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAE, "Change Muscle Group");
                        List<MuscleValue> muscles = new();
                        SetHandPoseValue(mg, value, muscles);
                        SetAnimationCurveMuscleValues(muscles);
                        if (VAW.VA.optionsMirror)
                        {
                            var mirrorNode = GetMirrorNode(mg);
                            if (mirrorNode != null)
                                SetHandPoseValue(mirrorNode, value, muscles);
                        }
                    }
                    GUI.backgroundColor = saveBackgroundColor;
                }
                {
                    var width = FloatFieldWidth + IndentWidth * Math.Max(GetTreeLevel(mg, 0), brotherMaxLevel);
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUILayout.FloatField(handPoseValues[handPoseTreeTable[mg]], GUILayout.Width(width));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAE, "Change Muscle Group");
                        List<MuscleValue> muscles = new();
                        SetHandPoseValue(mg, value, muscles);
                        SetAnimationCurveMuscleValues(muscles);
                        if (VAW.VA.optionsMirror)
                        {
                            var mirrorNode = GetMirrorNode(mg);
                            if (mirrorNode != null)
                                SetHandPoseValue(mirrorNode, value, muscles);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            if (mg.foldout)
            {
                if (mg.children != null && mg.children.Length > 0)
                {
                    int maxLevel = 0;
                    foreach (var child in mg.children)
                    {
                        maxLevel = Math.Max(GetTreeLevel(child, 0), maxLevel);
                    }
                    foreach (var child in mg.children)
                    {
                        HandPoseTreeNodeGUI(child, level + 1, maxLevel);
                    }
                }
                else if (mg.infoList != null && mg.infoList.Length > 0)
                {
                    #region Muscle
                    foreach (var info in mg.infoList)
                    {
                        var muscleIndex = HumanTrait.MuscleFromBone((int)info.hi, info.dof);
                        var humanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(muscleIndex);
                        var muscleValue = VAW.VA.GetAnimationValueAnimatorMuscle(muscleIndex);
                        EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                        {
                            EditorGUILayout.GetControlRect(false, GUILayout.Width(indentSpace + FoldoutWidth));
                            GUILayout.Space(IndentWidth);
                        }
                        {
                            var contains = VAW.VA.HumanoidBones[(int)humanoidIndex] != null || VeryAnimation.HumanVirtualBones[(int)humanoidIndex] != null;
                            EditorGUI.BeginDisabledGroup(!contains);
                            if (GUILayout.Button(new GUIContent(VAW.VA.MusclePropertyName.Names[muscleIndex], VAW.VA.MusclePropertyName.Names[muscleIndex]), GUILayout.Width(VAW.EditorSettings.SettingEditorNameFieldWidth)))
                            {
                                var humanoidIndexes = new HashSet<HumanBodyBones>();
                                var bindings = new HashSet<EditorCurveBinding>();
                                {
                                    humanoidIndexes.Add(info.hi);
                                    bindings.Add(VAW.VA.AnimationCurveBindingAnimatorMuscle(muscleIndex));
                                }
                                if (Shortcuts.IsKeyControl(e) || e.shift)
                                {
                                    var combineGoList = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                                    var combineVirtualList = new HashSet<HumanBodyBones>();
                                    if (VAW.VA.SelectionHumanVirtualBones != null)
                                        combineVirtualList.UnionWith(VAW.VA.SelectionHumanVirtualBones);
                                    foreach (var hi in humanoidIndexes)
                                    {
                                        if (VAW.VA.HumanoidBones[(int)hi] != null)
                                            combineGoList.Add(VAW.VA.HumanoidBones[(int)hi]);
                                        else if (VeryAnimation.HumanVirtualBones[(int)hi] != null)
                                            combineVirtualList.Add(hi);
                                    }
                                    VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                                    bindings.UnionWith(VAW.VA.UAw.GetCurveSelection());
                                    VAW.VA.SetAnimationWindowSynchroSelection(bindings.ToArray());
                                }
                                else
                                {
                                    if (humanoidIndexes.Count > 0)
                                    {
                                        foreach (var hi in humanoidIndexes)
                                        {
                                            if (VAW.VA.HumanoidBones[(int)hi] != null)
                                            {
                                                Selection.activeGameObject = VAW.VA.HumanoidBones[(int)hi];
                                                break;
                                            }
                                        }
                                    }
                                    VAW.VA.SelectHumanoidBones(humanoidIndexes.ToArray());
                                    VAW.VA.SetAnimationWindowSynchroSelection(bindings.ToArray());
                                }
                            }
                            EditorGUI.EndDisabledGroup();
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
                            EditorGUI.BeginDisabledGroup(!VAW.VA.HumanoidMuscleContains[muscleIndex]);
                            var saveBackgroundColor = GUI.backgroundColor;
                            switch (info.dof)
                            {
                                case 0: GUI.backgroundColor = Handles.xAxisColor; break;
                                case 1: GUI.backgroundColor = Handles.yAxisColor; break;
                                case 2: GUI.backgroundColor = Handles.zAxisColor; break;
                            }
                            EditorGUI.BeginChangeCheck();
                            var value2 = GUILayout.HorizontalSlider(muscleValue, -1f, 1f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                VAW.VA.SetAnimationValueAnimatorMuscle(muscleIndex, value2);
                            }
                            GUI.backgroundColor = saveBackgroundColor;
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            var value2 = EditorGUILayout.FloatField(muscleValue, GUILayout.Width(FloatFieldWidth));
                            if (EditorGUI.EndChangeCheck())
                            {
                                VAW.VA.SetAnimationValueAnimatorMuscle(muscleIndex, value2);
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                    }
                    #endregion
                }
            }
        }
        #endregion
    }
}
