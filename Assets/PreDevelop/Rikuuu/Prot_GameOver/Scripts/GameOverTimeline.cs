using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class GameOverTimeline : MonoBehaviour
{
    // アニメーションディレクター
    [SerializeField]
    private PlayableDirector m_director;

    // ゲームオーバーロゴ
    [SerializeField]
    private GameObject m_gameOverLogo;
    // 背景フレーム
    [SerializeField]
    private GameObject m_backGroundFlame;

    // イントロ後に表示されるオブジェクト
    [SerializeField]
    private GameObject m_afterIntroObject;

    // スクリプト無効にするためのCanvasGroup
    private CanvasGroup m_afterIntroGroup;

    private void Reset()
    {
        // エディタでアタッチした時だけ呼ばれる。director だけ取得する
        m_director = GetComponent<PlayableDirector>();
    }

    private void Awake()
    {
        // 実行時に取得する(非アクティブにする前に)
        m_afterIntroGroup = m_afterIntroObject.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        m_afterIntroGroup.interactable = false;
        m_afterIntroGroup.blocksRaycasts = false;

        // オブジェクトを非アクティブにする
        m_gameOverLogo.SetActive(false);
        m_backGroundFlame.SetActive(false);
        m_afterIntroObject.SetActive(false);

        // アニメーションディレクターの初期化
        m_director.playOnAwake = false;
        m_director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        m_director.extrapolationMode = DirectorWrapMode.Hold;

        Play();
    }


    private void Start()
    {
        
    }

    private void Update()
    {
    }

    public void Play()
    {
        // オブジェクトをアクティブにする
        m_gameOverLogo.SetActive(true);
        m_backGroundFlame.SetActive(true);
        m_afterIntroObject.SetActive(true);

        m_director.time = 0;
        m_director.Evaluate();
        m_director.Play();
    }

    public void OnIntroFinished()
    {
        m_afterIntroGroup.interactable = true;
        m_afterIntroGroup.blocksRaycasts = true;

        Debug.Log("ゲームオーバー演出が終了しました");
    }
}
