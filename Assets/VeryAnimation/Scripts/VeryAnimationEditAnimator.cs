using System;
using UnityEngine;

namespace VeryAnimation
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public class VeryAnimationEditAnimator : MonoBehaviour
    {
#if !UNITY_EDITOR
        private void Awake()
        {
            Destroy(this);
        }
#else
        public Action<int> onAnimatorIK;

        private void OnAnimatorIK(int layerIndex)
        {
            onAnimatorIK?.Invoke(layerIndex);
        }
#endif
    }
}
