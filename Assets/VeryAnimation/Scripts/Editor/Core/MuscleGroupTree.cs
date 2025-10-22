using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    [Serializable]
    internal class MuscleGroupTree
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        private enum MuscleGroupMode
        {
            Category,
            Part,
            Total,
        }
        private static readonly string[] MuscleGroupModeString =
        {
            MuscleGroupMode.Category.ToString(),
            MuscleGroupMode.Part.ToString(),
        };

        private MuscleGroupMode muscleGroupMode;

        private class MuscleInfo
        {
            public HumanBodyBones hi;
            public int dof;
            public float scale = 1f;
        }
        private class MuscleGroupNode
        {
            public string name;
            public string mirrorName;
            public bool foldout;
            public int dof = -1;
            public MuscleInfo[] infoList;
            public MuscleGroupNode[] children;
        }
        private readonly MuscleGroupNode[] muscleGroupNode;
        private readonly Dictionary<MuscleGroupNode, int> muscleGroupTreeTable;

        [SerializeField]
        private float[] muscleGroupValues;

        public MuscleGroupTree()
        {
            #region MuscleGroupNode
            {
                muscleGroupNode = new MuscleGroupNode[]
                {
#region Category
                    new() { name = MuscleGroupMode.Category.ToString(),
                        children = new MuscleGroupNode[]
                        {
#region Open Close
                            new() { name = "Open Close", dof = 2,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.Spine, dof = 2 },
                                    new() { hi = HumanBodyBones.Chest, dof = 2 },
                                    new() { hi = HumanBodyBones.UpperChest, dof = 2 },
                                    new() { hi = HumanBodyBones.Neck, dof = 2 },
                                    new() { hi = HumanBodyBones.Head, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftShoulder, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftUpperArm, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLowerArm, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftHand, dof = 2 },
                                    new() { hi = HumanBodyBones.RightShoulder, dof = 2 },
                                    new() { hi = HumanBodyBones.RightUpperArm, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLowerArm, dof = 2 },
                                    new() { hi = HumanBodyBones.RightHand, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftUpperLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLowerLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftFoot, dof = 2 },
                                    new() { hi = HumanBodyBones.RightUpperLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLowerLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.RightFoot, dof = 2 },
                                },
                                children = new MuscleGroupNode[]
                                {
                                    new() { name = "Head", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.Head, dof = 2 },
                                            new() { hi = HumanBodyBones.Neck, dof = 2 },
                                        },
                                    },
                                    new() { name = "Body", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.UpperChest, dof = 2 },
                                            new() { hi = HumanBodyBones.Chest, dof = 2 },
                                            new() { hi = HumanBodyBones.Spine, dof = 2 },
                                        },
                                    },
                                    new() { name = "Left Arm", mirrorName = "Open Close/Right Arm", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftShoulder, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftUpperArm, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLowerArm, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftHand, dof = 2 },
                                        },
                                    },
                                    new() { name = "Right Arm", mirrorName = "Open Close/Left Arm", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightShoulder, dof = 2 },
                                            new() { hi = HumanBodyBones.RightUpperArm, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLowerArm, dof = 2 },
                                            new() { hi = HumanBodyBones.RightHand, dof = 2 },
                                        },
                                    },
                                    new() { name = "Left Leg", mirrorName = "Open Close/Right Leg", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLowerLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftFoot, dof = 2 },
                                        },
                                    },
                                    new() { name = "Right Leg", mirrorName = "Open Close/Left Leg", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLowerLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.RightFoot, dof = 2 },
                                        },
                                    },
                                },
                            },
#endregion
#region Left Right
                            new() { name = "Left Right", dof = 1,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.Head, dof = 1 },
                                    new() { hi = HumanBodyBones.Neck, dof = 1 },
                                    new() { hi = HumanBodyBones.UpperChest, dof = 1 },
                                    new() { hi = HumanBodyBones.Chest, dof = 1 },
                                    new() { hi = HumanBodyBones.Spine, dof = 1 },
                                },
                            },
