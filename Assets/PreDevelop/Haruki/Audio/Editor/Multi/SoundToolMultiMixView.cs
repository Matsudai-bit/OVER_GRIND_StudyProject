#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2D 音源（BGM・プレイヤー音等）と 3D 音源を同時再生し、相互の音量バランスや
/// 合計出力のクリッピング・スペクトラムを確認しながら SoundDatabase を直接調整する画面です。
/// </summary>
public sealed class SoundToolMultiMixView
{
    // マスターアナライザーパネルの高さ
    private const float MASTER_ANALYZER_HEIGHT = 104.0f;

    // 音量の最小値
    private const float MIN_VOLUME = 0.0f;

    // 音量の最大値
    private const float MAX_VOLUME = 1.0f;

    // 最小距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離の余白
    private const float DISTANCE_MARGIN = 0.01f;

    // 2D バッジの色
    private static readonly Color BADGE_2D_COLOR = new Color(0.25f, 0.55f, 0.80f);

    // 3D バッジの色
    private static readonly Color BADGE_3D_COLOR = new Color(0.85f, 0.50f, 0.20f);

    // ミュートボタンのアクティブ色
    private static readonly Color MUTE_ACTIVE_COLOR = new Color(0.95f, 0.35f, 0.35f);

    // ソロボタンのアクティブ色
    private static readonly Color SOLO_ACTIVE_COLOR = new Color(0.95f, 0.78f, 0.20f);

    // クリッピング警告の強調色
    private static readonly Color CLIP_WARNING_COLOR = new Color(1.0f, 0.35f, 0.30f);

    // 再生中カードの薄い強調色
    private static readonly Color PLAYING_CARD_COLOR = new Color(0.25f, 0.70f, 0.40f, 0.10f);

    // グループ内パート取得用の一時リスト
    private readonly List<SoundDatabase.SoundData> m_tempGroupParts = new List<SoundDatabase.SoundData>();

    // I/L/O 個別スライダーを展開中のトラック参照
    private readonly HashSet<SoundToolMultiPreviewPlayer.MixTrack> m_expandedIloTracks = new HashSet<SoundToolMultiPreviewPlayer.MixTrack>();

    // 3D 距離減衰パラメーター編集を展開中のトラック参照
    private readonly HashSet<SoundToolMultiPreviewPlayer.MixTrack> m_expanded3DTracks = new HashSet<SoundToolMultiPreviewPlayer.MixTrack>();

    // トラック一覧のスクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    /// <summary>
    /// 同時ミックス画面を描画します。
    /// </summary>
    public void Draw()
    {
        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();
        if (database == null)
        {
            SoundToolGui.DrawMissingDatabase();
            return;
        }

        DrawMasterControlBar(database);
        DrawMasterAnalyzerSection();
        DrawGroupBalanceQuickTrimBar(database);
        DrawTrackList(database);
    }

