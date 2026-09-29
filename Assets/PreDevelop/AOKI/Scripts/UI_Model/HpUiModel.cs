using UnityEngine;
using System;

/// <summary>
/// HP のデータを管理するモデルです。
/// </summary>
public class HpUiModel : MonoBehaviour
{
    /// <summary>HP の最大値を取得します。</summary>
    public float MaxHp { get; private set; } = 100f;

    /// <summary>現在の HP を取得します。</summary>
    public float CurrentHp { get; private set; }

    /// <summary>HP が変更された際に発火するイベントです。</summary>
    public event Action<float, float> OnHpChanged;

    /// <summary>
    /// ゲーム開始時に現在の HP を最大値で初期化します。
    /// </summary>
    private void Start()
    {
        CurrentHp = MaxHp;
    }

    /// <summary>
    /// ダメージを受け、現在の HP を減算します。値の変更はイベントで通知されます。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    public void TakeDamage(float damage)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }
}