#endregion
#region Roll Left Right
                            new() { name = "Roll Left Right", dof = 0,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.Head, dof = 0 },
                                    new() { hi = HumanBodyBones.Neck, dof = 0 },
                                    new() { hi = HumanBodyBones.UpperChest, dof = 0 },
                                    new() { hi = HumanBodyBones.Chest, dof = 0 },
                                    new() { hi = HumanBodyBones.Spine, dof = 0 },
                                },
                            },
#endregion
#region In Out
                            new() { name = "In Out", dof = 1,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftShoulder, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftUpperArm, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftHand, dof = 1 },
                                    new() { hi = HumanBodyBones.RightShoulder, dof = 1 },
                                    new() { hi = HumanBodyBones.RightUpperArm, dof = 1 },
                                    new() { hi = HumanBodyBones.RightHand, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftUpperLeg, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftFoot, dof = 1 },
                                    new() { hi = HumanBodyBones.RightUpperLeg, dof = 1 },
                                    new() { hi = HumanBodyBones.RightFoot, dof = 1 },
                                },
                                children = new MuscleGroupNode[]
                                {
                                    new() { name = "Left Arm", mirrorName = "In Out/Right Arm", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftShoulder, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftUpperArm, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftHand, dof = 1 },
                                        },
                                    },
                                    new() { name = "Right Arm", mirrorName = "In Out/Left Arm", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightShoulder, dof = 1 },
                                            new() { hi = HumanBodyBones.RightUpperArm, dof = 1 },
                                            new() { hi = HumanBodyBones.RightHand, dof = 1 },
                                        },
                                    },
                                    new() { name = "Left Leg", mirrorName = "In Out/Right Leg", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperLeg, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftFoot, dof = 1 },
                                        },
                                    },
                                    new() { name = "Right Leg", mirrorName = "In Out/Left Leg", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperLeg, dof = 1 },
                                            new() { hi = HumanBodyBones.RightFoot, dof = 1 },
                                        },
                                    },
                                },
                            },
#endregion
#region Roll In Out
                            new() { name = "Roll In Out", dof = 0,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftUpperArm, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftLowerArm, dof = 0 },
                                    new() { hi = HumanBodyBones.RightUpperArm, dof = 0 },
                                    new() { hi = HumanBodyBones.RightLowerArm, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftUpperLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftLowerLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.RightUpperLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.RightLowerLeg, dof = 0 },
                                },
                                children = new MuscleGroupNode[]
                                {
                                    new() { name = "Left Arm", mirrorName = "Roll In Out/Right Arm", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperArm, dof = 0 },
                                            new() { hi = HumanBodyBones.LeftLowerArm, dof = 0 },
                                        },
                                    },
                                    new() { name = "Right Arm", mirrorName = "Roll In Out/Left Arm", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperArm, dof = 0 },
                                            new() { hi = HumanBodyBones.RightLowerArm, dof = 0 },
                                        },
                                    },
                                    new() { name = "Left Leg", mirrorName = "Roll In Out/Right Leg", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperLeg, dof = 0 },
                                            new() { hi = HumanBodyBones.LeftLowerLeg, dof = 0 },
                                        },
                                    },
                                    new() { name = "Right Leg", mirrorName = "Roll In Out/Left Leg", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperLeg, dof = 0 },
                                            new() { hi = HumanBodyBones.RightLowerLeg, dof = 0 },
                                        },
                                    },
                                },
                            },
