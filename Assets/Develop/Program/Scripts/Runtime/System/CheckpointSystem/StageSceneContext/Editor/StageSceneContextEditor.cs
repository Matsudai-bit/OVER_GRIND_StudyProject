using UnityEditor;
using UnityEngine;

/// <summary>
/// StageSceneContextのInspectorを拡張します。
/// </summary>
[CustomEditor(typeof(StageSceneContext))]
public sealed class StageSceneContextEditor : Editor
{
    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        StageSceneContext context =
            target as StageSceneContext;

        if (context == null)
        {
            return;
        }

        DrawDescription();

        EditorGUILayout.Space();

        DrawRuntimeContext(context);

        EditorGUILayout.Space();

        DrawDebugControls(context);
    }

    /// <summary>
    /// Contextの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "このAssetはScene間で一時的な開始情報を受け渡します。\n"
            + "値はAssetへ保存されず、ランタイム中のみ保持されます。",
            MessageType.Info);
    }

    /// <summary>
    /// 現在のランタイムContextを表示します。
    /// </summary>
    /// <param name="context">
    /// 表示対象のContext。
    /// </param>
    private void DrawRuntimeContext(
        StageSceneContext context)
    {
        EditorGUILayout.LabelField(
            "Runtime Context",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.EnumPopup(
                "Entry Type",
                context.EntryType);

            StageCheckpointSnapshot snapshot =
                context.CheckpointSnapshot;

            bool hasCheckpoint =
                snapshot != null;

            EditorGUILayout.Toggle(
                "Has Checkpoint",
                hasCheckpoint);

            if (hasCheckpoint)
            {
                EditorGUILayout.TextField(
                    "Checkpoint ID",
                    snapshot.CheckpointId);

                EditorGUILayout.EnumPopup(
                    "Boss Phase",
                    snapshot.BossPhase);

                EditorGUILayout.IntField(
                    "Participant Count",
                    snapshot.ParticipantSnapshots.Count);
            }
        }
    }

    /// <summary>
    /// デバッグ用のContext変更UIを表示します。
    /// </summary>
    /// <param name="context">
    /// 操作対象のContext。
    /// </param>
    private void DrawDebugControls(
        StageSceneContext context)
    {
        EditorGUILayout.LabelField(
            "Debug Control",
            EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Contextの変更はPlay Mode中のみ可能です。",
                MessageType.Warning);

            return;
        }

        if (GUILayout.Button(
            "Set New Game"))
        {
            context.SetNewGame();

            EditorUtility.SetDirty(
                context);
        }

        if (GUILayout.Button(
            "Set Restart"))
        {
            context.SetRestart();

            EditorUtility.SetDirty(
                context);
        }

        if (GUILayout.Button(
            "Clear Context"))
        {
            context.Clear();

            EditorUtility.SetDirty(
                context);
        }

        StageCheckpointSnapshot snapshot =
            context.CheckpointSnapshot;

        using (new EditorGUI.DisabledScope(
            snapshot == null))
        {
            if (GUILayout.Button(
                "Set Checkpoint"))
            {
                context.SetCheckpoint(
                    snapshot);

                EditorUtility.SetDirty(
                    context);
            }
        }

        if (snapshot == null)
        {
            EditorGUILayout.HelpBox(
                "CheckpointはStageCheckpointControllerが"
                + "Snapshotを生成した後に設定できます。",
                MessageType.Info);
        }

        // ランタイム値をリアルタイム表示
        Repaint();
    }
}