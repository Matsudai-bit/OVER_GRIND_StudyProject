using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tutorial用動画をEditモード中に軽量な形式へ再インポートします。
/// </summary>
public static class TutorialVideoImportOptimizer
{
    private const string STANDALONE_PLATFORM = "Standalone";

    private static readonly string[] s_videoFolders =
    {
        "Assets/Develop/Design/Movie/AirAttack",
        "Assets/Develop/Design/Movie/Jump",
        "Assets/Develop/Design/Movie/Move",
        "Assets/Develop/Design/Movie/NormalAttack",
        "Assets/Develop/Design/Movie/TurboCharge"
    };

    private static bool s_isImporting;

    /// <summary>Editor起動後、Editモードになってから設定を確認します。</summary>
    [InitializeOnLoadMethod]
    private static void ScheduleOptimization()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += OptimizeMissingVideos;
    }

    /// <summary>Playモード終了後に未処理の動画を確認します。</summary>
    /// <param name="state">Playモードの変更状態。</param>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += OptimizeMissingVideos;
        }
    }

    /// <summary>設定が異なる動画だけ再インポートします。</summary>
    private static void OptimizeMissingVideos()
    {
        OptimizeVideos(forceReimport: false);
    }

    /// <summary>Play開始前にTutorial用動画をすべて再インポートします。</summary>
    [MenuItem("Tools/Tutorial/Reimport All Videos")]
    public static void ReimportAllVideos()
    {
        OptimizeVideos(forceReimport: true);
    }

    /// <summary>対象フォルダ内の動画にStandalone向け設定を適用します。</summary>
    /// <param name="forceReimport">設定済み動画も再インポートするか。</param>
    private static void OptimizeVideos(bool forceReimport)
    {
        if (s_isImporting || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Tutorial動画の再インポートはEditモードで実行してください。");
            return;
        }

        s_isImporting = true;
        try
        {
            string[] guids = AssetDatabase.FindAssets("t:VideoClip", s_videoFolders);
            Array.Sort(guids, StringComparer.Ordinal);

            int importedCount = 0;
            foreach (string guid in guids)
            {
                string videoPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!videoPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                VideoClipImporter importer = AssetImporter.GetAtPath(videoPath) as VideoClipImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"動画のImporterを取得できません。{videoPath}");
                    continue;
                }

                VideoImporterTargetSettings settings = importer.GetTargetSettings(STANDALONE_PLATFORM)
                    ?? new VideoImporterTargetSettings();

                bool settingsMatch = settings.enableTranscoding &&
                    settings.codec == VideoCodec.VP8 &&
                    settings.resizeMode == VideoResizeMode.HalfRes &&
                    settings.bitrateMode == VideoBitrateMode.High;

                if (settingsMatch && !forceReimport)
                {
                    continue;
                }

                settings.enableTranscoding = true;
                settings.codec = VideoCodec.VP8;
                settings.resizeMode = VideoResizeMode.HalfRes;
                settings.bitrateMode = VideoBitrateMode.High;
                importer.SetTargetSettings(STANDALONE_PLATFORM, settings);
                importer.SaveAndReimport();
                ++importedCount;
                Debug.Log($"Tutorial動画を再インポートしました。{videoPath}");
            }

            Debug.Log($"Tutorial動画の再インポート完了: {importedCount}本 / 対象{guids.Length}本");
        }
        finally
        {
            s_isImporting = false;
        }
    }
}
