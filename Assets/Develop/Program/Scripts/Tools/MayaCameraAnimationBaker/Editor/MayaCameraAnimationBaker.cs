using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FBXのカメラアニメーションをCinemachineCamera用のAnimationClipへ焼き込みます。
/// </summary>
internal static class MayaCameraAnimationBaker
{
    private const int CHANNEL_COUNT = 8;
    private const float DEFAULT_FIELD_OF_VIEW = 60f;
    private const string FOV_PROPERTY_NAME = "Lens.FieldOfView";
    private const float POSITION_CHANGE_SQR_THRESHOLD = 0.00000001f;
    private const float ROTATION_CHANGE_THRESHOLD = 0.001f;
    private const float FOV_CHANGE_THRESHOLD = 0.001f;

    // 変換先Transformのアニメーションプロパティ
    private static readonly string[] TRANSFORM_PROPERTY_NAMES =
    {
        "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
        "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
    };

    /// <summary>
    /// ベイク結果と初期カメラ姿勢を保持します。
    /// </summary>
    internal readonly struct BakeResult
    {
        /// <summary>生成したAnimationClip。</summary>
        public AnimationClip Clip { get; }
        /// <summary>カメラの初期位置。</summary>
        public Vector3 InitialPosition { get; }
        /// <summary>カメラの初期回転。</summary>
        public Quaternion InitialRotation { get; }
        /// <summary>カメラの初期垂直FOV。</summary>
        public float InitialFieldOfView { get; }
        /// <summary>サンプリング間にカメラの変化があったか。</summary>
        public bool HasCameraAnimation { get; }

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
    /// 元FBXを一時的に評価し、位置・回転・垂直FOVの8カーブを生成します。
    /// </summary>
    internal static BakeResult Bake(GameObject sourceFbx, AnimationClip sourceClip,
        string cameraPath, float sampleFrameRate)
    {
        GameObject previewRoot = null;

        try
        {
            // 元FBXアセットを変更せず、一時インスタンス上でアニメーションを評価します。
            previewRoot = PrefabUtility.InstantiatePrefab(sourceFbx) as GameObject;
            if (previewRoot == null)
            {
                throw new InvalidOperationException("FBXを一時インスタンス化できませんでした。");
            }

            previewRoot.hideFlags = HideFlags.HideAndDontSave;
            Transform cameraTransform = string.IsNullOrEmpty(cameraPath)
                ? previewRoot.transform
                : previewRoot.transform.Find(cameraPath);

            if (cameraTransform == null ||
                !cameraTransform.TryGetComponent(out Camera sourceCamera))
            {
                throw new InvalidOperationException("指定したFBX階層のCameraが見つかりません。");
            }

            List<Keyframe>[] channels = new List<Keyframe>[CHANNEL_COUNT];
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new List<Keyframe>();
            }

            // FPSの端数も保ったままサンプリングし、末尾時刻を必ず含めます。
            int sampleSteps = Mathf.CeilToInt(sourceClip.length * sampleFrameRate);
            Quaternion previousRotation = Quaternion.identity;
            Vector3 previousPosition = Vector3.zero;
            float previousFieldOfView = 0f;
            Vector3 initialPosition = Vector3.zero;
            Quaternion initialRotation = Quaternion.identity;
            float initialFieldOfView = DEFAULT_FIELD_OF_VIEW;
            bool hasCameraAnimation = false;

            for (int i = 0; i <= sampleSteps; i++)
            {
                float time = i == sampleSteps
                    ? sourceClip.length
                    : i / sampleFrameRate;

                sourceClip.SampleAnimation(previewRoot, time);

                if (sourceCamera.orthographic)
                {
                    throw new InvalidOperationException(
                        "Orthographic Cameraは未対応です。Perspective Cameraを指定してください。");
                }

                Vector3 position = sourceCamera.transform.position;
                Quaternion rotation = sourceCamera.transform.rotation;
                float fieldOfView = sourceCamera.fieldOfView;

                if (float.IsNaN(fieldOfView) || float.IsInfinity(fieldOfView) ||
                    fieldOfView <= 0f || fieldOfView >= 180f)
                {
                    throw new InvalidOperationException($"不正なFOVを検出しました（時刻: {time:F3}秒）。");
                }

                // Quaternionのq/-q表現を揃え、回転の飛びを防ぎます。
                if (i > 0 && Quaternion.Dot(previousRotation, rotation) < 0f)
                {
                    rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                }

                if (i == 0)
                {
                    initialPosition = position;
                    initialRotation = rotation;
                    initialFieldOfView = fieldOfView;
                }
                else if (!hasCameraAnimation)
                {
                    hasCameraAnimation =
                        (position - previousPosition).sqrMagnitude > POSITION_CHANGE_SQR_THRESHOLD ||
                        Quaternion.Angle(previousRotation, rotation) > ROTATION_CHANGE_THRESHOLD ||
                        Mathf.Abs(fieldOfView - previousFieldOfView) > FOV_CHANGE_THRESHOLD;
                }

                previousPosition = position;
                previousRotation = rotation;
                previousFieldOfView = fieldOfView;

                // 位置3成分、回転4成分、FOV1成分を書き出します。
                channels[0].Add(new Keyframe(time, position.x));
                channels[1].Add(new Keyframe(time, position.y));
                channels[2].Add(new Keyframe(time, position.z));
                channels[3].Add(new Keyframe(time, rotation.x));
                channels[4].Add(new Keyframe(time, rotation.y));
                channels[5].Add(new Keyframe(time, rotation.z));
                channels[6].Add(new Keyframe(time, rotation.w));
                channels[7].Add(new Keyframe(time, fieldOfView));
            }

            AnimationClip resultClip = new AnimationClip
            {
                frameRate = sampleFrameRate,
                wrapMode = WrapMode.Once
            };

            // ローカルTransformとCinemachineCameraのレンズにカーブを割り当てます。
            EditorCurveBinding[] bindings = new EditorCurveBinding[CHANNEL_COUNT];
            AnimationCurve[] curves = new AnimationCurve[CHANNEL_COUNT];

            for (int i = 0; i < TRANSFORM_PROPERTY_NAMES.Length; i++)
            {
                bindings[i] = EditorCurveBinding.FloatCurve(
                    string.Empty, typeof(Transform), TRANSFORM_PROPERTY_NAMES[i]);
                curves[i] = CreateLinearCurve(channels[i]);
            }

            bindings[7] = EditorCurveBinding.FloatCurve(
                string.Empty, typeof(CinemachineCamera), FOV_PROPERTY_NAME);
            curves[7] = CreateLinearCurve(channels[7]);

            AnimationUtility.SetEditorCurves(resultClip, bindings, curves);
            resultClip.EnsureQuaternionContinuity();

            return new BakeResult(resultClip, initialPosition, initialRotation,
                initialFieldOfView, hasCameraAnimation);
        }
        finally
        {
            if (previewRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(previewRoot);
            }
        }
    }

    /// <summary>
    /// サンプル間を線形補間するカーブを作成します。
    /// </summary>
    private static AnimationCurve CreateLinearCurve(List<Keyframe> samples)
    {
        Keyframe[] keys = samples.ToArray();

        for (int i = 0; i < keys.Length; i++)
        {
            float previousSlope = i > 0
                ? (keys[i].value - keys[i - 1].value) /
                  (keys[i].time - keys[i - 1].time)
                : 0f;
            float nextSlope = i < keys.Length - 1
                ? (keys[i + 1].value - keys[i].value) /
                  (keys[i + 1].time - keys[i].time)
                : 0f;

            if (i == 0)
            {
                previousSlope = nextSlope;
            }
            if (i == keys.Length - 1)
            {
                nextSlope = previousSlope;
            }

            keys[i] = new Keyframe(keys[i].time, keys[i].value,
                previousSlope, nextSlope);
        }

        return new AnimationCurve(keys);
    }
}
