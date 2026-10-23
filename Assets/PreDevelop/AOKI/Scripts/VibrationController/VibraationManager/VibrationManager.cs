using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


/// コントローラーの振動を流す、揺らす
public class VibrationManager : MonoBehaviour
{
    /// <summary>
    /// 振動の受け渡しをインスタンス化させる
    /// </summary>
    public static VibrationManager Instance { get; private set; }


    /// <summary>
    /// もし振動を出す動きが２つ起きた時に重複しないようにする
    /// </summary>
    private Coroutine activeVibrationCoroutine;

    /// インスタンス化させる時Nullの時入れるもしNullじゃなければオブジェクトを消去する
    private void Awake()
    {
        if (Instance == null)
        {
            //オブジェクトを入れる
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            //オブジェクトを消す
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 指定された振動データに基づいてゲームパッドを振動させる
    /// </summary>
    public void Play(VibrationData data)
    {
        if (data == null) return;

        // 新しい振動がリクエストされたら現在の振動を打ち消して新しく開始
        if (activeVibrationCoroutine != null)
        {
            //古いオブジェクトが入ってきた場合削除する
            StopCoroutine(activeVibrationCoroutine);
        }

        //　新しいデータを挿入し開始させる
        activeVibrationCoroutine = StartCoroutine(PlayVibrationRoutine(data));
    }

    /// <summary>
    /// 振動を即座に停止します
    /// </summary>
    public void StopVibration()
    {
        //　コルーチンの強制終了と初期化
        if (activeVibrationCoroutine != null)
        {
            StopCoroutine(activeVibrationCoroutine);
            activeVibrationCoroutine = null;
        }

        //　物理モーターの完全振動リセット
        ResetGamepadMotors();
    }

    /// <summary>
    /// 時間経過で変化する振動を毎フレーム計算しながらゲームパッドを震わせ、指定時間が過ぎたら自動的に止める実質的なメイン処理
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private IEnumerator PlayVibrationRoutine(VibrationData data)
    {
        //　ゲームパッドの接続チェック
        var gamepad = Gamepad.current;
        if (gamepad == null) yield break;


        float elapsedTime = 0f;

        while (elapsedTime < data.m_duration)
        {
            elapsedTime += Time.deltaTime;

            //　進行度の計算と減衰カーブの適用
            float progress = Mathf.Clamp01(elapsedTime / data.m_duration);

            // アニメーションカーブから現在の減衰倍率（0.0 ~ 1.0）を取得
            float dampingMultiplier = data.dampingCurve.Evaluate(progress);

            //　モーター強度の計算と出力
            float currentLow = data.m_lowFrequency * dampingMultiplier;
            float currentHigh = data.m_highFrequency * dampingMultiplier;

            gamepad.SetMotorSpeeds(currentLow, currentHigh);

            yield return null; // 1フレーム待機
        }

        //　自動停止と参照解除を行い処理を止める
        ResetGamepadMotors();
        activeVibrationCoroutine = null;
    }

    /// <summary>
    /// 接続されているゲームパッドの物理的な振動をピタッと止める用
    /// </summary>
    private void ResetGamepadMotors()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    //　設定された GameObject が非アクティブ
    private void OnDisable()
    {
        ResetGamepadMotors();
    }


    //プレイヤーがゲームを終了した時
    private void OnApplicationQuit()
    {
        ResetGamepadMotors();
    }
 
}