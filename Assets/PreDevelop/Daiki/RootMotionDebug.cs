using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class RootAnimationDebug : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        AnimatorClipInfo[] clipInfos =
            animator.GetCurrentAnimatorClipInfo(0);

        AnimationClip clip =
            clipInfos.Length > 0 ? clipInfos[0].clip : null;

        // OnAnimatorMoveを実装すると組み込み適用が自動では行われないため、
        // デバッグ中も通常のRoot Motionを適用する。
        animator.ApplyBuiltinRootMotion();

        // Consoleの大量出力を防ぐ
        if (Time.frameCount % 10 != 0)
        {
            return;
        }

        string clipInfo = clip == null
            ? "None"
            : $"{clip.name}, " +
              $"humanMotion={clip.humanMotion}, " +
              $"hasMotionCurves={clip.hasMotionCurves}, " +
              $"hasRootCurves={clip.hasRootCurves}, " +
              $"hasGenericRootTransform={clip.hasGenericRootTransform}";

        Debug.Log(
            $"Clip=[{clipInfo}] " +
            $"Animator.hasRootMotion={animator.hasRootMotion} " +
            $"deltaPosition={animator.deltaPosition.ToString("F6")} " +
            $"deltaRotation={animator.deltaRotation.eulerAngles.ToString("F4")} " +
            $"objectPosition={transform.position.ToString("F4")}",
            this);
    }
}