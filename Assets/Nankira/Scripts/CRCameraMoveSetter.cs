using UnityEngine;
using UnityEngine.InputSystem;

namespace Nankira
{
    public class CRCameraMoveSetter : MonoBehaviour
    {
        [SerializeField] private CRCamraMove _crCameraMove;

        void Update()
        {
            // Actual（実際）の動きモードに設定
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                Debug.Log("【モード設定】実際の目の動きモード");
                SetEndPoints(true);
            }
            // Intuitive（直感的）の動きモードに設定
            else if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                SetEndPoints(false);
                Debug.Log("【モード設定】直感的な目の動きモード");
            }
        }

        private void SetEndPoints(bool isActual)
        {
            int sign = isActual ? 1 : -1;

            _crCameraMove.LeftEndPoint.position = new(-0.1f, -0.06f, 0.0f);
            _crCameraMove.LeftEndPoint.rotation = Quaternion.Euler(0.0f, 0.0f, sign * 40.0f);

            _crCameraMove.RightEndPoint.position = new(0.1f, -0.06f, 0.0f);
            _crCameraMove.RightEndPoint.rotation = Quaternion.Euler(0.0f, 0.0f, sign * -40.0f);
        }
    }
}