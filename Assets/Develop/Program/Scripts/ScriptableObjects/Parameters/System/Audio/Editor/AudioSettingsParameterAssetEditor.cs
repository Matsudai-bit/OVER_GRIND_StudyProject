using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioSettingsParameterAssetの
/// Inspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(AudioSettingsParameterAsset))]
public sealed class AudioSettingsParameterAssetEditor : Editor
{
    private const float SECTION_SPACE = 8.0f;

    // AudioMixer
    private SerializedProperty
        m_audioMixerProperty;

    // Master Parameter名
    private SerializedProperty
        m_masterVolumeParameterNameProperty;

    // BGM Parameter名
    private SerializedProperty
        m_bgmVolumeParameterNameProperty;

    // SE Parameter名
    private SerializedProperty
        m_seVolumeParameterNameProperty;

    // 最小音量
    private SerializedProperty
        m_minVolumeDbProperty;

    // 最大音量
    private SerializedProperty
        m_maxVolumeDbProperty;

    // dB変換確認用
    private float m_previewVolume = 1.0f;

    private void OnEnable()
    {
        m_audioMixerProperty =
            serializedObject.FindProperty(
                "m_audioMixer");

        m_masterVolumeParameterNameProperty =
            serializedObject.FindProperty(
                "m_masterVolumeParameterName");

        m_bgmVolumeParameterNameProperty =
            serializedObject.FindProperty(
                "m_bgmVolumeParameterName");

        m_seVolumeParameterNameProperty =
            serializedObject.FindProperty(
                "m_seVolumeParameterName");

        m_minVolumeDbProperty =
            serializedObject.FindProperty(
                "m_minVolumeDb");

        m_maxVolumeDbProperty =
            serializedObject.FindProperty(
                "m_maxVolumeDb");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawAudioMixerSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawParameterSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawVolumeRangeSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawVolumePreviewSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "SaveDataの0～1の音量設定を"
            + "AudioMixerのdB値へ変換するための"
            + "パラメータです。",
            MessageType.Info);
    }

    /// <summary>
    /// AudioMixer設定を表示します。
    /// </summary>
    private void DrawAudioMixerSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "Audio Mixer",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_audioMixerProperty,
            new GUIContent(
                "Audio Mixer"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Exposed Parameter設定を表示します。
    /// </summary>
    private void DrawParameterSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "Exposed Parameters",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_masterVolumeParameterNameProperty,
            new GUIContent(
                "Master",
                "Master音量のExposed Parameter名。"));

        EditorGUILayout.PropertyField(
            m_bgmVolumeParameterNameProperty,
            new GUIContent(
                "BGM",
                "BGM音量のExposed Parameter名。"));

        EditorGUILayout.PropertyField(
            m_seVolumeParameterNameProperty,
            new GUIContent(
                "SE",
                "SE音量のExposed Parameter名。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// dB範囲設定を表示します。
    /// </summary>
    private void DrawVolumeRangeSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "音量範囲",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_minVolumeDbProperty,
            new GUIContent(
                "最小音量 [dB]",
                "音量0のときに使用します。"));

        EditorGUILayout.PropertyField(
            m_maxVolumeDbProperty,
            new GUIContent(
                "最大音量 [dB]",
                "音量1のときに使用します。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 音量変換のプレビューを表示します。
    /// </summary>
    private void DrawVolumePreviewSection()
    {
        AudioSettingsParameterAsset parameterAsset =
            target as AudioSettingsParameterAsset;

        if (parameterAsset == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "音量変換プレビュー",
            EditorStyles.boldLabel);

        m_previewVolume =
            EditorGUILayout.Slider(
                "設定値",
                m_previewVolume,
                0.0f,
                1.0f);

        float volumeDb =
            parameterAsset
                .ConvertNormalizedVolumeToDb(
                    m_previewVolume);

        EditorGUI.BeginDisabledGroup(true);

        EditorGUILayout.FloatField(
            "AudioMixer [dB]",
            volumeDb);

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }
}