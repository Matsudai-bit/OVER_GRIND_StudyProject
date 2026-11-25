#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioCueIdの定義からAnimation Event用の参照アセットを一括作成します。
/// </summary>
public static class AudioCueReferenceAssetGenerator
{
    private const string AUDIO_FOLDER = "Assets/Audio";
    private const string OUTPUT_FOLDER = "Assets/Audio/CueReferences";
    private const string ASSET_PREFIX = "AudioCue_";

    /// <summary>
    /// 未作成の音声演出参照アセットを生成します。既存アセットは上書きしません。
    /// </summary>
    [MenuItem("Tools/Audio/Generate Cue Reference Assets")]
    public static void GenerateCueReferenceAssets()
    {
        // 共通の参照先ディレクトリを用意します。
        if (!AssetDatabase.IsValidFolder(AUDIO_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets", "Audio");
        }

        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
        {
            AssetDatabase.CreateFolder(AUDIO_FOLDER, "CueReferences");
        }

        int createdCount = 0;
        int skippedCount = 0;
        HashSet<int> usedValues = new HashSet<int>();

        foreach (AudioCueId cueId in Enum.GetValues(typeof(AudioCueId)))
        {
            if (cueId == AudioCueId.NONE || !usedValues.Add((int)cueId))
            {
                continue;
            }

            string assetPath = $"{OUTPUT_FOLDER}/{ASSET_PREFIX}{cueId}.asset";
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existing != null)
            {
                // GUIDを維持するため、同名アセットがあっても変更・置換しません。
                skippedCount++;
                if (existing is AudioCueReferenceAsset existingReference &&
                    existingReference.CueId != cueId)
                {
                    Debug.LogWarning(
                        $"[AudioCueReferenceAssetGenerator] 既存アセットのIDが異なります: {assetPath}");
                }
                continue;
            }

            AudioCueReferenceAsset reference = ScriptableObject.CreateInstance<AudioCueReferenceAsset>();
            SerializedObject serializedObject = new SerializedObject(reference);
            SerializedProperty cueIdProperty = serializedObject.FindProperty("m_cueId");
            if (cueIdProperty == null)
            {
                UnityEngine.Object.DestroyImmediate(reference);
                Debug.LogError("[AudioCueReferenceAssetGenerator] CueIdのフィールドが見つかりません。");
                return;
            }

            // enumの数値をそのまま保存します。enumの並び順には依存しません。
            cueIdProperty.intValue = (int)cueId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(reference, assetPath);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[AudioCueReferenceAssetGenerator] 作成: {createdCount}件、既存のためスキップ: {skippedCount}件。出力先: {OUTPUT_FOLDER}");
    }
}
#endif
