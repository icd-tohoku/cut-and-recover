using CriWare;
using UnityEngine;

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
        _percent = _syncronizer.percent;
        // 0〜1に正規化
        float t = _percent / 100f;

        _atomsource.SetAisacControl("AisacControl_00", t);
    }
}
