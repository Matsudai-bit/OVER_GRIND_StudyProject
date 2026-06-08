using JetBrains.Annotations;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.ScrollRect;

public class target : MonoBehaviour
{

    public enum MoveType { Static , Horizontal,Rotating}

    [Header("状態管理")]

    public　bool targetHit = false; //衝突した際に裏返すか？｛true裏返す｝｛false裏返っていない｝

    [Header("--- 挙動の設定 ---")]

    public MoveType currentmovement;  //挙動
    public float  moveSpeed = 1f;     //移動速度
    public float  moveDistance = 0f;  //移動幅
    public float rotateSpeed = 60.0f; //回転速度
    private Vector3 startPosition;    //初期位置
    private Quaternion startRotation; //初期回転


    public int hitCount = 0;//ゲームUIに渡すため

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPosition = transform.position;//開始位置
        startRotation = transform.rotation;//開始回転

        //初期に動きの挙動を決定させる
        Randommovement();
    }

    // Update is called once per frame
    void Update()
    {
        ////当たったら更新を止める
        //if (m_targetHit) return;

        //挙動を管理する
        HandleMovement();
    } 

    //ランダムな挙度を決定する
    void Randommovement()
    {
        //Typeに格納されている長さから１つランダムに数値を抜き取る
        int randindex = Random.Range(0,System.Enum.GetValues(typeof(MoveType)).Length);
        currentmovement = (MoveType)randindex;
    }

    //各種挙動に行かせる処理
    void HandleMovement()
    {
        switch(currentmovement)
        {
            //何もしない
            case MoveType.Static:　
                 break;

            //上下移動
            case MoveType.Horizontal:
                float x = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
                transform.position = startPosition + new Vector3(x, 0,0);
               　break;

            // その場でクルクル回転
            case MoveType.Rotating:
                
                transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
                break;
        }
    }

    //当たったかどうか？
    public void  OnHitTarget()
    {
        ////２重を防ぐ
        //if (m_targetHit) return;
       
        //当たった
        targetHit = true;

        //当たっら加算される
        hitCount++;

        //回る処理
        StartCoroutine(FlipAnimation());
    }

    //回転させるアニメーション
    IEnumerator FlipAnimation()
    {
        Quaternion targetRotation = startRotation * Quaternion.Euler(90.0f, 0f, 0f);

        float elapsed = 0f;

        float duration = 0.3f; // 0.3秒かけて倒れる
        Quaternion currentRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            //回転をさせるためかける
            transform.rotation = Quaternion.Slerp(currentRot, targetRotation, elapsed / duration);
            yield return null;
        }

        transform.rotation = targetRotation; // 完全に目標角度に固定

        //消滅させるならコメントを消す
        //Destroy(gameObject, 2f);
    }
}
