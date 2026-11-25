using UnityEngine;
using UnityEngine.Animations;

/// <summary>エフェクト内のParentConstraintと追従先アンカーIDを対応付けます。</summary>
[RequireComponent(typeof(ParentConstraint))]
public sealed class EffectAttachmentSlot : MonoBehaviour
{
    [SerializeField, Header("追従先アンカーID")]
    private EffectAnchorId m_anchorId = EffectAnchorId.ATTACK_A;
    [SerializeField, Header("変更するParentConstraintのSource番号"), Min(0)]
    private int m_sourceIndex;

    private ParentConstraint m_constraint;
    private bool m_isBound;
    private bool m_addedSource;
    private bool m_originalActive;
    private float m_originalWeight;

    /// <summary>使用するアンカーID。</summary>
    public EffectAnchorId AnchorId => m_anchorId;
    /// <summary>操作するConstraint。</summary>
    public ParentConstraint Constraint => GetConstraint();
    /// <summary>Source番号。</summary>
    public int SourceIndex => m_sourceIndex;

    /// <summary>設定が有効であるか確認します。</summary>
    public bool CanBind()
    {
        ParentConstraint constraint = GetConstraint();
        return constraint != null && m_sourceIndex >= 0 && m_sourceIndex <= constraint.sourceCount;
    }

    /// <summary>Sourceを上書きし、追従を有効化します。</summary>
    /// <param name="target">追従先Transform。</param>
    /// <returns>true：成功。false：設定が不正です。</returns>
    public bool TryBind(Transform target)
    {
        ParentConstraint constraint = GetConstraint();
        if (target == null || !CanBind())
        {
            return false;
        }
        if (m_isBound)
        {
            ClearBinding();
        }

        m_originalActive = constraint.constraintActive;
        m_addedSource = (m_sourceIndex == constraint.sourceCount);
        ConstraintSource source;
        if (m_addedSource)
        {
            source = new ConstraintSource { sourceTransform = target, weight = 1f };
            constraint.AddSource(source);
            m_originalWeight = 1f;
        }
        else
        {
            source = constraint.GetSource(m_sourceIndex);
            m_originalWeight = source.weight;
            source.sourceTransform = target;
            constraint.SetSource(m_sourceIndex, source);
        }
        constraint.constraintActive = true;
        m_isBound = true;
        return true;
    }

    /// <summary>参照を外し、Source構成と有効状態を戻します。</summary>
    public void ClearBinding()
    {
        if (!m_isBound)
        {
            return;
        }
        ParentConstraint constraint = GetConstraint();
        if (constraint != null)
        {
            constraint.constraintActive = false;
            if (m_addedSource && m_sourceIndex < constraint.sourceCount)
            {
                constraint.RemoveSource(m_sourceIndex);
            }
            else if (m_sourceIndex < constraint.sourceCount)
            {
                ConstraintSource source = constraint.GetSource(m_sourceIndex);
                source.sourceTransform = null;
                source.weight = m_originalWeight;
                constraint.SetSource(m_sourceIndex, source);
            }
            constraint.constraintActive = m_originalActive;
        }
        m_isBound = false;
        m_addedSource = false;
    }

    /// <summary>ParentConstraintを取得します。</summary>
    private ParentConstraint GetConstraint()
    {
        if (m_constraint == null)
        {
            m_constraint = GetComponent<ParentConstraint>();
        }
        return m_constraint;
    }
}
