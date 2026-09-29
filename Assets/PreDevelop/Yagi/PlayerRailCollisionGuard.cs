using System.Collections.Generic;
using UnityEngine;

/// <summary>離陸元のレールとPlayerの衝突だけを一時的に無視し、離れた後に復元します。</summary>
public sealed class PlayerRailCollisionGuard
{
    private readonly List<(Collider player, Collider rail)> m_pairs = new();
    private float m_restoreAfter;

    /// <summary>Player本体と指定レールに属する非トリガーの組み合わせだけを無視します。</summary>
    public void Begin(Rigidbody body, SplineRailInfo rail, float minimumDuration)
    {
        Restore();
        if (body == null || rail == null) return;
        m_restoreAfter = Time.fixedTime + Mathf.Max(Time.fixedDeltaTime, minimumDuration);
        Collider[] players = body.GetComponentsInChildren<Collider>(true);
        Collider[] rails = rail.GetComponentsInChildren<Collider>(true);
        foreach (Collider player in players)
        {
            if (player.isTrigger || player.attachedRigidbody != body) continue;
            foreach (Collider target in rails)
            {
                if (target.isTrigger || target.attachedRigidbody == body ||
                    target.GetComponentInParent<SplineRailInfo>() != rail ||
                    Physics.GetIgnoreCollision(player, target)) continue;
                // 既に無視されている組み合わせは登録せず、他の機能の設定を維持します。
                Physics.IgnoreCollision(player, target, true);
                m_pairs.Add((player, target));
            }
        }
    }

    /// <summary>最低時間と離隔を確認し、安全に離れた組み合わせから復元します。</summary>
    public void Update()
    {
        for (int i = m_pairs.Count - 1; i >= 0; i--)
        {
            var pair = m_pairs[i];
            if (pair.player == null || pair.rail == null)
            {
                m_pairs.RemoveAt(i);
                continue;
            }
            if (!pair.player.enabled || !pair.rail.enabled ||
                !pair.player.gameObject.activeInHierarchy || !pair.rail.gameObject.activeInHierarchy)
            {
                Physics.IgnoreCollision(pair.player, pair.rail, false);
                m_pairs.RemoveAt(i);
                continue;
            }
            if (Time.fixedTime < m_restoreAfter) continue;
            // AABBで保守的に判定し、複雑なMeshColliderでも重なったまま復元しません。
            if (pair.player.bounds.Intersects(pair.rail.bounds)) continue;
            Physics.IgnoreCollision(pair.player, pair.rail, false);
            m_pairs.RemoveAt(i);
        }
    }

    /// <summary>状態終了・被弾・無効化時に、このクラスが変更した組み合わせを復元します。</summary>
    public void Restore()
    {
        foreach (var pair in m_pairs)
            if (pair.player != null && pair.rail != null)
                Physics.IgnoreCollision(pair.player, pair.rail, false);
        m_pairs.Clear();
    }
}
