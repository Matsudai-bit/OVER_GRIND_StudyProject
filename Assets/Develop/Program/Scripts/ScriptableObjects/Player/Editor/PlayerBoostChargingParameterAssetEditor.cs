#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// ブーストチャージ用パラメータを日本語で編集・確認するInspectorを表示します。
/// </summary>
[CustomEditor(typeof(PlayerBoostChargingParameterAsset))]
public sealed class PlayerBoostChargingParameterAssetEditor : Editor
{
    /// <summary>
    /// パラメータ編集用Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("チャージ基本設定", EditorStyles.boldLabel);
        DrawParameter("m_movingStartChargeTime", "移動中開始：最大チャージ時間", "移動中にチャージを開始した場合の最大時間（秒）。");
        DrawParameter("m_stationaryStartChargeTime", "停止中開始：最大チャージ時間", "停止中にチャージを開始した場合の最大時間（秒）。");
        DrawParameter("m_minBoostChargeRate", "ダッシュに必要な最低チャージ割合", "この割合以上でチャージを離すとブーストダッシュへ移行します。");
        DrawParameter("m_chargeDeceleration", "チャージ中の減速度", "チャージ中に毎秒減少する速度（m/s²）。");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("入力判定", EditorStyles.boldLabel);
        DrawParameter("m_freeSteeringAngle", "方向固定前の自由旋回角度", "移動中チャージ開始時の進行方向から左右へこの角度以内なら自由に切り返せます。現在の進行方向が超えると、その側へ旋回方向を固定します（度）。");
        DrawParameter("m_steeringDeadZone", "スティックのデッドゾーン", "この値以下のスティック入力は無入力として扱います。");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ドリフト旋回", EditorStyles.boldLabel);
        DrawParameter("m_driftOutwardDuration", "外側へ膨らむ時間", "通常入力時に、入力と逆側へ膨らむ時間（秒）。");
        DrawParameter("m_driftOutwardTurnRate", "外側へ膨らむ旋回倍率", "通常入力開始時の逆向き旋回の強さ。大きいほど外側へ膨らみます。");
        DrawParameter("m_driftTurnSpeedAtChargeStart", "チャージ開始時の旋回速度", "チャージ開始直後の最大旋回速度（度/秒）。");
        DrawParameter("m_driftTurnSpeedAtFullCharge", "フルチャージ時の旋回速度", "フルチャージ時の最大旋回速度（度/秒）。");
        DrawParameter("m_driftForwardTurnRate", "進行方向入力の旋回倍率", "最初に決めた進行方向へ入力したときの倍率。1で基準の旋回速度です。");
        DrawParameter("m_driftNeutralTurnRate", "無入力の旋回倍率", "無入力時に維持する内向き旋回の倍率。0で旋回しません。");
        DrawParameter("m_driftCounterTurnRate", "逆入力の旋回倍率", "逆入力でも旋回方向は維持されます。値を小さくするほど緩やかに曲がります。0で旋回しません。");
        DrawParameter("m_counterInputMovementAngle", "逆入力の外側移動角度", "逆入力中に移動方向を旋回外側へ傾ける角度。右旋回なら左側、左旋回なら右側へ移動します。");
        DrawParameter("m_driftSteeringResponseTime", "旋回入力の反応時間", "旋回入力へ到達するまでの時間（秒）。大きいほど入力変化が穏やかになります。");
        DrawParameter("m_facingRotationSpeed", "プレイヤーの向き変更速度", "プレイヤーの見た目が進行方向へ向く速度（度/秒）。");
        DrawParameter("m_stationaryChargeRotationSpeed", "停止中チャージの回転速度", "停止中に左右入力したときの回転速度（度/秒）。");
        DrawParameter("m_movingChargeSidewaysLookAngle", "移動中モデルの横滑り角度", "移動中チャージでモデルが進行方向に対して横を向く最大角度（度）。");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("カメラ", EditorStyles.boldLabel);
        DrawParameter("m_counterSteeringLookRate", "逆入力・無入力時の体の角度割合", "0で進行方向、1で通常入力と同じ横向き角度。旋回倍率とは独立して調整します。");
        DrawParameter("m_movingChargeCameraLookAngle", "移動中カメラの先読み角度", "チャージ中にカメラがカーブ内側を先読みする角度（度）。");
        DrawParameter("m_movingChargeCameraLookDuration", "移動中カメラの追従時間", "カメラが目標方向へ追従する時間（秒）。");
        DrawParameter("m_cameraDriftLookTurnSpeed", "停止中・解除時のカメラ旋回速度", "停止中チャージとチャージ解除時にカメラが向きを変える速度（度/秒）。");
        DrawParameter("m_cameraDriftLookBlendRate", "停止中カメラの追従割合", "停止中チャージでプレイヤーの向きをカメラへ反映する割合（0～1）。");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("その他", EditorStyles.boldLabel);
        DrawParameter("m_noMoveInputGraceTime", "移動入力停止の猶予時間", "移動入力が一時的に途切れたとき、待機状態へ移るまでの猶予（秒）。");
        DrawParameter("m_speedLogInterval", "速度ログの出力間隔", "チャージ中の速度ログを出力する間隔（秒）。");

        serializedObject.ApplyModifiedProperties();

        PlayerBoostChargingParameterAsset parameterAsset =
            (PlayerBoostChargingParameterAsset)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("計算値プレビュー", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField(
                "移動中開始：最低チャージ時間（秒）",
                parameterAsset.MovingStartChargeTime * parameterAsset.MinBoostChargeRate);
            EditorGUILayout.FloatField(
                "停止中開始：最低チャージ時間（秒）",
                parameterAsset.StationaryStartChargeTime * parameterAsset.MinBoostChargeRate);
        }

        EditorGUILayout.HelpBox(
            "移動中は、チャージボタン押下時から実際の移動方向が「方向固定前の自由旋回角度」以上変化した場合のみチャージを開始できます。" +
            "進行方向入力・無入力・逆入力は、それぞれ個別の旋回倍率で調整できます。" +
            "逆入力でも最初に決めた旋回方向を維持し、実際の移動方向だけを設定角度だけ外側へ傾けます。",
            MessageType.Info);
    }

    /// <summary>指定したフィールドを日本語ラベルと説明付きで描画します。</summary>
    private void DrawParameter(string propertyName, string label, string tooltip)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label, tooltip));
    }
}

#endif
