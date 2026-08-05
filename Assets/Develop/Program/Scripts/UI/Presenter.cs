using TMPro;
using UnityEngine;
using UniRx;

// プレイヤーのUIマネージャー
public class Presenter : MonoBehaviour
{
    //初期設定
    [Header("初期設定")]
    public int maxHp = 100;     //最大体力
    public int maxMp = 50;      //最大魔力
    public int initialGold = 0; //最大金

    [Header("監視対象のView")]
    [SerializeField] private PlayerHealthView view;

    // モデル
    private PlayerHealthModel model;

    void Start()
    {
        model = new PlayerHealthModel(maxHp, maxMp, initialGold);

        //modelが監視対象
        //Subscribe変わった時の行動
        //view.UpdateHPDisplay安全装置
        model.CurrentHp.Subscribe(view.UpdateHPDisplay).AddTo(this);
        model.CurrentMp.Subscribe(view.UpdateMPDisplay).AddTo(this);
        model.Gold.Subscribe(view.UpdateGoldDisplay).AddTo(this);

        //view呼び鈴
        //+=呼び出す記号
        //HandleDamageInput呼び鈴がなった時に行われる処理
        view.OnDamageInput += HandleDamageInput;
        view.OnMagicInput += HandleMagicInput;
        view.OnGainGoldInput += HandleGainGoldInput;
    }

    // 入力の中継　　鳴った時に実行する処理
    private void HandleDamageInput() => model.TakeDamage(10);
    private void HandleMagicInput() => model.UseMagic(5);
    private void HandleGainGoldInput() => model.AddGold(100);

    void OnDestroy()
    {
        if (view != null)
        {
            view.OnDamageInput -= HandleDamageInput;
            view.OnMagicInput -= HandleMagicInput;
            view.OnGainGoldInput -= HandleGainGoldInput;
        }
    }
}