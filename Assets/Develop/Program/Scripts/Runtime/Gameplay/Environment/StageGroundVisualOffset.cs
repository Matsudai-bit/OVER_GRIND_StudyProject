using System.Collections.Generic;
using UnityEngine;

/// <summary>描画用メッシュだけを上方向へずらし、元のMeshColliderを維持します。</summary>
[DisallowMultipleComponent]
public sealed class StageGroundVisualOffset : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("ワールド上方向へずらす描画距離。MeshColliderには適用しません。")]
    private float m_worldOffset = 0.01f;

    private readonly List<OffsetMesh> m_offsetMeshes = new List<OffsetMesh>();

    /// <summary>子階層の描画メッシュを複製し、複製側の頂点だけを上へずらします。</summary>
    private void Awake()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh originalMesh = meshFilter.sharedMesh;
            if (originalMesh == null) continue;

            Mesh offsetMesh = Instantiate(originalMesh);
            offsetMesh.name = $"{originalMesh.name}_VisualOffset";
            Vector3 localOffset = meshFilter.transform.InverseTransformVector(
                Vector3.up * Mathf.Max(0f, m_worldOffset));
            Vector3[] vertices = offsetMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += localOffset;
            }

            offsetMesh.vertices = vertices;
            offsetMesh.RecalculateBounds();
            meshFilter.sharedMesh = offsetMesh;
            m_offsetMeshes.Add(new OffsetMesh(meshFilter, originalMesh, offsetMesh));
        }
    }

    /// <summary>描画メッシュを元へ戻し、複製したメッシュを破棄します。</summary>
    private void OnDestroy()
    {
        foreach (OffsetMesh offsetMesh in m_offsetMeshes)
        {
            if (offsetMesh.Filter != null && offsetMesh.Filter.sharedMesh == offsetMesh.Offset)
            {
                offsetMesh.Filter.sharedMesh = offsetMesh.Original;
            }

            if (offsetMesh.Offset != null)
            {
                Destroy(offsetMesh.Offset);
            }
        }

        m_offsetMeshes.Clear();
    }

    /// <summary>表示メッシュと差し替え前後のメッシュを保持します。</summary>
    private sealed class OffsetMesh
    {
        /// <summary>表示メッシュを保持します。</summary>
        public MeshFilter Filter { get; }

        /// <summary>差し替え前の共有メッシュを保持します。</summary>
        public Mesh Original { get; }

        /// <summary>頂点を移動した表示用メッシュを保持します。</summary>
        public Mesh Offset { get; }

        /// <summary>表示メッシュと差し替え前後のメッシュを登録します。</summary>
        public OffsetMesh(MeshFilter filter, Mesh original, Mesh offset)
        {
            Filter = filter;
            Original = original;
            Offset = offset;
        }
    }
}
