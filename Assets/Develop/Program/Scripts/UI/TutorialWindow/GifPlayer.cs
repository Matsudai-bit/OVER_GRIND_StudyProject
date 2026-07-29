using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UniGifでデコード済みのフレーム列をRawImageにループ再生するだけのシンプルなプレイヤー
/// </summary>
[RequireComponent(typeof(RawImage))]
public class GifPlayer : MonoBehaviour
{
    private RawImage rawImage;
    private Coroutine playRoutine;

    /// <summary>
    /// アタッチされているRawImage
    /// </summary>
    public RawImage RawImage => rawImage;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    /// <summary>
    /// 指定したフレーム列をループ再生します。
    /// </summary>
    public void Play(List<UniGif.GifTexture> frames)
    {
        Stop();

        if (frames == null || frames.Count == 0)
            return;

        // アニメーション開始までの空白を防ぐため、最初のフレームを即座に表示
        rawImage.texture = frames[0].m_texture2d;

        playRoutine = StartCoroutine(PlayRoutine(frames));
    }

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    private IEnumerator PlayRoutine(List<UniGif.GifTexture> frames)
    {
        int index = 0;

        while (true)
        {
            UniGif.GifTexture frame = frames[index];

            rawImage.texture = frame.m_texture2d;

            yield return new WaitForSeconds(frame.m_delaySec);

            index = (index + 1) % frames.Count;
        }
    }
}