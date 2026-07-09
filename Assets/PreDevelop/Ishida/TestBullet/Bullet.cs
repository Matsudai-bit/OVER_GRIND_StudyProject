using UnityEngine;

public class Bullet : MonoBehaviour
{

    [Header("初期パラメータ設定")]
    public int hp = 10;
    public int attackPower = 10;
    public int speed = 5;

    //[SerializeField] private Bullet_View _view; // Viewはシーン上のオブジェクトを参照

    //private BulletParameter _parameter;
    //private Bullet_Model _model;
    //private Bullet_Presenter _presenter;

    //private void Awake()
    //{
    //    // Inspectorで設定した値を使ってParameterを生成
    //    _parameter = new BulletParameter(hp, attackPower, speed);
    //    _model = new Bullet_Model(_parameter);
    //    _presenter = new Bullet_Presenter(_model, _view);
    //}

    //private void OnDestroy()
    //{
    //    _presenter?.Dispose();
    //}
}
