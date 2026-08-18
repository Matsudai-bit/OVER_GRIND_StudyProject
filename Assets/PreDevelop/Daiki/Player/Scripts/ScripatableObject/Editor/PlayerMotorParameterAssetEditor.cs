#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// プレイヤーの物理移動パラメータをInspectorに表示します。
/// </summary>
[CustomEditor(typeof(PlayerMotorParameterAsset))]
public sealed class PlayerMotorParameterAssetEditor : Editor
{
    // 計算時に使用する最小時間
    private const float MIN_TIME = 0.01f;

    /// <summary>
    /// パラメータ編集用Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerMotorParameterAsset parameterAsset =
            (PlayerMotorParameterAsset)target;

        PlayerMotorParameters parameters =
            parameterAsset.CreateParameters();

        float acceleration =
            parameters.MaxMoveSpeed /
            Mathf.Max(parameters.TimeToMaxSpeed, MIN_TIME);

        float deceleration =
            parameters.MaxMoveSpeed /
            Mathf.Max(parameters.TimeToStop, MIN_TIME);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "計算値プレビュー",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField(
                "加速度",
                acceleration);

            EditorGUILayout.FloatField(
                "減速度",
                deceleration);
        }

        EditorGUILayout.HelpBox(
            "加速度と減速度は、最高速度と到達時間から自動計算されます。",
            MessageType.Info);
    }
}

#endif