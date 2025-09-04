using UnityEngine;

public class RHandMove : MonoBehaviour
{
    public enum MirrorAxis { X, Y, Z }

    [System.Serializable]
    public class Item
    {
        [Header("動かしたい対象（上腕/前腕/手 など）")]
        public Transform driven;        // 駆動（例: 左）
        public Transform mirrorDriven;  // 自動ミラー（例: 右）

        [Header("0%/100%時の基準（driven親ローカル基準が理想）")]
        public Transform startPoint;    // 0%
        public Transform endPoint;      // 100%

        [Header("適用する成分")]
        public bool applyPosition = true;
        public bool applyRotation = true;

        [Header("ミラーの微調整（任意／Targetローカルで後付け回転）")]
        public Vector3 mirrorExtraEuler = Vector3.zero;
    }

    [Header("同期元（速度込み percent を想定）")]
    [SerializeField] CRAnimationSyncronizer syncronizer;

    [Header("補間カーブ（0..1→0..1）")]
    [SerializeField] AnimationCurve _curve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("ミラー基準（このTransformのローカル軸で左右を判断）")]
    [SerializeField] Transform mirrorRoot;          // 未指定なら this.transform を使用
    [SerializeField] MirrorAxis mirrorAxis = MirrorAxis.X;

    [Header("対象リスト（上腕/前腕/手…を追加）")]
    public Item[] items;

    void Update()
    {
        if (syncronizer == null || items == null) return;

        float p = Mathf.Clamp(syncronizer.percent, 0f, 100f);
        float t = Mathf.Clamp01(_curve.Evaluate(p / 100f));

        foreach (var it in items)
        {
            if (it == null || it.driven == null || it.startPoint == null || it.endPoint == null) continue;

            // --- driven 側：ローカル補間（元の実装踏襲） ---
            Vector3 p0 = it.startPoint.localPosition;
            Vector3 p1 = it.endPoint.localPosition;
            Quaternion r0 = it.startPoint.localRotation;
            Quaternion r1 = it.endPoint.localRotation;

            Vector3 pLerp = Vector3.Lerp(p0, p1, t);
            Quaternion rLerp = Quaternion.Slerp(r0, r1, t);

            if (it.applyPosition) it.driven.localPosition = pLerp;
            if (it.applyRotation) it.driven.localRotation = rLerp;

            // --- mirrorDriven 側：鏡映してワールド適用（親が違ってもOK） ---
            if (it.mirrorDriven != null)
            {
                Transform mr = mirrorRoot != null ? mirrorRoot : this.transform;

                // driven 親ローカル → ワールド
                Transform drivenParent = it.driven.parent != null ? it.driven.parent : it.driven;
                Matrix4x4 M_localDrivenParent = Matrix4x4.TRS(pLerp, rLerp, Vector3.one);
                Matrix4x4 M_world = drivenParent.localToWorldMatrix * M_localDrivenParent;

                // ワールド → mirrorRootローカル
                Matrix4x4 W2L = mr.worldToLocalMatrix;
                Matrix4x4 L2W = mr.localToWorldMatrix;
                Matrix4x4 M_localMR = W2L * M_world;

                // 鏡映行列 S
                Vector3 s = Vector3.one;
                switch (mirrorAxis)
                {
                    case MirrorAxis.X: s = new Vector3(-1, 1, 1); break;
                    case MirrorAxis.Y: s = new Vector3(1, -1, 1); break;
                    case MirrorAxis.Z: s = new Vector3(1, 1, -1); break;
                }

                // 位置: pos' = S * pos
                Vector3 posL = M_localMR.GetColumn(3);
                Vector3 posL_mir = new Vector3(s.x * posL.x, s.y * posL.y, s.z * posL.z);

                // 回転: R' = S * R * S
                // R = 3x3 のみ抽出
                float r00 = M_localMR.m00, r01 = M_localMR.m01, r02 = M_localMR.m02;
                float r10 = M_localMR.m10, r11 = M_localMR.m11, r12 = M_localMR.m12;
                float r20 = M_localMR.m20, r21 = M_localMR.m21, r22 = M_localMR.m22;

                // 右掛け R*S（列スケール）
                r00 *= s.x; r01 *= s.y; r02 *= s.z;
                r10 *= s.x; r11 *= s.y; r12 *= s.z;
                r20 *= s.x; r21 *= s.y; r22 *= s.z;

                // 左掛け S*(R*S)（行スケール）
                r00 *= s.x; r01 *= s.x; r02 *= s.x;
                r10 *= s.y; r11 *= s.y; r12 *= s.y;
                r20 *= s.z; r21 *= s.z; r22 *= s.z;

                // 行列→Quaternion（列ベクトル: forward=col2, up=col1）
                Vector3 f = new Vector3(r02, r12, r22);
                Vector3 u = new Vector3(r01, r11, r21);
                Quaternion rotL_mir = Quaternion.LookRotation(f, u);

                // 任意の微調整（Targetローカルで追加）
                if (it.mirrorExtraEuler != Vector3.zero)
                    rotL_mir = rotL_mir * Quaternion.Euler(it.mirrorExtraEuler);

                // mirrorRootローカル → ワールド
                Vector3 posW_mir = L2W.MultiplyPoint3x4(posL_mir);
                Quaternion rotW_mir = mr.rotation * rotL_mir;

                // ミラー適用（ワールドで安定反映）
                if (it.applyPosition) it.mirrorDriven.position = posW_mir;
                if (it.applyRotation) it.mirrorDriven.rotation = rotW_mir;
            }
        }
    }
}
