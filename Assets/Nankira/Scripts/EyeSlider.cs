using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EyeSlider : MonoBehaviour
{
    private Animator _anim;

    private static readonly int _onTriggerPressed = Animator.StringToHash("OnTriggerPressed");

    private void Start()
    {
        _anim = GetComponent<Animator>();
        TriggerRoutine().Forget();
    }

    // アニメーションを自動再生するテスト
    // 10トリガーなので5往復する
    private async UniTaskVoid TriggerRoutine()
    {
        for (var i = 0; i < 10; i++)
        {
            _anim.SetTrigger(_onTriggerPressed);
            await UniTask.WaitForSeconds(2f);
        }
    }
}