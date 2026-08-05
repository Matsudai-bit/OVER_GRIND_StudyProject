using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Genetrate : MonoBehaviour
{
    public GameObject obstaclePrefab;   // InspectorでObstacleプレハブをセット
    public float interval = 1f;         // 生成間隔（秒）
    public float spawnY = 10f;          // 生成するY座標（画面上）
    public float rangeX = 8f;           // X方向の生成範囲
    public float minDistance = 2f;      // 障害物同士の最小距離

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        InvokeRepeating(nameof(TrySpawn), 0f, interval);
    }

    void TrySpawn()
    {
        // ランダムな候補座標を数回試す
        for (int i = 0; i < 3; i++)
        {
            DebugManager.Log("壁の出現");
            float x = Random.Range(-rangeX, rangeX);
            Vector3 candidate = new Vector3(x, spawnY, Random.Range(5, 10.0f));

            if (IsFarEnough(candidate))
            {
                Instantiate(obstaclePrefab, candidate, Quaternion.identity);
                spawnedPositions.Add(candidate);
                // intervalと同じ秒数後に座標をリストから削除
                StartCoroutine(RemovePosition(candidate, interval));
                break;
            }
        }
    }

    IEnumerator RemovePosition(Vector3 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        spawnedPositions.Remove(pos);
    }

    bool IsFarEnough(Vector3 candidate)
    {
        foreach (var pos in spawnedPositions)
        {
            if (Vector3.Distance(candidate, pos) < minDistance)
                return false;
        }
        return true;
    }
}
