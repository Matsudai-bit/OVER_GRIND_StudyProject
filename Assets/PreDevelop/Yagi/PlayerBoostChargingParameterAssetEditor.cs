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

        float movingStartChargeTime =
            parameterAsset.MovingStartChargeTime;

        float stationaryStartChargeTime =
            parameterAsset.StationaryStartChargeTime;

        float minBoostChargeRate =
            parameterAsset.MinBoostChargeRate;

        // 移動中開始時の最低チャージ率到達時間
        float movingMinimumChargeTime =
            movingStartChargeTime *
            minBoostChargeRate;

        // 停止中開始時の最低チャージ率到達時間
        float stationaryMinimumChargeTime =
            stationaryStartChargeTime *
            minBoostChargeRate;

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
                "移動中開始 最大チャージ時間",
                movingStartChargeTime);

            EditorGUILayout.FloatField(
                "移動中開始 最低チャージ時間",
                movingMinimumChargeTime);

            EditorGUILayout.FloatField(
                "停止中開始 最大チャージ時間",
                stationaryStartChargeTime);

            EditorGUILayout.FloatField(
                "停止中開始 最低チャージ時間",
                stationaryMinimumChargeTime);

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
            "移動中開始と停止中開始で最大チャージ時間を個別に設定できます。\n" +
            "最低チャージ時間は、それぞれの最大チャージ時間 × 最低チャージ割合で計算されます。\n" +
            "停止中からチャージを開始した場合、チャージ中のステアリングはできません。",
            MessageType.Info);
    }
}

#endif