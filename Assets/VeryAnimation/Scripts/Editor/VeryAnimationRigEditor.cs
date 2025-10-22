#if VERYANIMATION_ANIMATIONRIGGING
using UnityEditor;
using UnityEngine;

namespace VeryAnimation
{
    [CustomEditor(typeof(VeryAnimationRig))]
    internal class VeryAnimationRigEditor : Editor
    {
        private VeryAnimationRig vaRig;
        private Animator animator;

        void OnEnable()
        {
            vaRig = target as VeryAnimationRig;
            animator = vaRig.GetComponentInParent<Animator>();
        }

        public override void OnInspectorGUI()
        {
            if (animator == null)
                return;

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Language.GetContent(Language.Help.VARigCurrentHumanScale));
                EditorGUILayout.LabelField(animator.humanScale.ToString(), GUILayout.Width(160f));
                if (GUILayout.Button("Copy"))
                {
                    GUIUtility.systemCopyBuffer = animator.humanScale.ToString();
                }
                EditorGUILayout.EndHorizontal();
            }
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Language.GetContent(Language.Help.VARigSourceHumanScale));
                EditorGUI.BeginChangeCheck();
                var value = EditorGUILayout.FloatField(vaRig.sourceHumanScale, GUILayout.Width(160f));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vaRig, "Change HumanScale");
                    vaRig.sourceHumanScale = value;
                }
                if (GUILayout.Button("Paste"))
                {
                    if (float.TryParse(GUIUtility.systemCopyBuffer, out var result))
                    {
                        Undo.RecordObject(vaRig, "Change HumanScale");
                        vaRig.sourceHumanScale = result;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Reset Scale"))
                {
                    Undo.RecordObject(vaRig.transform, "Change Transform Scale");
                    vaRig.ResetProperAdjustmentScale();
                }
                EditorGUILayout.Space();
                if (GUILayout.Button(Language.GetContent(Language.Help.VARigSetTransformScale)))
                {
                    Undo.RecordObject(vaRig.transform, "Change Transform Scale");
                    vaRig.SetProperAdjustmentScale();
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
