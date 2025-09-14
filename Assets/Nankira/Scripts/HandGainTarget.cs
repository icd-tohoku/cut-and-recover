using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Drawing;

public class HandGainTarget : MonoBehaviour
{
    [Header("ゲイン前の手")]
    [SerializeField] Transform _handPoint;
    public OVRHand handtracked;

    [System.Serializable]
    public class GainInfo
    {
        [Header("基準点とゲイン値")]
        public Transform referencePoint;
        public Vector3 positionGain = Vector3.one;

        [Header("ある点からの距離でゲインを有効化する場合")]
        public Transform headpoint;
        public bool useDistanceGate = false;
        public float enableDistance = 1.0f;
        public float distance = 1.0f;
    }

    [SerializeField] public GainInfo[] _gainInfos;

    void Update()
    {
        // ハンドトラッキングができていない、または信頼性が低い場合は処理を行わない

        Debug.Assert(_handPoint != null, "Hand point is not assigned.");

        if (_gainInfos == null || _gainInfos.Length == 0)
            return;

        // 1) ベース位置（基準点の平均）を作る
        Vector3 basePos = Vector3.zero;
        int baseCount = 0;

        // 2) 合成する差分（= offset * gain）の合計
        Vector3 totalOffset = Vector3.zero;

        foreach (var info in _gainInfos)
        {
            if (info == null) continue;
            /*

            // 距離ゲート
            if (info.useDistanceGate)
            {
                float dis = Vector3.Distance(_handPoint.position, info.referencePoint.position);
                info.distance = dis;
                if (dis > info.enableDistance) continue;
            }*/

            // ベース位置用（平均）
            basePos += info.referencePoint.position;
            baseCount++;

            // 差分 × ゲイン を合成
            Vector3 offset = _handPoint.position - info.referencePoint.position;
            totalOffset += Vector3.Scale(offset, info.positionGain); // 各軸ごとに乗算
        }

        if (baseCount == 0) return;

        basePos /= baseCount; // 平均

        // 3) 最終位置 = ベース位置 + 合成オフセット
        Vector3 calculatedPosition = basePos + totalOffset;

        if (!handtracked.IsTracked || !handtracked.IsDataHighConfidence)
        {
            calculatedPosition = transform.position;
           
        }

        // 4) めり込み防止処理                                                                                                                                                                                                                           
        foreach (var info in _gainInfos)
        {
            if (info != null && info.useDistanceGate)
            {
                float dis = Vector3.Distance(calculatedPosition, info.headpoint.position);
                if (dis < info.enableDistance)
                {
                    Vector3 direction = (calculatedPosition - info.headpoint.position).normalized; 
                    if (direction == Vector3.zero) direction = Vector3.up; // 念のため                                                                                                                                                                   
                    calculatedPosition = info.headpoint.position + direction * info.enableDistance;
                }
            }
        }
        transform.position = calculatedPosition;
    }
}
