using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/******************************************************************************
 * @file    DebugMonitorScanner.cs
 * @brief   DebugParameter収集クラス
 * @author  Ryo Yagi
 * @date    2026/06/19
 *
 * @detail
 * シーン内の全MonoBehaviourから
 * DebugParameterField付き変数を収集する
 *
 ******************************************************************************/

public static class DebugMonitorScanner
{
    /******************************************************************************
     * @fn      Scan
     * @brief   DebugParameter収集
     *
     * @return  DebugParameter一覧
     *
     ******************************************************************************/
    public static List<DebugParameterData> Scan()
    {
        List<DebugParameterData> result = new();

        // シーン内の全MonoBehaviour取得
        MonoBehaviour[] objects =
            Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsSortMode.None);

        foreach (var obj in objects)
        {
            if (obj == null)
                continue;

            // public/privateを含む全フィールド取得
            FieldInfo[] fields =
                obj.GetType().GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // DebugParameterFieldが無ければ無視
                if (field.GetCustomAttribute<DebugParameterFieldAttribute>() == null)
                    continue;

                result.Add(new DebugParameterData
                {
                    Target = obj,
                    Field = field
                });
            }
        }

        return result;
    }
}