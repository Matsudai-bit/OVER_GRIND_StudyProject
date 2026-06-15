using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum EffectID
{
    HitEffect
}

/// <summary>
/// エフェクト再生時の設定パラメータ。
/// Play()の引数として渡すか、再生後にSetLoop()/SetSpeed()で変更する。
/// </summary>
public class EffectPlayOptions
{
    /// <summary>ループ再生するか。デフォルトはfalse。</summary>
    public bool Loop = false;

    /// <summary>再生速度の倍率。デフォルトは1.0（等速）。</summary>
    public float Speed = 1.0f;

    /// <summary>
    /// 追従対象のTransform。
    /// nullの場合は再生開始位置に固定。
    /// Play(EffectID, Transform)オーバーロード使用時は自動設定される。
    /// </summary>
    public Transform Follow = null;
}

/// <summary>
/// エフェクトマネージャー。
/// EffectIDをキーにParticleSystemを管理し、オブジェクトプールで再利用する。
/// シングルトンとして動作し、シーンをまたいでも維持される。
/// </summary>
public class EffectManager : SingletonMonoBehaviour<EffectManager>
{
    // -----------------------------------------------------------------
    // インスペクタ登録用
    // -----------------------------------------------------------------

    [System.Serializable]
    private class EffectEntry
    {
        /// <summary>識別子となるEffectID ScriptableObject。</summary>
        public EffectID ID;

        /// <summary>再生するParticleSystemのPrefab。</summary>
        public ParticleSystem Prefab;
    }

    [SerializeField] private List<EffectEntry> _effectEntries = new();

    // -----------------------------------------------------------------
    // 内部データ
    // -----------------------------------------------------------------

    /// <summary>再生中エフェクトの管理情報。</summary>
    private class ActiveEffect
    {
        public int            Handle;
        public ParticleSystem Instance;
        public EffectID       ID;
        public Transform      Follow;
        public Coroutine      AutoReturnCoroutine;
    }

    // EffectID -> プール
    private readonly Dictionary<EffectID, Queue<ParticleSystem>> _pool        = new();
    // EffectID -> Prefab参照
    private readonly Dictionary<EffectID, ParticleSystem>        _prefabs     = new();
    // Handle -> アクティブエフェクト
    private readonly Dictionary<int, ActiveEffect>               _actives     = new();
    // 使用中のハンドル番号セット
    private readonly HashSet<int>                                _usedHandles = new();

    private System.Random _random = new();

    // -----------------------------------------------------------------
    // 初期化
    // -----------------------------------------------------------------

    /// <summary>
    /// インスペクタ登録内容をもとに内部テーブルとプールを初期化する。
    /// IDまたはPrefabがnullのエントリは無視する。
    /// </summary>
    protected override void Init()
    {
        foreach (var entry in _effectEntries)
        {
            if (entry.ID == null || entry.Prefab == null) continue;
            _prefabs[entry.ID] = entry.Prefab;
            _pool[entry.ID]    = new Queue<ParticleSystem>();
        }
    }

    // -----------------------------------------------------------------
    // public API
    // -----------------------------------------------------------------

    /// <summary>
    /// 指定したEffectIDのエフェクトをワールド座標で再生する。
    /// </summary>
    /// <param name="id">再生するエフェクトの識別子。</param>
    /// <param name="position">再生するワールド座標。</param>
    /// <param name="options">ループ・速度などの再生設定。nullの場合はデフォルト値を使用。</param>
    /// <returns>
    /// 再生を識別するハンドル番号。Stop()/SetLoop()/SetSpeed()に使用する。
    /// 未登録IDの場合は-1を返す。
    /// </returns>
    public int Play(EffectID id, Vector3 position, EffectPlayOptions options = null)
    {
        var ps = GetFromPool(id);
        if (ps == null) return -1;

        ps.transform.position = position;
        return Activate(id, ps, options);
    }

    /// <summary>
    /// 指定したEffectIDのエフェクトをTransformに追従させて再生する。
    /// </summary>
    /// <param name="id">再生するエフェクトの識別子。</param>
    /// <param name="follow">追従対象のTransform。毎フレーム座標を同期する。</param>
    /// <param name="options">ループ・速度などの再生設定。nullの場合はデフォルト値を使用。</param>
    /// <returns>
    /// 再生を識別するハンドル番号。Stop()/SetLoop()/SetSpeed()に使用する。
    /// 未登録IDの場合は-1を返す。
    /// </returns>
    public int Play(EffectID id, Transform follow, EffectPlayOptions options = null)
    {
        var ps = GetFromPool(id);
        if (ps == null) return -1;

        ps.transform.position = follow.position;
        options ??= new EffectPlayOptions();
        options.Follow = follow;
        return Activate(id, ps, options);
    }

    /// <summary>
    /// 指定ハンドルのエフェクトを即座に停止してプールに返却する。
    /// 無効なハンドルを指定した場合は何もしない。
    /// </summary>
    /// <param name="handle">Play()が返したハンドル番号。</param>
    public void Stop(int handle)
    {
        if (!_actives.TryGetValue(handle, out var active)) return;
        ReturnToPool(active);
    }

    /// <summary>
    /// 指定ハンドルのエフェクトのループ設定を変更する。
    /// ループをfalseに切り替えた場合は再生終了後に自動でプールへ返却される。
    /// 無効なハンドルを指定した場合は何もしない。
    /// </summary>
    /// <param name="handle">Play()が返したハンドル番号。</param>
    /// <param name="loop">trueでループ再生、falseで単発再生。</param>
    public void SetLoop(int handle, bool loop)
    {
        if (!_actives.TryGetValue(handle, out var active)) return;
        var main = active.Instance.main;
        main.loop = loop;
        // ループ解除時は自動返却コルーチンを再始動
        if (!loop) RestartAutoReturn(active);
    }

