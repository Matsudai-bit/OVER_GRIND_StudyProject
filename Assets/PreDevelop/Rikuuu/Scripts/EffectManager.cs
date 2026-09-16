using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// エフェクト再生時の設定をまとめるクラス
/// </summary>
public class EffectPlayOptions
{
    // 回転角度
    public float Rotate = 0.0f;

    // 再生速度の倍率
    public float Rate = 1.0f;

    // 追従対象の倍率
    public Transform Follow = null;
}

/// <summary>
/// エフェクトのデータ・再生・停止の管理を行うクラス
/// </summary>
public class EffectManager : SingletonMonoBehaviour<EffectManager>
{
    // EffectViewerで設定されたエフェクト配列
    [SerializeField]
    private EffectData[] m_effects;

    // 再生中のエフェクトの情報をまとめた内部クラス
    private class ActiveEffect
    {
        public int Handle;                      // ハンドルID                     
        public VisualEffect Instance;           // インスタンス          
        public string ID;                       // ID文字列                      
        public Transform Follow;                // 追尾対象のTransform               
        public Coroutine AutoReturnCoroutine;   // コルーチンの参照  
    }

    // エフェクトのオブジェクトプール [key：エフェクトID, value：未使用インスタンスのキュー]
    private readonly Dictionary<string, Queue<VisualEffect>> m_pool = new();
    // エフェクトとプレハブの対応表   [key：エフェクトID, value：プレハブ]
    private readonly Dictionary<string, VisualEffect> m_prefabs = new();
    // 再生中のエフェクト一覧表       [key：ハンドルID,   value：ActiveEffect情報]
    private readonly Dictionary<int, ActiveEffect> m_actives = new();
    // ハンドルIDの集合体
    private readonly HashSet<int> m_usedHandles = new();
    // ハンドルIDをランダム生成する乱数生成機
    private System.Random m_random = new();

    // --------------------------------------------------------------------------------------

    /// <summary>
    /// 初期化
    /// </summary>
    protected override void Init()
    {
        // 登録されている全エフェクトを確認
        foreach (var entry in m_effects)
        {
            // 名前またはアセットが設定されていない場合
            if (entry.GetEffectName() == null || entry.GetVfxAsset() == null)
            {
                // スキップする
                continue;
            }
            // エフェクトIDを取得
            string effectName = entry.GetEffectName();
            // VisualEffecプレハブを取得
            VisualEffect effect = entry.GetVfxAsset();

            // エフェクトとプレハブの対応表に登録
            m_prefabs[effectName] = effect;
            // エフェクトID用のオブジェクトプールを作成
            m_pool[effectName] = new Queue<VisualEffect>();
        }
    }

    /// <summary>
    /// エフェクトの再生を行う
    /// </summary>
    /// <param name="id">       エフェクトID                                  </param>
    /// <param name="position"> 再生する座標                                  </param>
    /// <param name="options">  再生中のエフェクトの情報をまとめた内部クラス  </param>
    public int Play(string id, Vector3 position, EffectPlayOptions options = null)
    {
        // プールから該当IDのエフェクトインスタンスを取得
        var ps = GetFromPool(id);
        // 取得できなかった場合
        if (ps == null)
        {
            // 再生失敗
            return -1;
        }

        // エフェクトの座標の初期化
        ps.transform.position = position;
        // 実際に再生処理を行い、ハンドルIDを返す
        return Activate(id, ps, options);
    }

    /// <summary>
    /// エフェクトの停止を行う
    /// </summary>
    /// <param name="handle">ハンドルID</param>
    public void Stop(int handle)
    {
        // 指定ハンドルのエフェクトが見つからなければ
        if (!m_actives.TryGetValue(handle, out var active))
        {
            // 終了
            return;
        }

        // 即座にプールに戻す
        //ReturnToPool(active);
        
        // 自然に消えるように停止処理を行う
        StopGracefully(active);
    }

    // --------------------------------------------------------------------------------------

    /// <summary>
    /// エフェクトが自然に消えるように停止させる
    /// </summary>
    /// <param name="active">再生中のエフェクト</param>
    private void StopGracefully(ActiveEffect active)
    {
        // すでに停止処理中の場合
        if (active.AutoReturnCoroutine != null)
        {
            // 何もせずに終了
            return;
        }

        // VisualEffectに対して「停止イベント」を送信する
        active.Instance.SendEvent(VisualEffectAsset.StopEventName);
        // パーティクルが消えるまで待ってからプールに戻すコルーチンを開始し、その参照を保存する
        active.AutoReturnCoroutine = StartCoroutine(WaitAndReturn(active));
    }

    /// <summary>
    /// エフェクトが完全に消えるまで待つ
    /// </summary>
    /// <param name="active">再生中のエフェクト</param>
    private IEnumerator WaitAndReturn(ActiveEffect active)
    {
        // aliveParticleCountが0になるまで待機
        yield return new WaitUntil(() =>
            active.Instance.aliveParticleCount == 0
        );

        // 完全に消えたらプールに戻す
        ReturnToPool(active);
    }

