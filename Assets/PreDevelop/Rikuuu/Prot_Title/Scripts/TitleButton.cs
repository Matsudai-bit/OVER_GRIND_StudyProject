using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TitleButton : MonoBehaviour
{
    [Header("定数設定")]
    // スケールの変化量
    [SerializeField]
    float SCALE_SPEED = 2.0f;

    // 文字サイズ
    [SerializeField]
    float FONT_SIZE = 36;
    // 画像の最大スケール
    [SerializeField]
    float MAX_FONT_SCALE = 1.4f;
    // 画像の最小スケール
    [SerializeField]
    float MIN_FONT_SCALE = 1.0f;

    // 細字の大きさ
    [SerializeField]
    float MIN_FACE_DILATE = 0.0f;
    // 太字の大きさ
    [SerializeField]
    float MAX_FACE_DILATE = 0.15f;


    [Header("ラベル設定")]
    // ラベルテキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_label;

    // ラベルに表示する文字のフォント
    [SerializeField]
    private TMP_FontAsset m_labelFont;

    // ラベルに表示する内容
    [SerializeField]
    private string m_labelText = "null";

    // ラベルの文字色（通常）
    [SerializeField]
    private Color m_defaultLabelColor = new Color32(180, 175, 165, 255);
    // ラベルの文字色（カーソル時）
    [SerializeField]
    private Color m_onCursorLabelColor = new Color32(255, 255, 255, 255);


    [Header("説明文設定")]
    // 表示する文字列
    [SerializeField]
    private string m_tooltip = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_tipText;


    // 現在表示してる文字サイズ
    private float m_currentScale;
    // カーソルで選択されているかどうか
    private bool m_onCursor = false;

    // 文字専用のマテリアルインスタンス
    private Material m_labelMaterial;

    private void Awake()
    {
        // ラベルの設定
        m_label.font = m_labelFont;             // フォント
        m_label.text = m_labelText;             // テキスト
        m_label.color = m_defaultLabelColor;    // 文字色

        //// 文字専用のマテリアルインスタンスを取得
        //m_labelMaterial = m_label.fontMaterial;

        // 文字サイズの初期化
        m_currentScale = MIN_FONT_SCALE;
    }

    private void Update()
    {
        // カーソルで選択されている場合
        if(m_onCursor)
        {
            // 変更
            m_currentScale += SCALE_SPEED * Time.deltaTime;
        }

        // スケールを範囲内に収める
        Clamp();

        // サイズを適用する
        m_label.fontSize = m_currentScale * FONT_SIZE;

        //// 太さを適用する
        //ApplyFontWeight();
    }

    // 押されたときの処理 ---------------------------------------

    public void OnCursor()
    {
        //// 太字にする
        //m_label.fontStyle = FontStyles.Bold;

        // ラベルの設定
        m_label.font = m_labelFont;             // フォント
        m_label.color = m_onCursorLabelColor;   // 文字色
        // 状態の変更
        m_onCursor = true;

        // 文字の置き換え
        m_tipText.text = m_tooltip;
    }

    public void OnCursorExit()
    {
        //// 通常の太さに戻す
        //m_label.fontStyle = FontStyles.Normal;

        // ラベルの設定
        m_label.font = m_labelFont;             // フォント
        m_label.color = m_defaultLabelColor;    // 文字色
        // 状態の変更
        m_onCursor = false;
        
        // 画像サイズの変更
        m_currentScale = MIN_FONT_SCALE;
    }

    // ----------------------------------------------------------

    private void Clamp()
    {
        if(m_currentScale > MAX_FONT_SCALE)
        {
            m_currentScale = MAX_FONT_SCALE;
        }
        if(m_currentScale < MIN_FONT_SCALE)
        {
            m_currentScale = MIN_FONT_SCALE;
        }
    }

    private void ApplyFontWeight()
    {
        // スケールの進行度(0～1)を求める
        float progress = Mathf.InverseLerp(MIN_FONT_SCALE, MAX_FONT_SCALE, m_currentScale);
        // 進行度に応じてFace Dilateを補間
        float dilate = Mathf.Lerp(MIN_FACE_DILATE, MAX_FACE_DILATE, progress);
        // マテリアルに反映
        m_labelMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
    }
}
