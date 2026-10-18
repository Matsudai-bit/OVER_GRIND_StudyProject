using System;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Maya FBXのCameraと複数AnimationClipを一括変換するEditorウィンドウです。
/// </summary>
public sealed class MayaCameraConverterWindow : EditorWindow
{
    private const string DEFAULT_OUTPUT_FOLDER = "Assets/MayaCameraAnimations";

    /// <summary>
    /// 変換対象Clipと選択状態を保持します。
    /// </summary>
    [Serializable]
    private sealed class ClipEntry
    {
        // FBXから自動取得、またはProjectから手動指定したClip
        [SerializeField]
        private AnimationClip m_clip;
        [SerializeField]
        private bool m_isSelected = true;

        internal AnimationClip Clip
        {
            get => m_clip;
            set => m_clip = value;
        }

        internal bool IsSelected
        {
            get => m_isSelected;
            set => m_isSelected = value;
        }

        internal ClipEntry(AnimationClip clip)
        {
            m_clip = clip;
        }
    }

    // FBXと複数Clipの指定
    [SerializeField, Header("変換元")]
    private GameObject m_sourceFbx;
    [SerializeField]
    private List<ClipEntry> m_sourceClips = new List<ClipEntry>();
    [SerializeField]
    private bool m_showClips = true;

    // 1台のみかFBX内全Cameraかの選択
    [SerializeField, Header("変換対象")]
    private bool m_convertAllCameras;
    [SerializeField]
    private string m_cameraPath = string.Empty;
    [SerializeField]
    private bool m_showCameraPaths = true;

    // 単体モードのCamera指定と一括出力先
    [SerializeField, Header("変換先")]
    private CinemachineCamera m_targetCamera;
    [SerializeField]
    private string m_batchOutputFolder = DEFAULT_OUTPUT_FOLDER;
    [SerializeField]
    private bool m_includeOtherAnimations;

    private Vector2 m_scrollPosition;

    [MenuItem("Tools/Cutscene/Maya Camera Converter")]
    private static void OpenWindow()
    {
        GetWindow<MayaCameraConverterWindow>("Maya Camera Converter");
    }

