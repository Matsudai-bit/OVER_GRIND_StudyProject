/// @ using :: 使用エンジン
using UnityEngine;
using UnityEditor;

/// @ className :: ウインドウクラス
/// @ name :: Aoki Hayate
/// @ date :: 2026/08/28
public class SaveDataWindow : EditorWindow
{
    private SaveData m_targetData; // 編集・保存する対象のデータ（メンバー変数規約適用）

    [MenuItem("Tools/SaveDataTool")]
    public static void ShowWindow()
    {
        GetWindow<SaveDataWindow>("セーブデータ管理");
    }

    private void OnGUI()
    {
        GUILayout.Label("JSONセーブデータ マネージャー", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ScriptableObjectをセットするスロットを表示
        m_targetData = (SaveData)EditorGUILayout.ObjectField(
            "対象のセーブデータ", m_targetData, typeof(SaveData), false);

        if (m_targetData == null)
        {
            EditorGUILayout.HelpBox(
                "Projectウィンドウで右クリック ＞ Create ＞ ScriptableObjects ＞ SaveData からデータを作成し、ここにアタッチしてください。",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // 現在のパラメータをエディタ上で確認・編集できるようにする
        EditorGUI.BeginChangeCheck();

        m_targetData.m_saveFileName = EditorGUILayout.TextField("保存ファイル名", m_targetData.m_saveFileName);
        EditorGUILayout.Space();

        m_targetData.m_clearedStage = EditorGUILayout.IntField("クリアステージ", m_targetData.m_clearedStage);
        m_targetData.m_bgmVolume = EditorGUILayout.Slider("BGM音量", m_targetData.m_bgmVolume, 0f, 1f);
        m_targetData.m_seVolume = EditorGUILayout.Slider("SE音量", m_targetData.m_seVolume, 0f, 1f);
        m_targetData.m_masterVolume = EditorGUILayout.Slider("マスター音量", m_targetData.m_masterVolume, 0f, 1f);
        m_targetData.m_screenSize = EditorGUILayout.IntField("スクリーンサイズ", m_targetData.m_screenSize);
        m_targetData.m_cameraSensitivity = EditorGUILayout.Slider("カメラ感度", m_targetData.m_cameraSensitivity, 0.1f, 10f);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(m_targetData);
        }

        EditorGUILayout.Space();

        // SaveSystemからパスを取得して表示
        string path = SaveSystem.GetFilePath(m_targetData.m_saveFileName);
        EditorGUILayout.HelpBox($"【保存先】\n{path}", MessageType.Info);
        EditorGUILayout.Space();

        // セーブ＆ロードボタン
        GUILayout.BeginHorizontal();

        // SaveSystem.Save を呼び出す
        if (GUILayout.Button("セーブする\n(JSONへ書出)", GUILayout.Height(40)))
        {
            SaveSystem.Save(m_targetData, m_targetData.m_saveFileName);
            AssetDatabase.SaveAssets();
        }

        // SaveSystem.LoadOverwrite を呼び出す
        if (GUILayout.Button("ロードする\n(JSONから読込)", GUILayout.Height(40)))
        {
            SaveSystem.LoadOverwrite(m_targetData, m_targetData.m_saveFileName);
            GUI.FocusControl(null);
        }

        GUILayout.EndHorizontal();
    }
}