using System.Collections.Generic;
using System.Linq;
using UnityEngine;



/// <summary>
/// サウンド管理クラス シングルトンクラス
/// </summary>
public class SoundManager : MonoBehaviour
{
    static public readonly int ERROR_SOUND_ID = -1;    // エラー用サウンド識別ID

    private static SoundManager s_instance = null;

    public  GameSoundData m_gameSoundData ;

    private Dictionary<SoundID, SoundData> m_soundDictionary = new();

    private Dictionary<int, AudioSource> m_audioSourceDict = new(); // AudioSource管理用Dictionary

    private AudioSource m_bgmAudioSource;

    private int m_nextAudioSourceID = 0;

    /// <summary>
    /// 唯一のインスタンスにアクセスするためのプロパティ
    /// </summary>
    public static SoundManager GetInstance
    {
        get
        {
            // シーン上にインスタンスが存在しない場合
            if (s_instance == null)
            {
                // シーン内からSoundManagerを検索

                // それでも見つからない場合
                if (s_instance == null)
                {
                    // 新しいGameObjectを作成し、SoundManagerコンポーネントを追加する
                    GameObject singletonObject = new GameObject(typeof(SoundManager).Name);
                    s_instance = singletonObject.AddComponent<SoundManager>();
                    Debug.Log($"[SoundManager] シングルトンを生成しました: {singletonObject.name}");
                }
            }
            return s_instance;
        }
    }

    private void Awake()
    {
        // 既にインスタンスが存在する場合
        if (s_instance != null && s_instance != this)
        {
            // このインスタンスを破棄して、重複を避ける
            Destroy(this.gameObject);
            return;
        }

        // シーン上の唯一のインスタンスとして自身を登録
        s_instance = this;

        // シーンを跨いで存続させる
        DontDestroyOnLoad(this.gameObject);

        Initialize();

    }

    private void Start()
    {
        // 初期化
    }

