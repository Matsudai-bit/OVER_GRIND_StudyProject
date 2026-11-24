using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// HelixSplineGenerator の Inspector 表示と自動再生成・Undo 操作を拡張するエディタクラスです。
/// </summary>
[CustomEditor(typeof(HelixSplineGenerator))]
public class HelixSplineGeneratorEditor : Editor
{
    // Undo 履歴に表示する操作名の定数
    private const string UNDO_GENERATE_LABEL = "Generate Helix Spline";
    private const string GENERATE_BUTTON_LABEL = "Generate Helix Spline";

    /// <summary>
    /// Inspector の GUI を描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        HelixSplineGenerator generator = target as HelixSplineGenerator;
        if (generator == null)
        {
            return;
        }

        // パラメータの変更を監視してデフォルトの Inspector を描画する
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool hasChanged = EditorGUI.EndChangeCheck();

        // 値が変更され、かつ自動更新が有効な場合は即座に螺旋を再生成する
        if (hasChanged && generator.GetAutoUpdate())
        {
            ExecuteGenerateWithUndo(generator);
        }

        EditorGUILayout.Space();

        // 手動で螺旋を再生成するためのボタンを配置する
        if (GUILayout.Button(GENERATE_BUTTON_LABEL))
        {
            ExecuteGenerateWithUndo(generator);
        }
    }

    /// <summary>
    /// Undo 対応付きで螺旋スプラインの生成を実行し、シーンビューを更新します。
    /// </summary>
    /// <param name="generator">対象の HelixSplineGenerator。</param>
    private void ExecuteGenerateWithUndo(HelixSplineGenerator generator)
    {
        SplineContainer splineContainer = generator.GetTargetSplineContainer();
        if (splineContainer == null)
        {
            return;
        }

        // Undo 履歴の記録と変更フラグの更新を行う
        Undo.RecordObject(splineContainer, UNDO_GENERATE_LABEL);
        generator.GenerateHelixSpline();
        EditorUtility.SetDirty(splineContainer);
        SceneView.RepaintAll();
    }
}