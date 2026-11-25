using UnityEngine;

/// <summary>
/// 平面反射専用カメラを識別し、反射描画するRendering Layerを渡します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlanarReflectionCameraMarker : MonoBehaviour
{
    // ReflectionRenderingLayerFeatureが参照する反射対象マスク
    private uint m_renderingLayerMask;

    /// <summary>反射描画するRendering Layerのビットマスクを取得します。</summary>
    public uint RenderingLayerMask => m_renderingLayerMask;

    /// <summary>反射描画するRendering Layerを設定します。</summary>
    public void SetRenderingLayers(RenderingLayerMask renderingLayers)
    {
        m_renderingLayerMask = renderingLayers;
    }
}
