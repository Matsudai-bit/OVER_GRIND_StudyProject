using UnityEngine;
using UnityEngine.InputSystem;

public class StorySkipNavigator : MonoBehaviour
{
    [Header("スキップ時に実行する関数")]
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onSkipAction;

    [SerializeField]
    private bool m_requireAllKeys = false;

    [Header("スキップ時に押すキー")]
    [SerializeField]
    private InputActionReference m_skipActionRef;

    [Header("スキップ関数を実行するまでの時間")]
    [SerializeField]
    private float m_skipAmount = 0.5f;

    // キーが同時押しされたか確認する
    private AllPressedChecker m_enterChecker;

    // ゲージ
    private float m_gauge = 0.0f;

    private void Awake()
    {
        if (m_enterChecker == null && m_requireAllKeys)
        {
            m_enterChecker = new AllPressedChecker(m_skipActionRef);
        }

        // キー入力を有効にする
        m_skipActionRef.action.Enable();
    }

    private void Update()
    {
        if (m_requireAllKeys)
        {
            // すべてのキーが押されたら
            if(IsEnterPressed())
            {
                m_gauge += Time.deltaTime;
            }
            else
            {
                m_gauge -= Time.deltaTime;
            }
        }
        else
        {
            // 該当するキーが押されていたら
            if(m_skipActionRef.action.WasPressedThisFrame())
            {
                m_gauge += Time.deltaTime;
            }
            else
            {
                m_gauge -= Time.deltaTime;
            }
        }

        // 値を制限する
        m_gauge = Mathf.Clamp(m_gauge, 0.0f, m_skipAmount);

        // ゲージが一定量溜まったら
        if(m_gauge >= m_skipAmount)
        {
            // 関数を実行する
            m_onSkipAction.Invoke();
        }
    }

    private bool IsEnterPressed()
    {
        return m_enterChecker != null && m_enterChecker.IsPressedNow(true);
    }
}
