using UnityEngine;

public class SceneNavigator : MonoBehaviour
{
    // 遷移時に使用するエフェクト
    SceneTransitionEffect m_sceneTransitionEffect;

    public void ChangeTransitionEffect(SceneTransitionEffect sceneTransitionEffect)
    {
        m_sceneTransitionEffect = sceneTransitionEffect;
    }

    public void ChangeScene(string sceneName)
    {
        // TransitionEffectのnullチェックを行う
        if (m_sceneTransitionEffect == null)
        {
            Debug.LogWarning("TransitionEffectが設定されていません。");
            return;
        }

        // シーン遷移を行う
        SceneTransitionManager.GetInstance.TransitionToScene(
            sceneName,
            m_sceneTransitionEffect,
            m_sceneTransitionEffect.IsTransitioning()
        );
    }
}
