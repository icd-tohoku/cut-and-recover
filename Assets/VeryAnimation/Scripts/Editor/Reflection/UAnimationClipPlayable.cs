using System.Reflection;
using UnityEngine.Animations;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimationClipPlayable
    {
        private readonly MethodInfo m_SetRemoveStartOffset;
        private readonly MethodInfo m_SetOverrideLoopTime;
        private readonly MethodInfo m_SetLoopTime;
        private readonly MethodInfo m_SetSampleRate;

        public UAnimationClipPlayable()
        {
            var animationClipPlayableType = typeof(AnimationClipPlayable);
            Assert.IsNotNull(m_SetRemoveStartOffset = animationClipPlayableType.GetMethod("SetRemoveStartOffset", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNotNull(m_SetOverrideLoopTime = animationClipPlayableType.GetMethod("SetOverrideLoopTime", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNotNull(m_SetLoopTime = animationClipPlayableType.GetMethod("SetLoopTime", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNotNull(m_SetSampleRate = animationClipPlayableType.GetMethod("SetSampleRate", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        public void SetRemoveStartOffset(AnimationClipPlayable playable, bool value)
        {
            m_SetRemoveStartOffset.Invoke(playable, new object[] { value });
        }
        public void SetOverrideLoopTime(AnimationClipPlayable playable, bool value)
        {
            m_SetOverrideLoopTime.Invoke(playable, new object[] { value });
        }
        public void SetLoopTime(AnimationClipPlayable playable, bool value)
        {
            m_SetLoopTime.Invoke(playable, new object[] { value });
        }
        public void SetSampleRate(AnimationClipPlayable playable, float value)
        {
            m_SetSampleRate.Invoke(playable, new object[] { value });
        }
    }
}
