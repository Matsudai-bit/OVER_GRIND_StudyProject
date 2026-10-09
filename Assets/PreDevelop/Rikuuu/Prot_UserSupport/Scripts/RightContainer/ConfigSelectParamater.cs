using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConfigSelectParamater : MonoBehaviour
{
    // 上下判定のしきい値
    private const float NAVIGATE_THRESHOLD = 0.75f;

    // 何の値かを格納する
    [SerializeField]
    private string m_paramaterName = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_paramaterText;

    // 何の値かを格納する
    [SerializeField]
    private string[] m_selectName;
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_selectText;

    // 初期の値
    [SerializeField]
    private int m_defaultValue = 0;

    // パラメータ変更時に呼び出す関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent<string> m_handleParameterChange;

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
        // 配列が空、または初期値が範囲外の場合に備えてクランプする
        if (m_selectText == null || m_selectName.Length == 0)
        {
            Debug.LogError($"{name}: m_selectTexture が未設定です", this);
            return;
        }

        // 表示するテクスチャの初期化
        m_paramaterText.text = m_paramaterName;
        m_selectText.text = m_selectName[m_defaultValue];

        // 値の初期化を通知する
        if(m_handleParameterChange.GetPersistentEventCount() > 0)
        {
            m_handleParameterChange.Invoke(
                m_selectName[m_defaultValue]
            );
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
                m_defaultValue--;

                // 値が0以下になった場合
                if (m_defaultValue < 0)
                {
                    // 最後尾に戻す
                    m_defaultValue = m_selectName.Length - 1;
                }

                // 値変更を通知する
                if (m_handleParameterChange.GetPersistentEventCount() > 0)
                {
                    m_handleParameterChange.Invoke(
                        m_selectName[m_defaultValue]
                    );
                }
            }
            // 右キーが押されたら
            if (nav.x > NAVIGATE_THRESHOLD &&
                m_previousNav.x <= NAVIGATE_THRESHOLD)
            {
                // 値を増加させる
                m_defaultValue++;

                // 値が配列の長さを超過した場合
                if(m_defaultValue >= m_selectName.Length)
                {
                    // 最初に戻す
                    m_defaultValue = 0;
                }

                // 値変更を通知する
                if (m_handleParameterChange.GetPersistentEventCount() > 0)
                {
                    m_handleParameterChange.Invoke(
                        m_selectName[m_defaultValue]
                    );
                }
            }
        }

        // 値に合わせたテクスチャを表示する
        m_selectText.text = m_selectName[m_defaultValue];

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
