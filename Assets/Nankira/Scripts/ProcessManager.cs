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
    public GameState gameState;

    [SerializeField] PlayableDirector _cutTimeline;

    [SerializeField] InputAction _startCut;

    [SerializeField] GameObject _rHandMoveObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameState = GameState.Ready;
        _startCut.Enable();
        _cutTimeline.Stop();
        _rHandMoveObj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameState)
        {
            case GameState.Ready:
                if (IsStartPressed())
                {
                    ChangeState(GameState.Cut);
                }
                break;

            case GameState.Cut:
                // 毎フレームの処理があればここに
                break;

            case GameState.Recover:
                // 復元フェーズの処理
                break;

            case GameState.End:
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
