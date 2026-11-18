using System;
using UnityEngine;

/// <summary>
/// 砂山のハイトマップデータと、削り・崩落シミュレーションを管理するクラス。
/// </summary>
public class SandHeightField
{
    /// <summary>
    /// 初期形状の生成方式。
    /// </summary>
    public enum SandShapeType
    {
        /// <summary>安息角に沿った円錐形。</summary>
        CONE,
        /// <summary>ハイトマップテクスチャから生成。</summary>
        HEIGHTMAP_TEXTURE,
        /// <summary>カスタム数式から生成。</summary>
        CUSTOM_FORMULA
    }

    // ---- 定数 ----
    private const float SETTLE_RATE = 0.12f;                  // 1ステップあたりの砂の移動割合
    private const float DIAGONAL_FACTOR = 1.41421356f;        // 斜め方向への距離倍率（√2）
    private const float MIN_EXCESS_HEIGHT = 1e-3f;            // 崩れ判定を行う最小の超過高さ
    private const float MIN_CARVE_DELTA = 1e-5f;              // 削り判定を行う最小の高さ変化量
    private const float MIN_SMOOTH_K = 1e-5f;                 // スムース補間を有効にする最小値
    private const float SLIDE_REPORT_THRESHOLD = 0.004f;      // 崩れイベントを通知する減少高さの閾値
    private const int MAX_SLIDE_REPORTS_PER_STEP = 4;         // 1ステップあたりの崩れ通知の最大回数
    private const float SLIDE_REPORT_PROBABILITY = 0.2f;      // 崩れ通知をランダムに間引く確率
    private const float CONE_SMOOTH_OFFSET = 0.0025f;         // 円錐先端を丸めるためのオフセット
    private const float CONE_PROFILE_SCALE = 0.95f;           // 円錐の最大高さ正規化係数（1 - 0.05）
    private const float CUSTOM_HALF_WIDTH = 1.0f;             // カスタム形状の幅半径
    private const float CUSTOM_HALF_LENGTH = 3.0f;            // カスタム形状の奥行き半径

    // 8近傍探索用のXオフセット
    private static readonly int[] NEIGHBOR_X = { 1, -1, 0, 0, 1, 1, -1, -1 };
    // 8近傍探索用のZオフセット
    private static readonly int[] NEIGHBOR_Z = { 0, 0, 1, -1, 1, -1, 1, -1 };

    // ---- メンバー変数 ----
    private readonly int m_gridSize;      // XZ方向のサンプル点数
    private readonly float m_voxelSize;   // サンプル点の間隔（m）
    private readonly float m_mountainHeight; // 砂山の最大高さ（m）
    private readonly float m_sink;        // Zファイト防止のために高さ0面を沈める量
    private readonly float[,] m_height;   // 各グリッドの砂の高さ（0以上）
    private readonly float[,] m_delta;    // 崩し処理用の作業バッファ

    /// <summary>XZ方向のサンプル点数。</summary>
    public int GridSize => m_gridSize;

    /// <summary>サンプル点の間隔（ローカル単位）。</summary>
    public float VoxelSize => m_voxelSize;

    /// <summary>床とのZファイト防止用の沈め量。</summary>
    public float Sink => m_sink;

    /// <summary>
    /// SandHeightField を初期化します。
    /// </summary>
    /// <param name="gridSize">XZ方向のサンプル点数。</param>
    /// <param name="voxelSize">サンプル点の間隔。</param>
    /// <param name="mountainHeight">砂山の最大高さ。</param>
    /// <param name="sink">床とのZファイト防止用の沈め量。</param>
    public SandHeightField(int gridSize, float voxelSize, float mountainHeight, float sink)
    {
        m_gridSize = gridSize;
        m_voxelSize = voxelSize;
        m_mountainHeight = mountainHeight;
        m_sink = sink;

        m_height = new float[m_gridSize, m_gridSize];
        m_delta = new float[m_gridSize, m_gridSize];
    }

    /// <summary>
    /// 指定されたグリッド座標の砂の高さを取得します。
    /// </summary>
    /// <param name="x">Xインデックス。</param>
    /// <param name="z">Zインデックス。</param>
    /// <returns>砂の高さ。</returns>
    public float GetHeight(int x, int z)
    {
        return m_height[x, z];
    }

