//using System.Collections.Generic;
//using UnityEngine;

//// ==========================================================
//// 1. SoundType (音の種類の定義)
//// ==========================================================
///// <summary>
///// 音の種類を識別するためのラベルです。
///// 後で「BGMだけ音量を下げる」「SEだけミュートする」といった処理をするために使います。
///// </summary>
//public enum SoundType
//{
//    BGM,
//    SE,
//    VOICE
//}

//// ==========================================================
//// 2. SoundData (1つ1つの音のデータ)
//// ==========================================================
///// <summary>
///// 1つの音声ファイルとその設定（名前や音量）をセットにしてまとめるクラスです。
///// </summary>
//[System.Serializable] // UnityのInspectorウィンドウ（画面上）で編集できるようにする属性
//public class SoundData
//{
//    [Tooltip("プログラムからこの音を呼び出すときの名前（例: 'Click', 'Explosion'）")]
//    public string id;

//    [Tooltip("実際の音声ファイルデータ（.wavや.mp3）")]
//    public AudioClip clip;

//    [Tooltip("この音の基本となる音量（0.0 ～ 1.0）。元から音が大きすぎる素材をここで調整します")]
//    [Range(0f, 1f)]
//    public float baseVolume = 1f;

//    [Tooltip("この音がBGMかSEかVOICEか")]
//    public SoundType type;
//}

//// ==========================================================
//// 3. SoundDatabase (音のカタログ)
//// ==========================================================
///// <summary>
///// 全てのSoundDataをリストとして保持し、カタログのように管理するクラスです。
///// ScriptableObjectを継承することで、Unityのプロジェクト内に「データファイル」として保存できます。
///// </summary>
//[CreateAssetMenu(fileName = "NewSoundDatabase", menuName = "Audio/SoundDatabase")]
//public class SoundDatabaseStudy : ScriptableObject
//{
//    [Tooltip("ゲーム内で使用する全ての音声データのリスト")]
//    public List<SoundData> soundList = new List<SoundData>();

//    /// <summary>
//    /// 指定されたID（名前）に一致する音声データをリストから探し出して返す関数です。
//    /// AudioManagerが「"Click"の音を鳴らしたい！」と言ってきたときに使われます。
//    /// </summary>
//    /// <param name="id">探したい音の名前（ID）</param>
//    /// <returns>見つかった音声データ。見つからなければ null を返します</returns>
//    public SoundData GetSoundData(string id)
//    {
//        // リストの中身を1つずつ確認するループ処理
//        foreach (SoundData data in soundList)
//        {
//            // もしリストの中のIDと、探しているIDが完全に一致したら
//            if (data.id == id)
//            {
//                return data; // 見つかったデータを返す（ここで処理が終了する）
//            }
//        }

//        // 最後まで探しても見つからなかった場合の処理
//        Debug.LogWarning($"[SoundDatabase] 指定されたIDの音声データが見つかりません: {id}");
//        return null; // 何もないことを表す null を返す
//    }
//}