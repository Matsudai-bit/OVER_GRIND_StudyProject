using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 複数Clip・複数Cameraの変換計画、保存、シーン構築を担当します。
/// </summary>
internal static class MayaCameraConverterService
{
    private const int MAX_NAME_LENGTH = 32;
    private const uint HASH_OFFSET = 2166136261;
    private const uint HASH_PRIME = 16777619;

    /// <summary>
    /// FBX内のCameraのパスと表示名です。
    /// </summary>
    internal readonly struct SourceCamera
    {
        internal string Path { get; }
        internal string Name { get; }

        internal SourceCamera(string path, string name)
        {
            Path = path;
            Name = name;
        }
    }

    /// <summary>
    /// 出力する1つのAnimationClipの設定です。
    /// </summary>
    internal sealed class ConversionItem
    {
        internal AnimationClip SourceClip { get; }
        internal string CameraPath { get; }
        internal string AssetPath { get; }
        internal string ObjectName { get; }
        internal CinemachineCamera TargetCamera { get; }

        internal ConversionItem(AnimationClip clip, string cameraPath, string assetPath,
            string objectName, CinemachineCamera targetCamera)
        {
            SourceClip = clip;
            CameraPath = cameraPath;
            AssetPath = assetPath;
            ObjectName = objectName;
            TargetCamera = targetCamera;
        }
    }

    /// <summary>
    /// 保存済みClipと変換後Cameraの参照です。
    /// </summary>
    internal readonly struct ConversionResult
    {
        internal AnimationClip SourceClip { get; }
        internal AnimationClip Clip { get; }
        internal CinemachineCamera Camera { get; }
        internal string SourceCameraPath { get; }
        internal bool HasCameraAnimation { get; }

        internal ConversionResult(AnimationClip sourceClip, AnimationClip clip,
            CinemachineCamera camera, string cameraPath, bool hasCameraAnimation)
        {
            SourceClip = sourceClip;
            Clip = clip;
            Camera = camera;
            SourceCameraPath = cameraPath;
            HasCameraAnimation = hasCameraAnimation;
        }
    }

    /// <summary>
    /// 元FBXに含まれるCameraを列挙します。
    /// </summary>
    internal static List<SourceCamera> GetSourceCameras(GameObject sourceFbx)
    {
        var cameras = new List<SourceCamera>();
        if (sourceFbx == null)
        {
            return cameras;
        }
        foreach (Camera camera in sourceFbx.GetComponentsInChildren<Camera>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(
                camera.transform, sourceFbx.transform);
            cameras.Add(new SourceCamera(path, camera.name));
        }
        cameras.Sort((a, b) => StringComparer.Ordinal.Compare(a.Path, b.Path));
        return cameras;
    }

