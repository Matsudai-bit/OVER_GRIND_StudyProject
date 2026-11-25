/// <summary>
/// サウンドを 2D / 3D のどちらで再生するかの指定です。
/// </summary>
public enum SoundSpatialMode
{
    // SoundDatabase の設定に従う
    USE_DATABASE,

    // 常に 2D で再生する
    FORCE_2D,

    // 常に 3D で再生する
    FORCE_3D
}