using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

/// <summary>
/// アニメーションイベントを処理するクラス
/// </summary>
public class AnimationEventHandler 
{
    private Animator m_animator; // アニメーションを制御するAnimatorコンポーネント

    private int     m_layerIndex;       // アニメーションレイヤーのインデックス

    private int m_currentAnimationHash; // 現在のアニメーションのハッシュ値

    private bool m_changedLayerWeight = false; // レイヤーのウェイトを変更するかどうか

    private TargetTimeData m_animationTargetTimeActionData; // アニメーションレイヤーの変更に関するデータ

    bool m_hasAnimationPlayed = false; // 置くアニメーションが再生されたことがあるかどうかのフラグ

    private string m_paramName; // アニメーションのパラメータ名

    public int LayerIndex => m_layerIndex;

    /// <summary>
    /// アニメーションレイヤーの変更に関するデータ構造
    /// </summary>
    public struct TargetTimeData
    {
        public float changedNormalizedTime; // レイヤーのウェイトを変更するアニメーションの再生割合
        public Action action;

    }

    public AnimationEventHandler(Animator animator)
    {
     
        m_animator = animator;

        ResetAnimation(); // アニメーションの初期化
    }




    /// <summary>
    /// アニメーションを再生するメソッド
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="layerIndex"></param>
    public void PlayAnimationTrigger( string paramName, string layerName, string animationName)
    {
        // アニメーションのパラメータ名を設定
        m_paramName = paramName; 

        // アニメーションを開始を指示
        m_animator.SetTrigger(paramName);

        // アニメーションの再生
        SetupAnimation(animationName, layerName);

    }

    /// <summary>
    /// アニメーションを再生するメソッド（ブール値を使用）
    /// </summary>
    /// <param name="boolName"></param>
    /// <param name="layerName"></param>
    /// <param name="animationName"></param>
    public void PlayAnimationBool(string paramName, string layerName, string animationName)
    {
        // アニメーションのパラメータ名を設定
        m_paramName = paramName;

        // アニメーションを開始
        m_animator.SetBool(m_paramName, true);

        // アニメーションの再生
        SetupAnimation(animationName, layerName);
    }

    public void ResetAnimation()
    {
        m_animationTargetTimeActionData = new TargetTimeData
        {
            changedNormalizedTime = 0.0f,
            action = null
        };

        m_layerIndex = 0;
        m_currentAnimationHash = 0;

        m_hasAnimationPlayed = false; // 置くアニメーションが再生されたことがあるかどうかのフラグを初期化

        m_changedLayerWeight = false; // レイヤーのウェイトを変更するかどうかのフラグを初期化

        m_paramName = string.Empty; // アニメーションのパラメータ名を初期化
    }

    /// <summary>
    /// 指定したアニメーションを停止するメソッド
    /// </summary>
    /// <param name="boolName"></param>
    public void StopAnimation()
    {
        // アニメーションを停止
        m_animator.SetBool(m_paramName, false);


       
      
    }

    private void SetupAnimation(string animationName, string layerName)
    {
        m_layerIndex = m_animator.GetLayerIndex(layerName);
        m_currentAnimationHash = Animator.StringToHash(animationName);
        m_hasAnimationPlayed = false; // アニメーションが再生されたことをリセット
    }

    /// <summary>
    /// 指定したアニメーションを再生するメソッド
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="layerName"></param>
    public void PlayAnimation(string animationName, string layerName)
    {
        SetupAnimation(animationName, layerName);
        m_animator.Play(animationName, m_layerIndex, 0.0f);

       
    }

    /// <summary>
    /// 現在のアニメーションが再生中かどうかを確認するメソッド
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying()
    {
        // fullPathHash → shortNameHash に統一
        int currentHash = m_animator.GetCurrentAnimatorStateInfo(m_layerIndex).shortNameHash;
        return (currentHash == m_currentAnimationHash
                && m_animator.GetCurrentAnimatorStateInfo(m_layerIndex).normalizedTime < 1.0f);
    }


    /// <summary>
    /// 指定した時間で実行するアクションを設定するメソッド。
    /// </summary>
    /// <param name="changedNormalizedTime"></param>
    /// <param name="action"></param>
    public void SetTargetTimeAction(float changedNormalizedTime, Action action)
    {
        // レイヤーのウェイトを変更するかどうかのフラグを立てる
        m_changedLayerWeight = true;
        // アニメーションの再生割合を取得
        m_animationTargetTimeActionData.changedNormalizedTime = changedNormalizedTime;
        m_animationTargetTimeActionData.action = action;
    }

    public void ResetTargetTimeAction()
    {
        m_changedLayerWeight = false;
        m_animationTargetTimeActionData.changedNormalizedTime = 0.0f;
        m_animationTargetTimeActionData.action = null;
    }

    /// <summary>
    /// 指定したレイヤーのウェイトを、一定時間かけて目標値まで変更するコルーチン。
    /// </summary>
    public IEnumerator TransitionLayerWeight(int layerIndex, float targetWeight, float duration)
    {
        float startWeight = m_animator.GetLayerWeight(layerIndex);
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float newWeight = Mathf.Lerp(startWeight, targetWeight, time / duration);
            m_animator.SetLayerWeight(layerIndex, newWeight);
            yield return null;
        }

        // 最後にウェイトを確実に目標値に設定
        m_animator.SetLayerWeight(layerIndex, targetWeight);
    }

    /// <summary>
    /// アニメーションイベントの更新処理。
    /// </summary>
    public void OnUpdate()
    {
        // 現在のアニメーションの状態を取得
        AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(m_layerIndex);        

        // アニメーションのハッシュを取得
        int currentHash = stateInfo.shortNameHash;
        if (currentHash == m_currentAnimationHash)
        {
            m_hasAnimationPlayed = true; // 置くアニメーションが再生中であることを記録

        }

        // レイヤーのウェイトを変更する必要があるか確認
        if (m_changedLayerWeight && IsPlaying() && m_hasAnimationPlayed)
        {
                   // レイヤーのウエイトを変え始める再生割合を取得
            float normalizedTime = m_animationTargetTimeActionData.changedNormalizedTime;

            // アニメーションの再生割合が指定された割合に達しているか確認
            if (stateInfo.normalizedTime >= normalizedTime )
            {
                // アニメーションの長さを取得
                float animationTotalTime = stateInfo.length;
                // コルーチンを開始
                //StartCoroutine(TransitionLayerWeight(m_layerIndex, m_animationTargetTimeActionData.targetWeight, m_animationTargetTimeActionData.duration));
                // アクションを実行
                m_animationTargetTimeActionData.action?.Invoke(); 
            }
        }

    }

    /// <summary>
    /// アニメーションが再生されたことがあるかどうかを確認するメソッド
    /// </summary>
    /// <returns></returns>
    public bool HasAnimationPlayed()
    {
        return m_hasAnimationPlayed; // 置くアニメーションが再生されたことがあるかどうかを返す
    }

    /// <summary>
    /// 現在のアニメーションを先頭から再生し直すメソッド
    /// </summary>
    public void RestartAnimation()
    {
        m_animator.StopPlayback();
        m_hasAnimationPlayed = false;
    }
}