    /// <summary>
    /// 個別Clipを任意の保存先に出力する計画です（Cameraのみモード）。
    /// </summary>
    internal static List<ConversionItem> CreateSinglePlan(
        AnimationClip sourceClip, string cameraPath, string assetPath,
        CinemachineCamera targetCamera)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath);
        string objectName = name.StartsWith("CM_", StringComparison.Ordinal) ? name : "CM_" + name;
        return new List<ConversionItem>
        {
            new ConversionItem(sourceClip, cameraPath, assetPath, objectName,
                targetCamera ?? FindExistingCamera(objectName))
        };
    }

    /// <summary>
    /// Cameraのみ：Camera×Clip件、統合：Clip件の出力計画を作ります。
    /// </summary>
    internal static List<ConversionItem> CreateBatchPlan(
        GameObject sourceFbx, IReadOnlyList<AnimationClip> sourceClips,
        IReadOnlyList<SourceCamera> cameras, string outputFolder,
        bool includeOtherAnimations, CinemachineCamera singleTargetCamera = null)
    {
        ValidateOutputFolder(outputFolder);
        if (sourceFbx == null || sourceClips == null || sourceClips.Count == 0 ||
            cameras == null || cameras.Count == 0)
        {
            throw new InvalidOperationException("FBX・Clip・Cameraを指定してください。");
        }
        if (singleTargetCamera != null && (cameras.Count != 1 || includeOtherAnimations))
        {
            throw new InvalidOperationException("Cameraを直接指定できるのはCameraのみ・1台モードです。");
        }

        string folder = outputFolder.Replace('\\', '/').TrimEnd('/');
        string fbxGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceFbx));
        var result = new List<ConversionItem>();

        if (includeOtherAnimations)
        {
            string rigName = GetRigName(sourceFbx);
            foreach (AnimationClip clip in sourceClips)
            {
                string clipKey = GetClipKey(clip);
                string fileName = "CM_Merged_" + GetSafeName(sourceFbx.name) + "_" +
                    GetSafeName(clip.name) + "_" + GetStableHash(fbxGuid + "|" + clipKey);
                result.Add(new ConversionItem(clip, null, folder + "/" + fileName + ".anim",
                    rigName, null));
            }
        }
        else
        {
            foreach (SourceCamera camera in cameras)
            {
                string cameraKey = fbxGuid + "|" + camera.Path;
                string cameraName = "CM_" + GetSafeName(sourceFbx.name) + "_" +
                    GetSafeName(camera.Name) + "_" + GetStableHash(cameraKey);
                CinemachineCamera target = singleTargetCamera ?? FindExistingCamera(cameraName);
                foreach (AnimationClip clip in sourceClips)
                {
                    string fileName = "CM_" + GetSafeName(sourceFbx.name) + "_" +
                        GetSafeName(clip.name) + "_" + GetSafeName(camera.Name) + "_" +
                        GetStableHash(fbxGuid + "|" + GetClipKey(clip) + "|" + camera.Path);
                    result.Add(new ConversionItem(clip, camera.Path,
                        folder + "/" + fileName + ".anim", cameraName, target));
                }
            }
        }

        ValidatePlan(result, includeOtherAnimations);
        return result;
    }

    /// <summary>
    /// Clip別に全Cameraを評価し、カメラ専用または統合Clipを生成します。
    /// </summary>
    internal static List<ConversionResult> Execute(
        GameObject sourceFbx, IReadOnlyList<SourceCamera> cameras,
        IReadOnlyList<ConversionItem> plan, bool includeOtherAnimations,
        Func<string, float, bool> checkCancellation = null)
    {
        ValidatePlan(plan, includeOtherAnimations);
        var cameraPaths = new List<string>();
        var uniquePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (SourceCamera camera in cameras)
        {
            if (!uniquePaths.Add(camera.Path))
            {
                throw new InvalidOperationException("同じ階層パスにCameraが複数あります: " + camera.Path);
            }
            cameraPaths.Add(camera.Path);
        }
        if (cameraPaths.Count == 0)
        {
            throw new InvalidOperationException("Cameraが選択されていません。");
        }

        var clipOrder = new List<AnimationClip>();
        foreach (ConversionItem item in plan)
        {
            if (!clipOrder.Contains(item.SourceClip))
            {
                clipOrder.Add(item.SourceClip);
            }
        }

        var bakedByClip = new Dictionary<AnimationClip,
            Dictionary<string, MayaCameraAnimationBaker.BakeResult>>();
        var generatedMerged = new Dictionary<AnimationClip, AnimationClip>();
        var results = new List<ConversionResult>();
        try
        {
            // 全Clipの評価が完了してからシーンとAssetを書き換えます。
            for (int i = 0; i < clipOrder.Count; i++)
            {
                AnimationClip sourceClip = clipOrder[i];
                int clipIndex = i;
                Dictionary<string, MayaCameraAnimationBaker.BakeResult> baked =
                    MayaCameraAnimationBaker.BakeMultiple(
                        sourceFbx, sourceClip, cameraPaths,
                        progress => checkCancellation != null && checkCancellation(
                            $"{sourceClip.name} ({clipIndex + 1}/{clipOrder.Count})",
                            (clipIndex + progress) / clipOrder.Count));
                bakedByClip.Add(sourceClip, baked);
            }

            if (includeOtherAnimations)
            {
                foreach (AnimationClip clip in clipOrder)
                {
                    generatedMerged.Add(clip, MayaCameraCombinedClipBuilder.Create(
                        clip, cameraPaths, bakedByClip[clip]));
                }
            }

            foreach (ConversionItem item in plan)
            {
                EnsureAssetFolder(Path.GetDirectoryName(item.AssetPath)?.Replace('\\', '/'));
            }

            // 統合モードはHierarchyのパス一致が必須なので、FBX階層を持つRigを1つ用意します。
            Dictionary<string, CinemachineCamera> rigCameras = includeOtherAnimations
                ? ConfigureRig(sourceFbx, cameras, bakedByClip[clipOrder[0]], plan[0].ObjectName)
                : null;
            var standAloneCameras = new Dictionary<string, CinemachineCamera>(StringComparer.Ordinal);

            foreach (ConversionItem item in plan)
            {
                AnimationClip generated = includeOtherAnimations
                    ? generatedMerged[item.SourceClip]
                    : bakedByClip[item.SourceClip][item.CameraPath].Clip;
                generated.name = Path.GetFileNameWithoutExtension(item.AssetPath);
                AnimationClip saved = SaveOrUpdateClip(generated, item.AssetPath);

                CinemachineCamera target;
                bool hasAnimation;
                if (includeOtherAnimations)
                {
                    target = rigCameras[cameraPaths[0]];
                    hasAnimation = false;
                    foreach (MayaCameraAnimationBaker.BakeResult cameraResult in bakedByClip[item.SourceClip].Values)
                    {
                        hasAnimation |= cameraResult.HasCameraAnimation;
                    }
                }
                else
                {
                    MayaCameraAnimationBaker.BakeResult bake = bakedByClip[item.SourceClip][item.CameraPath];
                    if (!standAloneCameras.TryGetValue(item.CameraPath, out target))
                    {
                        target = ConfigureStandaloneCamera(item, bake);
                        standAloneCameras.Add(item.CameraPath, target);
                    }
                    hasAnimation = bake.HasCameraAnimation;
                }
                results.Add(new ConversionResult(item.SourceClip, saved, target,
                    includeOtherAnimations ? "(統合)" : item.CameraPath, hasAnimation));
            }
            AssetDatabase.SaveAssets();
            return results;
        }
        catch (Exception exception)
        {
            Debug.LogError("[Maya Camera Converter] 変換失敗。出力済みのAssetとシーンを確認してください。" +
                "\n" + exception.Message);
            throw;
        }
        finally
        {
            foreach (Dictionary<string, MayaCameraAnimationBaker.BakeResult> baked in bakedByClip.Values)
            {
                foreach (MayaCameraAnimationBaker.BakeResult entry in baked.Values)
                {
                    if (entry.Clip != null && !EditorUtility.IsPersistent(entry.Clip))
                    {
                        UnityEngine.Object.DestroyImmediate(entry.Clip);
                    }
                }
            }
            foreach (AnimationClip clip in generatedMerged.Values)
            {
                if (clip != null && !EditorUtility.IsPersistent(clip))
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }
            }
        }
    }

    /// <summary>
    /// 計画の重複、Asset衝突、既存Cameraの構成を検証します。
    /// </summary>
    internal static void ValidatePlan(IReadOnlyList<ConversionItem> plan, bool includeOtherAnimations)
    {
        if (plan == null || plan.Count == 0)
        {
            throw new InvalidOperationException("出力するAnimationClipがありません。");
        }
        var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceClips = new HashSet<AnimationClip>();
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        var cameraTargets = new Dictionary<CinemachineCamera, string>();
        foreach (ConversionItem item in plan)
        {
            if (item.SourceClip == null || !EditorUtility.IsPersistent(item.SourceClip))
            {
                throw new InvalidOperationException("Project内の元Clipを指定してください。");
            }
            sourceClips.Add(item.SourceClip);
            if (!assets.Add(item.AssetPath) || !pairs.Add(
                GetClipKey(item.SourceClip) + "|" + item.CameraPath))
            {
                throw new InvalidOperationException("出力先またはCamera×Clipが重複しています: " + item.AssetPath);
            }
            UnityEngine.Object previous = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
            if (previous != null && !(previous is AnimationClip))
            {
                throw new InvalidOperationException("保存先に別のAssetがあります: " + item.AssetPath);
            }
            if (!includeOtherAnimations && item.TargetCamera != null)
            {
                ValidateStandaloneTarget(item.TargetCamera);
                if (cameraTargets.TryGetValue(item.TargetCamera, out string existingPath) &&
                    existingPath != item.CameraPath)
                {
                    throw new InvalidOperationException("異なるCameraで同じ変換先は使えません。");
                }
                cameraTargets[item.TargetCamera] = item.CameraPath;
            }
        }
        foreach (ConversionItem item in plan)
        {
            AnimationClip destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(item.AssetPath);
            if (destination != null && sourceClips.Contains(destination))
            {
                throw new InvalidOperationException("変換元Clipの上書きはできません。");
            }
        }
    }

    /// <summary>
    /// 上書き対象の出力Clip数を数えます。
    /// </summary>
    internal static int CountOverwrites(IReadOnlyList<ConversionItem> plan)
    {
        int count = 0;
        foreach (ConversionItem item in plan)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(item.AssetPath) != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 出力先フォルダがAssets以下の安全なパスか検証します。
    /// </summary>
    internal static void ValidateOutputFolder(string folder)
    {
        string normalized = folder?.Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized) ||
            (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal)) ||
            Path.IsPathRooted(normalized))
        {
            throw new InvalidOperationException("Assets以下の保存フォルダを指定してください。");
        }
        foreach (string part in normalized.Split('/'))
        {
            if (string.IsNullOrWhiteSpace(part) || part == "." || part == ".." ||
                part.IndexOfAny(new[] { '<', '>', ':', '"', '|', '?', '*' }) >= 0)
            {
                throw new InvalidOperationException("無効な保存先です: " + folder);
            }
        }
    }

    /// <summary>
    /// FBXの階層を保持し、CameraコンポーネントをCinemachineCameraへ置換します。
    /// </summary>
    private static Dictionary<string, CinemachineCamera> ConfigureRig(
        GameObject sourceFbx, IReadOnlyList<SourceCamera> cameras,
        IReadOnlyDictionary<string, MayaCameraAnimationBaker.BakeResult> firstBake,
        string rigName)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject rig = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rigName)
            {
                continue;
            }
            if (rig != null)
            {
                throw new InvalidOperationException("同名の統合Rigが複数あります: " + rigName);
            }
            rig = root;
        }

        if (rig == null)
        {
            rig = PrefabUtility.InstantiatePrefab(sourceFbx) as GameObject;
            if (rig == null)
            {
                throw new InvalidOperationException("FBXのシーンインスタンスを作成できませんでした。");
            }
            Undo.RegisterCreatedObjectUndo(rig, "Create Maya Cinemachine Rig");
            // 元FBXのカメラコンポーネントを削除するためPrefabを展開します。
            if (PrefabUtility.IsPartOfPrefabInstance(rig))
            {
                PrefabUtility.UnpackPrefabInstance(rig,
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            rig.name = rigName;
        }
        else if (!rig.TryGetComponent(out Animator _))
        {
            throw new InvalidOperationException(
                "同名オブジェクトが既にあります。統合Rigと区別できません: " + rigName);
        }

        var result = new Dictionary<string, CinemachineCamera>(StringComparer.Ordinal);
        foreach (SourceCamera source in cameras)
        {
            Transform targetTransform = string.IsNullOrEmpty(source.Path)
                ? rig.transform : rig.transform.Find(source.Path);
            if (targetTransform == null)
            {
                throw new InvalidOperationException(
                    "統合Rigの階層がFBXと一致しません。Rigを再生成してください: " + source.Path);
            }
            if (!targetTransform.TryGetComponent(out CinemachineCamera cmCamera))
            {
                cmCamera = Undo.AddComponent<CinemachineCamera>(targetTransform.gameObject);
            }

            if (cmCamera.GetComponents<CinemachineComponentBase>().Length > 0)
            {
                throw new InvalidOperationException(
                    "統合RigのCinemachineCameraはPosition/Rotation/NoiseをNoneにしてください: " + source.Path);
            }
            ConfigureLens(cmCamera, firstBake[source.Path].InitialFieldOfView);
            result.Add(source.Path, cmCamera);
        }

        // Maya由来のCameraは使わず、元のTransform・モデル・他のAnimation対象を残します。
        foreach (Camera camera in rig.GetComponentsInChildren<Camera>(true))
        {
            Undo.DestroyObjectImmediate(camera);
        }

        if (!rig.TryGetComponent(out Animator animator))
        {
            animator = Undo.AddComponent<Animator>(rig);
        }
        Undo.RecordObject(animator, "Configure Rig Animator");
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(rig.scene);
        return result;
    }

    /// <summary>
    /// 独立したCinemachineCameraとAnimatorを設定します。
    /// </summary>
    private static CinemachineCamera ConfigureStandaloneCamera(
        ConversionItem item, MayaCameraAnimationBaker.BakeResult bake)
    {
        CinemachineCamera target = item.TargetCamera;
        if (target == null)
        {
            GameObject cameraObject = new GameObject(item.ObjectName);
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Cinemachine Camera");
            target = Undo.AddComponent<CinemachineCamera>(cameraObject);
        }
        if (!target.TryGetComponent(out Animator animator))
        {
            animator = Undo.AddComponent<Animator>(target.gameObject);
        }
        Undo.RecordObject(target.transform, "Configure Camera Transform");
        target.transform.SetPositionAndRotation(bake.InitialPosition, bake.InitialRotation);
        ConfigureLens(target, bake.InitialFieldOfView);
        Undo.RecordObject(animator, "Configure Camera Animator");
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        return target;
    }

    private static void ConfigureLens(CinemachineCamera camera, float fov)
    {
        Undo.RecordObject(camera, "Configure Cinemachine Lens");
        camera.Target = default;
        LensSettings lens = camera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        lens.FieldOfView = fov;
        lens.Dutch = 0f;
        camera.Lens = lens;
        EditorUtility.SetDirty(camera);
    }

    private static string GetRigName(GameObject sourceFbx)
    {
        string fbxGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceFbx));
        return "CMRig_" + GetSafeName(sourceFbx.name) + "_" + GetStableHash(fbxGuid);
    }

    private static string GetClipKey(AnimationClip clip)
    {
        if (clip == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
            clip, out string guid, out long localId))
        {
            throw new InvalidOperationException("ClipのGUIDを取得できません。");
        }
        return guid + "|" + localId;
    }

    private static void EnsureAssetFolder(string folder)
    {
        ValidateOutputFolder(folder);
        string[] parts = folder.Split('/');
        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            if (!AssetDatabase.IsValidFolder(next))
            {
                throw new InvalidOperationException("出力フォルダ作成失敗: " + next);
            }
            current = next;
        }
    }

    private static AnimationClip SaveOrUpdateClip(AnimationClip generated, string assetPath)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(generated, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(generated, assetPath);
        return generated;
    }

    private static CinemachineCamera FindExistingCamera(string name)
    {
        CinemachineCamera found = null;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != name) { continue; }
            if (!root.TryGetComponent(out CinemachineCamera camera) || found != null)
            {
                throw new InvalidOperationException("同名のCamera候補があります: " + name);
            }
            found = camera;
        }
        return found;
    }

    private static void ValidateStandaloneTarget(CinemachineCamera target)
    {
        if (EditorUtility.IsPersistent(target) || target.transform.parent != null ||
            target.gameObject.scene != SceneManager.GetActiveScene())
        {
            throw new InvalidOperationException("変換先CameraはHierarchy直下を指定してください。");
        }
        if (target.GetComponents<CinemachineComponentBase>().Length > 0)
        {
            throw new InvalidOperationException("CinemachineのPosition/Rotation/Noise制御をNoneにしてください。");
        }
    }

    private static string GetSafeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { return "Unnamed"; }
        var builder = new StringBuilder();
        foreach (char character in name)
        {
            if (builder.Length >= MAX_NAME_LENGTH) { break; }
            builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_'
                ? character : '_');
        }
        string value = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(value) ? "Unnamed" : value;
    }

    private static string GetStableHash(string value)
    {
        uint hash = HASH_OFFSET;
        unchecked
        {
            foreach (byte item in Encoding.UTF8.GetBytes(value))
            {
                hash = (hash ^ item) * HASH_PRIME;
            }
        }
        return hash.ToString("x8");
    }
}