    /// <summary>
    /// 上部のマスター同時再生・停止・トラック追加バーを描画します。
    /// </summary>
    private void DrawMasterControlBar(SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        bool isAnyPlaying = SoundToolMultiPreviewPlayer.HasAnyPlaying();
        if (GUILayout.Button(isAnyPlaying ? "▶ 最初から同時再生" : "▶ 全トラック同時再生", GUILayout.Height(28.0f), GUILayout.Width(155.0f)))
        {
            SoundToolMultiPreviewPlayer.PlayAll(database);
        }

        if (GUILayout.Button("■ 全停止", GUILayout.Height(28.0f), GUILayout.Width(76.0f)))
        {
            SoundToolMultiPreviewPlayer.StopAll();
        }

        // 2D / 3D サウンドを追加するポップアップボタン
        Rect addRect = GUILayoutUtility.GetRect(new GUIContent("＋ 2D/3D トラック追加"), GUI.skin.button, GUILayout.Height(28.0f), GUILayout.Width(160.0f));
        if (GUI.Button(addRect, "＋ 2D/3D トラック追加"))
        {
            PopupWindow.Show(addRect, new SoundToolMultiTrackPickerPopup(database, selectedId =>
            {
                SoundToolMultiPreviewPlayer.AddTrack(selectedId, database);
            }));
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Mute 全解除", EditorStyles.miniButton, GUILayout.Width(80.0f)))
        {
            SetAllTrackFlags(false, false);
        }

        if (GUILayout.Button("Solo 全解除", EditorStyles.miniButton, GUILayout.Width(76.0f)))
        {
            SetAllTrackFlags(true, false);
        }

        if (GUILayout.Button("全クリア", EditorStyles.miniButton, GUILayout.Width(64.0f)))
        {
            m_expandedIloTracks.Clear();
            m_expanded3DTracks.Clear();
            SoundToolMultiPreviewPlayer.ClearAllTracks();
        }

        EditorGUILayout.EndHorizontal();

        // シーンエミッター連動とセッション最大ピーク表示
        EditorGUILayout.BeginHorizontal();

        SoundToolMultiPreviewPlayer.SyncSceneEmitters = EditorGUILayout.ToggleLeft(
            "シーン上の 3D 試聴エミッターも同時再生・合計解析に連動する",
            SoundToolMultiPreviewPlayer.SyncSceneEmitters,
            GUILayout.Width(350.0f));

        GUILayout.FlexibleSpace();

        float maxPeakDb = SoundToolAudioAnalyzer.SessionMaxPeakDb;
        string maxPeakText = maxPeakDb <= SoundToolAudioAnalyzer.MIN_DISPLAY_DB + 0.1f ? "-inf dB" : $"{maxPeakDb:F1} dB";

        if (SoundToolAudioAnalyzer.HasClippedInSession)
        {
            SoundToolGui.DrawColoredMiniLabel($"合計出力 CLIP 検出! (最大 {maxPeakText})", CLIP_WARNING_COLOR);
        }
        else
        {
            GUILayout.Label($"同時再生 最大ピーク: {maxPeakText}", EditorStyles.miniBoldLabel);
        }

        if (GUILayout.Button("ピークリセット", EditorStyles.miniButton, GUILayout.Width(86.0f)))
        {
            SoundToolAudioAnalyzer.ResetSessionPeakAndClip();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 全トラックの合計出力を表示するマスター・アナライザーを描画します。
    /// </summary>
    private static void DrawMasterAnalyzerSection()
    {
        SoundToolAnalyzerGui.DrawAnalyzerPanel(MASTER_ANALYZER_HEIGHT);
    }

    /// <summary>
    /// 登録中トラックの「2D 一括」「3D 一括」「BGM 一括」のバランス比較トリムバーを描画します。
    /// </summary>
    private void DrawGroupBalanceQuickTrimBar(SoundDatabase database)
    {
        IReadOnlyList<SoundToolMultiPreviewPlayer.MixTrack> tracks = SoundToolMultiPreviewPlayer.Tracks;
        if (tracks.Count == 0)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("バランス一括トリム:", EditorStyles.miniBoldLabel, GUILayout.Width(112.0f));

        // 2D 音源まとめ増減
        GUILayout.Label("[2D全体]", EditorStyles.miniLabel, GUILayout.Width(50.0f));
        if (GUILayout.Button("-1dB", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            ScaleTracksByFilter(database, false, SoundToolGui.ConvertDecibelDeltaToMultiplier(-1.0f));
        }
        if (GUILayout.Button("+1dB", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            ScaleTracksByFilter(database, false, SoundToolGui.ConvertDecibelDeltaToMultiplier(1.0f));
        }

        GUILayout.Space(8.0f);

        // 3D 音源まとめ増減
        GUILayout.Label("[3D全体]", EditorStyles.miniLabel, GUILayout.Width(50.0f));
        if (GUILayout.Button("-1dB", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            ScaleTracksByFilter(database, true, SoundToolGui.ConvertDecibelDeltaToMultiplier(-1.0f));
        }
        if (GUILayout.Button("+1dB", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            ScaleTracksByFilter(database, true, SoundToolGui.ConvertDecibelDeltaToMultiplier(1.0f));
        }

        GUILayout.Space(8.0f);

        // BGM カテゴリーのみまとめ増減
        GUILayout.Label("[BGMのみ]", EditorStyles.miniLabel, GUILayout.Width(58.0f));
        if (GUILayout.Button("-1dB", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            ScaleTracksByCategory(database, SoundCategory.BGM, SoundToolGui.ConvertDecibelDeltaToMultiplier(-1.0f));
        }
        if (GUILayout.Button("+1dB", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            ScaleTracksByCategory(database, SoundCategory.BGM, SoundToolGui.ConvertDecibelDeltaToMultiplier(1.0f));
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label($"登録 {tracks.Count} トラック", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 登録されている同時再生トラックの一覧を描画します。
    /// </summary>
    private void DrawTrackList(SoundDatabase database)
    {
        IReadOnlyList<SoundToolMultiPreviewPlayer.MixTrack> tracks = SoundToolMultiPreviewPlayer.Tracks;

        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (tracks.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "上の「＋ 2D/3D トラック追加」から、同時に聴き比べたい BGM・プレイヤー音（2D）や 3D 音源を追加してください。\n"
                + "追加後、「▶ 全トラック同時再生」を押すと、Mute/Solo や 3D 距離シミュレーションを使いながら SoundDatabase の音量を直接バランス調整できます。",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        SoundToolMultiPreviewPlayer.MixTrack trackToRemove = null;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (DrawTrackCard(database, tracks[i]))
            {
                trackToRemove = tracks[i];
            }
        }

        if (trackToRemove != null)
        {
            m_expandedIloTracks.Remove(trackToRemove);
            m_expanded3DTracks.Remove(trackToRemove);
            SoundToolMultiPreviewPlayer.RemoveTrack(trackToRemove);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 1トラック分の操作カードを描画します。
    /// </summary>
    /// <returns>
    /// true：削除ボタンが押されました。
    /// false：削除は押されていません。
    /// </returns>
    private bool DrawTrackCard(SoundDatabase database, SoundToolMultiPreviewPlayer.MixTrack track)
    {
        bool removeRequested = false;
        SoundDatabase.SoundData data = database.GetSoundData(track.AudioId);
        if (data == null)
        {
            return true;
        }

        CollectSequenceParts(database, track.AudioId, m_tempGroupParts);
        bool isSequenceGroup = m_tempGroupParts.Count > 1;
        bool isPlaying = track.IsPlaying();

        Rect cardRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (Event.current.type == EventType.Repaint && isPlaying)
        {
            EditorGUI.DrawRect(cardRect, PLAYING_CARD_COLOR);
        }

        // 1段目：再生・Mute・Solo・2D/3Dバッジ・カテゴリー・タイトル・削除
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(isPlaying ? "■" : "▶", GUILayout.Width(26.0f), GUILayout.Height(20.0f)))
        {
            if (isPlaying)
            {
                SoundToolMultiPreviewPlayer.StopTrack(track);
            }
            else
            {
                SoundToolMultiPreviewPlayer.PlayTrack(track, database);
            }
        }

        // Mute ボタン
        Color prevBg = GUI.backgroundColor;
        if (track.IsMuted)
        {
            GUI.backgroundColor = MUTE_ACTIVE_COLOR;
        }
        if (GUILayout.Button("M", EditorStyles.miniButton, GUILayout.Width(24.0f), GUILayout.Height(20.0f)))
        {
            track.IsMuted = !track.IsMuted;
        }
        GUI.backgroundColor = prevBg;

        // Solo ボタン
        if (track.IsSolo)
        {
            GUI.backgroundColor = SOLO_ACTIVE_COLOR;
        }
        if (GUILayout.Button("S", EditorStyles.miniButton, GUILayout.Width(24.0f), GUILayout.Height(20.0f)))
        {
            track.IsSolo = !track.IsSolo;
        }
        GUI.backgroundColor = prevBg;

        // 2D / 3D バッジ
        Rect dimRect = GUILayoutUtility.GetRect(32.0f, 18.0f, GUILayout.Width(32.0f), GUILayout.Height(18.0f));
        EditorGUI.DrawRect(dimRect, data.m_is3D ? BADGE_3D_COLOR : BADGE_2D_COLOR);
        GUI.Label(dimRect, data.m_is3D ? " 3D" : " 2D", EditorStyles.whiteMiniLabel);

        SoundToolGui.DrawCategoryTag(data.m_category, 92.0f);

        SoundToolSequenceHelper.ParseSoundName(data.m_id.ToString(), out string baseName, out SoundPartType _);
        string title = isSequenceGroup ? $"{baseName}  [I/L/O]" : data.m_id.ToString();
        GUILayout.Label(title, EditorStyles.boldLabel, GUILayout.Width(190.0f));

        // 再生オプション（I/L/O・リピート）
        if (isSequenceGroup)
        {
            track.PlayAsSequence = GUILayout.Toggle(track.PlayAsSequence, "I→L→O", EditorStyles.miniButton, GUILayout.Width(54.0f));
            if (isPlaying && (track.Stage == SoundToolMultiPreviewPlayer.STAGE_INTRO || track.Stage == SoundToolMultiPreviewPlayer.STAGE_LOOP))
            {
                if (GUILayout.Button("→O", EditorStyles.miniButton, GUILayout.Width(32.0f)))
                {
                    SoundToolMultiPreviewPlayer.TriggerTrackOutro(track);
                }
            }
        }

        track.AutoRepeat = GUILayout.Toggle(track.AutoRepeat, "間隔リピート", EditorStyles.miniButton, GUILayout.Width(78.0f));
        if (track.AutoRepeat)
        {
            track.RepeatInterval = EditorGUILayout.Slider(track.RepeatInterval, 0.2f, 6.0f, GUILayout.Width(110.0f));
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22.0f)))
        {
            removeRequested = true;
        }

        EditorGUILayout.EndHorizontal();

        // 2段目：個別リアルタイムメーター ＋ SoundDatabase 直接音量スライダー ＋ dB微調整
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("出力", EditorStyles.miniLabel, GUILayout.Width(28.0f));
        SoundToolGui.DrawVolumeMeter(track.MeterLevel, 64.0f, 10.0f);

        float displayVolume = isSequenceGroup ? CalculateAverageVolume(m_tempGroupParts) : data.m_volume;
        GUILayout.Label(isSequenceGroup ? "DB一括音量" : "DB音量", EditorStyles.miniBoldLabel, GUILayout.Width(64.0f));

        float newVolume = EditorGUILayout.Slider(displayVolume, MIN_VOLUME, MAX_VOLUME, GUILayout.MinWidth(140.0f));
        if (!Mathf.Approximately(newVolume, displayVolume))
        {
            if (isSequenceGroup)
            {
                ApplyVolumeDeltaToParts(database, m_tempGroupParts, newVolume - displayVolume);
            }
            else
            {
                Undo.RecordObject(database, "MultiMix Edit Volume");
                data.m_volume = Mathf.Clamp(newVolume, MIN_VOLUME, MAX_VOLUME);
                SoundToolDatabaseProvider.MarkDirty(database);
            }
        }

        GUILayout.Label(SoundToolGui.GetDecibelText(displayVolume), EditorStyles.miniLabel, GUILayout.Width(56.0f));

        if (GUILayout.Button("-3dB", EditorStyles.miniButton, GUILayout.Width(38.0f)))
        {
            ScaleTargetData(database, data, isSequenceGroup, SoundToolGui.ConvertDecibelDeltaToMultiplier(-3.0f));
        }
        if (GUILayout.Button("-1dB", EditorStyles.miniButton, GUILayout.Width(38.0f)))
        {
            ScaleTargetData(database, data, isSequenceGroup, SoundToolGui.ConvertDecibelDeltaToMultiplier(-1.0f));
        }
        if (GUILayout.Button("+1dB", EditorStyles.miniButton, GUILayout.Width(38.0f)))
        {
            ScaleTargetData(database, data, isSequenceGroup, SoundToolGui.ConvertDecibelDeltaToMultiplier(1.0f));
        }
        if (GUILayout.Button("+3dB", EditorStyles.miniButton, GUILayout.Width(38.0f)))
        {
            ScaleTargetData(database, data, isSequenceGroup, SoundToolGui.ConvertDecibelDeltaToMultiplier(3.0f));
        }

        if (isSequenceGroup)
        {
            bool iloOpen = m_expandedIloTracks.Contains(track);
            if (GUILayout.Toggle(iloOpen, "I/L/O個別", EditorStyles.miniButton, GUILayout.Width(64.0f)) != iloOpen)
            {
                ToggleSetMember(m_expandedIloTracks, track);
            }
        }

        EditorGUILayout.EndHorizontal();

        // I/L/O 個別音量スライダー展開時
        if (isSequenceGroup && m_expandedIloTracks.Contains(track))
        {
            DrawExpandedIloVolumes(database);
        }

        // 3段目（3D音源の場合のみ）：仮想距離・パンシミュレーターと 3D 減衰 DB 編集
        if (data.m_is3D)
        {
            Draw3DSimulatorRow(database, track, data, isSequenceGroup);
        }

        EditorGUILayout.EndVertical();
        return removeRequested;
    }

    /// <summary>
    /// 展開された I/L/O 個別音量バランス行を描画します。
    /// </summary>
    private void DrawExpandedIloVolumes(SoundDatabase database)
    {
        EditorGUI.indentLevel++;
        for (int i = 0; i < m_tempGroupParts.Count; i++)
        {
            SoundDatabase.SoundData part = m_tempGroupParts[i];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24.0f);
            GUILayout.Label(part.m_id.ToString(), EditorStyles.miniLabel, GUILayout.Width(170.0f));
            float newPartVol = EditorGUILayout.Slider(part.m_volume, MIN_VOLUME, MAX_VOLUME);
            if (!Mathf.Approximately(newPartVol, part.m_volume))
            {
                Undo.RecordObject(database, "MultiMix Edit Part Volume");
                part.m_volume = newPartVol;
                SoundToolDatabaseProvider.MarkDirty(database);
            }
            GUILayout.Label(SoundToolGui.GetDecibelText(part.m_volume), EditorStyles.miniLabel, GUILayout.Width(56.0f));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 3D 音源トラックの仮想距離・左右パンシミュレーターおよび減衰設定を描画します。
    /// </summary>
    private void Draw3DSimulatorRow(
        SoundDatabase database,
        SoundToolMultiPreviewPlayer.MixTrack track,
        SoundDatabase.SoundData data,
        bool isSequenceGroup)
    {
        float minDist = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
        float maxDist = Mathf.Max(minDist + DISTANCE_MARGIN, data.m_maxDistance);
        float sliderMax = Mathf.Max(maxDist * 1.2f, 10.0f);
        float attenuation = SoundToolMultiPreviewPlayer.EvaluateAttenuation(data, track.VirtualDistance);

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("3D仮想距離", EditorStyles.miniBoldLabel, GUILayout.Width(64.0f));
        track.VirtualDistance = EditorGUILayout.Slider(track.VirtualDistance, 0.0f, sliderMax, GUILayout.MinWidth(120.0f));
        GUILayout.Label($"m (減衰:{attenuation * 100.0f:F0}%)", EditorStyles.miniLabel, GUILayout.Width(86.0f));

        // 距離プリセット（近・中・遠）
        if (GUILayout.Button("近(Min)", EditorStyles.miniButton, GUILayout.Width(48.0f)))
        {
            track.VirtualDistance = minDist;
        }
        if (GUILayout.Button("中", EditorStyles.miniButton, GUILayout.Width(28.0f)))
        {
            track.VirtualDistance = (minDist + maxDist) * 0.5f;
        }
        if (GUILayout.Button("遠(Max)", EditorStyles.miniButton, GUILayout.Width(52.0f)))
        {
            track.VirtualDistance = maxDist;
        }

        GUILayout.Space(8.0f);
        GUILayout.Label("パン", EditorStyles.miniLabel, GUILayout.Width(28.0f));
        track.VirtualPan = EditorGUILayout.Slider(track.VirtualPan, -1.0f, 1.0f, GUILayout.Width(100.0f));

        if (GUILayout.Button("中央", EditorStyles.miniButton, GUILayout.Width(38.0f)))
        {
            track.VirtualPan = 0.0f;
        }

        bool is3DOpen = m_expanded3DTracks.Contains(track);
        if (GUILayout.Toggle(is3DOpen, "減衰DB編集", EditorStyles.miniButton, GUILayout.Width(72.0f)) != is3DOpen)
        {
            ToggleSetMember(m_expanded3DTracks, track);
        }

        EditorGUILayout.EndHorizontal();

        // 3D 減衰パラメーターの直接 DB 編集
        if (m_expanded3DTracks.Contains(track))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24.0f);

            AudioRolloffMode newRolloff = (AudioRolloffMode)EditorGUILayout.EnumPopup("減衰モード", data.m_rolloffMode, GUILayout.Width(220.0f));
            if (newRolloff != data.m_rolloffMode)
            {
                ApplyActionToTarget(database, data, isSequenceGroup, "MultiMix Edit Rolloff", item => item.m_rolloffMode = newRolloff);
            }

            float newMin = Mathf.Max(MIN_DISTANCE_LIMIT, EditorGUILayout.FloatField("Min距離", data.m_minDistance, GUILayout.Width(160.0f)));
            if (!Mathf.Approximately(newMin, data.m_minDistance))
            {
                ApplyActionToTarget(database, data, isSequenceGroup, "MultiMix Edit MinDistance", item =>
                {
                    item.m_minDistance = newMin;
                    item.m_maxDistance = Mathf.Max(newMin + DISTANCE_MARGIN, item.m_maxDistance);
                });
            }

            float newMax = Mathf.Max(data.m_minDistance + DISTANCE_MARGIN, EditorGUILayout.FloatField("Max距離", data.m_maxDistance, GUILayout.Width(160.0f)));
            if (!Mathf.Approximately(newMax, data.m_maxDistance))
            {
                ApplyActionToTarget(database, data, isSequenceGroup, "MultiMix Edit MaxDistance", item => item.m_maxDistance = newMax);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 2D または 3D に該当する登録トラックの音量をまとめて倍率調整します。
    /// </summary>
    private void ScaleTracksByFilter(SoundDatabase database, bool targetIs3D, float multiplier)
    {
        IReadOnlyList<SoundToolMultiPreviewPlayer.MixTrack> tracks = SoundToolMultiPreviewPlayer.Tracks;
        HashSet<AudioID> processedIds = new HashSet<AudioID>();

        Undo.RecordObject(database, targetIs3D ? "Scale 3D Tracks Volume" : "Scale 2D Tracks Volume");

        for (int i = 0; i < tracks.Count; i++)
        {
            SoundDatabase.SoundData data = database.GetSoundData(tracks[i].AudioId);
            if (data == null || data.m_is3D != targetIs3D)
            {
                continue;
            }

            CollectSequenceParts(database, tracks[i].AudioId, m_tempGroupParts);
            if (m_tempGroupParts.Count > 1)
            {
                for (int p = 0; p < m_tempGroupParts.Count; p++)
                {
                    if (processedIds.Add(m_tempGroupParts[p].m_id))
                    {
                        m_tempGroupParts[p].m_volume = Mathf.Clamp(m_tempGroupParts[p].m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
                    }
                }
            }
            else if (processedIds.Add(data.m_id))
            {
                data.m_volume = Mathf.Clamp(data.m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 指定カテゴリーに該当する登録トラックの音量をまとめて倍率調整します。
    /// </summary>
    private void ScaleTracksByCategory(SoundDatabase database, SoundCategory category, float multiplier)
    {
        IReadOnlyList<SoundToolMultiPreviewPlayer.MixTrack> tracks = SoundToolMultiPreviewPlayer.Tracks;
        HashSet<AudioID> processedIds = new HashSet<AudioID>();

        Undo.RecordObject(database, "Scale Category Tracks Volume");

        for (int i = 0; i < tracks.Count; i++)
        {
            SoundDatabase.SoundData data = database.GetSoundData(tracks[i].AudioId);
            if (data == null || data.m_category != category)
            {
                continue;
            }

            CollectSequenceParts(database, tracks[i].AudioId, m_tempGroupParts);
            if (m_tempGroupParts.Count > 1)
            {
                for (int p = 0; p < m_tempGroupParts.Count; p++)
                {
                    if (processedIds.Add(m_tempGroupParts[p].m_id))
                    {
                        m_tempGroupParts[p].m_volume = Mathf.Clamp(m_tempGroupParts[p].m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
                    }
                }
            }
            else if (processedIds.Add(data.m_id))
            {
                data.m_volume = Mathf.Clamp(data.m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 全トラックの Mute または Solo フラグを一括変更します。
    /// </summary>
    private static void SetAllTrackFlags(bool isSoloTarget, bool value)
    {
        IReadOnlyList<SoundToolMultiPreviewPlayer.MixTrack> tracks = SoundToolMultiPreviewPlayer.Tracks;
        for (int i = 0; i < tracks.Count; i++)
        {
            if (isSoloTarget)
            {
                tracks[i].IsSolo = value;
            }
            else
            {
                tracks[i].IsMuted = value;
            }
        }
    }

    /// <summary>
    /// 単体または I/L/O グループ音量を倍率で調整します。
    /// </summary>
    private void ScaleTargetData(SoundDatabase database, SoundDatabase.SoundData data, bool isSequenceGroup, float multiplier)
    {
        ApplyActionToTarget(database, data, isSequenceGroup, "MultiMix Scale Volume", item =>
        {
            item.m_volume = Mathf.Clamp(item.m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
        });
    }

    /// <summary>
    /// 単体または I/L/O グループ全体へ編集アクションを適用します。
    /// </summary>
    private void ApplyActionToTarget(
        SoundDatabase database,
        SoundDatabase.SoundData fallback,
        bool isSequenceGroup,
        string undoLabel,
        Action<SoundDatabase.SoundData> action)
    {
        Undo.RecordObject(database, undoLabel);
        if (isSequenceGroup)
        {
            for (int i = 0; i < m_tempGroupParts.Count; i++)
            {
                action(m_tempGroupParts[i]);
            }
        }
        else if (fallback != null)
        {
            action(fallback);
        }
        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// パート群へ音量差分を加算します。
    /// </summary>
    private static void ApplyVolumeDeltaToParts(SoundDatabase database, List<SoundDatabase.SoundData> parts, float delta)
    {
        Undo.RecordObject(database, "MultiMix Adjust Group Volume");
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].m_volume = Mathf.Clamp(parts[i].m_volume + delta, MIN_VOLUME, MAX_VOLUME);
        }
        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// パート群の平均音量を計算します。
    /// </summary>
    private static float CalculateAverageVolume(List<SoundDatabase.SoundData> parts)
    {
        if (parts.Count == 0)
        {
            return 0.0f;
        }

        float sum = 0.0f;
        for (int i = 0; i < parts.Count; i++)
        {
            sum += parts[i].m_volume;
        }

        return sum / parts.Count;
    }

    /// <summary>
    /// I/L/O グループに属する全パートを収集します。
    /// </summary>
    private static void CollectSequenceParts(SoundDatabase database, AudioID audioId, List<SoundDatabase.SoundData> destination)
    {
        destination.Clear();
        SoundToolSequenceHelper.ParseSoundName(audioId.ToString(), out string baseName, out SoundPartType partType);
        if (partType == SoundPartType.PART_SINGLE)
        {
            return;
        }

        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData item = database.m_soundList[i];
            if (item == null || item.m_id == AudioID.None)
            {
                continue;
            }

            SoundToolSequenceHelper.ParseSoundName(item.m_id.ToString(), out string itemBase, out SoundPartType itemPart);
            if (itemPart != SoundPartType.PART_SINGLE && string.Equals(baseName, itemBase, StringComparison.OrdinalIgnoreCase))
            {
                destination.Add(item);
            }
        }
    }

    /// <summary>
    /// HashSet 内の要素をトグルします。
    /// </summary>
    private static void ToggleSetMember(HashSet<SoundToolMultiPreviewPlayer.MixTrack> set, SoundToolMultiPreviewPlayer.MixTrack item)
    {
        if (!set.Remove(item))
        {
            set.Add(item);
        }
    }
}

#endif