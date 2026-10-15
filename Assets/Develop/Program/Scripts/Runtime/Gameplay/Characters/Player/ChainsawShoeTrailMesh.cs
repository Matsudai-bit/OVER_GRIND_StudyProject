using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>刃痕を固定容量のメッシュへ格納し、古い区間から再利用します。</summary>
public sealed class ChainsawShoeTrailMesh : System.IDisposable
{
    private static readonly int TRAIL_TIME = Shader.PropertyToID("_TrailTime");
    private static readonly int LIFETIME = Shader.PropertyToID("_Lifetime");
    private static readonly int FADE_START_RATIO = Shader.PropertyToID("_FadeStartRatio");
    private readonly GameObject m_object;
    private readonly Mesh m_mesh;
    private readonly Material m_material;
    private readonly Vector3[] m_vertices;
    private readonly Vector2[] m_uv;
    private readonly Vector2[] m_birthTimes;
    private readonly int m_capacity;
    private int m_nextSegment;
    private bool m_dirty;

    /// <summary>ワールド座標で描画するバッファと専用マテリアルを作成します。</summary>
    public ChainsawShoeTrailMesh(Material material, int capacity, float lifetime, Scene scene)
    {
        m_capacity = Mathf.Clamp(capacity, 128, 8192);
        m_vertices = new Vector3[m_capacity * 4];
        m_uv = new Vector2[m_vertices.Length];
        m_birthTimes = new Vector2[m_vertices.Length];
        int[] triangles = new int[m_capacity * 6];
        for (int i = 0; i < m_capacity; i++)
        {
            int vertex = i * 4;
            int index = i * 6;
            triangles[index] = vertex;
            triangles[index + 1] = vertex + 2;
            triangles[index + 2] = vertex + 1;
            triangles[index + 3] = vertex + 1;
            triangles[index + 4] = vertex + 2;
            triangles[index + 5] = vertex + 3;
        }
        m_mesh = new Mesh { name = "Chainsaw shoe marks" };
        m_mesh.MarkDynamic();
        m_mesh.vertices = m_vertices;
        m_mesh.triangles = triangles;
        m_material = new Material(material);
        m_material.SetFloat(LIFETIME, lifetime);
        m_object = new GameObject("ChainsawShoeMarks", typeof(MeshFilter), typeof(MeshRenderer));
        SceneManager.MoveGameObjectToScene(m_object, scene);
        m_object.GetComponent<MeshFilter>().sharedMesh = m_mesh;
        MeshRenderer renderer = m_object.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = m_material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    /// <summary>接地面の法線に沿う刃痕区間をバッファへ追加します。</summary>
    public void AddSegment(Vector3 start, Vector3 end, Vector3 startNormal, Vector3 endNormal,
        float width, float distance, float birthTime)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 startSide = Vector3.Cross(startNormal, direction).normalized * (width * 0.5f);
        Vector3 endSide = Vector3.Cross(endNormal, direction).normalized * (width * 0.5f);
        int vertex = m_nextSegment * 4;
        m_vertices[vertex] = start - startSide;
        m_vertices[vertex + 1] = start + startSide;
        m_vertices[vertex + 2] = end - endSide;
        m_vertices[vertex + 3] = end + endSide;
        float endDistance = distance + Vector3.Distance(start, end);
        m_uv[vertex] = new Vector2(0f, distance);
        m_uv[vertex + 1] = new Vector2(1f, distance);
        m_uv[vertex + 2] = new Vector2(0f, endDistance);
        m_uv[vertex + 3] = new Vector2(1f, endDistance);
        for (int i = 0; i < 4; i++) m_birthTimes[vertex + i] = new Vector2(birthTime, 0f);
        m_nextSegment = (m_nextSegment + 1) % m_capacity;
        m_dirty = true;
    }

    /// <summary>変更があるフレームだけメッシュを転送し、経過時間は毎フレーム更新します。</summary>
    public void Upload(float time)
    {
        m_material.SetFloat(TRAIL_TIME, time);
        if (!m_dirty) return;
        m_mesh.vertices = m_vertices;
        m_mesh.uv = m_uv;
        m_mesh.uv2 = m_birthTimes;
        m_mesh.RecalculateBounds();
        m_dirty = false;
    }

    /// <summary>刃痕の寿命と、薄くなり始める時点を更新します。</summary>
    /// <param name="lifetime">完全に消えるまでの秒数。</param>
    /// <param name="fadeStartRatio">寿命に対するフェード開始の割合。</param>
    public void SetFadeParameters(float lifetime, float fadeStartRatio)
    {
        m_material.SetFloat(LIFETIME, Mathf.Max(0.1f, lifetime));
        m_material.SetFloat(FADE_START_RATIO, Mathf.Clamp(fadeStartRatio, 0f, 0.99f));
    }

    /// <summary>動的生成したGameObject・メッシュ・マテリアルを破棄します。</summary>
    public void Dispose()
    {
        Object.Destroy(m_object);
        Object.Destroy(m_mesh);
        Object.Destroy(m_material);
    }
}
