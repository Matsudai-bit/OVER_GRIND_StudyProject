using UniRx;
using System;

public class Bullet_Presenter : IDisposable
{
    private readonly Bullet_Model _model;
    private readonly Bullet_View _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public Bullet_Presenter(Bullet_Model model, Bullet_View view)
    {
        _model = model;
        _view = view;

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
        _model.TakeDamage(damage);
        _view.FlashRed();
    }

    private void HandleHpChanged(int currentHp)
    {
        
        if (currentHp <= 0)
        {
            
           
        }
    }

    public void Dispose()
    {
        _view.OnDamage -= HandleDamage;
        _disposables.Dispose();
    }
}