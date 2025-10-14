using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

#if VERYANIMATION_ANIMATIONRIGGING
using UnityEngine.Animations.Rigging;

namespace VeryAnimation
{
    [Serializable]
    internal class VeryAnimationRangePinningWindow : EditorWindow
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        private UAnimationClipEditor uAnimationClipEditor;

        public int rangeFirstFrame;
        public int rangeLastFrame;

        public bool useEndFrame;

        public bool enableTransitionDuration;
        public int transitionDurationFrame;

        public bool weight = true;
        public bool[] targetPosition = new bool[3];
        public bool[] targetRotation = new bool[3];
        public bool[] hintPosition = new bool[3];

        private bool enableTargetRotation;
        private bool enableHintPosition;

        private void OnEnable()
        {
            titleContent = new GUIContent("Range Pinning");
            minSize = new Vector2(512, 240);
            position = new Rect(position.position, minSize);

            var endTime = EditorPrefs.GetFloat("VeryAnimation_RangePinning_Time", 0.1f);
            useEndFrame = EditorPrefs.GetBool("VeryAnimation_RangePinning_UseEndFrame", false);
            enableTransitionDuration = EditorPrefs.GetBool("VeryAnimation_RangePinning_EnableTransitionDuration", true);
            transitionDurationFrame = VAW.VA.GetTimeFrame(EditorPrefs.GetFloat("VeryAnimation_RangePinning_TransitionDurationTime", 0.05f));
            weight = EditorPrefs.GetBool("VeryAnimation_RangePinning_Weight", true);
            targetPosition[0] = EditorPrefs.GetBool("VeryAnimation_RangePinning_TargetPositionX", true);
            targetPosition[1] = EditorPrefs.GetBool("VeryAnimation_RangePinning_TargetPositionY", true);
            targetPosition[2] = EditorPrefs.GetBool("VeryAnimation_RangePinning_TargetPositionZ", true);
            targetRotation[0] = EditorPrefs.GetBool("VeryAnimation_RangePinning_TargetRotationX", true);
            targetRotation[1] = EditorPrefs.GetBool("VeryAnimation_RangePinning_TargetRotationY", true);
            targetRotation[2] = EditorPrefs.GetBool("VeryAnimation_RangePinning_TargetRotationZ", true);
            hintPosition[0] = EditorPrefs.GetBool("VeryAnimation_RangePinning_HintPositionX", true);
            hintPosition[1] = EditorPrefs.GetBool("VeryAnimation_RangePinning_HintPositionY", true);
            hintPosition[2] = EditorPrefs.GetBool("VeryAnimation_RangePinning_HintPositionZ", true);

            uAnimationClipEditor = new UAnimationClipEditor(VAW.VA.CurrentClip, VAW.VA.UAvatarPreview);

            rangeFirstFrame = VAW.VA.GetTimeFrame(VAW.VA.CurrentTime);
            rangeLastFrame = Math.Min(rangeFirstFrame + VAW.VA.GetTimeFrame(endTime), VAW.VA.GetLastFrame());

            if (VAW.VA.SelectionActiveGameObject.TryGetComponent<IRigConstraint>(out var constraint))
            {
                if (constraint is TwoBoneIKConstraint)
                {
                    enableTargetRotation = true;
                    enableHintPosition = true;
                }
            }
        }
        private void OnDisable()
        {
            uAnimationClipEditor.Dispose();
            uAnimationClipEditor = null;

            EditorPrefs.SetFloat("VeryAnimation_RangePinning_Time", VAW.VA.GetFrameTime(Math.Max(rangeLastFrame - rangeFirstFrame, 0)));
            EditorPrefs.SetBool("VeryAnimation_RangePinning_UseEndFrame", useEndFrame);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_EnableTransitionDuration", enableTransitionDuration);
            EditorPrefs.SetFloat("VeryAnimation_RangePinning_TransitionDurationTime", VAW.VA.GetFrameTime(Math.Max(transitionDurationFrame, 0)));
            EditorPrefs.SetBool("VeryAnimation_RangePinning_Weight", weight);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_TargetPositionX", targetPosition[0]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_TargetPositionY", targetPosition[1]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_TargetPositionZ", targetPosition[2]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_TargetRotationX", targetRotation[0]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_TargetRotationY", targetRotation[1]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_TargetRotationZ", targetRotation[2]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_HintPositionX", hintPosition[0]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_HintPositionY", hintPosition[1]);
            EditorPrefs.SetBool("VeryAnimation_RangePinning_HintPositionZ", hintPosition[2]);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(VAW.GuiStyleSkinBox);
            {
                float firstFrame = rangeFirstFrame;
                float lastFrame = rangeLastFrame;
                float additivePoseframe = 0.0f;
#pragma warning disable IDE0059
                uAnimationClipEditor.ClipRangeGUI(ref firstFrame, ref lastFrame, out bool changedStart, out bool changedStop, false, ref additivePoseframe, out bool changedAdditivePoseframe);
#pragma warning restore IDE0059
                if (changedStart)
                {
                    Undo.RecordObject(this, "Change first frame");
                    rangeFirstFrame = Mathf.RoundToInt(firstFrame);
                }
                if (changedStop)
                {
                    Undo.RecordObject(this, "Change last frame");
                    rangeLastFrame = Mathf.RoundToInt(lastFrame);
                }
            }
            const int FlagWidth = 64;
            {
                EditorGUI.BeginChangeCheck();
                var flag = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SelectionRangePinning_UseEndFrame), useEndFrame);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Change Range Pinning");
                    useEndFrame = flag;
                }
            }
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.Toggle(Language.GetContent(Language.Help.SelectionRangePinning_TransitionDuration), enableTransitionDuration);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        enableTransitionDuration = flag;
                    }
                }
                if (enableTransitionDuration)
                {
                    EditorGUILayout.Space();
                    {
                        EditorGUI.BeginChangeCheck();
                        var value = EditorGUILayout.IntField(transitionDurationFrame, GUILayout.Width(FlagWidth));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(this, "Change Range Pinning");
                            transitionDurationFrame = Math.Max(value, 0);
                        }
                    }
                    EditorGUILayout.LabelField("Frame", GUILayout.Width(FlagWidth));
                    EditorGUILayout.LabelField(string.Format("Time {0}", VAW.VA.GetFrameTime(transitionDurationFrame), VAW.GuiStyleMiddleRightMiniLabel));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            {
                EditorGUI.BeginChangeCheck();
                var flag = EditorGUILayout.Toggle("Weight", weight);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Change Range Pinning");
                    weight = flag;
                }
            }
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Target Position");
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("X", targetPosition[0], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        targetPosition[0] = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Y", targetPosition[1], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        targetPosition[1] = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Z", targetPosition[2], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        targetPosition[2] = flag;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (enableTargetRotation)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Target Rotation");
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("X", targetRotation[0], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        targetRotation[0] = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Y", targetRotation[1], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        targetRotation[1] = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Z", targetRotation[2], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        targetRotation[2] = flag;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (enableHintPosition)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Hint Position");
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("X", hintPosition[0], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        hintPosition[0] = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Y", hintPosition[1], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        hintPosition[1] = flag;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    var flag = EditorGUILayout.ToggleLeft("Z", hintPosition[2], GUILayout.Width(FlagWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change Range Pinning");
                        hintPosition[2] = flag;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Set"))
                {
                    Set();
                    Close();
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        private void Set()
        {
            var startTime = VAW.VA.GetFrameTime(rangeFirstFrame);
            var endTime = VAW.VA.GetFrameTime(rangeLastFrame);
            var startTransitionDurationTime = Mathf.Max(VAW.VA.GetFrameTime(rangeFirstFrame - transitionDurationFrame), 0f);
            var endTransitionDurationTime = Mathf.Min(VAW.VA.GetFrameTime(rangeLastFrame + transitionDurationFrame), VAW.VA.CurrentClip.length);

            List<EditorCurveBinding> changedBindings = new();
            foreach (var boneIndex in VAW.VA.SelectionBones)
            {
                if (!VAW.VA.Bones[boneIndex].TryGetComponent<IRigConstraint>(out var constraint))
                    continue;

                #region Weight
                if (weight)
                {
                    var path = AnimationUtility.CalculateTransformPath(VAW.VA.Bones[boneIndex].transform, VAW.GameObject.transform);
                    var binding = EditorCurveBinding.FloatCurve(path, constraint.GetType(), "m_Weight");
                    var curve = VAW.VA.GetAnimationCurveCustomProperty(binding);
                    if (enableTransitionDuration)
                    {
                        if (startTransitionDurationTime != startTime)
                        {
                            VAW.VA.SetKeyframe(curve, startTransitionDurationTime, curve.Evaluate(startTransitionDurationTime));
                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, startTransitionDurationTime);
                            VAW.VA.RemoveBetweenKeyframe(curve, startTransitionDurationTime, startTime);
                        }
                        if (endTime != endTransitionDurationTime)
                        {
                            VAW.VA.SetKeyframe(curve, endTransitionDurationTime, curve.Evaluate(endTransitionDurationTime));
                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, endTransitionDurationTime);
                            VAW.VA.RemoveBetweenKeyframe(curve, endTime, endTransitionDurationTime);
                        }
                    }
                    {
                        VAW.VA.SetKeyframe(curve, startTime, 1f);
                        VAW.VA.SetKeyframe(curve, endTime, 1f);
                        VAW.VA.RemoveBetweenKeyframe(curve, startTime, endTime);
                        VAW.VA.SetKeyframeTangentFlat(curve, startTime);
                        VAW.VA.SetKeyframeTangentFlat(curve, endTime);
                    }
                    VAW.VA.SetAnimationCurveCustomProperty(binding, curve);
                    changedBindings.Add(binding);
                }
                #endregion

                if (constraint is MultiAimConstraint)
                {
                    #region MultiAimConstraint
                    var multiAimConstraint = constraint as MultiAimConstraint;
                    for (int i = 0; i < multiAimConstraint.data.sourceObjects.Count; i++)
                    {
                        if (multiAimConstraint.data.sourceObjects[i].transform == null)
                            continue;

                        var targetBoneIndex = VAW.VA.BonesIndexOf(multiAimConstraint.data.sourceObjects[i].transform.gameObject);
                        if (targetBoneIndex >= 0)
                        {
                            if (targetPosition[0] || targetPosition[1] || targetPosition[2])
                            {
                                var startPosition = VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex, startTime);
                                var endPosition = useEndFrame ? VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex, endTime) : startPosition;
                                for (int dof = 0; dof < 3; dof++)
                                {
                                    if (!targetPosition[dof])
                                        continue;
                                    var curve = VAW.VA.GetAnimationCurveTransformPosition(targetBoneIndex, dof);
                                    if (enableTransitionDuration)
                                    {
                                        if (startTransitionDurationTime != startTime)
                                        {
                                            VAW.VA.SetKeyframe(curve, startTransitionDurationTime, curve.Evaluate(startTransitionDurationTime));
                                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, startTransitionDurationTime);
                                            VAW.VA.RemoveBetweenKeyframe(curve, startTransitionDurationTime, startTime);
                                        }
                                        if (endTime != endTransitionDurationTime)
                                        {
                                            VAW.VA.SetKeyframe(curve, endTransitionDurationTime, curve.Evaluate(endTransitionDurationTime));
                                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, endTransitionDurationTime);
                                            VAW.VA.RemoveBetweenKeyframe(curve, endTime, endTransitionDurationTime);
                                        }
                                    }
                                    {
                                        VAW.VA.SetKeyframe(curve, startTime, startPosition[dof]);
                                        VAW.VA.SetKeyframe(curve, endTime, endPosition[dof]);
                                        VAW.VA.RemoveBetweenKeyframe(curve, startTime, endTime);
                                        VAW.VA.SetKeyframeTangentFlat(curve, startTime);
                                        VAW.VA.SetKeyframeTangentFlat(curve, endTime);
                                    }
                                    VAW.VA.SetAnimationCurveTransformPosition(targetBoneIndex, dof, curve);
                                    changedBindings.Add(VAW.VA.AnimationCurveBindingTransformPosition(targetBoneIndex, dof));
                                }
                            }
                        }
                    }
                    #endregion
                }
                else if (constraint is TwoBoneIKConstraint)
                {
                    #region TwoBoneIKConstraint
                    var twoBoneIKConstraint = constraint as TwoBoneIKConstraint;
                    if (twoBoneIKConstraint.data.target != null)
                    {
                        var targetBoneIndex = VAW.VA.BonesIndexOf(twoBoneIKConstraint.data.target.gameObject);
                        if (targetBoneIndex >= 0)
                        {
                            if (targetPosition[0] || targetPosition[1] || targetPosition[2])
                            {
                                var startPosition = VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex, startTime);
                                var endPosition = useEndFrame ? VAW.VA.GetAnimationValueTransformPosition(targetBoneIndex, endTime) : startPosition;
                                for (int dof = 0; dof < 3; dof++)
                                {
                                    if (!targetPosition[dof])
                                        continue;
                                    var curve = VAW.VA.GetAnimationCurveTransformPosition(targetBoneIndex, dof);
                                    if (enableTransitionDuration)
                                    {
                                        if (startTransitionDurationTime != startTime)
                                        {
                                            VAW.VA.SetKeyframe(curve, startTransitionDurationTime, curve.Evaluate(startTransitionDurationTime));
                                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, startTransitionDurationTime);
                                            VAW.VA.RemoveBetweenKeyframe(curve, startTransitionDurationTime, startTime);
                                        }
                                        if (endTime != endTransitionDurationTime)
                                        {
                                            VAW.VA.SetKeyframe(curve, endTransitionDurationTime, curve.Evaluate(endTransitionDurationTime));
                                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, endTransitionDurationTime);
                                            VAW.VA.RemoveBetweenKeyframe(curve, endTime, endTransitionDurationTime);
                                        }
                                    }
                                    {
                                        VAW.VA.SetKeyframe(curve, startTime, startPosition[dof]);
                                        VAW.VA.SetKeyframe(curve, endTime, endPosition[dof]);
                                        VAW.VA.RemoveBetweenKeyframe(curve, startTime, endTime);
                                        VAW.VA.SetKeyframeTangentFlat(curve, startTime);
                                        VAW.VA.SetKeyframeTangentFlat(curve, endTime);
                                    }
                                    VAW.VA.SetAnimationCurveTransformPosition(targetBoneIndex, dof, curve);
                                    changedBindings.Add(VAW.VA.AnimationCurveBindingTransformPosition(targetBoneIndex, dof));
                                }
                            }
                            if (targetRotation[0] || targetRotation[1] || targetRotation[2])
                            {
                                var startRotation = VAW.VA.GetAnimationValueTransformRotation(targetBoneIndex, startTime).eulerAngles;
                                var endRotation = useEndFrame ? VAW.VA.GetAnimationValueTransformRotation(targetBoneIndex, endTime).eulerAngles : startRotation;
                                var rotationMode = VAW.VA.GetHaveAnimationCurveTransformRotationMode(targetBoneIndex);
                                if (rotationMode != URotationCurveInterpolation.Mode.Undefined)
                                {
                                    if (rotationMode != URotationCurveInterpolation.Mode.RawEuler)
                                    {
                                        VAW.VA.UpdateSyncEditorCurveClip();
                                        List<EditorCurveBinding> convertBindings = new();
                                        {
                                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                convertBindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(targetBoneIndex, dofIndex, URotationCurveInterpolation.Mode.Baked));
                                            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
                                                convertBindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(targetBoneIndex, dofIndex, URotationCurveInterpolation.Mode.NonBaked));
                                        }
                                        VAW.VA.URotationCurveInterpolation.SetInterpolation(VAW.VA.CurrentClip, convertBindings.ToArray(), URotationCurveInterpolation.Mode.RawEuler);
                                        VAW.VA.ClearEditorCurveCache();
                                        rotationMode = VAW.VA.GetHaveAnimationCurveTransformRotationMode(targetBoneIndex);
                                        #region FixReverseRotation
                                        for (int dof = 0; dof < 3; dof++)
                                        {
                                            var curve = VAW.VA.GetAnimationCurveTransformRotation(targetBoneIndex, dof, rotationMode, false);
                                            if (curve != null && VAW.VA.FixReverseRotationEuler(curve))
                                                VAW.VA.SetAnimationCurveTransformRotation(targetBoneIndex, dof, rotationMode, curve);
                                        }
                                        #endregion
                                    }
                                }
                                if (rotationMode == URotationCurveInterpolation.Mode.RawEuler)
                                {
                                    AnimationCurve[] curves = new AnimationCurve[3];
                                    for (int dof = 0; dof < 3; dof++)
                                    {
                                        curves[dof] = VAW.VA.GetAnimationCurveTransformRotation(targetBoneIndex, dof, rotationMode);

                                        if (!targetRotation[dof])
                                            continue;
                                        VAW.VA.SetKeyframe(curves[dof], startTime, curves[dof].Evaluate(startTime));
                                        VAW.VA.SetKeyframe(curves[dof], endTime, curves[dof].Evaluate(endTime));
                                        VAW.VA.RemoveBetweenKeyframe(curves[dof], startTime, endTime);
                                    }
                                    var fixStartRotation = VAW.VA.FixReverseRotationEuler(curves, startTime, startRotation);
                                    var fixEndRotation = VAW.VA.FixReverseRotationEuler(curves, endTime, endRotation);
                                    for (int dof = 0; dof < 3; dof++)
                                    {
                                        if (!targetRotation[dof])
                                            continue;
                                        if (enableTransitionDuration)
                                        {
                                            if (startTransitionDurationTime != startTime)
                                            {
                                                VAW.VA.SetKeyframe(curves[dof], startTransitionDurationTime, curves[dof].Evaluate(startTransitionDurationTime));
                                                VAW.VA.SetKeyframeTangentModeClampedAuto(curves[dof], startTransitionDurationTime);
                                                VAW.VA.RemoveBetweenKeyframe(curves[dof], startTransitionDurationTime, startTime);
                                            }
                                            if (endTime != endTransitionDurationTime)
                                            {
                                                VAW.VA.SetKeyframe(curves[dof], endTransitionDurationTime, curves[dof].Evaluate(endTransitionDurationTime));
                                                VAW.VA.SetKeyframeTangentModeClampedAuto(curves[dof], endTransitionDurationTime);
                                                VAW.VA.RemoveBetweenKeyframe(curves[dof], endTime, endTransitionDurationTime);
                                            }
                                        }
                                        {
                                            VAW.VA.SetKeyframe(curves[dof], startTime, fixStartRotation[dof]);
                                            VAW.VA.SetKeyframe(curves[dof], endTime, fixEndRotation[dof]);
                                            VAW.VA.SetKeyframeTangentFlat(curves[dof], startTime);
                                            VAW.VA.SetKeyframeTangentFlat(curves[dof], endTime);
                                        }
                                        VAW.VA.SetAnimationCurveTransformRotation(targetBoneIndex, dof, rotationMode, curves[dof]);
                                        changedBindings.Add(VAW.VA.AnimationCurveBindingTransformRotation(targetBoneIndex, dof, rotationMode));
                                    }
                                }
                            }
                        }
                    }
                    if (twoBoneIKConstraint.data.hint != null)
                    {
                        var hintBoneIndex = VAW.VA.BonesIndexOf(twoBoneIKConstraint.data.hint.gameObject);
                        if (hintBoneIndex >= 0)
                        {
                            if (hintPosition[0] || hintPosition[1] || hintPosition[2])
                            {
                                var startPosition = VAW.VA.GetAnimationValueTransformPosition(hintBoneIndex, startTime);
                                var endPosition = useEndFrame ? VAW.VA.GetAnimationValueTransformPosition(hintBoneIndex, endTime) : startPosition;
                                for (int dof = 0; dof < 3; dof++)
                                {
                                    if (!hintPosition[dof])
                                        continue;
                                    var curve = VAW.VA.GetAnimationCurveTransformPosition(hintBoneIndex, dof);
                                    if (enableTransitionDuration)
                                    {
                                        if (startTransitionDurationTime != startTime)
                                        {
                                            VAW.VA.SetKeyframe(curve, startTransitionDurationTime, curve.Evaluate(startTransitionDurationTime));
                                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, startTransitionDurationTime);
                                            VAW.VA.RemoveBetweenKeyframe(curve, startTransitionDurationTime, startTime);
                                        }
                                        if (endTime != endTransitionDurationTime)
                                        {
                                            VAW.VA.SetKeyframe(curve, endTransitionDurationTime, curve.Evaluate(endTransitionDurationTime));
                                            VAW.VA.SetKeyframeTangentModeClampedAuto(curve, endTransitionDurationTime);
                                            VAW.VA.RemoveBetweenKeyframe(curve, endTime, endTransitionDurationTime);
                                        }
                                    }
                                    {
                                        VAW.VA.SetKeyframe(curve, startTime, startPosition[dof]);
                                        VAW.VA.SetKeyframe(curve, endTime, endPosition[dof]);
                                        VAW.VA.RemoveBetweenKeyframe(curve, startTime, endTime);
                                        VAW.VA.SetKeyframeTangentFlat(curve, startTime);
                                        VAW.VA.SetKeyframeTangentFlat(curve, endTime);
                                    }
                                    VAW.VA.SetAnimationCurveTransformPosition(hintBoneIndex, dof, curve);
                                    changedBindings.Add(VAW.VA.AnimationCurveBindingTransformPosition(hintBoneIndex, dof));
                                }
                            }
                        }
                    }
                    #endregion
                }
            }

            EditorApplication.delayCall += () =>
            {
                VAW.VA.SetAnimationWindowSynchroSelection(changedBindings.ToArray());
            };
        }
    }
}
#endif
