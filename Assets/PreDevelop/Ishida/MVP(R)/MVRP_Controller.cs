using UnityEngine;

public class MVRP_Controller : MonoBehaviour
{
    //パラメータを記載
    [Header("初期パラメータ設定")]
    [SerializeField] private int maxHp = 100; 

    //XXXX_View
    [SerializeField] private MVRP_View _view; // 同じGameObjectにアタッチされたViewを参照

    //XXXX_Parameter
    private MVRP_Parameter _parameter;
    //XXXX_Model
    private MVRP_Model _model;
    //XXXX_Presenter
    private MVRP_Presenter _presenter;

    //UI生成用
    [SerializeField] private GameObject hpBarPrefab;
    [SerializeField] private Vector3 hpBarOffset = new Vector3(0, 100.0f, 0);


    //一番初めに呼ばれる関数
    private void Awake()
    {   
        //UIの作成
        // 子オブジェクトとして生成する（第2引数のtransformが親になる）
        //GameObject hpBarInstance = Instantiate(
        //    hpBarPrefab,
        //    transform.position + hpBarOffset,
        //    Quaternion.identity,
        //    transform // ← これが親を指定する引数
        //);
        //Image hpBarFill = hpBarInstance.GetComponentInChildren<Image>();
        //_view.SetHpBarFill(hpBarFill);

        //各生成
        //パラメータにはパラメータ変数
        _parameter = new MVRP_Parameter(maxHp); // ← ここで初めて数値が使われる
        //モデルにはパラメータクラス
        _model = new MVRP_Model(_parameter);
        //プレゼンターにはモデルとビュー
        _presenter = new MVRP_Presenter(_model, _view);


    }

    //Presenter後片付け関数を呼ぶ
    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
}