    /// <summary>
    /// 範囲外をクランプして指定座標の砂の高さを取得します。
    /// </summary>
    /// <param name="x">Xインデックス。</param>
    /// <param name="z">Zインデックス。</param>
    /// <returns>クランプ後の砂の高さ。</returns>
    public float GetClampedHeight(int x, int z)
    {
        int maxIndex = m_gridSize - 1;
        return m_height[Mathf.Clamp(x, 0, maxIndex), Mathf.Clamp(z, 0, maxIndex)];
    }

    /// <summary>
    /// グリッド座標をローカルXZ座標（Yは0）へ変換します。
    /// </summary>
    /// <param name="x">Xインデックス。</param>
    /// <param name="z">Zインデックス。</param>
    /// <returns>ローカル空間のXZ座標。</returns>
    public Vector3 ConvertGridToLocalXZ(int x, int z)
    {
        float offset = (m_gridSize - 1) * m_voxelSize * 0.5f;
        return new Vector3(x * m_voxelSize - offset, 0f, z * m_voxelSize - offset);
    }

    /// <summary>
    /// 指定された形状タイプで砂山の初期データを生成します。
    /// </summary>
    /// <param name="shapeType">初期形状の生成方式。</param>
    /// <param name="heightmapTexture">ハイトマップ画像（HEIGHTMAP_TEXTURE モード時に使用）。</param>
    /// <param name="reposeAngle">安息角（度）。</param>
    public void InitializeShape(SandShapeType shapeType, Texture2D heightmapTexture, float reposeAngle)
    {
        switch (shapeType)
        {
            case SandShapeType.HEIGHTMAP_TEXTURE:
                InitFromTexture(heightmapTexture, reposeAngle);
                break;
            case SandShapeType.CUSTOM_FORMULA:
                InitFromCustomFormula();
                break;
            case SandShapeType.CONE:
            default:
                InitDefaultCone(reposeAngle);
                break;
        }

        ClearBorder();
    }

    /// <summary>
    /// ローカル座標のXZ位置における表面の高さと法線を取得します。
    /// </summary>
    /// <param name="localPos">問い合わせるローカル座標。</param>
    /// <param name="surfaceY">算出された表面のローカルY座標。</param>
    /// <param name="localNormal">算出された表面のローカル法線。</param>
    /// <returns>
    /// true：グリッド範囲内でサンプリングに成功しました。
    /// false：グリッド範囲外です。
    /// </returns>
    public bool TrySampleSurface(Vector3 localPos, out float surfaceY, out Vector3 localNormal)
    {
        surfaceY = 0f;
        localNormal = Vector3.up;

        int n = m_gridSize;
        float offset = (n - 1) * m_voxelSize * 0.5f;
        float gx = (localPos.x + offset) / m_voxelSize;
        float gz = (localPos.z + offset) / m_voxelSize;

        // 範囲外チェック
        if (gx < 0f || gz < 0f || gx > n - 1 || gz > n - 1)
        {
            return false;
        }

        int x0 = Mathf.Min((int)gx, n - 2);
        int z0 = Mathf.Min((int)gz, n - 2);
        float fx = gx - x0;
        float fz = gz - z0;

        float h00 = m_height[x0, z0];
        float h10 = m_height[x0 + 1, z0];
        float h01 = m_height[x0, z0 + 1];
        float h11 = m_height[x0 + 1, z0 + 1];

        // バイリニア補間と勾配計算
        float h = Mathf.Lerp(Mathf.Lerp(h00, h10, fx), Mathf.Lerp(h01, h11, fx), fz);
        float dhdx = Mathf.Lerp(h10 - h00, h11 - h01, fz) / m_voxelSize;
        float dhdz = Mathf.Lerp(h01 - h00, h11 - h10, fx) / m_voxelSize;

        surfaceY = h - m_sink;
        localNormal = new Vector3(-dhdx, 1f, -dhdz).normalized;
        return true;
    }

