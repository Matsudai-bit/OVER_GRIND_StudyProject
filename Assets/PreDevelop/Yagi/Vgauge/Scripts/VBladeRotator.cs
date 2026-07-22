using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Vゲージ周辺の刃の回転を管理します。
/// </summary>
public class VBladeRotator : MonoBehaviour
{
    [Header("Rotation")]

    // 通常時の回転速度
    [SerializeField]
    private float normalRotationSpeed = 30f;

    // Vゲージ使用中の回転速度
    [SerializeField]
    private float fastRotationSpeed = 180f;

    [Header("Easing")]

    // 回転速度変更時のイージング
    [SerializeField]
    private Ease ease = Ease.InOutSine;

    // 回転速度を変更する時間
    [SerializeField]
    private float speedChangeDuration = 0.5f;

    [Header("Prototype")]

    // デバッグ入力を有効にするか
    [SerializeField]
    private bool usePrototypeInput = true;

    /// <summary>
    /// 現在の回転速度
    /// </summary>
    private float currentRotationSpeed;

    /// <summary>
    /// 回転速度変更Tween
    /// </summary>
    private Tween speedTween;

    /// <summary>
    /// 現在Vゲージ使用中か
    /// </summary>
    private bool isUsingGauge = false;

    /// <summary>
    /// 初期設定
    /// </summary>
    private void Start()
    {
        // 初期状態は通常速度
        currentRotationSpeed = normalRotationSpeed;
    }

    /// <summary>
    /// 毎フレーム更新
    /// </summary>
    private void Update()
    {
        // 常に刃を回転させる
        RotateBlade();

        // デバッグ入力を使用しない場合は終了
        if (!usePrototypeInput)
            return;

        bool usingGauge = false;

        // キーボード入力
        if (Keyboard.current != null)
        {
            usingGauge |= Keyboard.current.spaceKey.isPressed;
        }

        // コントローラー入力
        if (Gamepad.current != null)
        {
            // L1ボタンを押している間はVゲージ使用中とする
            usingGauge |= Gamepad.current.leftShoulder.isPressed;
        }

        // Vゲージ使用状態を更新する
        SetGaugeUsing(usingGauge);
    }

    /// <summary>
    /// 刃を回転させます。
    /// </summary>
    private void RotateBlade()
    {
        // 現在の回転速度で反時計回りに回転する
        transform.Rotate(
            0f,
            0f,
            currentRotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 回転速度を変更します。
    /// </summary>
    /// <param name="targetSpeed">変更後の回転速度</param>
    private void ChangeRotationSpeed(float targetSpeed)
    {
        // 前回のTweenが残っている場合は停止する
        speedTween?.Kill();

        // 指定時間かけて回転速度を変更する
        speedTween = DOTween.To(
            () => currentRotationSpeed,
            value =>
            {
                currentRotationSpeed = value;
            },
            targetSpeed,
            speedChangeDuration
        )
        .SetEase(ease);
    }

    /// <summary>
    /// Vゲージ使用状態を設定します。
    /// </summary>
    /// <param name="usingGauge">Vゲージ使用中か</param>
    public void SetGaugeUsing(bool usingGauge)
    {
        // 状態が変わらない場合は何もしない
        if (isUsingGauge == usingGauge)
            return;

        isUsingGauge = usingGauge;

        // Vゲージ使用中は高速回転、それ以外は通常回転へ変更する
        ChangeRotationSpeed(
            usingGauge
                ? fastRotationSpeed
                : normalRotationSpeed);
    }

    /// <summary>
    /// オブジェクト破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        // Tweenが残らないよう停止する
        speedTween?.Kill();
    }
}