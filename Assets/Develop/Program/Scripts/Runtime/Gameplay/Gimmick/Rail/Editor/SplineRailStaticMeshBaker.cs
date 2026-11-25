using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SplineMesh;

/// <summary>
/// 選択した静的レールの生成Meshをアセット化します。
/// Editor専用です。必ずEditorフォルダーに配置してください。
/// </summary>
public static class SplineRailStaticMeshBaker
{
    private const string ASSET_ROOT_FOLDER = "Assets/Generated/RailMeshes";
    private const string MENU_BAKE = "Tools/Rails/Static/Bake Selected Rail Meshes";
    private const string MENU_EDIT = "Tools/Rails/Static/Enable Editing For Selected Rails";

    /// <summary>
    /// 選択したレールをアセット化し、静的メッシュとして固定します。
    /// </summary>
    [MenuItem(MENU_BAKE)]
    public static void BakeSelectedRailMeshes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playモード中は静的レールをベイクできません。");
            return;
        }

        List<GameObject> roots = GetSelectedRoots();
        if (roots.Count == 0)
        {
            Debug.LogWarning("シーン上のレールのルートGameObjectを選択してください。");
            return;
        }

        EnsureAssetFolder();
        int bakedCount = 0;

        foreach (GameObject root in roots)
        {
            bakedCount += BakeRail(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"静的レールのベイクが完了しました。Mesh数: {bakedCount}");
    }

    /// <summary>
    /// ベイク済みMeshを複製して編集用Meshに戻し、ジェネレーターを有効化します。
    /// </summary>
    [MenuItem(MENU_EDIT)]
    public static void EnableEditingForSelectedRails()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Playモード中は静的レールの編集状態を変更できません。");
            return;
        }

        List<GameObject> roots = GetSelectedRoots();
        if (roots.Count == 0)
        {
            Debug.LogWarning("シーン上のレールのルートGameObjectを選択してください。");
            return;
        }

        foreach (GameObject root in roots)
        {
            // 先に保存済みアセットから分離する。順序を間違えると元アセットが上書きされる。
            MeshBender[] benders = root.GetComponentsInChildren<MeshBender>(true);
            foreach (MeshBender bender in benders)
            {
                MeshFilter meshFilter = bender.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Mesh originalMesh = meshFilter.sharedMesh;
                if (!AssetDatabase.Contains(originalMesh))
                {
                    continue;
                }

                Mesh editingMesh = Object.Instantiate(originalMesh);
                editingMesh.name = originalMesh.name + " (Editing)";

                Undo.RecordObject(meshFilter, "Unbake Rail Mesh");
                meshFilter.sharedMesh = editingMesh;
                RecordPrefabModification(meshFilter);

                MeshCollider collider = bender.GetComponent<MeshCollider>();
                if (collider != null && collider.sharedMesh == originalMesh)
                {
                    Undo.RecordObject(collider, "Unbake Rail Collider");
                    collider.sharedMesh = editingMesh;
                    RecordPrefabModification(collider);
                }
            }

            // 編集用Meshへの切り替え完了後に生成処理を再開する。
            foreach (SplineMesh.Spline spline in root.GetComponentsInChildren<SplineMesh.Spline>(true))
            {
                SetEnabledWithUndo(spline, true);
            }

            foreach (SplineSmoother smoother in root.GetComponentsInChildren<SplineSmoother>(true))
            {
                SetEnabledWithUndo(smoother, true);
            }

            foreach (SplineMeshSynchronizer synchronizer in root.GetComponentsInChildren<SplineMeshSynchronizer>(true))
            {
                SetEnabledWithUndo(synchronizer, true);
            }

            foreach (MeshBender bender in benders)
            {
                SetEnabledWithUndo(bender, true);
            }

            foreach (SplineMeshTiling tiling in root.GetComponentsInChildren<SplineMeshTiling>(true))
            {
                Undo.RecordObject(tiling, "Enable Static Rail Editing");
                tiling.updateInPlayMode = false;
                tiling.enabled = true;
                RecordPrefabModification(tiling);
            }

            MarkRootSceneDirty(root);
        }

        Debug.Log("静的レールを編集モードに戻しました。編集後に再度Bakeしてください。");
    }

    /// <summary>
    /// 1レール配下の生成Meshをベイクします。
    /// </summary>
    private static int BakeRail(GameObject root)
    {
        MeshBender[] benders = root.GetComponentsInChildren<MeshBender>(true);

        // アセットを参照したまま生成が有効になっている場合、外部アセットを書き換える危険がある。
        if (HasUnsafeEditableAssetReference(root, benders))
        {
            Debug.LogError($"{root.name}: 保存済みMeshを生成処理が参照しています。先に「Enable Editing」を実行してください。", root);
            return 0;
        }

        // 編集中のレールは、同期・Tiling・MeshBenderの順番で即時更新する。
        foreach (SplineMeshSynchronizer synchronizer in root.GetComponentsInChildren<SplineMeshSynchronizer>(true))
        {
            if (synchronizer.enabled)
            {
                synchronizer.SyncSpline();
            }
        }

        foreach (SplineMeshTiling tiling in root.GetComponentsInChildren<SplineMeshTiling>(true))
        {
            if (tiling.enabled)
            {
                tiling.CreateMeshes();
            }
        }

        // Tilingによって生成された子オブジェクトも対象にする。
        benders = root.GetComponentsInChildren<MeshBender>(true);
        if (benders.Length == 0)
        {
            Debug.LogWarning($"{root.name}: MeshBenderが見つかりません。", root);
            return 0;
        }

        foreach (MeshBender bender in benders)
        {
            if (bender.enabled)
            {
                bender.ComputeIfNeeded();
            }
        }

        // 不完全なメッシュがある場合は、生成処理を無効化せずに中止する。
        foreach (MeshBender bender in benders)
        {
            MeshFilter filter = bender.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
            {
                Debug.LogError($"{bender.name}: Meshの生成が完了していません。ベイクを中止します。", bender);
                return 0;
            }
        }

        // AssetDatabaseのAssetパスは、コンポーネントごとに安定したIDを使う。
        int bakedCount = 0;
        foreach (MeshBender bender in benders)
        {
            MeshFilter meshFilter = bender.GetComponent<MeshFilter>();
            Mesh sourceMesh = meshFilter.sharedMesh;
            string assetPath = GetMeshAssetPath(bender);
            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

            if (savedMesh == null)
            {
                Mesh meshCopy = Object.Instantiate(sourceMesh);
                meshCopy.name = bender.name + " (Baked)";
                AssetDatabase.CreateAsset(meshCopy, assetPath);
                savedMesh = meshCopy;
            }
            else if (sourceMesh != savedMesh)
            {
                // GUIDを維持したまま既存のMeshアセットを再ベイクする。
                Undo.RecordObject(savedMesh, "Rebake Rail Mesh Asset");
                EditorUtility.CopySerialized(sourceMesh, savedMesh);
                EditorUtility.SetDirty(savedMesh);
            }

            if (meshFilter.sharedMesh != savedMesh)
            {
                Undo.RecordObject(meshFilter, "Bake Rail Mesh");
                meshFilter.sharedMesh = savedMesh;
                RecordPrefabModification(meshFilter);
            }

            // 描画Meshをそのまま使っていたColliderのみ、保存先への参照を付け替える。
            MeshCollider collider = bender.GetComponent<MeshCollider>();
            if (collider != null && collider.sharedMesh == sourceMesh && collider.sharedMesh != savedMesh)
            {
                Undo.RecordObject(collider, "Bake Rail Mesh Collider");
                collider.sharedMesh = savedMesh;
                RecordPrefabModification(collider);
            }

            SetEnabledWithUndo(bender, false);
            bakedCount++;
        }

        // 静的レールはSceneを開いたときもPlay中も生成処理を走らせない。
        foreach (SplineMeshTiling tiling in root.GetComponentsInChildren<SplineMeshTiling>(true))
        {
            SetEnabledWithUndo(tiling, false);
        }

        foreach (SplineMeshSynchronizer synchronizer in root.GetComponentsInChildren<SplineMeshSynchronizer>(true))
        {
            SetEnabledWithUndo(synchronizer, false);
        }

        foreach (SplineSmoother smoother in root.GetComponentsInChildren<SplineSmoother>(true))
        {
            SetEnabledWithUndo(smoother, false);
        }

        foreach (SplineMesh.Spline spline in root.GetComponentsInChildren<SplineMesh.Spline>(true))
        {
            SetEnabledWithUndo(spline, false);
        }

        MarkRootSceneDirty(root);
        return bakedCount;
    }

    /// <summary>
    /// ベイク済みAssetが有効なジェネレーターで書き換えられる危険があるか調べます。
    /// </summary>
    /// <returns>true：危険。false：安全。</returns>
    private static bool HasUnsafeEditableAssetReference(GameObject root, MeshBender[] benders)
    {
        bool anyGeneratorEnabled = false;

        foreach (MeshBender bender in benders)
        {
            if (bender.enabled)
            {
                anyGeneratorEnabled = true;
                break;
            }
        }

        foreach (SplineMeshTiling tiling in root.GetComponentsInChildren<SplineMeshTiling>(true))
        {
            anyGeneratorEnabled |= tiling.enabled;
        }

        if (!anyGeneratorEnabled)
        {
            return false;
        }

        foreach (MeshBender bender in benders)
        {
            MeshFilter filter = bender.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && AssetDatabase.Contains(filter.sharedMesh))
            {
                return true;
            }
        }

        return false;
    }

    private static void SetEnabledWithUndo(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null && behaviour.enabled != enabled)
        {
            Undo.RecordObject(behaviour, "Change Static Rail Generation State");
            behaviour.enabled = enabled;
            RecordPrefabModification(behaviour);
        }
    }

    private static void RecordPrefabModification(Object component)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(component))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    private static void MarkRootSceneDirty(GameObject root)
    {
        if (root.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(root.scene);
        }
    }

    /// <summary>
    /// MeshBenderの安定した識別子からAssetパスを作ります。
    /// </summary>
    private static string GetMeshAssetPath(MeshBender bender)
    {
        string globalId = GlobalObjectId.GetGlobalObjectIdSlow(bender).ToString();
        string safeName = globalId.Replace("GlobalObjectId_V1-", string.Empty)
                                  .Replace(':', '_').Replace('/', '_');
        return $"{ASSET_ROOT_FOLDER}/Rail_{safeName}.asset";
    }

    private static void EnsureAssetFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(ASSET_ROOT_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "RailMeshes");
        }
    }

    /// <summary>
    /// シーン上で選択されたルートオブジェクトを取得します。
    /// </summary>
    private static List<GameObject> GetSelectedRoots()
    {
        List<GameObject> result = new List<GameObject>();
        GameObject[] selected = Selection.gameObjects;

        foreach (GameObject gameObject in selected)
        {
            // 保存済みシーンのオブジェクトにのみ使用する。
            if (gameObject == null || !gameObject.scene.IsValid() ||
                string.IsNullOrEmpty(gameObject.scene.path) ||
                PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                continue;
            }

            bool hasSelectedAncestor = false;
            Transform parent = gameObject.transform.parent;
            while (parent != null)
            {
                foreach (GameObject other in selected)
                {
                    if (parent.gameObject == other)
                    {
                        hasSelectedAncestor = true;
                        break;
                    }
                }

                if (hasSelectedAncestor)
                {
                    break;
                }

                parent = parent.parent;
            }

            if (!hasSelectedAncestor)
            {
                result.Add(gameObject);
            }
        }

        return result;
    }
}