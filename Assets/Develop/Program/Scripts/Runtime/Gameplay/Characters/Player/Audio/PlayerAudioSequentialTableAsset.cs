using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerAudioActionIDに対応する音声データの登録情報です。
///
/// 1つのActionIDに対して、
/// ・連続再生用のAudioSequentialAsset
/// ・単発再生用のAudioOneShotAsset
/// をそれぞれ登録できます。
/// </summary>
[Serializable]
public sealed class PlayerAudioSequentialEntry
{
    [SerializeField, Header("Action ID")]
    private PlayerAudioActionID m_actionID;

    [SerializeField, Header("Sequential Audio")]
    private AudioSequentialAsset m_audioSequentialAsset;

    [SerializeField, Header("One Shot Audio")]
    private AudioOneShotAsset m_audioOneShotAsset;

    /// <summary>
    /// Playerの行動IDを取得します。
    /// </summary>
    public PlayerAudioActionID ActionID =>
        m_actionID;

    /// <summary>
    /// 行動IDに対応する連続再生用音声データを取得します。
    /// </summary>
    public AudioSequentialAsset AudioSequentialAsset =>
        m_audioSequentialAsset;

    /// <summary>
    /// 行動IDに対応する単発再生用音声データを取得します。
    /// </summary>
    public AudioOneShotAsset AudioOneShotAsset =>
        m_audioOneShotAsset;
}

/// <summary>
/// Player専用のActionIDと音声Assetの対応表です。
///
/// Stateは具体的なAudioIDやAudioClipを直接参照せず、
/// PlayerAudioActionIDを指定してこの対応表から
/// AudioSequentialAsset / AudioOneShotAssetを取得します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerAudioSequentialTable",
    menuName = "Game/Audio/Player/Audio Table")]
public sealed class PlayerAudioSequentialTableAsset
    : ScriptableObject
{
    [SerializeField, Header("Player Audio Table")]
    private List<PlayerAudioSequentialEntry> m_entries =
        new List<PlayerAudioSequentialEntry>();

    // ActionIDから連続再生用Assetを検索するランタイム辞書
    private readonly Dictionary<PlayerAudioActionID, AudioSequentialAsset>
        m_sequentialLookup =
            new Dictionary<PlayerAudioActionID, AudioSequentialAsset>();

    // ActionIDから単発再生用Assetを検索するランタイム辞書
    private readonly Dictionary<PlayerAudioActionID, AudioOneShotAsset>
        m_oneShotLookup =
            new Dictionary<PlayerAudioActionID, AudioOneShotAsset>();

    // --------------------------------------------------------
    // Public API
    // --------------------------------------------------------

    /// <summary>
    /// ActionIDに対応するAudioSequentialAssetを取得します。
    /// </summary>
    /// <param name="actionID">
    /// 取得するPlayerの行動ID。
    /// </param>
    /// <param name="audioSequentialAsset">
    /// 見つかった連続再生用音声データ。
    /// </param>
    /// <returns>
    /// true：対応するデータがあります。
    /// false：未登録、またはAssetが未設定です。
    /// </returns>
    public bool TryGetAudioSequentialAsset(
        PlayerAudioActionID actionID,
        out AudioSequentialAsset audioSequentialAsset)
    {
        EnsureLookup();

        return m_sequentialLookup.TryGetValue(
            actionID,
            out audioSequentialAsset) &&
            audioSequentialAsset != null;
    }

    /// <summary>
    /// ActionIDに対応するAudioOneShotAssetを取得します。
    /// </summary>
    /// <param name="actionID">
    /// 取得するPlayerの行動ID。
    /// </param>
    /// <param name="audioOneShotAsset">
    /// 見つかった単発再生用音声データ。
    /// </param>
    /// <returns>
    /// true：対応するデータがあります。
    /// false：未登録、またはAssetが未設定です。
    /// </returns>
    public bool TryGetAudioOneShotAsset(
        PlayerAudioActionID actionID,
        out AudioOneShotAsset audioOneShotAsset)
    {
        EnsureLookup();

        return m_oneShotLookup.TryGetValue(
            actionID,
            out audioOneShotAsset) &&
            audioOneShotAsset != null;
    }

    // --------------------------------------------------------
    // Unity Event
    // --------------------------------------------------------

    /// <summary>
    /// Asset有効化時に検索用辞書を構築します。
    /// </summary>
    private void OnEnable()
    {
        RebuildLookup();
    }

    /// <summary>
    /// Inspector変更時に検索用辞書を更新します。
    /// </summary>
    private void OnValidate()
    {
        RebuildLookup();
    }

    // --------------------------------------------------------
    // Lookup
    // --------------------------------------------------------

    /// <summary>
    /// 必要に応じて検索用辞書を構築します。
    /// </summary>
    private void EnsureLookup()
    {
        if (m_entries == null ||
            m_entries.Count == 0)
        {
            return;
        }

        if (m_sequentialLookup.Count == 0 &&
            m_oneShotLookup.Count == 0)
        {
            RebuildLookup();
        }
    }

    /// <summary>
    /// Inspectorの対応表から検索用辞書を再構築します。
    /// </summary>
    private void RebuildLookup()
    {
        m_sequentialLookup.Clear();
        m_oneShotLookup.Clear();

        if (m_entries == null)
        {
            return;
        }

        HashSet<PlayerAudioActionID> registeredIDs =
            new HashSet<PlayerAudioActionID>();

        foreach (PlayerAudioSequentialEntry entry in
                 m_entries)
        {
            if (entry == null ||
                entry.ActionID == PlayerAudioActionID.NONE)
            {
                continue;
            }

            // 1つのActionIDは1行にまとめます。
            // SequentialとOneShotを別行へ分けることは許可しません。
            if (!registeredIDs.Add(
                    entry.ActionID))
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[{nameof(PlayerAudioSequentialTableAsset)}] " +
                    $"{entry.ActionID} が重複登録されています。" +
                    "1つのActionIDは1行にまとめてください。",
                    this);
#endif
                continue;
            }

            if (entry.AudioSequentialAsset != null)
            {
                m_sequentialLookup.Add(
                    entry.ActionID,
                    entry.AudioSequentialAsset);
            }

            if (entry.AudioOneShotAsset != null)
            {
                m_oneShotLookup.Add(
                    entry.ActionID,
                    entry.AudioOneShotAsset);
            }
        }
    }
}