#endregion
#region Finger Open Close
                            new() { name = "Finger Open Close", dof = 2,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftThumbProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftThumbIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftThumbDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftIndexProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftIndexIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftIndexDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftMiddleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftMiddleDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftRingProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftRingIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftRingDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLittleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLittleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLittleDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightThumbProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightThumbIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightThumbDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightIndexProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightIndexIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightIndexDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightMiddleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightMiddleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightMiddleDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightRingProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightRingIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightRingDistal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLittleProximal, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLittleIntermediate, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLittleDistal, dof = 2 },
                                },
                                children = new MuscleGroupNode[]
                                {
                                    new() { name = "Left Finger", mirrorName = "Finger Open Close/Right Finger", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftThumbProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftThumbIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftThumbDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftIndexProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftIndexIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftIndexDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftMiddleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftMiddleDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftRingProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftRingIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftRingDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLittleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLittleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLittleDistal, dof = 2 },
                                        },
                                        children = new MuscleGroupNode[]
                                        {
                                            new() { name = "Left Thumb", mirrorName = "Finger Open Close/Right Finger/Right Thumb", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.LeftThumbProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftThumbIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftThumbDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Left Index", mirrorName = "Finger Open Close/Right Finger/Right Index", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.LeftIndexProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftIndexIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftIndexDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Left Middle", mirrorName = "Finger Open Close/Right Finger/Right Middle", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftMiddleIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftMiddleDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Left Ring", mirrorName = "Finger Open Close/Right Finger/Right Ring", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.LeftRingProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftRingIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftRingDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Left Little", mirrorName = "Finger Open Close/Right Finger/Right Little", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.LeftLittleProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftLittleIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.LeftLittleDistal, dof = 2 },
                                                },
                                            },
                                        },
                                    },
                                    new() { name = "Right Finger", mirrorName = "Finger Open Close/Left Finger", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightThumbProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightThumbIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightThumbDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightIndexProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightIndexIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightIndexDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightMiddleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightMiddleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightMiddleDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightRingProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightRingIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightRingDistal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLittleProximal, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLittleIntermediate, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLittleDistal, dof = 2 },
                                        },
                                        children = new MuscleGroupNode[]
                                        {
                                            new() { name = "Right Thumb", mirrorName = "Finger Open Close/Left Finger/Left Thumb", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.RightThumbProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightThumbIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightThumbDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Right Index", mirrorName = "Finger Open Close/Left Finger/Left Index", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.RightIndexProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightIndexIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightIndexDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Right Middle", mirrorName = "Finger Open Close/Left Finger/Left Middle", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.RightMiddleProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightMiddleIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightMiddleDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Right Ring", mirrorName = "Finger Open Close/Left Finger/Left Ring", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.RightRingProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightRingIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightRingDistal, dof = 2 },
                                                },
                                            },
                                            new() { name = "Right Little", mirrorName = "Finger Open Close/Left Finger/Left Little", dof = 2,
                                                infoList = new MuscleInfo[]
                                                {
                                                    new() { hi = HumanBodyBones.RightLittleProximal, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightLittleIntermediate, dof = 2 },
                                                    new() { hi = HumanBodyBones.RightLittleDistal, dof = 2 },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
#endregion
#region Finger In Out
                            new() { name = "Finger In Out", dof = 1,
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftThumbProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftIndexProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftRingProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftLittleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightThumbProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightIndexProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightMiddleProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightRingProximal, dof = 1 },
                                    new() { hi = HumanBodyBones.RightLittleProximal, dof = 1 },
                                },
                                children = new MuscleGroupNode[]
                                {
                                    new() { name = "Left Finger", mirrorName = "Finger In Out/Right Finger", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftThumbProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftIndexProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftMiddleProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftRingProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftLittleProximal, dof = 1 },
                                        },
                                    },
                                    new() { name = "Right Finger", mirrorName = "Finger In Out/Left Finger", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightThumbProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightIndexProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightMiddleProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightRingProximal, dof = 1 },
                                            new() { hi = HumanBodyBones.RightLittleProximal, dof = 1 },
                                        },
                                    },
                                },
                            },
#endregion
                        },
                    },
