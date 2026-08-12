using UnityEngine;
using UnityEditor;

public class SaveDataWindow : EditorWindow
{
    private SaveData targetData; // 編集・保存する対象のデータ

    [MenuItem("Window/Save Data Manager")]
    public static void ShowWindow()
    {
        // ウィンドウを表示する
        GetWindow<SaveDataWindow>("セーブデータ管理");
    }

    private void OnGUI()
    {
        GUILayout.Label("JSONセーブデータ マネージャー", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. ScriptableObjectをセットするスロットを表示
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

        // 2. 現在のパラメータをエディタ上で確認・編集できるようにする
        EditorGUI.BeginChangeCheck();

        // ★ここを追加：エディタウィンドウ上で保存ファイル名を入力できるようにする
        targetData.saveFileName = EditorGUILayout.TextField("保存ファイル名", targetData.saveFileName);
        EditorGUILayout.Space();

        targetData.clearedStage = EditorGUILayout.IntField("クリアステージ", targetData.clearedStage);
        targetData.bgmVolume = EditorGUILayout.Slider("BGM音量", targetData.bgmVolume, 0f, 1f);
        targetData.seVolume = EditorGUILayout.Slider("SE音量", targetData.seVolume, 0f, 1f);
        targetData.masterVolume = EditorGUILayout.Slider("マスター音量", targetData.masterVolume, 0f, 1f);
        targetData.screenSize = EditorGUILayout.IntField("スクリーンサイズ", targetData.screenSize);
        targetData.cameraSensitivity = EditorGUILayout.Slider("カメラ感度", targetData.cameraSensitivity, 0.1f, 10f);

        // 値が変更されたらUnityに変更を検知させる
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(targetData);
        }

        EditorGUILayout.Space();

        // ★変更箇所：targetData.GetSavePath() からパスを取得するように修正
        EditorGUILayout.HelpBox($"【保存先】\n{targetData.GetSavePath()}", MessageType.Info);
        EditorGUILayout.Space();

        // 3. セーブ＆ロードボタン
        GUILayout.BeginHorizontal();

        // セーブボタン
        if (GUILayout.Button("セーブする\n(JSONへ書出)", GUILayout.Height(40)))
        {
            targetData.SaveToJson();
            AssetDatabase.SaveAssets();
        }

        // ロードボタン
        if (GUILayout.Button("ロードする\n(JSONから読込)", GUILayout.Height(40)))
        {
            targetData.LoadFromJson();
            GUI.FocusControl(null);
        }

        GUILayout.EndHorizontal();
    }
}