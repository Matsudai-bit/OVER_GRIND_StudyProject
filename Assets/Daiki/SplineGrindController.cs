using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Dynamic;

public class SplineGrindController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseGrindSpeed = 15f; // 基本速度
    [SerializeField] private float maxGrindSpeed = 40f;  // 最高速度

    // 現在の状態管理
    public bool IsGrinding { get; private set; } = false;

    private SplineRailInfo currentRail;
    private float currentT = 0f;       // 0.0 ~ 1.0 の進捗率
    private float splineLength = 0f;   // レールの総延長（メートル）
    private float currentSpeed = 0f;   // 現在のプレイヤーのリアルタイム速度
    private int directionFactor = 1;   // 1 = 順方向, -1 = 逆方向

    void Update()
    {
        if (IsGrinding)
        {
            ExecuteGrind();
        }
    }

    /// <summary>
    /// レールへの搭乗処理（トリガー衝突時などに外部から呼ぶ）
    /// </summary>
    public void StartGrind(SplineRailInfo rail)
    {
        currentRail = rail;
        splineLength = rail.Container.CalculateLength();

        // 1. プレイヤーの現在地から、レール上の最も近いノード（T値）を割り出す
        Vector3 localPlayerPos = rail.Container.transform.InverseTransformPoint(transform.position);
        SplineUtility.GetNearestPoint(rail.Container.Splines[0], localPlayerPos, out float3 _, out float nearestT);
        currentT = nearestT;

        // 2. 進入方向の判定（順方向か、逆方向か）
        Vector3 railDirection = Vector3.Normalize(rail.Container.EvaluateTangent(currentT));
        Vector3 playerDirection = transform.forward; // プレイヤーの進行方向

        // 内積を計算し、プレイヤーとレールの向きが逆なら逆走モード(-1)にする
        directionFactor = Vector3.Dot(railDirection, playerDirection) >= 0 ? 1 : -1;

        // 3. 初期速度の設定（現在の速度を引き継ぐか、基本速度にするか）
        currentSpeed = baseGrindSpeed * rail.SpeedMultiplier;
        IsGrinding = true;

        // ※ここで元の物理挙動（CharacterControllerやRigidbody）を無効化する
    }

    /// <summary>
    /// 毎フレームの移動計算
    /// </summary>
    private void ExecuteGrind()
    {
        if (currentRail == null) return;

        // 【ここがポイント！】速度ベースでT値を進める（逆走時はマイナスされる）
        float deltaT = (currentSpeed / splineLength) * Time.deltaTime;
        currentT += deltaT * directionFactor;

        // 【ソニックフィール】傾き（G値）による加減速をここに書く
        // 例: Vector3 tangent = currentRail.Container.EvaluateTangent(currentT);
        // tangent.y がマイナス（下り坂）なら currentSpeed を上げる、など

        // 終点または始点に達したら離脱
        if (currentT > 1.0f || currentT < 0.0f)
        {
            ExitGrind(isEndOfRail: true);
            return;
        }

        // 座標と回転の更新
        Vector3 nextPosition = currentRail.Container.EvaluatePosition(currentT);
        Vector3 nextTangent = currentRail.Container.EvaluateTangent(currentT);

        transform.position = nextPosition;
        if (nextTangent != Vector3.zero)
        {
            // 逆走時は回転も180度反転させる
            transform.rotation = Quaternion.LookRotation(nextTangent * directionFactor);
        }

        // プレイヤーのジャンプ入力で途中離脱
        if (Input.GetButtonDown("Jump"))
        {
            ExitGrind(isEndOfRail: false);
        }
    }

    /// <summary>
    /// レールからの離脱処理
    /// </summary>
    private void ExitGrind(bool isEndOfRail)
    {
        IsGrinding = false;

        // 離脱時のベクトルの計算（レールの向き × 最終速度）
        Vector3 exitVelocity = currentRail.Container.EvaluateTangent(currentT) * currentSpeed * directionFactor;

        if (!isEndOfRail)
        {
            // 途中ジャンプなら、上方向へのベクトルの足し算などを行う
            exitVelocity += Vector3.up * 10f;
        }

        // ※ここでプレイヤーの元の物理挙動を有効化し、exitVelocity を Rigidbody.velocity 等にブチ込む！

        currentRail = null;
        Debug.Log("グラインド終了！慣性速度: " + exitVelocity.magnitude);
    }
}