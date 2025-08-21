using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class GetCutAnimation : MonoBehaviour
{
    float _speed = 600f; // 1秒あたりの変化量
    [SerializeField] GameObject _headObj;

    SkinnedMeshRenderer _smr;
    int _index;


    void Start()
    {
        _smr = _headObj.GetComponent<SkinnedMeshRenderer>();
        _index = _smr.sharedMesh.GetBlendShapeIndex("キー 1"); //全角カタカナで「キー」，半角スペース，半角数字で1
        if (_index < 0) Debug.LogError("ブレンドシェイプ 'キー 1' が見つかりません");
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(HeadCut(_index, 100f));
        }
    }

    IEnumerator HeadCut(int index, float target)
    {
        float current = _smr.GetBlendShapeWeight(index);

        // Mathf.MoveTowards を使って一定速度で動かす
        while (!Mathf.Approximately(current, target))
        {
            current = Mathf.MoveTowards(current, target, _speed * Time.deltaTime);
            _smr.SetBlendShapeWeight(index, current);
            yield return null; 
        }

        // 誤差なくぴったり揃える
        _smr.SetBlendShapeWeight(index, target);

    }
}