    /// <summary>
    /// 指定ハンドルのエフェクトの再生速度を変更する。
    /// 無効なハンドルを指定した場合は何もしない。
    /// </summary>
    /// <param name="handle">Play()が返したハンドル番号。</param>
    /// <param name="speed">再生速度の倍率。1.0が等速、2.0が2倍速。</param>
    public void SetSpeed(int handle, float speed)
    {
        if (!_actives.TryGetValue(handle, out var active)) return;
        SetSpeed(active.Instance, speed);
    }

    private void SetSpeed(ParticleSystem ps, float speed)
    {
        var main = ps.main;
        main.simulationSpeed = speed;

        foreach (var child in ps.gameObject.GetComponentsInChildren<ParticleSystem>())
        {
            var cMain = child.main;
            cMain.simulationSpeed = speed;
        }
    }

    // -----------------------------------------------------------------
    // 内部処理
    // -----------------------------------------------------------------

    /// <summary>
    /// ParticleSystemにオプションを適用して再生を開始し、ハンドルを発行する。
    /// </summary>
    /// <param name="id">エフェクトの識別子。プール返却時のキーに使用。</param>
    /// <param name="ps">プールから取得済みのParticleSystemインスタンス。</param>
    /// <param name="options">再生設定。nullの場合はデフォルト値を使用。</param>
    /// <returns>発行したハンドル番号。</returns>
    private int Activate(EffectID id, ParticleSystem ps, EffectPlayOptions options)
    {
        options ??= new EffectPlayOptions();

        var main = ps.main;
        main.loop = options.Loop;
        SetSpeed(ps, options.Speed);

        ps.gameObject.SetActive(true);
        ps.Play(true);

        int handle = GenerateHandle();

        var active = new ActiveEffect
        {
            Handle   = handle,
            Instance = ps,
            ID       = id,
            Follow   = options.Follow,
        };

        _actives[handle] = active;
        _usedHandles.Add(handle);

    

        // ループでなければ再生終了時に自動返却
        if (!options.Loop)
            active.AutoReturnCoroutine = StartCoroutine(AutoReturnWhenFinished(active));

        return handle;
    }

    /// <summary>
    /// ParticleSystemの再生終了を監視し、完了したらプールへ返却するコルーチン。
    /// IsAlive()がfalseになるまで毎フレーム待機する。
    /// </summary>
    /// <param name="active">監視対象のアクティブエフェクト情報。</param>
    private IEnumerator AutoReturnWhenFinished(ActiveEffect active)
    {
        yield return new WaitUntil(() =>
            active.Instance == null || !active.Instance.IsAlive(true));

        if (_actives.ContainsKey(active.Handle))
            ReturnToPool(active);
    }

    /// <summary>
    /// エフェクトを停止して非アクティブ化し、プールへ戻す。
    /// 自動返却コルーチンが動作中であれば停止する。
    /// </summary>
    /// <param name="active">返却するアクティブエフェクト情報。</param>
    private void ReturnToPool(ActiveEffect active)
    {
        if (active.AutoReturnCoroutine != null)
            StopCoroutine(active.AutoReturnCoroutine);

        active.Instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        active.Instance.gameObject.SetActive(false);

        if (_pool.TryGetValue(active.ID, out var queue))
            queue.Enqueue(active.Instance);

        _actives.Remove(active.Handle);
        _usedHandles.Remove(active.Handle);
    }

    /// <summary>
    /// 自動返却コルーチンを停止して再始動する。
    /// ループ解除時など、残り再生時間を改めて監視したいときに使用する。
    /// </summary>
    /// <param name="active">対象のアクティブエフェクト情報。</param>
    private void RestartAutoReturn(ActiveEffect active)
    {
        if (active.AutoReturnCoroutine != null)
            StopCoroutine(active.AutoReturnCoroutine);
        active.AutoReturnCoroutine = StartCoroutine(AutoReturnWhenFinished(active));
    }

    /// <summary>
    /// プールからParticleSystemを取得する。
    /// プールが空の場合は新規にInstantiateして動的拡張する。
    /// 未登録のEffectIDが渡された場合はWarningを出してnullを返す。
    /// </summary>
    /// <param name="id">取得するエフェクトの識別子。</param>
    /// <returns>使用可能なParticleSystemインスタンス。未登録の場合はnull。</returns>
    private ParticleSystem GetFromPool(EffectID id)
    {
        if (!_prefabs.TryGetValue(id, out var prefab))
        {
            Debug.LogWarning($"[EffectManager] 未登録のEffectID: {id.ToString()}");
            return null;
        }

        if (_pool[id].Count > 0)
            return _pool[id].Dequeue();

        // プールに空きがなければ新規生成（動的拡張）
        var ps = Instantiate(prefab, transform);
        ps.gameObject.SetActive(false);
        return ps;
    }

    /// <summary>
    /// 使用中と重複しないランダムなハンドル番号を生成する。
    /// 重複した場合は再生成を繰り返す。
    /// </summary>
    /// <returns>重複のないハンドル番号（1以上int.MaxValue未満）。</returns>
    private int GenerateHandle()
    {
        int handle;
        do { handle = _random.Next(1, int.MaxValue); }
        while (_usedHandles.Contains(handle));
        return handle;
    }

    // -----------------------------------------------------------------
    // Update: Transform追従
    // -----------------------------------------------------------------

    /// <summary>
    /// Follow指定があるエフェクトの座標を毎フレーム同期する。
    /// </summary>
    private void Update()
    {
        foreach (var active in _actives.Values)
        {
            if (active.Follow != null)
                active.Instance.transform.position = active.Follow.position;
        }
    }
}
