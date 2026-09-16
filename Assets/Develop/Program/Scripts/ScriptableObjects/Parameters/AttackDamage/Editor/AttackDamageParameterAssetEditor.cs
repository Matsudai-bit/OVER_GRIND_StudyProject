using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// 攻撃ダメージパラメータAssetのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(AttackDamageParameterAssetBase), true)]
public sealed class AttackDamageParameterAssetEditor : Editor
{
    // SerializedProperty名
    private const string ATTACK_DAMAGE_PARAMETERS_PROPERTY_NAME = "m_attackDamageParameters";
    private const string ATTACK_IDENTIFIER_PROPERTY_NAME = "m_attackIdentifier";
    private const string HITBOX_DAMAGE_PARAMETERS_PROPERTY_NAME = "m_hitboxDamageParameters";
    private const string HITBOX_ID_PROPERTY_NAME = "m_hitboxId";
    private const string DAMAGE_PROPERTY_NAME = "m_damage";

    // 未設定時の表示名
    private const string UNASSIGNED_ATTACK_NAME = "Attack ID 未設定";
    private const string UNKNOWN_HITBOX_NAME = "HitBox ID";

    // 攻撃ダメージパラメータ
    private SerializedProperty m_attackDamageParametersProperty;

    // 攻撃ダメージ一覧
    private ReorderableList m_attackDamageParameterList;

    // 攻撃ごとのHitBoxダメージ一覧
    private readonly Dictionary<string, ReorderableList> m_hitboxDamageLists = new();

    private void OnEnable()
    {
        m_attackDamageParametersProperty =
            serializedObject.FindProperty(ATTACK_DAMAGE_PARAMETERS_PROPERTY_NAME);

        if (m_attackDamageParametersProperty == null)
        {
            return;
        }

        CreateAttackDamageParameterList();
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptReference();

        EditorGUILayout.Space();

        if (m_attackDamageParameterList != null)
        {
            m_attackDamageParameterList.DoLayoutList();
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Script参照を読み取り専用で表示します。
    /// </summary>
    private void DrawScriptReference()
    {
        SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");

        if (scriptProperty == null)
        {
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(scriptProperty);
        }
    }

    /// <summary>
    /// 攻撃ダメージ一覧を生成します。
    /// </summary>
    private void CreateAttackDamageParameterList()
    {
        m_attackDamageParameterList = new ReorderableList(
            serializedObject,
            m_attackDamageParametersProperty,
            true,
            true,
            true,
            true);

        m_attackDamageParameterList.drawHeaderCallback =
            rect => EditorGUI.LabelField(rect, "攻撃ダメージ");

        m_attackDamageParameterList.drawElementCallback =
            DrawAttackDamageParameterElement;

        m_attackDamageParameterList.elementHeightCallback =
            GetAttackDamageParameterElementHeight;

        m_attackDamageParameterList.onAddCallback =
            list =>
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);

                if (list.index >= 0 &&
                    list.index < m_attackDamageParametersProperty.arraySize)
                {
                    SerializedProperty elementProperty =
                        m_attackDamageParametersProperty.GetArrayElementAtIndex(list.index);

                    elementProperty.isExpanded = true;
                }

                m_hitboxDamageLists.Clear();
            };

        m_attackDamageParameterList.onRemoveCallback =
            list =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                m_hitboxDamageLists.Clear();
            };

