//#define Enable_Profiler

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
#if Enable_Profiler
using UnityEngine.Profiling;
#endif

namespace VeryAnimation
{
    internal class VeryAnimationControlWindow : EditorWindow
    {
        public static VeryAnimationControlWindow instance;

        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        #region Textures
        private Texture2D avatarHead;
        private Texture2D avatarTorso;
        private Texture2D avatarLeftArm;
        private Texture2D avatarLeftFingers;
        private Texture2D avatarLeftLeg;
        private Texture2D avatarRightArm;
        private Texture2D avatarRightFingers;
        private Texture2D avatarRightLeg;
        private Texture2D avatarHeadZoom;
        private Texture2D avatarLeftHandZoom;
        private Texture2D avatarRightHandZoom;
        private Texture2D avatarBodysilhouette;
        private Texture2D avatarHeadzoomsilhouette;
        private Texture2D avatarLefthandzoomsilhouette;
        private Texture2D avatarRighthandzoomsilhouette;
        private Texture2D avatarRoot;
        private Texture2D avatarLeftFeetIk;
        private Texture2D avatarRightFeetIk;
        private Texture2D avatarLeftFingersIk;
        private Texture2D avatarRightFingersIk;
        private Texture2D avatarBodyPartPicker;
        private Texture2D dotfill;
        private Texture2D dotframe;
        private Texture2D dotframedotted;
        private Texture2D dotselection;
        #endregion

        #region GUIStyles
        private GUIStyle guiStyleBackgroundBox;
        private GUIStyle guiStyleVerticalToolbar;
        private GUIStyle guiStyleBoneButton;
        #endregion

        #region Editor
        public enum HumanoidAvatarPartsMode
        {
            Body,
            Head,
            LeftHand,
            RightHand,
        }
        private readonly string[] HumanoidAvatarPartsModeStrings =
        {
            "Body",
            "Head",
            "Left Hand",
            "Right Hand",
        };
        public HumanoidAvatarPartsMode CurrentHumanoidAvatarPartsMode { get; private set; }

        private readonly Color GlayColor = new(0.2f, 0.2f, 0.2f);
        private readonly Color GreenColor = new(0.2f, 0.8f, 0.2f);
        private readonly Color BlueColor = new Color32(102, 178, 255, 255);

        private Vector2 windowScrollPosition;

        private bool guiAnimatorIkFoldout;
        private bool guiOriginalIkFoldout;
        private bool guiHumanoidFoldout;
        private bool guiSelectionFoldout;
        private bool guiHierarchyFoldout;

        private bool guiAnimatorIkVisible = true;
        private bool guiOriginalIkVisible = true;
        private bool guiHumanoidVisible = true;
        private bool guiSelectionVisible = true;
        private bool guiHierarchyVisible = true;

        private bool guiAnimatorIkHelp;
        private bool guiOriginalIkHelp;
        private bool guiHumanoidHelp;
        private bool guiSelectionHelp;
        private bool guiHierarchyHelp;

        private List<HumanBodyBones> selectionGameObjectsHumanoidIndex;
        private Dictionary<HumanBodyBones, Vector2> controlBoneList;
        private AvatarMaskBodyPart selectionAvatarMaskBodyPart;

        private Color[] maskBodyPartPicker;

        private enum SelectionType
        {
            List,
            Popup,
        }
        private static readonly string[] SelectionTypeString =
        {
            SelectionType.List.ToString(),
            SelectionType.Popup.ToString(),
        };
        private SelectionType selectionType;
        private bool updateSelectionList = true;
        private bool updateSelectionPopup = true;
        private int selectionSetIndex = -1;
        private ReorderableList selectionSetList;
        private string[] selectionSetStrings;
        #endregion

        #region Hierarchy
#if UNITY_6000_2_OR_NEWER
        private class HierarchyTreeView : TreeView<int>
#else
        private class HierarchyTreeView : TreeView
#endif
        {
            private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
            private VeryAnimationControlWindow VCW { get { return VeryAnimationControlWindow.instance; } }

            private Dictionary<Type, Texture2D> typeIconDic;

            #region GUIStyle
            private GUIStyle guiStyleLabelActiveGUIStyle;
            private GUIStyle guiStyleLabelNonActiveGUIStyle;

            private void GUIStyleReady()
            {
                #region GUIStyle
                guiStyleLabelActiveGUIStyle ??= new GUIStyle("TV Line");
                if (guiStyleLabelNonActiveGUIStyle == null)
                {
                    guiStyleLabelNonActiveGUIStyle = new GUIStyle("TV Line");

                    static Color AlphaMultiplied(Color color, float multiplier)
                    {
                        return new Color(color.r, color.g, color.b, color.a * multiplier);
                    }
                    guiStyleLabelNonActiveGUIStyle.normal.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.normal.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.hover.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.hover.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.active.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.active.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.focused.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.focused.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.onNormal.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.onNormal.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.onHover.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.onHover.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.onActive.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.onActive.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                    guiStyleLabelNonActiveGUIStyle.onFocused.textColor = AlphaMultiplied(guiStyleLabelNonActiveGUIStyle.onFocused.textColor, VeryAnimationWindow.GUINonActiveAlpha);
                }
                #endregion
            }
            #endregion


            private Texture2D GetIconTexture(Type type)
            {
                typeIconDic ??= new Dictionary<Type, Texture2D>();
                if (!typeIconDic.TryGetValue(type, out Texture2D tex))
                {
                    tex = VAW.UEditorGUIUtility.LoadIcon(type.Name + " icon");
                    typeIconDic.Add(type, tex);
                }
                return tex;
            }
#if UNITY_6000_2_OR_NEWER
            public HierarchyTreeView(TreeViewState<int> state) : base(state)
#else
            public HierarchyTreeView(TreeViewState state) : base(state)
#endif
            {
                extraSpaceBeforeIconAndLabel = 18f;

            }

#pragma warning disable IDE0060, IDE0051
#if UNITY_6000_2_OR_NEWER
            private void OnLabelOverlayGUI(TreeViewItem<int> item, Rect labelRect)
#else
            private void OnLabelOverlayGUI(TreeViewItem item, Rect labelRect)
#endif
            {

            }
#pragma warning restore IDE0060, IDE0051
#if UNITY_6000_2_OR_NEWER
            protected override TreeViewItem<int> BuildRoot()
            {
                var root = new TreeViewItem<int>(int.MinValue, -1, "Root");
                if (instance == null || VAW.VA == null || VAW.VA.IsEditError)
                {
                    root.children = new List<TreeViewItem<int>>();
                    return root;
                }
                TreeViewItem<int> CreateTreeViewItem(Transform t, int depth)
                {
                    var hi = VAW.VA.HumanoidBonesIndexOf(t.gameObject);
                    var name = t.gameObject.name;
                    if (VAW.VA.IsHuman && instance.hierarchyHumanoidName)
                    {
                        if (VAW.GameObject == t.gameObject)
                            name = "Root";
                        else if (hi >= 0)
                            name = hi.ToString();
                    }
                    var item = new TreeViewItem<int>(t.gameObject.GetInstanceID(), depth, name);
                    {
                        var boneIndex = VAW.VA.BonesIndexOf(t.gameObject);
                        var tex = boneIndex >= 0 ? GetIconTexture(VAW.VA.GetBoneType(boneIndex)) : null;
                        if (tex == null)
                            tex = GetIconTexture(typeof(Transform));
                        item.icon = tex;
                    }
                    item.children = new List<TreeViewItem<int>>(t.childCount);
                    for (int i = 0; i < t.childCount; i++)
                    {
                        item.children.Add(CreateTreeViewItem(t.GetChild(i), depth + 1));
                    }
                    return item;
                }

                int depth = 0;
                if (VCW.hierarchyWriteLock) depth++;
                root.children = (new TreeViewItem<int>[] { CreateTreeViewItem(VAW.GameObject.transform, depth) }).ToList();

                return root;
            }
#else
            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(int.MinValue, -1, "Root");
                if (instance == null || VAW.VA == null || VAW.VA.IsEditError)
                {
                    root.children = new List<TreeViewItem>();
                    return root;
                }
                TreeViewItem CreateTreeViewItem(Transform t, int depth)
                {
                    var hi = VAW.VA.HumanoidBonesIndexOf(t.gameObject);
                    var name = t.gameObject.name;
                    if (VAW.VA.IsHuman && instance.hierarchyHumanoidName)
                    {
                        if (VAW.GameObject == t.gameObject)
                            name = "Root";
                        else if (hi >= 0)
                            name = hi.ToString();
                    }
                    var item = new TreeViewItem(t.gameObject.GetInstanceID(), depth, name);
                    {
                        var boneIndex = VAW.VA.BonesIndexOf(t.gameObject);
                        var tex = boneIndex >= 0 ? GetIconTexture(VAW.VA.GetBoneType(boneIndex)) : null;
                        if (tex == null)
                            tex = GetIconTexture(typeof(Transform));
                        item.icon = tex;
                    }
                    item.children = new List<TreeViewItem>(t.childCount);
                    for (int i = 0; i < t.childCount; i++)
                    {
                        item.children.Add(CreateTreeViewItem(t.GetChild(i), depth + 1));
                    }
                    return item;
                }

                int depth = 0;
                if (VCW.hierarchyWriteLock) depth++;
                root.children = (new TreeViewItem[] { CreateTreeViewItem(VAW.GameObject.transform, depth) }).ToList();

                return root;
            }
#endif

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (VAW.VA == null || VAW.VA.IsEditError) return;

                HashSet<GameObject> selection = new();
                GameObject FindGameObject(int instanceID)
                {
                    GameObject FindChild(Transform t)
                    {
                        if (t.gameObject.GetInstanceID() == instanceID)
                            return t.gameObject;
                        for (int i = 0; i < t.childCount; i++)
                        {
                            var go = FindChild(t.GetChild(i));
                            if (go != null) return go;
                        }
                        return null;
                    }

                    return FindChild(VAW.GameObject.transform);
                }
                foreach (var instanceID in selectedIds)
                {
                    var go = FindGameObject(instanceID);
                    if (go != null)
                        selection.Add(go);
                }
                if (Event.current.alt)
                {
                    var lastGo = FindGameObject(state.lastClickedID);
                    var lastBoneIndex = VAW.VA.BonesIndexOf(lastGo);
                    VAW.VA.ActionAllBoneChildren(lastBoneIndex, (boneIndex) =>
                    {
                        selection.Add(VAW.VA.Bones[boneIndex]);
                    });
                }
                {
                    var lastGo = FindGameObject(state.lastClickedID);
                    if (lastGo != null)
                        Selection.activeGameObject = lastGo;
                }
                VAW.VA.SelectGameObjects(selection.ToArray());
            }

