using UnityEngine;

public class HandGainTarget : MonoBehaviour
{
    [Header("ゲイン前の手とゲインをかける基準点")]
    [SerializeField] Transform _handPoint;
    [SerializeField] Transform _referencePoint;

    [Header("ゲイン値")]
    [SerializeField] Vector3 _positionGain;
    [SerializeField] float _rorationGain = 1.0f;

    Vector3 _refPos;
    Vector3 _delta;
    float _geinedPos_x;
    float _geinedPos_y;
    float _geinedPos_z;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Assert( _handPoint != null && _referencePoint != null );

        _refPos = _referencePoint.position;
        _delta = _handPoint.position - _refPos;

        _geinedPos_x = _refPos.x + _delta.x * _positionGain.x;
        _geinedPos_y = _refPos.y + _delta.y * _positionGain.y;
        _geinedPos_z = _refPos.z + _delta.z * _positionGain.z;

        transform.position = new Vector3(_geinedPos_x, _geinedPos_y, _geinedPos_z);

        transform.rotation = Quaternion.Slerp(_referencePoint.rotation, _handPoint.rotation, _rorationGain);
    }
}
