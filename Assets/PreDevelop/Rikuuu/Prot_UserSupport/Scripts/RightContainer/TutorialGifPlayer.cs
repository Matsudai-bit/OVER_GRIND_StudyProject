using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UniGifを使用してGifファイルをデコードし、UI Imageに再生表示するクラス
/// InspectorのUnityEventから引数なしでPlay()/Stop()を呼び出すことを想定している
/// </summary>
public class TutorialGifPlayer : MonoBehaviour
{
    [Header("Gifファイル（拡張子を.bytesにリネームしてドラッグ）")]
    [SerializeField]
    private TextAsset m_gifFile;

    [Header("表示先")]
    [SerializeField]
    private UnityEngine.UI.Image m_targetImage;

    [Header("再生設定")]
    // ループ再生するかどうか
    [SerializeField]
    private bool m_isLoop = true;
    // 停止時に最初のフレームへ戻すかどうか
    [SerializeField]
    private bool m_resetToFirstFrameOnStop = false;

    // デコード済みのスプライト一覧とフレームごとの表示時間
    private readonly List<Sprite> m_frameSprites = new List<Sprite>();
    private readonly List<float> m_frameDelays = new List<float>();

    // デコードが完了しているかどうか
    private bool m_isDecoded = false;
    // デコード完了前にPlay()が呼ばれた場合、完了後に自動再生するための予約フラグ
    private bool m_playRequested = false;

    // 現在再生中のコルーチン
    private Coroutine m_playCoroutine;
    // 現在デコード中のコルーチン
    private Coroutine m_decodeCoroutine;

    private void OnEnable()
    {
        // 既にデコード済み、またはデコード中でなければ開始する
        if (!m_isDecoded && m_decodeCoroutine == null && m_gifFile != null)
        {
            m_decodeCoroutine = StartCoroutine(DecodeGif());
        }
    }

    private void OnDisable()
    {
        // 非アクティブ化されるタイミングでコルーチンをきちんと止め、状態をリセットする
        if (m_decodeCoroutine != null)
        {
            StopCoroutine(m_decodeCoroutine);
            m_decodeCoroutine = null;
        }

        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
            m_playCoroutine = null;
        }
    }

    /// <summary>
    /// Gifファイルをデコードし、フレームごとのSpriteをキャッシュする
    /// </summary>
    private IEnumerator DecodeGif()
    {
        yield return StartCoroutine(UniGif.GetTextureListCoroutine(
            m_gifFile.bytes,
            (gifTexList, loopCount, width, height) =>
            {
                if (gifTexList == null)
                {
                    Debug.LogError($"{name}: Gifのデコードに失敗しました", this);
                    return;
                }

                foreach (var gifTex in gifTexList)
                {
                    Sprite sprite = Sprite.Create(
                        gifTex.m_texture2d,
                        new Rect(0, 0, gifTex.m_texture2d.width, gifTex.m_texture2d.height),
                        new Vector2(0.5f, 0.5f));

                    m_frameSprites.Add(sprite);
                    m_frameDelays.Add(gifTex.m_delaySec);
                }

                m_isDecoded = true;
                Debug.Log($"{name}: デコード完了 frames={m_frameSprites.Count}", this); // ★追加

                if (m_playRequested)
                {
                    m_playRequested = false;
                    m_playCoroutine = StartCoroutine(PlayRoutine());
                }

                if (m_targetImage != null && m_frameSprites.Count > 0)
                {
                    m_targetImage.sprite = m_frameSprites[0];
                }
            }));
    }

    // ----------------------------------------------------------------------
    // InspectorのUnityEventから呼び出す用（引数なし）
    // ----------------------------------------------------------------------

    /// <summary>
    /// 再生を開始する。デコード未完了の場合は完了後に自動で再生する
    /// </summary>
    public void Play()
    {
        Debug.Log($"{name}: Play呼び出し isDecoded={m_isDecoded} coroutineRunning={m_playCoroutine != null}", this); // ★追加

        if (m_playCoroutine != null)
        {
            return;
        }

        if (!m_isDecoded)
        {
            m_playRequested = true;
            return;
        }

        m_playCoroutine = StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// 再生を停止する
    /// </summary>
    public void Stop()
    {
        Debug.Log("Stop");

        // 再生予約中だった場合はキャンセルする
        m_playRequested = false;

        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
            m_playCoroutine = null;
        }

        if (m_resetToFirstFrameOnStop && m_targetImage != null && m_frameSprites.Count > 0)
        {
            m_targetImage.sprite = m_frameSprites[0];
        }
    }

    // ----------------------------------------------------------------------

    /// <summary>
    /// フレームを一定間隔で切り替えて再生するコルーチン
    /// </summary>
    private IEnumerator PlayRoutine()
    {
        if (m_frameSprites.Count == 0 || m_targetImage == null)
        {
            m_playCoroutine = null;
            yield break;
        }

        do
        {
            for (int i = 0; i < m_frameSprites.Count; i++)
            {
                m_targetImage.sprite = m_frameSprites[i];
                yield return new WaitForSeconds(m_frameDelays[i]);
            }
        }
        while (m_isLoop);

        m_playCoroutine = null;
    }
}