using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;

/// <summary>
/// キャラクターに紐付いたTimeline演出を再生・停止します。
/// ボス・プレイヤーなどのゲームプレイ制御には依存しません。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationTimelineController : MonoBehaviour
{
    /// <summary>
    /// Cueと再生するDirectorの対応情報です。
    /// </summary>
    [Serializable]
    private sealed class TimelineEntry
    {
        // イベントから指定するCue
        [SerializeField, Header("演出識別")]
        private AnimationTimelineCue m_cue;

        // 再生するTimeline
        [SerializeField, Header("Timeline")]
        private PlayableDirector m_director;

        // このTimelineが専有するVolume。停止時にWeightを0に戻す
        [SerializeField, Header("停止時にリセットする専用Volume")]
        private List<Volume> m_ownedVolumes = new();

        // 実行状態（UnityのDirectorがHold/停止した状態と区別）
        private bool m_isRunning;

        /// <summary>識別用Cueを取得します。</summary>
        public AnimationTimelineCue Cue => m_cue;

        /// <summary>再生するDirectorを取得します。</summary>
        public PlayableDirector Director => m_director;

        /// <summary>管理上の再生状態を取得します。</summary>
        public bool IsRunning => m_isRunning;

        /// <summary>再生状態を設定します。</summary>
        public void SetRunning(bool isRunning)
        {
            m_isRunning = isRunning;
        }

        /// <summary>専用Volumeを無効なWeightに戻します。</summary>
        public void ResetOwnedVolumes()
        {
            if (m_ownedVolumes == null)
            {
                return;
            }

            foreach (Volume volume in m_ownedVolumes)
            {
                if (volume != null)
                {
                    volume.weight = 0.0f;
                }
            }
        }
    }

    // 同一Cueへ複数のDirectorを登録可能
    [SerializeField, Header("Timeline演出一覧")]
    private List<TimelineEntry> m_entries = new();

    // Cueから再生対象を検索するためのテーブル
    private readonly Dictionary<AnimationTimelineCue, List<TimelineEntry>>
        m_entryMap = new();

    // Directorの停止イベントから対応するエントリを検索するためのテーブル
    private readonly Dictionary<PlayableDirector, TimelineEntry>
        m_directorMap = new();

    /// <summary>
    /// Timeline演出が終了したときに通知します。
    /// 第2引数は明示的な停止だった場合にtrueです。
    /// </summary>
    public event Action<AnimationTimelineCue, bool> SequenceEnded;

    /// <summary>
    /// コンポーネント有効時に対応表を構築して停止イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        BuildMap();

        foreach (PlayableDirector director in m_directorMap.Keys)
        {
            director.stopped += HandleDirectorStopped;
        }
    }

    /// <summary>
    /// コンポーネント無効化時に演出をすべて停止します。
    /// </summary>
    private void OnDisable()
    {
        StopAll();

        foreach (PlayableDirector director in m_directorMap.Keys)
        {
            if (director != null)
            {
                director.stopped -= HandleDirectorStopped;
            }
        }

        m_entryMap.Clear();
        m_directorMap.Clear();
    }

    /// <summary>
    /// 指定Cueに対応するTimelineを再生します。
    /// すでに再生中のDirectorは再始動しません。
    /// </summary>
    /// <param name="cue">再生する演出の識別。</param>
    /// <returns>
    /// true：1つ以上のTimelineを開始しました。
    /// false：開始するTimelineがありませんでした。
    /// </returns>
    public bool TryPlay(AnimationTimelineCue cue)
    {
        if (cue == null || !m_entryMap.TryGetValue(cue, out List<TimelineEntry> entries))
        {
            return false;
        }

        bool hasStarted = false;

        foreach (TimelineEntry entry in entries)
        {
            if (entry.IsRunning || entry.Director == null)
            {
                continue;
            }

            // 前回の残留状態を除去してTimelineの先頭から再生
            entry.ResetOwnedVolumes();
            entry.Director.time = 0.0;
            entry.SetRunning(true);
            entry.Director.Play();
            hasStarted = true;
        }

        return hasStarted;
    }

    /// <summary>
    /// 指定Cueに対応する再生中のTimelineを停止します。
    /// </summary>
    /// <param name="cue">停止する演出の識別。</param>
    /// <returns>
    /// true：1つ以上のTimelineを停止しました。
    /// false：停止対象がありませんでした。
    /// </returns>
    public bool StopCue(AnimationTimelineCue cue)
    {
        if (cue == null || !m_entryMap.TryGetValue(cue, out List<TimelineEntry> entries))
        {
            return false;
        }

        bool hasStopped = false;

        foreach (TimelineEntry entry in entries)
        {
            hasStopped |= StopEntry(entry);
        }

        return hasStopped;
    }

    /// <summary>
    /// 現在再生中のTimelineをすべて停止します。
    /// </summary>
    public void StopAll()
    {
        if (m_entries == null)
        {
            return;
        }

        foreach (TimelineEntry entry in m_entries)
        {
            if (entry != null)
            {
                StopEntry(entry);
            }
        }
    }

    /// <summary>
    /// 指定Cueが1つ以上再生中か調べます。
    /// </summary>
    /// <param name="cue">確認する演出の識別。</param>
    /// <returns>
    /// true：再生中のTimelineがあります。
    /// false：再生中のTimelineはありません。
    /// </returns>
    public bool IsPlaying(AnimationTimelineCue cue)
    {
        if (cue == null || !m_entryMap.TryGetValue(cue, out List<TimelineEntry> entries))
        {
            return false;
        }

        foreach (TimelineEntry entry in entries)
        {
            if (entry.IsRunning)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Directorの停止通知を再生終了として処理します。
    /// 明示的な停止では、先に状態を解除して二重通知を防ぎます。
    /// </summary>
    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (director == null ||
            !m_directorMap.TryGetValue(director, out TimelineEntry entry) ||
            !entry.IsRunning)
        {
            return;
        }

        entry.SetRunning(false);
        entry.ResetOwnedVolumes();
        SequenceEnded?.Invoke(entry.Cue, false);
    }

    /// <summary>
    /// Directorを明示停止し、専用Volumeを初期化します。
    /// </summary>
    private bool StopEntry(TimelineEntry entry)
    {
        if (!entry.IsRunning)
        {
            return false;
        }

        // Stop()がstoppedイベントを同期発火しても二重通知しない
        entry.SetRunning(false);

        if (entry.Director != null)
        {
            entry.Director.Stop();
        }

        entry.ResetOwnedVolumes();
        SequenceEnded?.Invoke(entry.Cue, true);
        return true;
    }

    /// <summary>
    /// Inspectorで設定したCueとDirectorの対応表を作ります。
    /// </summary>
    private void BuildMap()
    {
        m_entryMap.Clear();
        m_directorMap.Clear();

        if (m_entries == null)
        {
            return;
        }

        foreach (TimelineEntry entry in m_entries)
        {
            if (entry == null || entry.Cue == null || entry.Director == null)
            {
                Debug.LogWarning("[AnimationTimelineController] CueまたはDirectorが未設定です。", this);
                continue;
            }

            if (m_directorMap.ContainsKey(entry.Director))
            {
                Debug.LogError("[AnimationTimelineController] 同じDirectorを複数のEntryには登録できません。", this);
                continue;
            }

            if (!m_entryMap.TryGetValue(entry.Cue, out List<TimelineEntry> entries))
            {
                entries = new List<TimelineEntry>();
                m_entryMap.Add(entry.Cue, entries);
            }

            entries.Add(entry);
            m_directorMap.Add(entry.Director, entry);
            entry.SetRunning(false);
            entry.ResetOwnedVolumes();
        }
    }
}
