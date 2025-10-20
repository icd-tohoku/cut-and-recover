using UnityEngine;

namespace VeryAnimation
{
    public class Human : MonoBehaviour
    {
        public GameObject door;

        private Vector3 savePosition;
        private Quaternion saveRotation;

        private void Awake()
        {
            savePosition = transform.localPosition;
            saveRotation = transform.localRotation;
        }

        public void Restart()
        {
            if (!TryGetComponent<Animator>(out _)) return;

#if UNITY_2022_3_OR_NEWER
            transform.SetLocalPositionAndRotation(savePosition, saveRotation);
#else
            transform.localPosition = savePosition;
            transform.localRotation = saveRotation;
#endif
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void OpenDoor()
        {
            if (door == null) return;
            if (!door.TryGetComponent<Animator>(out var animator)) return;
            animator.SetTrigger("Open");
        }
    }
}