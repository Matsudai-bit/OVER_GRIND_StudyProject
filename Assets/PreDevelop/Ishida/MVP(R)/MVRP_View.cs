using System;
using UnityEngine;
using UnityEngine.UI;

//見た目・入力・当たり判定クラス（オブジェクトの移動などもここで行う）
//UIの調整？変動なども行う
//このクラスではパラメータに関する計算や比較などはしない
//変更点やパラメータに関する事象が起きた場合Presenterに知らせる
public class MVRP_View : MonoBehaviour
{
    // Presenterへ「当たったこと」を知らせるイベント
    public event Action<int> OnDamage;

    private Image _hpBarFill; // 緑色の画像（Filled設定済み）


    // パラメータが必要ならばPresenterから貰う
    public void Initialize()
    {
        
    }

    // 移動など
    void Update()
    {

    }

    //当たった際にイベントでPresenterに必要事項を送る
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Bullet>(out var damageable))
        {
            DebugManager.Log("[View]弾に当たった。アタックパワー：" + damageable.attackPower);
            OnDamage?.Invoke(damageable.attackPower); // 「誰に当たったか」だけ知らせる
        }
    }

    //例として自身を消す関数、Parameterで体力が０の場合などを判定し呼ぶ関数が必要↓
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    public void UpdateHpBar(int currentHp, int maxHp)
    {
        float ratio = (float)currentHp / maxHp;
        DebugManager.Log($"[View]UpdateHpBarが呼ばれた。ratio  {ratio} _hpBarFillはnullか：{_hpBarFill == null}");
        _hpBarFill.fillAmount = ratio; // 0.0〜1.0を渡すだけ
    }



    // Controllerから、生成したHPバーの参照を渡してもらう
    public void SetHpBarFill(Image hpBarFill)
    {
        _hpBarFill = hpBarFill;
    }


    //普通の動きもここで行うのでDebug用関数も必要なら入れる
    private void OnDrawGizmos()
    {
        // DebugManagerが存在し、かつGizmo表示が有効なときだけ描画する
        if (DebugManager.Instance == null || !DebugManager.Instance.GizmosActive)
        {
            return;
        }

        // 索敵範囲を半透明の赤い球体で描画
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawCube(transform.position, transform.localScale);

        // 輪郭線を不透明な赤で描画
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
