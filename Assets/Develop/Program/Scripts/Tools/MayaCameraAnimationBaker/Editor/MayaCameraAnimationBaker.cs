using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// FBXの既存キーフレーム時刻でCameraを評価してCinemachine用カーブを生成します。
/// </summary>
internal static class MayaCameraAnimationBaker
{
    private const int CHANNEL_COUNT = 8;
    private const int MAX_SAMPLE_TIMES = 60000;
    private const float POSITION_CHANGE_SQR_THRESHOLD = 0.00000001f;
    private const float ROTATION_CHANGE_THRESHOLD = 0.001f;
    private const float FOV_CHANGE_THRESHOLD = 0.001f;
    internal const string FOV_PROPERTY_NAME = "Lens.FieldOfView";

    private static readonly string[] TRANSFORM_PROPERTY_NAMES =
    {
        "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
        "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
    };

    /// <summary>
    /// ベイク結果と変換先Cameraの初期値です。
    /// </summary>
    internal readonly struct BakeResult
    {
        internal AnimationClip Clip { get; }
        internal Vector3 InitialPosition { get; }
        internal Quaternion InitialRotation { get; }
        internal float InitialFieldOfView { get; }
        internal bool HasCameraAnimation { get; }

        internal BakeResult(AnimationClip clip, Vector3 position, Quaternion rotation,
            float fieldOfView, bool hasCameraAnimation)
        {
            Clip = clip;
            InitialPosition = position;
            InitialRotation = rotation;
            InitialFieldOfView = fieldOfView;
            HasCameraAnimation = hasCameraAnimation;
        }
    }

    /// <summary>
    /// 選択Cameraとその親階層の元キー時刻で、全Cameraを一度に評価します。
    /// </summary>
    internal static Dictionary<string, BakeResult> BakeMultiple(
        GameObject sourceFbx, AnimationClip sourceClip,
        IReadOnlyList<string> cameraPaths, Func<float, bool> checkCancellation = null)
    {
        if (sourceFbx == null || sourceClip == null || cameraPaths == null || cameraPaths.Count == 0)
        {
            throw new ArgumentException("FBX・AnimationClip・Cameraを指定してください。");
        }

        // Cameraごとに関連するTransform/Cameraのキーフレーム時刻だけを抽出します。
        Dictionary<string, SortedSet<float>> timesByCamera = CollectKeyTimes(sourceClip, cameraPaths);
        var allTimes = new SortedSet<float> { 0f };
        foreach (SortedSet<float> times in timesByCamera.Values)
        {
            allTimes.UnionWith(times);
        }
        if (allTimes.Count > MAX_SAMPLE_TIMES)
        {
            throw new InvalidOperationException($"元のキー時刻が{MAX_SAMPLE_TIMES}件を超えています。");
        }

        Scene previewScene = default;
        var generatedResults = new Dictionary<string, BakeResult>(StringComparer.Ordinal);
        try
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            GameObject previewRoot = PrefabUtility.InstantiatePrefab(sourceFbx, previewScene) as GameObject;
            if (previewRoot == null)
            {
                throw new InvalidOperationException("FBXのプレビューを作成できませんでした。");
            }

            var samplers = new List<CameraSampler>(cameraPaths.Count);
            foreach (string cameraPath in cameraPaths)
            {
                Transform cameraTransform = string.IsNullOrEmpty(cameraPath)
                    ? previewRoot.transform : previewRoot.transform.Find(cameraPath);
                if (cameraTransform == null || !cameraTransform.TryGetComponent(out Camera camera))
                {
                    throw new InvalidOperationException("FBX内にCameraがありません: " + cameraPath);
                }
                samplers.Add(new CameraSampler(cameraPath, camera, timesByCamera[cameraPath]));
            }

            // 最初の姿勢・FOVは、元のClipの時刻0で取得します（キーの追加とは別）。
            sourceClip.SampleAnimation(previewRoot, 0f);
            foreach (CameraSampler sampler in samplers)
            {
                sampler.CaptureInitial();
            }

            int processed = 0;
            foreach (float time in allTimes)
            {
                if ((processed % 32 == 0 || processed == allTimes.Count - 1) &&
                    checkCancellation != null && checkCancellation(processed / (float)allTimes.Count))
                {
                    throw new OperationCanceledException("変換をキャンセルしました。");
                }

                sourceClip.SampleAnimation(previewRoot, time);
                foreach (CameraSampler sampler in samplers)
                {
                    if (sampler.ContainsTime(time))
                    {
                        sampler.RecordSample(time);
                    }
                }
                processed++;
            }

            foreach (CameraSampler sampler in samplers)
            {
                generatedResults.Add(sampler.Path, sampler.CreateResult(sourceClip.frameRate));
            }
            return generatedResults;
        }
        catch
        {
            foreach (BakeResult result in generatedResults.Values)
            {
                if (result.Clip != null)
                {
                    UnityEngine.Object.DestroyImmediate(result.Clip);
                }
            }
            throw;
        }
        finally
        {
            if (previewScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }
    }

