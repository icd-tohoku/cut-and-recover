using UnityEngine;

public class adjustchair : MonoBehaviour
{
    [SerializeField] Transform Headpoint;
    [SerializeField] float height; //アバターの身長
    private bool ischeckd; //調整の有無を判断する
    [SerializeField] float checktime; //座高調整時間
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ischeckd = false;
        checktime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ischeckd)
        {
            //椅子の位置のうち、y座標のみを変更
            Vector3 chairpos = transform.position;
            chairpos.y = Headpoint.position.y - height;
            transform.position = chairpos;

            //十分な時間が経過したら調整を終了
            checktime++;
            if (checktime > 100) ischeckd = true;
        }  
    }
}
