using UnityEngine;

public class CamraMove : MonoBehaviour
{
    [Range(0f, 100f)] public float percent = 0f;

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
        // 0〜1に正規化
        float t = percent / 100f;

        // 位置補間
        _leftCameraObj.transform.localPosition = Vector3.Lerp(_startPoint.position, _leftEndPoint.position, t);
        _rightCameraObj.transform.localPosition= Vector3.Lerp(_startPoint.position, _rightEndPoint.position, t);

        // 回転補間
        _leftCameraObj.transform.localRotation = Quaternion.Lerp(_startPoint.rotation, _leftEndPoint.rotation, t);
        _rightCameraObj.transform.localRotation= Quaternion.Lerp(_startPoint.rotation, _rightEndPoint.rotation, t);
    }
}
