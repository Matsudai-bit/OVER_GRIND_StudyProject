using System.Collections.Generic;
using UnityEngine;

public class SpawnTarget : MonoBehaviour
{
    [Header("ターゲットスポーン位置設定")]

    public float leftEdgeX = -10f;   //スポーンする左端
    public float minY = -4.5f;      //Y座標下
    public float maxY = 4.5f;       //Y座標上

    public void SpawnTargets(List<GameObject> targetPrefabs)
    {
        

        foreach (GameObject prefab in targetPrefabs)
        {
            if (prefab == null) continue;

            // Xは左端固定、Yはランダム、Zは0
            Vector3 spawnPosition = new Vector3(
                leftEdgeX,
                Random.Range(minY, maxY),
                0f
            );

            // インスタンス化
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
    }
}
