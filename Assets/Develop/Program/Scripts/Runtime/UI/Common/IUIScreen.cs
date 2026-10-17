using System;

/// <summary>
/// 画面の表示・非表示と入力の制御を共通化します。
/// </summary>
public interface IUIScreen : IUIInputControllable
{
    /// <summary>
    /// 画面の表示を開始します。
    /// </summary>
    /// <param name="onCompleted">表示完了時の処理。</param>
    void Show(Action onCompleted = null);

    /// <summary>
    /// 画面の非表示を開始します。
    /// </summary>
    /// <param name="onCompleted">非表示完了時の処理。</param>
    void Hide(Action onCompleted = null);
}
