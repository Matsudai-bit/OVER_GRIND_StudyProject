using System;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// MayaのFBXカメラをCinemachineCameraとAnimationClipへ変換するEditorウィンドウです。
/// </summary>
public sealed class MayaCameraConverterWindow : EditorWindow
{
    private const float AUTO_FRAME_RATE = 0f;
    private const float MAX_FRAME_RATE = 240f;
    private const int MAX_SAMPLE_STEPS = 60000;

    // 変換元のFBXとAnimationClip
    [SerializeField]
    private GameObject m_sourceFbx;
    [SerializeField]
    private AnimationClip m_sourceClip;
    [SerializeField]
    private string m_cameraPath = string.Empty;

    // 変換先のカメラとサンプリング設定
    [SerializeField]
    private CinemachineCamera m_targetCamera;
    [SerializeField]
    private float m_samplingFrameRate = AUTO_FRAME_RATE;

    [MenuItem("Tools/Cutscene/Maya Camera Converter")]
    private static void OpenWindow()
    {
        GetWindow<MayaCameraConverterWindow>("Maya Camera Converter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("変換元 (Maya FBX)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        m_sourceFbx = (GameObject)EditorGUILayout.ObjectField(
            "FBX Model", m_sourceFbx, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            m_cameraPath = string.Empty;
        }

        m_sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip", m_sourceClip, typeof(AnimationClip), false);
        DrawCameraSelector();
        DrawSourceClipWarning();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("変換先", EditorStyles.boldLabel);
        m_targetCamera = (CinemachineCamera)EditorGUILayout.ObjectField(
            "CinemachineCamera (任意)", m_targetCamera,
            typeof(CinemachineCamera), true);
        m_samplingFrameRate = Mathf.Clamp(
            EditorGUILayout.FloatField("Sampling FPS (0 = Clip FPS)", m_samplingFrameRate),
            AUTO_FRAME_RATE, MAX_FRAME_RATE);

        EditorGUILayout.HelpBox(
            "未指定のCinemachineCameraは自動作成します。" +
            "既存の変換先はHierarchy直下に置き、Position/Rotation ControlをNoneにしてください。" +
            "出力する.animはTimelineのAnimation Trackで再生します。",
            MessageType.Info);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(
            m_sourceFbx == null || m_sourceClip == null ||
            EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("CinemachineCamera + AnimationClip に変換", GUILayout.Height(32)))
            {
                ConvertCamera();
            }
        }
    }

    /// <summary>
    /// FBXに含まれるCameraを階層パスから選択します。
    /// </summary>
    private void DrawCameraSelector()
    {
        if (m_sourceFbx == null)
        {
            return;
        }

        Camera[] cameras = m_sourceFbx.GetComponentsInChildren<Camera>(true);
        if (cameras.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "FBX内にCameraがありません。Model Import Settings > Camerasを有効にしてください。",
                MessageType.Warning);
            return;
        }

        string[] paths = new string[cameras.Length];
        string[] labels = new string[cameras.Length];
        int selectedIndex = 0;

        for (int i = 0; i < cameras.Length; i++)
        {
            paths[i] = AnimationUtility.CalculateTransformPath(
                cameras[i].transform, m_sourceFbx.transform);
            labels[i] = string.IsNullOrEmpty(paths[i]) ? "(Root)" : paths[i];
            if (paths[i] == m_cameraPath)
            {
                selectedIndex = i;
            }
        }

