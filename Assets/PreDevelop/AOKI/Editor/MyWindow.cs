using UnityEngine;
using UnityEditor; // エディタ拡張を使うために必須

public class MyWindow : EditorWindow
{
    // 画面上部のメニュー「Window」の中に「MyWindow」を追加する
    [MenuItem("Window/MyWindow")]
    public static void ShowWindow()
    {
        // ウィンドウを作成・表示する
        EditorWindow.GetWindow<MyWindow>("MyWindow");
    }

    // ウィンドウの中身（UI）を作る処理
    private void OnGUI()
    {
        // テキストを表示
        GUILayout.Label("データの保存や設定を行う自作ウィンドウ", EditorStyles.boldLabel);

        // ボタンを表示
        if (GUILayout.Button("テストボタン"))
        {
            Debug.Log("ボタンが押されました！");
        }
    }
}