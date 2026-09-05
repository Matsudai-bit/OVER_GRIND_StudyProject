using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspectorで編集可能な辞書を管理します。
/// </summary>
/// <typeparam name="TKey">キーの型。</typeparam>
/// <typeparam name="TValue">値の型。</typeparam>
[Serializable]
public class SerializeDictionary<TKey, TValue> :
    ISerializationCallbackReceiver,
    IEnumerable<KeyValuePair<TKey, TValue>>
{
    /// <summary>
    /// Inspectorに表示する辞書要素。
    /// </summary>
    [SerializeField]
    private List<DictionaryElement> m_dictionaryElements = new();

    /// <summary>
    /// 実行時に使用する辞書。
    /// </summary>
    private readonly Dictionary<TKey, TValue> m_dictionary = new();

    /// <summary>
    /// 辞書に登録されている要素数を取得します。
    /// </summary>
    public int Count => m_dictionary.Count;

    /// <summary>
    /// 指定したキーの値を取得または設定します。
    /// </summary>
    /// <param name="key">操作するキー。</param>
    /// <returns>キーに対応する値。</returns>
    public TValue this[TKey key]
    {
        get => m_dictionary[key];
        set
        {
            m_dictionary[key] = value;
            SynchronizeListFromDictionary();
        }
    }

    /// <summary>
    /// Inspectorに表示する辞書要素です。
    /// </summary>
    [Serializable]
    private sealed class DictionaryElement
    {
        /// <summary>
        /// 辞書のキー。
        /// </summary>
        [SerializeField]
        private TKey m_key;

        /// <summary>
        /// 辞書の値。
        /// </summary>
        [SerializeField]
        private TValue m_value;

        /// <summary>
        /// 辞書のキーを取得します。
        /// </summary>
        public TKey Key => m_key;

        /// <summary>
        /// 辞書の値を取得します。
        /// </summary>
        public TValue Value => m_value;

        /// <summary>
        /// 辞書要素を生成します。
        /// </summary>
        /// <param name="key">辞書のキー。</param>
        /// <param name="value">辞書の値。</param>
        public DictionaryElement(TKey key, TValue value)
        {
            m_key = key;
            m_value = value;
        }
    }

    /// <summary>
    /// 指定したキーと値を追加します。
    /// </summary>
    /// <param name="key">追加するキー。</param>
    /// <param name="value">追加する値。</param>
    /// <returns>
    /// true：追加しました。
    /// false：同じキーがすでに存在します。
    /// </returns>
    public bool TryAdd(TKey key, TValue value)
    {
        if (!m_dictionary.TryAdd(key, value))
        {
            return false;
        }

        SynchronizeListFromDictionary();
        return true;
    }

    /// <summary>
    /// 指定したキーを持つ要素を削除します。
    /// </summary>
    /// <param name="key">削除するキー。</param>
    /// <returns>
    /// true：削除しました。
    /// false：キーが存在しません。
    /// </returns>
    public bool Remove(TKey key)
    {
        if (!m_dictionary.Remove(key))
        {
            return false;
        }

        SynchronizeListFromDictionary();
        return true;
    }

    /// <summary>
    /// 指定したキーの値を取得します。
    /// </summary>
    /// <param name="key">検索するキー。</param>
    /// <param name="value">取得した値。</param>
    /// <returns>
    /// true：値を取得しました。
    /// false：キーが存在しません。
    /// </returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        return m_dictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// 指定したキーが存在するか確認します。
    /// </summary>
    /// <param name="key">確認するキー。</param>
    /// <returns>
    /// true：キーが存在します。
    /// false：キーが存在しません。
    /// </returns>
    public bool ContainsKey(TKey key)
    {
        return m_dictionary.ContainsKey(key);
    }

    /// <summary>
    /// すべての要素を削除します。
    /// </summary>
    public void Clear()
    {
        m_dictionary.Clear();
        m_dictionaryElements.Clear();
    }

    /// <summary>
    /// 辞書の列挙子を取得します。
    /// </summary>
    /// <returns>辞書の列挙子。</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return m_dictionary.GetEnumerator();
    }

    /// <summary>
    /// 辞書の列挙子を取得します。
    /// </summary>
    /// <returns>辞書の列挙子。</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Unityによるシリアライズ前の処理です。
    /// </summary>
    public void OnBeforeSerialize()
    {
    }

    /// <summary>
    /// Unityによるデシリアライズ後の処理です。
    /// </summary>
    public void OnAfterDeserialize()
    {
        RebuildDictionary();
    }

    /// <summary>
    /// Inspectorの要素から辞書を再構築します。
    /// </summary>
    private void RebuildDictionary()
    {
        m_dictionary.Clear();

        if (m_dictionaryElements == null)
        {
            m_dictionaryElements = new List<DictionaryElement>();
            return;
        }

        foreach (DictionaryElement element in m_dictionaryElements)
        {
            if (element == null)
            {
                continue;
            }

            TKey key = element.Key;

            // nullを許容するキー型の場合はnullキーを無視します。
            if (key is null)
            {
                continue;
            }

            // 重複時は後に設定された値で上書きします。
            m_dictionary[key] = element.Value;
        }
    }

    /// <summary>
    /// 辞書の内容をInspector用Listへ反映します。
    /// </summary>
    private void SynchronizeListFromDictionary()
    {
        m_dictionaryElements.Clear();

        foreach (KeyValuePair<TKey, TValue> pair in m_dictionary)
        {
            m_dictionaryElements.Add(
                new DictionaryElement(pair.Key, pair.Value));
        }
    }
}