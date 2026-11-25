#if UNITY_EDITOR

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// SoundEmitter の Inspector です。スロットごとの音量バランスを見ながら、試聴と切り替えができます。
/// </summary>
[CustomEditor(typeof(SoundEmitter))]
public sealed class SoundEmitterEditor : Editor
{
    // エミッター側のプロパティ名
    private const string PROP_SLOTS = "m_slots";
    private const string PROP_CURRENT_INDEX = "m_currentIndex";
    private const string PROP_EMITTER_VOLUME = "m_emitterVolume";
    private const string PROP_EMIT_POINT = "m_emitPoint";
    private const string PROP_FOLLOW = "m_isFollowEmitPoint";
    private const string PROP_PLAY_ON_ENABLE = "m_isPlayOnEnable";
    private const string PROP_STOP_ON_DISABLE = "m_isStopOnDisable";
    private const string PROP_FADE_IN = "m_fadeInSeconds";
    private const string PROP_FADE_OUT = "m_fadeOutSeconds";
    private const string PROP_SWITCH_FADE = "m_switchFadeSeconds";

    // スロット側のプロパティ名
    private const string SLOT_PROP_LABEL = "m_label";
    private const string SLOT_PROP_ID = "m_soundId";
    private const string SLOT_PROP_VOLUME = "m_volumeScale";
    private const string SLOT_PROP_PITCH = "m_pitchScale";
    private const string SLOT_PROP_SPATIAL = "m_spatialMode";

    // スロット1件あたりの行数
    private const int ROW_COUNT = 4;

    // 行どうしの間隔
    private const float ROW_SPACING = 2.0f;

    // スロットの上下余白
    private const float ROW_PADDING = 4.0f;

    // 現在スロットのラジオボタンの幅
    private const float RADIO_WIDTH = 18.0f;

    // 試聴・再生ボタンの幅
    private const float PLAY_BUTTON_WIDTH = 30.0f;

    // 横方向の間隔
    private const float GAP = 4.0f;

    // ラベル欄が占める割合
    private const float LABEL_RATIO = 0.38f;

    // ピッチスライダーが占める割合
    private const float PITCH_RATIO = 0.55f;

    // スロット内スライダーのラベル幅
    private const float SLOT_LABEL_WIDTH = 44.0f;

    // 2D / 3D 指定の表示名（SoundSpatialMode の宣言順）
    private static readonly string[] SPATIAL_LABELS = { "DB設定に従う", "常に 2D", "常に 3D" };

    // 警告表示の色
    private static readonly Color ERROR_COLOR = new Color(1.0f, 0.4f, 0.4f);

    // プロパティのキャッシュ
    private SerializedProperty m_slotsProperty;
    private SerializedProperty m_currentIndexProperty;
    private SerializedProperty m_emitterVolumeProperty;
    private SerializedProperty m_emitPointProperty;
    private SerializedProperty m_followProperty;
    private SerializedProperty m_playOnEnableProperty;
    private SerializedProperty m_stopOnDisableProperty;
    private SerializedProperty m_fadeInProperty;
    private SerializedProperty m_fadeOutProperty;
    private SerializedProperty m_switchFadeProperty;

    // スロットの並べ替え・追加・削除ができるリスト
    private ReorderableList m_slotList;

    private void OnEnable()
    {
        m_slotsProperty = serializedObject.FindProperty(PROP_SLOTS);
        m_currentIndexProperty = serializedObject.FindProperty(PROP_CURRENT_INDEX);
        m_emitterVolumeProperty = serializedObject.FindProperty(PROP_EMITTER_VOLUME);
        m_emitPointProperty = serializedObject.FindProperty(PROP_EMIT_POINT);
        m_followProperty = serializedObject.FindProperty(PROP_FOLLOW);
        m_playOnEnableProperty = serializedObject.FindProperty(PROP_PLAY_ON_ENABLE);
        m_stopOnDisableProperty = serializedObject.FindProperty(PROP_STOP_ON_DISABLE);
        m_fadeInProperty = serializedObject.FindProperty(PROP_FADE_IN);
        m_fadeOutProperty = serializedObject.FindProperty(PROP_FADE_OUT);
        m_switchFadeProperty = serializedObject.FindProperty(PROP_SWITCH_FADE);

        CreateSlotList();
    }

    private void OnDisable()
    {
        // 試聴中のまま Inspector を閉じても鳴り続けないようにする
        SoundToolPreviewPlayer.Stop();
    }

    /// <summary>
    /// 再生中・試聴中は常に再描画して、状態表示を更新します。
    /// </summary>
    /// <returns>
    /// true：常に再描画します。
    /// false：必要なときだけ再描画します。
    /// </returns>
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying || SoundToolPreviewPlayer.HasActivePreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ClampCurrentIndex();

