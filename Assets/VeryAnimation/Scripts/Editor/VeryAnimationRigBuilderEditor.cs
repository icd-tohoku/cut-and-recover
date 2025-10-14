#if VERYANIMATION_ANIMATIONRIGGING
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace VeryAnimation
{
    [CustomEditor(typeof(VeryAnimationRigBuilder))]
    internal class VeryAnimationRigBuilderEditor : Editor
    {
        void OnEnable()
        {
            //Must be in order before RigBuilder
            var vaRigBuilder = target as VeryAnimationRigBuilder;
            if (vaRigBuilder == null) return;
            var components = vaRigBuilder.GetComponents<MonoBehaviour>();
            var indexRigBuilder = ArrayUtility.FindIndex(components, x => x != null && x.GetType() == typeof(RigBuilder));
            var indexVARigBuilder = ArrayUtility.FindIndex(components, x => x != null && x.GetType() == typeof(VeryAnimationRigBuilder));
            if (indexRigBuilder >= 0 && indexVARigBuilder >= 0)
            {
                for (int i = 0; i < indexVARigBuilder - indexRigBuilder; i++)
                    ComponentUtility.MoveComponentUp(vaRigBuilder);
            }
        }

        public override void OnInspectorGUI()
        {
        }
    }
}
#endif
