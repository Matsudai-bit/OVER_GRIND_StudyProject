#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// サウンドの音量調整・検索・試聴・ID 管理をまとめた Editor ウィンドウです。
/// </summary>
public sealed class SoundToolWindow : EditorWindow
{
    // ウィンドウのタイトル
    private const string WINDOW_TITLE = "Sound ツール";

    // ウィンドウの最小幅
    private const float WINDOW_MIN_WIDTH = 780.0f;

    // ウィンドウの最小高さ
    private const float WINDOW_MIN_HEIGHT = 700.0f;

    // タブ全体の幅
    private const float TAB_TOOLBAR_WIDTH = 380.0f;

    // 保存ボタンの幅
    private const float SAVE_BUTTON_WIDTH = 50.0f;

    // DB 表示ボタンの幅
    private const float PING_BUTTON_WIDTH = 80.0f;

    // タブ：ミキサー
    private const int TAB_MIXER = 0;

    // タブ：ID の追加・削除
    private const int TAB_ID_MANAGE = 1;

    // タブの表示名
    private static readonly string[] TAB_LABELS = { "ミキサー（音量・検索・試聴）", "ID の追加・削除" };

    // 選択中のタブ（スクリプト再読み込み後も保持する）
    [SerializeField] private int m_tabIndex = TAB_MIXER;

    // ミキサー画面
    private SoundToolMixerView m_mixerView;

    // ID 管理画面
    private SoundToolIdManageView m_idManageView;

    /// <summary>
    /// Sound ツールのウィンドウを開きます。
    /// </summary>
    [MenuItem("Tools/Audio/Sound ツール（ミキサー・ID管理）")]
    public static void ShowWindow()
    {
        SoundToolWindow window = GetWindow<SoundToolWindow>(WINDOW_TITLE);
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
    }

    private void OnEnable()
    {
        // 各画面を生成する
        m_mixerView = new SoundToolMixerView();
        m_idManageView = new SoundToolIdManageView(Repaint);

        // ID 追加直後ならミキサーへ切り替え、追加した ID を選択状態にする
        if (m_mixerView.SelectRecentAddedIds())
        {
            m_tabIndex = TAB_MIXER;
        }

        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;

        SoundToolPreviewPlayer.Stop();
        SoundToolDatabaseProvider.SaveIfNeeded();
    }

    private void OnLostFocus()
    {
        // フォーカスを失うときに保存する
        SoundToolDatabaseProvider.SaveIfNeeded();
    }

    private void OnInspectorUpdate()
    {
        // 試聴中は再生終了を検知するため定期的に再描画する
        if (SoundToolPreviewPlayer.HasActivePreview())
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        switch (m_tabIndex)
        {
            case TAB_ID_MANAGE:
                m_idManageView.Draw();
                break;

            default:
                m_mixerView.Draw();
                break;
        }

        HandleAutoSave();
    }

    /// <summary>
    /// タブと保存状態を表示するツールバーを描画します。
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        m_tabIndex = GUILayout.Toolbar(m_tabIndex, TAB_LABELS, EditorStyles.toolbarButton, GUILayout.Width(TAB_TOOLBAR_WIDTH));

        GUILayout.FlexibleSpace();

        // 保存状態
        string statusText = SoundToolDatabaseProvider.HasUnsavedChanges ? "● 未保存" : "保存済み";
        GUILayout.Label(statusText, EditorStyles.miniLabel);

        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(SAVE_BUTTON_WIDTH)))
        {
            SoundToolDatabaseProvider.SaveIfNeeded();
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("DB を表示", EditorStyles.toolbarButton, GUILayout.Width(PING_BUTTON_WIDTH)))
        {
            SoundToolDatabaseProvider.PingDatabase();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// マウスやキーを離したタイミングで自動保存します。
    /// </summary>
    private void HandleAutoSave()
    {
        EventType rawType = Event.current.rawType;

        if (rawType == EventType.MouseUp || rawType == EventType.KeyUp)
        {
            SoundToolDatabaseProvider.SaveIfNeeded();
        }
    }
}

#endif