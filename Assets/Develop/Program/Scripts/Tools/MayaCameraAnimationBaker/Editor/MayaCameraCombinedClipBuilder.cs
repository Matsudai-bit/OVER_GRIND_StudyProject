using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FBXの元アニメーションを保持し、CameraのFOVだけCinemachineに置換します。
/// </summary>
internal static class MayaCameraCombinedClipBuilder
{
    /// <summary>
    /// Camera以外の全カーブを保持した統合AnimationClipを作ります。
    /// </summary>
    internal static AnimationClip Create(
        AnimationClip sourceClip,
        IReadOnlyList<string> cameraPaths,
        IReadOnlyDictionary<string, MayaCameraAnimationBaker.BakeResult> baked)
    {
        if (sourceClip == null || cameraPaths == null || baked == null)
        {
            throw new ArgumentNullException(nameof(sourceClip));
        }

        AnimationClip result = new AnimationClip
        {
            frameRate = sourceClip.frameRate > 0f ? sourceClip.frameRate : 30f,
            wrapMode = sourceClip.wrapMode,
            legacy = sourceClip.legacy
        };
        try
        {
            // Transform・SkinnedMeshRenderer等の元カーブとキー時刻／接線を保持します。
            // Cameraコンポーネントはシーンから削除するため、そのカーブだけ除外します。
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
            {
                if (binding.type == typeof(Camera))
                {
                    continue;
                }
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                if (curve != null)
                {
                    AnimationUtility.SetEditorCurve(result, binding, curve);
                }
            }

            // オブジェクト参照カーブ（Sprite、Material等）も同じパスで複製します。
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
            {
                if (binding.type == typeof(Camera))
                {
                    continue;
                }
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                if (keys != null)
                {
                    AnimationUtility.SetObjectReferenceCurve(result, binding, keys);
                }
            }

            AnimationUtility.SetAnimationEvents(result, AnimationUtility.GetAnimationEvents(sourceClip));

            // 元Cameraコンポーネントに存在したキー時刻だけをFOVへ書き込みます。
            foreach (string cameraPath in cameraPaths)
            {
                if (!baked.TryGetValue(cameraPath, out MayaCameraAnimationBaker.BakeResult bake))
                {
                    throw new InvalidOperationException("Cameraのベイク結果がありません: " + cameraPath);
                }

                var cameraTimes = new SortedSet<float>();
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
                {
                    if (binding.type != typeof(Camera) || binding.path != cameraPath)
                    {
                        continue;
                    }
                    AnimationCurve originalCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                    if (originalCurve == null)
                    {
                        continue;
                    }
                    foreach (Keyframe key in originalCurve.keys)
                    {
                        cameraTimes.Add(key.time);
                    }
                }

                // FOVに元キーがない場合は、静的なレンズ値を時刻0の1キーで保持します。
                if (cameraTimes.Count == 0)
                {
                    cameraTimes.Add(0f);
                }

                AnimationCurve bakedFov = AnimationUtility.GetEditorCurve(
                    bake.Clip, EditorCurveBinding.FloatCurve(
                        string.Empty, typeof(CinemachineCamera),
                        MayaCameraAnimationBaker.FOV_PROPERTY_NAME));
                if (bakedFov == null)
                {
                    throw new InvalidOperationException("FOVを取得できません: " + cameraPath);
                }

                var keysToWrite = new List<Keyframe>(cameraTimes.Count);
                foreach (float time in cameraTimes)
                {
                    // ベイク側は元キー時刻をすべて含むため、この評価は元サンプル値になります。
                    float fov = time == 0f ? bake.InitialFieldOfView : bakedFov.Evaluate(time);
                    keysToWrite.Add(new Keyframe(time, fov));
                }

                EditorCurveBinding lensBinding = EditorCurveBinding.FloatCurve(
                    cameraPath, typeof(CinemachineCamera),
                    MayaCameraAnimationBaker.FOV_PROPERTY_NAME);
                AnimationUtility.SetEditorCurve(result, lensBinding,
                    MayaCameraAnimationBaker.CreateLinearCurve(keysToWrite));
            }

            return result;
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(result);
            throw;
        }
    }
}
