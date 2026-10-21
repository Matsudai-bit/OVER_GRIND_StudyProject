using UnityEngine;

public class SceneNavigator : MonoBehaviour
{
    // 遷移時に使用するエフェクト
    SceneTransitionEffect m_sceneTransitionInEffect;
    // 遷移時に使用するエフェクト
    SceneTransitionEffect m_sceneTransitionOutEffect;

    public void ChangeTransitionInEffect(SceneTransitionEffect sceneTransitionInEffect)
    {
        m_sceneTransitionInEffect = sceneTransitionInEffect;
    }

    public void ChangeTransitionOutEffect(SceneTransitionEffect sceneTransitionOutEffect)
    {
        m_sceneTransitionOutEffect = sceneTransitionOutEffect;
    }

    public void ChangeScene(string sceneName)
    {
        // TransitionEffectのnullチェックを行う
        if (m_sceneTransitionOutEffect == null)
        {
            Debug.LogWarning("TransitionEffectが設定されていません。");
            return;
        }

        // シーン遷移を行う
        SceneTransitionManager.GetInstance.TransitionToScene(
            sceneName,
            m_sceneTransitionInEffect,
            m_sceneTransitionOutEffect
        );
    }
}
