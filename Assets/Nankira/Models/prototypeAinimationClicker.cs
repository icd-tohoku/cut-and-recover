using UnityEngine;
using System.Collections;

public class prototypeAinimationClicker : MonoBehaviour
{

    int blendShapeCount;
    SkinnedMeshRenderer skinnedMeshRenderer;
    Mesh skinnedMesh;
    int motion;
    int motionIndex;

    void Awake ()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer> ();
        skinnedMesh = GetComponent<SkinnedMeshRenderer> ().sharedMesh;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        motion = 0;
        blendShapeCount = skinnedMesh.blendShapeCount;
        motionIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("キー１");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
