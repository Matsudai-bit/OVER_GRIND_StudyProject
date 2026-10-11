using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

using SplineMeshSpline = SplineMesh.Spline;
using SplineMeshTiling = SplineMesh.SplineMeshTiling;

/// <summary>
/// SplineContainerを持つGameObjectへレール用Componentを設定します。
/// </summary>
public static class RailSplineSetupMenu
{
    private const string MENU_PATH =
        "GameObject/Rail/Apply Rail Components";

    private const string RAIL_LAYER_NAME =
        "Rail";

    /// <summary>
    /// 選択中のGameObjectへレール用Componentを追加します。
    /// </summary>
    /// <param name="menuCommand">メニュー実行情報。</param>
    [MenuItem(MENU_PATH, false, 10)]
    private static void ApplyRailComponents(
        MenuCommand menuCommand)
    {
        var target =
            menuCommand.context as GameObject;

        // 上部GameObjectメニューから実行された場合に対応
        if (target == null)
            target = Selection.activeGameObject;

        if (target == null)
            return;

        if (!target.TryGetComponent<SplineContainer>(
                out _))
        {
            Debug.LogWarning(
                $"{nameof(RailSplineSetupMenu)}: " +
                $"{target.name} に {nameof(SplineContainer)} がありません。",
                target);

            return;
        }

        ApplyComponents(target);
    }

    /// <summary>
    /// メニューを実行可能か判定します。
    /// </summary>
    /// <param name="menuCommand">メニュー実行情報。</param>
    /// <returns>
    /// true：実行可能です。
    /// false：実行できません。
    /// </returns>
    [MenuItem(MENU_PATH, true)]
    private static bool ValidateApplyRailComponents(
        MenuCommand menuCommand)
    {
        var target =
            menuCommand.context as GameObject;

        if (target == null)
            target = Selection.activeGameObject;

        if (target == null)
            return false;

        return target.TryGetComponent<SplineContainer>(
            out _);
    }

    /// <summary>
    /// レール用Componentを設定します。
    /// </summary>
    /// <param name="target">設定対象。</param>
    private static void ApplyComponents(
        GameObject target)
    {
        var wasActive =
            target.activeSelf;

        // SplineMeshTilingはOnEnable時に
        // SplineやMeshへアクセスするため一時的に無効化する
        if (wasActive)
        {
            Undo.RecordObject(
                target,
                "Apply Rail Components");

            target.SetActive(false);
        }

        try
        {
            AddComponentIfMissing<SplineRailInfo>(
                target);

            AddComponentIfMissing<SplineMeshSpline>(
                target);

            var splineMeshTiling =
                AddComponentIfMissing<SplineMeshTiling>(
                    target);

            // Meshが未設定の状態で処理させない
            if (splineMeshTiling.mesh == null)
                splineMeshTiling.enabled = false;

            AddComponentIfMissing<SplineMeshSynchronizer>(
                target);

            SetRailLayer(target);
        }
        finally
        {
            if (wasActive)
                target.SetActive(true);
        }

        EditorUtility.SetDirty(target);

        Selection.activeGameObject =
            target;
    }

    /// <summary>
    /// Componentが存在しない場合のみ追加します。
    /// </summary>
    /// <typeparam name="T">追加するComponent型。</typeparam>
    /// <param name="target">追加対象。</param>
    /// <returns>対象Component。</returns>
    private static T AddComponentIfMissing<T>(
        GameObject target)
        where T : Component
    {
        if (target.TryGetComponent<T>(
                out var component))
        {
            return component;
        }

        return Undo.AddComponent<T>(
            target);
    }

    /// <summary>
    /// Railレイヤーを設定します。
    /// </summary>
    /// <param name="target">設定対象。</param>
    private static void SetRailLayer(
        GameObject target)
    {
        var railLayer =
            LayerMask.NameToLayer(
                RAIL_LAYER_NAME);

        if (railLayer < 0)
        {
            Debug.LogWarning(
                $"{nameof(RailSplineSetupMenu)}: " +
                $"Layer '{RAIL_LAYER_NAME}' が存在しません。",
                target);

            return;
        }

        target.layer =
            railLayer;
    }
}