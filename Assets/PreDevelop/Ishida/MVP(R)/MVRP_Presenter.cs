using UniRx;
using System;


public class MVRP_Presenter : IDisposable
{
    private readonly MVRP_Model _model;
    private readonly MVRP_View _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public MVRP_Presenter(MVRP_Model model,MVRP_View view)
    {
        _model = model;
        _view = view;
        // ① Viewからの「知らせ」を受け取る → Modelに反映依頼
        _view.OnDamaged += HandleDamaged;

        // ② Modelの変化を「監視」する → Viewに反映指示
        _model.Hp
            .Subscribe(currentHp => _view.UpdateHpBar(currentHp, _model.MaxHp))
            .AddTo(_disposables);
    }

    private void HandleDamaged(int damage)
    {
        _model.TakeDamage(damage); // Modelにパラメータ更新を依頼するだけ
    }

    public void Dispose()
    {
        _view.OnDamaged -= HandleDamaged;
        _disposables.Dispose();
    }
}
