using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Vゲージの表示を管理します。
/// </summary>
public class VGaugeUI : MonoBehaviour
{
    [Header("Gauge")]

    // Vゲージ画像
    [SerializeField]
    private Image gaugeImage;

    // ゲージの最大値
    [SerializeField]
    private int maxGauge = 100;

    [Header("Debug")]

    // デバッグ入力を有効にするか
    [SerializeField]
    private bool useDebugInput = true;

    // デバッグ時に増減するゲージ量
    [SerializeField]
    private int debugGaugeStep = 1;

    /// <summary>
    /// 現在のゲージ値
    /// </summary>
    private int currentGauge;

    [Header("Repeat")]

    // 長押し開始までの時間
    [SerializeField]
    private float firstRepeatTime = 0.3f;

    // 長押し中の入力間隔
    [SerializeField]
    private float repeatInterval = 0.05f;

    [Header("Blade")]

    // Vゲージ周辺の刃
    // プロトタイプ用に回転速度を連動させる
    [SerializeField]
    private VBladeRotator bladeRotator;

    /// <summary>
    /// 長押し判定用タイマー
    /// </summary>
    private float repeatTimer;

    /// <summary>
    /// 長押し中か
    /// </summary>
    private bool isRepeating;

    /// <summary>
    /// 初期設定
    /// </summary>
    private void Start()
    {
        // ゲージを0で初期化する
        SetGauge(0);
    }

    /// <summary>
    /// 毎フレーム更新
    /// </summary>
    private void Update()
    {
        // デバッグ入力を使用しない場合は終了
        if (!useDebugInput)
            return;

        DebugInput();
    }

    /// <summary>
    /// デバッグ用入力
    /// </summary>
    private void DebugInput()
    {
        // コントローラー未接続なら終了
        if (Gamepad.current == null)
            return;

        // 十字キーまたは左スティック上入力
        bool increase =
            Gamepad.current.dpad.up.isPressed ||
            Gamepad.current.leftStick.up.isPressed;

        // 十字キーまたは左スティック下入力
        bool decrease =
            Gamepad.current.dpad.down.isPressed ||
            Gamepad.current.leftStick.down.isPressed;

        if (increase)
        {
            // ゲージを増加させる
            RepeatInput(debugGaugeStep);
        }
        else if (decrease)
        {
            // ゲージを減少させる
            RepeatInput(-debugGaugeStep);
        }
        else
        {
            // 入力が無くなったら長押し状態を解除する
            repeatTimer = 0f;
            isRepeating = false;

            // 刃を通常回転へ戻す
            if (bladeRotator != null)
            {
                bladeRotator.SetGaugeUsing(false);
            }
        }
    }

    /// <summary>
    /// 長押し入力を処理します。
    /// </summary>
    /// <param name="amount">増減量</param>
    private void RepeatInput(int amount)
    {
        // ゲージ減少中のみ刃を高速回転させる
        if (bladeRotator != null)
        {
            bladeRotator.SetGaugeUsing(amount < 0);
        }

        // 最初の入力は即時反映する
        if (!isRepeating)
        {
            AddGauge(amount);

            isRepeating = true;
            repeatTimer = firstRepeatTime;

            return;
        }

        // 長押しタイマーを更新する
        repeatTimer -= Time.deltaTime;

        // 一定時間ごとに入力を繰り返す
        if (repeatTimer <= 0f)
        {
            AddGauge(amount);

            repeatTimer = repeatInterval;
        }
    }

    /// <summary>
    /// ゲージ値を設定します。
    /// </summary>
    /// <param name="value">設定するゲージ値</param>
    public void SetGauge(int value)
    {
        // ゲージが0～最大値の範囲を超えないよう制限する
        currentGauge = Mathf.Clamp(
            value,
            0,
            maxGauge);

        // 表示を更新する
        UpdateGauge();
    }

    /// <summary>
    /// ゲージ値を増減します。
    /// </summary>
    /// <param name="amount">増減量</param>
    public void AddGauge(int amount)
    {
        // ゲージ減少中のみ刃を高速回転させる
        if (bladeRotator != null)
        {
            bladeRotator.SetGaugeUsing(amount < 0);
        }

        // 現在値へ増減量を加算する
        SetGauge(currentGauge + amount);
    }

    /// <summary>
    /// ゲージ表示を更新します。
    /// </summary>
    private void UpdateGauge()
    {
        // ゲージ画像が設定されていない場合は終了
        if (gaugeImage == null)
            return;

        // 現在のゲージ割合を画像へ反映する
        gaugeImage.fillAmount =
            currentGauge / (float)maxGauge;
    }

    /// <summary>
    /// 現在のゲージ値を取得します。
    /// </summary>
    public int GetGauge()
    {
        return currentGauge;
    }
}