    private void OnGUI()
    {
        if (m_sourceClips == null)
        {
            m_sourceClips = new List<ClipEntry>();
        }

        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
        EditorGUILayout.LabelField("変換元 (Maya FBX)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        m_sourceFbx = (GameObject)EditorGUILayout.ObjectField(
            "FBX Model", m_sourceFbx, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            m_cameraPath = string.Empty;
            m_targetCamera = null;
            m_sourceClips.Clear();
            AddFbxClips();
        }

        DrawClipSelector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("変換対象Camera", EditorStyles.boldLabel);
        int mode = EditorGUILayout.Popup("対象Camera",
            m_convertAllCameras ? 1 : 0,
            new[] { "選択した1台", "FBX内の全Camera" });
        m_convertAllCameras = mode == 1;

        List<MayaCameraConverterService.SourceCamera> cameras =
            MayaCameraConverterService.GetSourceCameras(m_sourceFbx);
        DrawCameraSelector(cameras);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("変換先", EditorStyles.boldLabel);
        m_includeOtherAnimations = EditorGUILayout.ToggleLeft(
            "カメラ以外のアニメーションも同じClipに含める", m_includeOtherAnimations);
        if (m_includeOtherAnimations)
        {
            EditorGUILayout.HelpBox(
                "有効: 元FBXの階層をシーンに展開し、CameraをCinemachineに置換します。" +
                "元Clipのその他のカーブとキー時刻を保持した統合.animをClipごとに作成します。" +
                "Timelineは統合RigのAnimatorにバインドしてください。", MessageType.Info);
        }
        if (!m_includeOtherAnimations && !m_convertAllCameras)
        {
            m_targetCamera = (CinemachineCamera)EditorGUILayout.ObjectField(
                "CinemachineCamera (任意)", m_targetCamera,
                typeof(CinemachineCamera), true);
        }

        List<AnimationClip> selectedClips = GetSelectedClips();
        bool requiresBatchFolder = m_includeOtherAnimations || m_convertAllCameras || selectedClips.Count > 1;
        if (requiresBatchFolder)
        {
            DrawOutputFolderSelector();
            if (!m_includeOtherAnimations)
            {
                EditorGUILayout.HelpBox(
                    "Camera×Clipごとに.animを作成し、CameraはClip間で共有します。",
                    MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Clipを1つ・Cameraを1台指定した場合は、変換時に.animの保存先を選択できます。",
                MessageType.Info);
        }

        EditorGUILayout.HelpBox(
            "出力キーは元Clipの関連するキー時刻に合わせます。" +
            "一定FPSでの再サンプリングは行いません。", MessageType.Info);

        int selectedCameraCount = m_convertAllCameras ? cameras.Count : Mathf.Min(1, cameras.Count);
        long outputCount = m_includeOtherAnimations
            ? selectedClips.Count : (long)selectedCameraCount * selectedClips.Count;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"選択Clip: {selectedClips.Count}件 / Camera: {selectedCameraCount}台 / 出力: {outputCount}件");

        using (new EditorGUI.DisabledScope(
            m_sourceFbx == null || selectedClips.Count == 0 || cameras.Count == 0 ||
            EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button($"{outputCount}件のAnimationClipを変換", GUILayout.Height(36)))
            {
                ConvertCameras(cameras, selectedClips, requiresBatchFolder);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 複数Clipをチェックボックスで選び、FBXから自動追加・手動追加します。
    /// </summary>
    private void DrawClipSelector()
    {
        EditorGUILayout.Space();
        m_showClips = EditorGUILayout.Foldout(
            m_showClips, $"Animation Clips ({m_sourceClips.Count})", true);

        if (m_showClips)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_sourceClips.Count; i++)
            {
                ClipEntry entry = m_sourceClips[i];
                if (entry == null)
                {
                    m_sourceClips.RemoveAt(i);
                    i--;
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    entry.IsSelected = EditorGUILayout.Toggle(entry.IsSelected, GUILayout.Width(20));
                    entry.Clip = (AnimationClip)EditorGUILayout.ObjectField(
                        entry.Clip, typeof(AnimationClip), false);
                    if (GUILayout.Button("削除", GUILayout.Width(44)))
                    {
                        m_sourceClips.RemoveAt(i);
                        i--;
                    }
                }
            }
            EditorGUI.indentLevel--;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(m_sourceFbx == null))
            {
                if (GUILayout.Button("FBXのClipを追加"))
                {
                    AddFbxClips();
                }
            }

            if (GUILayout.Button("+ Clip追加"))
            {
                m_sourceClips.Add(new ClipEntry(null));
                m_showClips = true;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("すべて選択"))
            {
                SetAllClipsSelected(true);
            }
            if (GUILayout.Button("すべて解除"))
            {
                SetAllClipsSelected(false);
            }
        }

        if (m_sourceFbx != null && m_sourceClips.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "AnimationClipが見つかりません。FBXのAnimation Importを確認するか、Clipを追加してください。",
                MessageType.Warning);
        }

