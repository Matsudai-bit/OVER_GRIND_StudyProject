using System;
using UnityEngine;

/// <summary>
/// Transformの位置・回転・スケールをチェックポイントへ保存します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TransformCheckpointParticipant :
    CheckpointParticipant
{
    /// <summary>
    /// Transformの記録データです。
    /// </summary>
    [Serializable]
    private struct TransformSnapshot
    {
        [SerializeField]
        private Vector3 m_position;

        [SerializeField]
        private Quaternion m_rotation;

        [SerializeField]
        private Vector3 m_localScale;

        public Vector3 Position =>
            m_position;

        public Quaternion Rotation =>
            m_rotation;

        public Vector3 LocalScale =>
            m_localScale;

        public TransformSnapshot(
            Vector3 position,
            Quaternion rotation,
            Vector3 localScale)
        {
            m_position =
                position;

            m_rotation =
                rotation;

            m_localScale =
                localScale;
        }
    }

    // 保存対象
    [SerializeField, Header("Transform")]
    private Transform m_target;

    // スケールも保存するか
    [SerializeField]
    private bool m_captureScale = false;

    private void Awake()
    {
        if (m_target == null)
        {
            m_target =
                transform;
        }
    }

    /// <summary>
    /// Transform状態をJSONへ変換します。
    /// </summary>
    /// <returns>Transform状態のJSON。</returns>
    protected override string CaptureStateJson()
    {
        if (m_target == null)
        {
            return null;
        }

        TransformSnapshot snapshot =
            new TransformSnapshot(
                m_target.position,
                m_target.rotation,
                m_target.localScale);

        return JsonUtility.ToJson(
            snapshot);
    }

    /// <summary>
    /// JSONからTransform状態を復元します。
    /// </summary>
    /// <param name="json">復元するJSON。</param>
    /// <returns>
    /// true：復元しました。
    /// false：復元できませんでした。
    /// </returns>
    protected override bool RestoreStateJson(
        string json)
    {
        if (m_target == null
            || string.IsNullOrEmpty(json))
        {
            return false;
        }

        TransformSnapshot snapshot =
            JsonUtility.FromJson<TransformSnapshot>(
                json);

        m_target.SetPositionAndRotation(
            snapshot.Position,
            snapshot.Rotation);

        if (m_captureScale)
        {
            m_target.localScale =
                snapshot.LocalScale;
        }

        return true;
    }
}
