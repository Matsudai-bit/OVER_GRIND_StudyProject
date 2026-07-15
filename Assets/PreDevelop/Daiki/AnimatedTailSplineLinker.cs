using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class AnimatedTailSplineLinker : MonoBehaviour
{
    [Header("尻尾のメッシュ表面に配置されているレール用ボーン")]
    [SerializeField] private Transform[] railKnotTransforms;

    private SplineContainer splineContainer;

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
    }

    void LateUpdate()
    {
        if (splineContainer == null || railKnotTransforms == null || railKnotTransforms.Length == 0) return;

        var spline = splineContainer.Spline;

        while (spline.Count < railKnotTransforms.Length)
        {
            spline.Add(
                new BezierKnot(
                    float3.zero,
                    float3.zero,
                    float3.zero,
                    quaternion.identity
                ),
                TangentMode.Linear
            );
        }

        while (spline.Count > railKnotTransforms.Length)
        {
            spline.RemoveAt(spline.Count - 1);
        }

        for (int i = 0; i < railKnotTransforms.Length; i++)
        {
            if (railKnotTransforms[i] == null) continue;

            Vector3 worldPos =
                railKnotTransforms[i].position;

            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            if (float.IsNaN(localPos.x) || float.IsNaN(localPos.y) || float.IsNaN(localPos.z) ||
                float.IsInfinity(localPos.x) || float.IsInfinity(localPos.y) || float.IsInfinity(localPos.z))
            {
                Debug.LogError($"Spline knot {i} の localPos が不正です: {localPos}", this);
                continue;
            }

            BezierKnot knot = spline[i];
            knot.Position = localPos;
            knot.TangentIn = float3.zero;
            knot.TangentOut = float3.zero;
            knot.Rotation = quaternion.identity;

            spline[i] = knot;

            spline.SetTangentMode(i, TangentMode.Linear);
        }
    }
}