#endregion
#region Part
                    new() { name = MuscleGroupMode.Category.ToString(),
                        children = new MuscleGroupNode[]
                        {
#region Face
                            new() { name = "Face",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftEye, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftEye, dof = 1 },
                                    new() { hi = HumanBodyBones.RightEye, dof = 2 },
                                    new() { hi = HumanBodyBones.RightEye, dof = 1 },
                                    new() { hi = HumanBodyBones.Jaw, dof = 2 },
                                    new() { hi = HumanBodyBones.Jaw, dof = 1 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Eyes Down Up
                                    new() { name = "Eyes Down Up",
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftEye, dof = 2 },
                                            new() { hi = HumanBodyBones.RightEye, dof = 2 },
                                        },
                                    },
#endregion
#region Eyes Left Right
                                    new() { name = "Eyes Left Right",
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftEye, dof = 1 },
                                            new() { hi = HumanBodyBones.RightEye, dof = 1, scale = -1f },
                                        },
                                    },
#endregion
#region Jaw
                                    new() { name = "Jaw",
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.Jaw, dof = 2 },
                                            new() { hi = HumanBodyBones.Jaw, dof = 1 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Head
                            new() { name = "Head",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.Neck, dof = 2 },
                                    new() { hi = HumanBodyBones.Neck, dof = 1 },
                                    new() { hi = HumanBodyBones.Neck, dof = 0 },
                                    new() { hi = HumanBodyBones.Head, dof = 2 },
                                    new() { hi = HumanBodyBones.Head, dof = 1 },
                                    new() { hi = HumanBodyBones.Head, dof = 0 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Open Close
                                    new() { name = "Open Close", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.Head, dof = 2 },
                                            new() { hi = HumanBodyBones.Neck, dof = 2 },
                                        },
                                    },
#endregion
#region Left Right
                                    new() { name = "Left Right", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.Head, dof = 1 },
                                            new() { hi = HumanBodyBones.Neck, dof = 1 },
                                        },
                                    },
#endregion
#region Roll Left Right
                                    new() { name = "Roll Left Right", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.Head, dof = 0 },
                                            new() { hi = HumanBodyBones.Neck, dof = 0 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Body
                            new() { name = "Body",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.Spine, dof = 2 },
                                    new() { hi = HumanBodyBones.Spine, dof = 1 },
                                    new() { hi = HumanBodyBones.Spine, dof = 0 },
                                    new() { hi = HumanBodyBones.Chest, dof = 2 },
                                    new() { hi = HumanBodyBones.Chest, dof = 1 },
                                    new() { hi = HumanBodyBones.Chest, dof = 0 },
                                    new() { hi = HumanBodyBones.UpperChest, dof = 2 },
                                    new() { hi = HumanBodyBones.UpperChest, dof = 1 },
                                    new() { hi = HumanBodyBones.UpperChest, dof = 0 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Open Close
                                    new() { name = "Open Close", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.UpperChest, dof = 2 },
                                            new() { hi = HumanBodyBones.Chest, dof = 2 },
                                            new() { hi = HumanBodyBones.Spine, dof = 2 },
                                        },
                                    },
#endregion
#region Left Right
                                    new() { name = "Left Right", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.UpperChest, dof = 1 },
                                            new() { hi = HumanBodyBones.Chest, dof = 1 },
                                            new() { hi = HumanBodyBones.Spine, dof = 1 },
                                        },
                                    },
#endregion
#region Roll Left Right
                                    new() { name = "Roll Left Right", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.UpperChest, dof = 0 },
                                            new() { hi = HumanBodyBones.Chest, dof = 0 },
                                            new() { hi = HumanBodyBones.Spine, dof = 0 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Left Arm
                            new() { name = "Left Arm", mirrorName = "Right Arm",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftShoulder, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftShoulder, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftUpperArm, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftUpperArm, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftUpperArm, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftLowerArm, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLowerArm, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftHand, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftHand, dof = 1 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Open Close
                                    new() { name = "Open Close", mirrorName = "Right Arm/Open Close", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftShoulder, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftUpperArm, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLowerArm, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftHand, dof = 2 },
                                        },
                                    },
#endregion
#region In Out
                                    new() { name = "In Out", mirrorName = "Right Arm/In Out", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftShoulder, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftUpperArm, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftHand, dof = 1 },
                                        },
                                    },
