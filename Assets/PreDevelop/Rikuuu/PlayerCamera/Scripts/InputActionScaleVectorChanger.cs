using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionScaleVectorChanger : MonoBehaviour
{
    // Scale Vector 2 の倍率の下限(0以下を防ぐ)
    private const float MIN_SCALE = 0.0001f;

    // カメラ操作
    [SerializeField]
    private InputActionReference m_lookAction;

    // 設定値
    [SerializeField]
    private Vector2 m_scaleVector;

    /// <summary>
    /// 開始時に現在の設定値を適用します。
    /// </summary>
    private void Start() => ApplyScale();

    /// <summary>
    /// Inspectorで値が変更されたときに再適用します(エディタのみ)。
    /// </summary>
    private void OnValidate()
    {
        // 0以下にならないよう補正する
        m_scaleVector = new Vector2(
            Mathf.Max(m_scaleVector.x, MIN_SCALE),
            Mathf.Max(m_scaleVector.y, MIN_SCALE));

        // 再生中以外は何もしない
        if (!Application.isPlaying)
        {
            return;
        }

        ApplyScale();
    }

    /// <summary>
    /// マウスのバインディングへ、Scale Vector 2の倍率を適用します。
    /// </summary>
    private void ApplyScale()
    {
        if (m_lookAction == null)
            return;

        InputAction action = m_lookAction.action;
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        string processors =
            $"ScaleVector2(x={m_scaleVector.x.ToString(culture)},y={m_scaleVector.y.ToString(culture)})";

        for (int i = 0; i < action.bindings.Count; i++)
        {
            // マウスのバインディングだけを対象にする
            string path = action.bindings[i].path;
            if (!path.Contains("Mouse") && !path.Contains("Pointer"))
                continue;

            action.ApplyBindingOverride(i, new InputBinding
            {
                overrideProcessors = processors
            });
        }
    }
}
