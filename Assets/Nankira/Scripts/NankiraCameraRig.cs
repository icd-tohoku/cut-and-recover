using UnityEngine;

namespace Nankira
{
    /// <summary>
    /// なんきらのカメラリグ
    /// </summary>
    /// <remarks>
    /// カメラアンカーとカメラ自身が（同一オブジェクトではなく）親子関係にあることを前提とする．
    /// アンカー座標はHMDの座標に合わせて更新される．
    /// カメラ座標はアンカー座標に対する相対座標で指定することが出来る．
    /// </remarks>
    public sealed class NankiraCameraRig : OVRCameraRig
    {
        [SerializeField] private new Camera _centerEyeCamera;
        [SerializeField] private new Camera _leftEyeCamera;
        [SerializeField] private new Camera _rightEyeCamera;

        protected override void Awake()
        {
            // カメラがインスペクターに設定されているか確認する
            // base.Awake()の前に行い，Nullチェックを回避する
            if (_centerEyeCamera == null)
                _centerEyeCamera = transform.Find(centerEyeAnchorName)
                    .GetComponentInChildren<Camera>(true);
            if (_leftEyeCamera == null)
                _leftEyeCamera = transform.Find(leftEyeAnchorName)
                    .GetComponentInChildren<Camera>(true);
            if (_rightEyeCamera == null)
                _rightEyeCamera = transform.Find(rightEyeAnchorName)
                    .GetComponentInChildren<Camera>(true);

            base.Awake();
        }

        public override void EnsureGameObjectIntegrity()
        {
            // カメラを事前に設定し，base.EnsureGameObjectIntegrity()におけるNullチェックを回避する
            if (base._centerEyeCamera == null)
                base._centerEyeCamera = _centerEyeCamera;
            if (base._leftEyeCamera == null)
                base._leftEyeCamera = _leftEyeCamera;
            if (base._rightEyeCamera == null)
                base._rightEyeCamera = _rightEyeCamera;

            base.EnsureGameObjectIntegrity();
        }
    }
}