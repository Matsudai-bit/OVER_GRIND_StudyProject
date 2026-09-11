using System;
using Unity.Behavior;

/// <summary>
/// ステートの実行状態を表します。
/// </summary>
[BlackboardEnum, Serializable]
public enum StateExecutionStatus
{
    RUNNING,
    SUCCEEDED,
    FAILED,
}