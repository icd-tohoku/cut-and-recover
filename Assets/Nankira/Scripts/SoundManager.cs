using CriWare;
using UnityEngine;
using DG.Tweening;

public class SoundManager : MonoBehaviour
{
    [SerializeField] CriAtomSource _atomsource;

    [SerializeField] CRAnimationSyncronizer _syncronizer;

    float _percent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(ProcessManager.gameState == ProcessManager.GameState.Recover)
        {
            _percent = _syncronizer.percent;
            // 0〜1に正規化
            float t = _percent / 100f;

            _atomsource.SetAisacControl("AisacControl_00", t);
        }
        
    }

    public void CutSoundEffect()
    {
        DOTween.To(
            () => 0f,
            x => _atomsource.SetAisacControl("AisacControl_00", x),
            1f,
            0.5f
            );
    }
}
