using System.Reflection;
using UnityEngine;

/******************************************************************************
 * @file    DebugParameterData.cs
 * @brief   デバッグパラメータ情報保持クラス
 * @author  Ryo Yagi
 * @date    2026/06/19
 *
 ******************************************************************************/

public class DebugParameterData
{
    public MonoBehaviour Target;

    public FieldInfo Field;

    public string Name => Field.Name;

    public object Value => Field.GetValue(Target);

    /******************************************************************************
     * @fn      SetValue
     * @brief   値設定
     *
     * @param   value : 設定値
     *
     ******************************************************************************/
    public void SetValue(object value)
    {
        Field.SetValue(Target, value);
    }

    public string TypeName
    {
        get
        {
            if (Field.FieldType == typeof(int))
                return "int";

            if (Field.FieldType == typeof(float))
                return "float";

            if (Field.FieldType == typeof(bool))
                return "bool";

            if (Field.FieldType == typeof(string))
                return "string";

            return Field.FieldType.Name;
        }
    }

    /******************************************************************************
 * @fn      UniqueKey
 * @brief   一意識別キー取得
 *
 * @return  一意キー
 *
 ******************************************************************************/
    public string UniqueKey =>
        $"{Target.GetInstanceID()}_{Name}";
}