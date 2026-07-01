using System;

/******************************************************************************
 * @file    DebugParameterFieldAttribute.cs
 * @brief   DebugParameterField属性
 * @author  Ryo Yagi
 * @date    2026/06/19
 *
 * @detail
 * DebugMonitorへ表示したい変数へ付与する属性
 *
 ******************************************************************************/

[AttributeUsage(AttributeTargets.Field)]
public class DebugParameterFieldAttribute : Attribute
{
}