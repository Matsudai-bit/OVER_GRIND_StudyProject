using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数のボス破壊可能部位の全破壊を検知します。
/// </summary>
[Serializable]
public sealed class BossBreakablePartGroup
{
    // グループとして監視する破壊可能部位
    [SerializeField]
    private List<BossBreakablePart> m_parts =
        new();

    // イベント購読済み部位
    private readonly HashSet<BossBreakablePart>
        m_registeredParts =
            new();

    // 全部位が破壊済みか
    private bool m_areAllPartsBroken;

    /// <summary>
    /// グループ内のすべての部位が破壊されたときに通知されます。
    /// </summary>
    public event Action<BossBreakablePartGroup>
        AllPartsBroken;

    /// <summary>
    /// グループ内のすべての部位が破壊済みか取得します。
    /// </summary>
    public bool AreAllPartsBroken =>
        m_areAllPartsBroken;

    /// <summary>
    /// 破壊監視を開始します。
    /// </summary>
    public void StartMonitoring()
    {
        StopMonitoring();

        m_areAllPartsBroken =
            false;

        foreach (BossBreakablePart part
                 in m_parts)
        {
            if (part == null)
            {
                continue;
            }

            if (!m_registeredParts.Add(
                    part))
            {
                continue;
            }

            part.Broken +=
                HandlePartBroken;
        }

        CheckAllPartsBroken();
    }

    /// <summary>
    /// 破壊監視を終了します。
    /// </summary>
    public void StopMonitoring()
    {
        foreach (BossBreakablePart part
                 in m_registeredParts)
        {
            if (part == null)
            {
                continue;
            }

            part.Broken -=
                HandlePartBroken;
        }

        m_registeredParts.Clear();
    }

    /// <summary>
    /// グループの完了状態をリセットします。
    /// </summary>
    public void Reset()
    {
        m_areAllPartsBroken =
            false;
    }

    /// <summary>
    /// 現在の部位状態から全破壊判定を更新します。
    /// </summary>
    public void Refresh()
    {
        CheckAllPartsBroken();
    }

    /// <summary>
    /// 部位破壊通知を処理します。
    /// </summary>
    /// <param name="part">破壊された部位。</param>
    private void HandlePartBroken(
        BossBreakablePart part)
    {
        CheckAllPartsBroken();
    }

    /// <summary>
    /// グループ内の全部位が破壊されたか確認します。
    /// </summary>
    private void CheckAllPartsBroken()
    {
        if (m_areAllPartsBroken ||
            m_parts == null ||
            m_parts.Count == 0)
        {
            return;
        }

        foreach (BossBreakablePart part
                 in m_parts)
        {
            /*
             * nullを無視すると設定漏れでも
             * グループ達成してしまうため未達成扱いにする。
             */
            if (part == null ||
                !part.IsBroken)
            {
                return;
            }
        }

        m_areAllPartsBroken =
            true;

        AllPartsBroken?.Invoke(
            this);
    }
}