    /// <summary>
    /// Cameraおよび祖先Transformに関係するキー時刻を収集します。
    /// </summary>
    private static Dictionary<string, SortedSet<float>> CollectKeyTimes(
        AnimationClip sourceClip, IReadOnlyList<string> cameraPaths)
    {
        var result = new Dictionary<string, SortedSet<float>>(StringComparer.Ordinal);
        foreach (string path in cameraPaths)
        {
            if (result.ContainsKey(path))
            {
                throw new InvalidOperationException("Cameraパスが重複しています: " + path);
            }
            result.Add(path, new SortedSet<float>());
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
        {
            bool isTransform = binding.type == typeof(Transform);
            bool isCamera = binding.type == typeof(Camera);
            if (!isTransform && !isCamera)
            {
                continue;
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (curve == null)
            {
                continue;
            }

            foreach (KeyValuePair<string, SortedSet<float>> entry in result)
            {
                bool relevant = isCamera
                    ? entry.Key == binding.path
                    : IsSelfOrAncestorPath(binding.path, entry.Key);
                if (!relevant)
                {
                    continue;
                }
                foreach (Keyframe key in curve.keys)
                {
                    entry.Value.Add(key.time);
                }
            }
        }

        // 静止Cameraにも初期値を設定できるよう、キーがないCameraだけ0秒を生成します。
        foreach (SortedSet<float> times in result.Values)
        {
            if (times.Count == 0)
            {
                times.Add(0f);
            }
        }
        return result;
    }

    /// <summary>
    /// bindingPathがcameraPathの祖先または自身であるかを調べます。
    /// </summary>
    private static bool IsSelfOrAncestorPath(string bindingPath, string cameraPath)
    {
        return string.IsNullOrEmpty(bindingPath) ||
            bindingPath == cameraPath ||
            cameraPath.StartsWith(bindingPath + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Cameraのサンプル値から8本のカーブを作成します。
    /// </summary>
    private sealed class CameraSampler
    {
        private readonly Camera m_camera;
        private readonly SortedSet<float> m_times;
        private readonly List<Keyframe>[] m_channels;
        private Vector3 m_previousPosition;
        private Quaternion m_previousRotation;
        private float m_previousFieldOfView;
        private Vector3 m_initialPosition;
        private Quaternion m_initialRotation;
        private float m_initialFieldOfView;
        private bool m_hasCameraAnimation;
        private int m_sampleCount;

        internal string Path { get; }

        internal CameraSampler(string path, Camera camera, SortedSet<float> times)
        {
            Path = path;
            m_camera = camera;
            m_times = times;
            m_channels = new List<Keyframe>[CHANNEL_COUNT];
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                m_channels[i] = new List<Keyframe>(times.Count);
            }
        }

        internal bool ContainsTime(float time) => m_times.Contains(time);

        /// <summary>
        /// クリップの時刻0における初期値を記録します。
        /// </summary>
        internal void CaptureInitial()
        {
            ValidateCamera(0f);
            m_initialPosition = m_camera.transform.position;
            m_initialRotation = m_camera.transform.rotation;
            m_initialFieldOfView = m_camera.fieldOfView;
            m_previousPosition = m_initialPosition;
            m_previousRotation = m_initialRotation;
            m_previousFieldOfView = m_initialFieldOfView;
        }

        /// <summary>
        /// 元キーと同じ時刻のCamera値を記録します。
        /// </summary>
        internal void RecordSample(float time)
        {
            ValidateCamera(time);
            Vector3 position = m_camera.transform.position;
            Quaternion rotation = m_camera.transform.rotation;
            float fov = m_camera.fieldOfView;

            if (Quaternion.Dot(m_previousRotation, rotation) < 0f)
            {
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
            }

            if (!m_hasCameraAnimation)
            {
                m_hasCameraAnimation =
                    (position - m_previousPosition).sqrMagnitude > POSITION_CHANGE_SQR_THRESHOLD ||
                    Quaternion.Angle(m_previousRotation, rotation) > ROTATION_CHANGE_THRESHOLD ||
                    Mathf.Abs(fov - m_previousFieldOfView) > FOV_CHANGE_THRESHOLD;
            }

            m_previousPosition = position;
            m_previousRotation = rotation;
            m_previousFieldOfView = fov;
            m_sampleCount++;
            m_channels[0].Add(new Keyframe(time, position.x));
            m_channels[1].Add(new Keyframe(time, position.y));
            m_channels[2].Add(new Keyframe(time, position.z));
            m_channels[3].Add(new Keyframe(time, rotation.x));
            m_channels[4].Add(new Keyframe(time, rotation.y));
            m_channels[5].Add(new Keyframe(time, rotation.z));
            m_channels[6].Add(new Keyframe(time, rotation.w));
            m_channels[7].Add(new Keyframe(time, fov));
        }

        private void ValidateCamera(float time)
        {
            float fov = m_camera.fieldOfView;
            if (m_camera.orthographic)
            {
                throw new InvalidOperationException("Orthographic Cameraは未対応です: " + Path);
            }
            if (float.IsNaN(fov) || float.IsInfinity(fov) || fov <= 0f || fov >= 180f)
            {
                throw new InvalidOperationException($"FOVが不正です: {Path} / {time:F4}秒");
            }
        }

        /// <summary>
        /// 録画データを独立したCinemachineCamera用Clipに変換します。
        /// </summary>
        internal BakeResult CreateResult(float sourceFrameRate)
        {
            if (m_sampleCount == 0)
            {
                throw new InvalidOperationException("Cameraのキーが記録されませんでした: " + Path);
            }

            AnimationClip clip = new AnimationClip
            {
                frameRate = sourceFrameRate > 0f ? sourceFrameRate : 30f,
                wrapMode = WrapMode.Once
            };
            try
            {
                var bindings = new EditorCurveBinding[CHANNEL_COUNT];
                var curves = new AnimationCurve[CHANNEL_COUNT];
                for (int i = 0; i < TRANSFORM_PROPERTY_NAMES.Length; i++)
                {
                    bindings[i] = EditorCurveBinding.FloatCurve(
                        string.Empty, typeof(Transform), TRANSFORM_PROPERTY_NAMES[i]);
                    curves[i] = CreateLinearCurve(m_channels[i]);
                }
                bindings[7] = EditorCurveBinding.FloatCurve(
                    string.Empty, typeof(CinemachineCamera), FOV_PROPERTY_NAME);
                curves[7] = CreateLinearCurve(m_channels[7]);
                AnimationUtility.SetEditorCurves(clip, bindings, curves);
                clip.EnsureQuaternionContinuity();
                return new BakeResult(clip, m_initialPosition, m_initialRotation,
                    m_initialFieldOfView, m_hasCameraAnimation);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(clip);
                throw;
            }
        }
    }

    /// <summary>
    /// サンプル間を線形補間するカーブを作ります（時刻は元Clipと同一）。
    /// </summary>
    internal static AnimationCurve CreateLinearCurve(IReadOnlyList<Keyframe> samples)
    {
        var keys = new Keyframe[samples.Count];
        for (int i = 0; i < samples.Count; i++)
        {
            keys[i] = samples[i];
        }
        for (int i = 0; i < keys.Length; i++)
        {
            float inSlope = i > 0
                ? (keys[i].value - keys[i - 1].value) / (keys[i].time - keys[i - 1].time)
                : 0f;
            float outSlope = i < keys.Length - 1
                ? (keys[i + 1].value - keys[i].value) / (keys[i + 1].time - keys[i].time)
                : 0f;
            if (i == 0) { inSlope = outSlope; }
            if (i == keys.Length - 1) { outSlope = inSlope; }
            keys[i].inTangent = inSlope;
            keys[i].outTangent = outSlope;
        }
        return new AnimationCurve(keys);
    }
}
