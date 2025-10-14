using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;

#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace VeryAnimation
{
    [Serializable]
    internal class AnimatorIKCore
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        public enum IKTarget
        {
            None = -1,
            Head,
            LeftHand,
            RightHand,
            LeftFoot,
            RightFoot,
            Total,
        }
        public static readonly string[] IKTargetStrings =
        {
            "Head",
            "Left Hand",
            "Right Hand",
            "Left Foot",
            "Right Foot",
        };
        private static readonly IKTarget[] IKTargetMirror =
        {
            IKTarget.None,
            IKTarget.RightHand,
            IKTarget.LeftHand,
            IKTarget.RightFoot,
            IKTarget.LeftFoot,
        };
        private readonly Quaternion[] IKTargetSyncRotation =
        {
            Quaternion.identity,
            Quaternion.Euler(0, 90, 180),
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(90, 0, 90),
            Quaternion.Euler(90, 0, 90),
        };

        public static readonly IKTarget[] HumanBonesUpdateAnimatorIK =
        {
            IKTarget.Total, //Hips = 0,
            IKTarget.LeftFoot, //LeftUpperLeg = 1,
            IKTarget.RightFoot, //RightUpperLeg = 2,
            IKTarget.LeftFoot, //LeftLowerLeg = 3,
            IKTarget.RightFoot, //RightLowerLeg = 4,
            IKTarget.LeftFoot, //LeftFoot = 5,
            IKTarget.RightFoot, //RightFoot = 6,
            IKTarget.Total, //Spine = 7,
            IKTarget.Total, //Chest = 8,
            IKTarget.Head, //Neck = 9,
            IKTarget.Head, //Head = 10,
            IKTarget.LeftHand, //LeftShoulder = 11,
            IKTarget.RightHand, //RightShoulder = 12,
            IKTarget.LeftHand, //LeftUpperArm = 13,
            IKTarget.RightHand, //RightUpperArm = 14,
            IKTarget.LeftHand, //LeftLowerArm = 15,
            IKTarget.RightHand, //RightLowerArm = 16,
            IKTarget.LeftHand, //LeftHand = 17,
            IKTarget.RightHand, //RightHand = 18,
            IKTarget.None, //LeftToes = 19,
            IKTarget.None, //RightToes = 20,
            IKTarget.Head, //LeftEye = 21,
            IKTarget.Head, //RightEye = 22,
            IKTarget.None, //Jaw = 23,
            IKTarget.None, //LeftThumbProximal = 24,
            IKTarget.None, //LeftThumbIntermediate = 25,
            IKTarget.None, //LeftThumbDistal = 26,
            IKTarget.None, //LeftIndexProximal = 27,
            IKTarget.None, //LeftIndexIntermediate = 28,
            IKTarget.None, //LeftIndexDistal = 29,
            IKTarget.None, //LeftMiddleProximal = 30,
            IKTarget.None, //LeftMiddleIntermediate = 31,
            IKTarget.None, //LeftMiddleDistal = 32,
            IKTarget.None, //LeftRingProximal = 33,
            IKTarget.None, //LeftRingIntermediate = 34,
            IKTarget.None, //LeftRingDistal = 35,
            IKTarget.None, //LeftLittleProximal = 36,
            IKTarget.None, //LeftLittleIntermediate = 37,
            IKTarget.None, //LeftLittleDistal = 38,
            IKTarget.None, //RightThumbProximal = 39,
            IKTarget.None, //RightThumbIntermediate = 40,
            IKTarget.None, //RightThumbDistal = 41,
            IKTarget.None, //RightIndexProximal = 42,
            IKTarget.None, //RightIndexIntermediate = 43,
            IKTarget.None, //RightIndexDistal = 44,
            IKTarget.None, //RightMiddleProximal = 45,
            IKTarget.None, //RightMiddleIntermediate = 46,
            IKTarget.None, //RightMiddleDistal = 47,
            IKTarget.None, //RightRingProximal = 48,
            IKTarget.None, //RightRingIntermediate = 49,
            IKTarget.None, //RightRingDistal = 50,
            IKTarget.None, //RightLittleProximal = 51,
            IKTarget.None, //RightLittleIntermediate = 52,
            IKTarget.None, //RightLittleDistal = 53,
            IKTarget.Total, //UpperChest = 54,
        };

        public GUIContent[] IKSpaceTypeStrings = new GUIContent[(int)AnimatorIKData.SpaceType.Total];

        [Serializable]
        public class AnimatorIKData
        {
            private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

            public enum SpaceType
            {
                Global,
                Local,
                Parent,
                Total
            }
            public enum SyncType
            {
                Skeleton,
                SceneObject,
                HumanoidIK,
                AnimationRigging,
            }

            public bool enable;
            public bool autoRotation;
            public SpaceType spaceType;
            public GameObject parent;
            public Vector3 position;
            public Quaternion rotation;
            //Head
            public float headWeight = 1f;
            public float eyesWeight = 0f;
            //Arm
            public bool enableShoulder;
            public float shoulderSensitivityY = 0.8f;
            public float shoulderSensitivityZ = 0.8f;
            //Swivel
            public float swivelRotation;
            //Sync
            public SyncType defaultSyncType;
            //AnimationRigging
            [Flags]
            public enum WriteFlags : uint
            {
                AnimationRiggingTarget = (1 << 0),
                AnimationRiggingHint = (1 << 1),
            }
            public WriteFlags writeFlags = (WriteFlags)uint.MaxValue;

            public Vector3 WorldPosition
            {
                get
                {
                    var getpos = position;
                    switch (spaceType)
                    {
                        case SpaceType.Global:
                            break;
                        case SpaceType.Local:
                            if (parentBoneIndex >= 0 && VAW.VA.Bones[parentBoneIndex] != null)
                                getpos = VAW.VA.Bones[parentBoneIndex].transform.localToWorldMatrix.MultiplyPoint3x4(getpos);
                            break;
                        case SpaceType.Parent:
                            if (parent != null)
                                getpos = parent.transform.localToWorldMatrix.MultiplyPoint3x4(getpos);
                            break;
                        default:
                            Assert.IsTrue(false);
                            break;
                    }
                    return getpos;
                }
                set
                {
                    var setpos = value;
                    switch (spaceType)
                    {
                        case SpaceType.Global:
                            break;
                        case SpaceType.Local:
                            if (parentBoneIndex >= 0 && VAW.VA.Bones[parentBoneIndex] != null)
                                setpos = VAW.VA.Bones[parentBoneIndex].transform.worldToLocalMatrix.MultiplyPoint3x4(setpos);
                            break;
                        case SpaceType.Parent:
                            if (parent != null)
                                setpos = parent.transform.worldToLocalMatrix.MultiplyPoint3x4(setpos);
                            break;
                        default:
                            Assert.IsTrue(false);
                            break;
                    }
                    position = setpos;
                }
            }
            public Quaternion WorldRotation
            {
                get
                {
                    var getrot = rotation;
                    switch (spaceType)
                    {
                        case SpaceType.Global:
                            break;
                        case SpaceType.Local:
                            if (parentBoneIndex >= 0 && VAW.VA.Bones[parentBoneIndex] != null)
                                getrot = VAW.VA.Bones[parentBoneIndex].transform.rotation * getrot;
                            break;
                        case SpaceType.Parent:
                            if (parent != null)
                                getrot = parent.transform.rotation * getrot;
                            break;
                        default:
                            Assert.IsTrue(false);
                            break;
                    }
                    return getrot;
                }
                set
                {
                    var setrot = value;
                    {   //Handles error -> Quaternion To Matrix conversion failed because input Quaternion is invalid
                        setrot.ToAngleAxis(out float angle, out Vector3 axis);
                        setrot = Quaternion.AngleAxis(angle, axis);
                    }
                    switch (spaceType)
                    {
                        case SpaceType.Global:
                            break;
                        case SpaceType.Local:
                            if (parentBoneIndex >= 0 && VAW.VA.Bones[parentBoneIndex] != null)
                                setrot = Quaternion.Inverse(VAW.VA.Bones[parentBoneIndex].transform.rotation) * setrot;
                            break;
                        case SpaceType.Parent:
                            if (parent != null)
                                setrot = Quaternion.Inverse(parent.transform.rotation) * setrot;
                            break;
                        default:
                            Assert.IsTrue(false);
                            break;
                    }
                    rotation = setrot;
                }
            }

            public bool IsUpdate { get { return enable && updateIKtarget && !synchroIKtarget; } }

            [NonSerialized]
            public int rootBoneIndex;
            [NonSerialized]
            public int parentBoneIndex;
            [NonSerialized]
            public bool updateIKtarget;
            [NonSerialized]
            public bool synchroIKtarget;
            [NonSerialized]
            public GameObject[] syncHumanoidBones;
            [NonSerialized]
            public Vector3 swivelPosition;
            [NonSerialized]
            public Vector3 optionPosition;
            [NonSerialized]
            public Quaternion optionRotation;

#if VERYANIMATION_ANIMATIONRIGGING
            [NonSerialized]
            public MonoBehaviour rigConstraint;
            [NonSerialized]
            public GUIContent rigConstraintGUIContent;
            [NonSerialized]
            public EditorCurveBinding rigConstraintWeight;
#endif
        }
        public AnimatorIKData[] ikData;

        public IKTarget[] ikTargetSelect;
        public IKTarget IKActiveTarget { get { return ikTargetSelect != null && ikTargetSelect.Length > 0 ? ikTargetSelect[0] : IKTarget.None; } }

        private AnimationCurve[] rootTCurves;
        private AnimationCurve[] rootQCurves;
        private AnimationCurve[] muscleCurves;
        private List<int> muscleCurvesUpdated;

        private int[] neckMuscleIndexes;
        private int[] headMuscleIndexes;

        private float[] defaultPoseShoulderToHandLength;
        private float[] armStretchToMaximumRate;

        private UDisc uDisc;

        private ReorderableList ikReorderableList;
        private bool advancedFoldout;

        public Avatar CloneAvatar { get; private set; }

        public void Initialize()
        {
            Release();

            ikData = new AnimatorIKData[(int)IKTarget.Total];
            for (int i = 0; i < ikData.Length; i++)
            {
                ikData[i] = new AnimatorIKData();
#if VERYANIMATION_ANIMATIONRIGGING
                UpdateAnimationRiggingConstraint((IKTarget)i);
#endif
            }
            ikTargetSelect = null;

            rootTCurves = new AnimationCurve[3];
            rootQCurves = new AnimationCurve[4];
            muscleCurves = new AnimationCurve[HumanTrait.MuscleCount];
            muscleCurvesUpdated = new List<int>();

            neckMuscleIndexes = new int[3];
            for (int i = 0; i < neckMuscleIndexes.Length; i++)
                neckMuscleIndexes[i] = HumanTrait.MuscleFromBone((int)HumanBodyBones.Neck, i);
            headMuscleIndexes = new int[3];
            for (int i = 0; i < headMuscleIndexes.Length; i++)
                headMuscleIndexes[i] = HumanTrait.MuscleFromBone((int)HumanBodyBones.Head, i);

            if (VAW.VA.IsHuman)
            {
                defaultPoseShoulderToHandLength = new float[2];
                armStretchToMaximumRate = new float[2];

                VAW.VA.Skeleton.SetTransformOrigin();
                var hp = new HumanPose()
                {
                    bodyPosition = new Vector3(0f, 1f, 0f),
                    bodyRotation = Quaternion.identity,
                    muscles = new float[HumanTrait.MuscleCount],
                };
                VAW.VA.Skeleton.HumanPoseHandler.SetHumanPose(ref hp);
                for (IKTarget target = IKTarget.LeftHand; target <= IKTarget.RightHand; target++)
                {
                    var index = (int)(target - IKTarget.LeftHand);
                    var shoulderIndex = (HumanBodyBones.LeftShoulder + index);
                    var upperArmIndex = (HumanBodyBones.LeftUpperArm + index);
                    var lowerArmIndex = (HumanBodyBones.LeftLowerArm + index);
                    var handIndex = (HumanBodyBones.LeftHand + index);
                    if (VAW.VA.Skeleton.HumanoidBones[(int)shoulderIndex] == null)
                        continue;

                    defaultPoseShoulderToHandLength[index] = (VAW.VA.Skeleton.HumanoidBones[(int)handIndex].transform.position - VAW.VA.Skeleton.HumanoidBones[(int)shoulderIndex].transform.position).magnitude;

                    var axisLength = VAW.VA.GetHumanoidAvatarAxisLength(shoulderIndex) + VAW.VA.GetHumanoidAvatarAxisLength(upperArmIndex) + VAW.VA.GetHumanoidAvatarAxisLength(lowerArmIndex);
                    armStretchToMaximumRate[index] = axisLength / defaultPoseShoulderToHandLength[index];
                }
                VAW.VA.Skeleton.SetTransformStart();
            }

            uDisc = new UDisc();

            UpdateReorderableList();

            UpdateGUIContentStrings();
            Language.OnLanguageChanged += UpdateGUIContentStrings;
        }
        public void Release()
        {
            Language.OnLanguageChanged -= UpdateGUIContentStrings;

            if (CloneAvatar != null)
            {
                Avatar.DestroyImmediate(CloneAvatar);
                CloneAvatar = null;
            }

            ikData = null;
            ikTargetSelect = null;
            rootTCurves = null;
            rootQCurves = null;
            muscleCurves = null;
            muscleCurvesUpdated = null;
            neckMuscleIndexes = null;
            headMuscleIndexes = null;
            uDisc = null;
            ikReorderableList = null;
        }

        public void LoadIKSaveSettings(VeryAnimationSaveSettings.AnimatorIKData[] saveIkData)
        {
            if (VAW.VA.IsHuman)
            {
                if (saveIkData != null && saveIkData.Length == ikData.Length)
                {
                    for (int i = 0; i < saveIkData.Length; i++)
                    {
                        var src = saveIkData[i];
                        var dst = ikData[i];
                        dst.enable = src.enable;
                        dst.autoRotation = src.autoRotation;
                        dst.spaceType = (AnimatorIKData.SpaceType)src.spaceType;
                        dst.parent = src.parent;
                        dst.position = src.position;
                        dst.rotation = src.rotation;
                        dst.headWeight = src.headWeight;
                        dst.eyesWeight = src.eyesWeight;
                        dst.enableShoulder = src.enableShoulder;
                        dst.shoulderSensitivityY = src.shoulderSensitivityY;
                        dst.shoulderSensitivityZ = src.shoulderSensitivityZ;
                        dst.swivelRotation = src.swivelRotation;
                        dst.defaultSyncType = (AnimatorIKData.SyncType)src.defaultSyncType;
                        dst.writeFlags = (AnimatorIKData.WriteFlags)src.writeFlags;
                        //Path
                        if (!string.IsNullOrEmpty(src.parentPath))
                        {
                            var t = VAW.GameObject.transform.Find(src.parentPath);
                            if (t != null)
                                dst.parent = t.gameObject;
                        }
                        SetSynchroIKtargetAnimatorIK((IKTarget)i);
                    }
                }
            }
        }
        public VeryAnimationSaveSettings.AnimatorIKData[] SaveIKSaveSettings()
        {
            if (VAW.VA.IsHuman)
            {
                var saveIkData = new List<VeryAnimationSaveSettings.AnimatorIKData>();
                if (ikData != null)
                {
                    foreach (var d in ikData)
                    {
                        saveIkData.Add(new VeryAnimationSaveSettings.AnimatorIKData()
                        {
                            enable = d.enable,
                            autoRotation = d.autoRotation,
                            spaceType = (int)d.spaceType,
                            parent = d.parent,
                            position = d.position,
                            rotation = d.rotation,
                            headWeight = d.headWeight,
                            eyesWeight = d.eyesWeight,
                            enableShoulder = d.enableShoulder,
                            shoulderSensitivityY = d.shoulderSensitivityY,
                            shoulderSensitivityZ = d.shoulderSensitivityZ,
                            swivelRotation = d.swivelRotation,
                            defaultSyncType = (int)d.defaultSyncType,
                            writeFlags = (uint)d.writeFlags,
                            //Path
                            parentPath = d.parent != null ? AnimationUtility.CalculateTransformPath(d.parent.transform, VAW.GameObject.transform) : "",
                        });
                    }
                }
                return saveIkData.ToArray();
            }
            else
            {
                return null;
            }
        }

        private void UpdateReorderableList()
        {
            ikReorderableList = null;
            if (ikData == null) return;
            ikReorderableList = new ReorderableList(ikData, typeof(AnimatorIKData), false, false, false, false);
            ikReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= ikData.Length)
                    return;

                float x = rect.x;
                {
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 4;
                    r.width = 16;
                    rect.xMin += r.width;
                    x = rect.x;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.Toggle(r, ikData[index].enable);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ChangeTargetIK((IKTarget)index);
                    }
                }

                EditorGUI.BeginDisabledGroup(!ikData[index].enable);
                {
                    const float Rate = 1f;
                    var r = rect;
                    r.x = x + 2;
                    r.y += 2;
                    r.height -= 4;
                    r.width = rect.width * Rate;
                    x += r.width;
                    r.width -= 4;
                    GUI.Label(r, IKTargetStrings[index]);
                }
                EditorGUI.EndDisabledGroup();

                if (!IsValid((IKTarget)index))
                {
                    var tex = VAW.UEditorGUIUtility.GetHelpIcon(MessageType.Warning);
                    var r = rect;
                    r.width = tex.width;
                    r.x = 20;
                    GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
                }

