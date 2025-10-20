#if VERYANIMATION_ANIMATIONRIGGING
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace VeryAnimation
{
    internal class AnimationRigging
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        public const string AnimationRiggingRigName = "VARig";

        public RigBuilder RigBuilder { get; private set; }

        public RigLayer RigLayer { get; private set; }

        public Rig ArRig { get; private set; }

        private VeryAnimationRigBuilder vaRigBuilder;

        public VeryAnimationRig VaRig { get; private set; }

        public bool IsValid { get { return vaRigBuilder != null && VaRig != null && RigBuilder != null && RigLayer != null && ArRig != null; } }
        public bool IsActive { get { return IsValid && RigBuilder.isActiveAndEnabled && RigLayer.active; } }
        public float Weight { get { return IsActive ? ArRig.weight : 0f; } }

        public void Initialize()
        {
            Release();

            RigBuilder = VAW.GameObject.GetComponent<RigBuilder>();
            ArRig = null;
            vaRigBuilder = VAW.GameObject.GetComponent<VeryAnimationRigBuilder>();
            VaRig = GetVeryAnimationRig(VAW.GameObject);
            if (VaRig != null)
                ArRig = VaRig.GetComponent<Rig>();
            if (RigBuilder != null)
                RigLayer = RigBuilder.layers.Find(x => x != null && x.rig == ArRig);
        }
        public void Release()
        {
            RigBuilder = null;
            RigLayer = null;
            ArRig = null;
            vaRigBuilder = null;
            VaRig = null;
        }

        public void Enable()
        {
            Disable();

            VAW.VA.StopRecording();
            {
                Create(VAW.GameObject);

                RigBuilder = VAW.GameObject.GetComponent<RigBuilder>();
                vaRigBuilder = VAW.GameObject.GetComponent<VeryAnimationRigBuilder>();
                VaRig = GetVeryAnimationRig(VAW.GameObject);
                ArRig = VaRig != null ? VaRig.GetComponent<Rig>() : null;
                RigLayer = RigBuilder != null ? RigBuilder.layers.Find(x => x != null && x.rig == ArRig) : null;
            }
            VAW.VA.OnHierarchyWindowChanged();
        }
        public void Disable()
        {
            VAW.VA.StopRecording();
            {
                Delete(VAW.GameObject);
            }
            VAW.VA.OnHierarchyWindowChanged();

            Release();
        }
        public static VeryAnimationRig GetVeryAnimationRig(GameObject gameObject)
        {
            return ArrayUtility.Find(gameObject.GetComponentsInChildren<VeryAnimationRig>(true), x => x.name == AnimationRiggingRigName);
        }
        public static void Create(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<RigBuilder>(out var rigBuilder))
            {
                rigBuilder = Undo.AddComponent<RigBuilder>(gameObject);
            }

            if (!gameObject.TryGetComponent<VeryAnimationRigBuilder>(out var vaRigBuilder))
            {
                vaRigBuilder = Undo.AddComponent<VeryAnimationRigBuilder>(gameObject);
            }

            //Must be in order before RigBuilder
            {
                var components = vaRigBuilder.GetComponents<MonoBehaviour>();
                var indexRigBuilder = ArrayUtility.FindIndex(components, x => x != null && x.GetType() == typeof(RigBuilder));
                var indexVARigBuilder = ArrayUtility.FindIndex(components, x => x != null && x.GetType() == typeof(VeryAnimationRigBuilder));
                if (indexRigBuilder >= 0 && indexVARigBuilder >= 0)
                {
                    for (int i = 0; i < indexVARigBuilder - indexRigBuilder; i++)
                        ComponentUtility.MoveComponentUp(vaRigBuilder);
                }
            }

            var vaRig = GetVeryAnimationRig(gameObject);
            if (vaRig == null)
            {
                var rigObj = new GameObject(AnimationRiggingRigName);
                rigObj.transform.SetParent(gameObject.transform);
#if UNITY_2022_3_OR_NEWER
                rigObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
                rigObj.transform.localPosition = Vector3.zero;
                rigObj.transform.localRotation = Quaternion.identity;
#endif
                rigObj.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(rigObj, "");
                var rig = Undo.AddComponent<Rig>(rigObj);
                vaRig = Undo.AddComponent<VeryAnimationRig>(rigObj);
                Undo.RecordObject(rigBuilder, "");
                var rigLayer = new RigLayer(rig);
                rigBuilder.layers.Add(rigLayer);
                Selection.activeGameObject = rigObj;
            }
        }
        public static void Delete(GameObject gameObject)
        {
            var rigBuilder = gameObject.GetComponent<RigBuilder>();
            var vaRigBuilder = gameObject.GetComponent<VeryAnimationRigBuilder>();
            var vaRig = GetVeryAnimationRig(gameObject);
            var rig = vaRig != null ? vaRig.GetComponent<Rig>() : null;

            var index = rigBuilder != null && rig != null ? rigBuilder.layers.FindIndex(x => x.rig == rig) : -1;
            if (rig != null)
            {
                Selection.activeGameObject = rig.gameObject;
                Unsupported.DeleteGameObjectSelection();
                if (rig != null)
                    return;
                rig = null;
            }
            if (vaRigBuilder != null)
            {
                Undo.DestroyObjectImmediate(vaRigBuilder);
                vaRigBuilder = null;
            }
            if (rigBuilder != null)
            {
                if (index >= 0 && index < rigBuilder.layers.Count)
                {
                    Undo.RecordObject(rigBuilder, "");
                    rigBuilder.layers.RemoveAt(index);
                    if (rigBuilder.layers.Count == 0)
                    {
                        Undo.DestroyObjectImmediate(rigBuilder);
                    }
                }
                rigBuilder = null;
            }
        }

        public static void ReplaceConstraintTransformReference(GameObject gameObject, Rig rig,
                                                                GameObject originalGameObject, Rig originalRig)
        {
            Transform GetPreviewTransform(Transform t)
            {
                if (t == null) return null;
                var path = AnimationUtility.CalculateTransformPath(t, originalGameObject.transform);
                return gameObject.transform.Find(path);
            }

            var originalRigConstraints = originalGameObject.GetComponentsInChildren<IRigConstraint>();
            foreach (var originalRigConstraint in originalRigConstraints)
            {
                #region BlendConstraint
                if (originalRigConstraint is BlendConstraint)
                {
                    var blendConstraint = originalRigConstraint as BlendConstraint;
                    if (GetPreviewTransform(blendConstraint.transform).TryGetComponent<BlendConstraint>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(blendConstraint.data.constrainedObject);
                        constraint.data.sourceObjectA = GetPreviewTransform(blendConstraint.data.sourceObjectA);
                        constraint.data.sourceObjectB = GetPreviewTransform(blendConstraint.data.sourceObjectB);
                    }
                }
                #endregion
                #region ChainIKConstraint
                else if (originalRigConstraint is ChainIKConstraint)
                {
                    var chainIKConstraint = originalRigConstraint as ChainIKConstraint;
                    if (GetPreviewTransform(chainIKConstraint.transform).TryGetComponent<ChainIKConstraint>(out var constraint))
                    {
                        constraint.data.root = GetPreviewTransform(chainIKConstraint.data.root);
                        constraint.data.tip = GetPreviewTransform(chainIKConstraint.data.tip);
                        constraint.data.target = GetPreviewTransform(chainIKConstraint.data.target);
                    }
                }
                #endregion
                #region DampedTransform
                else if (originalRigConstraint is DampedTransform)
                {
                    var dampedTransform = originalRigConstraint as DampedTransform;
                    if (GetPreviewTransform(dampedTransform.transform).TryGetComponent<DampedTransform>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(dampedTransform.data.constrainedObject);
                        constraint.data.sourceObject = GetPreviewTransform(dampedTransform.data.sourceObject);
                    }
                }
                #endregion
                #region MultiAimConstraint
                else if (originalRigConstraint is MultiAimConstraint)
                {
                    var multiAimConstraint = originalRigConstraint as MultiAimConstraint;
                    if (GetPreviewTransform(multiAimConstraint.transform).TryGetComponent<MultiAimConstraint>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(multiAimConstraint.data.constrainedObject);
                        var sourceObjects = constraint.data.sourceObjects;
                        for (int i = 0; i < multiAimConstraint.data.sourceObjects.Count; i++)
                            sourceObjects.SetTransform(i, GetPreviewTransform(multiAimConstraint.data.sourceObjects.GetTransform(i)));
                        constraint.data.sourceObjects = sourceObjects;
                    }
                }
                #endregion
                #region MultiParentConstraint
                else if (originalRigConstraint is MultiParentConstraint)
                {
                    var multiParentConstraint = originalRigConstraint as MultiParentConstraint;
                    if (GetPreviewTransform(multiParentConstraint.transform).TryGetComponent<MultiParentConstraint>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(multiParentConstraint.data.constrainedObject);
                        var sourceObjects = constraint.data.sourceObjects;
                        for (int i = 0; i < multiParentConstraint.data.sourceObjects.Count; i++)
                            sourceObjects.SetTransform(i, GetPreviewTransform(multiParentConstraint.data.sourceObjects.GetTransform(i)));
                        constraint.data.sourceObjects = sourceObjects;
                    }
                }
                #endregion
                #region MultiPositionConstraint
                else if (originalRigConstraint is MultiPositionConstraint)
                {
                    var multiPositionConstraint = originalRigConstraint as MultiPositionConstraint;
                    if (GetPreviewTransform(multiPositionConstraint.transform).TryGetComponent<MultiPositionConstraint>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(multiPositionConstraint.data.constrainedObject);
                        var sourceObjects = constraint.data.sourceObjects;
                        for (int i = 0; i < multiPositionConstraint.data.sourceObjects.Count; i++)
                            sourceObjects.SetTransform(i, GetPreviewTransform(multiPositionConstraint.data.sourceObjects.GetTransform(i)));
                        constraint.data.sourceObjects = sourceObjects;
                    }
                }
                #endregion
                #region MultiReferentialConstraint
                else if (originalRigConstraint is MultiReferentialConstraint)
                {
                    var multiReferentialConstraint = originalRigConstraint as MultiReferentialConstraint;
                    if (GetPreviewTransform(multiReferentialConstraint.transform).TryGetComponent<MultiReferentialConstraint>(out var constraint))
                    {
                        var sourceObjects = constraint.data.sourceObjects;
                        for (int i = 0; i < multiReferentialConstraint.data.sourceObjects.Count; i++)
                            sourceObjects[i] = GetPreviewTransform(multiReferentialConstraint.data.sourceObjects[i]);
                        constraint.data.sourceObjects = sourceObjects;
                    }
                }
                #endregion
                #region MultiRotationConstraint
                else if (originalRigConstraint is MultiRotationConstraint)
                {
                    var multiRotationConstraint = originalRigConstraint as MultiRotationConstraint;
                    if (GetPreviewTransform(multiRotationConstraint.transform).TryGetComponent<MultiRotationConstraint>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(multiRotationConstraint.data.constrainedObject);
                        var sourceObjects = constraint.data.sourceObjects;
                        for (int i = 0; i < multiRotationConstraint.data.sourceObjects.Count; i++)
                            sourceObjects.SetTransform(i, GetPreviewTransform(multiRotationConstraint.data.sourceObjects.GetTransform(i)));
                        constraint.data.sourceObjects = sourceObjects;
                    }
                }
                #endregion
                #region OverrideTransform
                else if (originalRigConstraint is OverrideTransform)
                {
                    var overrideTransform = originalRigConstraint as OverrideTransform;
                    if (GetPreviewTransform(overrideTransform.transform).TryGetComponent<OverrideTransform>(out var constraint))
                    {
                        constraint.data.constrainedObject = GetPreviewTransform(overrideTransform.data.constrainedObject);
                        constraint.data.sourceObject = GetPreviewTransform(overrideTransform.data.sourceObject);
                    }
                }
                #endregion
                #region TwistChainConstraint
                else if (originalRigConstraint is TwistChainConstraint)
                {
                    var twistChainConstraint = originalRigConstraint as TwistChainConstraint;
                    if (GetPreviewTransform(twistChainConstraint.transform).TryGetComponent<TwistChainConstraint>(out var constraint))
                    {
                        constraint.data.root = GetPreviewTransform(twistChainConstraint.data.root);
                        constraint.data.tip = GetPreviewTransform(twistChainConstraint.data.tip);
                        constraint.data.rootTarget = GetPreviewTransform(twistChainConstraint.data.rootTarget);
                        constraint.data.tipTarget = GetPreviewTransform(twistChainConstraint.data.tipTarget);
                    }
                }
                #endregion
                #region TwistCorrection
                else if (originalRigConstraint is TwistCorrection)
                {
                    var twistCorrection = originalRigConstraint as TwistCorrection;
                    if (GetPreviewTransform(twistCorrection.transform).TryGetComponent<TwistCorrection>(out var constraint))
                    {
                        constraint.data.sourceObject = GetPreviewTransform(twistCorrection.data.sourceObject);
                        var twistNodes = constraint.data.twistNodes;
                        for (int i = 0; i < twistCorrection.data.twistNodes.Count; i++)
                            twistNodes.SetTransform(i, GetPreviewTransform(twistCorrection.data.twistNodes.GetTransform(i)));
                        constraint.data.twistNodes = twistNodes;
                    }
                }
                #endregion
                #region TwoBoneIKConstraint
                else if (originalRigConstraint is TwoBoneIKConstraint)
                {
                    var twoBoneIKConstraint = originalRigConstraint as TwoBoneIKConstraint;
                    if (GetPreviewTransform(twoBoneIKConstraint.transform).TryGetComponent<TwoBoneIKConstraint>(out var constraint))
                    {
                        constraint.data.root = GetPreviewTransform(twoBoneIKConstraint.data.root);
                        constraint.data.mid = GetPreviewTransform(twoBoneIKConstraint.data.mid);
                        constraint.data.tip = GetPreviewTransform(twoBoneIKConstraint.data.tip);
                        constraint.data.target = GetPreviewTransform(twoBoneIKConstraint.data.target);
                        constraint.data.hint = GetPreviewTransform(twoBoneIKConstraint.data.hint);
                    }
                }
                #endregion
                else
                {
                    Debug.LogErrorFormat("<color=blue>[Very Animation]</color>Unknown IRigConstraint. {0}", originalRigConstraint);
                }
            }

            #region VeryAnimationRig
            {
                var vaRig = rig.GetComponent<VeryAnimationRig>();
                var originalVaRig = originalRig.GetComponent<VeryAnimationRig>();
                if (vaRig != null && originalVaRig != null)
                {
                    vaRig.basePoseLeftHand.constraint = GetPreviewTransform(originalVaRig.basePoseLeftHand.constraint);
                    vaRig.basePoseRightHand.constraint = GetPreviewTransform(originalVaRig.basePoseRightHand.constraint);
                    vaRig.basePoseLeftFoot.constraint = GetPreviewTransform(originalVaRig.basePoseLeftFoot.constraint);
                    vaRig.basePoseRightFoot.constraint = GetPreviewTransform(originalVaRig.basePoseRightFoot.constraint);
                }
            }
            #endregion
        }
    }
}
#endif
