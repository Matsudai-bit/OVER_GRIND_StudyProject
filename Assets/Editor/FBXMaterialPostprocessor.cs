using UnityEngine;
using UnityEditor;
using System.IO;

public class FBXMaterialPostprocessor : AssetPostprocessor
{
    private const string SHADER_PATH = "Assets/PreDevelop/Haruki/ShaderGraph/SG_main.shadergraph";

    // 1. モデルインポート開始時: マテリアルが確実に読み込まれる設定を強制する
    void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        if (importer != null)
        {
            // Unity6のデフォルトでマテリアルがインポートされない設定になっているのを防ぐ
            if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            }
        }
    }

    // 2. インポート完全完了後: 確実に生成されたマテリアルを検知してリマップ＆再適用
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            // FBXファイル以外は無視する
            if (!assetPath.ToLower().EndsWith(".fbx")) continue;

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            bool needsReimport = false;
            string fbxDirectory = Path.GetDirectoryName(assetPath);

            // 現在リマップ（紐づけ）されているリストを取得
            var externalMap = importer.GetExternalObjectMap();

            // インポートが完了したFBXファイル内部のデータを全て取得（ここでは確実に取得できる）
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (Object asset in assets)
            {
                // FBX内部のマテリアルデータを検知
                if (asset is Material fbxMaterial)
                {
                    string matName = fbxMaterial.name;
                    var sourceId = new AssetImporter.SourceAssetIdentifier(typeof(Material), matName);

                    // 既に外部マテリアルが紐づいている場合はスキップ（無限ループ防止）
                    if (externalMap.ContainsKey(sourceId) && externalMap[sourceId] != null)
                    {
                        continue;
                    }

                    Material targetMaterial = null;

                    // 判定1. FBXと同じ階層に同名のマテリアルファイルがあるか？
                    string expectedMaterialPath = $"{fbxDirectory}/{matName}.mat".Replace("\\", "/");
                    targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(expectedMaterialPath);

                    // 判定2. なければ、プロジェクト内の他の場所に同名のマテリアルがあるか？（共通化対応）
                    if (targetMaterial == null)
                    {
                        string[] guids = AssetDatabase.FindAssets($"t:Material {matName}");
                        foreach (string guid in guids)
                        {
                            string foundPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (Path.GetFileNameWithoutExtension(foundPath) == matName)
                            {
                                targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(foundPath);
                                break;
                            }
                        }
                    }

                    // 判定3. どこにもなければ、指定したShader Graphで新規マテリアルを作成する
                    if (targetMaterial == null)
                    {
                        Shader customShader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
                        targetMaterial = new Material(customShader != null ? customShader : Shader.Find("Standard"))
                        {
                            name = matName
                        };
                        AssetDatabase.CreateAsset(targetMaterial, expectedMaterialPath);
                    }

                    // 取得・作成したマテリアルを、FBXに紐づける（リマップ設定）
                    importer.AddRemap(sourceId, targetMaterial);
                    needsReimport = true;
                }
            }

            // 新たに紐づけ設定が行われた場合のみ、設定を保存して自動で再インポート（適用）する
            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }
    }
}