        m_attackDamageParameterList.onReorderCallback =
            _ => m_hitboxDamageLists.Clear();
    }

    /// <summary>
    /// 1つの攻撃ダメージパラメータを描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="index">要素番号。</param>
    /// <param name="isActive">選択中か。</param>
    /// <param name="isFocused">フォーカス中か。</param>
    private void DrawAttackDamageParameterElement(
        Rect rect,
        int index,
        bool isActive,
        bool isFocused)
    {
        if (!TryGetAttackElement(index, out SerializedProperty elementProperty))
        {
            return;
        }

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        rect.y += spacing;

        Rect foldoutRect = new Rect(
            rect.x,
            rect.y,
            rect.width,
            lineHeight);

        elementProperty.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            elementProperty.isExpanded,
            GetAttackName(elementProperty),
            true);

        if (!elementProperty.isExpanded)
        {
            return;
        }

        rect.y += lineHeight + spacing;

        SerializedProperty attackIdentifierProperty =
            elementProperty.FindPropertyRelative(ATTACK_IDENTIFIER_PROPERTY_NAME);

        if (attackIdentifierProperty != null)
        {
            Rect attackIdentifierRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                lineHeight);

            DrawAttackIdentifierField(
                attackIdentifierRect,
                attackIdentifierProperty);

            rect.y += lineHeight + spacing;
        }

        SerializedProperty hitboxParametersProperty =
            elementProperty.FindPropertyRelative(HITBOX_DAMAGE_PARAMETERS_PROPERTY_NAME);

        if (hitboxParametersProperty == null)
        {
            return;
        }

        ReorderableList hitboxList =
            GetOrCreateHitboxDamageList(hitboxParametersProperty);

        Rect hitboxListRect = new Rect(
            rect.x,
            rect.y,
            rect.width,
            hitboxList.GetHeight());

        hitboxList.DoList(hitboxListRect);
    }

    /// <summary>
    /// 攻撃IDを描画します。
    /// HeaderAttributeなどのDecoratorDrawerを経由させず、
    /// CustomEditor側のレイアウトだけで描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="property">攻撃ID。</param>
    private void DrawAttackIdentifierField(
        Rect rect,
        SerializedProperty property)
    {
        EditorGUI.BeginChangeCheck();

        Object newValue = EditorGUI.ObjectField(
            rect,
            "Attack ID",
            property.objectReferenceValue,
            typeof(AttackIdentifier),
            false);

        if (EditorGUI.EndChangeCheck())
        {
            property.objectReferenceValue = newValue;
        }
    }

    /// <summary>
    /// 攻撃ダメージパラメータの描画高さを取得します。
    /// </summary>
    /// <param name="index">要素番号。</param>
    /// <returns>描画高さ。</returns>
    private float GetAttackDamageParameterElementHeight(int index)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!TryGetAttackElement(index, out SerializedProperty elementProperty))
        {
            return lineHeight + spacing * 2.0f;
        }

        // 折りたたみ行
        float height = lineHeight + spacing * 2.0f;

        if (!elementProperty.isExpanded)
        {
            return height;
        }

        // Attack ID行
        height += lineHeight + spacing;

        SerializedProperty hitboxParametersProperty =
            elementProperty.FindPropertyRelative(HITBOX_DAMAGE_PARAMETERS_PROPERTY_NAME);

        if (hitboxParametersProperty != null)
        {
            ReorderableList hitboxList =
                GetOrCreateHitboxDamageList(hitboxParametersProperty);

            height += hitboxList.GetHeight();
        }

        return height + spacing;
    }

    /// <summary>
    /// HitBoxダメージ一覧を取得します。
    /// </summary>
    /// <param name="property">HitBoxダメージ一覧のSerializedProperty。</param>
    /// <returns>HitBoxダメージ一覧。</returns>
    private ReorderableList GetOrCreateHitboxDamageList(SerializedProperty property)
    {
        string propertyPath = property.propertyPath;

        if (m_hitboxDamageLists.TryGetValue(
                propertyPath,
                out ReorderableList cachedList))
        {
            return cachedList;
        }

        ReorderableList hitboxList = new ReorderableList(
            serializedObject,
            property,
            true,
            true,
            true,
            true);

        hitboxList.drawHeaderCallback =
            rect => EditorGUI.LabelField(rect, "HitBox別ダメージ");

        hitboxList.drawElementCallback =
            (rect, index, isActive, isFocused) =>
                DrawHitboxDamageParameterElement(
                    property,
                    rect,
                    index,
                    isActive,
                    isFocused);

        hitboxList.elementHeightCallback =
            index => GetHitboxDamageParameterElementHeight(
                property,
                index);

        hitboxList.onAddCallback =
            list =>
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);

                if (list.index >= 0 &&
                    list.index < property.arraySize)
                {
                    SerializedProperty elementProperty =
                        property.GetArrayElementAtIndex(list.index);

                    elementProperty.isExpanded = true;
                }
            };

        m_hitboxDamageLists.Add(propertyPath, hitboxList);

        return hitboxList;
    }

    /// <summary>
    /// 1つのHitBoxダメージパラメータを描画します。
    /// </summary>
    /// <param name="listProperty">HitBoxダメージ一覧。</param>
    /// <param name="rect">描画領域。</param>
    /// <param name="index">要素番号。</param>
    /// <param name="isActive">選択中か。</param>
    /// <param name="isFocused">フォーカス中か。</param>
    private void DrawHitboxDamageParameterElement(
        SerializedProperty listProperty,
        Rect rect,
        int index,
        bool isActive,
        bool isFocused)
    {
        if (index < 0 ||
            index >= listProperty.arraySize)
        {
            return;
        }

        SerializedProperty elementProperty =
            listProperty.GetArrayElementAtIndex(index);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        rect.y += spacing;

        Rect foldoutRect = new Rect(
            rect.x,
            rect.y,
            rect.width,
            lineHeight);

        elementProperty.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            elementProperty.isExpanded,
            GetHitboxName(elementProperty),
            true);

        if (!elementProperty.isExpanded)
        {
            return;
        }

        rect.y += lineHeight + spacing;

        SerializedProperty hitboxIdProperty =
            elementProperty.FindPropertyRelative(HITBOX_ID_PROPERTY_NAME);

        if (hitboxIdProperty != null)
        {
            Rect hitboxIdRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                lineHeight);

            DrawHitboxIdField(
                hitboxIdRect,
                hitboxIdProperty);

            rect.y += lineHeight + spacing;
        }

        SerializedProperty damageProperty =
            elementProperty.FindPropertyRelative(DAMAGE_PROPERTY_NAME);

        if (damageProperty != null)
        {
            Rect damageRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                lineHeight);

            DrawDamageField(
                damageRect,
                damageProperty);
        }
    }

    /// <summary>
    /// HitBox IDを描画します。
    /// HeaderAttributeなどのDecoratorDrawerを経由させず、
    /// enum値のみを描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="property">HitBox ID。</param>
    private void DrawHitboxIdField(
        Rect rect,
        SerializedProperty property)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.LabelField(rect, "HitBox ID", UNKNOWN_HITBOX_NAME);
            return;
        }

        int selectedIndex = property.enumValueIndex;

        EditorGUI.BeginChangeCheck();

        int newIndex = EditorGUI.Popup(
            rect,
            "HitBox ID",
            selectedIndex,
            property.enumNames);

        if (EditorGUI.EndChangeCheck())
        {
            property.enumValueIndex = newIndex;
        }
    }

    /// <summary>
    /// ダメージ量を描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="property">ダメージ量。</param>
    private void DrawDamageField(
        Rect rect,
        SerializedProperty property)
    {
        EditorGUI.BeginChangeCheck();

        int newDamage = EditorGUI.IntField(
            rect,
            "Damage",
            property.intValue);

        if (EditorGUI.EndChangeCheck())
        {
            property.intValue = Mathf.Max(0, newDamage);
        }
    }

    /// <summary>
    /// HitBoxダメージパラメータの描画高さを取得します。
    /// </summary>
    /// <param name="listProperty">HitBoxダメージ一覧。</param>
    /// <param name="index">要素番号。</param>
    /// <returns>描画高さ。</returns>
    private float GetHitboxDamageParameterElementHeight(
        SerializedProperty listProperty,
        int index)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (index < 0 ||
            index >= listProperty.arraySize)
        {
            return lineHeight + spacing * 2.0f;
        }

        SerializedProperty elementProperty =
            listProperty.GetArrayElementAtIndex(index);

        // 折りたたみ行
        float height = lineHeight + spacing * 2.0f;

        if (!elementProperty.isExpanded)
        {
            return height;
        }

        // HitBox ID + Damage
        height += (lineHeight + spacing) * 2.0f;

        return height;
    }

    /// <summary>
    /// 攻撃IDの表示名を取得します。
    /// </summary>
    /// <param name="elementProperty">攻撃ダメージパラメータ。</param>
    /// <returns>攻撃IDの表示名。</returns>
    private string GetAttackName(SerializedProperty elementProperty)
    {
        SerializedProperty attackIdentifierProperty =
            elementProperty.FindPropertyRelative(ATTACK_IDENTIFIER_PROPERTY_NAME);

        if (attackIdentifierProperty == null ||
            attackIdentifierProperty.objectReferenceValue == null)
        {
            return UNASSIGNED_ATTACK_NAME;
        }

        return attackIdentifierProperty.objectReferenceValue.name;
    }

    /// <summary>
    /// HitBox IDの表示名を取得します。
    /// </summary>
    /// <param name="elementProperty">HitBoxダメージパラメータ。</param>
    /// <returns>HitBox IDの表示名。</returns>
    private string GetHitboxName(SerializedProperty elementProperty)
    {
        SerializedProperty hitboxIdProperty =
            elementProperty.FindPropertyRelative(HITBOX_ID_PROPERTY_NAME);

        if (hitboxIdProperty == null ||
            hitboxIdProperty.propertyType != SerializedPropertyType.Enum)
        {
            return UNKNOWN_HITBOX_NAME;
        }

        int enumIndex = hitboxIdProperty.enumValueIndex;

        if (enumIndex < 0 ||
            enumIndex >= hitboxIdProperty.enumNames.Length)
        {
            return UNKNOWN_HITBOX_NAME;
        }

        return hitboxIdProperty.enumNames[enumIndex];
    }

    /// <summary>
    /// 指定Indexの攻撃ダメージパラメータを取得します。
    /// </summary>
    /// <param name="index">要素番号。</param>
    /// <param name="elementProperty">取得した要素。</param>
    /// <returns>取得できた場合はtrue。</returns>
    private bool TryGetAttackElement(
        int index,
        out SerializedProperty elementProperty)
    {
        elementProperty = null;

        if (m_attackDamageParametersProperty == null ||
            index < 0 ||
            index >= m_attackDamageParametersProperty.arraySize)
        {
            return false;
        }

        elementProperty =
            m_attackDamageParametersProperty.GetArrayElementAtIndex(index);

        return elementProperty != null;
    }
}