            protected override void DoubleClickedItem(int id)
            {
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }
            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                return true;
            }
            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                DragAndDrop.PrepareStartDrag();
                {
                    var list = new List<GameObject>();
                    foreach (var id in args.draggedItemIDs)
                    {
#if UNITY_6000_3_OR_NEWER
                        var go = EditorUtility.EntityIdToObject(id) as GameObject;
#else
                        var go = EditorUtility.InstanceIDToObject(id) as GameObject;
#endif
                        if (go == null) continue;
                        list.Add(go);
                    }
                    DragAndDrop.objectReferences = list.ToArray();
                }
                DragAndDrop.StartDrag("Dragging GameObject");
            }
            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                return DragAndDropVisualMode.Link;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (VAW.VA == null || VAW.VA.IsEditError || instance == null) return;

                const float MirrorIconWidth = 13f;
                const float MirrorIconHeight = 14f;
                const float ToggleIconWidth = 16f;

                GUIStyleReady();

#if UNITY_6000_3_OR_NEWER
                var gameObject = EditorUtility.EntityIdToObject(args.item.id) as GameObject;
#else
                var gameObject = EditorUtility.InstanceIDToObject(args.item.id) as GameObject;
#endif
                if (gameObject != null)
                {
                    var boneIndex = VAW.VA.BonesIndexOf(gameObject);
                    if (boneIndex >= 0)
                    {
                        if (VCW.hierarchyWriteLock)
                        {
                            Rect toggleRect = args.rowRect;
                            toggleRect.width = ToggleIconWidth;

                            EditorGUI.BeginChangeCheck();
                            var flag = EditorGUI.Toggle(toggleRect, VAW.VA.IsWriteLockBone(boneIndex), VAW.GuiStyleLockToggle);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change bone write lock flag");
                                VAW.VA.boneWriteLockFlags[boneIndex] = flag;
                                if (Event.current.alt && args.item.hasChildren)
                                {
#if UNITY_6000_2_OR_NEWER
                                    void SetChildren(TreeViewItem<int> item)
#else
                                    void SetChildren(TreeViewItem item)
#endif
                                    {
#if UNITY_6000_3_OR_NEWER
                                        var go = EditorUtility.EntityIdToObject(item.id) as GameObject;
#else
                                        var go = EditorUtility.InstanceIDToObject(item.id) as GameObject;
#endif
                                        if (go != null)
                                        {
                                            var bi = VAW.VA.BonesIndexOf(go);
                                            if (bi >= 0)
                                                VAW.VA.boneWriteLockFlags[bi] = flag;
                                        }
                                        if (item.hasChildren)
                                        {
                                            foreach (var i in item.children)
                                                SetChildren(i);
                                        }
                                    }

                                    foreach (var i in args.item.children)
                                        SetChildren(i);
                                }
                                InternalEditorUtility.RepaintAllViews();
                            }
                        }
                        {
                            Rect toggleRect = args.rowRect;
                            toggleRect.x += GetContentIndent(args.item);
                            toggleRect.width = ToggleIconWidth;
                            {
                                EditorGUI.BeginChangeCheck();
                                var flag = EditorGUI.Toggle(toggleRect, VAW.VA.boneShowFlags[boneIndex]);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(VAW, "Change bone show flag");
                                    VAW.VA.boneShowFlags[boneIndex] = flag;
                                    if (Event.current.alt && args.item.hasChildren)
                                    {
#if UNITY_6000_2_OR_NEWER
                                        void SetChildren(TreeViewItem<int> item)
#else
                                        void SetChildren(TreeViewItem item)
#endif
                                        {
#if UNITY_6000_3_OR_NEWER
                                            var go = EditorUtility.EntityIdToObject(item.id) as GameObject;
#else
                                            var go = EditorUtility.InstanceIDToObject(item.id) as GameObject;
#endif
                                            if (go != null)
                                            {
                                                var bi = VAW.VA.BonesIndexOf(go);
                                                if (bi >= 0)
                                                    VAW.VA.boneShowFlags[bi] = flag;
                                            }
                                            if (item.hasChildren)
                                            {
                                                foreach (var i in item.children)
                                                    SetChildren(i);
                                            }
                                        }

                                        foreach (var i in args.item.children)
                                            SetChildren(i);
                                    }
                                    VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                                    InternalEditorUtility.RepaintAllViews();
                                }
                            }
                        }
                        float currentX = 0f;
                        if (VCW.hierarchyMirrorObject)
                        {
                            Rect r = args.rowRect;
                            r.width = args.rowRect.width / 3f;
                            r.x = args.rowRect.width - r.width;
                            var mirrorBone = VAW.VA.MirrorBoneIndexes[boneIndex] >= 0 ? VAW.VA.Bones[VAW.VA.MirrorBoneIndexes[boneIndex]] : null;
                            EditorGUI.BeginDisabledGroup((VAW.VA.IsHuman && VAW.VA.BoneIndex2humanoidIndex[boneIndex] >= 0) || boneIndex == 0 || boneIndex == VAW.VA.RootMotionBoneIndex);
                            EditorGUI.BeginChangeCheck();
                            var changeBone = EditorGUI.ObjectField(r, mirrorBone, typeof(GameObject), true) as GameObject;
                            if (EditorGUI.EndChangeCheck())
                            {
                                VAW.VA.ChangeBonesMirror(boneIndex, VAW.VA.BonesIndexOf(changeBone));
                            }
                            EditorGUI.EndDisabledGroup();
                            currentX += r.width;
                        }
                        if (VAW.VA.MirrorBoneIndexes[boneIndex] >= 0)
                        {
                            Rect r = args.rowRect;
                            float Margin = (r.height - MirrorIconHeight) / 2f;
                            r.height -= Margin * 2f;
                            r.y += Margin;
                            r.width = MirrorIconWidth;
                            currentX += r.width;
                            r.x = args.rowRect.width - currentX;
                            if (GUI.Button(r, new GUIContent("", string.Format("Mirror bone: '{0}'", VAW.VA.Bones[VAW.VA.MirrorBoneIndexes[boneIndex]].name)), VAW.GuiStyleMirrorButton))
                            {
                                VAW.VA.SelectGameObject(VAW.VA.Bones[VAW.VA.MirrorBoneIndexes[boneIndex]]);
                            }
                        }

                        if (Event.current.rawType == EventType.Repaint)
                        {
                            const float IconWidth = 16f;
                            const float SpaceBetweenIconAndText = 2f;

                            var rect = args.rowRect;
                            rect.x += GetContentIndent(args.item) + ToggleIconWidth;

                            {
                                Texture icon = args.item.icon;
                                if (icon != null)
                                {
                                    Rect position = rect;
                                    position.width = IconWidth;
                                    Color color = GUI.color;
                                    color.a *= !VAW.VA.IsWriteLockBone(boneIndex) ? 1f : VeryAnimationWindow.GUINonActiveAlpha;
                                    GUI.DrawTexture(position, icon, ScaleMode.ScaleToFit, alphaBlend: true, 0f, color, 0f, 0f);
                                    rect.xMin += IconWidth + SpaceBetweenIconAndText;
                                }
                            }
                            {
                                var lineStyle = !VAW.VA.IsWriteLockBone(boneIndex) ? guiStyleLabelActiveGUIStyle : guiStyleLabelNonActiveGUIStyle;
                                lineStyle.Draw(rect, args.label, false, false, args.selected, args.focused);
                            }
                        }
                    }
                }
            }
        };

        private bool hierarchyWriteLock;
        private bool hierarchyMirrorObject;
        private bool hierarchyHumanoidName;

#if UNITY_6000_2_OR_NEWER
        private TreeViewState<int> hierarchyTreeState;
#else
        private TreeViewState hierarchyTreeState;