    private void FixedUpdate()
    {
        // 音量調整
        foreach (var kvp in m_audioSourceDict)
        {
            var audioSource = kvp.Value;
            if (audioSource != null)
            {
                audioSource.volume = 0.5f ;
            }
        }
        // BGM音量調整
        if (m_bgmAudioSource != null)
        {
            m_bgmAudioSource.volume = 0.2f;
        }
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void Initialize()
    {
  
        // サウンドデータのインスタンスを取得
        m_gameSoundData = GameSoundData.GetInstance; 

        // サウンドデータを辞書に変換
        foreach (var soundData in m_gameSoundData.SoundData)
        {
            m_soundDictionary.Add(soundData.id, soundData);
        }
    }

    //未使用のAudioSourceの取得 全て使用中の場合は作成する
    private KeyValuePair<int, AudioSource> GetUnusedAudioPairSource()
    {

        foreach (var kvp in m_audioSourceDict)
        {
            if (kvp.Value.isPlaying == false)
            {
                return kvp; //未使用のAudioSourceを発見
            }
        }

        // 見つからなかった場合生成する
        var audioSource = gameObject.AddComponent<AudioSource>();
        m_audioSourceDict.Add(m_nextAudioSourceID, audioSource);

        // 次回用IDをインクリメント
        m_nextAudioSourceID++;

        // 新規生成したAudioSourceを返す
        return m_audioSourceDict.Last(); 
    }

    /// <summary>
    /// 指定されたAudioClipを未使用のAudioSourceで再生
    /// </summary>
    /// <param name="clip"></param>
    /// <returns> サウンド識別ID </returns>
    private int Play(AudioClip clip, bool isLoop )
    {
        var audioPairSource = GetUnusedAudioPairSource();

        var audioSource = audioPairSource.Value;

        // 念のためnullチェック
        if (audioSource == null) return ERROR_SOUND_ID;

        // AudioSourceにAudioClipを設定して
        audioSource.clip = clip;
        // ループ設定
        audioSource.loop = isLoop;
        // 再生
        audioSource.Play();
        audioSource.volume = 0.8f;

        return audioPairSource.Key; //サウンド識別IDを返す
    }

    public void StopBGM()
    {
        if (m_bgmAudioSource != null)
        {
            m_bgmAudioSource.Stop();
        }
    }

    //指定された別名で登録されたAudioClipを再生
    public bool PlayBGM(SoundID id)
    {
        if (m_soundDictionary.TryGetValue(id, out var soundData)) //管理用Dictionary から、別名で探索
        {
            // BGM用のAudioSourceが存在しない場合は生成
            if (m_bgmAudioSource == null)
            {
                m_bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }

            // BGM用のAudioSourceを設定して再生
            m_bgmAudioSource.clip = soundData.clip;
            m_bgmAudioSource.loop = true;
            m_bgmAudioSource.volume = 0.2f;
            m_bgmAudioSource.Play();
            //Play(soundData.clip); //見つかったら、再生
            return true;
        }
        else
        {
            Debug.LogWarning($"その別名は登録されていません:{name}");
            return false;
        }
    }

    /// <summary>
    /// 指定された別名で登録されたAudioClipを再生
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isLoop"></param>
    /// <returns></returns>
    public int RequestPlaying(SoundID id, bool isLoop = false)
    {
        if (m_soundDictionary.TryGetValue(id, out var soundData)) //管理用Dictionary から、別名で探索
        {
 
            return Play(soundData.clip, isLoop); //見つかったら、再生
            
        }
        else
        {
            Debug.LogWarning($"その別名は登録されていません:{name}");
            return ERROR_SOUND_ID;
        }
    }

    /// <summary>
    /// 指定されたサウンド識別IDのAudioSourceを停止
    /// </summary>
    /// <param name="soundKeyID"></param>
    /// <returns></returns>
    public bool RequestStopping(int soundKeyID)
    {
        if (m_audioSourceDict.TryGetValue(soundKeyID, out var audioSource))
        {
            audioSource.Stop();
            return true;
        }
        else
        {
            Debug.LogWarning($"そのサウンド識別IDは登録されていません:{soundKeyID}");
            return false;
        }
    }

    /// <summary>
    /// 指定されたサウンド識別IDのAudioSourceが再生中かどうか
    /// </summary>
    /// <param name="soundKeyID"></param>
    /// <returns></returns>
    public bool IsPlaying(int soundKeyID)
    {
        if (m_audioSourceDict.TryGetValue(soundKeyID, out var audioSource))
        {
            return audioSource.isPlaying;
        }
        return false;
    }

    /// <summary>
    /// 全てのAudioSourceを停止
    /// </summary>
    public void RequestAllStopping(bool bgm = false)
    {
        foreach (var kvp in m_audioSourceDict)
        {
            kvp.Value.Stop();
        }

        // BGMも停止する場合
        if (bgm && m_bgmAudioSource != null)
        {
            m_bgmAudioSource.Stop();
        }
    }

    public void RequestPausing(int id)
    {
        if (m_audioSourceDict.TryGetValue(id, out var audioSource))
        {
            audioSource.Pause();
        }
        else
        {
            Debug.LogWarning($"そのサウンド識別IDは登録されていません:{id}");
        }
    }

    public void SetSpeed(int id, float speed)
    {
        if (m_audioSourceDict.TryGetValue(id, out var audioSource))
        {
            audioSource.pitch = speed;
        }
        else
        {
            Debug.LogWarning($"そのサウンド識別IDは登録されていません:{id}");
        }
    }

    public AudioSource GetAudioSource(int id)
    {
        if (m_audioSourceDict.TryGetValue(id, out var audioSource))
        {
            return audioSource;
        }
        else
        {
            Debug.LogWarning($"そのサウンド識別IDは登録されていません:{id}");
            return null;
        }
    }
}
