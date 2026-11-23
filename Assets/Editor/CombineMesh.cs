using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshCombineTool
{
    [MenuItem("Tools/Combine Single Object (Keep Materials)")]
    private static void CombineKeepMaterials()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogWarning("結合したいオブジェクトの親（または対象）をHierarchyで選択してください。");
            return;
        }

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
        List<MeshFilter> validFilters = new List<MeshFilter>();
        foreach (var mf in meshFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mf.sharedMesh != null && mr != null && mr.enabled && mf.gameObject.activeInHierarchy)
            {
                validFilters.Add(mf);
            }
        }

        if (validFilters.Count == 0)
        {
            Debug.LogWarning("結合可能なMeshFilter / MeshRendererが見つかりませんでした。");
            return;
        }

        Matrix4x4 rootWorldToLocal = root.transform.worldToLocalMatrix;

        // 元のMaterialインスタンスをそのまま保持してサブメッシュごとにまとめる
        List<Material> materials = new List<Material>();
        Dictionary<Material, List<CombineInstance>> subMeshMap = new Dictionary<Material, List<CombineInstance>>();

        foreach (var mf in validFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            Material[] sharedMats = mr.sharedMaterials;
            Mesh mesh = mf.sharedMesh;

            int subMeshCount = Mathf.Min(mesh.subMeshCount, sharedMats.Length);
            for (int s = 0; s < subMeshCount; s++)
            {
                Material mat = sharedMats[s];
                if (mat == null) continue;

                if (!subMeshMap.ContainsKey(mat))
                {
                    materials.Add(mat);
                    subMeshMap[mat] = new List<CombineInstance>();
                }

                CombineInstance ci = new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = s,
                    transform = rootWorldToLocal * mf.transform.localToWorldMatrix
                };
                subMeshMap[mat].Add(ci);
            }
        }

        if (materials.Count == 0)
        {
            Debug.LogWarning("有効なマテリアルが見つかりませんでした。");
            return;
        }

        string saveFolder = "Assets/CombinedMeshes";
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            AssetDatabase.CreateFolder("Assets", "CombinedMeshes");
        }

        List<Mesh> tempSubMeshes = new List<Mesh>();
        List<CombineInstance> finalCombineInstances = new List<CombineInstance>();

        foreach (var mat in materials)
        {
            Mesh subCombined = new Mesh();
            subCombined.indexFormat = IndexFormat.UInt32;
            // 同一マテリアルのパーツを1つのメッシュに結合（元の頂点属性・法線・UVを維持）
            subCombined.CombineMeshes(subMeshMap[mat].ToArray(), true, true);
            tempSubMeshes.Add(subCombined);

            CombineInstance finalCi = new CombineInstance
            {
                mesh = subCombined,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
            finalCombineInstances.Add(finalCi);
        }

        Mesh singleMesh = new Mesh();
        singleMesh.indexFormat = IndexFormat.UInt32;
        // mergeSubMeshes: false にしてマテリアルごとのサブメッシュを保持
        singleMesh.CombineMeshes(finalCombineInstances.ToArray(), false, false);
        singleMesh.RecalculateBounds();

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(saveFolder, $"{root.name}_SingleMesh.asset")
        );
        AssetDatabase.CreateAsset(singleMesh, assetPath);

        foreach (var m in tempSubMeshes)
        {
            Object.DestroyImmediate(m);
        }

        GameObject mergedObj = new GameObject(root.name + "_Merged");
        mergedObj.transform.SetParent(root.transform.parent, false);
        mergedObj.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
        mergedObj.transform.localScale = root.transform.localScale;

        MeshFilter targetMf = mergedObj.AddComponent<MeshFilter>();
        targetMf.sharedMesh = singleMesh;

        MeshRenderer targetMr = mergedObj.AddComponent<MeshRenderer>();
        targetMr.sharedMaterials = materials.ToArray();
        targetMr.shadowCastingMode = ShadowCastingMode.On;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        root.SetActive(false);
        Selection.activeGameObject = mergedObj;

        Debug.Log($"マテリアルを維持したまま1オブジェクトに結合しました: {mergedObj.name}（サブメッシュ／マテリアル数: {materials.Count}）");
    }
}