using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace VeryAnimation
{
    internal class UAnimationOffsetPlayable
    {
        public Type PlayableType { get; private set; }

        private readonly FieldInfo m_m_Handle;
        private readonly MethodInfo m_Create;
        private readonly MethodInfo m_SetPosition;
        private readonly MethodInfo m_SetRotation;
        private readonly MethodInfo m_GetPosition;
        private readonly MethodInfo m_GetRotation;

        private readonly object instance;
        private readonly UPlayable uPlayable;

        public UAnimationOffsetPlayable()
        {
            var asmUnityEngine = typeof(UnityEngine.Animations.AnimationClipPlayable).Assembly;
            Assert.IsNotNull(PlayableType = asmUnityEngine.GetType("UnityEngine.Animations.AnimationOffsetPlayable"));
            Assert.IsNotNull(m_m_Handle = PlayableType.GetField("m_Handle", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNotNull(m_Create = PlayableType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(m_SetPosition = PlayableType.GetMethod("SetPosition", BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNotNull(m_SetRotation = PlayableType.GetMethod("SetRotation", BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNotNull(m_GetPosition = PlayableType.GetMethod("GetPosition", BindingFlags.Public | BindingFlags.Instance));
            Assert.IsNotNull(m_GetRotation = PlayableType.GetMethod("GetRotation", BindingFlags.Public | BindingFlags.Instance));
            uPlayable = new UPlayable();
            instance = Activator.CreateInstance(PlayableType);
        }

        public Playable Create(PlayableGraph graph, Vector3 position, Quaternion rotation, int inputCount)
        {
            var obj = m_Create.Invoke(null, new object[] { graph, position, rotation, inputCount });
            var hanble = (PlayableHandle)m_m_Handle.GetValue(obj);
            return uPlayable.Create(hanble);
        }

        public void SetPosition(IPlayable playable, Vector3 value)
        {
            m_m_Handle.SetValue(instance, playable.GetHandle());
            m_SetPosition.Invoke(instance, new object[] { value });
        }
        public void SetRotation(IPlayable playable, Quaternion value)
        {
            m_m_Handle.SetValue(instance, playable.GetHandle());
            m_SetRotation.Invoke(instance, new object[] { value });
        }
        public Vector3 GetPosition(IPlayable playable)
        {
            m_m_Handle.SetValue(instance, playable.GetHandle());
            return (Vector3)m_GetPosition.Invoke(instance, null);
        }
        public Quaternion GetRotation(IPlayable playable)
        {
            m_m_Handle.SetValue(instance, playable.GetHandle());
            return (Quaternion)m_GetRotation.Invoke(instance, null);
        }
    }
}