#endregion
#region Roll In Out
                                    new() { name = "Roll In Out", mirrorName = "Right Arm/Roll In Out", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperArm, dof = 0 },
                                            new() { hi = HumanBodyBones.LeftLowerArm, dof = 0 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Right Arm
                            new() { name = "Right Arm", mirrorName = "Left Arm",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.RightShoulder, dof = 2 },
                                    new() { hi = HumanBodyBones.RightShoulder, dof = 1 },
                                    new() { hi = HumanBodyBones.RightUpperArm, dof = 2 },
                                    new() { hi = HumanBodyBones.RightUpperArm, dof = 1 },
                                    new() { hi = HumanBodyBones.RightUpperArm, dof = 0 },
                                    new() { hi = HumanBodyBones.RightLowerArm, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLowerArm, dof = 0 },
                                    new() { hi = HumanBodyBones.RightHand, dof = 2 },
                                    new() { hi = HumanBodyBones.RightHand, dof = 1 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Open Close
                                    new() { name = "Open Close", mirrorName = "Left Arm/Open Close", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightShoulder, dof = 2 },
                                            new() { hi = HumanBodyBones.RightUpperArm, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLowerArm, dof = 2 },
                                            new() { hi = HumanBodyBones.RightHand, dof = 2 },
                                        },
                                    },
#endregion
#region In Out
                                    new() { name = "In Out", mirrorName = "Left Arm/In Out", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightShoulder, dof = 1 },
                                            new() { hi = HumanBodyBones.RightUpperArm, dof = 1 },
                                            new() { hi = HumanBodyBones.RightHand, dof = 1 },
                                        },
                                    },
#endregion
#region Roll In Out
                                    new() { name = "Roll In Out", mirrorName = "Left Arm/Roll In Out", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperArm, dof = 0 },
                                            new() { hi = HumanBodyBones.RightLowerArm, dof = 0 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Left Leg
                            new() { name = "Left Leg", mirrorName = "Right Leg",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.LeftUpperLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftUpperLeg, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftUpperLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftLowerLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftLowerLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.LeftFoot, dof = 2 },
                                    new() { hi = HumanBodyBones.LeftFoot, dof = 1 },
                                    new() { hi = HumanBodyBones.LeftToes, dof = 2 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Open Close
                                    new() { name = "Open Close", mirrorName = "Right Leg/Open Close", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftLowerLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.LeftFoot, dof = 2 },
                                        },
                                    },
#endregion
#region In Out
                                    new() { name = "In Out", mirrorName = "Right Leg/In Out", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperLeg, dof = 1 },
                                            new() { hi = HumanBodyBones.LeftFoot, dof = 1 },
                                        },
                                    },
#endregion
#region Roll In Out
                                    new() { name = "Roll In Out", mirrorName = "Right Leg/Roll In Out", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftUpperLeg, dof = 0 },
                                            new() { hi = HumanBodyBones.LeftLowerLeg, dof = 0 },
                                        },
                                    },
#endregion
#region Toes
                                    new() { name = "Toes", mirrorName = "Right Leg/Toes", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.LeftToes, dof = 2 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Right Leg
                            new() { name = "Right Leg", mirrorName = "Left Leg",
                                infoList = new MuscleInfo[]
                                {
                                    new() { hi = HumanBodyBones.RightUpperLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.RightUpperLeg, dof = 1 },
                                    new() { hi = HumanBodyBones.RightUpperLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.RightLowerLeg, dof = 2 },
                                    new() { hi = HumanBodyBones.RightLowerLeg, dof = 0 },
                                    new() { hi = HumanBodyBones.RightFoot, dof = 2 },
                                    new() { hi = HumanBodyBones.RightFoot, dof = 1 },
                                    new() { hi = HumanBodyBones.RightToes, dof = 2 },
                                },
                                children = new MuscleGroupNode[]
                                {
#region Open Close
                                    new() { name = "Open Close", mirrorName = "Left Leg/Open Close", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.RightLowerLeg, dof = 2 },
                                            new() { hi = HumanBodyBones.RightFoot, dof = 2 },
                                        },
                                    },
#endregion
#region In Out
                                    new() { name = "In Out", mirrorName = "Left Leg/In Out", dof = 1,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperLeg, dof = 1 },
                                            new() { hi = HumanBodyBones.RightFoot, dof = 1 },
                                        },
                                    },
