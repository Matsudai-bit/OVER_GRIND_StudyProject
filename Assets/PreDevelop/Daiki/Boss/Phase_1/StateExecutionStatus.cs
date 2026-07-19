using Unity.Behavior;

/// <summary>
/// ステートの実行状態を表します。
/// </summary>
[BlackboardEnum]
public enum StateExecutionStatus
{
    RUNNING,
    SUCCEEDED,
    FAILED,
}