#endif
        private SearchField hierarchyTreeSearchField;
        private HierarchyTreeView hierarchyTreeView;

        private void UpdateHierarchyTree()
        {
            var expandList = hierarchyTreeView.GetExpanded();

            hierarchyTreeView.Reload();

            hierarchyTreeView.CollapseAll();
            hierarchyTreeView.SetExpanded(expandList);
        }

        private bool hierarchyButtonAll;
        private bool hierarchyButtonWeight;
        private bool hierarchyButtonRenderer;
        private bool hierarchyButtonRendererParent;
        private bool hierarchyButtonBody;
        private bool hierarchyButtonFace;
        private bool hierarchyButtonLeftHand;
        private bool hierarchyButtonRightHand;

        public void ActionAllExpand(Action<GameObject> action)
        {
            foreach (var id in hierarchyTreeView.GetExpanded())
            {
#if UNITY_6000_3_OR_NEWER
                var go = EditorUtility.EntityIdToObject(id) as GameObject;
#else
                var go = EditorUtility.InstanceIDToObject(id) as GameObject;
#endif
                if (go != null)
                {
                    action(go);
                }
            }
        }
        public void CollapseAll()
        {
            hierarchyTreeView.CollapseAll();
        }
        public void SetExpand(GameObject go, bool expanded)
        {
            hierarchyTreeView.SetExpanded(go.GetInstanceID(), expanded);
        }
        #endregion

        #region SelectionRect
        private struct SelectionRect
        {
            public void Reset()
            {
                Enable = false;
                Start = Vector2.zero;
                End = Vector2.zero;
                Distance = 0f;
                if (calcList == null)
                    calcList = new List<HumanBodyBones>();
                else
                    calcList.Clear();
                beforeSelection = null;
            }
            public void SetStart(Vector2 add)
            {
                Enable = true;
                Start = add;
                End = add;
                Distance = 0f;
            }
            public void SetEnd(Vector2 add)
            {
                Distance += Vector2.Distance(End, add);
                End = add;
            }
            public bool Enable { get; private set; }
            public readonly Vector2 Min => Vector2.Min(Start, End);
            public readonly Vector2 Max => Vector2.Max(Start, End);
            public readonly Rect Rect => new(Min.x, Min.y, Max.x - Min.x, Max.y - Min.y);

            public Vector2 Start { get; private set; }
            public Vector2 End { get; private set; }
            public float Distance { get; private set; }

            public List<HumanBodyBones> calcList;
            public HumanBodyBones[] beforeSelection;
        }
        private SelectionRect selectionRect;
        #endregion

        private bool initialized;

        void OnEnable()
        {
            if (VAW == null || VAW.VA == null) return;

            instance = this;

            titleContent = new GUIContent("VA Control");
            avatarHead = EditorGUIUtility.IconContent("avatarinspector/head").image as Texture2D;
            avatarTorso = EditorGUIUtility.IconContent("avatarinspector/torso").image as Texture2D;
            avatarLeftArm = EditorGUIUtility.IconContent("avatarinspector/leftarm").image as Texture2D;
            avatarLeftFingers = EditorGUIUtility.IconContent("avatarinspector/leftfingers").image as Texture2D;
            avatarLeftLeg = EditorGUIUtility.IconContent("avatarinspector/leftleg").image as Texture2D;
            avatarRightArm = EditorGUIUtility.IconContent("avatarinspector/rightarm").image as Texture2D;
            avatarRightFingers = EditorGUIUtility.IconContent("avatarinspector/rightfingers").image as Texture2D;
            avatarRightLeg = EditorGUIUtility.IconContent("avatarinspector/rightleg").image as Texture2D;
            avatarHeadZoom = EditorGUIUtility.IconContent("avatarinspector/headzoom").image as Texture2D;
            avatarLeftHandZoom = EditorGUIUtility.IconContent("avatarinspector/lefthandzoom").image as Texture2D;
            avatarRightHandZoom = EditorGUIUtility.IconContent("avatarinspector/righthandzoom").image as Texture2D;
            avatarBodysilhouette = EditorGUIUtility.IconContent("avatarinspector/bodysilhouette").image as Texture2D;
            avatarHeadzoomsilhouette = EditorGUIUtility.IconContent("avatarinspector/headzoomsilhouette").image as Texture2D;
            avatarLefthandzoomsilhouette = EditorGUIUtility.IconContent("avatarinspector/lefthandzoomsilhouette").image as Texture2D;
            avatarRighthandzoomsilhouette = EditorGUIUtility.IconContent("avatarinspector/righthandzoomsilhouette").image as Texture2D;
            avatarRoot = EditorGUIUtility.IconContent("avatarinspector/MaskEditor_Root").image as Texture2D;
            avatarLeftFeetIk = EditorGUIUtility.IconContent("avatarinspector/leftfeetik").image as Texture2D;
            avatarRightFeetIk = EditorGUIUtility.IconContent("avatarinspector/rightfeetik").image as Texture2D;
            avatarLeftFingersIk = EditorGUIUtility.IconContent("avatarinspector/leftfingersik").image as Texture2D;
            avatarRightFingersIk = EditorGUIUtility.IconContent("avatarinspector/rightfingersik").image as Texture2D;
            avatarBodyPartPicker = EditorGUIUtility.IconContent("avatarinspector/bodypartpicker").image as Texture2D;
            dotfill = EditorGUIUtility.IconContent("avatarinspector/dotfill").image as Texture2D;
            dotframe = EditorGUIUtility.IconContent("avatarinspector/dotframe").image as Texture2D;
            dotframedotted = EditorGUIUtility.IconContent("avatarinspector/dotframedotted").image as Texture2D;
            dotselection = EditorGUIUtility.IconContent("avatarinspector/dotselection").image as Texture2D;

            {
                var uBodyMaskEditor = new UBodyMaskEditor();
                maskBodyPartPicker = uBodyMaskEditor.GetMaskBodyPartPicker();
            }

            {
#if UNITY_6000_2_OR_NEWER
                hierarchyTreeState = new TreeViewState<int>();
#else
                hierarchyTreeState = new TreeViewState();
#endif
                hierarchyTreeSearchField = new SearchField();
                hierarchyTreeView = new HierarchyTreeView(hierarchyTreeState);
                hierarchyTreeSearchField.downOrUpArrowKeyPressed += hierarchyTreeView.SetFocusAndEnsureSelectedItem;
            }

            selectionGameObjectsHumanoidIndex = new List<HumanBodyBones>();
            controlBoneList = new Dictionary<HumanBodyBones, Vector2>();
            selectionAvatarMaskBodyPart = (AvatarMaskBodyPart)(-1);

            VAW.VA.OnHierarchyUpdated += UpdateHierarchyTree;
            VAW.VA.OnBoneShowFlagsUpdated += UpdateHierarchyFlags;

            GUIStyleClear();

            OnSelectionChange();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }
        void OnDisable()
        {
            if (VAW == null || VAW.VA == null) return;

            Release();

            VAW.VA.OnHierarchyUpdated -= UpdateHierarchyTree;
            VAW.VA.OnBoneShowFlagsUpdated -= UpdateHierarchyFlags;

            Undo.undoRedoPerformed -= UndoRedoPerformed;

            GUIStyleClear();

            instance = null;
        }

        public void Initialize()
        {
            Release();

            #region EditorPref
            {
                guiAnimatorIkFoldout = EditorPrefs.GetBool("VeryAnimation_Control_AnimatorIK", false);
                guiOriginalIkFoldout = EditorPrefs.GetBool("VeryAnimation_Control_OriginalIK", false);
                guiHumanoidFoldout = EditorPrefs.GetBool("VeryAnimation_Control_Humanoid", true);
                guiSelectionFoldout = EditorPrefs.GetBool("VeryAnimation_Control_Selection", false);
                guiHierarchyFoldout = EditorPrefs.GetBool("VeryAnimation_Control_Hierarchy", true);

                guiAnimatorIkVisible = EditorPrefs.GetBool("VeryAnimation_Control_AnimatorIKVisible", true);
                guiOriginalIkVisible = EditorPrefs.GetBool("VeryAnimation_Control_OriginalIKVisible", true);
                guiHumanoidVisible = EditorPrefs.GetBool("VeryAnimation_Control_HumanoidVisible", true);
                guiSelectionVisible = EditorPrefs.GetBool("VeryAnimation_Control_SelectionVisible", true);
                guiHierarchyVisible = EditorPrefs.GetBool("VeryAnimation_Control_HierarchyVisible", true);

                selectionType = (SelectionType)EditorPrefs.GetInt("VeryAnimation_Control_SelectionType", 0);
                hierarchyWriteLock = EditorPrefs.GetBool("VeryAnimation_Control_HierarchyWriteLock", false);
                hierarchyMirrorObject = EditorPrefs.GetBool("VeryAnimation_Control_HierarchyMirrorObject", false);
                hierarchyHumanoidName = EditorPrefs.GetBool("VeryAnimation_Control_HierarchyHumanoidName", true);
            }
            #endregion

            updateSelectionList = true;
            updateSelectionPopup = true;

            UpdateHierarchyTree();
            UpdateHierarchyFlags();

            hierarchyTreeView.ExpandAll();

            initialized = true;
        }
        private void Release()
        {
            if (!initialized) return;

            #region EditorPref
            {
                EditorPrefs.SetBool("VeryAnimation_Control_AnimatorIK", guiAnimatorIkFoldout);
                EditorPrefs.SetBool("VeryAnimation_Control_OriginalIK", guiOriginalIkFoldout);
                EditorPrefs.SetBool("VeryAnimation_Control_Humanoid", guiHumanoidFoldout);
                EditorPrefs.SetBool("VeryAnimation_Control_Selection", guiSelectionFoldout);
                EditorPrefs.SetBool("VeryAnimation_Control_Hierarchy", guiHierarchyFoldout);

                EditorPrefs.SetBool("VeryAnimation_Control_AnimatorIKVisible", guiAnimatorIkVisible);
                EditorPrefs.SetBool("VeryAnimation_Control_OriginalIKVisible", guiOriginalIkVisible);
                EditorPrefs.SetBool("VeryAnimation_Control_HumanoidVisible", guiHumanoidVisible);
                EditorPrefs.SetBool("VeryAnimation_Control_SelectionVisible", guiSelectionVisible);
                EditorPrefs.SetBool("VeryAnimation_Control_HierarchyVisible", guiHierarchyVisible);
            }
            #endregion
        }

        void OnSelectionChange()
        {
            if (VAW.VA == null || VAW.VA.IsEditError) return;

            if (hierarchyTreeState != null)
            {
                List<int> selectedIDs = new();
                foreach (var go in Selection.gameObjects)
                {
                    selectedIDs.Add(go.GetInstanceID());
                    if (VAW.EditorSettings.SettingHierarchyExpandSelectObject)
                    {
                        var tmp = go.transform.parent;
                        while (tmp != null)
                        {
                            SetExpand(tmp.gameObject, true);
                            tmp = tmp.transform.parent;
                        }
                    }
                }
                hierarchyTreeState.selectedIDs = selectedIDs;

                if (VAW.EditorSettings.SettingHierarchyExpandSelectObject &&
                    Selection.activeGameObject != null)
                {
                    try
                    {
#if UNITY_6000_3_OR_NEWER
                        hierarchyTreeView.FrameItem(Selection.activeEntityId);
#else
                        hierarchyTreeView.FrameItem(Selection.activeInstanceID);
#endif
                    }
                    catch
                    {
                    }
                }
            }

            if (guiSelectionFoldout)
            {
                EditorApplication.delayCall += () =>
                {
                    UpdateSelection();
                };
            }

            Repaint();
        }

        void OnInspectorUpdate()
        {
            if (VAW == null || VAW.VA == null || !VAW.Initialized || VAE == null)
            {
                Close();
                return;
            }
        }

        void OnGUI()
        {
            if (VAW.VA == null || VAW.VA.IsEditError || !VAW.GuiStyleReady) return;

#if Enable_Profiler
            Profiler.BeginSample("****VeryAnimationControlWindow.OnGUI");
#endif

            GUIStyleReady();

            Event e = Event.current;
            bool repaint = false;

            #region Event
            switch (e.type)
            {
                case EventType.MouseUp:
                    SceneView.RepaintAll();
                    break;
            }
            #endregion

            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (VAW.VA.IsHuman && guiAnimatorIkVisible)
                {
                    guiAnimatorIkFoldout = GUILayout.Toggle(guiAnimatorIkFoldout, "Animator IK", EditorStyles.toolbarButton);
                }
                if (guiOriginalIkVisible)
                {
                    guiOriginalIkFoldout = GUILayout.Toggle(guiOriginalIkFoldout, "Original IK", EditorStyles.toolbarButton);
                }
                if (VAW.VA.IsHuman && guiHumanoidVisible)
                {
                    guiHumanoidFoldout = GUILayout.Toggle(guiHumanoidFoldout, "Humanoid", EditorStyles.toolbarButton);
                }
                if (guiSelectionVisible)
                {
                    EditorGUI.BeginChangeCheck();
                    guiSelectionFoldout = GUILayout.Toggle(guiSelectionFoldout, "Selection", EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (guiSelectionFoldout)
                            UpdateSelection();
                    }
                }
                if (guiHierarchyVisible)
                {
                    guiHierarchyFoldout = GUILayout.Toggle(guiHierarchyFoldout, "Hierarchy", EditorStyles.toolbarButton);
                }
                if (EditorGUILayout.DropdownButton(VAW.UEditorGUI.GetTitleSettingsIcon(), FocusType.Passive, VAW.GuiStyleIconButton, GUILayout.Width(19)))
                {
                    GenericMenu menu = new();
                    if (VAW.VA.IsHuman)
                        menu.AddItem(new GUIContent("Animator IK"), guiAnimatorIkVisible, () => { guiAnimatorIkVisible = !guiAnimatorIkVisible; guiAnimatorIkFoldout = guiAnimatorIkVisible; });
                    menu.AddItem(new GUIContent("Original IK"), guiOriginalIkVisible, () => { guiOriginalIkVisible = !guiOriginalIkVisible; guiOriginalIkFoldout = guiOriginalIkVisible; });
                    if (VAW.VA.IsHuman)
                        menu.AddItem(new GUIContent("Humanoid"), guiHumanoidVisible, () => { guiHumanoidVisible = !guiHumanoidVisible; guiHumanoidFoldout = guiHumanoidVisible; });
                    menu.AddItem(new GUIContent("Selection"), guiSelectionVisible, () => { guiSelectionVisible = !guiSelectionVisible; guiSelectionFoldout = guiSelectionVisible; });
                    menu.AddItem(new GUIContent("Hierarchy"), guiHierarchyVisible, () => { guiHierarchyVisible = !guiHierarchyVisible; guiHierarchyFoldout = guiHierarchyVisible; });
                    menu.ShowAsContext();
                }
            }
            EditorGUILayout.EndHorizontal();

            #region AnimatorIK
            if (VAW.VA.IsHuman && guiAnimatorIkFoldout && guiAnimatorIkVisible)
            {
                EditorGUILayout.BeginHorizontal();
                guiAnimatorIkFoldout = EditorGUILayout.Foldout(guiAnimatorIkFoldout, "Animator IK", true, VAW.GuiStyleBoldFoldout);
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), guiAnimatorIkHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        guiAnimatorIkHelp = !guiAnimatorIkHelp;
                    }