#if VERYANIMATION_ANIMATIONRIGGING
                if (ikData[index].rigConstraint != null)
                {
                    var r = rect;
                    r.width = 140f;
                    r.x = rect.xMax - r.width - 80f;
                    if (GUI.Button(r, ikData[index].rigConstraintGUIContent))
                    {
                        VAW.VA.SelectGameObject(ikData[index].rigConstraint.gameObject);
                        {
                            var list = new List<EditorCurveBinding>();
                            {
                                list.AddRange(GetAnimationRiggingConstraintBindings((IKTarget)index));
                                list.Add(ikData[index].rigConstraintWeight);
                            }
                            VAW.VA.SetAnimationWindowSynchroSelection(list.ToArray());
                        }
                    }
                }
#endif

                {
                    var r = rect;
                    r.width = 60f;
                    r.x = rect.xMax - r.width - 14;
                    EditorGUI.LabelField(r, IKSpaceTypeStrings[(int)ikData[index].spaceType], VAW.GuiStyleMiddleRightGreyMiniLabel);
                }

#if VERYANIMATION_ANIMATIONRIGGING
                if (ikReorderableList.index == index)
#else
                if (ikReorderableList.index == index && (IKTarget)index == IKTarget.Head)
#endif
                {
                    var r = rect;
                    r.y += 2;
                    r.height -= 2;
                    r.width = 24;
                    r.x = rect.xMax - 12;
                    advancedFoldout = EditorGUI.Foldout(r, advancedFoldout, new GUIContent(" ", "Advanced"), true);
                }
            };
            ikReorderableList.onChangedCallback = list =>
            {
                Undo.RecordObject(VAW, "Change Animator IK Data");
                ikTargetSelect = null;
                VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
            };
            ikReorderableList.onSelectCallback = list =>
            {
                if (list.index >= 0 && list.index < ikData.Length)
                {
                    if (ikData[list.index].enable)
                        VAW.VA.SelectAnimatorIKTargetPlusKey((IKTarget)list.index);
                    else
                    {
                        var index = list.index;
                        var humanoidIndex = GetEndHumanoidIndex((IKTarget)list.index);
                        VAW.VA.SelectGameObject(VAW.VA.HumanoidBones[(int)humanoidIndex]);
                        list.index = index;
                    }
                }
            };
        }

        private void UpdateGUIContentStrings()
        {
            for (int i = 0; i < (int)AnimatorIKData.SpaceType.Total; i++)
            {
                IKSpaceTypeStrings[i] = new GUIContent(Language.GetContent(Language.Help.SelectionAnimatorIKSpaceTypeGlobal + i));
            }
        }

        public void UpdateBones()
        {
            if (!VAW.VA.IsHuman)
                return;

            #region Non-Stretch Avatar
            if (CloneAvatar != null)
            {
                Avatar.DestroyImmediate(CloneAvatar);
                CloneAvatar = null;
            }
            if (VAW.VA.AnimatorAvatar != null)
            {
                CloneAvatar = Avatar.Instantiate<Avatar>(VAW.VA.AnimatorAvatar);
                CloneAvatar.hideFlags |= HideFlags.HideAndDontSave;
                VAW.VA.UAvatar.SetArmStretch(CloneAvatar, 0.0001f);  //Since it is occasionally wrong value when it is 0
                VAW.VA.UAvatar.SetLegStretch(CloneAvatar, 0.0001f);
                VAW.VA.Skeleton.Animator.avatar = CloneAvatar;
            }
            #endregion

            VAW.VA.Skeleton.VaEdit.onAnimatorIK -= AnimatorOnAnimatorIK;
            VAW.VA.Skeleton.VaEdit.onAnimatorIK += AnimatorOnAnimatorIK;
        }

        public void OnSelectionChange()
        {
            if (ikReorderableList != null)
            {
                if (IKActiveTarget != IKTarget.None)
                {
                    ikReorderableList.index = (int)IKActiveTarget;
                }
                else
                {
                    ikReorderableList.index = -1;
                }
            }

            if (ikTargetSelect != null)
            {
                foreach (var target in ikTargetSelect)
                {
                    SetSynchroIKtargetAnimatorIK(target);
                }
            }
        }

        public void UpdateSynchroIKSet()
        {
            for (int i = 0; i < ikData.Length; i++)
            {
                if (ikData[i].enable && ikData[i].synchroIKtarget)
                {
                    SynchroSet((IKTarget)i);
                }
                ikData[i].synchroIKtarget = false;
            }
        }
        [Flags]
        public enum SynchroSetFlags : UInt32
        {
            None = 0,
            SceneObject = (1 << 0),
            HumanoidIK = (1 << 1),
            AnimationRigging = (1 << 2),
            Default = UInt32.MaxValue,
        }
        public void SynchroSet(IKTarget target, SynchroSetFlags syncFlags = SynchroSetFlags.Default)
        {
            if (!VAW.VA.IsHuman) return;

            var data = ikData[(int)target];

            if (syncFlags == SynchroSetFlags.Default)
            {
                syncFlags = SynchroSetFlags.None;
#if VERYANIMATION_ANIMATIONRIGGING
                if (VAW.VA.AnimationRigging.IsValid && data.defaultSyncType >= AnimatorIKData.SyncType.AnimationRigging)
                {
                    syncFlags |= SynchroSetFlags.AnimationRigging;
                }
#endif
                if (data.defaultSyncType >= AnimatorIKData.SyncType.HumanoidIK)
                {
                    syncFlags |= SynchroSetFlags.HumanoidIK;
                }
                if (data.defaultSyncType >= AnimatorIKData.SyncType.SceneObject)
                {
                    syncFlags |= SynchroSetFlags.SceneObject;
                }
            }

            data.rootBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)GetStartHumanoidIndex(target)];
            data.parentBoneIndex = -1;
            switch (data.spaceType)
            {
                case AnimatorIKData.SpaceType.Local:
                    switch (target)
                    {
                        case IKTarget.Head:
                            data.parentBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)HumanBodyBones.UpperChest];
                            if (data.parentBoneIndex < 0)
                                data.parentBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)HumanBodyBones.Chest];
                            break;
                        case IKTarget.LeftHand:
                        case IKTarget.RightHand:
                            if (!data.enableShoulder)
                                data.parentBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)(target == IKTarget.LeftHand ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder)];
                            if (data.parentBoneIndex < 0)
                                data.parentBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)HumanBodyBones.UpperChest];
                            if (data.parentBoneIndex < 0)
                                data.parentBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)HumanBodyBones.Chest];
                            break;
                        case IKTarget.LeftFoot:
                        case IKTarget.RightFoot:
                            data.parentBoneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)HumanBodyBones.Hips];
                            break;
                    }
                    break;
                case AnimatorIKData.SpaceType.Parent:
                    data.parentBoneIndex = VAW.VA.BonesIndexOf(data.parent);
                    break;
            }

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            float swivelRotation = 0f;
            Vector3? swivelPosition = null;
            GameObject[] syncHumanoidBones = VAW.VA.Skeleton.HumanoidBones;

            void GetSwivelRotation(Vector3 posStart, Vector3 posEnd, Vector3 posCenter, out float swivelRotation)
            {
                const float DotThresholdMin = 0.97f;
                const float DotThresholdMax = 1f;

                var worldToLocalMatrix = VAW.VA.TransformPoseSave.StartMatrix.inverse;
                posStart = worldToLocalMatrix.MultiplyPoint3x4(posStart);
                posEnd = worldToLocalMatrix.MultiplyPoint3x4(posEnd);
                posCenter = worldToLocalMatrix.MultiplyPoint3x4(posCenter);

                swivelRotation = 0f;
                var axis = posEnd - posStart;
                if (axis.sqrMagnitude > 0f)
                {
                    axis.Normalize();
                    var posP = posStart + axis * Vector3.Dot((posCenter - posStart), axis);

                    {
                        var vec = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, axis)) * (posCenter - posP).normalized;
                        var rot = Quaternion.FromToRotation(Vector3.up, vec);
                        swivelRotation = Mathf.Repeat(rot.eulerAngles.z + 360f, 360f);
                    }
                    var basicSwivelRotation = 0f;
                    {
                        var hintBasicVector = target switch
                        {
                            IKTarget.LeftHand => VAW.VA.GetHumanoidAvatarPostAxis(syncHumanoidBones, HumanBodyBones.LeftUpperArm, 2),
                            IKTarget.RightHand => -VAW.VA.GetHumanoidAvatarPostAxis(syncHumanoidBones, HumanBodyBones.RightUpperArm, 2),
                            IKTarget.LeftFoot => -VAW.VA.GetHumanoidAvatarPostAxis(syncHumanoidBones, HumanBodyBones.LeftUpperLeg, 1),
                            IKTarget.RightFoot => VAW.VA.GetHumanoidAvatarPostAxis(syncHumanoidBones, HumanBodyBones.RightUpperLeg, 1),
                            _ => Vector3.zero,
                        };
                        hintBasicVector = Quaternion.Inverse(VAW.VA.TransformPoseSave.StartRotation) * hintBasicVector;

                        var posCenterBasic = posP + hintBasicVector;
                        var vec = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, axis)) * (posCenterBasic - posP).normalized;
                        var rot = Quaternion.FromToRotation(Vector3.up, vec);
                        basicSwivelRotation = Mathf.Repeat(rot.eulerAngles.z + 360f, 360f);
                    }
                    if (Mathf.Abs(swivelRotation - basicSwivelRotation) > 180f)
                    {
                        if (swivelRotation < basicSwivelRotation)
                            swivelRotation += 360f;
                        else
                            basicSwivelRotation += 360f;
                    }

                    var dot = Vector3.Dot((posCenter - posStart).normalized, (posEnd - posStart).normalized);
                    var weight = Mathf.InverseLerp(DotThresholdMin, DotThresholdMax, Mathf.Abs(dot));
                    swivelRotation = Mathf.Lerp(swivelRotation, basicSwivelRotation, weight);
                    swivelRotation = Mathf.Repeat(swivelRotation + 180f, 360f) - 180f;
                }
            }

            bool done = false;
            switch (target)
            {
                case IKTarget.Head:
#if VERYANIMATION_ANIMATIONRIGGING
                    #region AnimationRigging
                    if (!done && syncFlags.HasFlag(SynchroSetFlags.AnimationRigging))
                    {
                        var constraint = data.rigConstraint as MultiAimConstraint;
                        if (constraint != null && constraint.data.sourceObjects.Count > 0)
                        {
                            var targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.sourceObjects[0].transform.gameObject);
                            if (targetBoneIndex >= 0 && VAW.VA.IsHaveAnimationCurveTransformPosition(targetBoneIndex))
                            {
                                var parent = VAW.VA.Skeleton.Bones[targetBoneIndex].transform.parent;
                                position = parent.localToWorldMatrix.MultiplyPoint3x4(VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex));
                                rotation = Quaternion.identity;
                                done = true;
                            }
                        }
                    }
                    #endregion
#endif
                    {
                        void SetFromHumanoidBones()
                        {
                            var t = syncHumanoidBones[(int)HumanBodyBones.Head].transform;
                            position = t.position - VAW.VA.GetHumanoidAvatarPostAxis(syncHumanoidBones, HumanBodyBones.Head, 1) * VAW.Animator.humanScale;
                            rotation = Quaternion.identity;
                        }

                        #region SceneObject
                        if (!done && syncFlags.HasFlag(SynchroSetFlags.SceneObject))
                        {
                            syncHumanoidBones = VAW.VA.HumanoidBones;
                            SetFromHumanoidBones();
                            done = true;
                        }
                        #endregion

                        #region Skeleton
                        if (!done)
                        {
                            SetFromHumanoidBones();
                            done = true;
                        }
                        #endregion
                    }

                    {
                        var hp = new HumanPose();
                        VAW.VA.GetSkeletonHumanPose(ref hp);

                        float angleNeck, angleHead;
                        {
                            var muscle = hp.muscles[neckMuscleIndexes[1]];
                            float angle = (VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Neck].max.y - VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Neck].min.y) / 2f;
                            angleNeck = (-angle * muscle);
                        }
                        {
                            var muscle = hp.muscles[headMuscleIndexes[1]];
                            float angle = (VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Head].max.y - VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Head].min.y) / 2f;
                            angleHead = (-angle * muscle);
                        }
                        swivelRotation = angleNeck + angleHead;
                    }
                    break;
                case IKTarget.LeftHand:
                case IKTarget.RightHand:
                case IKTarget.LeftFoot:
                case IKTarget.RightFoot:
                    var hiStart = GetStartHumanoidIndex(target);
                    var hiEnd = GetEndHumanoidIndex(target);
                    var hiCenter = GetCenterHumanoidIndex(target);
#if VERYANIMATION_ANIMATIONRIGGING
                    #region AnimationRigging
                    if (!done && syncFlags.HasFlag(SynchroSetFlags.AnimationRigging))
                    {
                        var constraint = data.rigConstraint as TwoBoneIKConstraint;
                        if (constraint != null && constraint.data.target != null && constraint.data.hint != null)
                        {
                            var tStart = VAW.VA.Skeleton.HumanoidBones[(int)hiStart].transform;
                            var tEnd = VAW.VA.Skeleton.HumanoidBones[(int)hiEnd].transform;
                            var tCenter = VAW.VA.Skeleton.HumanoidBones[(int)hiCenter].transform;
                            var targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.target.gameObject);
                            var hintBoneIndex = VAW.VA.BonesIndexOf(constraint.data.hint.gameObject);
                            if (targetBoneIndex >= 0 && VAW.VA.IsHaveAnimationCurveTransformPosition(targetBoneIndex) &&
                                VAW.VA.GetHaveAnimationCurveTransformRotationMode(targetBoneIndex) != URotationCurveInterpolation.Mode.Undefined &&
                                hintBoneIndex >= 0 && VAW.VA.IsHaveAnimationCurveTransformPosition(hintBoneIndex))
                            {
                                {
                                    var parent = VAW.VA.Skeleton.Bones[targetBoneIndex].transform.parent;
                                    position = parent.localToWorldMatrix.MultiplyPoint3x4(VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex));
                                    rotation = parent.rotation * VAW.VA.GetAnimationValueTransformRotation(targetBoneIndex);
                                    #region FeetBottomHeight
                                    switch (target)
                                    {
                                        case IKTarget.LeftFoot: position += new Vector3(0f, VAW.VA.Skeleton.Animator.leftFeetBottomHeight, 0f); break;
                                        case IKTarget.RightFoot: position += new Vector3(0f, VAW.VA.Skeleton.Animator.rightFeetBottomHeight, 0f); break;
                                    }
                                    #endregion
                                }
                                {
                                    var parent = VAW.VA.Skeleton.Bones[hintBoneIndex].transform.parent;
                                    swivelPosition = parent.localToWorldMatrix.MultiplyPoint3x4(VAW.VA.GetAnimationValueTransformPosition(hintBoneIndex));
                                    GetSwivelRotation(tStart.position, tEnd.position, swivelPosition.Value, out swivelRotation);
                                }
                                done = true;
                            }
                        }
                    }
                    #endregion
#endif
                    #region HumanoidIK
                    if (!done && syncFlags.HasFlag(SynchroSetFlags.HumanoidIK))
                    {
                        var tStart = VAW.VA.Skeleton.HumanoidBones[(int)hiStart].transform;
                        var tEnd = VAW.VA.Skeleton.HumanoidBones[(int)hiEnd].transform;
                        var tCenter = VAW.VA.Skeleton.HumanoidBones[(int)hiCenter].transform;
                        var ikIndex = VeryAnimation.AnimatorIKIndex.LeftHand + (int)(target - IKTarget.LeftHand);
                        if (VAW.VA.IsHaveAnimationCurveAnimatorIkT(ikIndex) &&
                            VAW.VA.IsHaveAnimationCurveAnimatorIkQ(ikIndex))
                        {
                            var rootT = VAW.VA.GetAnimationValueAnimatorRootT();
                            var rootQ = VAW.VA.GetAnimationValueAnimatorRootQ();

                            rotation = (rootQ * VAW.VA.GetAnimationValueAnimatorIkQ(ikIndex)) * IKTargetSyncRotation[(int)target];

                            position = VAW.VA.GetAnimationValueAnimatorIkT(ikIndex);
                            position = rootT + rootQ * position;
                            position *= VAW.VA.Skeleton.Animator.humanScale;

                            rotation = VAW.VA.TransformPoseSave.StartRotation * rotation;
                            position = VAW.VA.TransformPoseSave.StartMatrix.MultiplyPoint3x4(position);

                            switch (ikIndex)
                            {
                                case VeryAnimation.AnimatorIKIndex.LeftFoot:
                                    position += rotation * new Vector3(0f, VAW.VA.Skeleton.Animator.leftFeetBottomHeight, 0f);
                                    break;
                                case VeryAnimation.AnimatorIKIndex.RightFoot:
                                    position += rotation * new Vector3(0f, VAW.VA.Skeleton.Animator.rightFeetBottomHeight, 0f);
                                    break;
                            }

                            GetSwivelRotation(tStart.position, tEnd.position, tCenter.position, out swivelRotation);
                            done = true;
                        }
                    }
                    #endregion

                    {
                        void SetFromHumanoidBones()
                        {
                            var tStart = syncHumanoidBones[(int)hiStart].transform;
                            var tEnd = syncHumanoidBones[(int)hiEnd].transform;
                            var tCenter = syncHumanoidBones[(int)hiCenter].transform;
                            position = tEnd.position;
                            rotation = tEnd.rotation * VAW.VA.GetHumanoidAvatarPostRotation((HumanBodyBones)hiEnd) * IKTargetSyncRotation[(int)target];
                            GetSwivelRotation(tStart.position, tEnd.position, tCenter.position, out swivelRotation);
                        }

                        #region SceneObject
                        if (!done && syncFlags.HasFlag(SynchroSetFlags.SceneObject))
                        {
                            syncHumanoidBones = VAW.VA.HumanoidBones;
                            SetFromHumanoidBones();
                            done = true;
                        }
                        #endregion

                        #region Skeleton
                        if (!done)
                        {
                            SetFromHumanoidBones();
                            done = true;
                        }
                        #endregion
                    }
                    break;
            }
            if (!done)
                return;

            switch (data.spaceType)
            {
                case AnimatorIKData.SpaceType.Global:
                case AnimatorIKData.SpaceType.Local:
                    data.WorldPosition = position;
                    data.WorldRotation = rotation;
                    data.swivelRotation = Mathf.Repeat(swivelRotation + 180f, 360f) - 180f;
                    break;
                case AnimatorIKData.SpaceType.Parent:
                    //not update
                    if (target == IKTarget.Head)
                        data.rotation = Quaternion.identity;
                    break;
            }
            data.syncHumanoidBones = syncHumanoidBones;

            UpdateOptionPosition(target);
            if (!swivelPosition.HasValue)
                UpdateSwivelPosition(target);
            else
                data.swivelPosition = swivelPosition.Value;

