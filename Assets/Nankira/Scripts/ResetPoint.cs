using UnityEngine;
using UnityEngine.InputSystem;

public class ResetPoint : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            transform.position = Vector3.zero;
        }
        
    }
}
