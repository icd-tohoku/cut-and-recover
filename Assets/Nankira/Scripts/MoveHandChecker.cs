using System;
using UnityEngine;

public class MoveHandChecker : MonoBehaviour
{
    [SerializeField] Transform _handPos;
    [SerializeField] Transform _headPos;
    float _handPosX;
    float _prevHandPosX;

    float _threshTime = 1f;
    float _time;

    float _handXVelocity;

    int _shakeCount=0;

    bool _isWaveCheck = false;
    bool _isSpeed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        GetSpeed();

        if (_handXVelocity < -3f || _handXVelocity > 3f)
        {
            _isSpeed = true;
        }
        else
        {
            _isSpeed= false;
        }

        if (!_isWaveCheck && _isSpeed)
        {
            _isWaveCheck = true;
            _time = 0f;
        }

        if (_isWaveCheck)
        {
            _time += Time.deltaTime;
        }

        if(_time <= _threshTime)
        {
            if( _isSpeed)
            {
                //アニメーション再生
                Debug.Log("Shake!!");

                _isWaveCheck= false;
            }
        }
        else
        {
            _isWaveCheck = false;
        }
    }

    void GetSpeed()
    {
        if(Mathf.Approximately(Time.deltaTime, 0))
        {
            return;
        }
        Vector3 _handLocal = _headPos.InverseTransformPoint(_handPos.position);

        _handPosX = _handLocal.x;

        _handXVelocity = (_handPosX - _prevHandPosX) /Time.deltaTime;

        _prevHandPosX = _handPosX;
    }
}
