#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SoundScenePreviewEmitter のカスタムインスペクターおよび Scene ビュー操作 UI を担当します。
/// すべてのパラメーター変更は直接 SoundDatabase に記録されます。
/// </summary>
[CustomEditor(typeof(SoundScenePreviewEmitter))]
public sealed class SoundScenePreviewEmitterEditor : Editor
{
    // 音量の最小値
    private const float MIN_VOLUME = 0.0f;

    // 音量の最大値
    private const float MAX_VOLUME = 1.0f;

    // ピッチの最小値
    private const float MIN_PITCH = 0.1f;

    // ピッチの最大値
    private const float MAX_PITCH = 3.0f;

    // 最小距離の下限
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離の余白
    private const float DISTANCE_MARGIN = 0.01f;

    // 末尾ジャンプの秒数
    private const float NEAR_END_SECONDS = 3.0f;

    // シーン配置時のカメラ前方距離
    private const float SPAWN_FORWARD_DISTANCE = 5.0f;

    // 選択ボタンの高さ
    private const float SELECT_BUTTON_HEIGHT = 26.0f;

    // 前後送りボタンの幅
    private const float STEP_BUTTON_WIDTH = 26.0f;

    // 最小距離ハンドルの色
    private static readonly Color MIN_HANDLE_COLOR = new Color(0.20f, 0.90f, 0.55f, 1.0f);

    // 最大距離ハンドルの色
    private static readonly Color MAX_HANDLE_COLOR = new Color(1.00f, 0.65f, 0.20f, 1.0f);

    // グループ内パート取得用リスト
    private readonly List<SoundDatabase.SoundData> m_groupParts = new List<SoundDatabase.SoundData>();

    // 他エミッターのグループパート取得用一時リスト
    private readonly List<SoundDatabase.SoundData> m_tempEmitterParts = new List<SoundDatabase.SoundData>();

    // インスペクター内クイックリストを展開しているか
    private bool m_showInlineList = false;

    // インスペクター内クイック検索文字列
    private string m_inlineSearchText = "";

    // インスペクター内 3D のみフィルタ
    private bool m_inlineOnly3D = false;

    // インスペクター内 I/L/O まとめ表示
    private bool m_inlineGroupSequence = true;

    // インスペクター内クイックリストのスクロール位置
    private Vector2 m_inlineScrollPosition = Vector2.zero;

    // 長い名前でもはみ出さない選択ボタン用スタイル
    private GUIStyle m_clippedButtonStyle;

    /// <summary>
    /// シーン上に試聴用エミッターオブジェクトを作成して選択します。
    /// </summary>
    [MenuItem("GameObject/Audio/Sound データベース試聴エミッター", false, 10)]
    [MenuItem("Tools/Audio/シーン上に 3D 試聴エミッターを配置")]
    public static void CreateScenePreviewEmitter()
    {
        GameObject emitterObject = new GameObject("SoundPreviewEmitter");
        Undo.RegisterCreatedObjectUndo(emitterObject, "Create SoundPreviewEmitter");

        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            Transform camTransform = SceneView.lastActiveSceneView.camera.transform;
            emitterObject.transform.position = camTransform.position + (camTransform.forward * SPAWN_FORWARD_DISTANCE);
        }

