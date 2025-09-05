using UnityEngine;

public class CRCamraMove : MonoBehaviour
{
    [SerializeField] CRAnimationSyncronizer _syncronizer;

    [SerializeField] [Range(0f, 100f)] float _percent = 0f;
    [SerializeField] AnimationCurve _curve = AnimationCurve.Linear(0, 0, 1, 1);

    [SerializeField] GameObject _leftCameraObj;
    [SerializeField] GameObject _rightCameraObj;

    [SerializeField] Transform _startPoint;
    [SerializeField] Transform _leftEndPoint;
    [SerializeField] Transform _rightEndPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _percent = _syncronizer.percent;
        // 0〜1に正規化
        float t = _percent / 100f;
        t = Mathf.Clamp01(_curve.Evaluate(t));

        // 位置補間
        _leftCameraObj.transform.localPosition = Vector3.Lerp(_startPoint.position, _leftEndPoint.position, t);
        _rightCameraObj.transform.localPosition= Vector3.Lerp(_startPoint.position, _rightEndPoint.position, t);

        // 回転補間
        _leftCameraObj.transform.localRotation = Quaternion.Lerp(_startPoint.rotation, _leftEndPoint.rotation, t);
        _rightCameraObj.transform.localRotation= Quaternion.Lerp(_startPoint.rotation, _rightEndPoint.rotation, t);
    }
}
