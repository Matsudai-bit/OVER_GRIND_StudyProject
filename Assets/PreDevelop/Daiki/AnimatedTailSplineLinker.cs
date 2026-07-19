using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class AnimatedTailSplineLinker : MonoBehaviour
{
    [Header("尻尾のメッシュ表面に配置されているレール用ボーン")]
    [SerializeField] private Transform[] railKnotTransforms;

    private SplineContainer splineContainer;
    private Transform selfTransform;

    // 直近に構築したナット数。配列長が変わらない限り構造更新をスキップするためのキャッシュ。
    private int lastKnotCount = -1;

    // NaN/Infinity 検出時のログスパム防止用
    private bool loggedInvalidPosThisRun = false;

    void Awake()
    {
        Application.targetFrameRate = 60;
        splineContainer = GetComponent<SplineContainer>();
        selfTransform = transform;
    }

    void LateUpdate()
    {
        if (splineContainer == null || railKnotTransforms == null || railKnotTransforms.Length == 0) return;

        var spline = splineContainer.Spline;
        int knotTargetCount = railKnotTransforms.Length;

        // ナット数が変化した場合のみ構造(Add/Remove/TangentMode)を更新する
        if (lastKnotCount != knotTargetCount)
        {
            while (spline.Count < knotTargetCount)
            {
                spline.Add(
                    new BezierKnot(
                        float3.zero,
                        float3.zero,
                        float3.zero,
                        quaternion.identity
                    ),
                    TangentMode.Linear
                );
            }
            while (spline.Count > knotTargetCount)
            {
                spline.RemoveAt(spline.Count - 1);
            }
            // 新規追加分も含め、タンジェントモードは構造変更時のみ設定すれば十分
            for (int i = 0; i < knotTargetCount; i++)
            {
                spline.SetTangentMode(i, TangentMode.AutoSmooth);
            }
            lastKnotCount = knotTargetCount;
        }

        // Transform のネイティブ呼び出しを削減するため、行列を1回だけ取得して使い回す
        Matrix4x4 worldToLocal = selfTransform.worldToLocalMatrix;

        // ---- ここが重かった箇所の対策 ----
        // spline[i] = knot; (インデクサ経由の代入)は呼ぶたびに内部の SetDirty() を発火させ、
        // 「全ナット分のキャッシュを無効化するO(n)ループ」+ Changed イベント通知を実行する。
        // これをナット数ぶん毎フレーム呼んでいたため、全体としてO(n^2)になっていた。
        //
        // 対策として SetKnotNoNotify() で通知なしにナット配列だけを書き換え、
        // ループの最後に1回だけ通知を発火させる(＝キャッシュ無効化もイベント通知も1フレーム1回のみ)。
        bool anyKnotUpdated = false;
        int lastUpdatedIndex = -1;

        for (int i = 0; i < knotTargetCount; i++)
        {
            Transform railTransform = railKnotTransforms[i];
            if (railTransform == null) continue;

            Vector3 worldPos = railTransform.position;
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos);

            if (float.IsNaN(localPos.x) || float.IsNaN(localPos.y) || float.IsNaN(localPos.z) ||
                float.IsInfinity(localPos.x) || float.IsInfinity(localPos.y) || float.IsInfinity(localPos.z))
            {
                if (!loggedInvalidPosThisRun)
                {
                    Debug.LogError($"Spline knot {i} の localPos が不正です: {localPos}", this);
                    loggedInvalidPosThisRun = true; // 毎フレームのログスパムを防止
                }
                continue;
            }

            BezierKnot knot = spline[i];
            knot.Position = localPos;
            // TangentIn/TangentOut/Rotation は構造変更時に初期化済みで不変のため、ここでは再設定しない

            // 通知なしで配列だけを更新(キャッシュ無効化・イベント発火はしない)
            spline.SetKnotNoNotify(i, knot);
            anyKnotUpdated = true;
            lastUpdatedIndex = i;
        }

        // このフレームで1件でも更新があれば、最後に1回だけ通知を発火してキャッシュを同期する
        // (SetKnot は通知ありの通常APIなので、既に書き込み済みの値を渡すだけで良い)
        if (anyKnotUpdated)
        {
            spline.SetKnot(lastUpdatedIndex, spline[lastUpdatedIndex]);
        }
    }
}