using System;
using UnityEditor;
using UnityEngine;

namespace VeryAnimation
{
    internal class RootTrail
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        private readonly AnimationCurve[] curves;
        private Vector3[] trailPositions;

        public RootTrail()
        {
            curves = new AnimationCurve[3];
        }

        public void Draw()
        {
            if (!VAW.VA.IsHuman) return;

            #region CurveReady
            for (int dof = 0; dof < 3; dof++)
            {
                curves[dof] = VAW.VA.GetAnimationCurveAnimatorRootT(dof, false);
                if (curves[dof] == null) return;
            }
            #endregion

            var lastFrame = VAW.VA.GetLastFrame();
            if (trailPositions == null || trailPositions.Length != lastFrame + 1)
                trailPositions = new Vector3[lastFrame + 1];

            var matrix = VAW.VA.TransformPoseSave.StartMatrix;

            VAW.UHandleUtility.ApplyWireMaterial();
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.LINE_STRIP);
            GL.Color(VAW.EditorSettings.SettingExtraRootTrailColor);
            {
                var beforeTime = 0f;
                var beforePos = Vector3.zero;
                for (int frame = 0; frame <= lastFrame; frame++)
                {
                    var time = VAW.VA.GetFrameTime(frame);
                    var pos = matrix.MultiplyPoint3x4(GetCurveValue(time) * VAW.VA.Skeleton.Animator.humanScale);

                    if (frame > 0)
                    {
                        const float Granularity = 0.04f;
                        const int MaxCount = 64;
                        var screenLength = Vector2.Distance(HandleUtility.WorldToGUIPoint(beforePos), HandleUtility.WorldToGUIPoint(pos));
                        int count = Math.Min(Mathf.RoundToInt(screenLength * Granularity), MaxCount);
                        var step = 1f / (count + 1f);
                        for (int i = 0; i < count; i++)
                        {
                            var rate = step * (i + 1);
                            var stepTime = Mathf.Lerp(beforeTime, time, rate);
                            var stepPos = matrix.MultiplyPoint3x4(GetCurveValue(stepTime) * VAW.VA.Skeleton.Animator.humanScale);
                            GL.Vertex(stepPos);
                        }
                    }
                    GL.Vertex(pos);

                    beforeTime = time;
                    beforePos = pos;
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        private Vector3 GetCurveValue(float time)
        {
            var pos = Vector3.zero;
            for (int dof = 0; dof < 3; dof++)
                pos[dof] = curves[dof].Evaluate(time);
            return pos;
        }
    }
}
