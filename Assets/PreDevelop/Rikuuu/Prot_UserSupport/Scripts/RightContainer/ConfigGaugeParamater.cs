using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConfigGaugeParamater : MonoBehaviour
{
    // 上下判定のしきい値
    private const float NAVIGATE_THRESHOLD = 0.75f;

    // 何の値かを格納する
    [SerializeField]
    private string m_paramaterName = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_paramaterText;

    // 最大値
    [SerializeField]
    private float MAX_VALUE = 1.0f;
    // 最小値
    [SerializeField]
    private float MIN_VALUE = 0.0f;
    // 値の変化量
    [SerializeField]
    private float VALUE_AMOUNT = 0.1f;
    // 初期の値
    [SerializeField]
    private float m_defaultValue = 0.5f;
    // 現在の値を表示するテキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_valueText;

    // パラメータ変更時に呼び出す関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent<float> m_handleParameterChange;

    [Header("テクスチャ関連")]
    // ゲージテクスチャイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_gaugeImage;

    [Header("入力判定関連")]
    // 上キーが押される判定
    [SerializeField]
    private InputActionReference m_navigateActionRef;
    // 前フレームからのスティック移動距離
    private Vector2 m_previousNav = Vector2.zero;

    // 値の変更状態を固定・解除の有無
    private bool m_isLocked = true;

    private void OnEnable()
    {
        if (m_navigateActionRef == null)
        {
            Debug.LogWarning($"{gameObject.name}: {m_navigateActionRef} が設定されていません。Inspectorで割り当ててください。", this);
            return;
        }

        // 有効にする
        m_navigateActionRef?.action.Enable();
    }

    private void OnDisable()
    {
        // 無効にする
        m_navigateActionRef?.action.Disable();
    }

    private void Start()
    {
        // 文字の置き換え
        m_valueText.text = m_defaultValue.ToString();
        m_paramaterText.text = m_paramaterName;

        // 値の初期化を通知する
        if (m_handleParameterChange.GetPersistentEventCount() > 0)
        {
            m_handleParameterChange.Invoke(m_defaultValue);
        }
    }

    private void Update()
    {
        // 入力方向
        Vector2 nav = m_navigateActionRef.action.ReadValue<Vector2>();

        // 固定されていなければ
        if (!m_isLocked)
        {
            // 左キーが押されたら
            if (nav.x < -NAVIGATE_THRESHOLD &&
                m_previousNav.x >= -NAVIGATE_THRESHOLD)
            {
                // 値を減少させる
                m_defaultValue -= VALUE_AMOUNT;

                // 値変更を通知する
                if (m_handleParameterChange.GetPersistentEventCount() > 0)
                {
                    m_handleParameterChange.Invoke(m_defaultValue);
                }
            }
            // 右キーが押されたら
            if (nav.x > NAVIGATE_THRESHOLD &&
                m_previousNav.x <= NAVIGATE_THRESHOLD)
            {
                // 値を増加させる
                m_defaultValue += VALUE_AMOUNT;

                // 値変更を通知する
                if (m_handleParameterChange.GetPersistentEventCount() > 0)
                {
                    m_handleParameterChange.Invoke(m_defaultValue);
                }
            }
        }

        // 値の制限
        m_defaultValue = Mathf.Clamp(m_defaultValue, MIN_VALUE, MAX_VALUE);

        // 文字の置き換え
        m_valueText.text = m_defaultValue.ToString("F1");

        // イメージの割合表示
        m_gaugeImage.fillAmount = m_defaultValue;

        // 今フレームの値を保存し、次フレームの比較に使う
        m_previousNav = nav;
    }

    public void OnCursor()
    {
        m_isLocked = false;
    }

    public void OnCursorExit()
    {
        m_isLocked = true;
    }
}