        DrawSourceClipWarnings();
    }

    /// <summary>
    /// FBXサブアセットのAnimationClipを重複なく追加します。
    /// </summary>
    private void AddFbxClips()
    {
        if (m_sourceFbx == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(m_sourceFbx);
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        var registered = new HashSet<AnimationClip>();
        foreach (ClipEntry entry in m_sourceClips)
        {
            if (entry != null && entry.Clip != null)
            {
                registered.Add(entry.Clip);
            }
        }

        foreach (UnityEngine.Object asset in assets)
        {
            if (!(asset is AnimationClip clip) ||
                clip.name.StartsWith("__preview__", StringComparison.Ordinal) ||
                !registered.Add(clip))
            {
                continue;
            }
            m_sourceClips.Add(new ClipEntry(clip));
        }

        m_showClips = true;
    }

    /// <summary>
    /// Clipの選択状態をまとめて変更します。
    /// </summary>
    private void SetAllClipsSelected(bool selected)
    {
        foreach (ClipEntry entry in m_sourceClips)
        {
            if (entry != null)
            {
                entry.IsSelected = selected;
            }
        }
    }

    /// <summary>
    /// チェックが入っているClipを指定順で取得します。
    /// </summary>
    private List<AnimationClip> GetSelectedClips()
    {
        var result = new List<AnimationClip>();
        foreach (ClipEntry entry in m_sourceClips)
        {
            if (entry != null && entry.IsSelected)
            {
                result.Add(entry.Clip);
            }
        }
        return result;
    }

    /// <summary>
    /// 異なるFBX由来のClipや未設定のClipに注意を表示します。
    /// </summary>
    private void DrawSourceClipWarnings()
    {
        if (m_sourceFbx == null)
        {
            return;
        }

        string fbxPath = AssetDatabase.GetAssetPath(m_sourceFbx);
        var seen = new HashSet<AnimationClip>();
        foreach (ClipEntry entry in m_sourceClips)
        {
            if (entry == null || !entry.IsSelected)
            {
                continue;
            }

            if (entry.Clip == null)
            {
                EditorGUILayout.HelpBox("選択中にClip未設定の項目があります。", MessageType.Warning);
                return;
            }
            if (!seen.Add(entry.Clip))
            {
                EditorGUILayout.HelpBox("同じAnimationClipが複数回選択されています。", MessageType.Warning);
                return;
            }
            if (AssetDatabase.GetAssetPath(entry.Clip) != fbxPath)
            {
                EditorGUILayout.HelpBox(
                    "別アセット由来のClipがあります。Cameraの階層パスが一致することを確認してください。",
                    MessageType.Warning);
                return;
            }
        }
    }

    /// <summary>
    /// 単体モードでCameraを選択し、一括モードではCamera一覧を表示します。
    /// </summary>
    private void DrawCameraSelector(List<MayaCameraConverterService.SourceCamera> cameras)
    {
        if (m_sourceFbx == null)
        {
            return;
        }
        if (cameras.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Cameraがありません。FBXのModel Import SettingsでCamerasを有効にしてください。",
                MessageType.Warning);
            return;
        }

        if (!m_convertAllCameras)
        {
            string[] options = new string[cameras.Count];
            int selectedIndex = 0;
            for (int i = 0; i < cameras.Count; i++)
            {
                options[i] = string.IsNullOrEmpty(cameras[i].Path)
                    ? "(Root)"
                    : cameras[i].Path;
                if (m_cameraPath == cameras[i].Path)
                {
                    selectedIndex = i;
                }
            }
            selectedIndex = EditorGUILayout.Popup("Source Camera", selectedIndex, options);
            m_cameraPath = cameras[selectedIndex].Path;
            return;
        }

        m_showCameraPaths = EditorGUILayout.Foldout(
            m_showCameraPaths, $"変換予定のCamera: {cameras.Count}台", true);
        if (!m_showCameraPaths)
        {
            return;
        }
        EditorGUI.indentLevel++;
        foreach (MayaCameraConverterService.SourceCamera camera in cameras)
        {
            EditorGUILayout.LabelField(string.IsNullOrEmpty(camera.Path) ? "(Root)" : camera.Path);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// .animの一括出力先をAssets内から選択します。
    /// </summary>
    private void DrawOutputFolderSelector()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            m_batchOutputFolder = EditorGUILayout.TextField("Output Folder", m_batchOutputFolder);
            if (!GUILayout.Button("選択", GUILayout.Width(52)))
            {
                return;
            }

            string initialFolder = AssetDatabase.IsValidFolder(m_batchOutputFolder)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", m_batchOutputFolder))
                : Application.dataPath;
            string pickedFolder = EditorUtility.OpenFolderPanel(
                "AnimationClip出力フォルダ", initialFolder, string.Empty);
            if (string.IsNullOrEmpty(pickedFolder))
            {
                return;
            }

            string relativeFolder = FileUtil.GetProjectRelativePath(pickedFolder).Replace('\\', '/');
            if (string.IsNullOrEmpty(relativeFolder) ||
                (relativeFolder != "Assets" &&
                 !relativeFolder.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                EditorUtility.DisplayDialog("フォルダを選択できません",
                    "UnityプロジェクトのAssets以下を選択してください。", "OK");
                return;
            }
            m_batchOutputFolder = relativeFolder;
        }
    }

    /// <summary>
    /// 単体保存か一括保存かを判断して変換処理を実行します。
    /// </summary>
    private void ConvertCameras(
        List<MayaCameraConverterService.SourceCamera> cameras,
        List<AnimationClip> selectedClips, bool requiresBatchFolder)
    {
        try
        {
            List<MayaCameraConverterService.SourceCamera> selectedCameras =
                GetSelectedCameras(cameras);
            if (!ValidateInputs(selectedClips, selectedCameras))
            {
                return;
            }

            List<MayaCameraConverterService.ConversionItem> plan;
            if (requiresBatchFolder)
            {
                plan = MayaCameraConverterService.CreateBatchPlan(
                    m_sourceFbx, selectedClips, selectedCameras, m_batchOutputFolder,
                    m_includeOtherAnimations,
                    (m_includeOtherAnimations || m_convertAllCameras) ? null : m_targetCamera);
            }
            else
            {
                string suggestedName = selectedClips[0].name + "_Cinemachine";
                string assetPath = EditorUtility.SaveFilePanelInProject(
                    "AnimationClipの出力先", suggestedName, "anim",
                    "既存.animを指定した場合はカーブを上書きします。");
                if (string.IsNullOrEmpty(assetPath))
                {
                    return;
                }
                plan = MayaCameraConverterService.CreateSinglePlan(
                    selectedClips[0], selectedCameras[0].Path, assetPath, m_targetCamera);
            }

            MayaCameraConverterService.ValidatePlan(plan, m_includeOtherAnimations);
            int overwritten = MayaCameraConverterService.CountOverwrites(plan);
            if (overwritten > 0 && !EditorUtility.DisplayDialog(
                    "既存AnimationClipの上書き確認",
                    $"全{plan.Count}件のうち{overwritten}件の.animを上書きします。" +
                    "\nGUIDは維持されますが、内容は置き換わります。",
                    "上書きして変換", "キャンセル"))
            {
                return;
            }

            List<MayaCameraConverterService.ConversionResult> results =
                MayaCameraConverterService.Execute(
                    m_sourceFbx, selectedCameras, plan, m_includeOtherAnimations,
                    (label, progress) => EditorUtility.DisplayCancelableProgressBar(
                        "Maya Camera Converter", "キー時刻の評価: " + label, progress));

            if (!m_includeOtherAnimations && !m_convertAllCameras && results.Count > 0)
            {
                m_targetCamera = results[0].Camera;
            }
            if (results.Count > 0)
            {
                Selection.activeGameObject = results[0].Camera.gameObject;
                EditorGUIUtility.PingObject(results[0].Clip);
            }

            int staticCount = 0;
            foreach (MayaCameraConverterService.ConversionResult result in results)
            {
                string info = result.SourceClip.name + " / " + result.SourceCameraPath;
                if (!result.HasCameraAnimation)
                {
                    staticCount++;
                    Debug.LogWarning(
                        "[Maya Camera Converter] 位置・回転・FOVに変化がありません: " + info,
                        result.Clip);
                }
                Debug.Log("[Maya Camera Converter] " + info + " → " +
                    AssetDatabase.GetAssetPath(result.Clip), result.Clip);
            }

            string message = m_includeOtherAnimations
                ? $"{selectedClips.Count}個の統合.animを生成しました（Camera: {selectedCameras.Count}台）。"
                : $"{selectedClips.Count} Clip × {selectedCameras.Count} Camera = " +
                  $"{results.Count}件の.animを変換しました。";
            if (staticCount > 0)
            {
                message += $"\n変化のなかった組み合わせ: {staticCount}件（Consoleを確認）";
            }
            EditorUtility.DisplayDialog("変換完了", message, "OK");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[Maya Camera Converter] 変換をキャンセルしました。保存は行っていません。");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("変換に失敗しました", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 変換対象Cameraをリスト化します。
    /// </summary>
    private List<MayaCameraConverterService.SourceCamera> GetSelectedCameras(
        List<MayaCameraConverterService.SourceCamera> cameras)
    {
        if (m_convertAllCameras)
        {
            return new List<MayaCameraConverterService.SourceCamera>(cameras);
        }

        foreach (MayaCameraConverterService.SourceCamera camera in cameras)
        {
            if (camera.Path == m_cameraPath)
            {
                return new List<MayaCameraConverterService.SourceCamera> { camera };
            }
        }
        return new List<MayaCameraConverterService.SourceCamera>();
    }

    /// <summary>
    /// ClipとCameraの参照を検証します。
    /// </summary>
    /// <returns>
    /// true：変換できます。
    /// false：入力が不正です。
    /// </returns>
    private bool ValidateInputs(
        List<AnimationClip> sourceClips,
        List<MayaCameraConverterService.SourceCamera> cameras)
    {
        if (m_sourceFbx == null || !EditorUtility.IsPersistent(m_sourceFbx) ||
            !(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m_sourceFbx)) is ModelImporter))
        {
            EditorUtility.DisplayDialog("変換できません", "Project内のFBXモデルを指定してください。", "OK");
            return false;
        }
        if (cameras.Count == 0 || sourceClips.Count == 0)
        {
            EditorUtility.DisplayDialog("変換できません", "ClipとCameraを1件以上選択してください。", "OK");
            return false;
        }

        var uniqueClips = new HashSet<AnimationClip>();
        foreach (AnimationClip clip in sourceClips)
        {
            if (clip == null || !EditorUtility.IsPersistent(clip) || !uniqueClips.Add(clip))
            {
                EditorUtility.DisplayDialog("変換できません",
                    "未設定・重複、またはProject外のClipがあります。", "OK");
                return false;
            }
            if (float.IsNaN(clip.length) || float.IsInfinity(clip.length) || clip.length <= 0f)
            {
                EditorUtility.DisplayDialog("変換できません",
                    "再生時間が不正なClipがあります: " + clip.name, "OK");
                return false;
            }
        }
        return true;
    }
}