#if VERYANIMATION_ANIMATIONRIGGING
            UpdateAnimationRiggingConstraint(target);
#endif
        }
        public void UpdateOptionPosition(IKTarget target)
        {
            if (!VAW.VA.IsHuman) return;

            var data = ikData[(int)target];

            #region Heel
            switch (target)
            {
                case IKTarget.LeftFoot:
                    if (data.syncHumanoidBones[(int)HumanBodyBones.LeftToes] != null)
                    {
                        var tB = data.syncHumanoidBones[(int)HumanBodyBones.LeftFoot].transform;
                        var tD = data.syncHumanoidBones[(int)HumanBodyBones.LeftToes].transform;
                        data.optionRotation = data.WorldRotation;
                        data.optionPosition = data.WorldPosition + data.optionRotation * Vector3.back * Vector3.Distance(tD.position, tB.position) * 6f;
                    }
                    break;
                case IKTarget.RightFoot:
                    if (data.syncHumanoidBones[(int)HumanBodyBones.RightToes] != null)
                    {
                        var tB = data.syncHumanoidBones[(int)HumanBodyBones.RightFoot].transform;
                        var tD = data.syncHumanoidBones[(int)HumanBodyBones.RightToes].transform;
                        data.optionRotation = data.WorldRotation;
                        data.optionPosition = data.WorldPosition + data.optionRotation * Vector3.back * Vector3.Distance(tD.position, tB.position) * 6f;
                    }
                    break;
            }
            #endregion
        }
        public void UpdateSwivelPosition(IKTarget target)
        {
            if (!VAW.VA.IsHuman) return;
            if (target < IKTarget.LeftHand || target > IKTarget.RightFoot)
                return;

            var data = ikData[(int)target];

            #region Swivel
            {
                var hiA = GetStartHumanoidIndex(target);
                var hiB = GetEndHumanoidIndex(target);
                var hiC = GetCenterHumanoidIndex(target);
                var posA = data.syncHumanoidBones[(int)hiA].transform.position;
                var posB = data.syncHumanoidBones[(int)hiB].transform.position;
                var posC = data.syncHumanoidBones[(int)hiC].transform.position;

                var worldToLocalMatrix = VAW.VA.TransformPoseSave.StartMatrix.inverse;
                posA = worldToLocalMatrix.MultiplyPoint3x4(posA);
                posB = worldToLocalMatrix.MultiplyPoint3x4(posB);
                posC = worldToLocalMatrix.MultiplyPoint3x4(posC);

                var axis = posB - posA;
                if (axis.sqrMagnitude > 0f)
                {
                    axis.Normalize();
                    var posP = posA + axis * Vector3.Dot((posC - posA), axis);
                    float length = Vector3.Distance(posC, posA) + Vector3.Distance(posC, posB);
                    var vec = Quaternion.AngleAxis(data.swivelRotation, axis) * (Quaternion.FromToRotation(Vector3.forward, axis) * Vector3.up);
                    var posL = posP + vec * length;
                    data.swivelPosition = VAW.VA.TransformPoseSave.StartMatrix.MultiplyPoint3x4(posL);
                }
            }
            #endregion
        }

        public bool IsValid(IKTarget target)
        {
            if (!VAW.VA.IsHuman || VAW.VA.BoneDefaultPose == null)
                return false;

            var boneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)GetEndHumanoidIndex(target)];
            boneIndex = VAW.VA.ParentBoneIndexes[boneIndex];
            while (boneIndex >= 0)
            {
                if (VAW.VA.BoneDefaultPose[boneIndex] == null)
                    return false;
                if (Mathf.Abs(VAW.VA.BoneDefaultPose[boneIndex].scale.x - VAW.VA.Bones[boneIndex].transform.localScale.x) > 0.0001f ||
                    Mathf.Abs(VAW.VA.BoneDefaultPose[boneIndex].scale.y - VAW.VA.Bones[boneIndex].transform.localScale.y) > 0.0001f ||
                    Mathf.Abs(VAW.VA.BoneDefaultPose[boneIndex].scale.z - VAW.VA.Bones[boneIndex].transform.localScale.z) > 0.0001f)
                    return false;
                boneIndex = VAW.VA.ParentBoneIndexes[boneIndex];
            }

            return true;
        }

        public void UpdateIK(bool rootUpdated)
        {
            if (!VAW.VA.IsHuman) return;
            if (!GetUpdateIKtargetAll()) return;

            {
                for (int i = 0; i < 3; i++)
                    rootTCurves[i] = null;
                for (int i = 0; i < 4; i++)
                    rootQCurves[i] = null;
                for (int i = 0; i < muscleCurves.Length; i++)
                    muscleCurves[i] = null;
            }
            var hp = new HumanPose();

            VAW.VA.Skeleton.SetApplyIK(false);
            VAW.VA.Skeleton.SetTransformStart();

            VAW.VA.Skeleton.SampleAnimation(VAW.VA.CurrentClip, VAW.VA.CurrentTime);

            #region BeforeIK
            {
                bool updateBeforeIK = false;

                #region Reset Head LeftRight
                if (ikData[(int)IKTarget.Head].IsUpdate)
                {
                    for (int i = 0; i < neckMuscleIndexes.Length; i++)
                    {
                        var muscleIndex = neckMuscleIndexes[i];
                        muscleCurves[muscleIndex] = VAW.VA.GetAnimationCurveAnimatorMuscle(muscleIndex);
                    }
                    for (int i = 0; i < headMuscleIndexes.Length; i++)
                    {
                        var muscleIndex = headMuscleIndexes[i];
                        muscleCurves[muscleIndex] = VAW.VA.GetAnimationCurveAnimatorMuscle(muscleIndex);
                    }

                    VAW.VA.SetKeyframe(muscleCurves[neckMuscleIndexes[0]], VAW.VA.CurrentTime, 0f);
                    VAW.VA.SetKeyframe(muscleCurves[headMuscleIndexes[0]], VAW.VA.CurrentTime, 0f);
                    {
                        float angle = (VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Neck].max.y - VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Neck].min.y) / 2f;
                        var rate = (-ikData[(int)IKTarget.Head].swivelRotation / angle) / 2f;
                        VAW.VA.SetKeyframe(muscleCurves[neckMuscleIndexes[1]], VAW.VA.CurrentTime, rate);
                    }
                    {
                        float angle = (VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Head].max.y - VAW.VA.HumanoidMuscleLimit[(int)HumanBodyBones.Head].min.y) / 2f;
                        var rate = (-ikData[(int)IKTarget.Head].swivelRotation / angle) / 2f;
                        VAW.VA.SetKeyframe(muscleCurves[headMuscleIndexes[1]], VAW.VA.CurrentTime, rate);
                    }
                    {
                        var rate = muscleCurves[neckMuscleIndexes[2]].Evaluate(VAW.VA.CurrentTime);
                        rate = Mathf.Clamp(rate, -1f, 1f);
                        VAW.VA.SetKeyframe(muscleCurves[neckMuscleIndexes[2]], VAW.VA.CurrentTime, rate);
                    }
                    {
                        var rate = muscleCurves[headMuscleIndexes[2]].Evaluate(VAW.VA.CurrentTime);
                        rate = Mathf.Clamp(rate, -1f, 1f);
                        VAW.VA.SetKeyframe(muscleCurves[headMuscleIndexes[2]], VAW.VA.CurrentTime, rate);
                    }

                    for (int i = 0; i < neckMuscleIndexes.Length; i++)
                    {
                        var muscleIndex = neckMuscleIndexes[i];
                        VAW.VA.SetAnimationCurveAnimatorMuscle(muscleIndex, muscleCurves[muscleIndex]);
                    }
                    for (int i = 0; i < headMuscleIndexes.Length; i++)
                    {
                        var muscleIndex = headMuscleIndexes[i];
                        VAW.VA.SetAnimationCurveAnimatorMuscle(muscleIndex, muscleCurves[muscleIndex]);
                    }

                    updateBeforeIK = true;
                }
                #endregion
                #region CalcShoulder
                for (IKTarget target = IKTarget.LeftHand; target <= IKTarget.RightHand; target++)
                {
                    if (!ikData[(int)target].IsUpdate || !ikData[(int)target].enableShoulder)
                        continue;

                    var index = (int)(target - IKTarget.LeftHand);
                    var shoulderIndex = (HumanBodyBones.LeftShoulder + index);

                    var preAxisX = VAW.VA.GetHumanoidAvatarPreAxis(VAW.VA.Skeleton.HumanoidBones, shoulderIndex, 0);
                    var preAxisY = VAW.VA.GetHumanoidAvatarPreAxis(VAW.VA.Skeleton.HumanoidBones, shoulderIndex, 1);
                    if (target == IKTarget.LeftHand)
                        preAxisY = -preAxisY;
                    var preAxisZ = VAW.VA.GetHumanoidAvatarPreAxis(VAW.VA.Skeleton.HumanoidBones, shoulderIndex, 2);
                    if (target == IKTarget.LeftHand)
                        preAxisZ = -preAxisZ;

                    var posShoulder = VAW.VA.Skeleton.HumanoidBones[(int)shoulderIndex].transform.position;
                    var posTarget = ikData[(int)target].WorldPosition;
                    var vecTarget = posTarget - posShoulder;
                    var vecTargetSave = vecTarget;

                    //planeClamp
                    {
                        var posPlane = posShoulder + preAxisX * VAW.VA.GetHumanoidAvatarAxisLength(shoulderIndex);
                        var dot = Vector3.Dot(posTarget - posPlane, preAxisX);
                        if (dot <= 0f)
                        {
                            posTarget -= preAxisX * dot;
                            vecTarget = posTarget - posShoulder;
                        }
                    }

                    //muscleY
                    {
                        var muscleIndex = HumanTrait.MuscleFromBone((int)shoulderIndex, 1);
                        muscleCurves[muscleIndex] = VAW.VA.GetAnimationCurveAnimatorMuscle(muscleIndex);

                        var muscleValue = 0f;
                        //Vector
                        {
                            var dot = Vector3.Dot(vecTarget, preAxisY);
                            var posP = posTarget - preAxisY * dot;
                            var vecX = posP - posShoulder;
                            var angle = Vector3.Angle(preAxisX, vecX.normalized);
                            if (Vector3.Dot(preAxisZ, vecX) > 0f)
                                angle = -angle;
                            angle = Mathf.Clamp(angle, -90f, 90f);
                            muscleValue = VAW.VA.EulerAngle2Muscle(muscleIndex, angle);
                            muscleValue *= ikData[(int)target].shoulderSensitivityY;
                            muscleValue = Mathf.Clamp(muscleValue, -1f, 1f);
                        }
                        //Bend
                        {
                            var bendRate = vecTargetSave.magnitude / defaultPoseShoulderToHandLength[index];
                            if (bendRate >= 1f)
                            {
                                const float MuscleYBendRate = 0.9f;

                                bendRate = EditorCommon.InverseLerpUnclamped(1f, 1f + (armStretchToMaximumRate[index] - 1f) * MuscleYBendRate, bendRate);
                                bendRate *= ikData[(int)target].shoulderSensitivityY;
                                bendRate = Mathf.Clamp01(bendRate);
                                muscleValue = Mathf.Lerp(0f, muscleValue, bendRate);
                            }
                            else
                            {
                                const float MuscleYBendMin = 0.7f;
                                bendRate = EditorCommon.InverseLerpUnclamped(1f, MuscleYBendMin, bendRate);
                                bendRate *= ikData[(int)target].shoulderSensitivityY;
                                bendRate = Mathf.Clamp01(bendRate);
                                muscleValue = Mathf.Lerp(0f, 1f, bendRate);
                            }
                        }
                        //Inside
                        {
                            const float MuscleYInsideMax = 0f;
                            const float MuscleYInsideMin = -0.3f;

                            var dot = Vector3.Dot(vecTargetSave.normalized, preAxisX);
                            var insideRate = EditorCommon.InverseLerpUnclamped(MuscleYInsideMax, MuscleYInsideMin, dot);
                            insideRate *= ikData[(int)target].shoulderSensitivityY;
                            insideRate = Mathf.Clamp(insideRate, -1f, 1f);
                            muscleValue = Mathf.Lerp(muscleValue, -1f, insideRate);
                        }

                        if (VAW.VA.optionsClampMuscle)
                        {
                            muscleValue = Mathf.Clamp(muscleValue, -1f, 1f);
                        }

                        VAW.VA.SetKeyframe(muscleCurves[muscleIndex], VAW.VA.CurrentTime, muscleValue);
                        VAW.VA.SetAnimationCurveAnimatorMuscle(muscleIndex, muscleCurves[muscleIndex]);
                    }

                    //muscleZ
                    {
                        var muscleIndex = HumanTrait.MuscleFromBone((int)shoulderIndex, 2);
                        muscleCurves[muscleIndex] = VAW.VA.GetAnimationCurveAnimatorMuscle(muscleIndex);

                        var muscleValue = 0f;

                        //Vector
                        {
                            var dot = Vector3.Dot(vecTarget, preAxisZ);
                            var posP = posTarget - preAxisZ * dot;

                            var vecX = posP - posShoulder;
                            var angle = Vector3.Angle(preAxisX, vecX.normalized);
                            if (Vector3.Dot(preAxisY, vecX) < 0f)
                                angle = -angle;
                            angle = Mathf.Clamp(angle, -90f, 90f);
                            muscleValue = VAW.VA.EulerAngle2Muscle(muscleIndex, angle);
                            muscleValue *= ikData[(int)target].shoulderSensitivityZ;
                            muscleValue = Mathf.Clamp(muscleValue, -1f, 1f);
                        }

                        //Bend
                        {
                            const float MuscleZBendRate = 0.9f;

                            var bendRate = vecTargetSave.magnitude / defaultPoseShoulderToHandLength[index];
                            bendRate = EditorCommon.InverseLerpUnclamped(1f, 1f + (armStretchToMaximumRate[index] - 1f) * MuscleZBendRate, bendRate);
                            bendRate *= ikData[(int)target].shoulderSensitivityZ;
                            bendRate = Mathf.Clamp01(bendRate);
                            muscleValue = Mathf.Lerp(0f, muscleValue, bendRate);
                        }

                        if (VAW.VA.optionsClampMuscle)
                        {
                            muscleValue = Mathf.Clamp(muscleValue, -1f, 1f);
                        }

                        VAW.VA.SetKeyframe(muscleCurves[muscleIndex], VAW.VA.CurrentTime, muscleValue);
                        VAW.VA.SetAnimationCurveAnimatorMuscle(muscleIndex, muscleCurves[muscleIndex]);
                    }

                    updateBeforeIK = true;
                }
                #endregion

                if (updateBeforeIK)
                {
                    if (VAW.VA.ForceUpdateCurrentFrameAnimatorRootCorrectionImmediate())
                        rootUpdated = true;
                }
            }
            #endregion

            VAW.VA.Skeleton.SetApplyIK(true);
            VAW.VA.Skeleton.SetTransformOrigin();

            #region Loop
            int loopCount = 1;
            bool baseParent = false;
            {
                if (VAW.VA.rootCorrectionMode == VeryAnimation.RootCorrectionMode.Disable)
                {
                    loopCount = Math.Max(loopCount, 4);
                }
                foreach (var data in ikData)
                {
                    if (data.IsUpdate &&
                        data.spaceType == AnimatorIKData.SpaceType.Parent &&
                        data.parentBoneIndex >= 0)
                    {
                        baseParent = true;
                        loopCount = Math.Max(loopCount, 2);
                        break;
                    }
                }
            }
            for (int loop = 0; loop < loopCount; loop++)
            {
                muscleCurvesUpdated.Clear();

                if (baseParent)
                {
                    VAW.VA.SampleAnimation(VeryAnimation.EditObjectFlag.SceneObject);
                }
                VAW.VA.Skeleton.SampleAnimation(VAW.VA.CurrentClip, VAW.VA.CurrentTime);

                #region Options
                {
                    VAW.VA.Skeleton.HumanPoseHandler.GetHumanPose(ref hp);
                    #region Virtual Neck
                    if (ikData[(int)IKTarget.Head].IsUpdate && VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Neck] == null)
                    {
                        for (int dof = 0; dof < 3; dof++)
                        {
                            var muscleNeck = neckMuscleIndexes[dof];
                            var muscleHead = headMuscleIndexes[dof];
                            if (muscleNeck >= 0 && muscleHead >= 0)
                            {
                                hp.muscles[muscleNeck] = hp.muscles[muscleHead] / 2f;
                                hp.muscles[muscleHead] = hp.muscles[muscleHead] / 2f;
                            }
                        }
                    }
                    #endregion
                    for (int i = 0; i < hp.muscles.Length; i++)
                    {
                        var humanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(i);
                        var target = IsIKBone(humanoidIndex);
                        if (target == IKTarget.None)
                            continue;
                        var data = ikData[(int)target];
                        if (!data.IsUpdate)
                            continue;
                        if (data.autoRotation)
                        {
                            if (humanoidIndex == GetEndHumanoidIndex(target))
                                hp.muscles[i] = 0f;
                            else
                            {   //Twist
                                switch (target)
                                {
                                    case IKTarget.LeftHand:
                                        if (humanoidIndex == HumanBodyBones.LeftLowerArm && HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftLowerArm, 0) == i)
                                            hp.muscles[i] = 0f;
                                        break;
                                    case IKTarget.RightHand:
                                        if (humanoidIndex == HumanBodyBones.RightLowerArm && HumanTrait.MuscleFromBone((int)HumanBodyBones.RightLowerArm, 0) == i)
                                            hp.muscles[i] = 0f;
                                        break;
                                    case IKTarget.LeftFoot:
                                        if (humanoidIndex == HumanBodyBones.LeftLowerLeg && HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftLowerLeg, 0) == i)
                                            hp.muscles[i] = 0f;
                                        break;
                                    case IKTarget.RightFoot:
                                        if (humanoidIndex == HumanBodyBones.RightLowerLeg && HumanTrait.MuscleFromBone((int)HumanBodyBones.RightLowerLeg, 0) == i)
                                            hp.muscles[i] = 0f;
                                        break;
                                }
                            }
                        }
                        if (VAW.VA.optionsClampMuscle)
                        {
                            hp.muscles[i] = Mathf.Clamp(hp.muscles[i], -1f, 1f);
                        }
                    }
                    VAW.VA.Skeleton.ResetTranformRoot();
                    VAW.VA.Skeleton.HumanPoseHandler.SetHumanPose(ref hp);
                }
                #endregion

                #region SetKeyframe
                {
                    VAW.VA.Skeleton.HumanPoseHandler.GetHumanPose(ref hp);
                    #region Virtual Neck
                    if (ikData[(int)IKTarget.Head].IsUpdate && VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Neck] == null)
                    {
                        for (int dof = 0; dof < 3; dof++)
                        {
                            var muscleNeck = neckMuscleIndexes[dof];
                            var muscleHead = headMuscleIndexes[dof];
                            if (muscleNeck >= 0 && muscleHead >= 0)
                            {
                                hp.muscles[muscleNeck] = hp.muscles[muscleHead] / 2f;
                                hp.muscles[muscleHead] = hp.muscles[muscleHead] / 2f;
                            }
                        }
                    }
                    #endregion
                    for (int i = 0; i < hp.muscles.Length; i++)
                    {
                        var humanoidIndex = (HumanBodyBones)HumanTrait.BoneFromMuscle(i);
                        var target = IsIKBone(humanoidIndex);
                        if (target == IKTarget.None || !ikData[(int)target].IsUpdate)
                            continue;
                        if (muscleCurves[i] == null)
                            muscleCurves[i] = VAW.VA.GetAnimationCurveAnimatorMuscle(i);
                        VAW.VA.SetKeyframe(muscleCurves[i], VAW.VA.CurrentTime, hp.muscles[i]);
                        muscleCurvesUpdated.Add(i);
                    }
                    if (rootUpdated)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (rootTCurves[i] == null)
                                rootTCurves[i] = VAW.VA.GetAnimationCurveAnimatorRootT(i);
                            VAW.VA.SetKeyframe(rootTCurves[i], VAW.VA.CurrentTime, hp.bodyPosition[i]);
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            if (rootQCurves[i] == null)
                                rootQCurves[i] = VAW.VA.GetAnimationCurveAnimatorRootQ(i);
                            VAW.VA.SetKeyframe(rootQCurves[i], VAW.VA.CurrentTime, hp.bodyRotation[i]);
                        }
                    }
                }
                #endregion

                #region Write
                {
                    foreach (var i in muscleCurvesUpdated)
                    {
                        VAW.VA.SetAnimationCurveAnimatorMuscle(i, muscleCurves[i]);
                    }
                    if (rootUpdated)
                    {
                        for (int i = 0; i < 3; i++)
                            VAW.VA.SetAnimationCurveAnimatorRootT(i, rootTCurves[i]);
                        for (int i = 0; i < 4; i++)
                            VAW.VA.SetAnimationCurveAnimatorRootQ(i, rootQCurves[i]);
                    }
                }
                #endregion
            }
            #endregion

            VAW.VA.Skeleton.SetApplyIK(false);
            VAW.VA.Skeleton.SetTransformStart();

