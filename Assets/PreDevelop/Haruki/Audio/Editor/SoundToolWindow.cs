#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// サウンドの音量調整・縦型ミキサー卓・2D/3D同時ミックス・リアルタイム解析・ID 管理をまとめた Editor ウィンドウです。
/// </summary>
public sealed class SoundToolWindow : EditorWindow
{
    // ウィンドウのタイトル
    private const string WINDOW_TITLE = "Sound ツール";

    // ウィンドウの最小幅
    private const float WINDOW_MIN_WIDTH = 920.0f;

    // ウィンドウの最小高さ
    private const float WINDOW_MIN_HEIGHT = 720.0f;

    // タブ全体の幅
    private const float TAB_TOOLBAR_WIDTH = 660.0f;

    // 保存ボタンの幅
    private const float SAVE_BUTTON_WIDTH = 50.0f;

    // DB 表示ボタンの幅
    private const float PING_BUTTON_WIDTH = 80.0f;

    // タブ：ミキサー（一覧・詳細）
    private const int TAB_MIXER = 0;

    // タブ：コンソール（縦ミキサー卓）
    private const int TAB_CONSOLE = 1;

    // タブ：同時ミックス（2D/3D比較）
    private const int TAB_MULTI_MIX = 2;

    // タブ：ID の追加・変更・削除
    private const int TAB_ID_MANAGE = 3;

    // タブの表示名
    private static readonly string[] TAB_LABELS =
    {
        "ミキサー（一覧・詳細）",
        "コンソール（縦ミキサー卓）",
        "同時ミックス（2D/3D）",
        "ID の追加・変更・削除"
    };

    // 選択中のタブ（スクリプト再読み込み後も保持する）
    [SerializeField] private int m_tabIndex = TAB_MIXER;

    // ミキサー画面
    private SoundToolMixerView m_mixerView;

    // コンソール（縦ミキサー卓）画面
    private SoundToolConsoleView m_consoleView;

    // 複数トラック同時ミックス画面
    private SoundToolMultiMixView m_multiMixView;

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
        m_mixerView = new SoundToolMixerView(Repaint);
        m_consoleView = new SoundToolConsoleView();
        m_multiMixView = new SoundToolMultiMixView();
        m_idManageView = new SoundToolIdManageView(Repaint);

        // 単体試聴およびマルチトラック試聴の更新時に再描画する
        SoundToolPreviewPlayer.OnStateChanged += Repaint;
        SoundToolMultiPreviewPlayer.OnStateChanged += Repaint;

        // ID 追加直後ならミキサーへ切り替え、追加した ID を選択状態にする
        if (m_mixerView.SelectRecentAddedIds())
        {
            m_tabIndex = TAB_MIXER;
        }

        Undo.undoRedoPerformed += HandleUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;
        SoundToolPreviewPlayer.OnStateChanged -= Repaint;
        SoundToolMultiPreviewPlayer.OnStateChanged -= Repaint;

        SoundToolPreviewPlayer.Stop();
        SoundToolMultiPreviewPlayer.StopAll();
        SoundToolDatabaseProvider.SaveIfNeeded();
    }

    private void OnLostFocus()
    {
        // フォーカスを失うタイミングで保存する
        SoundToolDatabaseProvider.SaveIfNeeded();
    }

    private void OnGUI()
    {
        DrawToolbar();

        switch (m_tabIndex)
        {
            case TAB_CONSOLE:
                m_consoleView.Draw();
                break;

            case TAB_MULTI_MIX:
                m_multiMixView.Draw();
                break;

            case TAB_ID_MANAGE:
                m_idManageView.Draw();
                break;

            default:
                m_mixerView.Draw();
                break;
        }
    }

    /// <summary>
    /// Undo / Redo 実行時に未保存フラグと表示を更新します。
    /// </summary>
    private void HandleUndoRedo()
    {
        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();

        if (database != null)
        {
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        Repaint();
    }

    /// <summary>
    /// タブと保存状態を表示するツールバーを描画します。
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        m_tabIndex = GUILayout.Toolbar(m_tabIndex, TAB_LABELS, EditorStyles.toolbarButton, GUILayout.Width(TAB_TOOLBAR_WIDTH));

        GUILayout.FlexibleSpace();

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
}

#endif