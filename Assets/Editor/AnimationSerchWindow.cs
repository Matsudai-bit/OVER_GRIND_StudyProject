using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;
using System.Linq;

public class AnimationSearchWindow : EditorWindow
{
    private const float TARGET_FPS = 60f; // 60fps固定

    private DefaultAsset targetFolder;
    private string searchName = "";

    // サフィックスフィルター
    private bool filterAll = true;
    private bool filterI = false;
    private bool filterL = false;
    private bool filterO = false;

    // 検索結果
    private List<AnimationClip> searchResults = new List<AnimationClip>();
    private Vector2 scrollPosition;

    // Playable Graph 関連
    private GameObject scenePreviewModel;
    private AnimationClip selectedClip;

    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable clipPlayable;

    private bool isPlaying = false;
    private float currentTime = 0f;
    private double lastEditorTime = 0;

    [MenuItem("Tools/Animation Search Window")]
    public static void ShowWindow()
    {
        GetWindow<AnimationSearchWindow>("Animation Search");
    }

    private void OnGUI()
    {
        GUILayout.Label("アニメーション検索 ＆ Sceneプレビュー", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 検索ヘッダー
        DrawSearchHeader();

        EditorGUILayout.Space();

        // 2. 検索結果一覧
        DrawSearchResultsList();

        EditorGUILayout.Space();

        // 3. プレビュー＆シークバーGUI
        DrawScenePreviewControls();
    }

    private void DrawSearchHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // フォルダ指定
        EditorGUI.BeginChangeCheck();
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("検索対象フォルダ", targetFolder, typeof(DefaultAsset), false);

        // 文字列検索
        searchName = EditorGUILayout.TextField("名前検索", searchName);

        // サフィックスフィルター
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("サフィックス絞り込み:", GUILayout.Width(130));

        if (GUILayout.Toggle(filterAll, "すべて", "Button"))
        {
            filterAll = true;
            filterI = filterL = filterO = false;
        }
        if (GUILayout.Toggle(filterI, "_I (Start)", "Button"))
        {
            filterI = true;
            filterAll = false;
        }
        if (GUILayout.Toggle(filterL, "_L (Loop)", "Button"))
        {
            filterL = true;
            filterAll = false;
        }
        if (GUILayout.Toggle(filterO, "_O (Out)", "Button"))
        {
            filterO = true;
            filterAll = false;
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            ExecuteSearch();
        }

        if (GUILayout.Button("検索実行 / リフレッシュ", GUILayout.Height(25)))
        {
            ExecuteSearch();
        }

        EditorGUILayout.EndVertical();
    }

