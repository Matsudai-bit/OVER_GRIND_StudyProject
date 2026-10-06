using UnityEngine;
using System;

/// <summary>
/// タイマーの経過時間を管理するモデルです。
/// </summary>
public class TimerUiModel : MonoBehaviour
{
    /// <summary>現在の経過時間を取得します。</summary>
    public float CurrentTime { get; private set; }

    /// <summary>時間が変更された際に発火するイベントです。</summary>
    public event Action<float> OnTimeChanged;

    /// <summary>
    /// 経過時間を加算し、変更後の時間をイベントで通知します。
    /// </summary>
    /// <param name="deltaTime">加算する時間（秒）。</param>
    public void AddTime(float deltaTime)
    {
        CurrentTime += deltaTime;
        OnTimeChanged?.Invoke(CurrentTime);
    }

    /// <summary>
    /// 経過時間の更新
    /// </summary>
    private void Update()
    {
        // 毎フレーム時間を進める
        AddTime(Time.deltaTime);
    }
}