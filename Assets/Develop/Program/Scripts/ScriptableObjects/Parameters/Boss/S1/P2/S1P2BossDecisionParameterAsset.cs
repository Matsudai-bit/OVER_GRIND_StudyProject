using System;
using UnityEngine;

/// <summary>
/// S1P2ボスの行動選択パラメータを管理します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P2BossDecisionParameterAsset",
    menuName = "Game/Parameters/Boss/S1/P2/BossDecisionParameter")]
public sealed class S1P2BossDecisionParameterAsset : ScriptableObject
{
    /// <summary>
    /// うなじ噴射の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class NapeJetParameters
    {
        // 結合部への攻撃ヒット後から噴射までの待機時間
        [SerializeField, Header("発動待機時間"), Min(0.0f)]
        private float m_triggerDelay = 3.0f;

        // 発動条件成立時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_probability = 1.0f;

        /// <summary>
        /// 発動待機時間を取得します。
        /// </summary>
        public float TriggerDelay => m_triggerDelay;

        /// <summary>
        /// 選択確率を取得します。
        /// </summary>
        public float Probability => m_probability;
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

        // Playerが背中に乗っている場合の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_onBackProbability = 0.0f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.2f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 10.0f;

        /// <summary>
        /// 遠距離判定距離を取得します。
        /// </summary>
        public float Distance => m_distance;

        /// <summary>
        /// 遠距離時の選択確率を取得します。
        /// </summary>
        public float FarProbability => m_farProbability;

        /// <summary>
        /// Playerが背中に乗っている場合の選択確率を取得します。
        /// </summary>
        public float OnBackProbability => m_onBackProbability;

        /// <summary>
        /// 通常時の選択確率を取得します。
        /// </summary>
        public float DefaultProbability => m_defaultProbability;

        /// <summary>
        /// クールタイムを取得します。
        /// </summary>
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
        private float m_distance = 80.0f;

        // Playerが背中に乗っている場合の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_onBackProbability = 0.1f;

        // 近距離時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_nearProbability = 0.4f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.15f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 5.0f;

        /// <summary>
        /// 近距離判定距離を取得します。
        /// </summary>
        public float Distance => m_distance;

        /// <summary>
        /// Playerが背中に乗っている場合の選択確率を取得します。
        /// </summary>
        public float OnBackProbability => m_onBackProbability;

        /// <summary>
        /// 近距離時の選択確率を取得します。
        /// </summary>
        public float NearProbability => m_nearProbability;

        /// <summary>
        /// 通常時の選択確率を取得します。
        /// </summary>
        public float DefaultProbability => m_defaultProbability;

        /// <summary>
        /// クールタイムを取得します。
        /// </summary>
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// 突進攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class ChargeParameters
    {
        // 突進攻撃の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_probability = 0.3f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 5.0f;

        /// <summary>
        /// 選択確率を取得します。
        /// </summary>
        public float Probability => m_probability;

        /// <summary>
        /// クールタイムを取得します。
        /// </summary>
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class TailSlamParameters
    {
        // 尻尾周辺と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 0.0f;

        // 尻尾周辺にいる場合の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_nearProbability = 0.4f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.1f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 2.0f;

        /// <summary>
        /// 尻尾周辺判定距離を取得します。
        /// </summary>
        public float Distance => m_distance;

        /// <summary>
        /// 尻尾周辺にいる場合の選択確率を取得します。
        /// </summary>
        public float NearProbability => m_nearProbability;

        /// <summary>
        /// 通常時の選択確率を取得します。
        /// </summary>
        public float DefaultProbability => m_defaultProbability;

        /// <summary>
        /// クールタイムを取得します。
        /// </summary>
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// 移動の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class MoveParameters
    {
        // 移動の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_probability = 0.9f;

        /// <summary>
        /// 選択確率を取得します。
        /// </summary>
        public float Probability => m_probability;
    }

    [SerializeField, Header("うなじ噴射")]
    private NapeJetParameters m_napeJet = new();

    [SerializeField, Header("ミサイル")]
    private MissileParameters m_missile = new();

    [SerializeField, Header("排熱攻撃")]
    private HeatExhaustParameters m_heatExhaust = new();

    [SerializeField, Header("突進")]
    private ChargeParameters m_charge = new();

    [SerializeField, Header("尻尾叩きつけ")]
    private TailSlamParameters m_tailSlam = new();

    [SerializeField, Header("移動")]
    private MoveParameters m_move = new();

    /// <summary>
    /// うなじ噴射の行動選択パラメータを取得します。
    /// </summary>
    public NapeJetParameters NapeJet => m_napeJet;

    /// <summary>
    /// ミサイル攻撃の行動選択パラメータを取得します。
    /// </summary>
    public MissileParameters Missile => m_missile;

    /// <summary>
    /// 排熱攻撃の行動選択パラメータを取得します。
    /// </summary>
    public HeatExhaustParameters HeatExhaust => m_heatExhaust;

    /// <summary>
    /// 突進攻撃の行動選択パラメータを取得します。
    /// </summary>
    public ChargeParameters Charge => m_charge;

    /// <summary>
    /// 尻尾叩きつけ攻撃の行動選択パラメータを取得します。
    /// </summary>
    public TailSlamParameters TailSlam => m_tailSlam;

    /// <summary>
    /// 移動の行動選択パラメータを取得します。
    /// </summary>
    public MoveParameters Move => m_move;
}
