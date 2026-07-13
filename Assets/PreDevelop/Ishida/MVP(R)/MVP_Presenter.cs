using UniRx;
using System;

//仲介役クラス、ViewとModelを繋ぐ、それだけのクラス
public class MVP_Presenter : IDisposable
{
    private readonly MVP_Model m_model;
    private readonly MVP_View m_view;
    private readonly CompositeDisposable m_disposables = new CompositeDisposable();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="model">Modelクラス</param>
    /// <param name="view">Viewクラス</param>
    public MVP_Presenter(MVP_Model model, MVP_View view)
    {
        m_model = model;
        m_view = view;


    }

    //知らせを受けたらそれに対する関数をModelで実行する


    /// <summary>
    /// イベント解除する
    /// </summary>
    public void Dispose()
    {

    }
}
