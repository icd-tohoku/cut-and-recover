using UnityEngine;
using UnityEngine.InputSystem;

namespace Nankira
{
    public class CRCameraMoveSetter : MonoBehaviour
    {
        [SerializeField] private CRCamraMove _crCameraMove;
        [SerializeField] private CRCameraMoveDto _endPointActual;

        void Update()
        {
            // Actual（実際）の動きモードに設定
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                SetEndPoints(true);
                Debug.Log("【モード設定】実際の目の動きモード");
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

            _crCameraMove.LeftEndPoint.position = new(-1.0f * _endPointActual.PositionX, _endPointActual.PositionY, 0.0f);
            _crCameraMove.LeftEndPoint.rotation = Quaternion.Euler(0.0f, 0.0f, sign * _endPointActual.RollInDeg);

            _crCameraMove.RightEndPoint.position = new(_endPointActual.PositionX, _endPointActual.PositionY, 0.0f);
            _crCameraMove.RightEndPoint.rotation = Quaternion.Euler(0.0f, 0.0f, -sign * _endPointActual.RollInDeg);
        }
    }
}