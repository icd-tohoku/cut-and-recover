using System.Reflection;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace VeryAnimation
{
    internal class UPlayable
    {
        private readonly FieldInfo m_m_Handle;

        public UPlayable()
        {
            Assert.IsNotNull(m_m_Handle = typeof(Playable).GetField("m_Handle", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        public Playable Create(PlayableHandle handle)
        {
            object obj = new Playable();
            m_m_Handle.SetValue(obj, handle);
            return (Playable)obj;
        }
    }
}
