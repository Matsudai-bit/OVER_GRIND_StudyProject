using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif
/// <summary>フレームごとの移動量(Delta)を毎秒の速度へ変換します。Cancel Delta Time OFF と組み合わせて使います。</summary>
public class DeltaPerSecondProcessor : InputProcessor<Vector2>
{
    public override Vector2 Process(Vector2 value, InputControl control)
    {
        float dt = Time.deltaTime;
        return dt > 0.0f ? value / dt : Vector2.zero;
    }

#if UNITY_EDITOR
    static DeltaPerSecondProcessor() => Register();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() => InputSystem.RegisterProcessor<DeltaPerSecondProcessor>();
}