using System;
using UnityEngine;

/// <summary>
/// 砂山のハイトマップデータと、削り・崩落シミュレーションを管理するクラス。
/// 長方形グリッドおよびダーティ矩形追跡に対応します。
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
    private const float MIN_EXCESS_HEIGHT = 1e-3f;            // 崩れ判定を行う最小の超過高さ
    private const float MIN_CARVE_DELTA = 1e-5f;              // 削り判定を行う最小の高さ変化量
    private const float MIN_SMOOTH_K = 1e-5f;                 // スムース補間を有効にする最小値
    private const float MIN_CONE_RADIUS = 0.05f;              // 円錐生成時の最小底面半径
    private const float SLIDE_REPORT_THRESHOLD = 0.004f;      // 崩れイベントを通知する減少高さの閾値
    private const int MAX_SLIDE_REPORTS_PER_STEP = 4;         // 1ステップあたりの崩れ通知の最大回数
    private const float SLIDE_REPORT_PROBABILITY = 0.2f;      // 崩れ通知をランダムに間引く確率
    private const float CONE_SMOOTH_OFFSET = 0.0025f;         // 円錐先端を丸めるためのオフセット
    private const float CONE_PROFILE_SCALE = 0.95f;           // 円錐の最大高さ正規化係数（1 - 0.05）
    private const float CUSTOM_HALF_WIDTH = 1.0f;             // カスタム形状の幅半径
    private const float CUSTOM_HALF_LENGTH = 3.0f;            // カスタム形状の奥行き半径

    // 8近傍探索用のXオフセット（0〜1: X軸方向, 2〜3: Z軸方向, 4〜7: 斜め方向）
    private static readonly int[] NEIGHBOR_X = { 1, -1, 0, 0, 1, 1, -1, -1 };
    // 8近傍探索用のZオフセット
    private static readonly int[] NEIGHBOR_Z = { 0, 0, 1, -1, 1, -1, 1, -1 };

    // ---- メンバー変数 ----
    private readonly int m_gridSizeX;        // X方向のサンプル点数
    private readonly int m_gridSizeZ;        // Z方向のサンプル点数
    private readonly float m_voxelSizeX;     // X方向のサンプル点間隔（m）
    private readonly float m_voxelSizeZ;     // Z方向のサンプル点間隔（m）
    private readonly float m_mountainHeight; // 砂山の最大高さ（m）
    private readonly float m_sink;           // Zファイト防止のために高さ0面を沈める量
    private readonly float[,] m_height;      // 各グリッドの砂の高さ（0以上）
    private readonly float[,] m_delta;       // 崩し処理用の作業バッファ

    // 崩れ計算対象のアクティブ矩形範囲
    private bool m_hasActiveRect;
    private int m_activeMinX;
    private int m_activeMaxX;
    private int m_activeMinZ;
    private int m_activeMaxZ;

    /// <summary>X方向のサンプル点数。</summary>
    public int GridSizeX => m_gridSizeX;

    /// <summary>Z方向のサンプル点数。</summary>
    public int GridSizeZ => m_gridSizeZ;

    /// <summary>X方向のサンプル点間隔（ローカル単位）。</summary>
    public float VoxelSizeX => m_voxelSizeX;

    /// <summary>Z方向のサンプル点間隔（ローカル単位）。</summary>
    public float VoxelSizeZ => m_voxelSizeZ;

    /// <summary>床とのZファイト防止用の沈め量。</summary>
    public float Sink => m_sink;

    /// <summary>
    /// SandHeightField を初期化します。
    /// </summary>
    /// <param name="gridSizeX">X方向のサンプル点数。</param>
    /// <param name="gridSizeZ">Z方向のサンプル点数。</param>
    /// <param name="voxelSizeX">X方向のサンプル点間隔。</param>
    /// <param name="voxelSizeZ">Z方向のサンプル点間隔。</param>
    /// <param name="mountainHeight">砂山の最大高さ。</param>
    /// <param name="sink">床とのZファイト防止用の沈め量。</param>
    public SandHeightField(
        int gridSizeX,
        int gridSizeZ,
        float voxelSizeX,
        float voxelSizeZ,
        float mountainHeight,
        float sink)
    {
        m_gridSizeX = gridSizeX;
        m_gridSizeZ = gridSizeZ;
        m_voxelSizeX = voxelSizeX;
        m_voxelSizeZ = voxelSizeZ;
        m_mountainHeight = mountainHeight;
        m_sink = sink;

        m_height = new float[m_gridSizeX, m_gridSizeZ];
        m_delta = new float[m_gridSizeX, m_gridSizeZ];
    }

    /// <summary>
    /// 指定されたグリッド座標の砂の高さを取得します。
    /// </summary>
    public float GetHeight(int x, int z)
    {
        return m_height[x, z];
    }

    /// <summary>
    /// 範囲外をクランプして指定座標の砂の高さを取得します。
    /// </summary>
    public float GetClampedHeight(int x, int z)
    {
        return m_height[Mathf.Clamp(x, 0, m_gridSizeX - 1), Mathf.Clamp(z, 0, m_gridSizeZ - 1)];
    }

    /// <summary>
    /// グリッド座標をローカルXZ座標（Yは0）へ変換します。
    /// </summary>
    public Vector3 ConvertGridToLocalXZ(int x, int z)
    {
        float halfSizeX = (m_gridSizeX - 1) * m_voxelSizeX * 0.5f;
        float halfSizeZ = (m_gridSizeZ - 1) * m_voxelSizeZ * 0.5f;
        return new Vector3(x * m_voxelSizeX - halfSizeX, 0f, z * m_voxelSizeZ - halfSizeZ);
    }

    /// <summary>
    /// 指定された形状タイプで砂山の初期データを生成します。
    /// </summary>
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

        ClearAllBorder();
        m_hasActiveRect = false;
    }

    /// <summary>
    /// ローカル座標のXZ位置における表面のローカルY座標のみを高速に取得します。
    /// </summary>
    /// <returns>
    /// true：グリッド範囲内で取得できました。
    /// false：グリッド範囲外です。
    /// </returns>
    public bool TrySampleHeight(float localX, float localZ, out float surfaceY)
    {
        surfaceY = 0f;

        int nx = m_gridSizeX;
        int nz = m_gridSizeZ;
        float halfSizeX = (nx - 1) * m_voxelSizeX * 0.5f;
        float halfSizeZ = (nz - 1) * m_voxelSizeZ * 0.5f;

        float gx = (localX + halfSizeX) / m_voxelSizeX;
        float gz = (localZ + halfSizeZ) / m_voxelSizeZ;

        if (gx < 0f || gz < 0f || gx > nx - 1 || gz > nz - 1)
        {
            return false;
        }

        int x0 = Mathf.Min((int)gx, nx - 2);
        int z0 = Mathf.Min((int)gz, nz - 2);
        float fx = gx - x0;
        float fz = gz - z0;

        float h00 = m_height[x0, z0];
        float h10 = m_height[x0 + 1, z0];
        float h01 = m_height[x0, z0 + 1];
        float h11 = m_height[x0 + 1, z0 + 1];

        float h = Mathf.Lerp(Mathf.Lerp(h00, h10, fx), Mathf.Lerp(h01, h11, fx), fz);
        surfaceY = h - m_sink;
        return true;
    }

    /// <summary>
    /// ローカル座標のXZ位置における表面のローカル法線を計算します。
    /// </summary>
    public Vector3 CalculateLocalNormal(float localX, float localZ)
    {
        int nx = m_gridSizeX;
        int nz = m_gridSizeZ;
        float halfSizeX = (nx - 1) * m_voxelSizeX * 0.5f;
        float halfSizeZ = (nz - 1) * m_voxelSizeZ * 0.5f;

        float gx = Mathf.Clamp((localX + halfSizeX) / m_voxelSizeX, 0f, nx - 1);
        float gz = Mathf.Clamp((localZ + halfSizeZ) / m_voxelSizeZ, 0f, nz - 1);

        int x0 = Mathf.Min((int)gx, nx - 2);
        int z0 = Mathf.Min((int)gz, nz - 2);
        float fx = gx - x0;
        float fz = gz - z0;

        float h00 = m_height[x0, z0];
        float h10 = m_height[x0 + 1, z0];
        float h01 = m_height[x0, z0 + 1];
        float h11 = m_height[x0 + 1, z0 + 1];

        float dhdx = Mathf.Lerp(h10 - h00, h11 - h01, fz) / m_voxelSizeX;
        float dhdz = Mathf.Lerp(h01 - h00, h11 - h10, fx) / m_voxelSizeZ;
        return new Vector3(-dhdx, 1f, -dhdz).normalized;
    }

    /// <summary>
    /// ローカル座標のXZ位置における表面の高さと法線を取得します。
    /// </summary>
    /// <returns>
    /// true：グリッド範囲内でサンプリングに成功しました。
    /// false：グリッド範囲外です。
    /// </returns>
    public bool TrySampleSurface(Vector3 localPos, out float surfaceY, out Vector3 localNormal)
    {
        if (!TrySampleHeight(localPos.x, localPos.z, out surfaceY))
        {
            localNormal = Vector3.up;
            return false;
        }

        localNormal = CalculateLocalNormal(localPos.x, localPos.z);
        return true;
    }

    /// <summary>
    /// ローカル空間で球の下側の面を使って砂を削り、変更されたグリッド範囲を返します。
    /// </summary>
    /// <returns>
    /// true：1箇所以上の砂が削られました。
    /// false：砂の高さは変化しませんでした。
    /// </returns>
    public bool Carve(
        Vector3 localCenter,
        float localRadius,
        float smoothK,
        out float removedHeightSum,
        out int modifiedMinX,
        out int modifiedMaxX,
        out int modifiedMinZ,
        out int modifiedMaxZ)
    {
        removedHeightSum = 0f;
        modifiedMinX = int.MaxValue;
        modifiedMaxX = int.MinValue;
        modifiedMinZ = int.MaxValue;
        modifiedMaxZ = int.MinValue;

        int nx = m_gridSizeX;
        int nz = m_gridSizeZ;
        float halfSizeX = (nx - 1) * m_voxelSizeX * 0.5f;
        float halfSizeZ = (nz - 1) * m_voxelSizeZ * 0.5f;

        int cx = Mathf.RoundToInt((localCenter.x + halfSizeX) / m_voxelSizeX);
        int cz = Mathf.RoundToInt((localCenter.z + halfSizeZ) / m_voxelSizeZ);
        int rx = Mathf.CeilToInt(localRadius / m_voxelSizeX) + 1;
        int rz = Mathf.CeilToInt(localRadius / m_voxelSizeZ) + 1;
        float radiusSq = localRadius * localRadius;
        bool modified = false;

        int minX = Mathf.Max(1, cx - rx);
        int maxX = Mathf.Min(nx - 2, cx + rx);
        int minZ = Mathf.Max(1, cz - rz);
        int maxZ = Mathf.Min(nz - 2, cz + rz);

        for (int x = minX; x <= maxX; x++)
        {
            float dx = (x * m_voxelSizeX - halfSizeX) - localCenter.x;
            float dxSq = dx * dx;
            if (dxSq >= radiusSq)
            {
                continue;
            }

            for (int z = minZ; z <= maxZ; z++)
            {
                float dz = (z * m_voxelSizeZ - halfSizeZ) - localCenter.z;
                float distSq = dxSq + dz * dz;
                if (distSq >= radiusSq)
                {
                    continue;
                }

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

                    if (x < modifiedMinX) modifiedMinX = x;
                    if (x > modifiedMaxX) modifiedMaxX = x;
                    if (z < modifiedMinZ) modifiedMinZ = z;
                    if (z > modifiedMaxZ) modifiedMaxZ = z;
                }
            }
        }

        if (modified)
        {
            ExpandActiveRect(modifiedMinX, modifiedMaxX, modifiedMinZ, modifiedMaxZ);
        }

        return modified;
    }

    /// <summary>
    /// 安息角に基づいてアクティブ領域のみ砂の崩れを1ステップ進行させます。
    /// </summary>
    /// <returns>
    /// true：砂が動きました。
    /// false：すべての斜面が安定しています。
    /// </returns>
    public bool SimulateSettleStep(
        float reposeAngle,
        Action<Vector3, Vector3, float> onSandSlidLocal,
        out int changedMinX,
        out int changedMaxX,
        out int changedMinZ,
        out int changedMaxZ)
    {
        changedMinX = int.MaxValue;
        changedMaxX = int.MinValue;
        changedMinZ = int.MaxValue;
        changedMaxZ = int.MinValue;

        if (!m_hasActiveRect)
        {
            return false;
        }

        int nxMax = m_gridSizeX;
        int nzMax = m_gridSizeZ;
        float tanAngle = Mathf.Tan(reposeAngle * Mathf.Deg2Rad);
        float maxDiffX = tanAngle * m_voxelSizeX;
        float maxDiffZ = tanAngle * m_voxelSizeZ;
        float maxDiffDiag = tanAngle * Mathf.Sqrt(m_voxelSizeX * m_voxelSizeX + m_voxelSizeZ * m_voxelSizeZ);

        int scanMinX = Mathf.Max(1, m_activeMinX - 1);
        int scanMaxX = Mathf.Min(nxMax - 2, m_activeMaxX + 1);
        int scanMinZ = Mathf.Max(1, m_activeMinZ - 1);
        int scanMaxZ = Mathf.Min(nzMax - 2, m_activeMaxZ + 1);

        int clearMinX = scanMinX - 1;
        int clearMaxX = scanMaxX + 1;
        int clearMinZ = scanMinZ - 1;
        int clearMaxZ = scanMaxZ + 1;

        for (int x = clearMinX; x <= clearMaxX; x++)
        {
            for (int z = clearMinZ; z <= clearMaxZ; z++)
            {
                m_delta[x, z] = 0f;
            }
        }

        bool moved = false;
        int nextMinX = int.MaxValue;
        int nextMaxX = int.MinValue;
        int nextMinZ = int.MaxValue;
        int nextMaxZ = int.MinValue;

        for (int x = scanMinX; x <= scanMaxX; x++)
        {
            for (int z = scanMinZ; z <= scanMaxZ; z++)
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
                    float maxDiff = (i < 2) ? maxDiffX : (i < 4) ? maxDiffZ : maxDiffDiag;
                    float excess = (currentHeight - m_height[nx, nz]) - maxDiff;

                    if (excess > MIN_EXCESS_HEIGHT)
                    {
                        float transfer = excess * SETTLE_RATE;
                        m_delta[x, z] -= transfer;
                        m_delta[nx, nz] += transfer;
                        moved = true;

                        if (x < nextMinX) nextMinX = x;
                        if (x > nextMaxX) nextMaxX = x;
                        if (z < nextMinZ) nextMinZ = z;
                        if (z > nextMaxZ) nextMaxZ = z;

                        if (nx < nextMinX) nextMinX = nx;
                        if (nx > nextMaxX) nextMaxX = nx;
                        if (nz < nextMinZ) nextMinZ = nz;
                        if (nz > nextMaxZ) nextMaxZ = nz;
                    }
                }
            }
        }

        if (!moved)
        {
            m_hasActiveRect = false;
            return false;
        }

        int reportedCount = 0;
        bool checkSlide = onSandSlidLocal != null;

        for (int x = clearMinX; x <= clearMaxX; x++)
        {
            bool isBorderX = (x == 0 || x == nxMax - 1);
            for (int z = clearMinZ; z <= clearMaxZ; z++)
            {
                if (isBorderX || z == 0 || z == nzMax - 1)
                {
                    m_height[x, z] = 0f;
                    continue;
                }

                float d = m_delta[x, z];
                if (d == 0f)
                {
                    continue;
                }

                if (checkSlide
                    && d < -SLIDE_REPORT_THRESHOLD
                    && reportedCount < MAX_SLIDE_REPORTS_PER_STEP
                    && UnityEngine.Random.value < SLIDE_REPORT_PROBABILITY)
                {
                    reportedCount++;
                    Vector3 downhillLocal = new Vector3(
                        -(m_height[x + 1, z] - m_height[x - 1, z]) / (2f * m_voxelSizeX),
                        0f,
                        -(m_height[x, z + 1] - m_height[x, z - 1]) / (2f * m_voxelSizeZ)
                    );
                    Vector3 baseLocal = ConvertGridToLocalXZ(x, z);
                    Vector3 posLocal = new Vector3(baseLocal.x, m_height[x, z] - m_sink, baseLocal.z);
                    onSandSlidLocal.Invoke(posLocal, downhillLocal, -d);
                }

                m_height[x, z] = Mathf.Max(0f, m_height[x, z] + d);

                if (x < changedMinX) changedMinX = x;
                if (x > changedMaxX) changedMaxX = x;
                if (z < changedMinZ) changedMinZ = z;
                if (z > changedMaxZ) changedMaxZ = z;
            }
        }

        m_activeMinX = Mathf.Clamp(nextMinX, 1, nxMax - 2);
        m_activeMaxX = Mathf.Clamp(nextMaxX, 1, nxMax - 2);
        m_activeMinZ = Mathf.Clamp(nextMinZ, 1, nzMax - 2);
        m_activeMaxZ = Mathf.Clamp(nextMaxZ, 1, nzMax - 2);

        return true;
    }

    /// <summary>
    /// 崩れ計算対象のアクティブ矩形を指定範囲で拡張します。
    /// </summary>
    private void ExpandActiveRect(int minX, int maxX, int minZ, int maxZ)
    {
        if (!m_hasActiveRect)
        {
            m_hasActiveRect = true;
            m_activeMinX = minX;
            m_activeMaxX = maxX;
            m_activeMinZ = minZ;
            m_activeMaxZ = maxZ;
        }
        else
        {
            if (minX < m_activeMinX) m_activeMinX = minX;
            if (maxX > m_activeMaxX) m_activeMaxX = maxX;
            if (minZ < m_activeMinZ) m_activeMinZ = minZ;
            if (maxZ > m_activeMaxZ) m_activeMaxZ = maxZ;
        }
    }

    /// <summary>
    /// 安息角に沿ったデフォルトの円錐形を生成します。
    /// </summary>
    private void InitDefaultCone(float reposeAngle)
    {
        int nx = m_gridSizeX;
        int nz = m_gridSizeZ;
        float halfSizeX = (nx - 1) * m_voxelSizeX * 0.5f;
        float halfSizeZ = (nz - 1) * m_voxelSizeZ * 0.5f;
        float tanAngle = Mathf.Tan(reposeAngle * Mathf.Deg2Rad);
        float margin = 2.0f * Mathf.Max(m_voxelSizeX, m_voxelSizeZ);
        float maxRadius = Mathf.Max(MIN_CONE_RADIUS, Mathf.Min(halfSizeX, halfSizeZ) - margin);
        float baseRadius = Mathf.Min(m_mountainHeight / tanAngle, maxRadius);
        float peak = baseRadius * tanAngle + m_sink;

        for (int x = 0; x < nx; x++)
        {
            float dx = x * m_voxelSizeX - halfSizeX;
            for (int z = 0; z < nz; z++)
            {
                float dz = z * m_voxelSizeZ - halfSizeZ;
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

        if (!heightmapTexture.isReadable)
        {
            Debug.LogWarning($"[MarchingCubesMountain] テクスチャ '{heightmapTexture.name}' の Read/Write が無効です。Import Settings で Read/Write Enabled を有効にしてください。");
            InitDefaultCone(reposeAngle);
            return;
        }

        int nx = m_gridSizeX;
        int nz = m_gridSizeZ;
        for (int x = 0; x < nx; x++)
        {
            float u = (float)x / (nx - 1);
            for (int z = 0; z < nz; z++)
            {
                float v = (float)z / (nz - 1);
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
        int nx = m_gridSizeX;
        int nz = m_gridSizeZ;
        float halfSizeX = (nx - 1) * m_voxelSizeX * 0.5f;
        float halfSizeZ = (nz - 1) * m_voxelSizeZ * 0.5f;

        for (int x = 0; x < nx; x++)
        {
            float dx = x * m_voxelSizeX - halfSizeX;
            for (int z = 0; z < nz; z++)
            {
                float dz = z * m_voxelSizeZ - halfSizeZ;
                float shape = Mathf.Max(0f, 1f - Mathf.Abs(dx) / CUSTOM_HALF_WIDTH)
                            * Mathf.Max(0f, 1f - Mathf.Abs(dz) / CUSTOM_HALF_LENGTH);
                m_height[x, z] = shape * m_mountainHeight;
            }
        }
    }

    /// <summary>
    /// 初期化時にグリッド外周の砂の高さを0に設定します。
    /// </summary>
    private void ClearAllBorder()
    {
        for (int x = 0; x < m_gridSizeX; x++)
        {
            m_height[x, 0] = 0f;
            m_height[x, m_gridSizeZ - 1] = 0f;
        }

        for (int z = 0; z < m_gridSizeZ; z++)
        {
            m_height[0, z] = 0f;
            m_height[m_gridSizeX - 1, z] = 0f;
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