using System;

public abstract class ModelBase
{
    public string ParameterName { get; protected set; }

    // 文字列化の処理を子クラスに強制
    public abstract string GetValueString();

    // 通知用のイベント
    public event Action OnValueChanged;

    protected void NotifyValueChanged()
    {
        OnValueChanged?.Invoke();
    }
}