#if VERYANIMATION_ANIMATIONRIGGING
                    if (EditorGUILayout.DropdownButton(VAW.UEditorGUI.GetTitleSettingsIcon(), FocusType.Passive, VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        var rigEnable = VAW.VA.AnimationRigging.IsValid;
                        GenericMenu menu = new();
                        menu.AddItem(Language.GetContent(Language.Help.AnimatorIKAnimationRiggingEnable), rigEnable, () =>
                        {
                            Undo.RecordObject(VAW, "Animation Rigging Enable");
                            VAW.VA.AnimationRigging.Enable();
                        });
                        menu.AddItem(Language.GetContent(Language.Help.AnimatorIKAnimationRiggingDisable), !rigEnable, () =>
                        {
                            Undo.RecordObject(VAW, "Animation Rigging Disable");
                            VAW.VA.AnimationRigging.Disable();
                        });
                        menu.ShowAsContext();
                    }
#endif
                }
                EditorGUILayout.EndHorizontal();
                {
                    if (guiAnimatorIkHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpAnimatorIK), MessageType.Info);
                    }
                    VAW.VA.animatorIK.ControlGUI();
                }
            }
            #endregion

            #region OriginalIK
            if (guiOriginalIkFoldout && guiOriginalIkVisible)
            {
                EditorGUILayout.BeginHorizontal();
                guiOriginalIkFoldout = EditorGUILayout.Foldout(guiOriginalIkFoldout, "Original IK", true, VAW.GuiStyleBoldFoldout);
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), guiOriginalIkHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        guiOriginalIkHelp = !guiOriginalIkHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();
                {
                    if (guiOriginalIkHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpOriginalIK), MessageType.Info);
                    }
                    VAW.VA.originalIK.ControlGUI();
                }
            }
            #endregion

            #region Humanoid
            if (VAW.VA.IsHuman)
            {
                if (guiHumanoidFoldout && guiHumanoidVisible)
                {
                    if (e.type == EventType.Layout)
                    {
                        selectionGameObjectsHumanoidIndex.Clear();
                        if (VAW.VA.SelectionGameObjectsIndexOf(VAW.GameObject) >= 0)
                            selectionGameObjectsHumanoidIndex.Add((HumanBodyBones)(-1));
                        foreach (var hi in VAW.VA.SelectionGameObjectsHumanoidIndex())
                            selectionGameObjectsHumanoidIndex.Add(hi);
                    }
                    controlBoneList.Clear();
                    //
                    EditorGUILayout.BeginHorizontal();
                    guiHumanoidFoldout = EditorGUILayout.Foldout(guiHumanoidFoldout, "Humanoid", true, VAW.GuiStyleBoldFoldout);
                    {
                        EditorGUILayout.Space();
                        if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), guiHumanoidHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                        {
                            guiHumanoidHelp = !guiHumanoidHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    {
                        if (guiHumanoidHelp)
                        {
                            EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpHumanoid), MessageType.Info);
                        }

                        Rect backgroundRect;
                        {
                            backgroundRect = EditorGUILayout.GetControlRect();
                            backgroundRect.height = avatarBodyPartPicker.height;
                            GUI.Box(backgroundRect, "", guiStyleBackgroundBox);
                            GUILayout.Space(backgroundRect.height - 16);
                        }

                        var saveGUIColor = GUI.color;
                        if (CurrentHumanoidAvatarPartsMode == HumanoidAvatarPartsMode.Body)
                        {
                            #region Body
                            #region Root
                            GUI.color = VAW.VA.SelectionGameObjectsIndexOf(VAW.GameObject) < 0 ? GreenColor : BlueColor;
                            GUI.DrawTexture(backgroundRect, avatarRoot, ScaleMode.ScaleToFit);
                            #endregion
                            #region BackGround
                            GUI.color = GlayColor;
                            GUI.DrawTexture(backgroundRect, avatarBodysilhouette, ScaleMode.ScaleToFit);
                            GUI.color = GreenColor;
                            GUI.DrawTexture(backgroundRect, avatarHead, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarTorso, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarLeftArm, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarLeftFingers, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarLeftLeg, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarRightArm, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarRightFingers, ScaleMode.ScaleToFit);
                            GUI.DrawTexture(backgroundRect, avatarRightLeg, ScaleMode.ScaleToFit);
                            #endregion
                            #region IK
                            {
                                Color GetIKTargetColor(AnimatorIKCore.IKTarget t)
                                {
                                    if (!VAW.VA.animatorIK.ikData[(int)t].enable)
                                        return GlayColor;
                                    else if (VAW.VA.animatorIK.ikTargetSelect != null && EditorCommon.ArrayContains(VAW.VA.animatorIK.ikTargetSelect, t))
                                        return BlueColor;
                                    else
                                        return GreenColor;
                                }
                                void IKTragetToggle(AnimatorIKCore.IKTarget t, Vector2 position)
                                {
                                    Rect rect = new(position, new Vector2(GUI.skin.toggle.border.horizontal, GUI.skin.toggle.border.vertical));
                                    GUI.color = Color.white;
                                    EditorGUI.BeginChangeCheck();
                                    EditorGUI.Toggle(rect, VAW.VA.animatorIK.ikData[(int)t].enable);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        VAW.VA.animatorIK.ChangeTargetIK(t);
                                    }
                                }
                                {
                                    GUI.color = GetIKTargetColor(AnimatorIKCore.IKTarget.LeftFoot);
                                    GUI.DrawTexture(backgroundRect, avatarLeftFeetIk, ScaleMode.ScaleToFit);
                                    IKTragetToggle(AnimatorIKCore.IKTarget.LeftFoot, new Vector2(backgroundRect.center.x + 86, backgroundRect.y + 355));
                                }
                                {
                                    GUI.color = GetIKTargetColor(AnimatorIKCore.IKTarget.RightFoot);
                                    GUI.DrawTexture(backgroundRect, avatarRightFeetIk, ScaleMode.ScaleToFit);
                                    IKTragetToggle(AnimatorIKCore.IKTarget.RightFoot, new Vector2(backgroundRect.center.x - 100, backgroundRect.y + 355));
                                }
                                {
                                    GUI.color = GetIKTargetColor(AnimatorIKCore.IKTarget.LeftHand);
                                    GUI.DrawTexture(backgroundRect, avatarLeftFingersIk, ScaleMode.ScaleToFit);
                                    IKTragetToggle(AnimatorIKCore.IKTarget.LeftHand, new Vector2(backgroundRect.center.x + 76, backgroundRect.y + 220));
                                }
                                {
                                    GUI.color = GetIKTargetColor(AnimatorIKCore.IKTarget.RightHand);
                                    GUI.DrawTexture(backgroundRect, avatarRightFingersIk, ScaleMode.ScaleToFit);
                                    IKTragetToggle(AnimatorIKCore.IKTarget.RightHand, new Vector2(backgroundRect.center.x - 90, backgroundRect.y + 220));
                                }
                                {
                                    GUI.color = GetIKTargetColor(AnimatorIKCore.IKTarget.Head);
                                    var rect = backgroundRect;
                                    rect.center -= new Vector2(-12f, 212f);
                                    GUI.DrawTexture(rect, avatarRightFingersIk, ScaleMode.ScaleToFit);
                                    IKTragetToggle(AnimatorIKCore.IKTarget.Head, new Vector2(backgroundRect.center.x - 79, backgroundRect.y + 8));
                                }
                            }
                            #endregion
                            #region Bone
                            {
                                var position = backgroundRect.center;
                                position.y = backgroundRect.y - 19;
                                //HumanoidControlBoneGUI(new Vector2(position.x, position.y + 191), HumanBodyBones.Hips);
                                HumanoidControlBoneGUI(new Vector2(position.x, position.y + 170), HumanBodyBones.Spine);
                                HumanoidControlBoneGUI(new Vector2(position.x, position.y + 140), HumanBodyBones.Chest);
                                HumanoidControlBoneGUI(new Vector2(position.x, position.y + 112), HumanBodyBones.UpperChest);
                                HumanoidControlBoneGUI(new Vector2(position.x, position.y + 82), HumanBodyBones.Neck);
                                HumanoidControlBoneGUI(new Vector2(position.x, position.y + 63), HumanBodyBones.Head);
                                HumanoidControlBoneGUI(new Vector2(position.x + 12, position.y + 93), HumanBodyBones.LeftShoulder);
                                HumanoidControlBoneGUI(new Vector2(position.x - 12, position.y + 93), HumanBodyBones.RightShoulder);
                                HumanoidControlBoneGUI(new Vector2(position.x + 27, position.y + 99), HumanBodyBones.LeftUpperArm);
                                HumanoidControlBoneGUI(new Vector2(position.x + 43, position.y + 150), HumanBodyBones.LeftLowerArm);
                                HumanoidControlBoneGUI(new Vector2(position.x + 59, position.y + 201), HumanBodyBones.LeftHand);
                                HumanoidControlBoneGUI(new Vector2(position.x - 27, position.y + 99), HumanBodyBones.RightUpperArm);
                                HumanoidControlBoneGUI(new Vector2(position.x - 43, position.y + 150), HumanBodyBones.RightLowerArm);
                                HumanoidControlBoneGUI(new Vector2(position.x - 59, position.y + 201), HumanBodyBones.RightHand);
                                HumanoidControlBoneGUI(new Vector2(position.x + 14, position.y + 205), HumanBodyBones.LeftUpperLeg);
                                HumanoidControlBoneGUI(new Vector2(position.x + 18, position.y + 282), HumanBodyBones.LeftLowerLeg);
                                HumanoidControlBoneGUI(new Vector2(position.x + 20, position.y + 358), HumanBodyBones.LeftFoot);
                                HumanoidControlBoneGUI(new Vector2(position.x - 14, position.y + 205), HumanBodyBones.RightUpperLeg);
                                HumanoidControlBoneGUI(new Vector2(position.x - 18, position.y + 282), HumanBodyBones.RightLowerLeg);
                                HumanoidControlBoneGUI(new Vector2(position.x - 20, position.y + 358), HumanBodyBones.RightFoot);
                                HumanoidControlBoneGUI(new Vector2(position.x + 23, position.y + 375), HumanBodyBones.LeftToes);
                                HumanoidControlBoneGUI(new Vector2(position.x - 23, position.y + 375), HumanBodyBones.RightToes);

                                controlBoneList.Add((HumanBodyBones)(-1), new Vector2(position.x, position.y + 372));   //Root
                            }
                            #endregion
                            #endregion
                        }
                        else if (CurrentHumanoidAvatarPartsMode == HumanoidAvatarPartsMode.Head)
                        {
                            #region Head
                            #region BackGround
                            GUI.color = GlayColor;
                            GUI.DrawTexture(backgroundRect, avatarHeadzoomsilhouette, ScaleMode.ScaleToFit);
                            //base
                            {
                                GUI.color = GreenColor;
                                GUI.DrawTexture(backgroundRect, avatarHeadZoom, ScaleMode.ScaleToFit);
                            }
                            #endregion
                            #region Bone
                            {
                                var position = backgroundRect.center;
                                position.y = backgroundRect.y - 19;
                                HumanoidControlBoneGUI(new Vector2(position.x - 14, position.y + 263), HumanBodyBones.Head);
                                HumanoidControlBoneGUI(new Vector2(position.x - 18, position.y + 324), HumanBodyBones.Neck);
                                HumanoidControlBoneGUI(new Vector2(position.x + 56, position.y + 176), HumanBodyBones.LeftEye);
                                HumanoidControlBoneGUI(new Vector2(position.x + 13, position.y + 176), HumanBodyBones.RightEye);
                                HumanoidControlBoneGUI(new Vector2(position.x + 40, position.y + 282), HumanBodyBones.Jaw);
                            }
                            #endregion
                            #endregion
                        }
                        else if (CurrentHumanoidAvatarPartsMode == HumanoidAvatarPartsMode.LeftHand)
                        {
                            #region LeftHand
                            #region BackGround
                            GUI.color = GlayColor;
                            GUI.DrawTexture(backgroundRect, avatarLefthandzoomsilhouette, ScaleMode.ScaleToFit);
                            //base
                            GUI.color = VAW.VA.HumanoidHasLeftHand ? GreenColor : GlayColor;
                            GUI.DrawTexture(backgroundRect, avatarLeftHandZoom, ScaleMode.ScaleToFit);
                            #endregion
                            #region Bone
                            {
                                var position = backgroundRect.center;
                                position.y = backgroundRect.y - 19;
                                HumanoidControlBoneGUI(new Vector2(position.x - 42, position.y + 186), HumanBodyBones.LeftThumbProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 20, position.y + 162), HumanBodyBones.LeftThumbIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x - 4, position.y + 144), HumanBodyBones.LeftThumbDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 22, position.y + 186), HumanBodyBones.LeftIndexProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 54, position.y + 179), HumanBodyBones.LeftIndexIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x + 78, position.y + 175), HumanBodyBones.LeftIndexDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 26, position.y + 207), HumanBodyBones.LeftMiddleProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 62, position.y + 207), HumanBodyBones.LeftMiddleIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x + 88, position.y + 207), HumanBodyBones.LeftMiddleDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 19, position.y + 229), HumanBodyBones.LeftRingProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 54, position.y + 230), HumanBodyBones.LeftRingIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x + 79, position.y + 232), HumanBodyBones.LeftRingDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 10, position.y + 250), HumanBodyBones.LeftLittleProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 35, position.y + 251), HumanBodyBones.LeftLittleIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x + 54, position.y + 253), HumanBodyBones.LeftLittleDistal);
                            }
                            #endregion
                            #endregion
                        }
                        else if (CurrentHumanoidAvatarPartsMode == HumanoidAvatarPartsMode.RightHand)
                        {
                            #region RightHand
                            #region BackGround
                            GUI.color = GlayColor;
                            GUI.DrawTexture(backgroundRect, avatarRighthandzoomsilhouette, ScaleMode.ScaleToFit);
                            //base
                            GUI.color = VAW.VA.HumanoidHasRightHand ? GreenColor : GlayColor;
                            GUI.DrawTexture(backgroundRect, avatarRightHandZoom, ScaleMode.ScaleToFit);
                            #endregion
                            #region Bone
                            {
                                var position = backgroundRect.center;
                                position.y = backgroundRect.y - 19;
                                HumanoidControlBoneGUI(new Vector2(position.x + 42, position.y + 186), HumanBodyBones.RightThumbProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x + 20, position.y + 162), HumanBodyBones.RightThumbIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x + 4, position.y + 144), HumanBodyBones.RightThumbDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 22, position.y + 186), HumanBodyBones.RightIndexProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 54, position.y + 179), HumanBodyBones.RightIndexIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x - 78, position.y + 175), HumanBodyBones.RightIndexDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 26, position.y + 207), HumanBodyBones.RightMiddleProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 62, position.y + 207), HumanBodyBones.RightMiddleIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x - 88, position.y + 207), HumanBodyBones.RightMiddleDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 19, position.y + 229), HumanBodyBones.RightRingProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 54, position.y + 230), HumanBodyBones.RightRingIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x - 79, position.y + 232), HumanBodyBones.RightRingDistal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 10, position.y + 250), HumanBodyBones.RightLittleProximal);
                                HumanoidControlBoneGUI(new Vector2(position.x - 35, position.y + 251), HumanBodyBones.RightLittleIntermediate);
                                HumanoidControlBoneGUI(new Vector2(position.x - 54, position.y + 253), HumanBodyBones.RightLittleDistal);
                            }
                            #endregion
                            #endregion
                        }
                        GUI.color = saveGUIColor;

                        #region Toolbar
                        {
                            Rect rect = backgroundRect;
                            {
                                rect.position = new Vector2(backgroundRect.position.x + 5, backgroundRect.position.y + 308);
                                rect.width = 70;
                                rect.height = 64;
                            }
                            CurrentHumanoidAvatarPartsMode = (HumanoidAvatarPartsMode)GUI.SelectionGrid(rect, (int)CurrentHumanoidAvatarPartsMode, HumanoidAvatarPartsModeStrings, 1, guiStyleVerticalToolbar);
                        }
                        #endregion

                        #region Event
                        switch (e.type)
                        {
                            case EventType.MouseDown:
                                if (e.button == 0)
                                {
                                    selectionRect.Reset();
                                    selectionAvatarMaskBodyPart = (AvatarMaskBodyPart)(-1);
                                    if (GUIUtility.hotControl == 0 && backgroundRect.Contains(e.mousePosition))
                                    {
                                        var pos = e.mousePosition - backgroundRect.min;
                                        pos.x -= (backgroundRect.width - avatarBodyPartPicker.width) / 2f;
                                        if (CurrentHumanoidAvatarPartsMode == HumanoidAvatarPartsMode.Body &&
                                            pos.x >= 0f && pos.x < avatarBodyPartPicker.width &&
                                            pos.y >= 0f && pos.x < avatarBodyPartPicker.height)
                                        {
                                            var pixel = avatarBodyPartPicker.GetPixel((int)pos.x, avatarBodyPartPicker.height - (int)pos.y);
                                            selectionAvatarMaskBodyPart = (AvatarMaskBodyPart)EditorCommon.ArrayIndexOf(maskBodyPartPicker, pixel);
                                            switch (selectionAvatarMaskBodyPart)
                                            {
                                                case AvatarMaskBodyPart.Root:
                                                    VAW.VA.SelectGameObjectPlusKey(VAW.GameObject);
                                                    break;
                                                case AvatarMaskBodyPart.LeftFootIK:
                                                    if (VAW.VA.animatorIK.ikData[(int)AnimatorIKCore.IKTarget.LeftFoot].enable)
                                                        VAW.VA.SelectAnimatorIKTargetPlusKey(AnimatorIKCore.IKTarget.LeftFoot);
                                                    break;
                                                case AvatarMaskBodyPart.RightFootIK:
                                                    if (VAW.VA.animatorIK.ikData[(int)AnimatorIKCore.IKTarget.RightFoot].enable)
                                                        VAW.VA.SelectAnimatorIKTargetPlusKey(AnimatorIKCore.IKTarget.RightFoot);
                                                    break;
                                                case AvatarMaskBodyPart.LeftHandIK:
                                                    if (VAW.VA.animatorIK.ikData[(int)AnimatorIKCore.IKTarget.LeftHand].enable)
                                                        VAW.VA.SelectAnimatorIKTargetPlusKey(AnimatorIKCore.IKTarget.LeftHand);
                                                    break;
                                                case AvatarMaskBodyPart.RightHandIK:
                                                    if (VAW.VA.animatorIK.ikData[(int)AnimatorIKCore.IKTarget.RightHand].enable)
                                                        VAW.VA.SelectAnimatorIKTargetPlusKey(AnimatorIKCore.IKTarget.RightHand);
                                                    break;
                                                case AvatarMaskBodyPart.LastBodyPart:
                                                    if (VAW.VA.animatorIK.ikData[(int)AnimatorIKCore.IKTarget.Head].enable)
                                                        VAW.VA.SelectAnimatorIKTargetPlusKey(AnimatorIKCore.IKTarget.Head);
                                                    break;
                                                default:
                                                    VAW.VA.SelectGameObject(null);
                                                    break;
                                            }
                                        }
                                        {
                                            selectionRect.SetStart(e.mousePosition);
                                            if (Shortcuts.IsKeyControl(e) || e.shift)
                                            {
                                                selectionRect.beforeSelection = selectionGameObjectsHumanoidIndex.ToArray();
                                            }
                                        }
                                        e.Use();
                                        repaint = true;
                                    }
                                }
                                break;
                            case EventType.MouseUp:
                                if (e.button == 0)
                                {
                                    if (backgroundRect.Contains(e.mousePosition))
                                    {
                                        if (selectionAvatarMaskBodyPart < 0 && (!selectionRect.Enable || selectionRect.Distance <= 0f) && selectionRect.beforeSelection == null)
                                        {
                                            VAW.VA.SelectGameObject(null);
                                        }
                                        selectionRect.Reset();
                                        selectionAvatarMaskBodyPart = (AvatarMaskBodyPart)(-1);
                                        repaint = true;
                                    }
                                    else if (selectionRect.Enable)
                                    {
                                        selectionRect.Reset();
                                        selectionAvatarMaskBodyPart = (AvatarMaskBodyPart)(-1);
                                        repaint = true;
                                    }
                                }
                                break;
                            case EventType.MouseDrag:
                                if (e.button == 0)
                                {
                                    if (selectionRect.Enable)
                                    {
                                        if (GUIUtility.hotControl == 0)
                                        {
                                            {
                                                var rect = position;
                                                rect.position -= rect.position;
                                                if (rect.Contains(e.mousePosition - windowScrollPosition))
                                                {
                                                    var pos = e.mousePosition;
                                                    pos.x = Mathf.Clamp(pos.x, backgroundRect.xMin, backgroundRect.xMax);
                                                    pos.y = Mathf.Clamp(pos.y, backgroundRect.yMin, backgroundRect.yMax);
                                                    selectionRect.SetEnd(pos);
                                                }
                                                else
                                                {
                                                    selectionRect.Reset();
                                                }
                                            }
                                            #region Selection
                                            if (selectionRect.Enable)
                                            {
                                                List<HumanBodyBones> oldCalcList = new(selectionRect.calcList);
                                                selectionRect.calcList.Clear();
                                                var rect = selectionRect.Rect;
                                                foreach (var pair in controlBoneList)
                                                {
                                                    if (rect.Contains(pair.Value))
                                                        selectionRect.calcList.Add(pair.Key);
                                                }
                                                if ((Shortcuts.IsKeyControl(e) || e.shift) && selectionRect.beforeSelection != null)
                                                {
                                                    if (e.shift)
                                                    {
                                                        foreach (var hi in selectionRect.beforeSelection)
                                                        {
                                                            if (!selectionRect.calcList.Contains(hi))
                                                                selectionRect.calcList.Add(hi);
                                                        }
                                                    }
                                                    else if (Shortcuts.IsKeyControl(e))
                                                    {
                                                        foreach (var hi in selectionRect.beforeSelection)
                                                        {
                                                            if (!controlBoneList.ContainsKey(hi)) continue;
                                                            if (!rect.Contains(controlBoneList[hi]))
                                                            {
                                                                if (!selectionRect.calcList.Contains(hi))
                                                                    selectionRect.calcList.Add(hi);
                                                            }
                                                            else
                                                            {
                                                                selectionRect.calcList.Remove(hi);
                                                            }
                                                        }
                                                    }
                                                }
                                                bool selectionChange = oldCalcList.Count != selectionRect.calcList.Count;
                                                if (!selectionChange)
                                                {
                                                    for (int i = 0; i < selectionRect.calcList.Count; i++)
                                                    {
                                                        if (oldCalcList[i] != selectionRect.calcList[i])
                                                        {
                                                            selectionChange = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                if (selectionChange)
                                                {
                                                    VAW.VA.SelectHumanoidBones(selectionRect.calcList.ToArray());
                                                    ForceSelectionChange();
                                                }
                                            }
                                            #endregion
                                            e.Use();
                                        }
                                        else
                                        {
                                            selectionRect.Reset();
                                        }
                                    }
                                    repaint = true;
                                }
                                break;
                        }

                        #region SelectionRect
                        if (selectionRect.Enable && selectionRect.Rect.width > 0f && selectionRect.Rect.height > 0f)
                        {
                            GUI.Box(selectionRect.Rect, "", "SelectionRect");
                        }
                        #endregion
                        #endregion
                    }
                }
            }
            #endregion

            #region Selection
            if (guiSelectionFoldout && guiSelectionVisible)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                guiSelectionFoldout = EditorGUILayout.Foldout(guiSelectionFoldout, "Selection", true, VAW.GuiStyleBoldFoldout);
                if (EditorGUI.EndChangeCheck())
                {
                    if (guiSelectionFoldout)
                        UpdateSelection();
                }
                EditorGUILayout.Space();
                {
                    EditorGUI.BeginChangeCheck();
                    var type = (SelectionType)GUILayout.Toolbar((int)selectionType, SelectionTypeString, EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        selectionType = type;
                        EditorPrefs.SetInt("VeryAnimation_Control_SelectionType", (int)selectionType);
                        switch (selectionType)
                        {
                            case SelectionType.List:
                                updateSelectionList = true;
                                break;
                            case SelectionType.Popup:
                                updateSelectionPopup = true;
                                break;
                        }
                    }
                }
                EditorGUILayout.Space();
                {
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), guiSelectionHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        guiSelectionHelp = !guiSelectionHelp;
                    }
                }
                EditorGUILayout.EndHorizontal();
                {
                    if (guiSelectionHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpSelectionSet), MessageType.Info);
                    }
                    else
                    {
                        GUILayout.Space(2f);
                    }
                    SelectionGUI();
                }
            }
            #endregion

            #region Hierarchy
            if (guiHierarchyFoldout && guiHierarchyVisible)
            {
                EditorGUILayout.BeginHorizontal();
                guiHierarchyFoldout = EditorGUILayout.Foldout(guiHierarchyFoldout, "Hierarchy", true, VAW.GuiStyleBoldFoldout);
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(VAW.UEditorGUI.GetHelpIcon(), guiHierarchyHelp ? VAW.GuiStyleIconActiveButton : VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        guiHierarchyHelp = !guiHierarchyHelp;
                    }
                    if (EditorGUILayout.DropdownButton(VAW.UEditorGUI.GetTitleSettingsIcon(), FocusType.Passive, VAW.GuiStyleIconButton, GUILayout.Width(19)))
                    {
                        GenericMenu menu = new();
                        menu.AddItem(Language.GetContent(Language.Help.HierarchyWriteLock), hierarchyWriteLock, () =>
                        {
                            hierarchyWriteLock = !hierarchyWriteLock;
                            EditorPrefs.SetBool("VeryAnimation_Control_HierarchyWriteLock", hierarchyWriteLock);
                            hierarchyTreeView.Reload();
                        });
                        menu.AddSeparator(string.Empty);
                        menu.AddItem(Language.GetContent(Language.Help.HierarchyMirrorObject), hierarchyMirrorObject, () =>
                        {
                            hierarchyMirrorObject = !hierarchyMirrorObject;
                            EditorPrefs.SetBool("VeryAnimation_Control_HierarchyMirrorObject", hierarchyMirrorObject);
                        });
                        menu.AddItem(Language.GetContent(Language.Help.HierarchyMirrorAutomap), false, () =>
                        {
                            VAW.VA.BonesMirrorAutomap();
                            InternalEditorUtility.RepaintAllViews();
                        });
                        menu.AddItem(Language.GetContent(Language.Help.HierarchyMirrorClear), false, () =>
                        {
                            VAW.VA.BonesMirrorInitialize();
                            InternalEditorUtility.RepaintAllViews();
                        });
                        if (VAW.VA.IsHuman)
                        {
                            menu.AddSeparator(string.Empty);
                            menu.AddItem(Language.GetContent(Language.Help.HierarchyHumanoidName), hierarchyHumanoidName, () =>
                            {
                                hierarchyHumanoidName = !hierarchyHumanoidName;
                                EditorPrefs.SetBool("VeryAnimation_Control_HierarchyHumanoidName", hierarchyHumanoidName);
                                UpdateHierarchyTree();
                            });
                        }
                        menu.ShowAsContext();
                    }
                }
                EditorGUILayout.EndHorizontal();
                {
                    if (guiHierarchyHelp)
                    {
                        EditorGUILayout.HelpBox(Language.GetText(Language.Help.HelpHierarchy), MessageType.Info);
                    }
                    else
                    {
                        GUILayout.Space(2f);
                    }
                    HierarchyToolBarGUI();
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        hierarchyTreeView.searchString = hierarchyTreeSearchField.OnToolbarGUI(hierarchyTreeView.searchString);
                        EditorGUILayout.LabelField(new GUIContent(string.Format("{0} / {1}", VAW.VA.BoneShowCount, VAW.VA.boneShowFlags.Length), "Show / All"), VAW.GuiStyleMiddleRightMiniLabel, GUILayout.Width(60f));
                        EditorGUILayout.EndHorizontal();
                    }
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 0);
                        rect.height = Math.Max(position.height - rect.y, 0);
                        hierarchyTreeView.OnGUI(rect);
                    }
                }
            }
            #endregion

            EditorGUILayout.EndScrollView();

            if (repaint)
            {
                Repaint();
            }

