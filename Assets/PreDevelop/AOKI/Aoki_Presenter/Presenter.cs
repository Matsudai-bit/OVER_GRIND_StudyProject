using UnityEngine;
using UnityEngine.InputSystem;

public class ParameterPresenter : MonoBehaviour
{
    [SerializeField] private ParameterView view;

    // Modelのインスタンスを保持
    private protHP _hpModel;
    private protVGauge _vGaugeModel;

    private void Awake()
    {
        // 1. Modelの初期化（初期値を設定）
        _hpModel = new protHP(100);
        _vGaugeModel = new protVGauge(0);

        // 2. Modelの変更イベント（Reactive）をViewの描画メソッドにバインド
        _hpModel.OnHPChanged += view.RenderHP;
        _vGaugeModel.OnVGaugeChanged += view.RenderVGauge;
    }

    private void Start()
    {
        // 初回起動時に現在の値をViewに反映させる
        view.RenderHP(_hpModel.GetHP());
        view.RenderVGauge(_vGaugeModel.GetVGauge());
    }

    private void OnDestroy()
    {
        // オブジェクト破棄時にメモリリークを防ぐためイベント登録を解除
        if (_hpModel != null) _hpModel.OnHPChanged -= view.RenderHP;
        if (_vGaugeModel != null) _vGaugeModel.OnVGaugeChanged -= view.RenderVGauge;
    }

    // ----------------------------------------------------
    // 以下はテスト・デバッグ用（実際のゲームでは他から呼ばれる）
    // ----------------------------------------------------
    private void Update()
    {
        // スペースキーでHPを減らし、Vゲージを増やすテスト
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _hpModel.SetHP(_hpModel.GetHP() - 10);
            _vGaugeModel.SetVGauge(_vGaugeModel.GetVGauge() + 5);
        }
    }
}