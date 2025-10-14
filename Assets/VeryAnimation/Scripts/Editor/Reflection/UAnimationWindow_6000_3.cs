#if UNITY_6000_3_OR_NEWER
using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace VeryAnimation
{
    internal class UAnimationWindow_6000_3 : UAnimationWindow_2023_1
    {
        protected UAnimationWindowState_6000_3 uAnimationWindowState_6000_3;
        protected UAnimationWindowControl_6000_3 uAnimationWindowControl_6000_3;

        protected class UAnimationWindowState_6000_3 : UAnimationWindowState_2023_1
        {
            public UAnimationWindowState_6000_3(Assembly asmUnityEditor) : base(asmUnityEditor)
            {
                mi_ForceRefresh = null;
            }

            public override void ForceRefresh(object instance)
            {
                if (instance == null) return;

                pi_refresh.SetValue(instance, (int)RefreshType.Everything);
            }
            public override object GetControlInterface(object instance)
            {
                if (instance == null) return null;
                if (dg_get_controlInterface == null || dg_get_controlInterface.Target != instance)
                    dg_get_controlInterface = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), instance, instance.GetType().GetProperty("controller").GetGetMethod());
                return dg_get_controlInterface();
            }
        }
        protected class UAnimationWindowControl_6000_3 : UAnimationWindowControl
        {
            public UAnimationWindowControl_6000_3(Assembly asmUnityEditor) : base(asmUnityEditor)
            {
                var animationWindowControlType = asmUnityEditor.GetType("UnityEditor.AnimationWindowBuiltin.AnimationWindowControl");
                Assert.IsNotNull(dg_get_m_Graph = EditorCommon.CreateGetFieldDelegate<PlayableGraph>(animationWindowControlType.GetField("m_Graph", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_get_m_ClipPlayable = EditorCommon.CreateGetFieldDelegate<AnimationClipPlayable>(animationWindowControlType.GetField("m_ClipPlayable", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_get_m_CandidateClipPlayable = EditorCommon.CreateGetFieldDelegate<AnimationClipPlayable>(animationWindowControlType.GetField("m_CandidateClipPlayable", BindingFlags.NonPublic | BindingFlags.Instance)));
                Assert.IsNotNull(dg_get_m_DefaultPosePlayable = EditorCommon.CreateGetFieldDelegate<AnimationClipPlayable>(animationWindowControlType.GetField("m_DefaultPosePlayable", BindingFlags.NonPublic | BindingFlags.Instance)));
            }
        }

        public UAnimationWindow_6000_3()
        {
            var asmUnityEditor = Assembly.LoadFrom(InternalEditorUtility.GetEditorAssemblyPath());
            uAnimationWindowState = uAnimationWindowState_2023_1 = uAnimationWindowState_6000_3 = new UAnimationWindowState_6000_3(asmUnityEditor);
            uAnimationWindowControl = uAnimationWindowControl_6000_3 = new UAnimationWindowControl_6000_3(asmUnityEditor);
        }
    }
}
#endif
