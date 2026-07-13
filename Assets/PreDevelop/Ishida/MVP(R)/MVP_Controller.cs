using UnityEngine;

public class MVP_Controller : MonoBehaviour
{
    //パラメータを記載
    [Header("初期パラメータ設定")]


    //XXXX_View
    [SerializeField] private MVP_View m_view; // 同じGameObjectにアタッチされたViewを参照

    //XXXX_Parameter
    private MVP_Parameter m_parameter;
    //XXXX_Model
    private MVP_Model m_model;
    //XXXX_Presenter
    private MVP_Presenter m_presenter;


    /// <summary>
    /// 生成を行う
    /// </summary>
    private void Awake()
    {   
      

        //各生成
        //パラメータにはパラメータ変数
        m_parameter = new MVP_Parameter(); // ← ここで初めて数値が使われる
        //モデルにはパラメータクラス
        m_model = new MVP_Model(m_parameter);
        //プレゼンターにはモデルとビュー
        m_presenter = new MVP_Presenter(m_model, m_view);


    }

    /// <summary>
    /// Presenterの後片付け
    /// </summary>
    private void OnDestroy()
    {
        m_presenter?.Dispose();
    }
}
