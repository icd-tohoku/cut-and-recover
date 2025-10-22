using System;
using System.Reflection;
using UnityEditor.SceneManagement;

namespace VeryAnimation
{
    internal class UPrefabStage
    {
        private Func<bool> dg_get_autoSave;

        public UPrefabStage()
        {
        }

        public bool GetAutoSave(PrefabStage instance)
        {
            if (instance == null) return false;
            if (dg_get_autoSave == null || dg_get_autoSave.Target != (object)instance)
                dg_get_autoSave = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, instance.GetType().GetProperty("autoSave", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));
            return dg_get_autoSave();
        }
    }
}