#if VERYANIMATION_ANIMATIONRIGGING
            {
                VAW.VA.Skeleton.SampleAnimation(VAW.VA.CurrentClip, VAW.VA.CurrentTime);
                for (int target = 0; target < ikData.Length; target++)
                {
                    if (ikData[target].IsUpdate)
                    {
                        WriteAnimationRiggingConstraint((IKTarget)target, VAW.VA.CurrentTime);
                    }
                    else
                    {
                        #region Mirror
                        if (VAW.VA.optionsMirror && ikData[target].enable)
                        {
                            var mirrorTarget = IKTargetMirror[(int)target];
                            if (mirrorTarget != IKTarget.None && ikData[(int)mirrorTarget].IsUpdate)
                            {
                                var position = ikData[(int)mirrorTarget].WorldPosition;
                                var rotation = ikData[(int)mirrorTarget].WorldRotation;
                                var hintPosition = ikData[(int)mirrorTarget].swivelPosition;

                                position = VAW.VA.Skeleton.HumanoidHipsTransform.worldToLocalMatrix.MultiplyPoint3x4(position);
                                rotation = Quaternion.Inverse(VAW.VA.Skeleton.HumanoidHipsTransform.rotation) * rotation;
                                hintPosition = VAW.VA.Skeleton.HumanoidHipsTransform.worldToLocalMatrix.MultiplyPoint3x4(hintPosition);
                                position.x = -position.x;
                                rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
                                hintPosition.x = -hintPosition.x;
                                position = VAW.VA.Skeleton.HumanoidHipsTransform.localToWorldMatrix.MultiplyPoint3x4(position);
                                rotation = VAW.VA.Skeleton.HumanoidHipsTransform.rotation * rotation;
                                hintPosition = VAW.VA.Skeleton.HumanoidHipsTransform.localToWorldMatrix.MultiplyPoint3x4(hintPosition);

                                ikData[(int)target].WorldPosition = position;
                                ikData[(int)target].WorldRotation = rotation;
                                ikData[(int)target].swivelPosition = hintPosition;

                                WriteAnimationRiggingConstraint((IKTarget)target, VAW.VA.CurrentTime);
                            }
                        }
                        #endregion
                    }
                }
            }
