using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CutEffectController : MonoBehaviour
{
    [SerializeField] GameObject _leftMaskObj;
    [SerializeField] GameObject _rightMaskObj;
    [SerializeField, Range(0f, 100f)] float _ratio;
    [SerializeField] float _beforePosY;
    [SerializeField] float _afterPosY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        // �X���C�_�[�l��0~1�ɐ��K��
        float t = _ratio / 100f;

        // Y���W���ԁi���ڂƉE�ڂ̃}�X�N�I�u�W�F�N�g�̍��W������ł���O��j
        Vector3 targetPos = _leftMaskObj.transform.localPosition;
        targetPos.y = Mathf.Lerp(_beforePosY, _afterPosY, t);
        _leftMaskObj.transform.localPosition = targetPos;
        _rightMaskObj.transform.localPosition = targetPos;
    }
}