        selectedIndex = EditorGUILayout.Popup("Source Camera", selectedIndex, labels);
        m_cameraPath = paths[selectedIndex];
    }

    /// <summary>
    /// 別のFBXに属するAnimationClipの誤指定を警告します。
    /// </summary>
    private void DrawSourceClipWarning()
    {
        if (m_sourceFbx == null || m_sourceClip == null)
        {
            return;
        }

        string fbxPath = AssetDatabase.GetAssetPath(m_sourceFbx);
        string clipPath = AssetDatabase.GetAssetPath(m_sourceClip);
        if (fbxPath != clipPath)
        {
            EditorGUILayout.HelpBox(
                "AnimationClipが選択FBXとは別のアセットにあります。" +
                "カメラの階層パスが一致していない場合、正しく変換されません。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// 入力を検証し、アニメーションの生成・保存とカメラの設定を実行します。
    /// </summary>
    private void ConvertCamera()
    {
        if (!ValidateInput(out float frameRate))
        {
            return;
        }

        string suggestedName = m_sourceClip.name + "_Cinemachine";
        string assetPath = EditorUtility.SaveFilePanelInProject(
            "変換したAnimationClipの保存先", suggestedName, "anim",
            "既存の.animを選ぶとアセットGUIDを維持して内容を更新します。");
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (existingAsset != null && !(existingAsset is AnimationClip))
        {
            EditorUtility.DisplayDialog("保存できません",
                "保存先にAnimationClip以外のアセットがあります。", "OK");
            return;
        }

        if (existingAsset != null &&
            !EditorUtility.DisplayDialog("既存のAnimationClipを更新しますか？",
                "この.animのカーブは上書きされます。必要ならバックアップしてください。",
                "更新する", "キャンセル"))
        {
            return;
        }

        AnimationClip generatedClip = null;
        try
        {
            MayaCameraAnimationBaker.BakeResult bakeResult = MayaCameraAnimationBaker.Bake(
                m_sourceFbx, m_sourceClip, m_cameraPath, frameRate);
            generatedClip = bakeResult.Clip;
            generatedClip.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            AnimationClip savedClip = SaveOrUpdateClip(generatedClip, assetPath);
            if (savedClip == generatedClip)
            {
                // CreateAsset時はUnity側に所有権が移るため破棄しません。
                generatedClip = null;
            }
            else
            {
                DestroyImmediate(generatedClip);
                generatedClip = null;
            }

            ConfigureTargetCamera(bakeResult, suggestedName);
            Selection.activeGameObject = m_targetCamera.gameObject;
            EditorGUIUtility.PingObject(savedClip);

            if (!bakeResult.HasCameraAnimation)
            {
                Debug.LogWarning(
                    "[Maya Camera Converter] カメラ位置・回転・FOVがサンプリング中に変化しませんでした。" +
                    "AnimationClipとFBX階層を確認してください。", savedClip);
            }

            Debug.Log($"[Maya Camera Converter] 変換完了: {assetPath} " +
                $"(FPS={frameRate:0.###}, Camera={m_cameraPath})", savedClip);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("変換に失敗しました", exception.Message, "OK");
        }
        finally
        {
            if (generatedClip != null)
            {
                DestroyImmediate(generatedClip);
            }
        }
    }

    /// <summary>
    /// 変換元・変換先・サンプル数の妥当性を確認します。
    /// </summary>
    /// <returns>
    /// true：変換可能です。
    /// false：設定を修正する必要があります。
    /// </returns>
    private bool ValidateInput(out float frameRate)
    {
        frameRate = 0f;
        if (m_sourceFbx == null || m_sourceClip == null ||
            !EditorUtility.IsPersistent(m_sourceFbx) ||
            !EditorUtility.IsPersistent(m_sourceClip))
        {
            EditorUtility.DisplayDialog("変換できません",
                "Project内のFBXモデルとAnimationClipを指定してください。", "OK");
            return false;
        }

        string fbxPath = AssetDatabase.GetAssetPath(m_sourceFbx);
        if (!(AssetImporter.GetAtPath(fbxPath) is ModelImporter))
        {
            EditorUtility.DisplayDialog("変換できません",
                "FBXなどのModelImporterで読み込まれたモデルを指定してください。", "OK");
            return false;
        }

        if (FindSourceCamera(m_sourceFbx, m_cameraPath) == null)
        {
            EditorUtility.DisplayDialog("変換できません",
                "FBX内に選択したCameraがありません。", "OK");
            return false;
        }

        if (m_targetCamera != null)
        {
            if (EditorUtility.IsPersistent(m_targetCamera) ||
                m_targetCamera.transform.parent != null)
            {
                EditorUtility.DisplayDialog("変換できません",
                    "変換先CinemachineCameraはHierarchy直下に配置してください。", "OK");
                return false;
            }

            if (m_targetCamera.GetComponents<CinemachineComponentBase>().Length > 0)
            {
                EditorUtility.DisplayDialog("変換できません",
                    "変換先にCinemachineのPosition/Rotation/Noise制御が残っています。" +
                    "これらをNone（コンポーネントなし）にしてください。", "OK");
                return false;
            }
        }

        frameRate = m_samplingFrameRate <= AUTO_FRAME_RATE
            ? m_sourceClip.frameRate
            : m_samplingFrameRate;
        frameRate = Mathf.Clamp(frameRate, 1f, MAX_FRAME_RATE);

        if (m_sourceClip.length <= 0f ||
            (double)m_sourceClip.length * frameRate > MAX_SAMPLE_STEPS)
        {
            EditorUtility.DisplayDialog("変換できません",
                "クリップ長が0、またはサンプル数が" + MAX_SAMPLE_STEPS + "を超えます。", "OK");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 指定したパスからCameraコンポーネントを取得します。
    /// </summary>
    private static Camera FindSourceCamera(GameObject root, string cameraPath)
    {
        Transform cameraTransform = string.IsNullOrEmpty(cameraPath)
            ? root.transform
            : root.transform.Find(cameraPath);
        return cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
    }

    /// <summary>
    /// 同一GUIDを維持してAnimationClipアセットを作成・更新します。
    /// </summary>
    private static AnimationClip SaveOrUpdateClip(AnimationClip generatedClip, string assetPath)
    {
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existingClip != null)
        {
            EditorUtility.CopySerialized(generatedClip, existingClip);
            EditorUtility.SetDirty(existingClip);
            AssetDatabase.SaveAssets();
            return existingClip;
        }

        AssetDatabase.CreateAsset(generatedClip, assetPath);
        AssetDatabase.SaveAssets();
        return generatedClip;
    }

    /// <summary>
    /// シーン上のCinemachineCameraとAnimatorを初期化します。
    /// </summary>
    private void ConfigureTargetCamera(MayaCameraAnimationBaker.BakeResult bakeResult,
        string suggestedName)
    {
        if (m_targetCamera == null)
        {
            GameObject cameraObject = new GameObject("CM_" + suggestedName);
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Cinemachine Camera");
            m_targetCamera = Undo.AddComponent<CinemachineCamera>(cameraObject);
        }

        if (!m_targetCamera.TryGetComponent(out Animator animator))
        {
            animator = Undo.AddComponent<Animator>(m_targetCamera.gameObject);
        }

        Undo.RecordObject(m_targetCamera.transform, "Configure Cinemachine Transform");
        m_targetCamera.transform.SetPositionAndRotation(
            bakeResult.InitialPosition, bakeResult.InitialRotation);

        Undo.RecordObject(m_targetCamera, "Configure Cinemachine Lens");
        // 既存の追従設定やレンズの追加ロールがベイク済み姿勢に干渉しないようにします。
        m_targetCamera.Target = default;
        LensSettings lens = m_targetCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        lens.FieldOfView = bakeResult.InitialFieldOfView;
        lens.Dutch = 0f;
        m_targetCamera.Lens = lens;
        EditorUtility.SetDirty(m_targetCamera);

        Undo.RecordObject(animator, "Configure Cinemachine Animator");
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.applyRootMotion = false;
        EditorUtility.SetDirty(animator);

        EditorSceneManager.MarkSceneDirty(m_targetCamera.gameObject.scene);
    }
}