        emitterObject.AddComponent<SoundScenePreviewEmitter>();
        Selection.activeGameObject = emitterObject;
    }

    private void OnEnable()
    {
        EditorApplication.update += RepaintWhilePlaying;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintWhilePlaying;
    }

    /// <summary>
    /// インスペクターを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        SoundScenePreviewEmitter emitter = (SoundScenePreviewEmitter)target;
        SoundDatabase database = emitter.Database;

        if (database == null)
        {
            SoundToolGui.DrawMissingDatabase();
            return;
        }

        CollectSequenceGroupParts(database, emitter.SelectedAudioId, m_groupParts);

        DrawSoundSelectionSection(emitter, database);
        DrawTransportAndDistanceSection(emitter);
        DrawDatabaseDirectEditSection(emitter, database);
        DrawAllSceneEmittersSection(database);
    }

    /// <summary>
    /// Scene ビュー上の 3D 距離ハンドルとフローティング操作パネルを描画します。
    /// </summary>
    private void OnSceneGUI()
    {
        SoundScenePreviewEmitter emitter = (SoundScenePreviewEmitter)target;
        SoundDatabase database = emitter.Database;
        if (database == null)
        {
            return;
        }

        SoundDatabase.SoundData data = database.GetSoundData(emitter.SelectedAudioId);
        if (data == null)
        {
            return;
        }

        CollectSequenceGroupParts(database, emitter.SelectedAudioId, m_groupParts);

        // 3D音源の場合は Scene ビュー上で直接 Min/Max 距離リングをドラッグ編集できる
        if (data.m_is3D)
        {
            DrawSceneDistanceHandles(emitter, database, data);
        }

        DrawSceneOverlayWindow(emitter, database, data);
    }

    /// <summary>
    /// 1. 再生するサウンドの選択欄を描画します（長い名前でもレイアウトが崩れない固定配置）。
    /// </summary>
    private void DrawSoundSelectionSection(SoundScenePreviewEmitter emitter, SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. 試聴するサウンドの選択", EditorStyles.boldLabel);

        SoundDatabase.SoundData currentData = database.GetSoundData(emitter.SelectedAudioId);
        string buttonTitle = BuildSelectionButtonTitle(currentData);

        EditorGUILayout.BeginHorizontal();

        // 前後のサウンドへ切り替えるボタンを左側に固定幅で配置する
        if (GUILayout.Button("◀", GUILayout.Width(STEP_BUTTON_WIDTH), GUILayout.Height(SELECT_BUTTON_HEIGHT)))
        {
            StepAdjacentSound(emitter, database, -1);
        }

        if (GUILayout.Button("▶", GUILayout.Width(STEP_BUTTON_WIDTH), GUILayout.Height(SELECT_BUTTON_HEIGHT)))
        {
            StepAdjacentSound(emitter, database, 1);
        }

        // テキスト長に依存せず残り幅いっぱいに収まるよう GUIContent.none で領域を取得する
        Rect buttonRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUI.skin.button,
            GUILayout.ExpandWidth(true),
            GUILayout.MinWidth(60.0f),
            GUILayout.Height(SELECT_BUTTON_HEIGHT));

        if (GUI.Button(buttonRect, new GUIContent(buttonTitle, buttonTitle), GetClippedButtonStyle()))
        {
            PopupWindow.Show(buttonRect, new SoundSceneSelectionPopup(emitter, selectedId =>
            {
                Undo.RecordObject(emitter, "Select Emitter Sound");
                emitter.SelectedAudioId = selectedId;
                EditorUtility.SetDirty(emitter);
                Repaint();
            }));
        }

        EditorGUILayout.EndHorizontal();

        // 選択中サウンドの補足情報（Clip名やI/L/O構成を別行で表示して見やすくする）
        if (currentData != null)
        {
            string clipInfo = currentData.m_clip != null ? currentData.m_clip.name : "Clip未設定";
            string groupInfo = m_groupParts.Count > 1 ? $"[I/L/O グループ: {m_groupParts.Count} パート]  " : "";
            EditorGUILayout.LabelField($"{groupInfo}Clip: {clipInfo}", EditorStyles.miniLabel);
        }

        // インスペクター内展開型クイックリスト
        m_showInlineList = EditorGUILayout.Foldout(m_showInlineList, "インスペクター内でリストを展開して選ぶ", true);
        if (m_showInlineList)
        {
            DrawInlineQuickList(emitter, database);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 選択ボタンに表示するタイトル文字列を作成します。
    /// </summary>
    private string BuildSelectionButtonTitle(SoundDatabase.SoundData currentData)
    {
        if (currentData == null)
        {
            return "▼ クリックしてサウンドを選択...";
        }

        if (m_groupParts.Count > 1)
        {
            SoundToolSequenceHelper.ParseSoundName(currentData.m_id.ToString(), out string baseName, out SoundPartType _);
            return $"▼ {baseName}  [I/L/O まとめ]";
        }

        return $"▼ {currentData.m_id}";
    }

    /// <summary>
    /// 長い文字列をはみ出さずにクリップするボタンスタイルを取得します。
    /// </summary>
    private GUIStyle GetClippedButtonStyle()
    {
        if (m_clippedButtonStyle == null)
        {
            m_clippedButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(8, 8, 2, 2)
            };
        }

        return m_clippedButtonStyle;
    }

    /// <summary>
    /// インスペクター内に埋め込まれた簡易フィルタ付きサウンド選択リストを描画します。
    /// </summary>
    private void DrawInlineQuickList(SoundScenePreviewEmitter emitter, SoundDatabase database)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_inlineSearchText = EditorGUILayout.TextField(m_inlineSearchText, EditorStyles.toolbarSearchField);
        m_inlineGroupSequence = GUILayout.Toggle(m_inlineGroupSequence, "I/L/Oまとめ", EditorStyles.miniButton, GUILayout.Width(76.0f));
        m_inlineOnly3D = GUILayout.Toggle(m_inlineOnly3D, "3Dのみ", EditorStyles.miniButton, GUILayout.Width(54.0f));
        EditorGUILayout.EndHorizontal();

        m_inlineScrollPosition = EditorGUILayout.BeginScrollView(m_inlineScrollPosition, EditorStyles.helpBox, GUILayout.Height(150.0f));
        string keyword = (m_inlineSearchText ?? "").Trim();
        HashSet<string> shownBaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        SoundToolSequenceHelper.ParseSoundName(emitter.SelectedAudioId.ToString(), out string currentBaseName, out SoundPartType currentPart);

        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData item = database.m_soundList[i];
            if (item == null || item.m_id == AudioID.None)
            {
                continue;
            }

            if (m_inlineOnly3D && !item.m_is3D)
            {
                continue;
            }

            string idText = item.m_id.ToString();
            SoundToolSequenceHelper.ParseSoundName(idText, out string baseName, out SoundPartType partType);

            if (!string.IsNullOrEmpty(keyword)
                && idText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0
                && baseName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            bool isSequencePart = partType != SoundPartType.PART_SINGLE;
            if (m_inlineGroupSequence && isSequencePart && !shownBaseNames.Add(baseName))
            {
                continue;
            }

            bool isSelected = (m_inlineGroupSequence && isSequencePart && currentPart != SoundPartType.PART_SINGLE)
                ? string.Equals(baseName, currentBaseName, StringComparison.OrdinalIgnoreCase)
                : (item.m_id == emitter.SelectedAudioId);

            string rowLabel = (m_inlineGroupSequence && isSequencePart)
                ? $"{baseName} [I/L/O]  ({SoundToolGui.GetDecibelText(item.m_volume)})"
                : $"{idText}  ({SoundToolGui.GetDecibelText(item.m_volume)})";

            if (GUILayout.Toggle(isSelected, rowLabel, EditorStyles.miniButton) && !isSelected)
            {
                Undo.RecordObject(emitter, "Select Emitter Sound");
                emitter.SelectedAudioId = item.m_id;
                emitter.PlayPreview();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 2. 再生コントロールとリスナー距離減衰メーターを描画します。
    /// </summary>
    private void DrawTransportAndDistanceSection(SoundScenePreviewEmitter emitter)
    {
        SoundDatabase.SoundData data = emitter.GetActiveSoundData();
        bool isPlaying = emitter.IsPlaying();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. シーン再生 & 3D 距離減衰モニター", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "■ 停止" : "▶ シーン上で再生", GUILayout.Height(28.0f)))
        {
            if (isPlaying)
            {
                emitter.StopPreview();
            }
            else
            {
                emitter.PlayPreview();
            }
        }

        if (isPlaying)
        {
            if (GUILayout.Button($"末尾-{NEAR_END_SECONDS:0}秒", GUILayout.Width(72.0f), GUILayout.Height(28.0f)))
            {
                emitter.JumpNearEnd(NEAR_END_SECONDS);
            }

            if (emitter.SequenceStage == 2 || emitter.SequenceStage == 3)
            {
                if (GUILayout.Button("→ O (終了へ)", GUILayout.Width(90.0f), GUILayout.Height(28.0f)))
                {
                    emitter.TriggerOutro();
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (m_groupParts.Count > 1)
        {
            emitter.PlayAsSequence = EditorGUILayout.ToggleLeft("I→L→O 一連再生", emitter.PlayAsSequence, GUILayout.Width(120.0f));
            emitter.AutoOutro = EditorGUILayout.ToggleLeft("ループ1周で自動O", emitter.AutoOutro, GUILayout.Width(130.0f));
        }

        emitter.AutoRepeat = EditorGUILayout.ToggleLeft("間隔リピート", emitter.AutoRepeat, GUILayout.Width(95.0f));
        if (emitter.AutoRepeat)
        {
            emitter.RepeatInterval = EditorGUILayout.Slider(emitter.RepeatInterval, 0.2f, 8.0f);
        }

        EditorGUILayout.EndHorizontal();

        emitter.UseSceneCameraAsListener = EditorGUILayout.ToggleLeft(
            "Scene カメラ位置をリスナーにする（カメラ移動で3D減衰と左右パンを確認）",
            emitter.UseSceneCameraAsListener);

        if (data != null)
        {
            float distance = emitter.GetDistanceToListener();
            float attenuation = emitter.EvaluateAttenuation(data, distance);
            float effectiveVolume = data.m_volume * attenuation;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"リスナー距離: {distance:F2} m", EditorStyles.miniBoldLabel, GUILayout.Width(130.0f));
            EditorGUILayout.LabelField($"距離減衰率: {attenuation * 100.0f:F0} %", EditorStyles.miniBoldLabel, GUILayout.Width(110.0f));
            EditorGUILayout.LabelField($"実効音量: {effectiveVolume:F2} ({SoundToolGui.GetDecibelText(effectiveVolume)})", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            SoundToolGui.DrawVolumeMeter(effectiveVolume, EditorGUIUtility.currentViewWidth - 45.0f, 10.0f);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 3. SoundDatabase の数値を直接編集するセクションを描画します（I/L/O まとめ＆個別調整対応）。
    /// </summary>
    private void DrawDatabaseDirectEditSection(SoundScenePreviewEmitter emitter, SoundDatabase database)
    {
        SoundDatabase.SoundData data = database.GetSoundData(emitter.SelectedAudioId);
        if (data == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("3. SoundDatabase 直接調整（変更は即 DB に反映）", EditorStyles.boldLabel);

        // I/L/O が複数ある場合は「一括連動スライダー」と「パート別スライダー」の両方を表示する
        if (m_groupParts.Count > 1)
        {
            DrawGroupVolumeBalanceControls(emitter, database);
            SoundToolGui.DrawSeparator();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            float newVolume = EditorGUILayout.Slider("DB 音量", data.m_volume, MIN_VOLUME, MAX_VOLUME);
            if (!Mathf.Approximately(newVolume, data.m_volume))
            {
                Undo.RecordObject(database, "Edit SoundDatabase Volume");
                data.m_volume = newVolume;
                SoundToolDatabaseProvider.MarkDirty(database);
            }
            GUILayout.Label(SoundToolGui.GetDecibelText(data.m_volume), EditorStyles.miniLabel, GUILayout.Width(58.0f));
            EditorGUILayout.EndHorizontal();
        }

        // dB ステップ微調整ボタン（I/L/O の場合はグループ全体のバランスを保ったまま増減）
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("dB 一括微調整:", GUILayout.Width(86.0f));
        if (GUILayout.Button("-3dB"))
        {
            ScaleTargetVolumes(database, data, SoundToolGui.ConvertDecibelDeltaToMultiplier(-3.0f));
        }
        if (GUILayout.Button("-1dB"))
        {
            ScaleTargetVolumes(database, data, SoundToolGui.ConvertDecibelDeltaToMultiplier(-1.0f));
        }
        if (GUILayout.Button("+1dB"))
        {
            ScaleTargetVolumes(database, data, SoundToolGui.ConvertDecibelDeltaToMultiplier(1.0f));
        }
        if (GUILayout.Button("+3dB"))
        {
            ScaleTargetVolumes(database, data, SoundToolGui.ConvertDecibelDeltaToMultiplier(3.0f));
        }
        EditorGUILayout.EndHorizontal();

        // ピッチ・ループ・3D 設定
        float newPitch = EditorGUILayout.Slider("DB ピッチ", data.m_pitch, MIN_PITCH, MAX_PITCH);
        if (!Mathf.Approximately(newPitch, data.m_pitch))
        {
            ApplyToTargetParts(database, data, "Edit SoundDatabase Pitch", item => item.m_pitch = newPitch);
        }

        EditorGUILayout.BeginHorizontal();
        bool newLoop = EditorGUILayout.Toggle("ループ", data.m_loop);
        if (newLoop != data.m_loop)
        {
            Undo.RecordObject(database, "Edit SoundDatabase Loop");
            data.m_loop = newLoop;
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        bool newIs3D = EditorGUILayout.Toggle("3D 音源", data.m_is3D);
        if (newIs3D != data.m_is3D)
        {
            ApplyToTargetParts(database, data, "Edit SoundDatabase 3D", item => item.m_is3D = newIs3D);
        }
        EditorGUILayout.EndHorizontal();

        // 3D 減衰パラメーター
        if (data.m_is3D)
        {
            AudioRolloffMode newRolloff = (AudioRolloffMode)EditorGUILayout.EnumPopup("距離減衰モード", data.m_rolloffMode);
            if (newRolloff != data.m_rolloffMode)
            {
                ApplyToTargetParts(database, data, "Edit SoundDatabase Rolloff", item => item.m_rolloffMode = newRolloff);
            }

            float newMin = Mathf.Max(MIN_DISTANCE_LIMIT, EditorGUILayout.FloatField("最小距離 (Min)", data.m_minDistance));
            if (!Mathf.Approximately(newMin, data.m_minDistance))
            {
                ApplyToTargetParts(database, data, "Edit SoundDatabase MinDistance", item =>
                {
                    item.m_minDistance = newMin;
                    item.m_maxDistance = Mathf.Max(newMin + DISTANCE_MARGIN, item.m_maxDistance);
                });
            }

            float newMax = Mathf.Max(data.m_minDistance + DISTANCE_MARGIN, EditorGUILayout.FloatField("最大距離 (Max)", data.m_maxDistance));
            if (!Mathf.Approximately(newMax, data.m_maxDistance))
            {
                ApplyToTargetParts(database, data, "Edit SoundDatabase MaxDistance", item => item.m_maxDistance = newMax);
            }

            if (data.m_rolloffMode == AudioRolloffMode.Custom)
            {
                AnimationCurve sourceCurve = data.m_customRolloffCurve ?? new AnimationCurve();
                EditorGUI.BeginChangeCheck();
                AnimationCurve editedCurve = EditorGUILayout.CurveField("カスタム減衰カーブ", new AnimationCurve(sourceCurve.keys));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyToTargetParts(database, data, "Edit SoundDatabase Curve", item =>
                    {
                        item.m_customRolloffCurve = new AnimationCurve(editedCurve.keys);
                    });
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// I / L / O グループの一括連動スライダーとパート別個別スライダーを描画します。
    /// </summary>
    private void DrawGroupVolumeBalanceControls(SoundScenePreviewEmitter emitter, SoundDatabase database)
    {
        float avgVolume = CalculateAverageVolume(m_groupParts);

        // 1. グループ全体の一括音量スライダー（パート間の音量差を保ったまま連動増減）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("I/L/O 一括音量 (連動)", EditorStyles.boldLabel, GUILayout.Width(135.0f));
        float newAvgVolume = EditorGUILayout.Slider(avgVolume, MIN_VOLUME, MAX_VOLUME);
        if (!Mathf.Approximately(newAvgVolume, avgVolume))
        {
            ApplyVolumeDeltaToParts(database, m_groupParts, newAvgVolume - avgVolume);
        }

        GUILayout.Label(SoundToolGui.GetDecibelText(avgVolume), EditorStyles.miniBoldLabel, GUILayout.Width(58.0f));

        if (GUILayout.Button("揃える", EditorStyles.miniButton, GUILayout.Width(48.0f)))
        {
            Undo.RecordObject(database, "Unify Group Volumes");
            for (int i = 0; i < m_groupParts.Count; i++)
            {
                m_groupParts[i].m_volume = avgVolume;
            }
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2.0f);
        EditorGUILayout.LabelField("パート別の個別音量バランス（▶でそのパートを単体試聴）", EditorStyles.miniLabel);

        // 2. 各パート（_I / _L / _O）の個別スライダー
        for (int i = 0; i < m_groupParts.Count; i++)
        {
            SoundDatabase.SoundData part = m_groupParts[i];
            SoundToolSequenceHelper.ParseSoundName(part.m_id.ToString(), out string _, out SoundPartType partType);

            EditorGUILayout.BeginHorizontal();

            // そのパートだけをエミッターから単体試聴するボタン
            if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(24.0f)))
            {
                emitter.SelectedAudioId = part.m_id;
                emitter.PlayAsSequence = false;
                emitter.PlayPreview();
            }

            SoundToolGui.DrawPartBadge(GetPartBadgeText(partType), partType, true, emitter.GetActiveSoundData() == part && emitter.IsPlaying(), 26.0f);
            GUILayout.Label(part.m_id.ToString(), EditorStyles.miniLabel, GUILayout.Width(115.0f));

            float newPartVol = EditorGUILayout.Slider(part.m_volume, MIN_VOLUME, MAX_VOLUME);
            if (!Mathf.Approximately(newPartVol, part.m_volume))
            {
                Undo.RecordObject(database, "Edit Part Volume");
                part.m_volume = newPartVol;
                SoundToolDatabaseProvider.MarkDirty(database);
            }

            GUILayout.Label(SoundToolGui.GetDecibelText(part.m_volume), EditorStyles.miniLabel, GUILayout.Width(58.0f));
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// パート種別に応じた短いバッジ文字を取得します。
    /// </summary>
    private static string GetPartBadgeText(SoundPartType partType)
    {
        switch (partType)
        {
            case SoundPartType.PART_INTRO:
                return "_I";
            case SoundPartType.PART_LOOP:
                return "_L";
            case SoundPartType.PART_OUTRO:
                return "_O";
            default:
                return "-";
        }
    }

    /// <summary>
    /// 4. シーン内の全試聴エミッターを一覧表示し、同時再生・相対バランス調整を行います（I/L/O まとめ調整対応）。
    /// </summary>
    private void DrawAllSceneEmittersSection(SoundDatabase database)
    {
        SoundScenePreviewEmitter[] emitters = FindObjectsByType<SoundScenePreviewEmitter>(FindObjectsSortMode.None);
        if (emitters.Length <= 1)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"4. シーン内の全エミッター同時バランス確認 ({emitters.Length} 個)", EditorStyles.boldLabel);

        if (GUILayout.Button("すべて再生", EditorStyles.miniButton, GUILayout.Width(72.0f)))
        {
            foreach (SoundScenePreviewEmitter item in emitters)
            {
                item.PlayPreview();
            }
        }

        if (GUILayout.Button("すべて停止", EditorStyles.miniButton, GUILayout.Width(72.0f)))
        {
            foreach (SoundScenePreviewEmitter item in emitters)
            {
                item.StopPreview();
            }
        }

        EditorGUILayout.EndHorizontal();

        foreach (SoundScenePreviewEmitter item in emitters)
        {
            CollectSequenceGroupParts(database, item.SelectedAudioId, m_tempEmitterParts);
            SoundDatabase.SoundData itemData = database.GetSoundData(item.SelectedAudioId);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(item.IsPlaying() ? "■" : "▶", GUILayout.Width(24.0f)))
            {
                if (item.IsPlaying())
                {
                    item.StopPreview();
                }
                else
                {
                    item.PlayPreview();
                }
            }

            string labelText = m_tempEmitterParts.Count > 1
                ? $"{item.name} ({item.SelectedAudioId} [I/L/O])"
                : $"{item.name} ({item.SelectedAudioId})";

            Rect selectRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel, GUILayout.Width(175.0f));
            if (GUI.Button(selectRect, new GUIContent(labelText, labelText), EditorStyles.miniLabel))
            {
                Selection.activeGameObject = item.gameObject;
            }

            if (m_tempEmitterParts.Count > 1)
            {
                float avgVol = CalculateAverageVolume(m_tempEmitterParts);
                float newAvgVol = EditorGUILayout.Slider(avgVol, MIN_VOLUME, MAX_VOLUME);
                if (!Mathf.Approximately(newAvgVol, avgVol))
                {
                    ApplyVolumeDeltaToParts(database, m_tempEmitterParts, newAvgVol - avgVol);
                }
                GUILayout.Label(SoundToolGui.GetDecibelText(avgVol), EditorStyles.miniLabel, GUILayout.Width(56.0f));
            }
            else if (itemData != null)
            {
                float newVol = EditorGUILayout.Slider(itemData.m_volume, MIN_VOLUME, MAX_VOLUME);
                if (!Mathf.Approximately(newVol, itemData.m_volume))
                {
                    Undo.RecordObject(database, "Edit SoundDatabase Volume");
                    itemData.m_volume = newVol;
                    SoundToolDatabaseProvider.MarkDirty(database);
                }
                GUILayout.Label(SoundToolGui.GetDecibelText(itemData.m_volume), EditorStyles.miniLabel, GUILayout.Width(56.0f));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Scene ビュー上で MinDistance / MaxDistance の半径ハンドルを描画・編集します。
    /// </summary>
    private void DrawSceneDistanceHandles(SoundScenePreviewEmitter emitter, SoundDatabase database, SoundDatabase.SoundData data)
    {
        Vector3 position = emitter.transform.position;

        Handles.color = MIN_HANDLE_COLOR;
        EditorGUI.BeginChangeCheck();
        float newMin = Handles.RadiusHandle(Quaternion.identity, position, Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance));
        if (EditorGUI.EndChangeCheck())
        {
            float clampedMin = Mathf.Max(MIN_DISTANCE_LIMIT, newMin);
            ApplyToTargetParts(database, data, "Edit MinDistance Handle", item =>
            {
                item.m_minDistance = clampedMin;
                item.m_maxDistance = Mathf.Max(clampedMin + DISTANCE_MARGIN, item.m_maxDistance);
            });
        }

        Handles.color = MAX_HANDLE_COLOR;
        EditorGUI.BeginChangeCheck();
        float newMax = Handles.RadiusHandle(Quaternion.identity, position, Mathf.Max(data.m_minDistance + DISTANCE_MARGIN, data.m_maxDistance));
        if (EditorGUI.EndChangeCheck())
        {
            float clampedMax = Mathf.Max(data.m_minDistance + DISTANCE_MARGIN, newMax);
            ApplyToTargetParts(database, data, "Edit MaxDistance Handle", item => item.m_maxDistance = clampedMax);
        }
    }

    /// <summary>
    /// Scene ビュー左下にコンパクトな試聴＆DB音量調整オーバーレイを描画します（I/L/O 一括連動対応）。
    /// </summary>
    private void DrawSceneOverlayWindow(SoundScenePreviewEmitter emitter, SoundDatabase database, SoundDatabase.SoundData data)
    {
        Handles.BeginGUI();

        Rect panelRect = new Rect(12.0f, 12.0f, 330.0f, 112.0f);
        GUILayout.BeginArea(panelRect, "Sound シーン試聴 & DB音量調整", GUI.skin.window);

        float distance = emitter.GetDistanceToListener();
        float attenuation = emitter.EvaluateAttenuation(data, distance);
        bool isPlaying = emitter.IsPlaying();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "■ 停止" : "▶ 再生", GUILayout.Width(64.0f)))
        {
            if (isPlaying)
            {
                emitter.StopPreview();
            }
            else
            {
                emitter.PlayPreview();
            }
        }

        GUILayout.Label($"{data.m_id}  (距離:{distance:F1}m / 減衰:{attenuation * 100.0f:F0}%)", EditorStyles.miniBoldLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        bool isGroup = m_groupParts.Count > 1;
        float displayVol = isGroup ? CalculateAverageVolume(m_groupParts) : data.m_volume;

        GUILayout.Label(isGroup ? "一括音量" : "DB音量", GUILayout.Width(52.0f));
        float newVolume = GUILayout.HorizontalSlider(displayVol, MIN_VOLUME, MAX_VOLUME);
        if (!Mathf.Approximately(newVolume, displayVol))
        {
            if (isGroup)
            {
                ApplyVolumeDeltaToParts(database, m_groupParts, newVolume - displayVol);
            }
            else
            {
                Undo.RecordObject(database, "Scene View Adjust Volume");
                data.m_volume = newVolume;
                SoundToolDatabaseProvider.MarkDirty(database);
            }
        }
        GUILayout.Label(SoundToolGui.GetDecibelText(displayVol), EditorStyles.miniLabel, GUILayout.Width(58.0f));
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    /// <summary>
    /// 指定された AudioID が I/L/O グループに属する場合、全パートをリストへ収集します。
    /// </summary>
    private static void CollectSequenceGroupParts(
        SoundDatabase database,
        AudioID selectedId,
        List<SoundDatabase.SoundData> destination)
    {
        destination.Clear();
        if (database == null || selectedId == AudioID.None)
        {
            return;
        }

        SoundToolSequenceHelper.ParseSoundName(selectedId.ToString(), out string baseName, out SoundPartType partType);
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
    /// パートリストの平均音量を算出します。
    /// </summary>
    private static float CalculateAverageVolume(List<SoundDatabase.SoundData> parts)
    {
        if (parts == null || parts.Count == 0)
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
    /// パートリストの音量へ同じ差分を加算し、相対バランスを保ったまま一括増減します。
    /// </summary>
    private static void ApplyVolumeDeltaToParts(
        SoundDatabase database,
        List<SoundDatabase.SoundData> parts,
        float delta)
    {
        if (parts == null || parts.Count == 0)
        {
            return;
        }

        Undo.RecordObject(database, "Adjust Group Volume Balance");
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].m_volume = Mathf.Clamp(parts[i].m_volume + delta, MIN_VOLUME, MAX_VOLUME);
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 単体または I/L/O グループ全体の音量を倍率でスケーリングします。
    /// </summary>
    private void ScaleTargetVolumes(SoundDatabase database, SoundDatabase.SoundData currentData, float multiplier)
    {
        ApplyToTargetParts(database, currentData, "Scale SoundDatabase Volume", item =>
        {
            item.m_volume = Mathf.Clamp(item.m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
        });
    }

    /// <summary>
    /// 単体またはグループ内の全パートに対して SoundDatabase の変更を適用します。
    /// </summary>
    private void ApplyToTargetParts(
        SoundDatabase database,
        SoundDatabase.SoundData fallbackSingle,
        string undoLabel,
        Action<SoundDatabase.SoundData> applyAction)
    {
        Undo.RecordObject(database, undoLabel);

        if (m_groupParts.Count > 1)
        {
            for (int i = 0; i < m_groupParts.Count; i++)
            {
                applyAction(m_groupParts[i]);
            }
        }
        else if (fallbackSingle != null)
        {
            applyAction(fallbackSingle);
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// データベース順で前後のサウンド（I/L/O グループの場合は別グループ）へ切り替えます。
    /// </summary>
    private static void StepAdjacentSound(SoundScenePreviewEmitter emitter, SoundDatabase database, int direction)
    {
        if (database.m_soundList.Count == 0)
        {
            return;
        }

        int currentIndex = database.m_soundList.FindIndex(item => item != null && item.m_id == emitter.SelectedAudioId);
        SoundToolSequenceHelper.ParseSoundName(emitter.SelectedAudioId.ToString(), out string currentBase, out SoundPartType currentPart);

        int step = direction >= 0 ? 1 : -1;
        int index = currentIndex < 0 ? 0 : currentIndex + step;

        while (index >= 0 && index < database.m_soundList.Count)
        {
            SoundDatabase.SoundData candidate = database.m_soundList[index];
            if (candidate != null && candidate.m_id != AudioID.None)
            {
                SoundToolSequenceHelper.ParseSoundName(candidate.m_id.ToString(), out string candBase, out SoundPartType candPart);

                // 同じ I/L/O グループ内の別パートはスキップして次の曲へ移動する
                bool isSameGroup = currentPart != SoundPartType.PART_SINGLE
                    && candPart != SoundPartType.PART_SINGLE
                    && string.Equals(currentBase, candBase, StringComparison.OrdinalIgnoreCase);

                if (!isSameGroup)
                {
                    Undo.RecordObject(emitter, "Step Emitter Sound");
                    emitter.SelectedAudioId = candidate.m_id;
                    emitter.PlayPreview();
                    return;
                }
            }

            index += step;
        }
    }

    /// <summary>
    /// 試聴中は距離メーターを更新するためインスペクターを再描画します。
    /// </summary>
    private void RepaintWhilePlaying()
    {
        SoundScenePreviewEmitter emitter = target as SoundScenePreviewEmitter;
        if (emitter != null && emitter.IsPlaying())
        {
            Repaint();
        }
    }
}

#endif