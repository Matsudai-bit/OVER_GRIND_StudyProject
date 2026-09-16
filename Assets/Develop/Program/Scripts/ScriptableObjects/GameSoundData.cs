using System;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// サウンドID
/// </summary>
[Serializable]
public struct SoundData
{
    public string   labelName;
    public SoundID  id;
    public AudioClip clip;
}

/// <summary>
/// サウンドデータのScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "GameSoundData", menuName = "ScriptableObjects/GameSoundData")]
public class GameSoundData : ScriptableObject
{
    [Header("サウンドデータ配列")]
    [SerializeField]
    private SoundData[] soundData;

    public SoundData[] SoundData
    {
        get { return soundData; }
    }


    private static GameSoundData s_instance;



    public static GameSoundData GetInstance
    {
        get
        {
            if (s_instance == null)
            {
                // リソースフォルダからスクリプタブルオブジェクトをロード
                s_instance = Resources.Load<GameSoundData>("GameSoundData");
                if (s_instance == null)
                {
                    Debug.LogError("GameSoundDataスクリプタブルオブジェクトが'Resources'フォルダに見つかりません。");
                }

            }
            return s_instance;
        }
    }


}

// **********************************************
// ************** エディタコード ****************
// **********************************************
#if UNITY_EDITOR

[CustomEditor(typeof(GameSoundData))]
public class GameSoundDataEditor : Editor
{
    // SoundID の enum 型をここで取得します
    private System.Type soundIDType = typeof(SoundID);

    public override void OnInspectorGUI()
    {
        GameSoundData data = (GameSoundData)target;

        // 1. Fetch ボタンの描画
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Fetch SoundID Labels", GUILayout.Height(30)))
        {
            FetchLabels(data);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // 2. 通常のインスペクター描画
        DrawDefaultInspector();

        // 変更を保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }

        // 3. labelNameをインスペクターに表示（デバッグ用）
        DrawLabels(data);
    }

    private void DrawLabels(GameSoundData data)
    {
        // 配列のカスタム表示
        SerializedProperty soundDataProp = serializedObject.FindProperty("soundData");

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Sound Data Labels (Current)", EditorStyles.boldLabel);

        // 配列の各要素の labelName を表示
        for (int i = 0; i < soundDataProp.arraySize; i++)
        {
            SerializedProperty element = soundDataProp.GetArrayElementAtIndex(i);
            // labelName は [HideInInspector] で非表示にしているため、ここで表示
            EditorGUILayout.PropertyField(element.FindPropertyRelative("labelName"));
        }

        serializedObject.ApplyModifiedProperties();
    }


    /// <summary>
    /// SoundIDのenum名を取得し、labelNameに設定する処理
    /// </summary>
    private void FetchLabels(GameSoundData data)
    {
        if (data.SoundData == null) return;

        if (!soundIDType.IsEnum)
        {
            Debug.LogError("GameSoundDataEditor: soundIDType が有効な enum 型ではありません。コード内のコメントに従って修正してください。");
            return;
        }

        Undo.RecordObject(data, "Fetch SoundID Labels");

        for (int i = 0; i < data.SoundData.Length; i++)
        {
            SoundData sound = data.SoundData[i];

            // SoundDataのid (enum値) をその enum 名に変換
            string enumName = System.Enum.GetName(soundIDType, sound.id);

            if (!string.IsNullOrEmpty(enumName))
            {
                sound.labelName = enumName;
            }
            else
            {
                sound.labelName = "ID_" + sound.id.ToString() + " (Invalid)";
            }

            data.SoundData[i] = sound;
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets(); // アセットへの変更を確実に保存
        Debug.Log("SoundID の enum 名を labelName に設定しました。");
    }
}
#endif