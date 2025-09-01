using UnityEngine;
using UnityEngine.InputSystem;

public class CRAnimationSyncronizer : MonoBehaviour
{
    [Header("Input (毎フレーム外部から設定)")]
    [SerializeField] float _speed = 0f;           // 単位: 進行度/秒（例: 100なら 0→100 を1秒）

    float _target = 100f;  // percentの到達先（0 or 100）
    [Range(0, 100)] public float percent = 0f;

    void Update()
    {
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            _speed += 3f;
        }
        // ←を押すたびに速度が減速（逆方向）
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            _speed -= 3f;
        }
        // speed に応じて 0..100 の進行度を前進/後退させる
        float step = _speed * Time.deltaTime;           // 進む量

        if(0<=percent && percent <= 100)
        {
            percent = Mathf.MoveTowards(percent, _target, step);
        }
        
    }

    public void StartCut()
    {
        _speed = 300f;
    }

    // 任意イベント用のヘルパ
    public void GoTo100() => _target = 100f;
    public void GoTo0() => _target = 0f;
    public void SetSpeed(float s) => _speed = s;   // 外部から毎フレーム呼んでもOK
}
