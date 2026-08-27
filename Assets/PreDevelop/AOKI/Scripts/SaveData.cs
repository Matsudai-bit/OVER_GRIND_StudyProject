/// @ using :: 使用エンジン
using UnityEngine;

///　@　className ::　セーブデータぅラス
///　@  name :: Aoki Hayate
///　@  date :: 2026/08/27

/// @ data :: ファイル名とメニュー名を初期設定
[CreateAssetMenu(fileName = "SaveData", menuName = "ScriptableObjects/SaveData")]


/// className :: セーブデータクラス
public class SaveData : ScriptableObject
{
    /// <summary>
    /// Header :: インスペクター表示
    /// Tooltip :: 変数の意図を表示する
    /// Range :: バグらないように初期値を挿入
    /// </summary>
    [Header("セーブファイル設定")]
    [Tooltip("保存するファイル名（例: savedata.json）")]

    //　初期名を指定
    public string saveFileName = "savedata.json";

    [Header("ゲーム進行")]
    //　クリアステージを指定
    public int clearedStage = 0;

    [Header("オーディオ設定")]


    /// <summary>
    /// BGM の音量の指定
    /// SE　の音量の設定
    /// MASTERの音量の設定
    /// <summary>
    [Range(0f, 1f)] public float bgmVolume = 1.0f;
    [Range(0f, 1f)] public float seVolume = 1.0f;
    [Range(0f, 1f)] public float masterVolume = 1.0f;

    /// <summary>
    /// スクリーンの大きさの設定
    /// カメラの感度の設定
    /// </summary>
    [Header("システム設定")]
    public int screenSize = 1;
    public float cameraSensitivity = 1.0f;
}