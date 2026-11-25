using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 選択したGameObjectと子階層のRendererへRendering Layerを一括設定します。
/// </summary>
public static class RenderingLayerHierarchyEditor
{
    // 反射描画に使用するRendering Layer名
    private const string REFLECTION_RENDERING_LAYER_NAME = "PlanarReflection";

    // メニューとUndoに表示する名前
    private const string ADD_MENU_PATH = "GameObject/Rendering Layers/Add PlanarReflection To Children";
    private const string REMOVE_MENU_PATH = "GameObject/Rendering Layers/Remove PlanarReflection From Children";
    private const string ADD_UNDO_NAME = "Add PlanarReflection Rendering Layer";
    private const string REMOVE_UNDO_NAME = "Remove PlanarReflection Rendering Layer";

    /// <summary>
    /// 選択したオブジェクトとその子階層のRendererに反射用Rendering Layerを追加します。
    /// </summary>
    [MenuItem(ADD_MENU_PATH, false, 30)]
    public static void AddReflectionLayerToChildren()
    {
        ApplyReflectionLayer(true);
    }

    /// <summary>
    /// 選択したオブジェクトとその子階層のRendererから反射用Rendering Layerを削除します。
    /// </summary>
    [MenuItem(REMOVE_MENU_PATH, false, 31)]
    public static void RemoveReflectionLayerFromChildren()
    {
        ApplyReflectionLayer(false);
    }

    /// <summary>
    /// メニューを選択中のGameObjectがある場合にのみ有効化します。
    /// </summary>
    [MenuItem(ADD_MENU_PATH, true)]
    [MenuItem(REMOVE_MENU_PATH, true)]
    private static bool ValidateSelection()
    {
        return Selection.gameObjects.Length > 0;
    }

    /// <summary>
    /// 選択階層のRendererにRendering Layerを追加または削除します。
    /// </summary>
    /// <param name="addLayer">true：追加します。false：削除します。</param>
    private static void ApplyReflectionLayer(bool addLayer)
    {
        uint targetMask = RenderingLayerMask.GetMask(REFLECTION_RENDERING_LAYER_NAME);

        // プロジェクトに対象のRendering Layerが存在することを確認
        if (targetMask == 0)
        {
            Debug.LogWarning($"Rendering Layer '{REFLECTION_RENDERING_LAYER_NAME}' が見つかりません。" +
                             " Project Settings > Tags and Layers で作成してください。");
            return;
        }

        // 複数の親オブジェクトを選択しても同じRendererを重複処理しない
        var processedRenderers = new HashSet<Renderer>();
        int changedCount = 0;
        string undoName = addLayer ? ADD_UNDO_NAME : REMOVE_UNDO_NAME;

        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject == null || EditorUtility.IsPersistent(selectedObject))
            {
                continue;
            }

            // 非アクティブな子も含め、子階層のすべてのRendererを取得
            Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null || !processedRenderers.Add(targetRenderer))
                {
                    continue;
                }

                uint currentMask = targetRenderer.renderingLayerMask;
                uint nextMask = addLayer
                    ? currentMask | targetMask
                    : currentMask & ~targetMask;

                if (currentMask == nextMask)
                {
                    continue;
                }

                // UndoおよびPrefabインスタンスの変更記録に対応
                Undo.RecordObject(targetRenderer, undoName);
                targetRenderer.renderingLayerMask = nextMask;
                if (PrefabUtility.IsPartOfPrefabInstance(targetRenderer))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(targetRenderer);
                }

                EditorUtility.SetDirty(targetRenderer);
                changedCount++;
            }
        }

        Debug.Log($"{undoName}: {changedCount} 個のRendererを変更しました。");
    }
}