    private void ExecuteSearch()
    {
        searchResults.Clear();

        string folderPath = targetFolder != null ? AssetDatabase.GetAssetPath(targetFolder) : "Assets";
        string[] searchPaths = new string[] { folderPath };

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip t:Model", searchPaths);
        HashSet<AnimationClip> foundClips = new HashSet<AnimationClip>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    foundClips.Add(clip);
                }
            }
        }

        searchResults = foundClips.Where(clip =>
        {
            if (!string.IsNullOrEmpty(searchName) && clip.name.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (filterAll) return true;
            if (filterI && clip.name.EndsWith("_I")) return true;
            if (filterL && clip.name.EndsWith("_L")) return true;
            if (filterO && clip.name.EndsWith("_O")) return true;

            return false;
        }).OrderBy(c => c.name).ToList();
    }

    private void DrawSearchResultsList()
    {
        EditorGUILayout.LabelField($"検索結果 ({searchResults.Count} 件):", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        for (int i = 0; i < searchResults.Count; i++)
        {
            var clip = searchResults[i];
            if (clip == null) continue;

            bool isSelected = selectedClip == clip;
            EditorGUILayout.BeginHorizontal(isSelected ? "SelectionRect" : "HelpBox");

            // クリップ選択（選択時に即座に自動再生スタート）
            if (GUILayout.Button(clip.name, EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                SelectClipForScenePreview(clip);
            }

            // Projectビューでのハイライト表示
            if (GUILayout.Button("Ping", GUILayout.Width(45)))
            {
                EditorGUIUtility.PingObject(clip);
            }

            EditorGUILayout.EndHorizontal();

            // 他ツールへのドラッグ＆ドロップ対応
            Rect lastRect = GUILayoutUtility.GetLastRect();
            HandleDragAndDropForClip(lastRect, clip);
        }

        EditorGUILayout.EndScrollView();
    }

    private void HandleDragAndDropForClip(Rect rect, AnimationClip clip)
    {
        Event evt = Event.current;
        if (evt.type == EventType.MouseDrag && rect.Contains(evt.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[] { clip };
            DragAndDrop.StartDrag(clip.name);
            evt.Use();
        }
    }

    private void DrawScenePreviewControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("【Sceneビュー直接プレビュー設定 (60fps換算)】", EditorStyles.boldLabel);

        // Scene上のモデル設定（モデルが選択・変更されたら自動再生を開始）
        EditorGUI.BeginChangeCheck();
        scenePreviewModel = (GameObject)EditorGUILayout.ObjectField("Scene上のプレビューモデル", scenePreviewModel, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck())
        {
            if (selectedClip != null && scenePreviewModel != null)
            {
                currentTime = 0f;
                RebuildPlayableGraph();
                StartPreview(); // モデル指定時に自動再生
            }
        }

        if (selectedClip == null)
        {
            EditorGUILayout.HelpBox("一覧からアニメーションを選択すると、Sceneビュー上のモデルで自動再生されます。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        float maxDuration = selectedClip.length;
        int currentFrame = Mathf.RoundToInt(currentTime * TARGET_FPS);
        int totalFrames = Mathf.RoundToInt(maxDuration * TARGET_FPS);

        EditorGUILayout.LabelField("選択中:", $"{selectedClip.name}");
        EditorGUILayout.LabelField("全体フレーム / 全体時間:", $"F {totalFrames}  ({maxDuration:F2}s)");

        EditorGUI.BeginDisabledGroup(scenePreviewModel == null);

        // 1. シークバーとFrame入力ボックス（相互連動）
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("シークバー:", GUILayout.Width(70));

        EditorGUI.BeginChangeCheck();
        float sliderTime = GUILayout.HorizontalSlider(currentTime, 0f, Mathf.Max(0.001f, maxDuration));
        if (EditorGUI.EndChangeCheck())
        {
            PausePreview();
            currentTime = sliderTime;
            EnsureGraphValid();
            EvaluateAtTime(currentTime);
        }

        GUILayout.Label("Frame:", GUILayout.Width(45));

        EditorGUI.BeginChangeCheck();
        int inputFrame = EditorGUILayout.IntField(currentFrame, GUILayout.Width(50));
        if (EditorGUI.EndChangeCheck())
        {
            PausePreview();
            int clampedFrame = Mathf.Clamp(inputFrame, 0, totalFrames);
            currentTime = clampedFrame / TARGET_FPS;
            EnsureGraphValid();
            EvaluateAtTime(currentTime);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 2. コマ送り・コマ戻しボタン群
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("|< 0f (最初へ)", GUILayout.Height(22)))
        {
            PausePreview();
            currentTime = 0f;
            EnsureGraphValid();
            EvaluateAtTime(0f);
        }
        if (GUILayout.Button("< -1f (コマ戻し)", GUILayout.Height(22)))
        {
            PausePreview();
            currentTime = Mathf.Max(0f, currentTime - (1f / TARGET_FPS));
            EnsureGraphValid();
            EvaluateAtTime(currentTime);
        }
        if (GUILayout.Button("+1f > (コマ送り)", GUILayout.Height(22)))
        {
            PausePreview();
            currentTime = Mathf.Min(maxDuration, currentTime + (1f / TARGET_FPS));
            EnsureGraphValid();
            EvaluateAtTime(currentTime);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 3. メイン再生ボタン
        if (GUILayout.Button(isPlaying ? "停止 (一時停止)" : "再生テスト", GUILayout.Height(35)))
        {
            if (isPlaying) PausePreview();
            else StartPreview();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    private void SelectClipForScenePreview(AnimationClip clip)
    {
        selectedClip = clip;
        currentTime = 0f;
        RebuildPlayableGraph();

        // モデルが設定されていれば自動で再生を開始
        if (scenePreviewModel != null)
        {
            StartPreview();
        }
        else
        {
            EvaluateAtTime(0f);
        }

        Repaint();
    }

    private void StartPreview()
    {
        EnsureGraphValid();
        if (!playableGraph.IsValid()) return;

        isPlaying = true;
        lastEditorTime = EditorApplication.timeSinceStartup;

        playableGraph.Play();

        EditorApplication.update -= UpdatePreview;
        EditorApplication.update += UpdatePreview;
    }

    private void EnsureGraphValid()
    {
        if (!playableGraph.IsValid())
        {
            RebuildPlayableGraph();
        }
    }

    private void RebuildPlayableGraph()
    {
        StopPreview();

        if (scenePreviewModel == null || selectedClip == null) return;

        Animator animator = scenePreviewModel.GetComponent<Animator>();
        if (animator == null) return;

        playableGraph = PlayableGraph.Create("SearchPreviewGraph");
        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

        mixer = AnimationMixerPlayable.Create(playableGraph, 1);
        output.SetSourcePlayable(mixer);

        clipPlayable = AnimationClipPlayable.Create(playableGraph, selectedClip);
        playableGraph.Connect(clipPlayable, 0, mixer, 0);
        mixer.SetInputWeight(0, 1.0f);

        playableGraph.Stop();
    }

    private void UpdatePreview()
    {
        if (!isPlaying) return;

        double currentEditorTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentEditorTime - lastEditorTime);
        lastEditorTime = currentEditorTime;

        currentTime += deltaTime;

        // ループ再生処理
        if (selectedClip != null && selectedClip.length > 0f)
        {
            if (currentTime >= selectedClip.length)
            {
                currentTime %= selectedClip.length;
            }
        }

        EvaluateAtTime(currentTime);
    }

    private void EvaluateAtTime(float evalTime)
    {
        if (!playableGraph.IsValid() || selectedClip == null) return;

        float targetTime = Mathf.Clamp(evalTime, 0f, selectedClip.length);
        clipPlayable.SetTime(targetTime);

        playableGraph.Evaluate(0f);

        Repaint();
        SceneView.RepaintAll();
    }

    private void PausePreview()
    {
        isPlaying = false;
        EditorApplication.update -= UpdatePreview;
        if (playableGraph.IsValid())
        {
            playableGraph.Stop();
        }
        Repaint();
        SceneView.RepaintAll();
    }

    private void StopPreview()
    {
        PausePreview();
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnDisable()
    {
        StopPreview();
    }
}