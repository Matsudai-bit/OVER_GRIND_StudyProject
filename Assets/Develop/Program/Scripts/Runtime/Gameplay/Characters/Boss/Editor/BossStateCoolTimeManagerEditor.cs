using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// BossStateCoolTimeManagerのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(BossStateCoolTimeManager))]
public sealed class BossStateCoolTimeManagerEditor : Editor
{
    // Inspector表示用のクールタイム情報
    private readonly List<
        BossStateCoolTimeManager.CoolTimeDebugInfo>
        m_debugInfos = new();

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        BossStateCoolTimeManager coolTimeManager =
            (BossStateCoolTimeManager)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "クールタイム状況",
            EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "クールタイムはゲーム実行中に表示されます。",
                MessageType.Info);

            return;
        }

        coolTimeManager.GetCoolTimeDebugInfos(
            m_debugInfos);

        if (m_debugInfos.Count <= 0)
        {
            EditorGUILayout.LabelField(
                "クールタイム中のStateはありません。");

            return;
        }

        foreach (
            BossStateCoolTimeManager.CoolTimeDebugInfo debugInfo
            in m_debugInfos)
        {
            DrawCoolTime(debugInfo);
        }
    }

    /// <summary>
    /// Stateのクールタイムを描画します。
    /// </summary>
    /// <param name="debugInfo">クールタイム情報。</param>
    private static void DrawCoolTime(
        BossStateCoolTimeManager.CoolTimeDebugInfo debugInfo)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            debugInfo.StateName);

        EditorGUILayout.LabelField(
            $"{debugInfo.RemainingTime:F2} 秒",
            GUILayout.Width(80.0f));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 実行中はInspectorを継続更新します。
    /// </summary>
    /// <returns>
    /// true：継続更新します。
    /// false：通常更新します。
    /// </returns>
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}