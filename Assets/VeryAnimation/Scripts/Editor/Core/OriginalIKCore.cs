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
    internal class OriginalIKCore
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }
        private VeryAnimationControlWindow VAC { get { return VeryAnimationControlWindow.instance; } }
        private VeryAnimationEditorWindow VAE { get { return VeryAnimationEditorWindow.instance; } }

        public enum SolverType
        {
            CcdIK,
            LimbIK,
            LookAt,
            Total,
        }
        public GUIContent[] SolverTypeStrings = new GUIContent[(int)SolverType.LimbIK + 1];

        private const int SolverLevelMin = 2;
        private const int SolverLevelMax = 16;
        private readonly GUIContent[] SolverLevelStrings = new GUIContent[]
        {
            new("2", "IK Level"),
            new("3", "IK Level"),
            new("4", "IK Level"),
            new("5", "IK Level"),
            new("6", "IK Level"),
            new("7", "IK Level"),
            new("8", "IK Level"),
            new("9", "IK Level"),
            new("10", "IK Level"),
            new("11", "IK Level"),
            new("12", "IK Level"),
            new("13", "IK Level"),
            new("14", "IK Level"),
            new("15", "IK Level"),
            new("16", "IK Level"),
        };

        public GUIContent[] IKSpaceTypeStrings = new GUIContent[(int)OriginalIKData.SpaceType.Total];

        [Serializable]
        public class OriginalIKData
        {
            protected VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

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
            }

            public bool enable;
            public bool autoRotation;
            public SpaceType spaceType;
            public GameObject parent;
            public Vector3 position;
            public Quaternion rotation;
            public float swivel;
            public SyncType defaultSyncType;

            public string name;
            public SolverType solverType;
            public bool resetRotations;  //CCD
            public int level;           //CCD
            public float limbDirection;   //Limb
            [Serializable]
            public class JointData
            {
                public GameObject bone;
                public float weight;

                [NonSerialized]
                public int boneIndex;
                [NonSerialized]
                public bool foldout;
            }
            public List<JointData> joints;

            public bool IsValid { get { return enable && solver != null && solver.IsValid; } }
            public bool IsUpdate { get { return IsValid && updateIKtarget && !synchroIKtarget; } }
            public GameObject Tip { get { return joints != null && joints.Count > 0 && joints[0].boneIndex >= 0 ? VAW.VA.Skeleton.Bones[joints[0].boneIndex] : null; } }
            public GameObject Root { get { return joints != null && joints.Count > 0 && joints[level - 1].boneIndex >= 0 ? VAW.VA.Skeleton.Bones[joints[level - 1].boneIndex] : null; } }

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
                            if (Root != null && Root.transform.parent != null)
                                getpos = Root.transform.parent.localToWorldMatrix.MultiplyPoint3x4(getpos);
                            break;
                        case SpaceType.Parent:
                            if (parent != null)
                                getpos = parent.transform.localToWorldMatrix.MultiplyPoint3x4(getpos);
                            break;
                        default:
                            Assert.IsTrue(false); getpos = position;
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
                            if (Root != null && Root.transform.parent != null)
                                setpos = Root.transform.parent.worldToLocalMatrix.MultiplyPoint3x4(setpos);
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
                            if (Root != null && Root.transform.parent != null)
                                getrot = Root.transform.parent.rotation * getrot;
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
                            if (Root != null && Root.transform.parent != null)
                                setrot = Quaternion.Inverse(Root.transform.parent.rotation) * setrot;
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

            [NonSerialized]
            public int rootBoneIndex;
            [NonSerialized]
            public int parentBoneIndex;
            [NonSerialized]
            public bool updateIKtarget;
            [NonSerialized]
            public bool synchroIKtarget;
            [NonSerialized]
            public GameObject[] syncBones;
            [NonSerialized]
            public SolverBase solver;
        }
        public List<OriginalIKData> ikData;

        public int[] ikTargetSelect;
        public int IKActiveTarget { get { return ikTargetSelect != null && ikTargetSelect.Length > 0 ? ikTargetSelect[0] : -1; } }

        private struct UpdateData
        {
            public float time;
            public Quaternion rotation;
        }
        private List<UpdateData>[] updateRotations;

        private class TmpCurves
        {
            public AnimationCurve[] curves = new AnimationCurve[4];

            public void Clear()
            {
                for (int i = 0; i < 4; i++)
                {
                    curves[i] = null;
                }
            }
        }
        private TmpCurves tmpCurves;

        private UDisc uDisc;

        private ReorderableList ikReorderableList;
        private bool advancedFoldout;

        #region Solver
        public abstract class SolverBase
        {
            protected VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

            public bool IsValid { get; protected set; }
            public Transform[] BoneTransforms { get; protected set; }
            public int[] BoneIndexes { get; protected set; }

            public Transform Tip { get { return BoneTransforms[0]; } }
            public Transform Root { get { return BoneTransforms[^1]; } }

            public virtual bool Initialize(GameObject[] bones, GameObject[] joints)
            {
                IsValid = false;
                BoneTransforms = null;
                BoneIndexes = null;

                if (joints.Length < 2) return false;

                #region Check
                {
                    for (int i = 0; i < joints.Length; i++)
                    {
                        if (joints[i] == null)
                            return false;
                        if (EditorCommon.ArrayIndexOf(bones, joints[i]) < 0)
                            return false;
                        {
                            int count = 0;
                            var t = joints[i].transform.parent;
                            while (t != null)
                            {
                                for (int j = i + 1; j < joints.Length; j++)
                                {
                                    if (joints[j] != null && t == joints[j].transform)
                                        count++;
                                }
                                t = t.parent;
                            }
                            if (count < joints.Length - (i + 1))
                                return false;
                        }
                    }
                }
                #endregion

                #region Set
                {
                    BoneTransforms = new Transform[joints.Length];
                    BoneIndexes = new int[joints.Length];
                    for (int i = 0; i < joints.Length; i++)
                    {
                        BoneTransforms[i] = joints[i].transform;
                        BoneIndexes[i] = EditorCommon.ArrayIndexOf(bones, joints[i]);
                    }
                }
                #endregion

                IsValid = true;

                return true;
            }

            public virtual Vector3 GetBasicDir()
            {
                const float ToleranceSq = 0.0001f;
                var posA = Root.position;
                var posB = Tip.position;
                var axis = posB - posA;
                axis.Normalize();
                if (axis.sqrMagnitude <= ToleranceSq)
                    return (BoneTransforms[^2].position - Root.position).normalized;
                if (BoneTransforms.Length <= 2)
                {
                    Vector3 cross;
                    if (Mathf.Abs(Vector3.Dot(axis, Root.up)) < 0.5f)
                    {
                        cross = Vector3.Cross(axis, Root.up);
                        cross.Normalize();
                    }
                    else
                    {
                        cross = Vector3.Cross(axis, Root.right);
                        cross.Normalize();
                    }
                    return cross;
                }
                else
                {
                    var posC = BoneTransforms[^2].position;
                    var vecCP = posC - (posA + axis * Vector3.Dot((posC - posA), axis));
                    vecCP.Normalize();
                    if (vecCP.sqrMagnitude <= ToleranceSq) return Root.up;
                    return vecCP;
                }
            }

            protected void FixReverseRotation()
            {
                for (int i = 0; i < BoneTransforms.Length; i++)
                {
                    var save = VAW.VA.BoneSaveTransforms[BoneIndexes[i]];
                    var rot = BoneTransforms[i].localRotation * Quaternion.Inverse(save.localRotation);
                    if (rot.w < 0f)
                    {
                        var rotation = BoneTransforms[i].localRotation;
                        for (int dof = 0; dof < 4; dof++)
                            rotation[dof] = -rotation[dof];
                        BoneTransforms[i].localRotation = rotation;
                    }
                }
            }

            public abstract void Update(Vector3 targetPos, Quaternion? targetRotation);

            public virtual float GetSwivel(GameObject[] bones) { return 0f; }

            public Quaternion GetTipAutoRotation()
            {
                return Tip.parent.rotation * VAW.VA.BoneSaveTransforms[BoneIndexes[0]].localRotation;
            }
        }
        #region Cyclic-Coordinate-Descent (CCD)
        public class SolverCCD : SolverBase
        {
            private const int Iteration = 16;

            public bool resetRotations;
            public float swivel;
            public float[] weights;

            private TransformPoseSave.SaveData[] transformSave;

            public override bool Initialize(GameObject[] bones, GameObject[] joints)
            {
                if (!base.Initialize(bones, joints))
                    return false;

                transformSave = new TransformPoseSave.SaveData[BoneTransforms.Length];
                for (int i = 0; i < transformSave.Length; i++)
                    transformSave[i] = new TransformPoseSave.SaveData();

                {
                    weights = new float[joints.Length];
                    for (int i = 0; i < joints.Length; i++)
                        weights[i] = 1f;
                }
                swivel = GetSwivel(bones);

                return true;
            }

            public override void Update(Vector3 targetPos, Quaternion? targetRotation)
            {
                if (!IsValid) return;

                #region Reset
                if (resetRotations)
                {
                    for (int i = 0; i < BoneTransforms.Length; i++)
                        BoneTransforms[i].localRotation = VAW.VA.BoneSaveTransforms[BoneIndexes[i]].localRotation;
                }
                #endregion 

                #region StraightAvoidance
                {
                    var vecTarget = targetPos - Root.position;
                    var lengthTarget = vecTarget.magnitude;
                    vecTarget.Normalize();
                    if (vecTarget.sqrMagnitude > 0f)
                    {
                        int count = 0;
                        float lengthTotal = 0f;
                        for (int i = BoneTransforms.Length - 1; i > 0; i--)
                        {
                            var vec = BoneTransforms[i - 1].position - BoneTransforms[i].position;
                            var lengthVec = vec.magnitude;
                            vec.Normalize();
                            if (vec.sqrMagnitude > 0f)
                            {
                                lengthTotal += lengthVec;
                                var dot = Vector3.Dot(vecTarget, vec);
                                if (Mathf.Approximately(Mathf.Abs(dot), 1f))
                                    count++;
                            }
                        }
                        if (lengthTarget < lengthTotal && count == BoneTransforms.Length - 1)
                        {
                            Root.rotation *= Quaternion.AngleAxis(1f, GetBasicDir());
                        }
                    }
                }
                #endregion

                const float ToleranceSq = 0.0001f;
                for (int i = 0; i < Iteration; i++)
                {
                    for (int j = 1; j < BoneTransforms.Length; j++)
                    {
                        Vector3 localTargetPos;
                        Vector3 localEffectorPos;
                        {
                            var invRot = Quaternion.Inverse(BoneTransforms[j].rotation);
                            var position = BoneTransforms[j].position;
                            localEffectorPos = invRot * (Tip.position - position);
                            localTargetPos = invRot * (targetPos - position);
                        }
                        localEffectorPos.Normalize();
                        localTargetPos.Normalize();
                        if (localEffectorPos.sqrMagnitude <= 0f || localTargetPos.sqrMagnitude <= 0f)
                            continue;
                        {
                            var rotationAdd = Quaternion.FromToRotation(localEffectorPos, localTargetPos);
                            if (weights[j] != 1f)
                                rotationAdd = Quaternion.Slerp(Quaternion.identity, rotationAdd, weights[j]);
                            BoneTransforms[j].localRotation *= rotationAdd;
                        }
                    }
                    if ((Tip.position - targetPos).sqrMagnitude < ToleranceSq)
                        break;
                }

                #region Swivel
                if (resetRotations && swivel != 0f)
                {
                    var axis = (Tip.position - Root.position).normalized;
                    if (axis.sqrMagnitude > 0f)
                    {
                        axis = Root.transform.worldToLocalMatrix.MultiplyVector(axis);
                        Root.localRotation *= Quaternion.AngleAxis(swivel, axis);
                    }
                }
                #endregion

                FixReverseRotation();

                if (targetRotation.HasValue)
                {
                    Tip.rotation = targetRotation.Value;
                }
                else
                {
                    Tip.rotation = GetTipAutoRotation();
                }
            }
            public override float GetSwivel(GameObject[] bones)
            {
                if (!IsValid || !resetRotations || BoneTransforms.Length < 3) return 0f;

                var center = BoneTransforms[1];

                #region Save
                var saveSwivel = swivel;
                for (int i = 0; i < BoneTransforms.Length; i++)
                    transformSave[i].Save(BoneTransforms[i]);
                #endregion

                for (int i = BoneTransforms.Length - 1; i >= 0; i--)
                {
                    if (bones[BoneIndexes[i]].transform == BoneTransforms[i])
                        continue;
#if UNITY_2022_3_OR_NEWER
                    bones[BoneIndexes[i]].transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
#else
                    var position = bones[BoneIndexes[i]].transform.position;
                    var rotation = bones[BoneIndexes[i]].transform.rotation;
#endif
                    BoneTransforms[i].SetPositionAndRotation(position, rotation);
                }
                Vector3 vecAxis = (Tip.position - Root.position).normalized;
                Vector3 vecAfter;
                {
                    var u = Vector3.Dot((center.position - Root.position), vecAxis) / vecAxis.sqrMagnitude;
                    var posP = Root.position + vecAxis * u;
                    vecAfter = (center.position - posP).normalized;
                }

                Vector3 vecBefore;
                try
                {
                    swivel = 0f;
                    Update(Tip.position, Tip.rotation);
                    {
                        var u = Vector3.Dot((center.position - Root.position), vecAxis) / vecAxis.sqrMagnitude;
                        var posP = Root.position + vecAxis * u;
                        vecBefore = (center.position - posP).normalized;
                    }
                }
                finally
                {
                    swivel = saveSwivel;

                    #region Load
                    for (int i = 0; i < BoneTransforms.Length; i++)
                        transformSave[i].LoadLocal(BoneTransforms[i]);
                    #endregion
                }

                return Vector3.SignedAngle(vecBefore, vecAfter, vecAxis);
            }
        }
        #endregion
        #region Limb
        public class SolverLimb : SolverBase
        {
            public Transform Lower { get { return BoneTransforms[1]; } }
            public Transform Upper { get { return BoneTransforms[2]; } }

            public float swivel;
            public float direction;

            private TransformPoseSave.SaveData[] transformSave;
            private Vector3 lowerAxis;
            private Vector3 lowerDirection;
            private Quaternion linearizationForward;
            private Quaternion linearizationLower;

            public override bool Initialize(GameObject[] bones, GameObject[] joints)
            {
                if (joints.Length != 3)
                    return false;
                if (!base.Initialize(bones, joints))
                    return false;

                transformSave = new TransformPoseSave.SaveData[BoneTransforms.Length];
                for (int i = 0; i < transformSave.Length; i++)
                    transformSave[i] = new TransformPoseSave.SaveData();

                direction = 0f;
                swivel = GetSwivel(bones);

                return true;
            }

            public override Vector3 GetBasicDir()
            {
                var rotation = (Upper.rotation * Quaternion.Inverse(VAW.VA.BoneSaveTransforms[BoneIndexes[2]].rotation)) * Quaternion.Inverse(linearizationForward);
                return rotation * GetLowerAxis();
            }
            public override void Update(Vector3 targetPos, Quaternion? targetRotation)
            {
                if (!IsValid) return;

                var upperLength = Vector3.Distance(Lower.position, Upper.position);
                var lowerLength = Vector3.Distance(Tip.position, Lower.position);
                if (upperLength <= 0f || lowerLength <= 0f)
                    return;

                #region Reset
                for (int i = BoneTransforms.Length - 1; i >= 0; i--)
                    BoneTransforms[i].rotation = VAW.VA.BoneSaveTransforms[BoneIndexes[i]].rotation;
                {
                    var vLower = (Lower.position - Upper.position).normalized;
                    var lookRot = Quaternion.LookRotation(vLower);
                    {
                        linearizationForward = Quaternion.Inverse(lookRot);
                    }
                    {
                        var invRot = Quaternion.Inverse(Lower.rotation);
                        linearizationLower = Quaternion.FromToRotation(invRot * (Tip.position - Lower.position).normalized, invRot * vLower);
                    }
                    {
                        lowerAxis = lookRot * Vector3.up;
                        lowerAxis = linearizationForward * lowerAxis;
                        lowerDirection = Vector3.Cross(Vector3.forward, lowerAxis).normalized;
                    }
                }
                #endregion

                Upper.rotation = linearizationForward * Upper.rotation;
                Lower.localRotation *= linearizationLower;

                var vGoal = linearizationForward * (targetPos - Upper.position);

                Quaternion upperRot = Quaternion.identity;
                Quaternion lowerRot;
                {
                    const float Tolerance = 0.000001f;
                    var vAxis = GetLowerAxis();
                    var vDirection = GetLowerDirection();
                    var distGoal = vGoal.magnitude;
                    //Far
                    if (distGoal >= upperLength + lowerLength)
                    {
                        distGoal = upperLength + lowerLength - Tolerance;
                        vGoal = vGoal.normalized * distGoal;
                    }
                    //Near
                    if (distGoal < Tolerance)
                    {
                        lowerRot = Quaternion.AngleAxis(180f, vAxis);
                        upperRot = Quaternion.identity;
                    }
                    else if (upperLength >= lowerLength && distGoal < upperLength - lowerLength + Tolerance)
                    {
                        lowerRot = Quaternion.AngleAxis(180f, vAxis);
                        upperRot = Quaternion.FromToRotation(Vector3.forward, vGoal.normalized);
                    }
                    else if (upperLength < lowerLength && distGoal < lowerLength - upperLength + Tolerance)
                    {
                        lowerRot = Quaternion.AngleAxis(180f, vAxis);
                        upperRot = Quaternion.FromToRotation(Vector3.forward, -vGoal.normalized);
                    }
                    else
                    {
                        //Ry
                        {
                            float rY = Mathf.Acos(Mathf.Clamp((distGoal * distGoal - upperLength * upperLength - lowerLength * lowerLength) / (2f * upperLength * lowerLength), -1f, 1f));
                            Assert.IsFalse(rY < 0);
                            lowerRot = Quaternion.AngleAxis(rY * Mathf.Rad2Deg, vAxis);
                        }
                        Vector3 lowerPos;
                        {
                            var vGoalN = vGoal.normalized;
                            Vector3 posCenter;
                            float circleRadius;
                            {
                                var cosAlpha = Mathf.Min((distGoal * distGoal + upperLength * upperLength - lowerLength * lowerLength) / (2f * distGoal * upperLength), 1f);
                                posCenter = cosAlpha * upperLength * vGoalN;
                                circleRadius = Mathf.Sqrt(1f - cosAlpha * cosAlpha) * upperLength;
                            }
                            var vU = (vDirection - Vector3.Dot(vDirection, vGoalN) * vGoalN).normalized;
                            var radSwivel = swivel * Mathf.Deg2Rad;
                            lowerPos = posCenter + circleRadius * (Mathf.Cos(radSwivel) * vU + Mathf.Sin(radSwivel) * Vector3.Cross(vGoalN, vU));
                        }
                        //R1
                        {
                            var vR1Z = lowerPos.normalized;
                            var vR1X = (vGoal - Vector3.Dot(vGoal, vR1Z) * vR1Z).normalized;
                            var vR1Y = Vector3.Cross(vR1Z, vR1X);
                            {
                                var forward = new Vector3(vR1X.z, vR1Y.z, vR1Z.z);
                                var upwards = new Vector3(vR1X.y, vR1Y.y, vR1Z.y);
                                if (forward.sqrMagnitude > 0f && upwards.sqrMagnitude > 0f)
                                {
                                    upperRot = Quaternion.LookRotation(forward, upwards);
                                    upperRot = Quaternion.Inverse(upperRot);
                                }
                            }
                            upperRot *= Quaternion.AngleAxis(direction, Vector3.forward);
                        }
                    }
                }
                Lower.rotation = lowerRot * Lower.rotation;
                Upper.rotation = upperRot * Upper.rotation;

                Upper.rotation = Quaternion.Inverse(linearizationForward) * Upper.rotation;

                FixReverseRotation();

                if (targetRotation.HasValue)
                {
                    Tip.rotation = targetRotation.Value;
                }
                else
                {
                    Tip.rotation = GetTipAutoRotation();
                }
            }
            public override float GetSwivel(GameObject[] bones)
            {
                if (!IsValid) return 0f;

                #region Save
                var saveSwivel = swivel;
                for (int i = 0; i < BoneTransforms.Length; i++)
                    transformSave[i].Save(BoneTransforms[i]);
                #endregion

                for (int i = BoneTransforms.Length - 1; i >= 0; i--)
                {
                    if (bones[BoneIndexes[i]].transform == BoneTransforms[i])
                        continue;
#if UNITY_2022_3_OR_NEWER
                    bones[BoneIndexes[i]].transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
#else
                    var position = bones[BoneIndexes[i]].transform.position;
                    var rotation = bones[BoneIndexes[i]].transform.rotation;
#endif
                    BoneTransforms[i].SetPositionAndRotation(position, rotation);
                }

                Vector3 vecAxis = (Tip.position - Root.position).normalized;
                Vector3 vecAfter;
                {
                    var u = Vector3.Dot((Lower.position - Root.position), vecAxis) / vecAxis.sqrMagnitude;
                    var posP = Root.position + vecAxis * u;
                    vecAfter = (Lower.position - posP).normalized;
                }

                Vector3 vecBefore;
                try
                {
                    swivel = 0f;
                    Update(Tip.position, Tip.rotation);
                    {
                        var u = Vector3.Dot((Lower.position - Root.position), vecAxis) / vecAxis.sqrMagnitude;
                        var posP = Root.position + vecAxis * u;
                        vecBefore = (Lower.position - posP).normalized;
                    }
                }
                finally
                {
                    swivel = saveSwivel;

                    #region Load
                    for (int i = 0; i < BoneTransforms.Length; i++)
                        transformSave[i].LoadLocal(BoneTransforms[i]);
                    #endregion
                }

                return Vector3.SignedAngle(vecBefore, vecAfter, vecAxis);
            }

            public float GetDirectionFromTransform()
            {
                if (!IsValid) return 0f;

                var vUpper = (Lower.position - Upper.position).normalized;
                var vLower = (Tip.position - Lower.position).normalized;
                if (Mathf.Abs(Vector3.Dot(vUpper, vLower)) > 1f - 0.0001f)
                    return 0f;

                var vFw = (Tip.position - Upper.position).normalized;

                Vector3 direction;
                {
                    var posP = Upper.position + vFw * Vector3.Dot((Lower.position - Upper.position), vFw);
                    direction = (Lower.position - posP).normalized;
                }
                Vector3 basicDirection;
                {
                    var rotation = (Upper.rotation * Quaternion.Inverse(VAW.VA.BoneSaveTransforms[BoneIndexes[2]].rotation)) * Quaternion.Inverse(linearizationForward);
                    basicDirection = Vector3.Cross(vFw, rotation * lowerAxis);
                }

                float result;
                {
                    var offsetRot = Quaternion.FromToRotation(direction, basicDirection);
                    offsetRot.ToAngleAxis(out result, out Vector3 tmpAxis);
                    if (Vector3.Dot(vFw, tmpAxis) < 0f)
                        result = -result;
                    result = Mathf.Repeat(result + 180f, 360f) - 180f;
                }
                return result;
            }

            private Vector3 GetLowerAxis()
            {
                return Quaternion.AngleAxis(-direction, Vector3.forward) * lowerAxis;
            }
            private Vector3 GetLowerDirection()
            {
                return Quaternion.AngleAxis(-direction, Vector3.forward) * lowerDirection;
            }
        }
        #endregion
        #endregion

        public void Initialize()
        {
            Release();

            ikData = new List<OriginalIKData>();
            ikTargetSelect = null;

            updateRotations = new List<UpdateData>[VAW.VA.Bones.Length];
            tmpCurves = new TmpCurves();

            uDisc = new UDisc();

            UpdateReorderableList();

            UpdateGUIContentStrings();
            Language.OnLanguageChanged += UpdateGUIContentStrings;

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }
        public void Release()
        {
            Language.OnLanguageChanged -= UpdateGUIContentStrings;
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            ikData = null;
            ikTargetSelect = null;
            updateRotations = null;
            tmpCurves = null;
            uDisc = null;
            ikReorderableList = null;
        }

        public void LoadIKSaveSettings(VeryAnimationSaveSettings.OriginalIKData[] saveIkData)
        {
            if (saveIkData != null)
            {
                ikData.Clear();
                if (ikReorderableList != null)
                    ikReorderableList.index = -1;
                ikTargetSelect = null;
                foreach (var d in saveIkData)
                {
                    if (d.level < SolverLevelMin || d.joints.Count < SolverLevelMin)
                        continue;

                    var data = new OriginalIKData()
                    {
                        enable = d.enable,
                        autoRotation = d.autoRotation,
                        spaceType = (OriginalIKData.SpaceType)d.spaceType,
                        parent = d.parent,
                        position = d.position,
                        rotation = d.rotation,
                        swivel = d.swivel,
                        defaultSyncType = (OriginalIKData.SyncType)d.defaultSyncType,
                        name = d.name,
                        solverType = (SolverType)d.solverType,
                        resetRotations = d.resetRotations,
                        level = d.level,
                        limbDirection = d.limbDirection,
                        joints = new List<OriginalIKData.JointData>(),
                    };
                    //Path
                    if (!string.IsNullOrEmpty(d.parentPath))
                    {
                        var t = VAW.GameObject.transform.Find(d.parentPath);
                        if (t != null)
                            data.parent = t.gameObject;
                    }
                    foreach (var joint in d.joints)
                    {
                        var jdata = new OriginalIKData.JointData()
                        {
                            bone = joint.bone,
                            weight = joint.weight,
                        };
                        //Path
                        if (!string.IsNullOrEmpty(joint.bonePath))
                        {
                            var t = VAW.GameObject.transform.Find(joint.bonePath);
                            if (t != null)
                                jdata.bone = t.gameObject;
                        }
                        data.joints.Add(jdata);
                    }
                    ikData.Add(data);
                }
                for (int target = 0; target < ikData.Count; target++)
                {
                    UpdateSolver(target);
                    SetSynchroIKtargetOriginalIK(target);
                }
            }
        }
        public VeryAnimationSaveSettings.OriginalIKData[] SaveIKSaveSettings()
        {
            if (VAW.VA.originalIK == null || ikData == null)
                return null;
            var saveIkData = new List<VeryAnimationSaveSettings.OriginalIKData>();
            foreach (var d in ikData)
            {
                var data = new VeryAnimationSaveSettings.OriginalIKData()
                {
                    enable = d.enable,
                    autoRotation = d.autoRotation,
                    spaceType = (int)d.spaceType,
                    parent = d.parent,
                    position = d.position,
                    rotation = d.rotation,
                    swivel = d.swivel,
                    defaultSyncType = (int)d.defaultSyncType,
                    name = d.name,
                    solverType = (int)d.solverType,
                    resetRotations = d.resetRotations,
                    level = d.level,
                    limbDirection = d.limbDirection,
                    joints = new List<VeryAnimationSaveSettings.OriginalIKData.JointData>(),
                    //Path
                    parentPath = d.parent != null ? AnimationUtility.CalculateTransformPath(d.parent.transform, VAW.GameObject.transform) : "",
                };
                for (int i = 0; i < d.joints.Count; i++)
                {
                    data.joints.Add(new VeryAnimationSaveSettings.OriginalIKData.JointData()
                    {
                        bone = d.joints[i].bone,
                        weight = d.joints[i].weight,
                        //Path
                        bonePath = d.joints[i].bone != null ? AnimationUtility.CalculateTransformPath(d.joints[i].bone.transform, VAW.GameObject.transform) : "",
                    });
                }
                saveIkData.Add(data);
            }
            return saveIkData.ToArray();
        }

        private int CreateIKData(GameObject jointTip)
        {
            {
                var boneIndex = VAW.VA.BonesIndexOf(jointTip);
                if (boneIndex < 0) return -1;
                if (VAW.VA.IsHuman && VAW.VA.HumanoidConflict[boneIndex]) return -1;
            }

            var data = new OriginalIKData()
            {
                enable = true,
                name = jointTip.name,
                solverType = SolverType.CcdIK,
                resetRotations = true,
                joints = new List<OriginalIKData.JointData>(),
            };
            {
                var t = jointTip.transform;
                for (int i = 0; i < 3; i++)
                {
                    if (!IsValidAddBone(t.gameObject))
                        break;
                    data.joints.Add(new OriginalIKData.JointData()
                    {
                        bone = t.gameObject,
                        weight = 1f,
                    });
                    t = t.parent;
                    if (t == null) break;
                }
                if (data.joints.Count < 2)
                    return -1;
                data.level = data.joints.Count;
            }
            ikData.Add(data);
            UpdateSolver(ikData.Count - 1);
            SynchroSet(ikData.Count - 1);
            return ikData.Count - 1;
        }
        private bool ChangeSolverType(int target, SolverType solverType)
        {
            if (target < 0 || target >= ikData.Count)
                return false;
            ikData[target].solverType = solverType;
            switch (ikData[target].solverType)
            {
                case SolverType.LimbIK:
                    ikData[target].level = 3;
                    while (ikData[target].joints.Count < ikData[target].level)
                    {
                        ikData[target].joints.Add(new OriginalIKData.JointData()
                        {
                            bone = null,
                            weight = 1f,
                        });
                    }
                    break;
            }

            UpdateSolver(target);

            if (ikData[target].solver.IsValid)
            {
                switch (ikData[target].solverType)
                {
                    case SolverType.LimbIK:
                        #region Auto set limbDirection
                        {
                            var solverLimb = ikData[target].solver as SolverLimb;
                            ikData[target].limbDirection = solverLimb.GetDirectionFromTransform();
                            solverLimb.direction = ikData[target].limbDirection;
                        }
                        #endregion
                        break;
                }
                SynchroSet(target);
            }

            return true;
        }
        public bool ChangeTypeSetting(int target, float add)
        {
            if (target < 0 || target >= ikData.Count)
                return false;
            switch (ikData[target].solverType)
            {
                case SolverType.CcdIK:
                case SolverType.LookAt:
                    if (add > 0)
                    {
                        Transform t = null;
                        if (ikData[target].joints.Count > 0)
                        {
                            var root = ikData[target].joints[ikData[target].level - 1].bone;
                            if (root != null)
                                t = root.transform.parent;
                        }
                        for (int i = 0; i < add; i++)
                        {
                            if (t == null) break;
                            if (!IsValidAddBone(t.gameObject))
                                break;
                            if (ikData[target].level + 1 > SolverLevelMax)
                                break;
                            if (ikData[target].joints.Count < ++ikData[target].level)
                            {
                                ikData[target].joints.Add(new OriginalIKData.JointData()
                                {
                                    bone = t.gameObject,
                                    weight = 1f,
                                });
                            }
                            t = t.parent;
                        }
                    }
                    else if (add < 0)
                    {
                        for (int i = 0; i < Math.Abs(add); i++)
                        {
                            if (ikData[target].level - 1 < SolverLevelMin)
                                break;
                            ikData[target].level--;
                        }
                    }
                    break;
                case SolverType.LimbIK:
                    {
                        ikData[target].limbDirection = Mathf.Repeat(ikData[target].limbDirection + add + 180f, 360f) - 180f;
                    }
                    break;
                default:
                    return false;
            }
            UpdateSolver(target);
            VAW.VA.SetUpdateIKtargetOriginalIK(target);
            return true;
        }

        private void UpdateGUIContentStrings()
        {
            for (int i = 0; i <= (int)SolverType.LimbIK; i++)
            {
                SolverTypeStrings[i] = new GUIContent(Language.GetContent(Language.Help.OriginalIKTypeCcdIK + i));
            }
            for (int i = 0; i < (int)OriginalIKData.SpaceType.Total; i++)
            {
                IKSpaceTypeStrings[i] = new GUIContent(Language.GetContent(Language.Help.SelectionOriginalIKSpaceTypeGlobal + i));
            }
        }

        private void UpdateReorderableList()
        {
            ikReorderableList = null;
            if (ikData == null) return;
            ikReorderableList = new ReorderableList(ikData, typeof(OriginalIKData), true, true, true, true);
            ikReorderableList.drawHeaderCallback = rect =>
            {
                float x = rect.x;
                {
                    const float ButtonWidth = 100f;
                    #region Add
                    {
                        var r = rect;
                        r.width = ButtonWidth;
                        if (GUI.Button(r, Language.GetContent(Language.Help.OriginalIKTemplate), EditorStyles.toolbarDropDown))
                        {
                            var originalIKTemplates = new Dictionary<string, string>();
                            {
                                var guids = AssetDatabase.FindAssets("t:originaliktemplate");
                                for (int i = 0; i < guids.Length; i++)
                                {
                                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                                    var name = path["Assets/".Length..];
                                    originalIKTemplates.Add(name, path);
                                }
                            }

                            var menu = new GenericMenu();
                            {
                                var enu = originalIKTemplates.GetEnumerator();
                                while (enu.MoveNext())
                                {
                                    var value = enu.Current.Value;
                                    menu.AddItem(new GUIContent(enu.Current.Key), false, () =>
                                    {
                                        var originalIKTemplate = AssetDatabase.LoadAssetAtPath<OriginalIKTemplate>(value);
                                        if (originalIKTemplate != null)
                                        {
                                            Undo.RecordObject(VAW, "Template OriginalIK");
                                            LoadIKSaveSettings(originalIKTemplate.originalIkData);
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
                            Undo.RecordObject(VAW, "Clear Original IK Data");
                            ikData.Clear();
                            ikReorderableList.index = -1;
                            ikTargetSelect = null;
                            VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
                        }
                    }
                    #endregion
                    #region Save as
                    {
                        var r = rect;
                        r.width = ButtonWidth;
                        r.x = rect.xMax - r.width;
                        if (GUI.Button(r, Language.GetContent(Language.Help.OriginalIKSaveAs), EditorStyles.toolbarButton))
                        {
                            string path = EditorUtility.SaveFilePanel("Save as OriginalIK Template", VAE.TemplateSaveDefaultDirectory, string.Format("{0}_OriginalIK.asset", VAW.GameObject.name), "asset");
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
                                    var originalIKTemplate = ScriptableObject.CreateInstance<OriginalIKTemplate>();
                                    {
                                        originalIKTemplate.originalIkData = SaveIKSaveSettings();
                                    }
                                    try
                                    {
                                        VeryAnimationWindow.CustomAssetModificationProcessor.Pause();
                                        AssetDatabase.CreateAsset(originalIKTemplate, path);
                                    }
                                    finally
                                    {
                                        VeryAnimationWindow.CustomAssetModificationProcessor.Resume();
                                    }
                                    VAC.Focus();
                                }
                            }
                        }
                    }
                    #endregion
                }
            };
            ikReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= ikData.Count)
                    return;
                var isValid = ikData[index].solver != null && ikData[index].solver.IsValid;

                var saveColor = GUI.backgroundColor;

                float x = rect.x;
                {
                    var r = rect;
                    r.x = x;
                    r.y += 2;
                    r.height -= 2;
                    r.width = 16;
                    rect.xMin += r.width;
                    x = rect.x;
                    if (!isValid)
                    {
                        ikData[index].enable = false;
                        advancedFoldout = true;
                        GUI.backgroundColor = Color.red;
                    }
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.Toggle(r, ikData[index].enable);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ChangeTargetIK(index);
                        }
                    }
                    GUI.backgroundColor = saveColor;
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
                    EditorGUI.LabelField(r, ikData[index].name);
                }

                {
                    var r = rect;
                    r.width = 100f;
                    r.x = rect.xMax - r.width - 14;
                    EditorGUI.LabelField(r, string.Format("{0} : {1}", SolverTypeStrings[(int)ikData[index].solverType].text, IKSpaceTypeStrings[(int)ikData[index].spaceType].text), VAW.GuiStyleMiddleRightGreyMiniLabel);
                }

                EditorGUI.EndDisabledGroup();

                if (ikReorderableList.index == index)
                {
                    var r = rect;
                    r.y += 2;
                    r.height -= 2;
                    r.width = 12;
                    r.x = rect.xMax - r.width;
                    advancedFoldout = EditorGUI.Foldout(r, advancedFoldout, new GUIContent("", "Advanced"), true);
                }
            };
            ikReorderableList.onCanAddCallback = list =>
            {
                if (VAW.VA.SelectionActiveBone < 0) return false;
                if (VAW.VA.IsHuman && VAW.VA.HumanoidConflict[VAW.VA.SelectionActiveBone]) return false;
                return ikData.FindIndex((data) => data.Tip == VAW.VA.Skeleton.Bones[VAW.VA.SelectionActiveBone]) < 0;
            };
            ikReorderableList.onAddCallback = list =>
            {
                VAW.VA.originalIK.ChangeSelectionIK();
                VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
            };
            ikReorderableList.onCanRemoveCallback = list =>
            {
                return list.index >= 0 && list.index < ikData.Count;
            };
            ikReorderableList.onRemoveCallback = list =>
            {
                Undo.RecordObject(VAW, "Change Original IK Data");
                ikData.RemoveAt(list.index);
                ikTargetSelect = null;
                VAW.SetRepaintGUI(VeryAnimationWindow.RepaintGUI.All);
            };
            ikReorderableList.onSelectCallback = list =>
            {
                if (list.index >= 0 && list.index < ikData.Count)
                {
                    if (ikData[list.index].enable)
                        VAW.VA.SelectOriginalIKTargetPlusKey(list.index);
                    else
                    {
                        var index = list.index;
                        VAW.VA.SelectGameObject(ikData[list.index].Tip);
                        list.index = index;
                    }
                }
            };
        }
        private void UpdateSolver(int target)
        {
            ikData[target].solver = null;
            if (target < 0 || target >= ikData.Count || ikData[target].joints == null) return;
            var joints = new GameObject[ikData[target].level];
            for (int i = 0; i < ikData[target].level; i++)
            {
                var boneIndex = VAW.VA.BonesIndexOf(ikData[target].joints[i].bone);
                ikData[target].joints[i].boneIndex = boneIndex;
                joints[i] = boneIndex >= 0 ? VAW.VA.Skeleton.Bones[boneIndex] : null;
            }
            switch (ikData[target].solverType)
            {
                case SolverType.CcdIK:
                    {
                        var solverCcd = new SolverCCD();
                        solverCcd.Initialize(VAW.VA.Skeleton.Bones, joints);
                        ikData[target].solver = solverCcd;
                    }
                    break;
                case SolverType.LimbIK:
                    {
                        var solverLimb = new SolverLimb();
                        solverLimb.Initialize(VAW.VA.Skeleton.Bones, joints);
                        ikData[target].solver = solverLimb;
                    }
                    break;
            }
            SetSolverParam(target);
        }

        private bool IsErrorJoint(int target, int index)
        {
            var data = ikData[target];
            if (target < 0 || target >= ikData.Count || data.joints == null)
                return true;
            if (index < 0 || index >= data.level || data.joints[index].bone == null)
                return true;
            {
                var indexBone = data.joints[index].bone.transform;
                var t = data.joints[0].bone.transform;
                int level = -1;
                while (t != null)
                {
                    level = data.joints.FindIndex(x => x.bone == t.gameObject);
                    if (t == indexBone)
                        break;
                    if (level > index)
                        break;
                    t = t.parent;
                }
                if (level != index)
                    return true;
            }
            return false;
        }
        private bool IsValidAddBone(GameObject gameObject)
        {
            var boneIndex = VAW.VA.BonesIndexOf(gameObject);
            if (boneIndex < 0) return false;
            if (VAW.VA.IsHuman && VAW.VA.HumanoidConflict[boneIndex]) return false;
            if (ikData.FindIndex((d) =>
            {
                if (d.joints == null) return false;
                for (int i = 0; i < d.level; i++)
                {
                    if (d.joints[i].bone == VAW.VA.Bones[boneIndex])
                        return true;
                }
                return false;
            }) >= 0) return false;
            return true;
        }
        private int GetMirrorTarget(int target)
        {
            if (target >= 0 && target < ikData.Count)
            {
                if (VAW.VA.Skeleton.BoneDictionary.TryGetValue(ikData[target].Tip, out int boneIndex))
                {
                    if (boneIndex >= 0 && VAW.VA.MirrorBoneIndexes[boneIndex] >= 0)
                    {
                        for (int i = 0; i < ikData.Count; i++)
                        {
                            if (ikData[i].Tip == VAW.VA.Skeleton.Bones[VAW.VA.MirrorBoneIndexes[boneIndex]])
                                return i;
                        }
                    }
                }
            }
            return -1;
        }

        public void OnSelectionChange()
        {
            if (ikReorderableList != null)
            {
                if (IKActiveTarget >= 0 && IKActiveTarget < ikReorderableList.count)
                {
                    ikReorderableList.index = IKActiveTarget;
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
                    SetSynchroIKtargetOriginalIK(target);
                }
            }
        }

        public void UpdateSynchroIKSet()
        {
            for (int i = 0; i < ikData.Count; i++)
            {
                if (ikData[i].enable && ikData[i].synchroIKtarget)
                {
                    SynchroSet(i);
                }
                ikData[i].synchroIKtarget = false;
            }
        }
        [Flags]
        public enum SynchroSetFlags : UInt32
        {
            None = 0,
            SceneObject = (1 << 0),
            Default = UInt32.MaxValue,
        }
        public void SynchroSet(int target, SynchroSetFlags syncFlags = SynchroSetFlags.Default)
        {
            if (target < 0 || target >= ikData.Count) return;
            var data = ikData[target];
            if (data.solver == null || !data.solver.IsValid) return;

            if (syncFlags == SynchroSetFlags.Default)
            {
                syncFlags = SynchroSetFlags.None;

                if (data.defaultSyncType >= OriginalIKData.SyncType.SceneObject)
                {
                    syncFlags |= SynchroSetFlags.SceneObject;
                }
            }

            data.rootBoneIndex = data.joints.Count > 0 ? data.joints[data.level - 1].boneIndex : -1;
            data.parentBoneIndex = -1;
            switch (data.spaceType)
            {
                case OriginalIKData.SpaceType.Local:
                    data.parentBoneIndex = VAW.VA.ParentBoneIndexes[data.rootBoneIndex];
                    break;
                case OriginalIKData.SpaceType.Parent:
                    data.parentBoneIndex = VAW.VA.BonesIndexOf(data.parent);
                    break;
            }

            foreach (var joint in data.joints)
            {
                joint.boneIndex = VAW.VA.BonesIndexOf(joint.bone);
            }

            bool done = false;
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            float swivelRotation = 0f;
            GameObject[] syncBones = VAW.VA.Skeleton.Bones;

            #region SceneObject
            if (!done && syncFlags.HasFlag(SynchroSetFlags.SceneObject))
            {
                syncBones = VAW.VA.Bones;
                var tipBone = syncBones[data.solver.BoneIndexes[0]].transform;
#if UNITY_2022_3_OR_NEWER
                tipBone.GetPositionAndRotation(out position, out rotation);
#else
                position = tipBone.position;
                rotation = tipBone.rotation;
#endif
                swivelRotation = data.solver.GetSwivel(syncBones);
                done = true;
            }
            #endregion

            #region Skeleton
            if (!done)
            {
                var tipBone = syncBones[data.solver.BoneIndexes[0]].transform;
#if UNITY_2022_3_OR_NEWER
                tipBone.GetPositionAndRotation(out position, out rotation);
#else
                position = tipBone.position;
                rotation = tipBone.rotation;
#endif
                swivelRotation = data.solver.GetSwivel(syncBones);
            }
            #endregion

            switch (data.spaceType)
            {
                case OriginalIKData.SpaceType.Global:
                case OriginalIKData.SpaceType.Local:
                    data.WorldPosition = position;
                    data.WorldRotation = rotation;
                    data.swivel = Mathf.Repeat(swivelRotation + 180f, 360f) - 180f;
                    break;
                case OriginalIKData.SpaceType.Parent:
                    //not update
                    break;
            }
            data.syncBones = syncBones;
        }

        private void SetSolverParam(int target)
        {
            if (target < 0 || target >= ikData.Count) return;
            var data = ikData[target];
            if (!data.IsValid) return;
            switch (data.solverType)
            {
                case SolverType.CcdIK:
                    {
                        var solverCcd = data.solver as SolverCCD;
                        solverCcd.resetRotations = data.resetRotations;
                        solverCcd.swivel = data.swivel;
                        for (int i = 0; i < data.level; i++)
                        {
                            solverCcd.weights[i] = data.joints[i].weight;
                        }
                    }
                    break;
                case SolverType.LimbIK:
                    {
                        var solverLimb = data.solver as SolverLimb;
                        solverLimb.swivel = data.swivel;
                        solverLimb.direction = data.limbDirection;
                    }
                    break;
                case SolverType.LookAt:
                    break;
            }
        }
        public void UpdateIK()
        {
            if (!GetUpdateIKtargetAll()) return;

            for (int boneIndex = 0; boneIndex < updateRotations.Length; boneIndex++)
            {
                if (updateRotations[boneIndex] == null) continue;
                updateRotations[boneIndex].Clear();
            }
            tmpCurves.Clear();

            #region Loop
            int loopCount = 1;
            bool baseParent = false;
            {
                if (VAW.VA.IsHuman &&
                    VAW.VA.rootCorrectionMode == VeryAnimation.RootCorrectionMode.Disable)
                {
                    loopCount = Math.Max(loopCount, 2);
                }
                foreach (var data in ikData)
                {
                    if (data.IsValid &&
                        data.spaceType == OriginalIKData.SpaceType.Parent &&
                        data.parentBoneIndex >= 0)
                    {
                        baseParent = true;
                        loopCount = Math.Max(loopCount, 2);
                    }
                }
            }
            for (int loop = 0; loop < loopCount; loop++)
            {
                if (baseParent)
                {
                    VAW.VA.SampleAnimation(VeryAnimation.EditObjectFlag.SceneObject);
                }
                VAW.VA.SampleAnimation(VeryAnimation.EditObjectFlag.Skeleton);

                #region Update
                {
                    #region OriginalIK
                    for (int i = 0; i < ikData.Count; i++)
                    {
                        var data = ikData[i];
                        if (!data.IsUpdate)
                            continue;
                        SetSolverParam(i);
                        {
                            Quaternion? worldRotation = null;
                            if (!data.autoRotation)
                                worldRotation = data.WorldRotation;
                            data.solver.Update(data.WorldPosition, worldRotation);
                        }
                    }
                    #endregion
                }
                #endregion

                #region SetValue
                {
                    for (int target = 0; target < ikData.Count; target++)
                    {
                        var data = ikData[target];
                        if (!data.IsUpdate)
                            continue;
                        for (int i = 0; i < data.solver.BoneIndexes.Length; i++)
                        {
                            var boneIndex = data.solver.BoneIndexes[i];
                            if (boneIndex < 0) continue;
                            if (updateRotations[boneIndex] == null)
                            {
                                updateRotations[boneIndex] = new List<UpdateData>();
                            }
                            updateRotations[boneIndex].Add(new UpdateData() { time = VAW.VA.CurrentTime, rotation = data.solver.BoneTransforms[i].localRotation });
                        }
                    }
                }
                #endregion

                #region Write
                for (int boneIndex = 0; boneIndex < updateRotations.Length; boneIndex++)
                {
                    if (updateRotations[boneIndex] == null || updateRotations[boneIndex].Count == 0) continue;
                    var mode = VAW.VA.GetHaveAnimationCurveTransformRotationMode(boneIndex);
                    if (mode == URotationCurveInterpolation.Mode.Undefined)
                        mode = URotationCurveInterpolation.Mode.RawQuaternions;
                    if (mode == URotationCurveInterpolation.Mode.RawQuaternions)
                    {
                        #region RawQuaternions
                        for (int dof = 0; dof < 4; dof++)
                        {
                            tmpCurves.curves[dof] = VAW.VA.GetAnimationCurveTransformRotation(boneIndex, dof, mode);
                        }
                        for (int i = 0; i < updateRotations[boneIndex].Count; i++)
                        {
                            var rotation = VAW.VA.FixReverseRotationQuaternion(tmpCurves.curves, updateRotations[boneIndex][i].time, updateRotations[boneIndex][i].rotation);
                            for (int dof = 0; dof < 4; dof++)
                            {
                                VAW.VA.SetKeyframe(tmpCurves.curves[dof], updateRotations[boneIndex][i].time, rotation[dof]);
                            }
                        }
                        for (int dof = 0; dof < 4; dof++)
                        {
                            VAW.VA.SetAnimationCurveTransformRotation(boneIndex, dof, mode, tmpCurves.curves[dof]);
                        }
                        #endregion
                    }
                    else
                    {
                        #region RawEuler
                        for (int dof = 0; dof < 3; dof++)
                        {
                            tmpCurves.curves[dof] = VAW.VA.GetAnimationCurveTransformRotation(boneIndex, dof, mode);
                        }
                        for (int i = 0; i < updateRotations[boneIndex].Count; i++)
                        {
                            var eulerAngles = VAW.VA.FixReverseRotationEuler(tmpCurves.curves, updateRotations[boneIndex][i].time, updateRotations[boneIndex][i].rotation.eulerAngles);
                            for (int dof = 0; dof < 3; dof++)
                            {
                                VAW.VA.SetKeyframe(tmpCurves.curves[dof], updateRotations[boneIndex][i].time, eulerAngles[dof]);
                            }
                        }
                        for (int dof = 0; dof < 3; dof++)
                        {
                            VAW.VA.SetAnimationCurveTransformRotation(boneIndex, dof, mode, tmpCurves.curves[dof]);
                        }
                        #endregion
                    }
                    updateRotations[boneIndex].Clear();
                }
                tmpCurves.Clear();
                #endregion
            }
            #endregion
        }

        public void HandleGUI()
        {
            if (ikTargetSelect == null || ikTargetSelect.Length <= 0) return;
            if (IKActiveTarget < 0) return;
            var activeData = ikData[IKActiveTarget];
            if (!activeData.IsValid) return;

            var worldPosition = activeData.WorldPosition;
            var worldRotation = activeData.WorldRotation;

            {
                if ((activeData.solverType == SolverType.CcdIK && activeData.resetRotations) ||
                    activeData.solverType == SolverType.LimbIK)
                {
                    #region IKSwivel
                    var posA = VAW.VA.Skeleton.Bones[activeData.solver.BoneIndexes[^1]].transform.position;
                    var posB = worldPosition;
                    var axis = posB - posA;
                    axis.Normalize();
                    if (axis.sqrMagnitude > 0f)
                    {
                        var posP = Vector3.Lerp(posA, posB, 0.5f);
                        if (activeData.solver.BoneIndexes.Length > 2)
                        {
                            Handles.color = new Color(Handles.centerColor.r, Handles.centerColor.g, Handles.centerColor.b, Handles.centerColor.a * 0.5f);
                            Handles.DrawWireDisc(posP, axis, HandleUtility.GetHandleSize(posP));
                            Handles.color = Handles.centerColor;
                            Vector3 vecPS;
                            {
                                var posC = VAW.VA.Skeleton.Bones[activeData.solver.BoneIndexes[^2]].transform.position;
                                var u = Vector3.Dot((posC - posA), axis) / axis.sqrMagnitude;
                                var resultP = posA + axis * u;
                                vecPS = Quaternion.AngleAxis(activeData.swivel, axis) * (posC - resultP).normalized;
                            }
                            Handles.DrawLine(posP, posP + vecPS * HandleUtility.GetHandleSize(posP));
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
                                foreach (var ikTarget in ikTargetSelect)
                                {
                                    int target = (int)ikTarget;
                                    ikData[target].swivel = Mathf.Repeat(ikData[target].swivel - rotDist + 180f, 360f) - 180f;
                                    VAW.VA.SetUpdateIKtargetOriginalIK(target);
                                }
                            }
                        }
                    }
                    #endregion

                    #region Diection
                    if (activeData.solverType == SolverType.LimbIK && activeData.solver.IsValid)
                    {
                        var solverLimb = activeData.solver as SolverLimb;
                        var vUpper = (solverLimb.Upper.position - solverLimb.Lower.position).normalized;
                        var vTip = (solverLimb.Tip.position - solverLimb.Lower.position).normalized;
                        float angle;
                        {
                            var rot = Quaternion.FromToRotation(vUpper, vTip);
                            rot.ToAngleAxis(out angle, out _);
                            if (angle > 180f) angle = 360f - angle;
                        }
                        var length = Mathf.Min(Vector3.Distance(solverLimb.Lower.position, solverLimb.Upper.position), Vector3.Distance(solverLimb.Tip.position, solverLimb.Lower.position)) / 4f;
                        Handles.color = VAW.EditorSettings.SettingIKTargetActiveColor;
                        Handles.DrawSolidArc(solverLimb.Lower.position, solverLimb.GetBasicDir(), vTip, angle, length);
                    }
                    #endregion
                }
                if (!activeData.autoRotation && VAW.VA.LastTool != Tool.Move)
                {
                    #region Rotate
                    EditorGUI.BeginChangeCheck();
                    var rotation = Handles.RotationHandle(Tools.pivotRotation == PivotRotation.Local ? worldRotation : Tools.handleRotation, worldPosition);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Rotate IK Target");
                        if (Tools.pivotRotation == PivotRotation.Local)
                        {
                            var move = Quaternion.Inverse(worldRotation) * rotation;
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[target].WorldRotation = ikData[target].WorldRotation * move;
                                {   //Handles.ConeCap -> Quaternion To Matrix conversion failed because input Quaternion is invalid
                                    ikData[target].WorldRotation.ToAngleAxis(out float angle, out Vector3 axis);
                                    ikData[target].WorldRotation = Quaternion.AngleAxis(angle, axis);
                                }
                                VAW.VA.SetUpdateIKtargetOriginalIK(target);
                            }
                        }
                        else
                        {
                            (Quaternion.Inverse(Tools.handleRotation) * rotation).ToAngleAxis(out float angle, out Vector3 axis);
                            var move = Quaternion.Inverse(worldRotation) * Quaternion.AngleAxis(angle, Tools.handleRotation * axis) * worldRotation;
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[target].WorldRotation = ikData[target].WorldRotation * move;
                                {   //Handles.ConeCap -> Quaternion To Matrix conversion failed because input Quaternion is invalid
                                    ikData[target].WorldRotation.ToAngleAxis(out angle, out axis);
                                    ikData[target].WorldRotation = Quaternion.AngleAxis(angle, axis);
                                }
                                VAW.VA.SetUpdateIKtargetOriginalIK(target);
                                Tools.handleRotation = rotation;
                            }
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
                            ikData[target].WorldPosition = ikData[target].WorldPosition + move;
                            VAW.VA.SetUpdateIKtargetOriginalIK(target);
                        }
                    }
                    #endregion
                }
            }
        }
        public void TargetGUI()
        {
            var e = Event.current;

            for (int target = 0; target < ikData.Count; target++)
            {
                if (!ikData[target].IsValid) continue;

                var worldPosition = ikData[target].WorldPosition;
                var worldRotation = ikData[target].WorldRotation;

                if (ikTargetSelect != null &&
                    EditorCommon.ArrayContains(ikTargetSelect, target))
                {
                    #region Active
                    {
                        if (target == IKActiveTarget)
                        {
                            Handles.color = Color.white;
                            var boneIndex = ikData[target].solver.BoneIndexes[^1];
                            var worldPosition2 = VAW.VA.Skeleton.Bones[boneIndex].transform.position;
                            Handles.DrawLine(worldPosition, worldPosition2);
                        }
                        Handles.color = VAW.EditorSettings.SettingIKTargetActiveColor;
                        if (ikData[target].solverType == SolverType.LookAt)
                            Handles.SphereHandleCap(0, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, EventType.Repaint);
                        else
                            Handles.CubeHandleCap(0, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, EventType.Repaint);
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
                        if (ikData[target].solverType == SolverType.LookAt)
                            Handles.SphereHandleCap(id, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, eventType);
                        else
                            Handles.CubeHandleCap(id, worldPosition, worldRotation, HandleUtility.GetHandleSize(worldPosition) * VAW.EditorSettings.SettingIKTargetSize, eventType);
                    });
                    if (GUIUtility.hotControl == freeMoveHandleControlID)
                    {
                        if (e.type == EventType.Layout)
                        {
                            GUIUtility.hotControl = -1;
                            {
                                var ikTarget = target;
                                EditorApplication.delayCall += () =>
                                {
                                    VAW.VA.SelectOriginalIKTargetPlusKey(ikTarget);
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
            if (IKActiveTarget < 0) return;
            var activeData = ikData[IKActiveTarget];
            if (!activeData.IsValid) return;
            #region IK
            {
                EditorGUILayout.BeginHorizontal();
                #region Mirror
                {
                    var mirrorTarget = GetMirrorTarget(IKActiveTarget);
                    if (GUILayout.Button(Language.GetContentFormat(Language.Help.SelectionMirror, (mirrorTarget >= 0 ? string.Format("From 'IK: {0}'", ikData[mirrorTarget].name) : "From self"))))
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
                        VAW.VA.SetUpdateIKtargetOriginalIK(target);
                    }
                }
                #endregion
                EditorGUILayout.Space();
                #region Sync
                EditorGUI.BeginDisabledGroup(activeData.spaceType == OriginalIKData.SpaceType.Parent);
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

                        menu.AddItem(new GUIContent("Set Default/Scene Object (Result)"), activeData.defaultSyncType == OriginalIKData.SyncType.SceneObject, () =>
                        {
                            Undo.RecordObject(VAW, "Sync Set Default");
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[(int)target].defaultSyncType = OriginalIKData.SyncType.SceneObject;
                                SynchroSet(target);
                            }
                            SceneView.RepaintAll();
                        });
                        menu.AddItem(new GUIContent("Set Default/Skeleton (Animation Clip)"), activeData.defaultSyncType == OriginalIKData.SyncType.Skeleton, () =>
                        {
                            Undo.RecordObject(VAW, "Sync Set Default");
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[(int)target].defaultSyncType = OriginalIKData.SyncType.Skeleton;
                                SynchroSet(target);
                            }
                            SceneView.RepaintAll();
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUI.EndDisabledGroup();
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
                            VAW.VA.SetUpdateIKtargetOriginalIK(target);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            #region SpaceType
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Space", GUILayout.Width(64));
                EditorGUI.BeginChangeCheck();
                var spaceType = (OriginalIKData.SpaceType)GUILayout.Toolbar((int)activeData.spaceType, IKSpaceTypeStrings, EditorStyles.miniButton);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(VAW, "Change IK Position");
                    foreach (var target in ikTargetSelect)
                    {
                        ChangeSpaceType(target, spaceType);
                    }
                    VAC.Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            #region Parent
            if (activeData.spaceType == OriginalIKData.SpaceType.Local || activeData.spaceType == OriginalIKData.SpaceType.Parent)
            {
                EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                EditorGUILayout.LabelField("Parent", GUILayout.Width(64));
                EditorGUI.BeginChangeCheck();
                if (activeData.spaceType == OriginalIKData.SpaceType.Local)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    var parent = activeData.parentBoneIndex >= 0 ? VAW.VA.Bones[activeData.parentBoneIndex] : null;
                    EditorGUILayout.ObjectField(parent, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();
                }
                else if (activeData.spaceType == OriginalIKData.SpaceType.Parent)
                {
                    var parent = EditorGUILayout.ObjectField(activeData.parent, typeof(GameObject), true) as GameObject;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Position");
                        foreach (var target in ikTargetSelect)
                        {
                            var data = ikData[target];
                            var worldPosition = data.WorldPosition;
                            var worldRotation = data.WorldRotation;
                            data.parent = parent;
                            data.WorldPosition = worldPosition;
                            data.WorldRotation = worldRotation;
                            VAW.VA.SetSynchroIKtargetOriginalIK(target);
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
                        ikData[target].position += move;
                        VAW.VA.SetUpdateIKtargetOriginalIK(target);
                    }
                }
                if (activeData.spaceType == OriginalIKData.SpaceType.Parent)
                {
                    if (GUILayout.Button("Reset", GUILayout.Width(44)))
                    {
                        Undo.RecordObject(VAW, "Change IK Position");
                        foreach (var target in ikTargetSelect)
                        {
                            ikData[target].position = Vector3.zero;
                            VAW.VA.SetUpdateIKtargetOriginalIK(target);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
            if (activeData.solverType == SolverType.CcdIK || activeData.solverType == SolverType.LimbIK)
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
                            ikData[target].rotation.eulerAngles += move;
                            VAW.VA.SetUpdateIKtargetOriginalIK(target);
                        }
                    }
                    if (activeData.spaceType == OriginalIKData.SpaceType.Parent)
                    {
                        if (GUILayout.Button("Reset", GUILayout.Width(44)))
                        {
                            Undo.RecordObject(VAW, "Change IK Rotation");
                            foreach (var target in ikTargetSelect)
                            {
                                ikData[target].rotation = Quaternion.identity;
                                VAW.VA.SetUpdateIKtargetOriginalIK(target);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            if ((activeData.solverType == SolverType.CcdIK && activeData.resetRotations) ||
                activeData.solverType == SolverType.LimbIK)
            {
                #region Swivel
                {
                    EditorGUILayout.BeginHorizontal(RowCount++ % 2 == 0 ? VAW.GuiStyleAnimationRowEvenStyle : VAW.GuiStyleAnimationRowOddStyle);
                    EditorGUILayout.LabelField("Swivel", GUILayout.Width(64));
                    EditorGUI.BeginChangeCheck();
                    var swivel = EditorGUILayout.Slider(activeData.swivel, -180f, 180f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(VAW, "Change IK Swivel");
                        var move = swivel - activeData.swivel;
                        foreach (var target in ikTargetSelect)
                        {
                            ikData[target].swivel = Mathf.Repeat(ikData[target].swivel + move + 180f, 360f) - 180f;
                            VAW.VA.SetUpdateIKtargetOriginalIK(target);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                #endregion
            }
            #endregion
        }
        public void ControlGUI()
        {
            var saveColor = GUI.backgroundColor;

            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            if (ikReorderableList != null)
            {
                ikReorderableList.DoLayoutList();
                if (advancedFoldout && ikReorderableList.index >= 0 && ikReorderableList.index < ikData.Count)
                {
                    advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced", true);
                    EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
                    var target = ikReorderableList.index;
                    {
                        EditorGUI.BeginChangeCheck();
                        var name = EditorGUILayout.TextField("Name", ikData[target].name);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(VAW, "Change Original IK Data");
                            ikData[target].name = name;
                        }
                    }
                    {
                        {
                            EditorGUI.BeginChangeCheck();
                            var type = (SolverType)EditorGUILayout.Popup(new GUIContent("Type"), (int)ikData[target].solverType, SolverTypeStrings);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(VAW, "Change Original IK Data");
                                ChangeSolverType(target, type);
                            }
                        }
                        EditorGUI.indentLevel++;
                        {
                            if (ikData[target].solverType == SolverType.CcdIK || ikData[target].solverType == SolverType.LookAt)
                            {
                                #region CcdIK || LookAt
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var resetRotations = EditorGUILayout.Toggle(Language.GetContent(Language.Help.OriginalIKResetRotations), ikData[target].resetRotations);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Original IK Data");
                                        ikData[target].resetRotations = resetRotations;
                                        VAW.VA.SetUpdateIKtargetOriginalIK(target);
                                    }
                                }
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var level = EditorGUILayout.Popup(new GUIContent("Level"), ikData[target].level - SolverLevelMin, SolverLevelStrings) + SolverLevelMin;
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Original IK Data");
                                        ChangeTypeSetting(target, level - ikData[target].level);
                                    }
                                }
                                #endregion
                            }
                            else if (ikData[target].solverType == SolverType.LimbIK)
                            {
                                #region LimbIK
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var limbDirection = EditorGUILayout.Slider(Language.GetContent(Language.Help.OriginalIKDirection), ikData[target].limbDirection, -180f, 180f);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(VAW, "Change Original IK Data");
                                        ChangeTypeSetting(target, limbDirection - ikData[target].limbDirection);
                                    }
                                }
                                #endregion
                            }
                            EditorGUI.indentLevel++;
                            if (ikData[target].joints != null)
                            {
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField("Target", ikData[target].joints[0].bone, typeof(GameObject), true);
                                    EditorGUI.EndDisabledGroup();
                                }
                                bool haveError = false;
                                if (ikData[target].solverType == SolverType.CcdIK)
                                {
                                    #region CcdIK
                                    for (int i = 1; i < ikData[target].level; i++)
                                    {
                                        var isError = IsErrorJoint(target, i);
                                        if (isError) { GUI.backgroundColor = Color.red; haveError = true; }
                                        else GUI.backgroundColor = saveColor;

                                        var data = ikData[target];

                                        EditorGUILayout.BeginHorizontal();
                                        {
                                            {
                                                EditorGUI.BeginChangeCheck();
                                                var foldout = EditorGUILayout.Foldout(data.joints[i].foldout, i.ToString(), true);
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    Undo.RecordObject(VAW, "Change Original IK Data");
                                                    var joint = data.joints[i];
                                                    joint.foldout = foldout;
                                                    data.joints[i] = joint;
                                                }
                                            }
                                            {
                                                EditorGUI.BeginChangeCheck();
                                                var bone = EditorGUILayout.ObjectField(data.joints[i].bone, typeof(GameObject), true) as GameObject;
                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    Undo.RecordObject(VAW, "Change Original IK Data");
                                                    var joint = data.joints[i];
                                                    joint.bone = bone;
                                                    data.joints[i] = joint;
                                                    UpdateSolver(target);
                                                }
                                            }
                                        }
                                        EditorGUILayout.EndHorizontal();

                                        if (data.joints[i].foldout)
                                        {
                                            EditorGUI.indentLevel++;
                                            {
                                                if (ikData[target].solverType == SolverType.CcdIK || ikData[target].solverType == SolverType.LookAt)
                                                {
                                                    EditorGUI.BeginChangeCheck();
                                                    var weight = EditorGUILayout.Slider("Weight", data.joints[i].weight, 0f, 1f);
                                                    if (EditorGUI.EndChangeCheck())
                                                    {
                                                        Undo.RecordObject(VAW, "Change Original IK Data");
                                                        var joint = data.joints[i];
                                                        joint.weight = weight;
                                                        data.joints[i] = joint;
                                                        UpdateSolver(target);
                                                        VAW.VA.SetUpdateIKtargetOriginalIK(target);
                                                    }
                                                }
                                            }
                                            EditorGUI.indentLevel--;
                                        }

                                        GUI.backgroundColor = saveColor;
                                    }
                                    #endregion
                                }
                                else if (ikData[target].solverType == SolverType.LimbIK)
                                {
                                    #region LimbIK
                                    for (int i = 1; i < ikData[target].level; i++)
                                    {
                                        var isError = IsErrorJoint(target, i);
                                        if (isError) { GUI.backgroundColor = Color.red; haveError = true; }
                                        else GUI.backgroundColor = saveColor;

                                        var data = ikData[target];

                                        {
                                            EditorGUI.BeginChangeCheck();
                                            var bone = EditorGUILayout.ObjectField(i == 1 ? "Lower" : "Upper", data.joints[i].bone, typeof(GameObject), true) as GameObject;
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                Undo.RecordObject(VAW, "Change Original IK Data");
                                                var joint = data.joints[i];
                                                joint.bone = bone;
                                                data.joints[i] = joint;
                                                UpdateSolver(target);
                                            }
                                        }

                                        GUI.backgroundColor = saveColor;
                                    }
                                    #endregion
                                }
                                if (haveError)
                                {
                                    EditorGUILayout.HelpBox(Language.GetText(Language.Help.OriginalIKPleasespecifyGameObject), MessageType.Error);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(Language.GetContent(Language.Help.OriginalIKChangeAll)))
                    {
                        Undo.RecordObject(VAW, "Change Original IK Data");
                        bool flag = !ikData.Any(v => !v.enable);
                        for (int target = 0; target < ikData.Count; target++)
                        {
                            ikData[target].enable = !flag;
                            if (ikData[target].enable)
                            {
                                UpdateSolver(target);
                                SynchroSet(target);
                            }
                        }
                        VAW.VA.SetAnimationWindowSynchroSelection();
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button(Language.GetContent(Language.Help.OriginalIKSelectAll)))
                    {
                        var list = new List<int>();
                        for (int target = 0; target < ikData.Count; target++)
                        {
                            if (ikData[target].enable)
                                list.Add(target);
                        }
                        VAW.VA.SelectIKTargets(null, list.ToArray());
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = saveColor;
        }

        private void UndoRedoPerformed()
        {
            if (ikData == null) return;

            for (int target = 0; target < ikData.Count; target++)
            {
                UpdateSolver(target);
            }
        }

        private void Reset(int target)
        {
            var data = ikData[target];
            switch (data.solverType)
            {
                case SolverType.LookAt:
                    {
                        var t = VAW.VA.Skeleton.GameObject.transform;
                        Vector3 vec = data.WorldPosition - t.position;
                        var normal = t.rotation * Vector3.right;
                        var dot = Vector3.Dot(vec, normal);
                        data.WorldPosition -= normal * dot;
                    }
                    break;
                default:
                    if (VAW.VA.Skeleton.BoneDictionary.TryGetValue(data.Tip, out int boneIndex) &&
                        boneIndex >= 0)
                    {
                        data.WorldRotation = data.Tip.transform.parent.rotation * VAW.VA.BoneSaveTransforms[boneIndex].localRotation;
                    }
                    break;
            }
            VAW.VA.SetUpdateIKtargetOriginalIK(target);
        }

        private void ChangeSpaceType(int target, OriginalIKData.SpaceType spaceType)
        {
            if (target < 0 || target >= ikData.Count) return;
            var data = ikData[target];
            if (data.spaceType == spaceType) return;
            var position = data.WorldPosition;
            var rotation = data.WorldRotation;
            data.spaceType = spaceType;
            data.WorldPosition = position;
            data.WorldRotation = rotation;

            SetSynchroIKtargetOriginalIK(target);
        }

        public int IsIKBone(HumanBodyBones humanoidIndex)
        {
            var boneIndex = VAW.VA.HumanoidIndex2boneIndex[(int)humanoidIndex];
            if (boneIndex < 0) return -1;
            return IsIKBone(boneIndex);
        }
        public int IsIKBone(int boneIndex)
        {
            for (int i = 0; i < ikData.Count; i++)
            {
                if (!ikData[i].enable || ikData[i].joints == null) continue;
                for (int j = 0; j < ikData[i].level; j++)
                {
                    if (ikData[i].joints[j].bone == VAW.VA.Bones[boneIndex])
                        return i;
                }
            }
            return -1;
        }

        public void ChangeTargetIK(int target)
        {
            Undo.RecordObject(VAW, "Change IK");
            if (ikData[target].enable)
            {
                var selectGameObjects = new List<GameObject>();
                ikData[target].enable = false;
                if (ikData[target].Tip != null)
                    selectGameObjects.Add(ikData[target].Tip);
                VAW.VA.SelectGameObjects(selectGameObjects.ToArray());
            }
            else
            {
                ikData[target].enable = true;
                UpdateSolver(target);
                SynchroSet(target);
                VAW.VA.SelectOriginalIKTargetPlusKey(target);
            }
        }
        public bool ChangeSelectionIK()
        {
            Undo.RecordObject(VAW, "Change IK");
            if (ikTargetSelect != null && ikTargetSelect.Length > 0)
            {
                var selectGameObjects = new List<GameObject>();
                foreach (var target in ikTargetSelect)
                {
                    if (target < 0 || target >= ikData.Count) continue;
                    ikData[target].enable = false;
                    if (VAW.VA.Skeleton.BoneDictionary.TryGetValue(ikData[target].Tip, out int boneIndex) &&
                        boneIndex >= 0)
                    {
                        selectGameObjects.Add(VAW.VA.Bones[boneIndex]);
                    }
                }
                if (selectGameObjects.Count > 0)
                {
                    VAW.VA.SelectGameObjects(selectGameObjects.ToArray());
                    return true;
                }
            }
            else
            {
                var selectIkTargets = new HashSet<int>();
                foreach (var boneIndex in VAW.VA.SelectionBones)
                {
                    var target = ikData.FindIndex((data) =>
                    {
                        if (data.joints == null) return false;
                        return data.joints.FindIndex((joint) => joint.bone == VAW.VA.Bones[boneIndex]) >= 0;
                    });
                    if (target < 0)
                    {
                        target = CreateIKData(VAW.VA.Bones[boneIndex]);
                        if (target >= 0)
                        {
                            selectIkTargets.Add(target);
                        }
                    }
                    else
                    {
                        selectIkTargets.Add(target);
                    }
                }
                if (selectIkTargets.Count > 0)
                {
                    foreach (var target in selectIkTargets)
                    {
                        ikData[target].enable = true;
                        UpdateSolver(target);
                        SynchroSet(target);
                    }
                    VAW.VA.SelectIKTargets(null, selectIkTargets.ToArray());
                    return true;
                }
            }
            return false;
        }

        public void SetUpdateIKtargetBone(int boneIndex)
        {
            if (boneIndex < 0 || ikData == null)
                return;
            for (int target = 0; target < ikData.Count; target++)
            {
                var data = ikData[target];
                if (!data.enable || data.updateIKtarget)
                    continue;
                for (int i = 0; i < data.level; i++)
                {
                    if (data.joints[i].bone == null)
                        continue;
                    var t = data.joints[i].bone.transform;
                    while (t != null && VAW.GameObject.transform.parent != t)
                    {
                        if (VAW.VA.Bones[boneIndex] == t.gameObject)
                        {
                            SetUpdateIKtargetOriginalIK(target);
                            break;
                        }
                        t = t.parent;
                    }
                }
            }
            SetUpdateLinkedIKTarget(boneIndex);
        }
        public void SetUpdateIKtargetOriginalIK(int target, bool force = false)
        {
            if (ikData == null || target < 0)
                return;
            if (target >= ikData.Count)
            {
                SetUpdateIKtargetAll();
            }
            else if (ikData[target].enable)
            {
                if (force || ikData[target].spaceType != OriginalIKData.SpaceType.Local)
                {
                    ikData[target].updateIKtarget = true;

                    SetUpdateLinkedIKTarget(ikData[(int)target].rootBoneIndex);
                }
                else
                {
                    SetSynchroIKtargetOriginalIK(target);
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
                if (data.spaceType == OriginalIKData.SpaceType.Parent &&
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
            for (var i = 0; i < ikData.Count; i++)
            {
                if (!ikData[i].enable)
                    continue;
                SetUpdateIKtargetOriginalIK(i);
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
            if (boneIndex < 0 || ikData == null) return;
            for (int target = 0; target < ikData.Count; target++)
            {
                var data = ikData[target];
                if (!data.enable || data.synchroIKtarget) continue;
                for (int i = 0; i < data.level; i++)
                {
                    if (data.joints[i].bone == null) continue;
                    var t = data.joints[i].bone.transform;
                    while (t != null && VAW.GameObject.transform.parent != t)
                    {
                        if (VAW.VA.Bones[boneIndex] == t.gameObject)
                        {
                            SetSynchroIKtargetOriginalIK(target);
                            break;
                        }
                        t = t.parent;
                    }
                }
            }
        }
        public void SetSynchroIKtargetOriginalIK(int target)
        {
            if (ikData == null || target < 0)
                return;
            if (target == ikData.Count)
            {
                SetSynchroIKtargetAll();
                return;
            }
            if (!ikData[target].updateIKtarget)
                ikData[target].synchroIKtarget = true;
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
                if (data.IsValid && data.synchroIKtarget)
                    return true;
            }
            return false;
        }

        public List<int> SelectionOriginalIKTargetsBoneIndexes()
        {
            var list = new List<int>();
            if (ikTargetSelect != null && ikData != null)
            {
                foreach (var ikTarget in ikTargetSelect)
                {
                    for (int i = 0; i < ikData[ikTarget].level; i++)
                    {
                        var boneIndex = VAW.VA.BonesIndexOf(ikData[ikTarget].joints[i].bone);
                        if (boneIndex < 0) continue;
                        list.Add(boneIndex);
                    }
                }
            }
            return list;
        }
    }
}
