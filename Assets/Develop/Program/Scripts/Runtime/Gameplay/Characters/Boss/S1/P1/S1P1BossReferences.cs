using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ2固有の参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P1BossReferences : BossPhaseParameterProvider

{
    // 左足の座標
    [SerializeField, Header("左足の座標")]
    private Transform m_leftLegTransform;
    // 右足の座標
    [SerializeField, Header("右足の座標")]
    private Transform m_rightLegTransform;
    // プレイヤーの座標
    [SerializeField, Header("プレイヤーの座標")]
    private Transform m_playerTransform;
    // プレイヤーの占有範囲
    [SerializeField, Header("プレイヤーの占有範囲")]
    private BossNavMeshFootprint m_bossNavMeshFootprint;

    // ミサイルの発射地点
    [SerializeField, Header("ミサイルリファレンス")]
    private S1P1BossMissileReferences m_missileReferences;


    // 左足の座標
    public Transform LeftLegTransform => m_leftLegTransform;
    // 右足の座標
    public Transform RightLegTransform => m_rightLegTransform;
    public Transform PlayerTransform => m_playerTransform;
    public BossNavMeshFootprint BossNavMeshFootprint => m_bossNavMeshFootprint;
    public S1P1BossMissileReferences MissileReferences => m_missileReferences;

    public override BossPhaseParameters CreatePhaseParameters()
    {

            return BossPhaseParameters.Empty;
    
    }
}