    /// <summary>
    /// ローカル空間で球の下側の面を使って砂を削ります。
    /// </summary>
    /// <param name="localCenter">削り球の中心（ローカル座標）。</param>
    /// <param name="localRadius">削り球の半径（ローカル単位）。</param>
    /// <param name="smoothK">縁のなめらかさ係数。</param>
    /// <param name="removedHeightSum">取り除かれた高さの合計（ローカル単位）。</param>
    /// <returns>
    /// true：1箇所以上の砂が削られました。
    /// false：砂の高さは変化しませんでした。
    /// </returns>
    public bool Carve(Vector3 localCenter, float localRadius, float smoothK, out float removedHeightSum)
    {
        removedHeightSum = 0f;

        int n = m_gridSize;
        float offset = (n - 1) * m_voxelSize * 0.5f;
        int cx = Mathf.RoundToInt((localCenter.x + offset) / m_voxelSize);
        int cz = Mathf.RoundToInt((localCenter.z + offset) / m_voxelSize);
        int r = Mathf.CeilToInt(localRadius / m_voxelSize) + 1;
        float radiusSq = localRadius * localRadius;
        bool modified = false;

        int minX = Mathf.Max(1, cx - r);
        int maxX = Mathf.Min(n - 2, cx + r);
        int minZ = Mathf.Max(1, cz - r);
        int maxZ = Mathf.Min(n - 2, cz + r);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                float dx = (x * m_voxelSize - offset) - localCenter.x;
                float dz = (z * m_voxelSize - offset) - localCenter.z;
                float distSq = dx * dx + dz * dz;
                if (distSq >= radiusSq)
                {
                    continue;
                }

                // 列ごとの球の下側面を算出し、現在の高さと比較して削る
                float bottom = localCenter.y - Mathf.Sqrt(radiusSq - distSq);
                float surface = m_height[x, z] - m_sink;
                float nextSurface = (smoothK > MIN_SMOOTH_K)
                    ? CalculateSmoothMin(surface, bottom, smoothK)
                    : Mathf.Min(surface, bottom);
                float newHeight = Mathf.Max(0f, nextSurface + m_sink);

                if (newHeight < m_height[x, z] - MIN_CARVE_DELTA)
                {
                    removedHeightSum += m_height[x, z] - newHeight;
                    m_height[x, z] = newHeight;
                    modified = true;
                }
            }
        }

        return modified;
    }

    /// <summary>
    /// 安息角に基づいて砂の崩れを1ステップ進行させます。
    /// </summary>
    /// <param name="reposeAngle">安息角（度）。</param>
    /// <param name="onSandSlidLocal">砂が滑り落ちた際の通知コールバック（ローカル位置、ローカル方向、減少高さ）。</param>
    /// <returns>
    /// true：砂が動きました。
    /// false：すべての斜面が安定しています。
    /// </returns>
    public bool SimulateSettleStep(float reposeAngle, Action<Vector3, Vector3, float> onSandSlidLocal)
    {
        int n = m_gridSize;
        float tanAngle = Mathf.Tan(reposeAngle * Mathf.Deg2Rad);
        bool moved = false;

        Array.Clear(m_delta, 0, m_delta.Length);

        // 8近傍との高さの差から移動量を計算
        for (int x = 1; x < n - 1; x++)
        {
            for (int z = 1; z < n - 1; z++)
            {
                float currentHeight = m_height[x, z];
                if (currentHeight <= 0f)
                {
                    continue;
                }

                for (int i = 0; i < 8; i++)
                {
                    int nx = x + NEIGHBOR_X[i];
                    int nz = z + NEIGHBOR_Z[i];
                    float dist = m_voxelSize * (i < 4 ? 1f : DIAGONAL_FACTOR);
                    float excess = (currentHeight - m_height[nx, nz]) - tanAngle * dist;

                    if (excess > MIN_EXCESS_HEIGHT)
                    {
                        float transfer = excess * SETTLE_RATE;
                        m_delta[x, z] -= transfer;
                        m_delta[nx, nz] += transfer;
                        moved = true;
                    }
                }
            }
        }

        if (!moved)
        {
            return false;
        }

        int reportedCount = 0;

        // 移動量を高さへ適用し、大きく減った箇所を通知
        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                float d = m_delta[x, z];

                if (onSandSlidLocal != null
                    && d < -SLIDE_REPORT_THRESHOLD
                    && reportedCount < MAX_SLIDE_REPORTS_PER_STEP
                    && x > 0 && z > 0 && x < n - 1 && z < n - 1
                    && UnityEngine.Random.value < SLIDE_REPORT_PROBABILITY)
                {
                    reportedCount++;
                    Vector3 downhillLocal = new Vector3(
                        -(GetClampedHeight(x + 1, z) - GetClampedHeight(x - 1, z)),
                        0f,
                        -(GetClampedHeight(x, z + 1) - GetClampedHeight(x, z - 1))
                    );
                    Vector3 baseLocal = ConvertGridToLocalXZ(x, z);
                    Vector3 posLocal = new Vector3(baseLocal.x, m_height[x, z] - m_sink, baseLocal.z);
                    onSandSlidLocal.Invoke(posLocal, downhillLocal, -d);
                }

                m_height[x, z] = Mathf.Max(0f, m_height[x, z] + d);
            }
        }

        ClearBorder();
        return true;
    }

    /// <summary>
    /// 安息角に沿ったデフォルトの円錐形を生成します。
    /// </summary>
    private void InitDefaultCone(float reposeAngle)
    {
        int n = m_gridSize;
        float center = (n - 1) / 2.0f;
        float tanAngle = Mathf.Tan(reposeAngle * Mathf.Deg2Rad);
        float maxRadius = (center - 2.0f) * m_voxelSize;
        float baseRadius = Mathf.Min(m_mountainHeight / tanAngle, maxRadius);
        float peak = baseRadius * tanAngle + m_sink;

        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                float dx = (x - center) * m_voxelSize;
                float dz = (z - center) * m_voxelSize;
                float t = Mathf.Sqrt(dx * dx + dz * dz) / baseRadius;
                float tSmooth = Mathf.Sqrt(t * t + CONE_SMOOTH_OFFSET);
                float profile = Mathf.Max(0f, (1f - tSmooth) / CONE_PROFILE_SCALE);
                m_height[x, z] = peak * profile;
            }
        }
    }

    /// <summary>
    /// テクスチャ（ハイトマップ）から初期形状を生成します。
    /// </summary>
    private void InitFromTexture(Texture2D heightmapTexture, float reposeAngle)
    {
        if (heightmapTexture == null)
        {
            Debug.LogWarning("[MarchingCubesMountain] 初期ハイトマップテクスチャが設定されていません。円錐で代用します。");
            InitDefaultCone(reposeAngle);
            return;
        }

        int n = m_gridSize;
        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                float u = (float)x / (n - 1);
                float v = (float)z / (n - 1);
                float pixelVal = heightmapTexture.GetPixelBilinear(u, v).grayscale;
                m_height[x, z] = pixelVal * m_mountainHeight;
            }
        }
    }

    /// <summary>
    /// 数式ロジックから初期形状を生成します。
    /// </summary>
    private void InitFromCustomFormula()
    {
        int n = m_gridSize;
        float center = (n - 1) / 2.0f;

        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                float dx = (x - center) * m_voxelSize;
                float dz = (z - center) * m_voxelSize;
                float shape = Mathf.Max(0f, 1f - Mathf.Abs(dx) / CUSTOM_HALF_WIDTH)
                            * Mathf.Max(0f, 1f - Mathf.Abs(dz) / CUSTOM_HALF_LENGTH);
                m_height[x, z] = shape * m_mountainHeight;
            }
        }
    }

    /// <summary>
    /// グリッド外周の砂の高さを0に設定します（外周から落ちる扱い）。
    /// </summary>
    private void ClearBorder()
    {
        int n = m_gridSize;
        for (int i = 0; i < n; i++)
        {
            m_height[0, i] = 0f;
            m_height[n - 1, i] = 0f;
            m_height[i, 0] = 0f;
            m_height[i, n - 1] = 0f;
        }
    }

    /// <summary>
    /// 2つの値をなめらかに結合するスムースミニマムを計算します。
    /// </summary>
    private static float CalculateSmoothMin(float a, float b, float k)
    {
        float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
        return Mathf.Min(a, b) - h * h * k * 0.25f;
    }
}