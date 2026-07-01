using UnityEditor; // UnityEditor の基本機能を使用
using UnityEditorInternal; // ReorderableList を使用するための using
using UnityEngine; // UnityEngine の基本機能

/// <summary>
/// EffectViewer のカスタムインスペクタを定義するクラス
/// </summary>
[CustomEditor(typeof(EffectViewer))] // EffectViewer 用のカスタムエディタを指定
public class EffectViewerEditor : Editor // Editor を継承
{
    private const float GUI_SPACE = 10f; // GUI の余白
    private const float BUTTON_HEIGHT = 25f; // ボタンの高さ

    private SerializedProperty m_effectManagerProp; // EffectManager を参照する SerializedProperty
    private ReorderableList m_effectList; // エフェクト一覧を表示する ReorderableList
    private SerializedObject m_managerSO; // EffectManager の SerializedObject
    private Object m_cachedManagerTarget; // キャッシュされた EffectManager 参照

    private void OnEnable() // エディタが有効化されたときに呼ばれる
    {
        m_effectManagerProp = serializedObject.FindProperty("m_effectManager"); // m_effectManager を取得
    }

    /// <summary>
    /// Inspector GUI の描画処理
    /// </summary>
    public override void OnInspectorGUI() // Inspector 描画メソッド
    {
        serializedObject.Update(); // SerializedObject を更新

        EditorGUILayout.PropertyField(m_effectManagerProp); // EffectManager のフィールドを描画

        if (m_effectManagerProp.objectReferenceValue != null) // Manager が設定されている場合
        {
            DrawEffectList(); // エフェクト一覧を描画

            GUILayout.Space(GUI_SPACE); // 余白を追加

            if (GUILayout.Button("Add Effect", GUILayout.Height(BUTTON_HEIGHT))) // Add ボタン
            {
                CreateNewEffect(); // 新規エフェクト作成処理
            }

            if (GUILayout.Button("Delete Effect", GUILayout.Height(BUTTON_HEIGHT))) // Delete ボタン
            {
                DeleteEffect(); // エフェクト削除処理
            }
        }

        serializedObject.ApplyModifiedProperties(); // 変更を適用
    }

    /// <summary>
    /// EffectManager のエフェクト配列を ReorderableList で描画する
    /// </summary>
    private void DrawEffectList() // エフェクト一覧描画処理
    {
        if (m_managerSO == null || m_cachedManagerTarget != m_effectManagerProp.objectReferenceValue) // Manager が変わった場合
        {
            m_managerSO = new SerializedObject(m_effectManagerProp.objectReferenceValue); // 新しい SerializedObject を作成
            m_cachedManagerTarget = m_effectManagerProp.objectReferenceValue; // キャッシュ更新
            m_effectList = null; // リストを再生成させるため null にする
        }

        m_managerSO.Update(); // Manager の SerializedObject を更新

        SerializedProperty effectsProp = m_managerSO.FindProperty("m_effects"); // m_effects 配列を取得

        if (m_effectList == null) // リストが未生成なら作成
        {
            m_effectList = new ReorderableList(m_managerSO, effectsProp, true, true, true, true); // ReorderableList を作成

            m_effectList.drawHeaderCallback = (Rect rect) => // ヘッダー描画
            {
                EditorGUI.LabelField(rect, "Effects"); // ヘッダー名
            };

            m_effectList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => // 各要素の描画
            {
                EditorGUI.PropertyField(rect, m_effectList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none); // 配列要素を描画
            };
        }

        m_effectList.DoLayoutList(); // リストを描画

        m_managerSO.ApplyModifiedProperties(); // Manager の変更を適用
    }

    /// <summary>
    /// 新しい EffectData アセットを作成する処理
    /// </summary>
    private void CreateNewEffect() // 新規エフェクト作成
    {
        string folder = "Assets/Develop/Effect"; // エフェクト保存フォルダ

        if (!AssetDatabase.IsValidFolder(folder)) // フォルダが存在しない場合
        {
            AssetDatabase.CreateFolder("Assets", "Effects"); // フォルダを作成
        }

        string defaultName = "NewEffect.asset"; // デフォルト名

        ProjectWindowUtil.StartNameEditingIfProjectWindowExists( // 名前編集開始
            0, // ID
            ScriptableObject.CreateInstance<CreateEffectAssetAction>(), // 作成アクション
            $"{folder}/{defaultName}", // パス
            null, // アイコン
            null // テンプレート
        );
    }

    /// <summary>
    /// 選択中のエフェクトを削除する処理
    /// </summary>
    private void DeleteEffect() // エフェクト削除処理
    {
        if (m_effectList == null || m_effectList.index < 0) // リストが無い or 選択されていない場合
        {
            return; // 何もしない
        }

        m_managerSO.Update(); // Manager の SerializedObject を更新

        SerializedProperty effectsProp = m_managerSO.FindProperty("m_effects"); // m_effects 配列を取得

        SerializedProperty element = effectsProp.GetArrayElementAtIndex(m_effectList.index); // 選択された要素
        Object obj = element.objectReferenceValue; // 削除対象のアセット

        effectsProp.DeleteArrayElementAtIndex(m_effectList.index); // 配列から削除

        if (obj != null) // アセットが存在する場合
        {
            string path = AssetDatabase.GetAssetPath(obj); // アセットのパスを取得
            if (!string.IsNullOrEmpty(path)) // パスが有効なら
            {
                AssetDatabase.DeleteAsset(path); // アセットを削除
                AssetDatabase.SaveAssets(); // 保存
            }
        }

        m_managerSO.ApplyModifiedProperties(); // Manager の変更を適用

        m_effectList.index = -1; // 選択を解除
    }
}
