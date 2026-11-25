#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// サウンドのパート種別を表します。
/// </summary>
public enum SoundPartType
{
    PART_SINGLE,
    PART_INTRO,
    PART_LOOP,
    PART_OUTRO
}

/// <summary>
/// サフィックス（_I / _L / _O）によるサウンドのグループ化と解析を担当します。
/// </summary>
public static class SoundToolSequenceHelper
{
    // 開始パートのサフィックス
    private const string SUFFIX_INTRO = "_I";

    // ループパートのサフィックス
    private const string SUFFIX_LOOP = "_L";

    // 終了パートのサフィックス
    private const string SUFFIX_OUTRO = "_O";

    // サフィックスの最小文字数（ベース名1文字以上 + サフィックス2文字）
    private const int MIN_SUFFIXED_LENGTH = 3;

    // サフィックスの長さ
    private const int SUFFIX_LENGTH = 2;

    /// <summary>
    /// 同一ベース名を持つサウンド（単体または _I / _L / _O）のグループ情報です。
    /// </summary>
    public sealed class SoundGroup
    {
        // グループの識別キー
        private readonly string m_groupKey;

        // グループのベース名
        private readonly string m_baseName;

        // I/L/O のシーケンスグループか
        private readonly bool m_isSequenceGroup;

        // 単体音源データ
        private SoundDatabase.SoundData m_singleData;

        // 開始音源データ（_I）
        private SoundDatabase.SoundData m_introData;

        // ループ音源データ（_L）
        private SoundDatabase.SoundData m_loopData;

        // 終了音源データ（_O）
        private SoundDatabase.SoundData m_outroData;

        /// <summary>
        /// グループの識別キーを取得します。
        /// </summary>
        public string GroupKey => m_groupKey;

        /// <summary>
        /// グループのベース名を取得します。
        /// </summary>
        public string BaseName => m_baseName;

        /// <summary>
        /// I / L / O を扱うシーケンスグループかを取得します。
        /// </summary>
        public bool IsSequenceGroup => m_isSequenceGroup;

        /// <summary>
        /// 単体音源データを取得または設定します。
        /// </summary>
        public SoundDatabase.SoundData SingleData
        {
            get => m_singleData;
            set => m_singleData = value;
        }

        /// <summary>
        /// 開始音源データ（_I）を取得または設定します。
        /// </summary>
        public SoundDatabase.SoundData IntroData
        {
            get => m_introData;
            set => m_introData = value;
        }

        /// <summary>
        /// ループ音源データ（_L）を取得または設定します。
        /// </summary>
        public SoundDatabase.SoundData LoopData
        {
            get => m_loopData;
            set => m_loopData = value;
        }

        /// <summary>
        /// 終了音源データ（_O）を取得または設定します。
        /// </summary>
        public SoundDatabase.SoundData OutroData
        {
            get => m_outroData;
            set => m_outroData = value;
        }

        /// <summary>
        /// サウンドグループを生成します。
        /// </summary>
        /// <param name="groupKey">グループ識別キー。</param>
        /// <param name="baseName">ベース名。</param>
        /// <param name="isSequenceGroup">シーケンスグループか。</param>
        public SoundGroup(string groupKey, string baseName, bool isSequenceGroup)
        {
            m_groupKey = groupKey;
            m_baseName = baseName;
            m_isSequenceGroup = isSequenceGroup;
        }

        /// <summary>
        /// グループ内の代表となるサウンドデータを取得します（Loop → Intro → Outro → Single の優先順）。
        /// </summary>
        /// <returns>代表データ。無ければ null。</returns>
        public SoundDatabase.SoundData GetPrimaryData()
        {
            if (m_loopData != null)
            {
                return m_loopData;
            }

            if (m_introData != null)
            {
                return m_introData;
            }

            if (m_outroData != null)
            {
                return m_outroData;
            }

            return m_singleData;
        }

        /// <summary>
        /// 指定パートのサウンドデータを取得します。
        /// </summary>
        /// <param name="partType">パート種別。</param>
        /// <returns>該当データ。無ければ null。</returns>
        public SoundDatabase.SoundData GetPartData(SoundPartType partType)
        {
            switch (partType)
            {
                case SoundPartType.PART_INTRO:
                    return m_introData;
                case SoundPartType.PART_LOOP:
                    return m_loopData;
                case SoundPartType.PART_OUTRO:
                    return m_outroData;
                default:
                    return m_singleData;
            }
        }

        /// <summary>
        /// グループ内に含まれる有効なサウンドデータを再生順でリストへ格納します。
        /// </summary>
        /// <param name="destination">格納先のリスト。</param>
        public void CollectOrderedParts(List<SoundDatabase.SoundData> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();

            if (!m_isSequenceGroup)
            {
                if (m_singleData != null)
                {
                    destination.Add(m_singleData);
                }

                return;
            }

            if (m_introData != null)
            {
                destination.Add(m_introData);
            }

            if (m_loopData != null)
            {
                destination.Add(m_loopData);
            }

            if (m_outroData != null)
            {
                destination.Add(m_outroData);
            }
        }

