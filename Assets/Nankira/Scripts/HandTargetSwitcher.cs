using UnityEngine;
using RootMotion.FinalIK;
using UnityEngine.InputSystem;

public class HandTargetSwitcher : MonoBehaviour
{
    [SerializeField] VRIK _vrik;

    [Header("トラッキング時のtargetの手")]
    [SerializeField] Transform _trackedHandL;
    [SerializeField] Transform _trackedHandR;

    [Header("戻すときのtargetの手")]
    [SerializeField] Transform _recoverHandL;
    [SerializeField] Transform _recoverHandR;

    public bool isTrack = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Assert(_vrik);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.aKey.wasPressedThisFrame)
        {
            ChangeTrack(!isTrack);
        }
    }

    public void ChangeTrack(bool isTrack)
    {
        if (isTrack)
        {
            _vrik.solver.leftArm.target = _trackedHandL;
            _vrik.solver.rightArm.target = _trackedHandR;
        }
        else
        {
            _vrik.solver.leftArm.target = _recoverHandL;
            _vrik.solver.rightArm.target = _recoverHandR;
        }
        this.isTrack = isTrack;
    }
}
