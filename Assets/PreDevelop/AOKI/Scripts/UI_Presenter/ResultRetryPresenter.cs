using UnityEngine;

/// <summary>
/// リトライ回数の Model (データ) と View (表示) の更新イベントを仲介する Presenter クラスです。
/// </summary>
public class ResultRetryPresenter : MonoBehaviour
{
    [Header("★ MVP 参照設定")]
    [Tooltip("リトライ回数データを保持する Model コンポーネント")]
    [SerializeField] private ResultRetryModel m_retryModel;

    [Tooltip("リトライ回数の UI 描画を行う View コンポーネント")]
    [SerializeField] private ResultRetryView m_retryView;

    /// <summary>
    /// 初期化および参照の自動取得を行います。
    /// </summary>
    private void Awake()
    {
        InitializeReferences();
    }

    /// <summary>
    /// 有効化時に Model の変更イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        if (m_retryModel != null)
        {
            m_retryModel.OnRetryCountChanged += HandleRetryCountChanged;

            // 初回表示の同期
            HandleRetryCountChanged(m_retryModel.RetryCount);
        }
    }

    /// <summary>
    /// 無効化時に Model の変更イベント購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_retryModel != null)
        {
            m_retryModel.OnRetryCountChanged -= HandleRetryCountChanged;
        }
    }

    /// <summary>
    /// コンポーネント参照が未設定の場合に自動取得を試みます。
    /// </summary>
    private void InitializeReferences()
    {
        if (m_retryModel == null)
        {
            m_retryModel = GetComponent<ResultRetryModel>();
        }

        if (m_retryView == null)
        {
            m_retryView = GetComponent<ResultRetryView>();
        }
    }

    /// <summary>
    /// Model のリトライ回数変更通知を受け取って View に表示を要求します。
    /// </summary>
    /// <param name="newCount">変更後のリトライ回数。</param>
    private void HandleRetryCountChanged(int newCount)
    {
        if (m_retryView != null)
        {
            m_retryView.UpdateRetryCountDisplay(newCount);
        }
    }
}