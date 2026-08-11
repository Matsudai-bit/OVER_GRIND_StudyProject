using System;
using Unity.Behavior;
using UnityEngine;
/// <summary>
/// 対象のステート実行状態を比較するConditionノードです。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CompareStateStatus", story: "[TargetObject] state status is [ExpectedStatus]", category: "Conditions", id: "8aa3b92293aa71758913b9352dba3d42")]
public partial class CompareStateStatusCondition : Condition
{

    /// <summary>
    /// Statusを確認する対象のGameObject。
    /// </summary>
    [SerializeReference]
    public BlackboardVariable<GameObject> TargetObject;

    /// <summary>
    /// 比較するステート実行状態。
    /// </summary>
    [SerializeReference]
    public BlackboardVariable<StateExecutionStatus> ExpectedStatus;

    /// <summary>
    /// 対象のStatusが指定Statusと一致するか確認します。
    /// </summary>
    /// <returns>
    /// true：指定されたStatusと一致します。
    /// false：指定されたStatusと一致しません。
    /// </returns>
    public override bool IsTrue()
    {
        // Blackboard変数を確認します。
        if (TargetObject == null ||
            TargetObject.Value == null)
        {
            return false;
        }

        if (ExpectedStatus == null)
        {
            return false;
        }

        // Status提供コンポーネントを取得します。
        if (!TryGetStateStatusProvider(
                TargetObject.Value,
                out IStateStatusProvider statusProvider))
        {
            return false;
        }

        // 現在Statusと指定Statusを比較します。
        StateExecutionStatus currentStatus =
            statusProvider.GetStateExecutionStatus();

        return currentStatus == ExpectedStatus.Value;
    }

    /// <summary>
    /// Statusを提供するコンポーネントを取得します。
    /// </summary>
    /// <param name="targetObject">検索対象のGameObject。</param>
    /// <param name="statusProvider">取得したStatus提供オブジェクト。</param>
    /// <returns>
    /// true：Status提供オブジェクトを取得しました。
    /// false：Status提供オブジェクトを取得できませんでした。
    /// </returns>
    private bool TryGetStateStatusProvider(
        GameObject targetObject,
        out IStateStatusProvider statusProvider)
    {
        statusProvider = null;

        // InterfaceはTryGetComponentで直接取得できない場合があるため、
        // MonoBehaviourから実装コンポーネントを検索します。
        MonoBehaviour[] components =
            targetObject.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is not IStateStatusProvider provider)
            {
                continue;
            }

            statusProvider = provider;
            return true;
        }

        return false;
    }
}
