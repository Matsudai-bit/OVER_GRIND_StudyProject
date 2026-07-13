using System.Reflection; 
using UnityEditor; 
using UnityEngine; 


[CustomPropertyDrawer(typeof(EffectSelectorAttribute))]
public class EffectSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var selectorAttribute = attribute as EffectSelectorAttribute;
        var target = property.serializedObject.targetObject;

        var managerField = target.GetType().GetField(
            selectorAttribute.GetManagerFieldName(),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (managerField == null)
        {
            EditorGUI.LabelField(position, label.text, "指定の EffectManager が見つかりません");
            EditorGUI.EndProperty();
            return;
        }

        var manager = managerField.GetValue(target) as EffectManager;
        if (manager == null)
        {
            EditorGUI.LabelField(position, label.text, "EffectManager が設定されていません");
            EditorGUI.EndProperty();
            return;
        }

        var effects = manager.GetEffects();
        if (effects == null || effects.Length == 0)
        {
            EditorGUI.LabelField(position, label.text, "EffectManager にエフェクトがありません");
            EditorGUI.EndProperty();
            return;
        }

        int currentIndex = -1;
        var current = property.objectReferenceValue as EffectData;

        string[] names = new string[effects.Length];
        for (int i = 0; i < effects.Length; i++)
        {
            names[i] = effects[i].GetEffectName();
            if (effects[i] == current)
            {
                currentIndex = i;
            }
        }

        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, names);

        if (newIndex >= 0)
        {
            property.objectReferenceValue = effects[newIndex];
        }

        EditorGUI.EndProperty();
    }
}
