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
        
        // speed に応じて 0..100 の進行度を前進/後退させる
        float step = Mathf.Abs(_speed) * Time.deltaTime;

        if(0<=percent && percent <= 100)
        {
            if(ProcessManager.gameState == ProcessManager.GameState.Cut)
            {
                _target = 100f;
            }
            else if(ProcessManager.gameState == ProcessManager.GameState.Recover)
            {
                _target = 0f;
            }
            percent = Mathf.MoveTowards(percent, _target, step);

        }
        
    }
    public void SetSpeed(float s) => _speed = s;
}