#endif
        }

        public void HandleGUI()
        {
            if (!VAW.VA.IsHuman) return;

            if (IKActiveTarget != IKTarget.None && ikData[(int)IKActiveTarget].enable)
            {
                var activeData = ikData[(int)IKActiveTarget];
                var worldPosition = activeData.WorldPosition;
                var worldRotation = activeData.WorldRotation;
                var hiA = GetStartHumanoidIndex(IKActiveTarget);
                {
                    if (IKActiveTarget == IKTarget.Head)
                    {
                        #region IKSwivel
                        var posA = VAW.VA.Skeleton.HumanoidBones[(int)hiA].transform.position;
                        var posB = worldPosition;
                        var axis = posB - posA;
                        axis.Normalize();
                        if (axis.sqrMagnitude > 0f)
                        {
                            var posP = Vector3.Lerp(posA, posB, 0.5f);
                            Vector3 posPC;
                            {
                                var tpos = posP;
                                {
                                    var right = VAW.VA.GetHumanoidAvatarPostAxis(VAW.VA.Skeleton.HumanoidBones, HumanBodyBones.Head, 0);
                                    tpos += right;
                                }
                                Vector3 vec;
                                vec = tpos - posP;
                                var length = Vector3.Dot(vec, axis);
                                posPC = tpos - axis * length;
                            }
                            {
                                Handles.color = new Color(Handles.centerColor.r, Handles.centerColor.g, Handles.centerColor.b, Handles.centerColor.a * 0.5f);
                                Handles.DrawWireDisc(posP, axis, HandleUtility.GetHandleSize(posP));
                                {
                                    Handles.color = Handles.centerColor;
                                    Handles.DrawLine(posP, posP + (posPC - posP).normalized * HandleUtility.GetHandleSize(posP));
                                }
                            }
                            {
                                EditorGUI.BeginChangeCheck();
                                Handles.color = Handles.yAxisColor;
                                var rotDofDistSave = uDisc.GetRotationDist();
                                Handles.Disc(Quaternion.identity, posP, axis, HandleUtility.GetHandleSize(posP), true, EditorSnapSettings.rotate);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(VAW, "Rotate IK Swivel");
                                    var rotDist = uDisc.GetRotationDist() - rotDofDistSave;
                                    foreach (var target in ikTargetSelect)
                                    {
                                        var data = ikData[(int)target];
                                        data.swivelRotation = Mathf.Repeat(data.swivelRotation - rotDist + 180f, 360f) - 180f;
                                        UpdateSwivelPosition(target);
                                        VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region IKSwivel
                        var posA = VAW.VA.Skeleton.HumanoidBones[(int)hiA].transform.position;
                        var posB = worldPosition;
                        var axis = posB - posA;
                        axis.Normalize();
                        if (axis.sqrMagnitude > 0f)
                        {
                            var posP = Vector3.Lerp(posA, posB, 0.5f);
                            {
                                Handles.color = new Color(Handles.centerColor.r, Handles.centerColor.g, Handles.centerColor.b, Handles.centerColor.a * 0.5f);
                                Handles.DrawWireDisc(posP, axis, HandleUtility.GetHandleSize(posP));

                                var posPC = posA + axis * Vector3.Dot((activeData.swivelPosition - posA), axis);
                                Handles.color = Handles.centerColor;
                                Handles.DrawLine(posP, posP + (activeData.swivelPosition - posPC).normalized * HandleUtility.GetHandleSize(posP));

                                //DebugSwivel
                                //Handles.color = Color.red;
                                //Handles.DrawLine(posP, activeData.swivelPosition);
                            }
                            {
                                EditorGUI.BeginChangeCheck();
                                Handles.color = Handles.zAxisColor;
                                var rotDofDistSave = uDisc.GetRotationDist();
                                Handles.Disc(Quaternion.identity, posP, axis, HandleUtility.GetHandleSize(posP), true, EditorSnapSettings.rotate);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(VAW, "Rotate IK Swivel");
                                    var rotDist = uDisc.GetRotationDist() - rotDofDistSave;
                                    foreach (var target in ikTargetSelect)
                                    {
                                        var data = ikData[(int)target];
                                        data.swivelRotation = Mathf.Repeat(data.swivelRotation - rotDist + 180f, 360f) - 180f;
                                        UpdateSwivelPosition(target);
                                        VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    if (IKActiveTarget != IKTarget.Head &&
                        !activeData.autoRotation && VAW.VA.LastTool != Tool.Move)
                    {
                        #region Rotate
                        EditorGUI.BeginChangeCheck();
                        var rotation = Handles.RotationHandle(Tools.pivotRotation == PivotRotation.Local ? worldRotation : Tools.handleRotation, worldPosition);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Rotate IK Target");
                            void RotationAction(Quaternion move)
                            {
                                foreach (var target in ikTargetSelect)
                                {
                                    var data = ikData[(int)target];
                                    data.WorldRotation *= move;
                                    UpdateOptionPosition(target);
                                    UpdateSwivelPosition(target);
                                    VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                                }
                            }
                            if (Tools.pivotRotation == PivotRotation.Local)
                            {
                                var move = Quaternion.Inverse(worldRotation) * rotation;
                                RotationAction(move);
                            }
                            else
                            {
                                (Quaternion.Inverse(Tools.handleRotation) * rotation).ToAngleAxis(out float angle, out Vector3 axis);
                                var move = Quaternion.Inverse(worldRotation) * Quaternion.AngleAxis(angle, Tools.handleRotation * axis) * worldRotation;
                                RotationAction(move);
                                Tools.handleRotation = rotation;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region Move
                        Handles.color = Color.white;
                        EditorGUI.BeginChangeCheck();
                        var position = Handles.PositionHandle(worldPosition, Tools.pivotRotation == PivotRotation.Local ? worldRotation : Quaternion.identity);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Move IK Target");
                            var move = position - worldPosition;
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[(int)target].WorldPosition = ikData[(int)target].WorldPosition + move;
                                UpdateOptionPosition(target);
                                UpdateSwivelPosition(target);
                                VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                            }
                        }
                        #endregion
                    }
                    if (!activeData.autoRotation &&
                        ((IKActiveTarget == IKTarget.LeftFoot && VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.LeftToes] != null) ||
                       (IKActiveTarget == IKTarget.RightFoot && VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.RightToes] != null)))
                    {
                        {
                            Handles.color = Handles.centerColor;
                            Handles.DrawLine(worldPosition, activeData.optionPosition);
                        }
                        {
                            Handles.color = Color.white;
                            EditorGUI.BeginChangeCheck();
                            var handlePosition = Handles.PositionHandle(activeData.optionPosition, Tools.pivotRotation == PivotRotation.Local ? activeData.optionRotation : Quaternion.identity);
                            if (EditorGUI.EndChangeCheck())
                            {
                                var toesTransform = IKActiveTarget == IKTarget.LeftFoot ? VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.LeftToes].transform : VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.RightToes].transform;
                                var toesPos = toesTransform.position;
                                var beforeVec = activeData.optionPosition - toesPos;
                                var afterVec = handlePosition - toesPos;
                                beforeVec.Normalize();
                                afterVec.Normalize();
                                if (beforeVec.sqrMagnitude > 0f && afterVec.sqrMagnitude > 0f)
                                {
                                    Quaternion rotationY;
                                    {
                                        var normal = activeData.WorldRotation * Vector3.up;
                                        var beforeP = activeData.optionPosition - normal * Vector3.Dot(activeData.optionPosition - worldPosition, normal);
                                        var afterP = handlePosition - normal * Vector3.Dot(handlePosition - worldPosition, normal);
                                        rotationY = Quaternion.AngleAxis(Vector3.SignedAngle((beforeP - toesPos).normalized, (afterP - toesPos).normalized, normal), normal);
                                    }
                                    Quaternion rotationX;
                                    {
                                        var normal = activeData.WorldRotation * Vector3.right;
                                        var beforeP = activeData.optionPosition - normal * Vector3.Dot(activeData.optionPosition - worldPosition, normal);
                                        var afterP = handlePosition - normal * Vector3.Dot(handlePosition - worldPosition, normal);
                                        rotationX = Quaternion.AngleAxis(Vector3.SignedAngle((beforeP - toesPos).normalized, (afterP - toesPos).normalized, normal), normal);
                                    }
                                    var rotation = rotationX * rotationY;
                                    var afterPosition = toesPos + rotation * (worldPosition - toesPos);
                                    var movePosition = afterPosition - worldPosition;
                                    var moveRotation = rotation;
                                    foreach (var target in ikTargetSelect)
                                    {
                                        ikData[(int)target].WorldPosition = ikData[(int)target].WorldPosition + movePosition;
                                        ikData[(int)target].WorldRotation = moveRotation * ikData[(int)target].WorldRotation;
                                        VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                                        if (target == IKTarget.LeftFoot || target == IKTarget.RightFoot)
                                        {
                                            toesTransform.rotation = Quaternion.Inverse(moveRotation) * toesTransform.rotation;
                                            var hpAfter = new HumanPose();
                                            VAW.VA.GetSkeletonHumanPose(ref hpAfter);
                                            var muscleIndex = target == IKTarget.LeftFoot ? HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftToes, 2) : HumanTrait.MuscleFromBone((int)HumanBodyBones.RightToes, 2);
                                            var muscle = hpAfter.muscles[muscleIndex];
                                            if (VAW.VA.optionsClampMuscle)
                                                muscle = Mathf.Clamp(muscle, -1f, 1f);
                                            VAW.VA.SetAnimationValueAnimatorMuscle(muscleIndex, muscle);
                                        }
                                    }
                                }
                                activeData.optionPosition = handlePosition;
                                UpdateSwivelPosition(IKActiveTarget);
                            }
                        }
                    }
                }
            }
        }
        public void TargetGUI()
        {
            if (!VAW.VA.IsHuman) return;

            var e = Event.current;

            for (int target = 0; target < (int)IKTarget.Total; target++)
            {
                if (!ikData[target].enable) continue;

                var worldPosition = ikData[target].WorldPosition;
                var worldRotation = ikData[target].WorldRotation;
                var ikTarget = (IKTarget)target;
                var hiA = GetStartHumanoidIndex(ikTarget);
                if (ikTargetSelect != null &&
                    EditorCommon.ArrayContains(ikTargetSelect, ikTarget))
                {
                    #region Active
                    {
                        if (ikTarget == IKActiveTarget)
                        {
                            Handles.color = Color.white;
                            var hiA2 = hiA;
                            if (target == (int)IKTarget.Head)
                            {
                                if (VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Neck] != null)
                                    hiA2 = HumanBodyBones.Neck;
                                else
                                    hiA2 = HumanBodyBones.Head;
                            }
                            Vector3 worldPosition2 = VAW.VA.Skeleton.HumanoidBones[(int)hiA2].transform.position;
                            Handles.DrawLine(worldPosition, worldPosition2);
                        }
                        Handles.color = VAW.EditorSettings.SettingIKTargetActiveColor;
                        if (ikTarget == IKTarget.Head)
                            Handles.SphereHandleCap(0, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, EventType.Repaint);
                        else
                            Handles.ConeHandleCap(0, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, EventType.Repaint);
                    }
                    #endregion
                }
                else
                {
                    #region NonActive
                    var freeMoveHandleControlID = -1;
#if UNITY_2022_1_OR_NEWER
                    Handles.FreeMoveHandle(worldPosition, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, EditorSnapSettings.move, (id, pos, rot, size, eventType) =>
#else
                    Handles.FreeMoveHandle(worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, EditorSnapSettings.move, (id, pos, rot, size, eventType) =>
#endif
                    {
                        freeMoveHandleControlID = id;
                        Handles.color = VAW.EditorSettings.SettingIKTargetNormalColor;
                        if (ikTarget == IKTarget.Head)
                            Handles.SphereHandleCap(id, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, eventType);
                        else
                            Handles.ConeHandleCap(id, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, eventType);
                    });
                    if (GUIUtility.hotControl == freeMoveHandleControlID)
                    {
                        if (e.type == EventType.Layout)
                        {
                            GUIUtility.hotControl = -1;
                            {
                                var ikTargetTmp = ikTarget;
                                EditorApplication.delayCall += () =>
                                {
                                    VAW.VA.SelectAnimatorIKTargetPlusKey(ikTargetTmp);
                                };
                            }
                        }
                    }
                    #endregion
                }
            }
        }
        public void SelectionGUI()
        {
            if (!VAW.VA.IsHuman) return;
            if (IKActiveTarget == IKTarget.None) return;
            var activeData = ikData[(int)IKActiveTarget];
            #region Warning
            if (!IsValid(IKActiveTarget))
            {
                EditorGUILayout.HelpBox(Language.GetText(Language.Help.AnimatorIKScaleChangedErrorWarning), MessageType.Warning);
            }
            #endregion
            #region IK
            {
                EditorGUILayout.BeginHorizontal();
                #region Mirror
                {
                    var mirrorTarget = IKTargetMirror[(int)IKActiveTarget];
                    if (GUILayout.Button(Language.GetContentFormat(Language.Help.SelectionMirror, (mirrorTarget != IKTarget.None ? string.Format("From 'IK: {0}'", IKTargetStrings[(int)mirrorTarget]) : "From self"))))
                    {
                        VAW.VA.SetSelectionMirror();
                    }
                }
                #endregion
                EditorGUILayout.Space();
                #region Update
                if (GUILayout.Button(Language.GetContent(Language.Help.SelectionUpdateIK)))
                {
                    Undo.RecordObject(VAW, "Update IK");
                    foreach (var target in ikTargetSelect)
                    {
                        VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                    }
                }
                #endregion
                EditorGUILayout.Space();
                #region Sync
                {
                    EditorGUI.BeginDisabledGroup(activeData.spaceType == AnimatorIKData.SpaceType.Parent);
                    if (GUILayout.Button(Language.GetContent(Language.Help.SelectionSyncIK), VAW.GuiStyleDropDown))
                    {
                        var menu = new GenericMenu();
                        {
                            menu.AddItem(new GUIContent("Sync Default"), false, () =>
                            {
                                Undo.RecordObject(VAW, "Sync IK");
                                foreach (var target in ikTargetSelect)
                                    SynchroSet(target);
                                SceneView.RepaintAll();
                            });

                            menu.AddSeparator(string.Empty);

#if VERYANIMATION_ANIMATIONRIGGING
                            if (VAW.VA.AnimationRigging.IsValid)
                            {
                                menu.AddItem(new GUIContent("Sync Animation Rigging (Constraint target curves)"), false, () =>
                                {
                                    Undo.RecordObject(VAW, "Sync IK");
                                    foreach (var target in ikTargetSelect)
                                        SynchroSet(target, SynchroSetFlags.AnimationRigging);
                                    SceneView.RepaintAll();
                                });
                            }
#endif
                            if (VAW.VA.IsHuman && IKActiveTarget >= IKTarget.LeftHand && IKActiveTarget <= IKTarget.RightFoot)
                            {
                                menu.AddItem(new GUIContent("Sync Humanoid IK (Foot IK or Hand IK curves)"), false, () =>
                                {
                                    Undo.RecordObject(VAW, "Sync IK");
                                    foreach (var target in ikTargetSelect)
                                        SynchroSet(target, SynchroSetFlags.HumanoidIK);
                                    SceneView.RepaintAll();
                                });
                            }
                            menu.AddItem(new GUIContent("Sync Scene Object (Result)"), false, () =>
                            {
                                Undo.RecordObject(VAW, "Sync IK");
                                foreach (var target in ikTargetSelect)
                                    SynchroSet(target, SynchroSetFlags.SceneObject);
                                SceneView.RepaintAll();
                            });
                            menu.AddItem(new GUIContent("Sync Skeleton (Animation Clip)"), false, () =>
                            {
                                Undo.RecordObject(VAW, "Sync IK");
                                foreach (var target in ikTargetSelect)
                                    SynchroSet(target, SynchroSetFlags.None);
                                SceneView.RepaintAll();
                            });

                            menu.AddSeparator(string.Empty);

#if VERYANIMATION_ANIMATIONRIGGING
                            if (VAW.VA.AnimationRigging.IsValid)
                            {
                                menu.AddItem(new GUIContent("Set Default/Animation Rigging (Constraint)"), activeData.defaultSyncType == AnimatorIKData.SyncType.AnimationRigging, () =>
                                {
                                    Undo.RecordObject(VAW, "Sync Set Default");
                                    foreach (var target in ikTargetSelect)
                                    {
                                        ikData[(int)target].defaultSyncType = AnimatorIKData.SyncType.AnimationRigging;
                                        SynchroSet(target);
                                    }
                                    SceneView.RepaintAll();
                                });
                            }
                            else
#endif
                            {
                                menu.AddDisabledItem(new GUIContent("Set Default/Animation Rigging (Constraint)"), activeData.defaultSyncType == AnimatorIKData.SyncType.AnimationRigging);
                            }
                            if (VAW.VA.IsHuman && IKActiveTarget >= IKTarget.LeftHand && IKActiveTarget <= IKTarget.RightFoot)
                            {
                                menu.AddItem(new GUIContent("Set Default/Humanoid IK (Foot IK or Hand IK)"), activeData.defaultSyncType == AnimatorIKData.SyncType.HumanoidIK, () =>
                                {
                                    Undo.RecordObject(VAW, "Sync Set Default");
                                    foreach (var target in ikTargetSelect)
                                    {
                                        ikData[(int)target].defaultSyncType = AnimatorIKData.SyncType.HumanoidIK;
                                        SynchroSet(target);
                                    }
                                    SceneView.RepaintAll();
                                });
                            }
                            menu.AddItem(new GUIContent("Set Default/Scene Object (Result)"), activeData.defaultSyncType == AnimatorIKData.SyncType.SceneObject, () =>
                            {
                                Undo.RecordObject(VAW, "Sync Set Default");
                                foreach (var target in ikTargetSelect)
                                {
                                    ikData[(int)target].defaultSyncType = AnimatorIKData.SyncType.SceneObject;
                                    SynchroSet(target);
                                }
                                SceneView.RepaintAll();
                            });
                            menu.AddItem(new GUIContent("Set Default/Skeleton (Animation Clip)"), activeData.defaultSyncType == AnimatorIKData.SyncType.Skeleton, () =>
                            {
                                Undo.RecordObject(VAW, "Sync Set Default");
                                foreach (var target in ikTargetSelect)
                                {
                                    ikData[(int)target].defaultSyncType = AnimatorIKData.SyncType.Skeleton;
                                    SynchroSet(target);
                                }
                                SceneView.RepaintAll();
                            });
                        }
                        menu.ShowAsContext();
                    }
                    EditorGUI.EndDisabledGroup();
                }
                #endregion
                EditorGUILayout.Space();
                #region Reset
                if (GUILayout.Button(Language.GetContent(Language.Help.SelectionResetIK)))
                {
                    Undo.RecordObject(VAW, "Reset IK");
                    foreach (var target in ikTargetSelect)
                    {
                        Reset(target);
                    }
                }
                #endregion
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            int RowCount = 0;

            #region Options
            if (IKActiveTarget > IKTarget.Head)
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Options", GUILayout.Width(64));
                {
                    EditorGUI.BeginChangeCheck();
                    var autoRotation = GUILayout.Toggle(activeData.autoRotation, Language.GetContent(Language.Help.SelectionAnimatorIKOptionsAutoRotation), EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Options autoRotation");
                        foreach (var target in ikTargetSelect)
                        {
                            ikData[(int)target].autoRotation = autoRotation;
                            SynchroSet(target);
                            VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                        }
                    }
                }
                if (IKActiveTarget >= IKTarget.LeftHand && IKActiveTarget <= IKTarget.RightHand)
                {
                    var boneShoulder = IKActiveTarget == IKTarget.LeftHand ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder;
                    EditorGUI.BeginDisabledGroup(VAW.VA.HumanoidBones[(int)boneShoulder] == null);
                    EditorGUI.BeginChangeCheck();
                    var enableShoulder = GUILayout.Toggle(activeData.enableShoulder, Language.GetContent(Language.Help.SelectionAnimatorIKOptionsShoulder), EditorStyles.miniButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Options enableShoulder");
                        foreach (var target in ikTargetSelect)
                        {
                            ikData[(int)target].enableShoulder = enableShoulder;
                            SynchroSet(target);
                            VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                            EditorApplication.delayCall += () =>
                            {
                                VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                            };
                        }
                        VeryAnimationControlWindow.ForceRepaint();
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            #region SpaceType
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Space", GUILayout.Width(64));
                EditorGUI.BeginChangeCheck();
                var spaceType = (AnimatorIKData.SpaceType)GUILayout.Toolbar((int)activeData.spaceType, IKSpaceTypeStrings, EditorStyles.miniButton);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(VAW, "Change IK Settings");
                    foreach (var target in ikTargetSelect)
                    {
                        ChangeSpaceType(target, spaceType);
                    }
                    SceneView.RepaintAll();
                    VeryAnimationControlWindow.instance.Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            #region Parent
            if (activeData.spaceType == AnimatorIKData.SpaceType.Local || activeData.spaceType == AnimatorIKData.SpaceType.Parent)
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Parent", GUILayout.Width(64));
                EditorGUI.BeginChangeCheck();
                if (activeData.spaceType == AnimatorIKData.SpaceType.Local)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    var parent = activeData.parentBoneIndex >= 0 ? VAW.VA.Bones[activeData.parentBoneIndex] : null;
                    EditorGUILayout.ObjectField(parent, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();
                }
                else if (activeData.spaceType == AnimatorIKData.SpaceType.Parent)
                {
                    var parent = EditorGUILayout.ObjectField(activeData.parent, typeof(GameObject), true) as GameObject;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Position");
                        foreach (var target in ikTargetSelect)
                        {
                            var data = ikData[(int)target];
                            var worldPosition = data.WorldPosition;
                            var worldRotation = data.WorldRotation;
                            data.parent = parent;
                            data.WorldPosition = worldPosition;
                            data.WorldRotation = worldRotation;
                            VAW.VA.SetSynchroIKtargetAnimatorIK(target);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            #region Position
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Position", GUILayout.Width(64));
                EditorGUI.BeginChangeCheck();
                var position = EditorGUILayout.Vector3Field("", activeData.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(VAW, "Change IK Position");
                    var move = position - activeData.position;
                    foreach (var target in ikTargetSelect)
                    {
                        ikData[(int)target].position += move;
                        VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                    }
                }
                if (activeData.spaceType == AnimatorIKData.SpaceType.Parent)
                {
                    if (GUILayout.Button("Reset", GUILayout.Width(44)))
                    {
                        Undo.RecordObject(VAW, "Change IK Position");
                        foreach (var target in ikTargetSelect)
                        {
                            ikData[(int)target].position = Vector3.zero;
                            VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            if (IKActiveTarget > IKTarget.Head)
            {
                #region Rotation
                if (!activeData.autoRotation)
                {
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    EditorGUILayout.LabelField("Rotation", GUILayout.Width(64));
                    EditorGUI.BeginChangeCheck();
                    var eulerAngles = EditorGUILayout.Vector3Field("", activeData.rotation.eulerAngles);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Rotation");
                        var move = eulerAngles - activeData.rotation.eulerAngles;
                        foreach (var target in ikTargetSelect)
                        {
                            if (target >= IKTarget.LeftHand && target <= IKTarget.RightFoot)
                            {
                                ikData[(int)target].rotation.eulerAngles += move;
                                VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                            }
                        }
                    }
                    if (activeData.spaceType == AnimatorIKData.SpaceType.Parent)
                    {
                        if (GUILayout.Button("Reset", GUILayout.Width(44)))
                        {
                            Undo.RecordObject(VAW, "Change IK Rotation");
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[(int)target].rotation = Quaternion.identity;
                                VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            #region Swivel
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Swivel", GUILayout.Width(64));
                EditorGUI.BeginChangeCheck();
                var swivelRotation = EditorGUILayout.Slider(activeData.swivelRotation, -180f, 180f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(VAW, "Change IK Swivel");
                    var move = swivelRotation - activeData.swivelRotation;
                    foreach (var target in ikTargetSelect)
                    {
                        var data = ikData[(int)target];
                        data.swivelRotation = Mathf.Repeat(data.swivelRotation + move + 180f, 360f) - 180f;
                        UpdateSwivelPosition(target);
                        VAW.VA.SetUpdateIKtargetAnimatorIK(target);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
#if VERYANIMATION_ANIMATIONRIGGING
            #region AnimationRiggingConstraint
            if (activeData.rigConstraint != null)
            {
                EditorGUILayout.BeginVertical(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation Rigging", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();

                GetAnimationRiggingConstraintBindings(IKActiveTarget, out int targetBoneIndex, out int hintBoneIndex);

                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                {
                    EditorGUILayout.LabelField("Write on Update", GUILayout.Width(128));
                    EditorGUI.BeginChangeCheck();
                    var updateFlags = (AnimatorIKData.WriteFlags)EditorGUILayout.EnumFlagsField(activeData.writeFlags);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change Animator IK Data");
                        foreach (var target in ikTargetSelect)
                        {
                            var data = ikData[(int)target];
                            data.writeFlags = updateFlags;
                        }
                        VAW.VA.SetAnimationWindowSynchroSelection();
                    }
                }
                EditorGUILayout.EndHorizontal();

                #region Weight
                {
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    if (GUILayout.Button(new GUIContent("Weight", "IRigConstraint.weight"), GUILayout.Width(128)))
                    {
                        var list = new List<EditorCurveBinding>();
                        foreach (var target in ikTargetSelect)
                        {
                            list.Add(ikData[(int)target].rigConstraintWeight);
                        }
                        VAW.VA.SetAnimationWindowSynchroSelection(list.ToArray());
                    }
                    {
                        var rigConstraint = activeData.rigConstraint as IRigConstraint;
                        EditorGUI.BeginChangeCheck();
                        var weight = EditorGUILayout.Slider(VAW.VA.GetAnimationValueCustomProperty(activeData.rigConstraintWeight), 0f, 1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var target in ikTargetSelect)
                            {
                                if (ikData[(int)target].rigConstraint != null)
                                    VAW.VA.SetAnimationValueCustomProperty(ikData[(int)target].rigConstraintWeight, weight);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                #region Position
                {
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    if (GUILayout.Button("Target Position", GUILayout.Width(128)))
                    {
                        var list = new List<EditorCurveBinding>();
                        foreach (var target in ikTargetSelect)
                        {
                            GetAnimationRiggingConstraintBindings(target, out int tBoneIndex, out int hBoneIndex);
                            for (int dof = 0; dof < 3; dof++)
                                list.Add(VAW.VA.AnimationCurveBindingTransformPosition(tBoneIndex, dof));
                        }
                        VAW.VA.SetAnimationWindowSynchroSelection(list.ToArray());
                    }
                    EditorGUI.BeginChangeCheck();
                    {
                        var localPosition = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var target in ikTargetSelect)
                            {
                                GetAnimationRiggingConstraintBindings(target, out int tBoneIndex, out int hBoneIndex);
                                VAW.VA.SetAnimationValueTransformPosition(tBoneIndex, localPosition);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                #region Rotation
                if (IKActiveTarget >= IKTarget.LeftHand && IKActiveTarget <= IKTarget.RightFoot)
                {
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    if (GUILayout.Button("Target Rotation", GUILayout.Width(128)))
                    {
                        var list = new List<EditorCurveBinding>();
                        foreach (var target in ikTargetSelect)
                        {
                            GetAnimationRiggingConstraintBindings(target, out int tBoneIndex, out int hBoneIndex);
                            for (int dof = 0; dof < 3; dof++)
                                list.Add(VAW.VA.AnimationCurveBindingTransformRotation(tBoneIndex, dof, URotationCurveInterpolation.Mode.Baked));
                            for (int dof = 0; dof < 3; dof++)
                                list.Add(VAW.VA.AnimationCurveBindingTransformRotation(tBoneIndex, dof, URotationCurveInterpolation.Mode.NonBaked));
                            for (int dof = 0; dof < 4; dof++)
                                list.Add(VAW.VA.AnimationCurveBindingTransformRotation(tBoneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                            for (int dof = 0; dof < 3; dof++)
                                list.Add(VAW.VA.AnimationCurveBindingTransformRotation(tBoneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));

                        }
                        VAW.VA.SetAnimationWindowSynchroSelection(list.ToArray());
                    }
                    EditorGUI.BeginChangeCheck();
                    {
                        var localEulerAngles = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueTransformRotation(targetBoneIndex).eulerAngles);
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var target in ikTargetSelect)
                            {
                                GetAnimationRiggingConstraintBindings(target, out int tBoneIndex, out int hBoneIndex);
                                VAW.VA.SetAnimationValueTransformRotation(tBoneIndex, Quaternion.Euler(localEulerAngles));
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                #region Position
                if (IKActiveTarget >= IKTarget.LeftHand && IKActiveTarget <= IKTarget.RightFoot)
                {
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    if (GUILayout.Button("Hint Position", GUILayout.Width(128)))
                    {
                        var list = new List<EditorCurveBinding>();
                        foreach (var target in ikTargetSelect)
                        {
                            GetAnimationRiggingConstraintBindings(target, out int tBoneIndex, out int hBoneIndex);
                            for (int dof = 0; dof < 3; dof++)
                                list.Add(VAW.VA.AnimationCurveBindingTransformPosition(hBoneIndex, dof));
                        }
                        VAW.VA.SetAnimationWindowSynchroSelection(list.ToArray());
                    }
                    EditorGUI.BeginChangeCheck();
                    {
                        var localPosition = EditorGUILayout.Vector3Field("", VAW.VA.GetAnimationValueTransformPosition(hintBoneIndex));
                        if (EditorGUI.EndChangeCheck())
                        {
                            foreach (var target in ikTargetSelect)
                            {
                                GetAnimationRiggingConstraintBindings(target, out int tBoneIndex, out int hBoneIndex);
                                VAW.VA.SetAnimationValueTransformPosition(hBoneIndex, localPosition);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
                EditorGUILayout.EndVertical();
            }
            #endregion
#endif
            #endregion
        }
        public void ControlGUI()
        {
            if (!VAW.VA.IsHuman) return;

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            if (ikReorderableList != null)
            {
                ikReorderableList.DoLayoutList();
                GUILayout.Space(-14);
                if (advancedFoldout && ikReorderableList.index >= 0 && ikReorderableList.index < ikData.Length)
                {
                    var target = ikReorderableList.index;
                    EditorGUI.BeginDisabledGroup(!ikData[target].enable);
#if !VERYANIMATION_ANIMATIONRIGGING
                    if ((IKTarget)target == IKTarget.Head)
#endif
                    {
                        advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced", true);
                        if ((IKTarget)target == IKTarget.Head)
                        {
                            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Look At Weight");
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Reset"))
                                {
                                    Undo.RecordObject(VAW, "Change Animator IK Data");
                                    ikData[target].headWeight = 1f;
                                    ikData[target].eyesWeight = 0f;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel++;
                            {
                                EditorGUI.BeginChangeCheck();
                                var weight = EditorGUILayout.Slider("Head Weight", ikData[target].headWeight, 0f, 1f);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(VAW, "Change Animator IK Data");
                                    ikData[target].headWeight = weight;
                                    ikData[target].eyesWeight = 1f - ikData[target].headWeight;
                                }
                            }
                            {
                                EditorGUI.BeginChangeCheck();
                                var weight = EditorGUILayout.Slider("Eyes Weight", ikData[target].eyesWeight, 0f, 1f);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(VAW, "Change Animator IK Data");
                                    ikData[target].eyesWeight = weight;
                                    ikData[target].headWeight = 1f - ikData[target].eyesWeight;
                                }
                            }
                            EditorGUI.indentLevel--;
                            EditorGUILayout.EndVertical();
                        }
                        else if ((IKTarget)target == IKTarget.LeftHand || (IKTarget)target == IKTarget.RightHand)
                        {
                            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
                            {
                                var boneShoulder = (IKTarget)target == IKTarget.LeftHand ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder;
                                EditorGUI.BeginDisabledGroup(VAW.VA.HumanoidBones[(int)boneShoulder] == null || !ikData[target].enableShoulder);
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var param = EditorGUILayout.Slider("Shoulder Sensitivity Y", ikData[target].shoulderSensitivityY, 0f, 1f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Animator IK Data");
                                        ikData[target].shoulderSensitivityY = param;
                                    }
                                }
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var param = EditorGUILayout.Slider("Shoulder Sensitivity Z", ikData[target].shoulderSensitivityZ, 0f, 1f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Animator IK Data");
                                        ikData[target].shoulderSensitivityZ = param;
                                    }
                                }
                                EditorGUI.EndDisabledGroup();
                            }
                            EditorGUILayout.EndVertical();
                        }
#if VERYANIMATION_ANIMATIONRIGGING
                        #region AnimationRigging
                        {
                            EditorGUILayout.BeginHorizontal(VAW.GuiStyleSkinBox);
                            EditorGUILayout.LabelField("Animation Rigging Constraint");
                            GUILayout.FlexibleSpace();
                            {
                                EditorGUI.BeginDisabledGroup(ikData[target].rigConstraint != null);
                                if (GUILayout.Button("Create"))
                                {
                                    EditorApplication.delayCall += () =>
                                    {
                                        Undo.RecordObject(VAW, "Change Animation Rigging Constraint");
                                        CreateAnimationRiggingConstraint((IKTarget)target);
                                    };
                                }
                                EditorGUI.EndDisabledGroup();
                            }
                            EditorGUILayout.Space();
                            {
                                EditorGUI.BeginDisabledGroup(ikData[target].rigConstraint == null);
                                if (GUILayout.Button("Delete"))
                                {
                                    EditorApplication.delayCall += () =>
                                    {
                                        Undo.RecordObject(VAW, "Change Animation Rigging Constraint");
                                        DeleteAnimationRiggingConstraint((IKTarget)target);
                                    };
                                }
                                EditorGUI.EndDisabledGroup();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
#endif
                    }
                    EditorGUI.EndDisabledGroup();
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(Language.GetContent(Language.Help.AnimatorIKChangeAll)))
                    {
                        Undo.RecordObject(VAW, "Change Animator IK Data");
                        bool flag = !ikData.Any(v => !v.enable);
                        for (int target = 0; target < ikData.Length; target++)
                        {
                            ikData[target].enable = !flag;
                            if (ikData[target].enable)
                            {
                                SynchroSet((IKTarget)target);
                            }
                        }
                        VAW.VA.SetAnimationWindowSynchroSelection();
                        VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button(Language.GetContent(Language.Help.AnimatorIKSelectAll)))
                    {
                        var list = new List<IKTarget>();
                        for (int target = 0; target < ikData.Length; target++)
                        {
                            if (ikData[target].enable)
                                list.Add((IKTarget)target);
                        }
                        VAW.VA.SelectIKTargets(list.ToArray(), null);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void AnimatorOnAnimatorIK(int layerIndex)
        {
            var animator = VAW.VA.Skeleton.Animator;

            {
                var data = ikData[(int)IKTarget.Head];
                if (ikData[(int)IKTarget.Head].IsUpdate)
                {
                    GetCalcWorldTransform(IKTarget.Head, out Vector3 position, out _, out _);

                    animator.SetLookAtPosition(position);
                    animator.SetLookAtWeight(1f, 0f, data.headWeight, data.eyesWeight, 0f);
                }
                else
                {
                    animator.SetLookAtWeight(0f);
                }
            }
            {
                var data = ikData[(int)IKTarget.LeftHand];
                if (data.IsUpdate)
                {
                    GetCalcWorldTransform(IKTarget.LeftHand, out Vector3 position, out Quaternion rotation, out Vector3 hintPosition);

                    animator.SetIKPosition(AvatarIKGoal.LeftHand, position);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, rotation);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                    animator.SetIKHintPosition(AvatarIKHint.LeftElbow, hintPosition);
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1f);
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
                }
            }
            {
                var data = ikData[(int)IKTarget.RightHand];
                if (data.IsUpdate)
                {
                    GetCalcWorldTransform(IKTarget.RightHand, out Vector3 position, out Quaternion rotation, out Vector3 hintPosition);

                    animator.SetIKPosition(AvatarIKGoal.RightHand, position);
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, rotation);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                    animator.SetIKHintPosition(AvatarIKHint.RightElbow, hintPosition);
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 1f);
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
                }
            }
            {
                var data = ikData[(int)IKTarget.LeftFoot];
                if (data.IsUpdate)
                {
                    GetCalcWorldTransform(IKTarget.LeftFoot, out Vector3 position, out Quaternion rotation, out Vector3 hintPosition);

                    animator.SetIKPosition(AvatarIKGoal.LeftFoot, position);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
                    animator.SetIKRotation(AvatarIKGoal.LeftFoot, rotation);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
                    animator.SetIKHintPosition(AvatarIKHint.LeftKnee, hintPosition);
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 1f);
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
                    animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
                }
            }
            {
                var data = ikData[(int)IKTarget.RightFoot];
                if (data.IsUpdate)
                {
                    GetCalcWorldTransform(IKTarget.RightFoot, out Vector3 position, out Quaternion rotation, out Vector3 hintPosition);

                    animator.SetIKPosition(AvatarIKGoal.RightFoot, position);
                    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
                    animator.SetIKRotation(AvatarIKGoal.RightFoot, rotation);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
                    animator.SetIKHintPosition(AvatarIKHint.RightKnee, hintPosition);
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 1f);
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
                }
            }
        }
        private void GetCalcWorldTransform(IKTarget target, out Vector3 position, out Quaternion rotation, out Vector3 hintPosition)
        {
            var data = ikData[(int)target];

            var worldToLocalRotation = Quaternion.Inverse(VAW.VA.TransformPoseSave.StartRotation);
            var worldToLocalMatrix = VAW.VA.TransformPoseSave.StartMatrix.inverse;

            position = worldToLocalMatrix.MultiplyPoint3x4(data.WorldPosition);
            rotation = worldToLocalRotation * data.WorldRotation;

            hintPosition = worldToLocalMatrix.MultiplyPoint3x4(data.swivelPosition);
        }

        private void Reset(IKTarget target)
        {
            var data = ikData[(int)target];
            switch (target)
            {
                case IKTarget.Head:
                    {
                        var t = VAW.VA.Skeleton.GameObject.transform;
                        Vector3 vec = data.WorldPosition - t.position;
                        var normal = t.rotation * Vector3.right;
                        var dot = Vector3.Dot(vec, normal);
                        data.WorldPosition -= normal * dot;
                        data.swivelRotation = 0f;
                    }
                    break;
                case IKTarget.LeftHand:
                    {
                        var posA = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.LeftUpperArm].transform.position;
                        var posB = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.LeftHand].transform.position;
                        var posC = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.LeftLowerArm].transform.position;
                        var up = data.WorldPosition - Vector3.Lerp(posA, posB, 0.5f);
                        data.WorldRotation = Quaternion.LookRotation(posB - posC, up);
                    }
                    break;
                case IKTarget.RightHand:
                    {
                        var posA = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.RightUpperArm].transform.position;
                        var posB = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.RightHand].transform.position;
                        var posC = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.RightLowerArm].transform.position;
                        var up = data.WorldPosition - Vector3.Lerp(posA, posB, 0.5f);
                        data.WorldRotation = Quaternion.LookRotation(posB - posC, up);
                    }
                    break;
                case IKTarget.LeftFoot:
                    {
                        var limitSign = VAW.VA.GetHumanoidAvatarLimitSign(HumanBodyBones.Hips);
                        var rot = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Hips].transform.rotation * VAW.VA.GetHumanoidAvatarPostRotation(HumanBodyBones.Hips) * Quaternion.Euler(90f, 90f, 0);
                        {
                            var vec = rot * Vector3.forward * limitSign.z;
                            rot = Quaternion.LookRotation((new Vector3(vec.x, 0f, vec.z)).normalized, Vector3.up);
                        }
                        data.WorldRotation = rot;
                    }
                    break;
                case IKTarget.RightFoot:
                    {
                        var limitSign = VAW.VA.GetHumanoidAvatarLimitSign(HumanBodyBones.Hips);
                        var rot = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Hips].transform.rotation * VAW.VA.GetHumanoidAvatarPostRotation(HumanBodyBones.Hips) * Quaternion.Euler(90f, 90f, 0);
                        {
                            var vec = rot * Vector3.forward * limitSign.z;
                            rot = Quaternion.LookRotation((new Vector3(vec.x, 0f, vec.z)).normalized, Vector3.up);
                        }
                        data.WorldRotation = rot;
                    }
                    break;
            }
            UpdateOptionPosition(target);
            UpdateSwivelPosition(target);
            VAW.VA.SetUpdateIKtargetAnimatorIK(target);
        }
        private void ChangeSpaceType(IKTarget target, AnimatorIKData.SpaceType spaceType)
        {
            if (target < 0 || target >= IKTarget.Total) return;
            var data = ikData[(int)target];
            if (data.spaceType == spaceType) return;
            var position = data.WorldPosition;
            var rotation = data.WorldRotation;
            data.spaceType = spaceType;
            data.WorldPosition = position;
            data.WorldRotation = rotation;

            SetSynchroIKtargetAnimatorIK(target);
        }

        public IKTarget IsIKBone(HumanBodyBones hi)
        {
            if (ikData[(int)IKTarget.Head].enable)
            {
                if (ikData[(int)IKTarget.Head].headWeight > 0f)
                    if (hi == HumanBodyBones.Head || hi == HumanBodyBones.Neck)
                        return IKTarget.Head;
                if (ikData[(int)IKTarget.Head].eyesWeight > 0f)
                    if (hi == HumanBodyBones.LeftEye || hi == HumanBodyBones.RightEye)
                        return IKTarget.Head;
            }
            if (ikData[(int)IKTarget.LeftHand].enable)
            {
                if (hi == HumanBodyBones.LeftHand || hi == HumanBodyBones.LeftLowerArm || hi == HumanBodyBones.LeftUpperArm)
                    return IKTarget.LeftHand;
                else if (hi == HumanBodyBones.LeftShoulder)
                {
                    if (VAW.VA.HumanoidBones[(int)HumanBodyBones.LeftShoulder] == null)
                        return IKTarget.LeftHand;
                    else if (ikData[(int)IKTarget.LeftHand].enableShoulder)
                        return IKTarget.LeftHand;
                }
            }
            if (ikData[(int)IKTarget.RightHand].enable)
            {
                if (hi == HumanBodyBones.RightHand || hi == HumanBodyBones.RightLowerArm || hi == HumanBodyBones.RightUpperArm)
                    return IKTarget.RightHand;
                else if (hi == HumanBodyBones.RightShoulder)
                {
                    if (VAW.VA.HumanoidBones[(int)HumanBodyBones.RightShoulder] == null)
                        return IKTarget.RightHand;
                    else if (ikData[(int)IKTarget.RightHand].enableShoulder)
                        return IKTarget.RightHand;
                }
            }
            if (ikData[(int)IKTarget.LeftFoot].enable)
            {
                if (hi == HumanBodyBones.LeftFoot || hi == HumanBodyBones.LeftLowerLeg || hi == HumanBodyBones.LeftUpperLeg)
                    return IKTarget.LeftFoot;
            }
            if (ikData[(int)IKTarget.RightFoot].enable)
            {
                if (hi == HumanBodyBones.RightFoot || hi == HumanBodyBones.RightLowerLeg || hi == HumanBodyBones.RightUpperLeg)
                    return IKTarget.RightFoot;
            }
            return IKTarget.None;
        }

        public void ChangeTargetIK(IKTarget target)
        {
            Undo.RecordObject(VAW, "Change IK");
            if (ikData[(int)target].enable)
            {
                var selectGameObjects = new List<GameObject>();
                switch (target)
                {
                    case IKTarget.Head:
                        ikData[(int)target].enable = false;
                        selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.Head]);
                        break;
                    case IKTarget.LeftHand:
                        ikData[(int)target].enable = false;
                        selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.LeftHand]);
                        break;
                    case IKTarget.RightHand:
                        ikData[(int)target].enable = false;
                        selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.RightHand]);
                        break;
                    case IKTarget.LeftFoot:
                        ikData[(int)target].enable = false;
                        selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.LeftFoot]);
                        break;
                    case IKTarget.RightFoot:
                        ikData[(int)target].enable = false;
                        selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.RightFoot]);
                        break;
                }
                VAW.VA.SelectGameObjects(selectGameObjects.ToArray());
            }
            else
            {
                ikData[(int)target].enable = true;
                SynchroSet(target);
                VAW.VA.SelectAnimatorIKTargetPlusKey(target);
            }
        }
        public bool ChangeSelectionIK()
        {
            Undo.RecordObject(VAW, "Change IK");
            bool changed = false;
            if (ikTargetSelect != null && ikTargetSelect.Length > 0)
            {
                var selectGameObjects = new List<GameObject>();
                foreach (var target in ikTargetSelect)
                {
                    switch (target)
                    {
                        case IKTarget.Head:
                            ikData[(int)target].enable = false;
                            selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.Head]);
                            changed = true;
                            break;
                        case IKTarget.LeftHand:
                            ikData[(int)target].enable = false;
                            selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.LeftHand]);
                            changed = true;
                            break;
                        case IKTarget.RightHand:
                            ikData[(int)target].enable = false;
                            selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.RightHand]);
                            changed = true;
                            break;
                        case IKTarget.LeftFoot:
                            ikData[(int)target].enable = false;
                            selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.LeftFoot]);
                            changed = true;
                            break;
                        case IKTarget.RightFoot:
                            ikData[(int)target].enable = false;
                            selectGameObjects.Add(VAW.VA.HumanoidBones[(int)HumanBodyBones.RightFoot]);
                            changed = true;
                            break;
                    }
                }
                if (changed)
                    VAW.VA.SelectGameObjects(selectGameObjects.ToArray());
            }
            else
            {
                var selectIkTargets = new HashSet<IKTarget>();
                foreach (var humanoidIndex in VAW.VA.SelectionGameObjectsHumanoidIndex())
                {
                    var target = HumanBonesUpdateAnimatorIK[(int)humanoidIndex];
                    if (target < 0 || target >= IKTarget.Total)
                        continue;
                    selectIkTargets.Add(target);
                    changed = true;
                }
                if (changed)
                {
                    foreach (var target in selectIkTargets)
                    {
                        ikData[(int)target].enable = true;
                        SynchroSet(target);
                    }
                    VAW.VA.SelectIKTargets(selectIkTargets.ToArray(), null);
                }
            }
            return changed;
        }

        public void SetUpdateIKtargetBone(int boneIndex)
        {
            if (boneIndex < 0)
                return;
            {
                var humanoidIndex = VAW.VA.BoneIndex2humanoidIndex[boneIndex];
                if (humanoidIndex >= 0)
                {
                    SetUpdateIKtargetAnimatorIK(HumanBonesUpdateAnimatorIK[(int)humanoidIndex]);
                }
            }
            SetUpdateLinkedIKTarget(boneIndex);
        }
        public void SetUpdateIKtargetAnimatorIK(IKTarget target, bool force = false)
        {
            if (ikData == null || target < 0)
                return;
            if (target == IKTarget.Total)
            {
                SetUpdateIKtargetAll();
            }
            else if (ikData[(int)target].enable)
            {
                if (force || ikData[(int)target].spaceType != AnimatorIKData.SpaceType.Local)
                {
                    ikData[(int)target].updateIKtarget = true;

                    SetUpdateLinkedIKTarget(ikData[(int)target].rootBoneIndex);
                }
                else
                {
                    SetSynchroIKtargetAnimatorIK(target);
                }
            }
        }
        private void SetUpdateLinkedIKTarget(int boneIndex)
        {
            if (boneIndex < 0)
                return;
            foreach (var data in ikData)
            {
                if (data.updateIKtarget)
                    continue;
                if (data.spaceType == AnimatorIKData.SpaceType.Parent &&
                    data.parentBoneIndex >= 0)
                {
                    var index = data.parentBoneIndex;
                    while (index >= 0)
                    {
                        if (boneIndex == index)
                        {
                            data.updateIKtarget = true;
                            break;
                        }
                        index = VAW.VA.ParentBoneIndexes[index];
                    }
                }
            }
        }
        public void ResetUpdateIKtargetAll()
        {
            if (ikData == null) return;
            foreach (var data in ikData)
            {
                data.updateIKtarget = false;
            }
        }
        public void SetUpdateIKtargetAll()
        {
            if (ikData == null) return;
            for (var i = 0; i < ikData.Length; i++)
            {
                if (!ikData[i].enable)
                    continue;
                SetUpdateIKtargetAnimatorIK((IKTarget)i);
            }
        }
        public bool GetUpdateIKtargetAll()
        {
            if (ikData == null) return false;
            foreach (var data in ikData)
            {
                if (data.IsUpdate)
                    return true;
            }
            return false;
        }

        public void SetSynchroIKtargetBone(int boneIndex)
        {
            if (boneIndex < 0) return;
            var humanoidIndex = VAW.VA.BoneIndex2humanoidIndex[boneIndex];
            if (humanoidIndex < 0) return;
            SetSynchroIKtargetAnimatorIK(HumanBonesUpdateAnimatorIK[(int)humanoidIndex]);
        }
        public void SetSynchroIKtargetAnimatorIK(IKTarget target)
        {
            if (ikData == null || target < 0)
                return;
            if (target == IKTarget.Total)
            {
                SetSynchroIKtargetAll();
                return;
            }
            if (!ikData[(int)target].updateIKtarget)
                ikData[(int)target].synchroIKtarget = true;
        }
        public void ResetSynchroIKtargetAll()
        {
            if (ikData == null) return;
            foreach (var data in ikData)
            {
                data.synchroIKtarget = false;
            }
        }
        public void SetSynchroIKtargetAll()
        {
            if (ikData == null) return;
            foreach (var data in ikData)
            {
                if (!data.updateIKtarget)
                    data.synchroIKtarget = true;
            }
        }
        public bool GetSynchroIKtargetAll()
        {
            if (ikData == null) return false;
            foreach (var data in ikData)
            {
                if (data.enable && data.synchroIKtarget)
                    return true;
            }
            return false;
        }

        public HumanBodyBones GetStartHumanoidIndex(IKTarget target)
        {
            var humanoidIndex = HumanBodyBones.Hips;
            switch ((IKTarget)target)
            {
                case IKTarget.Head: humanoidIndex = VAW.VA.Skeleton.HumanoidBones[(int)HumanBodyBones.Neck] != null ? HumanBodyBones.Neck : HumanBodyBones.Head; break;
                case IKTarget.LeftHand: humanoidIndex = HumanBodyBones.LeftUpperArm; break;
                case IKTarget.RightHand: humanoidIndex = HumanBodyBones.RightUpperArm; break;
                case IKTarget.LeftFoot: humanoidIndex = HumanBodyBones.LeftUpperLeg; break;
                case IKTarget.RightFoot: humanoidIndex = HumanBodyBones.RightUpperLeg; break;
            }
            return humanoidIndex;
        }
        public HumanBodyBones GetCenterHumanoidIndex(IKTarget target)
        {
            var humanoidIndex = HumanBodyBones.Hips;
            switch ((IKTarget)target)
            {
                case IKTarget.Head: break;
                case IKTarget.LeftHand: humanoidIndex = HumanBodyBones.LeftLowerArm; break;
                case IKTarget.RightHand: humanoidIndex = HumanBodyBones.RightLowerArm; break;
                case IKTarget.LeftFoot: humanoidIndex = HumanBodyBones.LeftLowerLeg; break;
                case IKTarget.RightFoot: humanoidIndex = HumanBodyBones.RightLowerLeg; break;
            }
            return humanoidIndex;
        }
        public HumanBodyBones GetEndHumanoidIndex(IKTarget target)
        {
            var humanoidIndex = HumanBodyBones.Hips;
            switch ((IKTarget)target)
            {
                case IKTarget.Head: humanoidIndex = HumanBodyBones.Head; break;
                case IKTarget.LeftHand: humanoidIndex = HumanBodyBones.LeftHand; break;
                case IKTarget.RightHand: humanoidIndex = HumanBodyBones.RightHand; break;
                case IKTarget.LeftFoot: humanoidIndex = HumanBodyBones.LeftFoot; break;
                case IKTarget.RightFoot: humanoidIndex = HumanBodyBones.RightFoot; break;
            }
            return humanoidIndex;
        }

        public List<HumanBodyBones> SelectionAnimatorIKTargetsHumanoidIndexes()
        {
            var list = new List<HumanBodyBones>();
            if (ikTargetSelect != null)
            {
                foreach (var ikTarget in ikTargetSelect)
                {
                    for (int i = 0; i < HumanBonesUpdateAnimatorIK.Length; i++)
                    {
                        if (ikTarget == HumanBonesUpdateAnimatorIK[i])
                            list.Add((HumanBodyBones)i);
                    }
                }
            }
            return list;
        }

