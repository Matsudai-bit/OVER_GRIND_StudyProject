using UnityEngine;
using UnityEditor;

public class MeshLODPolygonCounterWindow : EditorWindow
{
    [MenuItem("Tools/Mesh LOD Polygon Counter")]
    public static void ShowWindow()
    {
        GetWindow<MeshLODPolygonCounterWindow>("Mesh LOD Counter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Mesh LOD / LODGroup 計測ツール", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            EditorGUILayout.HelpBox("シーンまたはヒエラルキーからオブジェクトを選択してください。", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("選択中オブジェクト:", obj.name);
        EditorGUILayout.Space();

        // 1. Unity 6の「Mesh LOD」機能（単一Mesh内に複数LODがインポートされている場合）のチェック
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
        Mesh sharedMesh = null;

        if (mf != null) sharedMesh = mf.sharedMesh;
        else if (smr != null) sharedMesh = smr.sharedMesh;

        if (sharedMesh != null && sharedMesh.lodCount > 1)
        {
            EditorGUILayout.HelpBox($"Unity 6 Mesh LOD 検出 (LOD階層数: {sharedMesh.lodCount})", MessageType.Info);
            EditorGUILayout.HelpBox("※ 頂点バッファは全LODで共通です。インデックス（ポリゴン数）のみが段階的に削減されます。", MessageType.None);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("頂点数 (Vertices - 全LOD共通):", sharedMesh.vertexCount.ToString("N0"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("各LODレベルの三角ポリゴン数内訳", EditorStyles.boldLabel);

            for (int lod = 0; lod < sharedMesh.lodCount; lod++)
            {
                int totalTrisForLod = 0;

                for (int sub = 0; sub < sharedMesh.subMeshCount; sub++)
                {
                    // 各LODのインデックス範囲を取得し、intにキャストしてトライアングル数（indexCount / 3）を計算
                    MeshLodRange lodData = sharedMesh.GetLod(sub, lod);
                    totalTrisForLod += (int)lodData.indexCount / 3; // 修正箇所: (int)キャストを追加
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"LOD {lod}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  三角ポリゴン数 (Tris): {totalTrisForLod:N0}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            return;
        }

        // 2. 従来の LODGroup コンポーネントがある場合
        LODGroup lodGroup = obj.GetComponentInChildren<LODGroup>();
        if (lodGroup != null)
        {
            LOD[] lods = lodGroup.GetLODs();
            EditorGUILayout.HelpBox($"従来型 LODGroup 検出 (LOD階層数: {lods.Length})", MessageType.Info);
            EditorGUILayout.Space();

            for (int i = 0; i < lods.Length; i++)
            {
                int lodVerts = 0;
                int lodTris = 0;

                foreach (var render in lods[i].renderers)
                {
                    if (render == null) continue;

                    Mesh mesh = null;
                    if (render is SkinnedMeshRenderer s) mesh = s.sharedMesh;
                    else
                    {
                        MeshFilter filter = render.GetComponent<MeshFilter>();
                        if (filter != null) mesh = filter.sharedMesh;
                    }

                    if (mesh != null)
                    {
                        lodVerts += mesh.vertexCount;
                        lodTris += mesh.triangles.Length / 3;
                    }
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"LOD {i}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  頂点数 (Vertices): {lodVerts:N0}");
                EditorGUILayout.LabelField($"  三角ポリゴン数 (Tris): {lodTris:N0}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            return;
        }

        // 3. LODがない通常のオブジェクトの場合
        int totalVerts = 0;
        int totalTris = 0;

        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
        foreach (var m in meshFilters)
        {
            if (m.sharedMesh != null)
            {
                totalVerts += m.sharedMesh.vertexCount;
                totalTris += m.sharedMesh.triangles.Length / 3;
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var s in skinnedMeshRenderers)
        {
            if (s.sharedMesh != null)
            {
                totalVerts += s.sharedMesh.vertexCount;
                totalTris += s.sharedMesh.triangles.Length / 3;
            }
        }

        EditorGUILayout.HelpBox("LODは検出されませんでした（通常メッシュとして集計）", MessageType.Warning);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("合計 頂点数 (Vertices):", totalVerts.ToString("N0"));
        EditorGUILayout.LabelField("合計 三角ポリゴン数 (Tris):", totalTris.ToString("N0"));

        Repaint();
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}