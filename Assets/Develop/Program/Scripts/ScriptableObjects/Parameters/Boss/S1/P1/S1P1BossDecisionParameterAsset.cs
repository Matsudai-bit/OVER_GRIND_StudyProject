using System;
using UnityEngine;

/// <summary>
/// S1P1ボスの行動選択パラメータを管理します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P1BossDecisionParameterAsset",
    menuName = "Game/Parameters/Boss/S1/P1/BossDecisionParameter")]
public sealed class S1P1BossDecisionParameterAsset : ScriptableObject
{
    /// <summary>
    /// 踏みつけ攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class StompParameters
    {
        // 足元と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 20.0f;

        // 高確率判定に必要な滞在時間
        [SerializeField, Header("必要滞在時間"), Min(0.0f)]
        private float m_requiredStayDuration = 4.0f;

        // 条件成立時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_nearProbability = 0.8f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.1f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 7.0f;

        public float Distance => m_distance;
        public float RequiredStayDuration => m_requiredStayDuration;
        public float NearProbability => m_nearProbability;
        public float DefaultProbability => m_defaultProbability;
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// ミサイル攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class MissileParameters
    {
        // 遠距離と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 80.0f;

        // 遠距離時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_farProbability = 0.7f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.0f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 10.0f;

        public float Distance => m_distance;
        public float FarProbability => m_farProbability;
        public float DefaultProbability => m_defaultProbability;
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// 排熱攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class HeatExhaustParameters
    {
        // 近距離と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 40.0f;

        // 近距離時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_nearProbability = 0.6f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.0f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 10.0f;

        public float Distance => m_distance;
        public float NearProbability => m_nearProbability;
        public float DefaultProbability => m_defaultProbability;
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// 方向転換の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class TurnParameters
    {
        // 遠距離と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 80.0f;

        // 遠距離時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_farProbability = 0.2f;

        // 通常時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.1f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 5.0f;

        public float Distance => m_distance;
        public float FarProbability => m_farProbability;
        public float DefaultProbability => m_defaultProbability;
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// 歩行の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class WalkParameters
    {
        // 歩行の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_probability = 0.4f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 3.0f;

        public float Probability => m_probability;
        public float CoolTime => m_coolTime;
    }

    [SerializeField, Header("踏みつけ")]
    private StompParameters m_stomp = new();

    [SerializeField, Header("ミサイル")]
    private MissileParameters m_missile = new();

    [SerializeField, Header("排熱攻撃")]
    private HeatExhaustParameters m_heatExhaust = new();

    [SerializeField, Header("方向転換")]
    private TurnParameters m_turn = new();

    [SerializeField, Header("歩行")]
    private WalkParameters m_walk = new();

    /// <summary>
    /// 踏みつけ攻撃の行動選択パラメータを取得します。
    /// </summary>
    public StompParameters Stomp => m_stomp;

    /// <summary>
    /// ミサイル攻撃の行動選択パラメータを取得します。
    /// </summary>
    public MissileParameters Missile => m_missile;

    /// <summary>
    /// 排熱攻撃の行動選択パラメータを取得します。
    /// </summary>
    public HeatExhaustParameters HeatExhaust => m_heatExhaust;

    /// <summary>
    /// 方向転換の行動選択パラメータを取得します。
    /// </summary>
    public TurnParameters Turn => m_turn;

    /// <summary>
    /// 歩行の行動選択パラメータを取得します。
    /// </summary>
    public WalkParameters Walk => m_walk;
}
