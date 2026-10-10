using System;
using UnityEngine;

/// <summary>
/// S1P3ボスの行動選択パラメータを管理します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P3BossDecisionParameterAsset",
    menuName = "Game/Parameters/Boss/S1/P3/BossDecisionParameter")]
public sealed class S1P3BossDecisionParameterAsset : ScriptableObject
{
    /// <summary>
    /// 噛み潰し攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class BiteParameters
    {
        // Playerが口内にいる場合の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_inMouthProbability = 1.0f;

        // Playerが口内にいない場合の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.0f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 0.0f;

        /// <summary>
        /// Playerが口内にいる場合の選択確率を取得します。
        /// </summary>
        public float InMouthProbability => m_inMouthProbability;

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
    /// 連続突進攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class ChargeParameters
    {
        // 前回の突進後に必要な他行動の選択回数
        [SerializeField, Header("発動条件"), Min(0)]
        private int m_requiredOtherActionCount = 5;

        // 条件成立時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_readyProbability = 1.0f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.0f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 0.0f;

        /// <summary>
        /// 前回の突進後に必要な他行動の選択回数を取得します。
        /// </summary>
        public int RequiredOtherActionCount =>
            m_requiredOtherActionCount;

        /// <summary>
        /// 条件成立時の選択確率を取得します。
        /// </summary>
        public float ReadyProbability => m_readyProbability;

        /// <summary>
        /// 条件不成立時の選択確率を取得します。
        /// </summary>
        public float DefaultProbability => m_defaultProbability;

        /// <summary>
        /// クールタイムを取得します。
        /// </summary>
        public float CoolTime => m_coolTime;
    }

    /// <summary>
    /// ドレッド攻撃の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class DreadAttackParameters
    {
        // 近距離と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 70.0f;

        // 近距離時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_nearProbability = 0.8f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.4f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 0.0f;

        /// <summary>
        /// 近距離判定距離を取得します。
        /// </summary>
        public float Distance => m_distance;

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
    /// エネルギー砲の行動選択パラメータです。
    /// </summary>
    [Serializable]
    public sealed class EnergyCannonParameters
    {
        // 遠距離と判定する距離
        [SerializeField, Header("判定距離"), Min(0.0f)]
        private float m_distance = 70.0f;

        // 遠距離時の選択確率
        [SerializeField, Header("選択確率"), Range(0.0f, 1.0f)]
        private float m_farProbability = 1.0f;

        // 条件不成立時の選択確率
        [SerializeField, Range(0.0f, 1.0f)]
        private float m_defaultProbability = 0.0f;

        // 行動終了後のクールタイム
        [SerializeField, Header("クールタイム"), Min(0.0f)]
        private float m_coolTime = 0.0f;

        /// <summary>
        /// 遠距離判定距離を取得します。
        /// </summary>
        public float Distance => m_distance;

        /// <summary>
        /// 遠距離時の選択確率を取得します。
        /// </summary>
        public float FarProbability => m_farProbability;

        /// <summary>
        /// 通常時の選択確率を取得します。
        /// </summary>
        public float DefaultProbability => m_defaultProbability;

        /// <summary>
        /// クールタイムを取得します。
        /// </summary>
        public float CoolTime => m_coolTime;
    }

    [SerializeField, Header("噛み潰し")]
    private BiteParameters m_bite = new();

    [SerializeField, Header("突進")]
    private ChargeParameters m_charge = new();

    [SerializeField, Header("ドレッド攻撃")]
    private DreadAttackParameters m_dreadAttack = new();

    [SerializeField, Header("エネルギー砲")]
    private EnergyCannonParameters m_energyCannon = new();

    /// <summary>
    /// 噛み潰し攻撃の行動選択パラメータを取得します。
    /// </summary>
    public BiteParameters Bite => m_bite;

    /// <summary>
    /// 連続突進攻撃の行動選択パラメータを取得します。
    /// </summary>
    public ChargeParameters Charge => m_charge;

    /// <summary>
    /// ドレッド攻撃の行動選択パラメータを取得します。
    /// </summary>
    public DreadAttackParameters DreadAttack => m_dreadAttack;

    /// <summary>
    /// エネルギー砲の行動選択パラメータを取得します。
    /// </summary>
    public EnergyCannonParameters EnergyCannon => m_energyCannon;
}
