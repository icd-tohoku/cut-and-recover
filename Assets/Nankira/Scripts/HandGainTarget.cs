using UnityEngine;

public class HandGainTarget : MonoBehaviour
{
    [Header("ゲイン前の手とゲインをかける基準点")]
    [SerializeField] Transform _handPoint;
    [SerializeField] Transform _referencePoint;

    [Header("ゲイン値")]
    [SerializeField] float _positionGain = 1.5f;
    [SerializeField] float _rorationGain = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Assert( _handPoint != null && _referencePoint != null );

        Vector3 _refPos = _referencePoint.position;
        Vector3 _delta = _handPoint.position - _refPos;
        transform.position = _refPos + _delta * _positionGain;

        transform.rotation = Quaternion.Slerp(_referencePoint.rotation, _handPoint.rotation, _rorationGain);
    }
}
