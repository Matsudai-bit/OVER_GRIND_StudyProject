using System;
using UnityEngine;

/// <summary>
/// シーン遷移エフェクト基底クラス
/// </summary>
public abstract class SceneTransitionEffect : MonoBehaviour, ISceneTransitionEffect
{
     abstract public bool IsTransitioning();

    abstract public void StartTransition(Action onComplete);


}
