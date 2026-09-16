using UnityEngine;
using UnityEngine.InputSystem;

public class Effects_Player : MonoBehaviour
{
    [Header("再生したいエフェクトのIDを選択")]
    [SerializeField] private EffectID m_moveEffectID;
    [SerializeField] private EffectID m_jumpEffectID;
    [SerializeField] private EffectID m_actionEffectID;

    [Header("生成位置（空欄ならプレイヤーの位置）")]
    [SerializeField] private Transform m_footPoint;

    private void Update()
    {
        // キーボードが接続されていない場合は処理しない
        if (Keyboard.current == null) return;

        // [Space] キーで移動エフェクト
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlayMoveEffect();
        }

        // [Z] キーでジャンプエフェクト
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            PlayJumpEffect();
        }

        // [X] キーでアクションエフェクト
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            PlayActionEffect();
        }
    }

    public void PlayMoveEffect()
    {
        Vector3 pos = m_footPoint != null ? m_footPoint.position : transform.position;
        VFXManager.Instance.Play(m_moveEffectID, pos);
    }

    public void PlayJumpEffect()
    {
        Vector3 pos = m_footPoint != null ? m_footPoint.position : transform.position;
        VFXManager.Instance.Play(m_jumpEffectID, pos);
    }

    public void PlayActionEffect()
    {
        Vector3 pos = transform.position;
        VFXManager.Instance.Play(m_actionEffectID, pos);
    }
}