#if VERYANIMATION_ANIMATIONRIGGING
        #region AnimationRigging
        private void UpdateAnimationRiggingConstraint(IKTarget target)
        {
            ikData[(int)target].rigConstraint = null;
            if (VAW.VA.AnimationRigging.IsValid)
            {
                for (int i = 0; i < VAW.VA.AnimationRigging.ArRig.transform.childCount; i++)
                {
                    var rigConstraints = VAW.VA.AnimationRigging.ArRig.transform.GetChild(i).GetComponents<IRigConstraint>();
                    foreach (var rigConstraint in rigConstraints)
                    {
                        MonoBehaviour mono = null;
                        switch (target)
                        {
                            case IKTarget.Head:
                                mono = rigConstraint as MultiAimConstraint;
                                break;
                            case IKTarget.LeftHand:
                            case IKTarget.RightHand:
                            case IKTarget.LeftFoot:
                            case IKTarget.RightFoot:
                                mono = rigConstraint as TwoBoneIKConstraint;
                                break;
                        }
                        if (mono == null || mono.name != GetAnimationRiggingConstraintName(target))
                            continue;
                        ikData[(int)target].rigConstraint = mono;
                        var text = mono.GetType().ToString();
                        {
                            var lastIndex = text.LastIndexOf('.');
                            if (lastIndex >= 0)
                                text = text[(lastIndex + 1)..];
                        }
                        ikData[(int)target].rigConstraintGUIContent = new GUIContent(text, mono.GetType().ToString());

                        var path = AnimationUtility.CalculateTransformPath(mono.transform, VAW.GameObject.transform);
                        switch (target)
                        {
                            case IKTarget.Head:
                                ikData[(int)target].rigConstraintWeight = EditorCurveBinding.FloatCurve(path, typeof(MultiAimConstraint), "m_Weight");
                                break;
                            case IKTarget.LeftHand:
                            case IKTarget.RightHand:
                            case IKTarget.LeftFoot:
                            case IKTarget.RightFoot:
                                ikData[(int)target].rigConstraintWeight = EditorCurveBinding.FloatCurve(path, typeof(TwoBoneIKConstraint), "m_Weight");
                                break;
                        }
                    }
                }
            }
        }
        private bool CreateAnimationRiggingConstraint(IKTarget target)
        {
            DeleteAnimationRiggingConstraint(target);

            VAW.VA.StopRecording();

            if (!VAW.VA.AnimationRigging.IsValid)
            {
                VAW.VA.AnimationRigging.Enable();
            }
            if (!VAW.VA.AnimationRigging.IsValid)
                return false;

            var listIndex = ikReorderableList.index;
            {
                var go = AddAnimationRiggingConstraint(VAW.GameObject, target);
                UpdateAnimationRiggingConstraint(target);
                VAW.VA.OnHierarchyWindowChanged();
                Selection.activeGameObject = go;
                VAW.VA.SetUpdateSampleAnimation();
                VAW.VA.SetSynchroIKtargetAll();
            }
            ikReorderableList.index = listIndex;

            #region MirrorMapping
            if (IKTargetMirror[(int)target] != IKTarget.None &&
                ikData[(int)IKTargetMirror[(int)target]].rigConstraint != null)
            {
                switch (target)
                {
                    case IKTarget.LeftHand:
                    case IKTarget.RightHand:
                    case IKTarget.LeftFoot:
                    case IKTarget.RightFoot:
                        {
                            var constraint = ikData[(int)target].rigConstraint as TwoBoneIKConstraint;
                            var mirrorConstraint = ikData[(int)IKTargetMirror[(int)target]].rigConstraint as TwoBoneIKConstraint;
                            if (constraint != null && mirrorConstraint != null)
                            {
                                {
                                    var boneIndex = VAW.VA.BonesIndexOf(constraint.gameObject);
                                    var mirrorBoneIndex = VAW.VA.BonesIndexOf(mirrorConstraint.gameObject);
                                    VAW.VA.ChangeBonesMirror(boneIndex, mirrorBoneIndex);
                                }
                                if (constraint.data.target != null)
                                {
                                    var targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.target.gameObject);
                                    var mirrorTargetBoneIndex = VAW.VA.BonesIndexOf(mirrorConstraint.data.target.gameObject);
                                    VAW.VA.ChangeBonesMirror(targetBoneIndex, mirrorTargetBoneIndex);
                                }
                                if (constraint.data.hint != null)
                                {
                                    var hintBoneIndex = VAW.VA.BonesIndexOf(constraint.data.hint.gameObject);
                                    var mirrorHintBoneIndex = VAW.VA.BonesIndexOf(mirrorConstraint.data.hint.gameObject);
                                    VAW.VA.ChangeBonesMirror(hintBoneIndex, mirrorHintBoneIndex);
                                }
                            }
                        }
                        break;
                }
            }
            #endregion

            return true;
        }
        private void DeleteAnimationRiggingConstraint(IKTarget target)
        {
            if (ikData[(int)target].rigConstraint == null)
                return;

            VAW.VA.StopRecording();

            var listIndex = ikReorderableList.index;
            {
                Selection.activeGameObject = ikData[(int)target].rigConstraint.gameObject;
                Unsupported.DeleteGameObjectSelection();
                if (ikData[(int)target].rigConstraint != null)
                    return;
                ikData[(int)target].rigConstraint = null;
                switch (target)
                {
                    case IKTarget.Head:
                        break;
                    case IKTarget.LeftHand:
                        VAW.VA.AnimationRigging.VaRig.basePoseLeftHand.Reset();
                        break;
                    case IKTarget.RightHand:
                        VAW.VA.AnimationRigging.VaRig.basePoseRightHand.Reset();
                        break;
                    case IKTarget.LeftFoot:
                        VAW.VA.AnimationRigging.VaRig.basePoseLeftFoot.Reset();
                        break;
                    case IKTarget.RightFoot:
                        VAW.VA.AnimationRigging.VaRig.basePoseRightFoot.Reset();
                        break;
                }
                VAW.VA.OnHierarchyWindowChanged();
            }
            ikReorderableList.index = listIndex;
        }
        public void WriteAnimationRiggingConstraint(IKTarget target, float time)
        {
            if (ikData[(int)target].rigConstraint == null || !ikData[(int)target].enable)
                return;

            var worldToLocalRotation = Quaternion.Inverse(VAW.VA.TransformPoseSave.StartRotation);
            var worldToLocalMatrix = VAW.VA.TransformPoseSave.StartMatrix.inverse;

            var position = ikData[(int)target].WorldPosition;
            var rotation = ikData[(int)target].WorldRotation;
            var hintPosition = ikData[(int)target].swivelPosition;
            {
                position = worldToLocalMatrix.MultiplyPoint3x4(position);
                rotation = worldToLocalRotation * rotation;
                hintPosition = worldToLocalMatrix.MultiplyPoint3x4(hintPosition);
            }

            #region FeetBottomHeight
            switch (target)
            {
                case IKTarget.LeftFoot: position += new Vector3(0f, -VAW.VA.Skeleton.Animator.leftFeetBottomHeight, 0f); break;
                case IKTarget.RightFoot: position += new Vector3(0f, -VAW.VA.Skeleton.Animator.rightFeetBottomHeight, 0f); break;
            }
            #endregion

            switch (target)
            {
                case IKTarget.Head:
                    {
                        var constraint = ikData[(int)target].rigConstraint as MultiAimConstraint;
                        if (constraint != null && constraint.data.sourceObjects.Count > 0 && constraint.data.sourceObjects[0].transform != null)
                        {
                            var constraintTransform = VAW.VA.Bones[VAW.VA.BonesIndexOf(constraint.gameObject)].transform;
                            var parentMatrix = (worldToLocalMatrix * constraintTransform.localToWorldMatrix).inverse;
                            var targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.sourceObjects[0].transform.gameObject);
                            var localPosition = parentMatrix.MultiplyPoint3x4(position);
                            if (ikData[(int)target].writeFlags.HasFlag(AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                            {
                                VAW.VA.SetAnimationValueTransformPosition(targetBoneIndex, localPosition, time);
                            }
                        }
                    }
                    break;
                case IKTarget.LeftHand:
                case IKTarget.RightHand:
                case IKTarget.LeftFoot:
                case IKTarget.RightFoot:
                    {
                        var constraint = ikData[(int)target].rigConstraint as TwoBoneIKConstraint;
                        if (constraint != null)
                        {
                            var constraintTransform = VAW.VA.Bones[VAW.VA.BonesIndexOf(constraint.gameObject)].transform;
                            var parentRotation = Quaternion.Inverse(worldToLocalRotation * constraintTransform.rotation);
                            var parentMatrix = (worldToLocalMatrix * constraintTransform.localToWorldMatrix).inverse;
                            if (constraint.data.target != null)
                            {
                                var targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.target.gameObject);
                                var localPosition = parentMatrix.MultiplyPoint3x4(position);
                                var localRotation = parentRotation * rotation;
                                if (ikData[(int)target].writeFlags.HasFlag(AnimatorIKData.WriteFlags.AnimationRiggingTarget))
                                {
                                    VAW.VA.SetAnimationValueTransformPosition(targetBoneIndex, localPosition, time);
                                    VAW.VA.SetAnimationValueTransformRotation(targetBoneIndex, localRotation, time, URotationCurveInterpolation.Mode.RawEuler);
                                }
                            }
                            if (constraint.data.hint != null)
                            {
                                var hintBoneIndex = VAW.VA.BonesIndexOf(constraint.data.hint.gameObject);
                                var localPosition = parentMatrix.MultiplyPoint3x4(hintPosition);
                                if (ikData[(int)target].writeFlags.HasFlag(AnimatorIKData.WriteFlags.AnimationRiggingHint))
                                {
                                    VAW.VA.SetAnimationValueTransformPosition(hintBoneIndex, localPosition, time);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public EditorCurveBinding[] GetAnimationRiggingConstraintBindings(IKTarget target)
        {
            var bindings = new List<EditorCurveBinding>();
            switch (target)
            {
                case IKTarget.Head:
                    {
                        var constraint = ikData[(int)target].rigConstraint as MultiAimConstraint;
                        if (constraint != null && constraint.data.sourceObjects.Count > 0 && constraint.data.sourceObjects[0].transform != null)
                        {
                            var boneIndex = VAW.VA.BonesIndexOf(constraint.data.sourceObjects[0].transform.gameObject);
                            for (int dof = 0; dof < 3; dof++)
                                bindings.Add(VAW.VA.AnimationCurveBindingTransformPosition(boneIndex, dof));
                        }
                    }
                    break;
                case IKTarget.LeftHand:
                case IKTarget.RightHand:
                case IKTarget.LeftFoot:
                case IKTarget.RightFoot:
                    {
                        var constraint = ikData[(int)target].rigConstraint as TwoBoneIKConstraint;
                        if (constraint != null)
                        {
                            if (constraint.data.target != null)
                            {
                                var boneIndex = VAW.VA.BonesIndexOf(constraint.data.target.gameObject);
                                for (int dof = 0; dof < 3; dof++)
                                    bindings.Add(VAW.VA.AnimationCurveBindingTransformPosition(boneIndex, dof));
                                for (int dof = 0; dof < 3; dof++)
                                    bindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.Baked));
                                for (int dof = 0; dof < 3; dof++)
                                    bindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.NonBaked));
                                for (int dof = 0; dof < 4; dof++)
                                    bindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawQuaternions));
                                for (int dof = 0; dof < 3; dof++)
                                    bindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(boneIndex, dof, URotationCurveInterpolation.Mode.RawEuler));
                            }
                            if (constraint.data.hint != null)
                            {
                                var boneIndex = VAW.VA.BonesIndexOf(constraint.data.hint.gameObject);
                                for (int dof = 0; dof < 3; dof++)
                                    bindings.Add(VAW.VA.AnimationCurveBindingTransformPosition(boneIndex, dof));
                            }
                        }
                    }
                    break;
            }
            return bindings.ToArray();
        }

        private void GetAnimationRiggingConstraintBindings(IKTarget target, out int targetBoneIndex, out int hintBoneIndex)
        {
            targetBoneIndex = -1;
            hintBoneIndex = -1;
            switch (target)
            {
                case IKTarget.Head:
                    {
                        var constraint = ikData[(int)target].rigConstraint as MultiAimConstraint;
                        if (constraint != null && constraint.data.sourceObjects.Count > 0 && constraint.data.sourceObjects[0].transform != null)
                        {
                            targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.sourceObjects[0].transform.gameObject);
                        }
                    }
                    break;
                case IKTarget.LeftHand:
                case IKTarget.RightHand:
                case IKTarget.LeftFoot:
                case IKTarget.RightFoot:
                    {
                        var constraint = ikData[(int)target].rigConstraint as TwoBoneIKConstraint;
                        if (constraint != null)
                        {
                            if (constraint.data.target != null)
                            {
                                targetBoneIndex = VAW.VA.BonesIndexOf(constraint.data.target.gameObject);
                            }
                            if (constraint.data.hint != null)
                            {
                                hintBoneIndex = VAW.VA.BonesIndexOf(constraint.data.hint.gameObject);
                            }
                        }
                    }
                    break;
            }
        }

        public static string GetAnimationRiggingConstraintName(IKTarget target)
        {
            return string.Format("{0}_{1}", AnimationRigging.AnimationRiggingRigName, target);
        }
        public static GameObject GetAnimationRiggingConstraint(GameObject gameObject, IKTarget target)
        {
            var vaRig = AnimationRigging.GetVeryAnimationRig(gameObject);
            if (vaRig == null)
                return null;
            if (!vaRig.TryGetComponent<Rig>(out _))
                return null;
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null || animator.avatar == null)
                return null;
            var child = vaRig.transform.Find(GetAnimationRiggingConstraintName(target));
            if (child == null)
                return null;
            if (target == IKTarget.Head)
            {
                if (child.GetComponent<MultiAimConstraint>() == null)
                    return null;
            }
            else
            {
                if (child.GetComponent<TwoBoneIKConstraint>() == null)
                    return null;
            }
            return child.gameObject;
        }
        public static GameObject AddAnimationRiggingConstraint(GameObject gameObject, IKTarget target)
        {
            var vaRig = AnimationRigging.GetVeryAnimationRig(gameObject);
            if (vaRig == null)
                return null;
            if (!vaRig.TryGetComponent<Rig>(out var rig))
                return null;
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null || animator.avatar == null)
                return null;

            if (!animator.isInitialized)
                animator.Rebind();

            var uAvatar = new UAvatar();

            var go = new GameObject(GetAnimationRiggingConstraintName(target));
            go.transform.SetParent(rig.transform);