#if Enable_Profiler
            Profiler.EndSample();
#endif
        }

        private void GUIStyleReady()
        {
            guiStyleBackgroundBox ??= new GUIStyle("CurveEditorBackground");
            guiStyleVerticalToolbar ??= new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 0, 0),
                fontSize = 9
            };
            if (guiStyleBoneButton == null)
            {
                guiStyleBoneButton = new GUIStyle(GUI.skin.button)
                {
                    border = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    overflow = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0)
                };
                guiStyleBoneButton.active = guiStyleBoneButton.normal;
            }
        }
        private void GUIStyleClear()
        {
            guiStyleBackgroundBox = null;
            guiStyleVerticalToolbar = null;
            guiStyleBoneButton = null;
        }

        private void SelectionGUI()
        {
            var e = Event.current;

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                if (selectionType == SelectionType.List)
                {
                    #region List
                    if (Event.current.type == EventType.Layout && updateSelectionList)
                    {
                        #region SelectionSet
                        selectionSetList = null;
                        if (VAW.VA.selectionSetList != null)
                        {
                            selectionSetList = new ReorderableList(VAW.VA.selectionSetList, typeof(VeryAnimationSaveSettings.SelectionData), true, true, true, true);
                            selectionSetList.drawHeaderCallback = rect =>
                            {
                                float x = rect.x;
                                {
                                    const float ButtonWidth = 100f;
                                    #region Add
                                    {
                                        var r = rect;
                                        r.width = ButtonWidth;
                                        if (GUI.Button(r, Language.GetContent(Language.Help.SelectionSetTemplate), EditorStyles.toolbarDropDown))
                                        {
                                            var selectionTemplates = new Dictionary<string, string>();
                                            {
                                                var guids = AssetDatabase.FindAssets("t:selectiontemplate");
                                                for (int i = 0; i < guids.Length; i++)
                                                {
                                                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                                                    var name = path["Assets/".Length..];
                                                    selectionTemplates.Add(name, path);
                                                }
                                            }

                                            var menu = new GenericMenu();
                                            {
                                                var enu = selectionTemplates.GetEnumerator();
                                                while (enu.MoveNext())
                                                {
                                                    var value = enu.Current.Value;
                                                    menu.AddItem(new GUIContent(enu.Current.Key), false, () =>
                                                    {
                                                        var selectionTemplate = AssetDatabase.LoadAssetAtPath<SelectionTemplate>(value);
                                                        if (selectionTemplate != null)
                                                        {
                                                            Undo.RecordObject(VAW, "Template Selection");
                                                            VAW.VA.selectionSetList.AddRange(VAW.VA.LoadSelectionSaveSettings(selectionTemplate.selectionData));
                                                            updateSelectionList = true;
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
                                            Undo.RecordObject(VAW, "Clear Selection Set");
                                            VAW.VA.selectionSetList.Clear();
                                            selectionSetList.index = -1;
                                            updateSelectionList = true;
                                        }
                                    }
                                    #endregion
                                    #region Save as
                                    {
                                        var r = rect;
                                        r.width = ButtonWidth;
                                        r.x = rect.xMax - r.width;
                                        if (GUI.Button(r, Language.GetContent(Language.Help.SelectionSetSaveAs), EditorStyles.toolbarButton))
                                        {
                                            string path = EditorUtility.SaveFilePanel("Save as Selection Template", VAE.TemplateSaveDefaultDirectory, string.Format("{0}_Selection.asset", VAW.GameObject.name), "asset");
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
                                                    var selectionTemplate = ScriptableObject.CreateInstance<SelectionTemplate>();
                                                    {
                                                        selectionTemplate.selectionData = VAW.VA.SaveSelectionSaveSettings();
                                                    }
                                                    try
                                                    {
                                                        VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
                                                        AssetDatabase.CreateAsset(selectionTemplate, path);
                                                    }
                                                    finally
                                                    {
                                                        VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
                                                    }
                                                    Focus();
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            };
                            selectionSetList.drawElementCallback = (rect, index, isActive, isFocused) =>
                            {
                                if (index >= VAW.VA.selectionSetList.Count)
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
                                    if (index == selectionSetList.index)
                                    {
                                        EditorGUI.BeginChangeCheck();
                                        var text = EditorGUI.TextField(r, VAW.VA.selectionSetList[index].name);
                                        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(text))
                                        {
                                            Undo.RecordObject(VAW, "Change Selection Set");
                                            VAW.VA.selectionSetList[index].name = text;
                                        }
                                    }
                                    else
                                    {
                                        EditorGUI.LabelField(r, VAW.VA.selectionSetList[index].name);
                                    }
                                }
                                {
                                    const float Rate = 0.2f;
                                    var r = rect;
                                    r.width = rect.width * Rate;
                                    r.x = rect.xMax - r.width;
                                    r.y += 2;
                                    r.height -= 4;
                                    EditorGUI.LabelField(r, VAW.VA.selectionSetList[index].Count.ToString(), VAW.GuiStyleCenterAlignLabel);
                                }
                            };
                            selectionSetList.onSelectCallback = list =>
                            {
                                if (list.index < 0 || list.index >= VAW.VA.selectionSetList.Count)
                                    return;
                                if (VAW.VA.selectionSetList[list.index].bones.Length > 0)
                                    Selection.activeGameObject = VAW.VA.selectionSetList[list.index].bones[0];
                                VAW.VA.SelectGameObjects(VAW.VA.selectionSetList[list.index].bones, VAW.VA.selectionSetList[list.index].virtualBones);
                                InternalEditorUtility.RepaintAllViews();
                            };
                            selectionSetList.onCanAddCallback = list =>
                            {
                                return (VAW.VA.SelectionGameObjects != null && VAW.VA.SelectionGameObjects.Count > 0) || (VAW.VA.SelectionHumanVirtualBones != null && VAW.VA.SelectionHumanVirtualBones.Count > 0);
                            };
                            selectionSetList.onAddCallback = list =>
                            {
                                if ((VAW.VA.SelectionGameObjects == null || VAW.VA.SelectionGameObjects.Count <= 0) && (VAW.VA.SelectionHumanVirtualBones == null || VAW.VA.SelectionHumanVirtualBones.Count <= 0))
                                    return;

                                Undo.RecordObject(VAW, "Add Selection Set");
                                {
                                    var data = new VeryAnimationSaveSettings.SelectionData()
                                    {
                                        name = "New Set",
                                        bones = VAW.VA.SelectionGameObjects.ToArray(),
                                        virtualBones = VAW.VA.SelectionHumanVirtualBones != null ? VAW.VA.SelectionHumanVirtualBones.ToArray() : new HumanBodyBones[0],
                                    };
                                    if (VAW.VA.SelectionActiveGameObject != null)
                                        data.name = VAW.VA.SelectionActiveGameObject.name;
                                    else if (VAW.VA.SelectionHumanVirtualBones != null && VAW.VA.SelectionHumanVirtualBones.Count > 0)
                                        data.name = "Virtual" + VAW.VA.SelectionHumanVirtualBones[0].ToString();
                                    VAW.VA.selectionSetList.Add(data);
                                }
                                updateSelectionList = true;
                            };
                            selectionSetList.onRemoveCallback = list =>
                            {
                                if (list.index < 0 || list.index >= VAW.VA.selectionSetList.Count)
                                    return;
                                Undo.RecordObject(VAW, "Remove Selection Set");
                                VAW.VA.selectionSetList.RemoveAt(list.index);
                                list.index = -1;
                                updateSelectionList = true;
                            };
                        }
                        #endregion
                        updateSelectionList = false;
                        UpdateSelection();
                        Repaint();
                    }
                    selectionSetList?.DoLayoutList();
                    #endregion
                }
                else if (selectionType == SelectionType.Popup)
                {
                    #region Popup
                    if (Event.current.type == EventType.Layout && updateSelectionPopup)
                    {
                        selectionSetStrings = new string[VAW.VA.selectionSetList.Count];
                        for (int i = 0; i < VAW.VA.selectionSetList.Count; i++)
                        {
                            selectionSetStrings[i] = VAW.VA.selectionSetList[i].name;
                        }
                        updateSelectionPopup = false;
                        UpdateSelection();
                        Repaint();
                    }
                    if (selectionSetStrings != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        selectionSetIndex = EditorGUILayout.Popup("Selection Set", selectionSetIndex, selectionSetStrings);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (selectionSetIndex >= 0 && selectionSetIndex < VAW.VA.selectionSetList.Count)
                            {
                                if (VAW.VA.selectionSetList[selectionSetIndex].bones.Length > 0)
                                    Selection.activeGameObject = VAW.VA.selectionSetList[selectionSetIndex].bones[0];
                                VAW.VA.SelectGameObjects(VAW.VA.selectionSetList[selectionSetIndex].bones, VAW.VA.selectionSetList[selectionSetIndex].virtualBones);
                                InternalEditorUtility.RepaintAllViews();
                            }
                        }
                    }
                    #endregion
                }

                #region Move select
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(VAW.VA.SelectionActiveBone < 0);
                    EditorGUILayout.PrefixLabel(Language.GetContent(Language.Help.MoveSelect));
                    if (GUILayout.Button("Upper"))
                    {
                        #region Upper
                        HashSet<GameObject> selectBones;
                        if (Shortcuts.IsKeyControl(e) || e.shift)
                            selectBones = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                        else
                            selectBones = new HashSet<GameObject>();
                        foreach (var boneIndex in VAW.VA.SelectionBones)
                        {
                            if (VAW.VA.ParentBoneIndexes[boneIndex] >= 0)
                                selectBones.Add(VAW.VA.Bones[VAW.VA.ParentBoneIndexes[boneIndex]]);
                        }
                        if (VAW.VA.SelectionActiveGameObject != null)
                        {
                            GameObject activeGo = null;
                            if (selectBones.Contains(VAW.VA.SelectionActiveGameObject))
                            {
                                activeGo = VAW.VA.SelectionActiveGameObject;
                            }
                            else
                            {
                                var pt = VAW.VA.SelectionActiveGameObject.transform.parent;
                                if (pt != null && selectBones.Contains(pt.gameObject))
                                {
                                    activeGo = pt.gameObject;
                                }
                            }
                            if (activeGo != null)
                            {
                                Selection.activeGameObject = activeGo;
                            }
                        }
                        VAW.VA.SelectGameObjects(selectBones.ToArray());
                        #endregion
                    }
                    if (GUILayout.Button("Lower"))
                    {
                        #region Lower
                        HashSet<GameObject> selectBones;
                        if (Shortcuts.IsKeyControl(e) || e.shift)
                            selectBones = new HashSet<GameObject>(VAW.VA.SelectionGameObjects);
                        else
                            selectBones = new HashSet<GameObject>();
                        foreach (var boneIndex in VAW.VA.SelectionBones)
                        {
                            for (int i = 0; i < VAW.VA.Bones.Length; i++)
                            {
                                if (boneIndex == VAW.VA.ParentBoneIndexes[i])
                                    selectBones.Add(VAW.VA.Bones[i]);
                            }
                        }
                        if (VAW.VA.SelectionActiveGameObject != null)
                        {
                            GameObject activeGo = null;
                            if (selectBones.Contains(VAW.VA.SelectionActiveGameObject))
                            {
                                activeGo = VAW.VA.SelectionActiveGameObject;
                            }
                            else
                            {
                                for (int i = 0; i < VAW.VA.SelectionActiveGameObject.transform.childCount; i++)
                                {
                                    var ct = VAW.VA.SelectionActiveGameObject.transform.GetChild(i);
                                    if (selectBones.Contains(ct.gameObject))
                                    {
                                        activeGo = ct.gameObject;
                                        break;
                                    }
                                }
                            }
                            if (activeGo != null)
                            {
                                Selection.activeGameObject = activeGo;
                            }
                        }
                        VAW.VA.SelectGameObjects(selectBones.ToArray());
                        #endregion
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            EditorGUILayout.EndVertical();
        }
        private void UpdateSelection()
        {
            selectionSetIndex = -1;
            if (VAW.VA.selectionSetList != null)
            {
                for (int i = 0; i < VAW.VA.selectionSetList.Count; i++)
                {
                    #region Bone
                    {
                        if ((VAW.VA.selectionSetList[i].bones != null ? VAW.VA.selectionSetList[i].bones.Length : 0) != (VAW.VA.SelectionGameObjects != null ? VAW.VA.SelectionGameObjects.Count : 0))
                            continue;
                        if (VAW.VA.selectionSetList[i].bones != null && VAW.VA.selectionSetList[i].bones.Length > 0)
                        {
                            if (VAW.VA.selectionSetList[i].bones[0] != VAW.VA.SelectionActiveGameObject)
                                continue;
                        }
                        if (VAW.VA.SelectionGameObjects != null)
                        {
                            bool contain = true;
                            foreach (var bone in VAW.VA.SelectionGameObjects)
                            {
                                if (!EditorCommon.ArrayContains(VAW.VA.selectionSetList[i].bones, bone))
                                {
                                    contain = false;
                                    break;
                                }
                            }
                            if (!contain) continue;
                        }
                    }
                    #endregion
                    #region VirtualBone
                    {
                        if ((VAW.VA.selectionSetList[i].virtualBones != null ? VAW.VA.selectionSetList[i].virtualBones.Length : 0) != (VAW.VA.SelectionHumanVirtualBones != null ? VAW.VA.SelectionHumanVirtualBones.Count : 0))
                            continue;
                        if (VAW.VA.SelectionHumanVirtualBones != null)
                        {
                            bool contain = true;
                            foreach (var bone in VAW.VA.SelectionHumanVirtualBones)
                            {
                                if (!EditorCommon.ArrayContains(VAW.VA.selectionSetList[i].virtualBones, bone))
                                {
                                    contain = false;
                                    break;
                                }
                            }
                            if (!contain) continue;
                        }
                    }
                    #endregion
                    selectionSetIndex = i;
                    break;
                }
            }
            if (selectionSetList != null)
                selectionSetList.index = selectionSetIndex;
            Repaint();
        }

        private void HierarchyToolBarGUI()
        {
            if (VAW.VA == null || VAW.VA.IsEditError) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                #region All
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = GUILayout.Toggle(hierarchyButtonAll, Language.GetContent(Language.Help.HierarchyToolbarAll), EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change bone show flag");
                        VAW.VA.ActionBoneShowFlagsAll((index) =>
                        {
                            VAW.VA.boneShowFlags[index] = flag;
                        });
                        VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
                #endregion
                #region Weight
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = GUILayout.Toggle(hierarchyButtonWeight, Language.GetContent(Language.Help.HierarchyToolbarWeight), EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change bone show flag");
                        VAW.VA.ActionBoneShowFlagsHaveWeight((index) =>
                        {
                            VAW.VA.boneShowFlags[index] = flag;
                        });
                        VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
                #endregion
                if (VAW.VA.IsHuman)
                {
                    #region Body
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(hierarchyButtonBody, Language.GetContent(Language.Help.HierarchyToolbarBody), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change bone show flag");
                            VAW.VA.ActionBoneShowFlagsHumanoidBody((index) =>
                            {
                                VAW.VA.boneShowFlags[index] = flag;
                            });
                            VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                            InternalEditorUtility.RepaintAllViews();
                        }
                    }
                    #endregion
                    #region Face
                    {
                        EditorGUI.BeginDisabledGroup(VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.LeftEye] == null &&
                                                        VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.RightEye] == null &&
                                                        VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Jaw] == null);
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(hierarchyButtonFace, Language.GetContent(Language.Help.HierarchyToolbarFace), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change bone show flag");
                            VAW.VA.ActionBoneShowFlagsHumanoidFace((index) =>
                            {
                                VAW.VA.boneShowFlags[index] = flag;
                            });
                            VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                            InternalEditorUtility.RepaintAllViews();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    #endregion
                    #region LeftHand
                    {
                        EditorGUI.BeginDisabledGroup(!VAW.VA.HumanoidHasLeftHand);
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(hierarchyButtonLeftHand, Language.GetContent(Language.Help.HierarchyToolbarLeftHand), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change bone show flag");
                            VAW.VA.ActionBoneShowFlagsHumanoidLeftHand((index) =>
                            {
                                VAW.VA.boneShowFlags[index] = flag;
                            });
                            VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                            InternalEditorUtility.RepaintAllViews();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    #endregion
                    #region RightHand
                    {
                        EditorGUI.BeginDisabledGroup(!VAW.VA.HumanoidHasRightHand);
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(hierarchyButtonRightHand, Language.GetContent(Language.Help.HierarchyToolbarRightHand), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change bone show flag");
                            VAW.VA.ActionBoneShowFlagsHumanoidRightHand((index) =>
                            {
                                VAW.VA.boneShowFlags[index] = flag;
                            });
                            VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                            InternalEditorUtility.RepaintAllViews();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    #endregion
                }
                else
                {
                    #region Renderer
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(hierarchyButtonRenderer, Language.GetContent(Language.Help.HierarchyToolbarRenderer), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change bone show flag");
                            VAW.VA.ActionBoneShowFlagsHaveRenderer((index) =>
                            {
                                VAW.VA.boneShowFlags[index] = flag;
                            });
                            VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                            InternalEditorUtility.RepaintAllViews();
                        }
                    }
                    #endregion
                    #region RendererParent
                    {
                        EditorGUI.BeginChangeCheck();
                        var flag = GUILayout.Toggle(hierarchyButtonRendererParent, Language.GetContent(Language.Help.HierarchyToolbarRendererParent), EditorStyles.toolbarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change bone show flag");
                            VAW.VA.ActionBoneShowFlagsHaveRendererParent((index) =>
                            {
                                VAW.VA.boneShowFlags[index] = flag;
                            });
                            VAW.VA.OnBoneShowFlagsUpdated.Invoke();
                            InternalEditorUtility.RepaintAllViews();
                        }
                    }
                    #endregion
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        private void HumanoidControlBoneGUI(Vector2 position, HumanBodyBones select)
        {
            if (VAW.VA.IsIKBone(select))
                return;

            var bone = VAW.VA.HumanoidBones[(int)select];
            if (bone == null && VeryAnimation.HumanVirtualBones[(int)select] == null)
                return;

            var saveGUIColor = GUI.color;
            GUI.color = GreenColor;

            var selected = (selectionGameObjectsHumanoidIndex != null && selectionGameObjectsHumanoidIndex.Contains(select));

            Texture2D frameTex = bone != null ? dotframe : dotframedotted;
            Rect rect = new(new Vector2(position.x - frameTex.width / 2f, position.y - frameTex.height / 2f), new Vector2(frameTex.width, frameTex.height));

            guiStyleBoneButton.normal.background = frameTex;
            guiStyleBoneButton.normal.scaledBackgrounds = null;
            guiStyleBoneButton.active.background = frameTex;
            guiStyleBoneButton.active.scaledBackgrounds = null;
            if (GUI.Button(rect, dotfill, guiStyleBoneButton))
            {
                if (bone != null)
                    VAW.VA.SelectGameObjectPlusKey(bone);
                else
                    VAW.VA.SelectVirtualBonePlusKey(select);
                ForceSelectionChange();
            }

            if (selected)
            {
                GUI.color = BlueColor;
                GUI.DrawTexture(rect, dotselection, ScaleMode.ScaleToFit);
            }

            GUI.color = saveGUIColor;

            controlBoneList.Add(select, position);
        }

        private void UndoRedoPerformed()
        {
            if (VAW.VA == null || VAW.VA.IsEditError) return;

            UpdateHierarchyFlags();
        }

        private void UpdateHierarchyFlags()
        {
            if (VAW.VA == null || VAW.VA.IsEditError) return;

            {
                hierarchyButtonAll = true;
                VAW.VA.ActionBoneShowFlagsAll((index) =>
                {
                    if (!VAW.VA.boneShowFlags[index])
                        hierarchyButtonAll = false;
                });
            }
            {
                hierarchyButtonWeight = true;
                VAW.VA.ActionBoneShowFlagsHaveWeight((index) =>
                {
                    if (!VAW.VA.boneShowFlags[index])
                        hierarchyButtonWeight = false;
                });
            }
            if (VAW.VA.IsHuman)
            {
                {
                    hierarchyButtonBody = true;
                    VAW.VA.ActionBoneShowFlagsHumanoidBody((index) =>
                    {
                        if (!VAW.VA.boneShowFlags[index])
                            hierarchyButtonBody = false;
                    });
                }
                {
                    hierarchyButtonFace = true;
                    VAW.VA.ActionBoneShowFlagsHumanoidFace((index) =>
                    {
                        if (!VAW.VA.boneShowFlags[index])
                            hierarchyButtonFace = false;
                    });
                }
                {
                    hierarchyButtonLeftHand = true;
                    VAW.VA.ActionBoneShowFlagsHumanoidLeftHand((index) =>
                    {
                        if (!VAW.VA.boneShowFlags[index])
                            hierarchyButtonLeftHand = false;
                    });
                }
                {
                    hierarchyButtonRightHand = true;
                    VAW.VA.ActionBoneShowFlagsHumanoidRightHand((index) =>
                    {
                        if (!VAW.VA.boneShowFlags[index])
                            hierarchyButtonRightHand = false;
                    });
                }
            }
            else
            {
                {
                    hierarchyButtonRenderer = true;
                    VAW.VA.ActionBoneShowFlagsHaveRenderer((index) =>
                    {
                        if (!VAW.VA.boneShowFlags[index])
                            hierarchyButtonRenderer = false;
                    });
                }
                {
                    hierarchyButtonRendererParent = true;
                    VAW.VA.ActionBoneShowFlagsHaveRendererParent((index) =>
                    {
                        if (!VAW.VA.boneShowFlags[index])
                            hierarchyButtonRendererParent = false;
                    });
                }
            }
        }

        public static void ForceRepaint()
        {
            if (instance == null) return;
            instance.Repaint();
        }

        public static void ForceSelectionChange()
        {
            if (instance == null) return;
            if (instance.guiSelectionFoldout)
            {
                instance.UpdateSelection();
            }
            ForceRepaint();
        }
    }
}
