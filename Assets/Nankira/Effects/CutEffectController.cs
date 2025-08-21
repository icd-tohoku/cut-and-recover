using UnityEngine;

public class CutEffectController : MonoBehaviour
{
    [SerializeField] GameObject _leftMaskObj;
    [SerializeField] GameObject _rightMaskObj;
    [SerializeField, Range(0f, 100f)] float _percent;
    [SerializeField] float _beforePosY;
    [SerializeField] float _afterPosY;

    void Update()
    {
        // スライダー値を0~1に正規化
        float t = _percent / 100f;

        // Y座標を補間（左目と右目のマスクオブジェクトの座標が同一である前提）
        Vector3 targetPos = _leftMaskObj.transform.localPosition;
        targetPos.y = Mathf.Lerp(_beforePosY, _afterPosY, t);
        _leftMaskObj.transform.localPosition = targetPos;
        _rightMaskObj.transform.localPosition = targetPos;
    }
}