using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class UAnimationMode
    {
        readonly FieldInfo m_onAnimationRecordingStart;
        readonly FieldInfo m_onAnimationRecordingStop;
        readonly MethodInfo mi_RevertPropertyModificationsForGameObject;

        public UAnimationMode()
        {
            var type = typeof(AnimationMode);

            Assert.IsNotNull(m_onAnimationRecordingStart = type.GetField("onAnimationRecordingStart", BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(m_onAnimationRecordingStop = type.GetField("onAnimationRecordingStop", BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(mi_RevertPropertyModificationsForGameObject = type.GetMethod("RevertPropertyModificationsForGameObject", BindingFlags.NonPublic | BindingFlags.Static));
        }

        public Action GetOnAnimationRecordingStart()
        {
            return (Action)m_onAnimationRecordingStart.GetValue(null);
        }
        public void SetOnAnimationRecordingStart(Action action)
        {
            m_onAnimationRecordingStart.SetValue(null, action);
        }

        public Action GetOnAnimationRecordingStop()
        {
            return (Action)m_onAnimationRecordingStop.GetValue(null);
        }
        public void SetOnAnimationRecordingStop(Action action)
        {
            m_onAnimationRecordingStop.SetValue(null, action);
        }

        public void RevertPropertyModificationsForGameObject(GameObject gameObject)
        {
            if (gameObject == null) return;
            mi_RevertPropertyModificationsForGameObject.Invoke(null, new object[] { gameObject });
        }
    }
}
