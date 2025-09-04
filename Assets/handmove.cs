using Unity.VisualScripting;
using UnityEngine;

public class BoneTest : MonoBehaviour
{
    public OVRHand leftHand;
    public OVRHand rightHand;

    public Animator anim;
    Transform righthand;
    Transform lefthand;
    Transform leftelbow;
    Transform rightelbow;
    Transform leftshoulder;
    Transform rightshoulder;

    void Start()
    {
        righthand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        lefthand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        leftelbow = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightelbow = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        leftshoulder = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightshoulder = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);

    }

    void Update()
    {
        if (leftHand.IsTracked)
        {
            lefthand.position = leftHand.transform.position;
            leftelbow.position = (lefthand.position + leftshoulder.position) / 2;
            
        }
        
        if (rightHand.IsTracked)
        {
            righthand.position = rightHand.transform.position;
            rightelbow.position = (righthand.position + rightshoulder.position) / 2;
        }
    }
}