#if UNITY_2022_3_OR_NEWER
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
#endif
            go.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(go, "");
            var targetObj = new GameObject(go.name + "_Target");
            targetObj.transform.SetParent(go.transform);
#if UNITY_2022_3_OR_NEWER
            targetObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
            targetObj.transform.localPosition = Vector3.zero;
            targetObj.transform.localRotation = Quaternion.identity;
#endif
            targetObj.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(targetObj, "");

            var transformPoseSave = new TransformPoseSave(gameObject);
            transformPoseSave.CreateExtraTransforms();
            var saveRoot = transformPoseSave.GetHumanDescriptionTransforms(gameObject.transform);
            if (saveRoot == null)
                return null;

            static Mesh EditorHelperLoadShape(string filename)
            {
                return AssetDatabase.LoadAssetAtPath<Mesh>("Packages/com.unity.animation.rigging/Editor/Shapes/" + filename);
            }

            var rootRotationInv = Quaternion.Inverse(saveRoot.rotation);
            if (target == IKTarget.Head)
            {
                var tHead = animator.GetBoneTransform(HumanBodyBones.Head);
                if (tHead != null)
                {
                    var constraint = Undo.AddComponent<MultiAimConstraint>(go);
                    Undo.RecordObject(constraint, "");

                    constraint.weight = 0f;
                    constraint.data.constrainedObject = tHead;
                    {
                        var list = constraint.data.sourceObjects;
                        list.Add(new WeightedTransform(targetObj.transform, 1f));
                        constraint.data.sourceObjects = list;
                    }
                    {
                        var limitSign = uAvatar.GetLimitSign(animator.avatar, (int)HumanBodyBones.Head);
                        var forward = uAvatar.GetPostRotation(animator.avatar, (int)HumanBodyBones.Head) * Vector3.down * limitSign.y;
                        var dotX = Vector3.Dot(forward, Vector3.right);
                        var dotY = Vector3.Dot(forward, Vector3.up);
                        var dotZ = Vector3.Dot(forward, Vector3.forward);
                        var axis = forward;
                        if (Mathf.Abs(dotX) > Mathf.Abs(dotY) && Mathf.Abs(dotX) > Mathf.Abs(dotZ))
                        {
                            if (dotX > 0f)
                            {
                                constraint.data.aimAxis = MultiAimConstraintData.Axis.X;
                                axis = Vector3.right;
                            }
                            else
                            {
                                constraint.data.aimAxis = MultiAimConstraintData.Axis.X_NEG;
                                axis = Vector3.left;
                            }
                            constraint.data.constrainedXAxis = false;
                        }
                        else if (Mathf.Abs(dotY) > Mathf.Abs(dotX) && Mathf.Abs(dotY) > Mathf.Abs(dotZ))
                        {
                            if (dotY > 0f)
                            {
                                constraint.data.aimAxis = MultiAimConstraintData.Axis.Y;
                                axis = Vector3.up;
                            }
                            else
                            {
                                constraint.data.aimAxis = MultiAimConstraintData.Axis.Y_NEG;
                                axis = Vector3.down;
                            }
                            constraint.data.constrainedYAxis = false;
                        }
                        else
                        {
                            if (dotZ > 0f)
                            {
                                constraint.data.aimAxis = MultiAimConstraintData.Axis.Z;
                                axis = Vector3.forward;
                            }
                            else
                            {
                                constraint.data.aimAxis = MultiAimConstraintData.Axis.Z_NEG;
                                axis = Vector3.back;
                            }
                            constraint.data.constrainedZAxis = false;
                        }
                        constraint.data.offset = Quaternion.FromToRotation(forward, axis).eulerAngles;
                    }
                    {
                        var limit = uAvatar.GetMuscleLimitNonError(animator.avatar, HumanBodyBones.Head);
                        var min = Mathf.Min(limit.min[0], limit.min[2]);
                        var max = Mathf.Min(limit.max[0], limit.max[2]);
                        constraint.data.limits = new Vector2(min, max);
                    }
                    {
                        constraint.data.maintainOffset = false;
                    }

                    {
                        rig.RemoveEffector(targetObj.transform);
                        rig.AddEffector(targetObj.transform, new RigEffectorData.Style()
                        {
                            shape = EditorHelperLoadShape("BallEffector.asset"),
                            color = new(1f, 0.92f, 0.016f, 0.5f),
                            size = 0.10f * animator.humanScale,
                            position = Vector3.zero,
                            rotation = Vector3.zero
                        });
                        var effector = (rig.effectors as List<RigEffectorData>).Find(data => data.transform == targetObj.transform);
                        effector.visible = true;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("<color=blue>[Very Animation]</color>Unknown avatar file error. {0}", animator.avatar);
                }
            }
            else
            {
                Transform tRoot, tMid, tTip;
                switch (target)
                {
                    case IKTarget.LeftHand:
                        tRoot = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                        tMid = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                        tTip = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                        break;
                    case IKTarget.RightHand:
                        tRoot = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                        tMid = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                        tTip = animator.GetBoneTransform(HumanBodyBones.RightHand);
                        break;
                    case IKTarget.LeftFoot:
                        tRoot = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                        tMid = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                        tTip = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                        break;
                    case IKTarget.RightFoot:
                        tRoot = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                        tMid = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                        tTip = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                        break;
                    default:
                        tRoot = tMid = tTip = null;
                        break;
                }
                if (tRoot != null && tMid != null && tTip != null)
                {
                    var constraint = Undo.AddComponent<TwoBoneIKConstraint>(go);
                    var hintObj = new GameObject(go.name + "_Hint");
                    hintObj.transform.SetParent(go.transform);
#if UNITY_2022_3_OR_NEWER
                    hintObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
                    hintObj.transform.localPosition = Vector3.zero;
                    hintObj.transform.localRotation = Quaternion.identity;
#endif
                    hintObj.transform.localScale = Vector3.one;
                    Undo.RegisterCreatedObjectUndo(hintObj, "");
                    Undo.RecordObject(constraint, "");
                    Undo.RecordObject(vaRig, "");
                    constraint.weight = 0f;
                    Color effectorColor = Color.white;
                    string effectorTarget = "";
                    string effectorHint = "";
                    float effectorTargetSize = 0.10f * animator.humanScale;
                    float effectorHintSize = 0.10f * animator.humanScale;
                    switch (target)
                    {
                        case IKTarget.LeftHand:
                            constraint.data.root = tRoot;
                            constraint.data.mid = tMid;
                            constraint.data.tip = tTip;
                            constraint.data.maintainTargetRotationOffset = true;
                            {
                                var save = transformPoseSave.GetHumanDescriptionTransforms(constraint.data.tip);
                                if (save != null)
                                {
                                    var rotation = (rootRotationInv * save.rotation);
                                    {
                                        var limitSign = uAvatar.GetLimitSign(animator.avatar, (int)HumanBodyBones.LeftHand);
                                        var vecPost = (rotation * uAvatar.GetPostRotation(animator.avatar, (int)HumanBodyBones.LeftHand)) * Vector3.right * limitSign.x;
                                        var offset = Quaternion.FromToRotation(vecPost, Vector3.left);
                                        rotation = Quaternion.Euler(0f, 90f, 0f) * offset * rotation;
                                    }
                                    vaRig.basePoseLeftHand = new VeryAnimationRig.BasePoseTransformOffset(go.transform, rotation);
                                }
                            }
                            effectorColor = new(0f, 0f, 1f, 0.5f);
                            effectorTarget = "BoxEffector.asset";
                            effectorHint = "BallEffector.asset";
                            effectorHintSize = 0.05f * animator.humanScale;
                            break;
                        case IKTarget.RightHand:
                            constraint.data.root = tRoot;
                            constraint.data.mid = tMid;
                            constraint.data.tip = tTip;
                            constraint.data.maintainTargetRotationOffset = true;
                            {
                                var save = transformPoseSave.GetHumanDescriptionTransforms(constraint.data.tip);
                                if (save != null)
                                {
                                    var rotation = (rootRotationInv * save.rotation);
                                    {
                                        var limitSign = uAvatar.GetLimitSign(animator.avatar, (int)HumanBodyBones.LeftHand);
                                        var vecPost = (rotation * uAvatar.GetPostRotation(animator.avatar, (int)HumanBodyBones.RightHand)) * Vector3.right * limitSign.x;
                                        var offset = Quaternion.FromToRotation(vecPost, Vector3.right);
                                        rotation = Quaternion.Euler(0f, -90f, 0f) * offset * rotation;
                                    }
                                    vaRig.basePoseRightHand = new VeryAnimationRig.BasePoseTransformOffset(go.transform, rotation);
                                }
                            }
                            effectorColor = new(1f, 0f, 0f, 0.5f);
                            effectorTarget = "BoxEffector.asset";
                            effectorHint = "BallEffector.asset";
                            effectorHintSize = 0.05f * animator.humanScale;
                            break;
                        case IKTarget.LeftFoot:
                            constraint.data.root = tRoot;
                            constraint.data.mid = tMid;
                            constraint.data.tip = tTip;
                            constraint.data.maintainTargetPositionOffset = true;
                            constraint.data.maintainTargetRotationOffset = true;
                            {
                                var save = transformPoseSave.GetHumanDescriptionTransforms(constraint.data.tip);
                                if (save != null)
                                {
                                    var rotation = save.rotation;
                                    vaRig.basePoseLeftFoot = new VeryAnimationRig.BasePoseTransformOffset(go.transform, new Vector3(0f, animator.leftFeetBottomHeight, 0f), rootRotationInv * rotation);
                                }
                            }
                            effectorColor = new(0f, 0f, 1f, 0.5f);
                            effectorTarget = "SquareEffector.asset";
                            effectorHint = "LocatorEffector.asset";
                            effectorTargetSize = 0.2f * animator.humanScale;
                            break;
                        case IKTarget.RightFoot:
                            constraint.data.root = tRoot;
                            constraint.data.mid = tMid;
                            constraint.data.tip = tTip;
                            constraint.data.maintainTargetPositionOffset = true;
                            constraint.data.maintainTargetRotationOffset = true;
                            {
                                var save = transformPoseSave.GetHumanDescriptionTransforms(constraint.data.tip);
                                if (save != null)
                                {
                                    var rotation = save.rotation;
                                    vaRig.basePoseRightFoot = new VeryAnimationRig.BasePoseTransformOffset(go.transform, new Vector3(0f, animator.rightFeetBottomHeight, 0f), rootRotationInv * rotation);
                                }
                            }
                            effectorColor = new(1f, 0f, 0f, 0.5f);
                            effectorTarget = "SquareEffector.asset";
                            effectorHint = "LocatorEffector.asset";
                            effectorTargetSize = 0.2f * animator.humanScale;
                            break;
                    }
                    constraint.data.target = targetObj.transform;
                    constraint.data.hint = hintObj.transform;

                    {
                        rig.RemoveEffector(targetObj.transform);
                        rig.AddEffector(targetObj.transform, new RigEffectorData.Style()
                        {
                            shape = EditorHelperLoadShape(effectorTarget),
                            color = effectorColor,
                            size = effectorTargetSize,
                            position = Vector3.zero,
                            rotation = Vector3.zero
                        });
                        var effector = (rig.effectors as List<RigEffectorData>).Find(data => data.transform == targetObj.transform);
                        effector.visible = true;
                    }
                    {
                        rig.RemoveEffector(hintObj.transform);
                        rig.AddEffector(hintObj.transform, new RigEffectorData.Style()
                        {
                            shape = EditorHelperLoadShape(effectorHint),
                            color = effectorColor,
                            size = effectorHintSize,
                            position = Vector3.zero,
                            rotation = Vector3.zero
                        });
                        var effector = (rig.effectors as List<RigEffectorData>).Find(data => data.transform == hintObj.transform);
                        effector.visible = true;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("<color=blue>[Very Animation]</color>Unknown avatar file error. {0}", animator.avatar);
                }
            }

            return go;
        }
        #endregion
#endif
    }
}
