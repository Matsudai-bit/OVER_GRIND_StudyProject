using System.Collections.Generic;
using UnityEngine;

//自動でリストを構築しランダムで出すクラス
public class AutoGameSpawn : MonoBehaviour
{
    [Header("スポーンさせるプレハブのリスト")]
    public List<GameObject> myExternalTargetList;

    [Header("SpawnTargetスクリプトへの参照")]
    public SpawnTarget spawner;

    [Header("スポーン間隔（秒）")]
    public float spawnInterval = 2.0f;

    [Header("一度に同時にスポーンさせる個数")]
    public int spawnCount = 3;


    void Start()
    {
        // 繰り返し実行を開始
        InvokeRepeating(nameof(SpawnMultipleTargets), 1f, spawnInterval);
    }

    //リストからランダムで位置を決定する関数
    void SpawnMultipleTargets()
    {
        //リストの中身がない場合返す
        if (myExternalTargetList == null || myExternalTargetList.Count == 0) return;
        if (spawner == null) return;
        Debug.Log("オブジェクトのスポーン");

        //新しくリストを作る
        List<GameObject> deployList = new List<GameObject>();

        //指定されたカウント分作る
        for (int i = 0; i < spawnCount; i++)
        {
            // 原本リストから直接ランダムに選ぶ
            int randomIndex = Random.Range(0, myExternalTargetList.Count);
            deployList.Add(myExternalTargetList[randomIndex]);
        }

        //オブジェクトをゲームシーンに出す
        spawner.SpawnTargets(deployList);
    }
}