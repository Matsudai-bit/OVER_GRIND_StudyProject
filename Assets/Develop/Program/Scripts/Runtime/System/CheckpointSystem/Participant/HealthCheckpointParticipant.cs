using System;
using UnityEngine;

/// <summary>
/// Healthの現在HPをチェックポイントへ保存・復元します。
/// </summary>
[DisallowMultipleComponent]
public sealed class HealthCheckpointParticipant :
    CheckpointParticipant
{
    /// <summary>
    /// Healthのチェックポイント記録データです。
    /// </summary>
    [Serializable]
    private struct HealthSnapshot
    {
        [SerializeField]
        private int m_currentHealth;

        /// <summary>
        /// 記録時の現在HPを取得します。
        /// </summary>
        public int CurrentHealth =>
            m_currentHealth;

        /// <summary>
        /// HPスナップショットを生成します。
        /// </summary>
        /// <param name="currentHealth">記録する現在HP。</param>
        public HealthSnapshot(
            int currentHealth)
        {
            m_currentHealth =
                currentHealth;
        }
    }

    // HP管理
    [SerializeField, Header("Health")]
    private Health m_health;

    private void Awake()
    {
        ResolveHealth();
    }

    /// <summary>
    /// 現在のHPをJSONへ変換します。
    /// </summary>
    /// <returns>
    /// HP状態を表すJSON。
    /// 取得できない場合はnull。
    /// </returns>
    protected override string CaptureStateJson()
    {
        if (!ResolveHealth()
            || !m_health.IsInitialized)
        {
            return null;
        }

        HealthSnapshot snapshot =
            new HealthSnapshot(
                m_health.CurrentHealth);

        return JsonUtility.ToJson(
            snapshot);
    }

    /// <summary>
    /// JSONからHPを復元します。
    /// </summary>
    /// <param name="json">復元するHPデータ。</param>
    /// <returns>
    /// true：復元しました。
    /// false：復元できませんでした。
    /// </returns>
    protected override bool RestoreStateJson(
        string json)
    {
        if (!ResolveHealth()
            || !m_health.IsInitialized
            || string.IsNullOrEmpty(json))
        {
            return false;
        }

        HealthSnapshot snapshot =
            JsonUtility.FromJson<HealthSnapshot>(
                json);

        m_health.SetHealth(
            snapshot.CurrentHealth);

        return true;
    }

    /// <summary>
    /// Health参照を取得します。
    /// </summary>
    /// <returns>
    /// true：Healthを取得できました。
    /// false：Healthを取得できませんでした。
    /// </returns>
    private bool ResolveHealth()
    {
        if (m_health != null)
        {
            return true;
        }

        m_health =
            GetComponent<Health>();

        if (m_health != null)
        {
            return true;
        }

        Debug.LogError(
            $"{nameof(Health)} が見つかりません。",
            this);

        return false;
    }
}
