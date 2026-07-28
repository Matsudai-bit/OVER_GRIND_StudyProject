/// <summary>
/// ステートの実行状態を提供します。
/// </summary>
public interface IStateStatusProvider
{
    /// <summary>
    /// 現在のステート実行状態を取得します。
    /// </summary>
    /// <returns>現在のステート実行状態。</returns>
    StateExecutionStatus GetStateExecutionStatus();
}