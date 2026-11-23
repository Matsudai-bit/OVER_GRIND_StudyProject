#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// PlayerAudioSequentialTableAssetのInspectorを描画します。
/// </summary>
[CustomEditor(typeof(PlayerAudioSequentialTableAsset))]
public sealed class PlayerAudioSequentialTableAssetEditor
    : Editor
{
    // ActionIDと音声Assetの対応表
    private SerializedProperty m_entriesProperty;

    // 配列要素名をActionIDで表示するためのリスト
    private ReorderableList m_entryList;

    /// <summary>
    /// SerializedPropertyとReorderableListを初期化します。
    /// </summary>
    private void OnEnable()
    {
        m_entriesProperty =
            serializedObject.FindProperty(
                "m_entries");

        InitializeEntryList();
    }

    /// <summary>
    /// Player用音声対応表のInspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "PlayerAudioActionIDと音声Assetの対応表です。\n" +
            "各ActionIDにSequential / OneShotをそれぞれ登録できます。\n" +
            "State側はActionIDだけを指定し、ここで実際の音声Assetを差し替えます。",
            MessageType.Info);

        EditorGUILayout.Space(
            6.0f);

        DrawMissingActionWarning();

        if (GUILayout.Button(
                "不足しているActionIDを追加"))
        {
            AddMissingActionIDs();
        }

        EditorGUILayout.Space(
            6.0f);

        m_entryList?.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// ActionIDを要素名として表示するリストを初期化します。
    /// </summary>
    private void InitializeEntryList()
    {
        if (m_entriesProperty == null)
        {
            return;
        }

        m_entryList =
            new ReorderableList(
                serializedObject,
                m_entriesProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);

        m_entryList.drawHeaderCallback =
            rect =>
            {
                EditorGUI.LabelField(
                    rect,
                    "Player Audio Table");
            };

        m_entryList.elementHeightCallback =
            GetElementHeight;

        m_entryList.drawElementCallback =
            DrawElement;
    }

    /// <summary>
    /// 1要素分の高さを取得します。
    /// </summary>
    /// <param name="index">配列インデックス。</param>
    /// <returns>描画に必要な高さ。</returns>
    private float GetElementHeight(
        int index)
    {
        if (index < 0 ||
            index >= m_entriesProperty.arraySize)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        SerializedProperty elementProperty =
            m_entriesProperty.GetArrayElementAtIndex(
                index);

        return EditorGUI.GetPropertyHeight(
                   elementProperty,
                   includeChildren: true) +
               EditorGUIUtility.standardVerticalSpacing;
    }

    /// <summary>
    /// 配列要素をActionID名付きで描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="index">配列インデックス。</param>
    /// <param name="isActive">選択中か。</param>
    /// <param name="isFocused">フォーカス中か。</param>
    private void DrawElement(
        Rect rect,
        int index,
        bool isActive,
        bool isFocused)
    {
        if (index < 0 ||
            index >= m_entriesProperty.arraySize)
        {
            return;
        }

        SerializedProperty elementProperty =
            m_entriesProperty.GetArrayElementAtIndex(
                index);

        SerializedProperty actionIDProperty =
            elementProperty.FindPropertyRelative(
                "m_actionID");

        string elementLabel =
            GetActionIDLabel(
                actionIDProperty,
                index);

        rect.y +=
            EditorGUIUtility.standardVerticalSpacing *
            0.5f;

        EditorGUI.PropertyField(
            rect,
            elementProperty,
            new GUIContent(
                elementLabel),
            includeChildren: true);
    }

    /// <summary>
    /// 配列要素に表示するActionID名を取得します。
    /// </summary>
    /// <param name="actionIDProperty">
    /// ActionIDのSerializedProperty。
    /// </param>
    /// <param name="index">
    /// ActionIDを取得できない場合に使用する配列インデックス。
    /// </param>
    /// <returns>
    /// WALKINGなどのActionID名。
    /// </returns>
    private static string GetActionIDLabel(
        SerializedProperty actionIDProperty,
        int index)
    {
        if (actionIDProperty == null)
        {
            return $"Element {index}";
        }

        PlayerAudioActionID actionID =
            (PlayerAudioActionID)
            actionIDProperty.intValue;

        return actionID.ToString();
    }

    /// <summary>
    /// 対応表へ未登録のActionIDがある場合に警告します。
    /// </summary>
    private void DrawMissingActionWarning()
    {
        List<PlayerAudioActionID> missingIDs =
            GetMissingActionIDs();

        if (missingIDs.Count == 0)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "未登録ActionID: " +
            string.Join(", ", missingIDs),
            MessageType.Warning);
    }

    /// <summary>
    /// 対応表へ未登録のActionIDを追加します。
    /// </summary>
    private void AddMissingActionIDs()
    {
        List<PlayerAudioActionID> missingIDs =
            GetMissingActionIDs();

        foreach (PlayerAudioActionID actionID in
                 missingIDs)
        {
            int index =
                m_entriesProperty.arraySize;

            m_entriesProperty.InsertArrayElementAtIndex(
                index);

            SerializedProperty entryProperty =
                m_entriesProperty.GetArrayElementAtIndex(
                    index);

            SerializedProperty actionIDProperty =
                entryProperty.FindPropertyRelative(
                    "m_actionID");

            SerializedProperty sequentialAssetProperty =
                entryProperty.FindPropertyRelative(
                    "m_audioSequentialAsset");

            SerializedProperty oneShotAssetProperty =
                entryProperty.FindPropertyRelative(
                    "m_audioOneShotAsset");

            actionIDProperty.intValue =
                (int)actionID;

            sequentialAssetProperty.objectReferenceValue =
                null;

            oneShotAssetProperty.objectReferenceValue =
                null;
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 現在の対応表へ未登録のActionIDを取得します。
    /// </summary>
    private List<PlayerAudioActionID> GetMissingActionIDs()
    {
        HashSet<PlayerAudioActionID> registeredIDs =
            new HashSet<PlayerAudioActionID>();

        for (int i = 0;
             i < m_entriesProperty.arraySize;
             i++)
        {
            SerializedProperty entryProperty =
                m_entriesProperty.GetArrayElementAtIndex(
                    i);

            SerializedProperty actionIDProperty =
                entryProperty.FindPropertyRelative(
                    "m_actionID");

            registeredIDs.Add(
                (PlayerAudioActionID)
                actionIDProperty.intValue);
        }

        List<PlayerAudioActionID> missingIDs =
            new List<PlayerAudioActionID>();

        foreach (PlayerAudioActionID actionID in
                 Enum.GetValues(
                     typeof(PlayerAudioActionID)))
        {
            if (actionID == PlayerAudioActionID.NONE ||
                registeredIDs.Contains(actionID))
            {
                continue;
            }

            missingIDs.Add(
                actionID);
        }

        return missingIDs;
    }
}

#endif
