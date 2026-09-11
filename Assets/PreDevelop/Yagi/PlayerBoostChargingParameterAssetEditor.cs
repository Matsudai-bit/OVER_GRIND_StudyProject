#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// ブーストチャージ用パラメータをInspectorに表示します。
/// </summary>
[CustomEditor(typeof(PlayerBoostChargingParameterAsset))]
public sealed class PlayerBoostChargingParameterAssetEditor : Editor
{
    /// <summary>
    /// パラメータ編集用Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        // --------------------------------------------------------
        // 通常のInspector
        // --------------------------------------------------------

        DrawDefaultInspector();


        // --------------------------------------------------------
        // パラメータ取得
        // --------------------------------------------------------

        PlayerBoostChargingParameterAsset parameterAsset =
            (PlayerBoostChargingParameterAsset)target;


        // --------------------------------------------------------
        // 計算値
        // --------------------------------------------------------

        float maxChargeTime =
            parameterAsset.MaxChargeTime;

        float minBoostChargeRate =
            parameterAsset.MinBoostChargeRate;

        // 最低チャージ率に到達するまでの時間
        float minimumChargeTime =
            maxChargeTime *
            minBoostChargeRate;

        // チャージ開始時の移動速度倍率
        float chargeMoveSpeedRate =
            parameterAsset.ChargeMoveSpeedRate;

        // --------------------------------------------------------
        // プレビュー表示
        // --------------------------------------------------------

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "計算値プレビュー",
            EditorStyles.boldLabel);


        using (new EditorGUI.DisabledScope(true))
        {
            // ----------------------------------------------------
            // チャージ
            // ----------------------------------------------------

            EditorGUILayout.LabelField(
                "チャージ設定",
                EditorStyles.boldLabel);

            EditorGUILayout.FloatField(
                "最大チャージ時間",
                maxChargeTime);

            EditorGUILayout.FloatField(
                "最低チャージ時間",
                minimumChargeTime);

            EditorGUILayout.FloatField(
                "最低チャージ割合",
                minBoostChargeRate);


            EditorGUILayout.Space();


            // ----------------------------------------------------
            // 移動
            // ----------------------------------------------------

            EditorGUILayout.LabelField(
                "移動設定",
                EditorStyles.boldLabel);

            EditorGUILayout.FloatField(
                "チャージ中速度倍率",
                chargeMoveSpeedRate);


            EditorGUILayout.Space();


            // ----------------------------------------------------
            // ドリフト
            // ----------------------------------------------------

            EditorGUILayout.LabelField(
                "ドリフト設定",
                EditorStyles.boldLabel);

            EditorGUILayout.FloatField(
                "チャージ開始時の旋回速度",
                parameterAsset.DriftTurnSpeedAtChargeStart);

            EditorGUILayout.FloatField(
                "フルチャージ時の旋回速度",
                parameterAsset.DriftTurnSpeedAtFullCharge);

            EditorGUILayout.FloatField(
                "向き変更速度",
                parameterAsset.FacingRotationSpeed);

            EditorGUILayout.FloatField(
                "ステアリングデッドゾーン",
                parameterAsset.SteeringDeadZone);


            EditorGUILayout.Space();


            // ----------------------------------------------------
            // カメラ
            // ----------------------------------------------------

            EditorGUILayout.LabelField(
                "カメラ設定",
                EditorStyles.boldLabel);

            EditorGUILayout.FloatField(
                "カメラ追従速度",
                parameterAsset.CameraDriftLookTurnSpeed);

            EditorGUILayout.FloatField(
                "カメラブレンド率",
                parameterAsset.CameraDriftLookBlendRate);


            EditorGUILayout.Space();


            // ----------------------------------------------------
            // その他
            // ----------------------------------------------------

            EditorGUILayout.LabelField(
                "その他",
                EditorStyles.boldLabel);

            EditorGUILayout.FloatField(
                "入力停止猶予時間",
                parameterAsset.NoMoveInputGraceTime);

            EditorGUILayout.FloatField(
                "速度ログ間隔",
                parameterAsset.SpeedLogInterval);
        }


        // --------------------------------------------------------
        // 補足
        // --------------------------------------------------------

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "最低チャージ時間は、最大チャージ時間 × 最低チャージ割合で計算されます。\n" +
            "ドリフト旋回速度は、チャージ開始時からフルチャージ時にかけて徐々に変化します。",
            MessageType.Info);
    }
}

#endif