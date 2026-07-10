using UniRx;
using System;

//仲介役クラス、ViewとModelを繋ぐ、それだけのクラス
public class MVRP_Presenter : IDisposable
{
    private readonly MVRP_Model _model;
    private readonly MVRP_View _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public MVRP_Presenter(MVRP_Model model, MVRP_View view)
    {
        _model = model;
        _view = view;

        _view.Initialize();

        // ① Viewからの「知らせ」を受け取る
        _view.OnDamage += HandleDamage;

        // ② Modelの変化を監視する（Hpがある場合）
        _model.Hp
            .Subscribe(currentHp => HandleHpChanged(currentHp))
            .AddTo(_disposables);
    }

    //知らせを受けたらそれに対する関数をModelで実行する
    private void HandleDamage(int damage)
    {
        // 弾自体はHPを持つなら減らす（貫通弾なら複数回ヒットに耐える、など）
        DebugManager.Log("[Presenter]HandleDamageが呼ばれた");
        _model.TakeDamage(damage);
    }

    private void HandleHpChanged(int currentHp)
    {
        DebugManager.Log("[Presenter]HandleHpChangeが呼ばれた");
        _view.UpdateHpBar(currentHp, _model.MaxHp);

        if (currentHp <= 0)
        {
            _view.DestroySelf();
        }
    }

    //後片付け関数
    public void Dispose()
    {
        _view.OnDamage -= HandleDamage;//ダメージを受けるイベント解除
        _disposables.Dispose();　　　　//Ｈｐ監視部分（Subscrideまとめて解除）
    }
}
