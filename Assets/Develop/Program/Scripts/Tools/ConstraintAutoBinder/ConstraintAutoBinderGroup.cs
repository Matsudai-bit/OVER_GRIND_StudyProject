using UnityEngine;

/// <summary>
/// 子階層に存在するConstraintAutoBinderを一括操作します。
/// </summary>
[DisallowMultipleComponent]
public sealed class ConstraintAutoBinderGroup : MonoBehaviour
{
    /// <summary>
    /// 子階層に存在するすべてのConstraintAutoBinderで
    /// 現在のバインド情報を保存します。
    /// </summary>
    public void CaptureAllBindings()
    {
        ConstraintAutoBinder[] binders =
            GetBinders();

        if (binders.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(ConstraintAutoBinderGroup)}: " +
                "子階層にConstraintAutoBinderが存在しません。",
                this);

            return;
        }

        foreach (ConstraintAutoBinder binder in binders)
        {
            binder.CaptureBindings();
        }

        Debug.Log(
            $"{nameof(ConstraintAutoBinderGroup)}: " +
            $"{binders.Length}個のConstraintAutoBinderで" +
            "バインド情報を保存しました。",
            this);
    }

    /// <summary>
    /// 子階層に存在するすべてのConstraintAutoBinderで
    /// Constraintの再バインドを実行します。
    /// </summary>
    public void BindAllConstraints()
    {
        ConstraintAutoBinder[] binders =
            GetBinders();

        if (binders.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(ConstraintAutoBinderGroup)}: " +
                "子階層にConstraintAutoBinderが存在しません。",
                this);

            return;
        }

        foreach (ConstraintAutoBinder binder in binders)
        {
            binder.BindAll();
        }

        Debug.Log(
            $"{nameof(ConstraintAutoBinderGroup)}: " +
            $"{binders.Length}個のConstraintAutoBinderで" +
            "再バインドを実行しました。",
            this);
    }

    /// <summary>
    /// 子階層に存在するConstraintAutoBinderを取得します。
    /// </summary>
    public ConstraintAutoBinder[] GetBinders()
    {
        return GetComponentsInChildren<ConstraintAutoBinder>(true);
    }
}