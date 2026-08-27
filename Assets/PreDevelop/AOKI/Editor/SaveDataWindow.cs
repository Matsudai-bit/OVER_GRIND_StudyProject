/// @ using :: 使用エンジン
using UnityEngine;
using UnityEditor;

/// @ className :: ウインドウクラス
/// @ name :: Aoki Hayate
/// @ date :: 2026/08/27
public class SaveDataWindow : EditorWindow
{
    private SaveData targetData; // 編集・保存する対象のデータ

    [MenuItem("Window/Save Data Manager")]
    public static void ShowWindow()
    {
        GetWindow<SaveDataWindow>("セーブデータ管理");
    }

    private void OnGUI()
    {
        GUILayout.Label("JSONセーブデータ マネージャー", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //  ScriptableObjectをセットするスロットを表示
        targetData = (SaveData)EditorGUILayout.ObjectField(
            "対象のセーブデータ", targetData, typeof(SaveData), false);

        if (targetData == null)
        {
            EditorGUILayout.HelpBox(
                "Projectウィンドウで右クリック ＞ Create ＞ ScriptableObjects ＞ SaveData からデータを作成し、ここにアタッチしてください。",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // 現在のパラメータをエディタ上で確認・編集できるようにする
        EditorGUI.BeginChangeCheck();

        targetData.saveFileName = EditorGUILayout.TextField("保存ファイル名", targetData.saveFileName);
        EditorGUILayout.Space();

        targetData.clearedStage = EditorGUILayout.IntField("クリアステージ", targetData.clearedStage);
        targetData.bgmVolume = EditorGUILayout.Slider("BGM音量", targetData.bgmVolume, 0f, 1f);
        targetData.seVolume = EditorGUILayout.Slider("SE音量", targetData.seVolume, 0f, 1f);
        targetData.masterVolume = EditorGUILayout.Slider("マスター音量", targetData.masterVolume, 0f, 1f);
        targetData.screenSize = EditorGUILayout.IntField("スクリーンサイズ", targetData.screenSize);
        targetData.cameraSensitivity = EditorGUILayout.Slider("カメラ感度", targetData.cameraSensitivity, 0.1f, 10f);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(targetData);
        }

        EditorGUILayout.Space();

        // SaveSystemからパスを取得して表示
        string path = SaveSystem.GetFilePath(targetData.saveFileName);
        EditorGUILayout.HelpBox($"【保存先】\n{path}", MessageType.Info);
        EditorGUILayout.Space();

        // セーブ＆ロードボタン
        GUILayout.BeginHorizontal();

        // SaveSystem.Save を呼び出す
        if (GUILayout.Button("セーブする\n(JSONへ書出)", GUILayout.Height(40)))
        {
            SaveSystem.Save(targetData, targetData.saveFileName);
            AssetDatabase.SaveAssets();
        }

        // SaveSystem.LoadOverwrite を呼び出す
        if (GUILayout.Button("ロードする\n(JSONから読込)", GUILayout.Height(40)))
        {
            SaveSystem.LoadOverwrite(targetData, targetData.saveFileName);
            GUI.FocusControl(null);
        }

        GUILayout.EndHorizontal();
    }
}