using UniRx;
using System;


//仲介役クラス、ViewとModelを繋ぐ、それだけのクラス
public class Obstacle_Presenter : IDisposable
{
    private readonly Obstacle_Model _model;
    private readonly Obstacle_View _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public Obstacle_Presenter(Obstacle_Model model, Obstacle_View view)
    {
        _model = model;
        _view = view;

        _view.Initialize(_model.Speed);

        // ① Viewからの「知らせ」を受け取る
        _view.OnDamage += HandleDamage;

        // ② Modelの変化を監視する（Hpがある場合）
        _model.Hp
            .Subscribe(currentHp => HandleHpChanged(currentHp))
            .AddTo(_disposables);
    }

    private void HandleDamage(int damage)
    {
        // 弾自体はHPを持つなら減らす（貫通弾なら複数回ヒットに耐える、など）
        DebugManager.Log("[Presenter]HandleDamageが呼ばれた");
        _model.TakeDamage(damage);
    }

    private void HandleHpChanged(int currentHp)
    {
        DebugManager.Log("[Presenter]HandleHpChangeが呼ばれた");
        _view.UpdateHpBar(currentHp,_model.MaxHp);

        if (currentHp <= 0)
        {
            _view.DestroySelf();
        }
    }

    public void Dispose()
    {
        _view.OnDamage -= HandleDamage;
        _disposables.Dispose();
    }
}