        DrawPlayControls();

        EditorGUILayout.Space(4.0f);
        EditorGUILayout.PropertyField(
            m_emitterVolumeProperty,
            new GUIContent("エミッター音量", "このオブジェクト全体の音量倍率です。"));

        EditorGUILayout.Space(4.0f);
        m_slotList.DoLayoutList();

        EditorGUILayout.Space(4.0f);
        DrawOtherSettings();

        serializedObject.ApplyModifiedProperties();

        SyncPreviewVolume();
    }

    /// <summary>
    /// スロットのリストを作成します。
    /// </summary>
    private void CreateSlotList()
    {
        m_slotList = new ReorderableList(serializedObject, m_slotsProperty, true, true, true, true);

        m_slotList.drawHeaderCallback = DrawSlotHeader;
        m_slotList.drawElementCallback = DrawSlotElement;
        m_slotList.elementHeightCallback = GetSlotElementHeight;
        m_slotList.onAddCallback = AddSlot;
    }

    /// <summary>
    /// 現在のスロット番号を範囲内に収めます。
    /// </summary>
    private void ClampCurrentIndex()
    {
        int maxIndex = Mathf.Max(0, m_slotsProperty.arraySize - 1);
        m_currentIndexProperty.intValue = Mathf.Clamp(m_currentIndexProperty.intValue, 0, maxIndex);
    }

    /// <summary>
    /// ゲーム実行中の操作ボタンを描画します。
    /// </summary>
    private void DrawPlayControls()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SoundEmitter emitter = (SoundEmitter)target;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 状態表示
        string stateText = emitter.IsPlaying() ? "再生中" : "停止中";
        SoundEmitterSlot currentSlot = emitter.GetSlot(emitter.CurrentIndex);
        string slotText = currentSlot != null ? currentSlot.DisplayName : "（スロットなし）";

        EditorGUILayout.LabelField($"{stateText}　現在のスロット：{slotText}", EditorStyles.boldLabel);

        // 操作ボタン
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("◀ 前へ"))
        {
            emitter.SwitchPrevious();
        }

        if (GUILayout.Button("▶ 再生"))
        {
            emitter.Play();
        }

        if (GUILayout.Button("■ 停止"))
        {
            emitter.Stop();
        }

        if (GUILayout.Button("次へ ▶"))
        {
            emitter.SwitchNext();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 発生位置・タイミング・フェードの設定を描画します。
    /// </summary>
    private void DrawOtherSettings()
    {
        EditorGUILayout.LabelField("発生位置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_emitPointProperty, new GUIContent("発音位置", "未設定のときはこのオブジェクトから鳴ります。"));
        EditorGUILayout.PropertyField(m_followProperty, new GUIContent("3D 再生中に追従する"));

        EditorGUILayout.Space(4.0f);
        EditorGUILayout.LabelField("再生・停止のタイミング", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_playOnEnableProperty, new GUIContent("有効化時に再生"));
        EditorGUILayout.PropertyField(m_stopOnDisableProperty, new GUIContent("無効化時に停止"));

        EditorGUILayout.Space(4.0f);
        EditorGUILayout.LabelField("フェード（秒）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_fadeInProperty, new GUIContent("フェードイン"));
        EditorGUILayout.PropertyField(m_fadeOutProperty, new GUIContent("フェードアウト"));
        EditorGUILayout.PropertyField(m_switchFadeProperty, new GUIContent("切り替え時のクロスフェード"));
    }

    /// <summary>
    /// スロットリストの見出しを描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    private void DrawSlotHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "サウンドスロット（● が現在のスロット　▶ で即切り替え）");
    }

    /// <summary>
    /// スロット1件分の高さを取得します。
    /// </summary>
    /// <param name="index">スロット番号。</param>
    /// <returns>要素の高さ。</returns>
    private float GetSlotElementHeight(int index)
    {
        return ROW_COUNT * (EditorGUIUtility.singleLineHeight + ROW_SPACING) + ROW_PADDING * 2.0f;
    }

    /// <summary>
    /// スロットを追加し、初期値を設定します。
    /// </summary>
    /// <param name="list">対象のリスト。</param>
    private void AddSlot(ReorderableList list)
    {
        int newIndex = m_slotsProperty.arraySize;
        m_slotsProperty.arraySize++;

        // 直前の要素が複製されるため、初期値を明示的に設定する
        SerializedProperty element = m_slotsProperty.GetArrayElementAtIndex(newIndex);
        element.FindPropertyRelative(SLOT_PROP_LABEL).stringValue = "";
        element.FindPropertyRelative(SLOT_PROP_ID).enumValueIndex = 0;
        element.FindPropertyRelative(SLOT_PROP_VOLUME).floatValue = 1.0f;
        element.FindPropertyRelative(SLOT_PROP_PITCH).floatValue = 1.0f;
        element.FindPropertyRelative(SLOT_PROP_SPATIAL).enumValueIndex = 0;

        list.index = newIndex;
    }

    /// <summary>
    /// スロット1件分を描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="index">スロット番号。</param>
    /// <param name="isActive">アクティブか。</param>
    /// <param name="isFocused">フォーカス中か。</param>
    private void DrawSlotElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SoundEmitter emitter = (SoundEmitter)target;
        SerializedProperty element = m_slotsProperty.GetArrayElementAtIndex(index);

        SerializedProperty labelProperty = element.FindPropertyRelative(SLOT_PROP_LABEL);
        SerializedProperty idProperty = element.FindPropertyRelative(SLOT_PROP_ID);
        SerializedProperty volumeProperty = element.FindPropertyRelative(SLOT_PROP_VOLUME);
        SerializedProperty pitchProperty = element.FindPropertyRelative(SLOT_PROP_PITCH);
        SerializedProperty spatialProperty = element.FindPropertyRelative(SLOT_PROP_SPATIAL);

        // サウンド ID はオブジェクト側から取得する（列挙値と並び順の違いを避けるため）
        SoundEmitterSlot slot = emitter.GetSlot(index);
        AudioID soundId = slot != null ? slot.SoundId : AudioID.None;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float rowStep = lineHeight + ROW_SPACING;
        float y = rect.y + ROW_PADDING;

        // 1行目：現在スロット・ラベル・ID・再生ボタン
        Rect radioRect = new Rect(rect.x, y, RADIO_WIDTH, lineHeight);
        Rect playRect = new Rect(rect.xMax - PLAY_BUTTON_WIDTH, y, PLAY_BUTTON_WIDTH, lineHeight);

        float fieldsX = radioRect.xMax + GAP;
        float fieldsWidth = playRect.x - GAP - fieldsX;
        float labelWidth = fieldsWidth * LABEL_RATIO;

        Rect labelRect = new Rect(fieldsX, y, labelWidth, lineHeight);
        Rect idRect = new Rect(labelRect.xMax + GAP, y, fieldsWidth - labelWidth - GAP, lineHeight);

        // 現在スロットの選択
        bool isCurrent = m_currentIndexProperty.intValue == index;
        bool newIsCurrent = GUI.Toggle(radioRect, isCurrent, GUIContent.none, EditorStyles.radioButton);

        if (newIsCurrent && !isCurrent)
        {
            m_currentIndexProperty.intValue = index;
        }

        labelProperty.stringValue = EditorGUI.TextField(labelRect, labelProperty.stringValue);
        EditorGUI.PropertyField(idRect, idProperty, GUIContent.none);

        DrawSlotPlayButton(playRect, emitter, index, soundId, volumeProperty.floatValue, pitchProperty.floatValue);

        // 2行目：スロット音量
        y += rowStep;
        Rect volumeRect = new Rect(rect.x + RADIO_WIDTH + GAP, y, rect.width - RADIO_WIDTH - GAP, lineHeight);
        volumeProperty.floatValue = DrawSlotSlider(volumeRect, "音量", volumeProperty.floatValue, 0.0f, 1.0f);

        // 3行目：ピッチと 2D / 3D 指定
        y += rowStep;
        float rowWidth = rect.width - RADIO_WIDTH - GAP;
        float pitchWidth = rowWidth * PITCH_RATIO;

        Rect pitchRect = new Rect(rect.x + RADIO_WIDTH + GAP, y, pitchWidth, lineHeight);
        Rect spatialRect = new Rect(pitchRect.xMax + GAP, y, rowWidth - pitchWidth - GAP, lineHeight);

        pitchProperty.floatValue = DrawSlotSlider(pitchRect, "ピッチ", pitchProperty.floatValue, 0.5f, 2.0f);
        spatialProperty.enumValueIndex = EditorGUI.Popup(spatialRect, spatialProperty.enumValueIndex, SPATIAL_LABELS);

        // 4行目：音量バランス
        y += rowStep;
        Rect balanceRect = new Rect(rect.x + RADIO_WIDTH + GAP, y, rect.width - RADIO_WIDTH - GAP, lineHeight);
        DrawBalanceBar(balanceRect, soundId, volumeProperty.floatValue);
    }

    /// <summary>
    /// ラベル幅を狭めたスライダーを描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="label">ラベル。</param>
    /// <param name="value">現在値。</param>
    /// <param name="min">最小値。</param>
    /// <param name="max">最大値。</param>
    /// <returns>変更後の値。</returns>
    private static float DrawSlotSlider(Rect rect, string label, float value, float min, float max)
    {
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = SLOT_LABEL_WIDTH;

        float newValue = EditorGUI.Slider(rect, label, value, min, max);

        EditorGUIUtility.labelWidth = previousLabelWidth;

        return newValue;
    }

    /// <summary>
    /// スロットの再生・試聴ボタンを描画します。
    /// ゲーム実行中は即座に切り替え、停止中は Editor 上で試聴します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="emitter">対象のエミッター。</param>
    /// <param name="index">スロット番号。</param>
    /// <param name="id">サウンド ID。</param>
    /// <param name="slotVolume">スロット音量。</param>
    /// <param name="slotPitch">スロットピッチ。</param>
    private void DrawSlotPlayButton(Rect rect, SoundEmitter emitter, int index, AudioID id, float slotVolume, float slotPitch)
    {
        bool isPlayMode = Application.isPlaying;
        bool isPlaying = isPlayMode
            ? emitter.IsPlaying() && emitter.CurrentIndex == index
            : SoundToolPreviewPlayer.IsPlaying(id);

        if (!GUI.Button(rect, isPlaying ? "■" : "▶"))
        {
            return;
        }

        // ゲーム実行中は実際の再生を切り替える
        if (isPlayMode)
        {
            if (isPlaying)
            {
                emitter.Stop();
            }
            else
            {
                emitter.PlaySlot(index);
            }

            return;
        }

        // 停止中は Editor 上で試聴する
        TogglePreview(id, slotVolume, slotPitch);
    }

    /// <summary>
    /// Editor 上での試聴を切り替えます。
    /// </summary>
    /// <param name="id">サウンド ID。</param>
    /// <param name="slotVolume">スロット音量。</param>
    /// <param name="slotPitch">スロットピッチ。</param>
    private void TogglePreview(AudioID id, float slotVolume, float slotPitch)
    {
        if (SoundToolPreviewPlayer.IsPlaying(id))
        {
            SoundToolPreviewPlayer.Stop();
            return;
        }

        SoundDatabase.SoundData data = FindSoundData(id);

        if (data == null || data.m_clip == null)
        {
            return;
        }

        float volume = data.m_volume * slotVolume * m_emitterVolumeProperty.floatValue;
        SoundToolPreviewPlayer.Play(id, data.m_clip, volume, data.m_pitch * slotPitch, data.m_loop);
    }

    /// <summary>
    /// 音量の掛け合わせ（DB × スロット × エミッター）を棒グラフで描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="id">サウンド ID。</param>
    /// <param name="slotVolume">スロット音量。</param>
    private void DrawBalanceBar(Rect rect, AudioID id, float slotVolume)
    {
        SoundDatabase.SoundData data = FindSoundData(id);

        // 再生できない場合は理由を赤字で表示する
        if (data == null || data.m_clip == null)
        {
            Color previousColor = GUI.contentColor;
            GUI.contentColor = ERROR_COLOR;

            string message = id == AudioID.None ? "ID が未設定です" : "データ未登録、または AudioClip が未設定です";
            EditorGUI.LabelField(rect, message, EditorStyles.miniLabel);

            GUI.contentColor = previousColor;
            return;
        }

        float emitterVolume = m_emitterVolumeProperty.floatValue;
        float finalVolume = data.m_volume * slotVolume * emitterVolume;

        string text = $"DB {data.m_volume:F2} × スロット {slotVolume:F2} × エミッター {emitterVolume:F2} = {finalVolume:F2}（{SoundToolGui.GetDecibelText(finalVolume)}）";
        EditorGUI.ProgressBar(rect, Mathf.Clamp01(finalVolume), text);
    }

    /// <summary>
    /// 試聴中の音量を、最新のスロット設定に合わせます。
    /// </summary>
    private void SyncPreviewVolume()
    {
        AudioID playingId = SoundToolPreviewPlayer.PlayingId;

        if (Application.isPlaying || playingId == AudioID.None)
        {
            return;
        }

        SoundEmitter emitter = (SoundEmitter)target;
        SoundDatabase.SoundData data = FindSoundData(playingId);

        if (data == null)
        {
            return;
        }

        // 試聴中の ID を持つスロットの音量を反映する
        for (int i = 0; i < emitter.SlotCount; i++)
        {
            SoundEmitterSlot slot = emitter.GetSlot(i);

            if (slot != null && slot.SoundId == playingId)
            {
                SoundToolPreviewPlayer.SetVolume(data.m_volume * slot.VolumeScale * emitter.EmitterVolume);
                return;
            }
        }
    }

    /// <summary>
    /// サウンド ID に対応する設定を取得します。
    /// </summary>
    /// <param name="id">サウンド ID。</param>
    /// <returns>サウンド設定。見つからない場合は null。</returns>
    private static SoundDatabase.SoundData FindSoundData(AudioID id)
    {
        if (id == AudioID.None)
        {
            return null;
        }

        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();

        return database != null ? database.GetSoundData(id) : null;
    }
}

#endif