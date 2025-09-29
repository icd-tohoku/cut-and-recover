using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

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

    [SerializeField] ParticleSystem _recoverEffect;


    readonly String[] SwordIdleMotions = { "SwordIdle_sub1", "SwordIdle_sub2", "SwordIdle_sub3", "SwordIdle_sub4" };

    bool _sentC5 = false, _sentC6 = false, _sentC7 = false;

    bool _isStop = false;

    bool _isFinish = false;


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

        if (_pressure > 0)
        {
            _switcher.ChangeTrack(false);
        }
        else
        {
            _switcher.ChangeTrack(true);
        }

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
                break;

            case GameState.Recover:
                _syncronizer.SetSpeed(_pressure);

                if (!_isStop && _pressure <= 0)
                {
                    _serialManager.SendCommandToAllPorts("STOP;");
                    _isStop = true;
                    _sentC5 = false;
                    _sentC6 = false;
                    _sentC7 = false;
                    Debug.Log("STOP sent");
                }

                // pressure > 0 のとき、現在の区間に応じて一度だけ送る
                if (_pressure > 0)
                {
                    // 100~50
                    if (!_sentC5 && _syncronizer.percent >= 50f)
                    {
                        _serialManager.SendCommandToAllPorts("C5;");
                        _isStop = false;
                        _sentC5 = true;
                         Debug.Log("C5 sent");
                    }
                    // 50~25
                    else if (!_sentC6 && _syncronizer.percent >= 25f && _syncronizer.percent < 50f)
                    {
                        _serialManager.SendCommandToAllPorts("C6;");
                        _isStop = false;
                        _sentC6 = true;
                         Debug.Log("C6 sent");
                    }
                    // 25~0
                    else if (!_sentC7 && _syncronizer.percent > 0f && _syncronizer.percent < 25f)
                    {
                        _serialManager.SendCommandToAllPorts("C7;");
                        _isStop = false;
                        _sentC7 = true;
                         Debug.Log("C7 sent");
                    }
                }

                // percent が 0 になったら STOP（多重送信防止に _isStop を立てる）
                if (_syncronizer.percent <= 0f && !_isFinish)
                {
                    _serialManager.SendCommandToAllPorts("STOP;");
                    _isFinish = true;                // ★ 追加：連打防止
                    _recoverEffect.Play();
                    ChangeState(GameState.End);
                    _soundManager.PlaySE("Cue_2");
                }

                break;

            case GameState.End:
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    gameState = GameState.Ready;
                    _syncronizer.SetSpeed(0);
                    _sentC5 = false;
                    _sentC6 = false;
                    _sentC7 = false;
                    _isStop = false;
                    _isFinish=false;
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
