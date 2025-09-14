using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class ProcessManager : MonoBehaviour
{
    public enum  GameState
    {
        Ready,
        Cut,
        Recover,
        End
    }
    public static GameState gameState;

    [SerializeField] PlayableDirector _cutTimeline;

    [SerializeField] InputAction _startCut;

    [SerializeField] GameObject _rHandMoveObj;

    [SerializeField] GameObject _cutManObj;
    Animator _animator;

    [SerializeField] float _time = 0;
    float _threshTime = 8f;

    [SerializeField] SerialManager _serialManager;

    [SerializeField] CRAnimationSyncronizer _syncronizer;

    [SerializeField] HandTargetSwitcher _switcher;

    [SerializeField] SoundManager _soundManager;


    readonly String[] SwordIdleMotions = { "SwordIdle_sub1", "SwordIdle_sub2", "SwordIdle_sub3", "SwordIdle_sub4" };

    bool _sentP2 = false, _sentP3 = false, _sentP4 = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameState = GameState.Ready;
        _startCut.Enable();
        _cutTimeline.Stop();
        _rHandMoveObj.SetActive(false);
        _animator = _cutManObj.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float _pressure = _serialManager.GetAveragePressure("COM3");

        //if (_pressure > 0)
        //{
        //    _switcher.ChangeTrack(false);
        //}
        //else
        //{
        //    _switcher.ChangeTrack(true);
        //}

        switch (gameState)
        {
            case GameState.Ready:
                if (IsStartPressed())
                {
                    ChangeState(GameState.Cut);
                }

                //待機中切る人がランダムにモーションをとる
                _time += Time.deltaTime;

                if(_time > _threshTime)
                {
                    int _randomIndex = UnityEngine.Random.Range(0, SwordIdleMotions.Length);
                    _animator.CrossFade(SwordIdleMotions[_randomIndex], 0.6f);
                    _time = 0;

                    if(_randomIndex == 0 || _randomIndex == 3)
                    {
                        _threshTime = UnityEngine.Random.Range(16, 20);
                    }
                    else
                    {
                        _threshTime = UnityEngine.Random.Range(9, 12);
                    }
                    
                }

                break;

            case GameState.Cut:
                // 毎フレームの処理があればここに
                break;

            case GameState.Recover:
                //_syncronizer.SetSpeed(_pressure);

                ////P4,3,2で送る
                //if (!_sentP4 && _syncronizer.percent <= 50)
                //{
                //    _serialManager.SendCommandToAllPorts("P4;");
                //    _sentP4 = true;
                //}
                //if (!_sentP3 && _syncronizer.percent <= 25)
                //{
                //    _serialManager.SendCommandToAllPorts("P3;");
                //    _sentP3 = true;
                //}

                if (_syncronizer.percent <= 0)
                {
                    //_serialManager.SendCommandToAllPorts("P2;");
                    //_sentP2 = true;
                    ChangeState(GameState.End);
                    _soundManager.PlaySE("Cue_2");
                }
                break;

            case GameState.End:
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    gameState = GameState.Ready;
                    _syncronizer.SetSpeed(0);
                    _sentP2 = false;
                    _sentP3 = false;
                    _sentP4 = false;
                }
                break;
        }
    }

    void ChangeState(GameState nextState)
    {
        Debug.Assert(nextState != gameState);
        gameState = nextState;

        if(nextState == GameState.Cut)
        {
            Debug.Assert(_cutTimeline != null);

            _cutTimeline.time = 0;
            _cutTimeline.Play();
        }
        else if(nextState == GameState.Recover)
        {

        }
        else if( nextState == GameState.End)
        {
            
        }
    }

    bool IsStartPressed()
    {
        Debug.Assert(_startCut != null);
        return _startCut.WasPressedThisFrame();
    }

    public void OnCutTimelineEnd()
    {
        if (gameState == GameState.Cut)
        {
            ChangeState(GameState.Recover);
            _rHandMoveObj.SetActive(true);
        }
    }
}
