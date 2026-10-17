using UnityEditor;
using UnityEngine;

/// <summary>Player用ParameterAssetの共通Inspectorを描画します。</summary>
public abstract class PlayerParameterAssetEditorBase : Editor
{
    /// <summary>ParameterAssetの全シリアライズ項目と利用方法を表示します。</summary>
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "対応ComponentへこのAssetを設定するとAsset値を使用し、未設定の場合はComponent内の既定値を使用します。",
            MessageType.Info);
        DrawDefaultInspector();
    }
}
