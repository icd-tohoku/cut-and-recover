using UnityEngine;
using UnityEngine.InputSystem;


public class EffectPlay : MonoBehaviour
{
    [SerializeField] GameObject _leftEffectObj;
    [SerializeField] GameObject _rightEffectObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _leftEffectObj.SetActive(true);
            _rightEffectObj.SetActive(true);
        }
    }
}