    // --------------------------------------------------------------------------------------

    /// <summary>
    /// エフェクトの再生速度を設定する
    /// </summary>
    /// <param name="ps">       VisualEffectインスタンス</param>
    /// <param name="speed">    適用後の再生速度        </param>
    private void SetSpeed(VisualEffect ps, float speed)
    {
        // VisualEffect の再生速度を設定
        ps.playRate = speed;

        // 子のParticleSystemを全て取得してループする
        foreach (var child in ps.gameObject.GetComponentsInChildren<ParticleSystem>())
        {
            // mainモジュールを取得
            var cMain = child.main;
            // パーティクルシミュレーション速度を設定
            cMain.simulationSpeed = speed;
        }
    }

    // --------------------------------------------------------------------------------------

    /// <summary>
    /// エフェクトの再生を行う
    /// </summary>
    /// <param name="id">       エフェクトID                                  </param>
    /// <param name="ps">       再生するVisualEffectインスタンス              </param>
    /// <param name="options">  再生中のエフェクトの情報をまとめた内部クラス  </param>
    private int Activate(string id, VisualEffect ps, EffectPlayOptions options)
    {
        // optionsがnullの場合はデフォルト値で新規作成する
        options ??= new EffectPlayOptions();

        // 再生速度の設定
        SetSpeed(ps, options.Rate);

        // GameObjectを有効化
        ps.gameObject.SetActive(true);
        // VisualEffectの再生を開始する
        ps.Play();

        // ハンドルIDをランダムで生成する
        int handle = GenerateHandle();

        // 再生中エフェクトの情報をまとめた ActiveEffect インスタンスを作成
        var active = new ActiveEffect
        {
            Handle = handle,
            Instance = ps,
            ID = id,
            Follow = options.Follow,
        };

        // 再生中リストに登録
        m_actives[handle] = active;
        // 使用中ハンドル配列に追加
        m_usedHandles.Add(handle);

        // ハンドルIDを返す
        return handle;
    }

    /// <summary>
    /// 再生が終了したエフェクトをプールに戻す
    /// </summary>
    /// <param name="active">再生が終了したエフェクト</param>
    private void ReturnToPool(ActiveEffect active)
    {
        // 自動返却コルーチンが動いていたら
        if (active.AutoReturnCoroutine != null)
        {
            // そのコルーチンを停止する
            StopCoroutine(active.AutoReturnCoroutine);
        }

        // VisualEffectを停止する
        active.Instance.Stop();
        // GameObjectを非アクティブにする
        active.Instance.gameObject.SetActive(false);

        // 対応するIDのプールキューを取得する
        if (m_pool.TryGetValue(active.ID, out var queue))
        {
            // インスタンスをプールに戻す
            queue.Enqueue(active.Instance);
        }

        // 再生中リストから削除する
        m_actives.Remove(active.Handle);
        // 使用中ハンドル配列から削除する
        m_usedHandles.Remove(active.Handle);
    }

    /// <summary>
    /// 指定IDのエフェクトインスタンスをプールから取得、新規作成する
    /// </summary>
    /// <param name="active">再生が終了したエフェクト</param>
    private VisualEffect GetFromPool(string id)
    {
        // IDのプレハブが登録されていない場合
        if (!m_prefabs.TryGetValue(id, out var prefab))
        {
            // 警告ログを出す
            Debug.LogWarning($"[EffectManager] 未登録のEffectID: {id.ToString()}");
            // 取得失敗を返す
            return null;
        }

        // プールに未使用インスタンスがある場合
        if (m_pool[id].Count > 0)
        {
            // 先頭のインスタンスを取り出して返す
            return m_pool[id].Dequeue();
        }

        // 新しくインスタンスを生成し。EffectManagerの子にする
        var ps = Instantiate(prefab, transform);
        // 生成直後は非アクティブにする
        ps.gameObject.SetActive(false);

        // 生成したインスタンスを返す
        return ps;
    }

    /// <summary>
    /// 他のものと重ならないハンドルIDを作成する
    /// </summary>
    private int GenerateHandle()
    {
        // 返すハンドルID
        int handle;

        // 1～int.MaxValueの範囲で値をランダム生成する
        do
        {
            handle = m_random.Next(1, int.MaxValue);
        }
        // すでに使われている場合は再生成を行う
        while (m_usedHandles.Contains(handle));

        // ハンドルIDを返す
        return handle;
    }

    // --------------------------------------------------------------------------------------

    /// <summary>
    /// 更新処理
    /// </summary>
    private void Update()
    {
        // 再生中の全エフェクトをループする
        foreach (var active in m_actives.Values)
        {
            // 追従対象が設定されている場合
            if (active.Follow != null)
            {
                // エフェクトの座標を追尾対象の座標に更新する
                active.Instance.transform.position = active.Follow.position;
            }
        }
    }

    /// <summary>
    /// 登録されているエフェクト一覧表を取得する
    /// </summary>
    public EffectData[] GetEffects()
    {
        // m_effectsがnullの場合は空配列を返す
        return m_effects ?? System.Array.Empty<EffectData>();
    }
}
