using System;
using UnityEngine;

public class CRAvatarAnimation : MonoBehaviour
{
    [SerializeField] CRAnimationSyncronizer _syncronizer;

    [SerializeField] [Range(0f, 100f)] float _percent = 0f;
    [SerializeField] AnimationCurve _curve = AnimationCurve.Linear(0, 0, 1, 1);

    [SerializeField] GameObject _headObj;

    SkinnedMeshRenderer _smr;
    int _index;


    void Start()
    {
        _smr = _headObj.GetComponent<SkinnedMeshRenderer>();
        _index = _smr.sharedMesh.GetBlendShapeIndex("Cut off");
        if (_index < 0) Debug.LogError("ブレンドシェイプが見つかりません");
    }

    void Update()
    {

        if (_index < 0) return;

        _percent = _syncronizer.percent;
        // 0〜1に正規化
        float t = _percent / 100f;
        t = Mathf.Clamp01(_curve.Evaluate(t));
        float w = Mathf.Lerp(0, 100, t); 
        _smr.SetBlendShapeWeight(_index, w);
    }


}
