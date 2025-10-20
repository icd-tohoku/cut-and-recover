using System;
using System.Reflection;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace VeryAnimation
{
    internal class UAnimationMotionXToDeltaPlayable
    {
        public Type PlayableType { get; private set; }

        private readonly FieldInfo m_m_Handle;
        private readonly MethodInfo m_Create;
        private readonly MethodInfo m_SetAbsoluteMotion;

        private readonly UPlayable uPlayable;

        public UAnimationMotionXToDeltaPlayable()
        {
            var asmUnityEngine = typeof(UnityEngine.Animations.AnimationClipPlayable).Assembly;
            Assert.IsNotNull(PlayableType = asmUnityEngine.GetType("UnityEngine.Animations.AnimationMotionXToDeltaPlayable"));
            Assert.IsNotNull(m_m_Handle = PlayableType.GetField("m_Handle", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNotNull(m_Create = PlayableType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(m_SetAbsoluteMotion = PlayableType.GetMethod("SetAbsoluteMotion"));
            uPlayable = new UPlayable();
        }

        public Playable Create(PlayableGraph graph)
        {
            var obj = m_Create.Invoke(null, new object[] { graph });
            var hanble = (PlayableHandle)m_m_Handle.GetValue(obj);
            return uPlayable.Create(hanble);
        }

        public void SetAbsoluteMotion(Playable playable, bool value)
        {
            var tmp = Activator.CreateInstance(PlayableType);
            m_m_Handle.SetValue(tmp, playable.GetHandle());
            m_SetAbsoluteMotion.Invoke(tmp, new object[] { value });
        }
    }
}