        /// <summary>
        /// グループ内パートの平均音量を取得します。
        /// </summary>
        /// <returns>平均音量。</returns>
        public float GetAverageVolume()
        {
            float total = 0.0f;
            int count = 0;

            if (m_singleData != null)
            {
                total += m_singleData.m_volume;
                count++;
            }

            if (m_introData != null)
            {
                total += m_introData.m_volume;
                count++;
            }

            if (m_loopData != null)
            {
                total += m_loopData.m_volume;
                count++;
            }

            if (m_outroData != null)
            {
                total += m_outroData.m_volume;
                count++;
            }

            return count > 0 ? total / count : 0.0f;
        }

        /// <summary>
        /// グループ内で音量のばらつきがあるかを確認します。
        /// </summary>
        /// <returns>
        /// true：パート間で音量差があります。
        /// false：すべて同じ音量、または1パートのみです。
        /// </returns>
        public bool HasVolumeDifference()
        {
            float minVolume = float.MaxValue;
            float maxVolume = float.MinValue;
            int count = 0;

            CheckMinMax(m_introData, ref minVolume, ref maxVolume, ref count);
            CheckMinMax(m_loopData, ref minVolume, ref maxVolume, ref count);
            CheckMinMax(m_outroData, ref minVolume, ref maxVolume, ref count);

            if (count <= 1)
            {
                return false;
            }

            return !Mathf.Approximately(minVolume, maxVolume);
        }

        /// <summary>
        /// 最小値と最大値を更新します。
        /// </summary>
        private static void CheckMinMax(SoundDatabase.SoundData data, ref float minValue, ref float maxValue, ref int count)
        {
            if (data == null)
            {
                return;
            }

            minValue = Mathf.Min(minValue, data.m_volume);
            maxValue = Mathf.Max(maxValue, data.m_volume);
            count++;
        }
    }

    /// <summary>
    /// AudioID 名からベース名とパート種別（_I / _L / _O）を解析します。
    /// </summary>
    /// <param name="idName">解析する AudioID 名。</param>
    /// <param name="baseName">解析されたベース名。</param>
    /// <param name="partType">解析されたパート種別。</param>
    public static void ParseSoundName(string idName, out string baseName, out SoundPartType partType)
    {
        if (string.IsNullOrEmpty(idName) || idName.Length < MIN_SUFFIXED_LENGTH)
        {
            baseName = idName ?? "";
            partType = SoundPartType.PART_SINGLE;
            return;
        }

        if (idName.EndsWith(SUFFIX_INTRO, StringComparison.OrdinalIgnoreCase))
        {
            baseName = idName.Substring(0, idName.Length - SUFFIX_LENGTH);
            partType = SoundPartType.PART_INTRO;
            return;
        }

        if (idName.EndsWith(SUFFIX_LOOP, StringComparison.OrdinalIgnoreCase))
        {
            baseName = idName.Substring(0, idName.Length - SUFFIX_LENGTH);
            partType = SoundPartType.PART_LOOP;
            return;
        }

        if (idName.EndsWith(SUFFIX_OUTRO, StringComparison.OrdinalIgnoreCase))
        {
            baseName = idName.Substring(0, idName.Length - SUFFIX_LENGTH);
            partType = SoundPartType.PART_OUTRO;
            return;
        }

        baseName = idName;
        partType = SoundPartType.PART_SINGLE;
    }

    /// <summary>
    /// サウンドリストからグループ一覧を構築します。
    /// </summary>
    /// <param name="soundList">対象のサウンドリスト。</param>
    /// <param name="destination">出力先のグループリスト。</param>
    public static void BuildGroups(IReadOnlyList<SoundDatabase.SoundData> soundList, List<SoundGroup> destination)
    {
        if (destination == null)
        {
            return;
        }

        destination.Clear();

        if (soundList == null)
        {
            return;
        }

        Dictionary<string, SoundGroup> groupMap = new Dictionary<string, SoundGroup>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < soundList.Count; i++)
        {
            SoundDatabase.SoundData data = soundList[i];

            if (data == null || data.m_id == AudioID.None)
            {
                continue;
            }

            string idName = data.m_id.ToString();
            ParseSoundName(idName, out string baseName, out SoundPartType partType);

            bool isSequence = partType != SoundPartType.PART_SINGLE;
            string groupKey = isSequence ? $"SEQ:{baseName}" : $"SGL:{idName}";

            if (!groupMap.TryGetValue(groupKey, out SoundGroup group))
            {
                group = new SoundGroup(groupKey, baseName, isSequence);
                groupMap.Add(groupKey, group);
                destination.Add(group);
            }

            AssignPartToGroup(group, data, partType);
        }
    }

    /// <summary>
    /// グループ内の該当パートへデータを割り当てます。
    /// </summary>
    private static void AssignPartToGroup(SoundGroup group, SoundDatabase.SoundData data, SoundPartType partType)
    {
        switch (partType)
        {
            case SoundPartType.PART_INTRO:
                group.IntroData = data;
                break;
            case SoundPartType.PART_LOOP:
                group.LoopData = data;
                break;
            case SoundPartType.PART_OUTRO:
                group.OutroData = data;
                break;
            default:
                group.SingleData = data;
                break;
        }
    }
}

#endif