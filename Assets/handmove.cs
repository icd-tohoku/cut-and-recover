using Unity.VisualScripting;
using UnityEngine;

public class BoneTest : MonoBehaviour
{
    public OVRHand leftHand;
    public OVRHand rightHand;

    public Animator anim;
    Transform righthand;
    Transform lefthand;

    void Start()
    {
        righthand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        lefthand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
    }

    void Update()
    {
        righthand.position = rightHand.transform.position;
        lefthand.position = leftHand.transform.position;
    }
}