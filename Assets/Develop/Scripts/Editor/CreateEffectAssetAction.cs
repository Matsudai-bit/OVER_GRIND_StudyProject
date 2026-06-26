using UnityEditor; // UnityEditor の機能を使用するための using
using UnityEditor.ProjectWindowCallback; // ProjectWindowUtil のコールバック用
using UnityEngine; // UnityEngine の基本機能

/// <summary>
/// エフェクト用の EffectData アセットを作成するクラス
/// </summary>
public class CreateEffectAssetAction : EndNameEditAction // 名前確定時に呼ばれるクラスを継承
{
    /// <summary>
    /// 名前確定時に呼ばれ、EffectData アセットを生成する処理
    /// </summary>
    /// <param name="instanceId">対象インスタンスID</param>
    /// <param name="pathName">作成されるアセットのパス</param>
    /// <param name="resourceFile">未使用（テンプレート用）</param>
    public override void Action(int instanceId, string pathName, string resourceFile) // アセット作成処理
    {
        var newEffect = ScriptableObject.CreateInstance<EffectData>(); // 新しい EffectData インスタンスを生成

        AssetDatabase.CreateAsset(newEffect, pathName); // アセットとして保存
        AssetDatabase.SaveAssets(); // アセットデータベースを保存

        EditorUtility.FocusProjectWindow(); // Project ウィンドウにフォーカス
        Selection.activeObject = newEffect; // 作成したアセットを選択状態にする

        AddToEffectManager(newEffect); // EffectManager に追加
    }

    /// <summary>
    /// 作成したエフェクトを EffectManager の配列に追加する
    /// </summary>
    /// <param name="effect">追加する EffectData</param>
    private void AddToEffectManager(ScriptableObject effect) // EffectManager に追加する処理
    {
        var viewer = Object.FindFirstObjectByType<EffectViewer>(); // シーン内の EffectViewer を検索

        if (viewer == null || viewer.GetEffectManager() == null) // Viewer または Manager が無い場合
        {
            return; // 何もせず終了
        }

        var manager = new SerializedObject(viewer.GetEffectManager()); // EffectManager を SerializedObject 化
        var effectsProp = manager.FindProperty("m_effects"); // m_effects 配列を取得

        effectsProp.arraySize++; // 配列サイズを1つ増やす
        effectsProp.GetArrayElementAtIndex(effectsProp.arraySize - 1).objectReferenceValue = effect; // 最後に追加

        manager.ApplyModifiedProperties(); // 変更を適用
    }
}
