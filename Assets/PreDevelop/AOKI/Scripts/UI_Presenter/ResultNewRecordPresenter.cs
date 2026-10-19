using UnityEngine;

/// <summary>
/// 今回のタイムと前のタイムを見比べ、新記録時のみ Blinker_UI（NEW RECORD）を点滅表示させる Presenter
/// </summary>
public class ResultNewRecordPresenter : MonoBehaviour
{
    [Header("★ View と Model")]
    [SerializeField] private Blinker_UI m_blinkerView;
    [SerializeField] private ResultTimeModel m_timeModel;

    [Header("★ 演出タイミング")]
    [Tooltip("タイム回転（カウントアップ）演出が終わってから NEW RECORD が出現するまでの遅延時間（秒）")]
    [SerializeField] private float m_appearanceDelay = 1.0f;

    private void Awake()
    {
        if (m_blinkerView == null) m_blinkerView = GetComponent<Blinker_UI>();
        if (m_timeModel == null) m_timeModel = FindFirstObjectByType<ResultTimeModel>();
    }

    private void OnEnable()
    {
        if (m_timeModel != null)
        {
            m_timeModel.OnTimeChanged += OnTimeChanged;
        }
    }

    private void OnDisable()
    {
        if (m_timeModel != null)
        {
            m_timeModel.OnTimeChanged -= OnTimeChanged;
        }
    }

    private void Start()
    {
        EvaluateAndApply();
    }

    private void OnTimeChanged(float newTime)
    {
        EvaluateAndApply();
    }

    /// <summary>
    /// 新旧タイムを比較し、NEW RECORD の表示/非表示・点滅制御を行います
    /// </summary>
    public void EvaluateAndApply()
    {
        if (m_blinkerView == null || m_timeModel == null) return;

        // Model 側で比較した判定結果を取得
        bool isNewRecord = m_timeModel.IsNewRecord;

        // 新記録ならスライドイン＆点滅再生、更新なしなら非表示
        m_blinkerView.SetNewRecord(isNewRecord, m_appearanceDelay);
    }
}