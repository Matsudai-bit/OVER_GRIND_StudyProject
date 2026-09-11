/// @ using :: 使用エンジン
using UnityEngine;

/// @ className :: セーブデータクラス
/// @ name :: Aoki Hayate
/// @ date :: 2026/08/27

/// @ data :: ファイル名とメニュー名を初期設定
[CreateAssetMenu(fileName = "SaveData", menuName = "ScriptableObjects/SaveData")]
public class SaveData : ScriptableObject
{
    /// <summary>
    /// Header :: インスペクター表示
    /// Tooltip :: 変数の意図を表示する
    /// Range :: バグらないように初期値を挿入
    /// </summary>
    [Header("セーブファイル設定")]
    [Tooltip("保存するファイル名（例: savedata.json）")]
    // 初期名を指定
    public string m_saveFileName = "savedata.json";

    [Header("ゲーム進行")]
    // クリアステージを指定
    public int m_clearedStage = 0;

    [Header("オーディオ設定")]
    /// <summary>
    /// BGM の音量の指定
    /// SE の音量の設定
    /// MASTERの音量の設定
    /// </summary>
    [Range(0f, 1f)] public float m_bgmVolume = 1.0f;
    [Range(0f, 1f)] public float m_seVolume = 1.0f;
    [Range(0f, 1f)] public float m_masterVolume = 1.0f;

    [Header("システム設定")]
    /// <summary>
    /// スクリーンの大きさの設定
    /// カメラの感度の設定
    /// </summary>
    public int m_screenSize = 1;
    public float m_cameraSensitivity = 1.0f;
}