#endregion
#region Roll In Out
                                    new() { name = "Roll In Out", mirrorName = "Left Leg/Roll In Out", dof = 0,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightUpperLeg, dof = 0 },
                                            new() { hi = HumanBodyBones.RightLowerLeg, dof = 0 },
                                        },
                                    },
#endregion
#region Toes
                                    new() { name = "Toes", mirrorName = "Left Leg/Toes", dof = 2,
                                        infoList = new MuscleInfo[]
                                        {
                                            new() { hi = HumanBodyBones.RightToes, dof = 2 },
                                        },
                                    },
#endregion
                                },
                            },
#endregion
#region Left Finger
                            new() { name = "Left Finger", mirrorName = "Right Finger",
                                infoList = new MuscleInfo[]
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
                                children = new MuscleGroupNode[]
                                {
#region Left Thumb
                                    new() { name = "Left Thumb", mirrorName = "Right Finger/Right Thumb",
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                infoList = new MuscleInfo[]
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
                                children = new MuscleGroupNode[]
                                {
#region Right Thumb
                                    new() { name = "Right Thumb", mirrorName = "Left Finger/Left Thumb",
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                                        infoList = new MuscleInfo[]
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
                    },
#endregion
                };

                {
                    muscleGroupTreeTable = new Dictionary<MuscleGroupNode, int>();
                    int counter = 0;
                    void AddTable(MuscleGroupNode mg)
                    {
                        muscleGroupTreeTable.Add(mg, counter++);
                        if (mg.children != null)
                        {
                            foreach (var child in mg.children)
                            {
                                AddTable(child);
                            }
                        }
                    }

                    foreach (var node in muscleGroupNode)
                    {
                        AddTable(node);
                    }

                    muscleGroupValues = new float[muscleGroupTreeTable.Count];
                }
            }
            #endregion
        }

        public void LoadEditorPref()
        {
            muscleGroupMode = (MuscleGroupMode)EditorPrefs.GetInt("VeryAnimation_MuscleGroupMode", 0);
        }
        public void SaveEditorPref()
        {
            EditorPrefs.SetInt("VeryAnimation_MuscleGroupMode", (int)muscleGroupMode);
        }

        public void MuscleGroupToolbarGUI()
        {
            EditorGUI.BeginChangeCheck();
            var m = (MuscleGroupMode)GUILayout.Toolbar((int)muscleGroupMode, MuscleGroupModeString, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                muscleGroupMode = m;
            }
        }

        private struct MuscleValue
        {
            public int muscleIndex;
            public float value;
        }
        public void MuscleGroupTreeGUI()
        {
            RowCount = 0;

            var e = Event.current;

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                var mgRoot = muscleGroupNode[(int)muscleGroupMode];

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
                            combineGoList.Add(VAW.GameObject);
                            for (int hi = 0; hi < (int)HumanBodyBones.LastBone; hi++)
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
                            combineGoList.Add(VAW.GameObject);
                            for (int hi = 0; hi < (int)HumanBodyBones.LastBone; hi++)
                            {
                                if (VAW.VA.HumanoidBones[hi] != null)
                                    combineGoList.Add(VAW.VA.HumanoidBones[hi]);
                                else if (VeryAnimation.HumanVirtualBones[hi] != null)
                                    combineVirtualList.Add((HumanBodyBones)hi);
                            }
                            Selection.activeGameObject = VAW.GameObject;
                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), combineVirtualList.ToArray());
                        }
                    }
                    GUILayout.Space(10);
                    if (GUILayout.Button("Root", GUILayout.Width(100)))
                    {
                        if (Shortcuts.IsKeyControl(e) || e.shift)
                        {
                            var combineGoList = new List<GameObject>(VAW.VA.SelectionGameObjects)
                            {
                                VAW.GameObject
                            };
                            VAW.VA.SelectGameObjects(combineGoList.ToArray(), VAW.VA.SelectionHumanVirtualBones?.ToArray());
                        }
                        else
                        {
                            VAW.VA.SelectGameObject(VAW.GameObject);
                        }
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Reset All", GUILayout.Width(100)))
                    {
                        Undo.RecordObject(VAE, "Reset All Muscle Group");
                        foreach (var root in mgRoot.children)
                        {
                            List<MuscleValue> muscles = new();
                            SetMuscleGroupValue(root, 0f, muscles);
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
                        MuscleGroupTreeNodeGUI(root, 0, maxLevel);
                    }
                }
                #endregion
            }
            EditorGUILayout.EndVertical();
        }
        #region MuscleGroupTreeGUI
        private int RowCount = 0;
        private const int IndentWidth = 15;
        private int GetTreeLevel(MuscleGroupNode mg, int level)
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
        private MuscleGroupNode GetMirrorNode(MuscleGroupNode mg)
        {
            if (string.IsNullOrEmpty(mg.mirrorName))
                return null;
            var splits = mg.mirrorName.Split('/');
            MuscleGroupNode mirrorNode = muscleGroupNode[(int)muscleGroupMode];
            for (int i = 0; i < splits.Length; i++)
            {
                var index = ArrayUtility.FindIndex(mirrorNode.children, (node) => node.name == splits[i]);
                mirrorNode = mirrorNode.children[index];
            }
            Assert.IsTrue(mirrorNode.name == Path.GetFileName(mg.mirrorName));
            return mirrorNode;
        }
        private void SetMuscleGroupFoldout(MuscleGroupNode mg, bool foldout)
        {
            mg.foldout = foldout;
            if (mg.children != null)
            {
                foreach (var child in mg.children)
                {
                    SetMuscleGroupFoldout(child, foldout);
                }
            }
        }
        private bool ContainsMuscleGroup(MuscleGroupNode mg)
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
                    if (ContainsMuscleGroup(child)) return true;
                }
            }
            return false;
        }
        private void SetMuscleGroupValue(MuscleGroupNode mg, float value, List<MuscleValue> muscles)
        {
            muscleGroupValues[muscleGroupTreeTable[mg]] = value;
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
                    SetMuscleGroupValue(child, value, muscles);
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
        private void MuscleGroupTreeNodeGUI(MuscleGroupNode mg, int level, int brotherMaxLevel)
        {
            const int FoldoutWidth = 22;
            const int FoldoutSpace = 17;
            const int FloatFieldWidth = 44;
            var indentSpace = IndentWidth * level;
            var e = Event.current;
            var mgContains = ContainsMuscleGroup(mg);
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
                            SetMuscleGroupFoldout(mg, mg.foldout);
                    }
                    EditorGUI.indentLevel = 0;
                }
                {
                    void SelectNodeAll(MuscleGroupNode node)
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
                    var value = GUILayout.HorizontalSlider(muscleGroupValues[muscleGroupTreeTable[mg]], -1f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAE, "Change Muscle Group");
                        List<MuscleValue> muscles = new();
                        SetMuscleGroupValue(mg, value, muscles);
                        SetAnimationCurveMuscleValues(muscles);
                        if (VAW.VA.optionsMirror)
                        {
                            var mirrorNode = GetMirrorNode(mg);
                            if (mirrorNode != null)
                                SetMuscleGroupValue(mirrorNode, value, muscles);
                        }
                    }
                    GUI.backgroundColor = saveBackgroundColor;
                }
                {
                    var width = FloatFieldWidth + IndentWidth * Math.Max(GetTreeLevel(mg, 0), brotherMaxLevel);
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUILayout.FloatField(muscleGroupValues[muscleGroupTreeTable[mg]], GUILayout.Width(width));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAE, "Change Muscle Group");
                        List<MuscleValue> muscles = new();
                        SetMuscleGroupValue(mg, value, muscles);
                        SetAnimationCurveMuscleValues(muscles);
                        if (VAW.VA.optionsMirror)
                        {
                            var mirrorNode = GetMirrorNode(mg);
                            if (mirrorNode != null)
                                SetMuscleGroupValue(mirrorNode, value, muscles);
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
                        MuscleGroupTreeNodeGUI(child, level + 1, maxLevel);
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
