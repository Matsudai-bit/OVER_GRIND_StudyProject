using UnityEngine;
using UnityEngine.UI;

public class Obstacle_Controller : MonoBehaviour
{
    [Header("初期パラメータ設定")]
    [SerializeField] private int maxHp = 3;
    [SerializeField] private int speed = 5;

    [SerializeField] private Obstacle_View _view; // 同じGameObjectにアタッチされたViewを参照

    private Obstacle_Parameter _parameter;
    private Obstacle_Model _model;
    private Obstacle_Presenter _presenter;

    [SerializeField] private GameObject hpBarPrefab;
    [SerializeField] private Vector3 hpBarOffset = new Vector3(0, 100.0f, 0);


    private void Awake()
    {   // 子オブジェクトとして生成する（第2引数のtransformが親になる）
        GameObject hpBarInstance = Instantiate(
            hpBarPrefab,
            transform.position + hpBarOffset,
            Quaternion.identity,
            transform // ← これが親を指定する引数
        );
        Image hpBarFill = hpBarInstance.GetComponentInChildren<Image>();
        _view.SetHpBarFill(hpBarFill);

        _parameter = new Obstacle_Parameter(maxHp, speed); // ← ここで初めて数値が使われる
        _model = new Obstacle_Model(_parameter);
        _presenter = new Obstacle_Presenter(_model, _view);


    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
}
