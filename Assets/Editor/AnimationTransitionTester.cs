using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;
using System.Linq;

public class AnimationTransitionTester : EditorWindow
{
    private const float TARGET_FPS = 60f; // 60fps固定

    private GameObject previewModel;

    // 遷移元 (Group A) と 遷移先 (Group B) のクリップリスト
    private List<AnimationClip> sequenceA = new List<AnimationClip>() { null };
    private List<AnimationClip> sequenceB = new List<AnimationClip>() { null };

    private int transitionFrames = 15; // クロスフェード時間（フレーム数）
    private bool loopSequenceA = false;
    private bool loopSequenceB = false;

    // FBXインポート設定
    private enum ImportTargetGroup { GroupA, GroupB }
    private ImportTargetGroup importTarget = ImportTargetGroup.GroupA;

    // Playable Graph 関連
    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mainMixer;
    private AnimationMixerPlayable mixerA;
    private AnimationMixerPlayable mixerB;
    private List<AnimationClipPlayable> playablesA = new List<AnimationClipPlayable>();
    private List<AnimationClipPlayable> playablesB = new List<AnimationClipPlayable>();

    private List<AnimationClip> validClipsA = new List<AnimationClip>();
    private List<AnimationClip> validClipsB = new List<AnimationClip>();

    private bool isPlaying = false;
    private float currentTime = 0f;
    private float totalDurationA = 0f;
    private float totalDurationB = 0f;
    private double lastEditorTime = 0;

    // GUI表示用
    private string activeClipNameA = "-";
    private string activeClipNameB = "-";
    private float currentWeightB = 0f;

    private float TransitionDurationSeconds => transitionFrames / TARGET_FPS;

    [MenuItem("Tools/Animation Transition Tester")]
    public static void ShowWindow()
    {
        GetWindow<AnimationTransitionTester>("Transition Tester");
    }

    private void OnGUI()
    {
        GUILayout.Label("アニメーション繋ぎ（遷移）プレビューツール", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 0. FBXドラッグ＆ドロップ自動解析エリア
        DrawFBXDropArea();
        EditorGUILayout.Space();

        // 1. 対象モデルの選択
        previewModel = (GameObject)EditorGUILayout.ObjectField("プレビュー用モデル", previewModel, typeof(GameObject), true);
        EditorGUILayout.Space();

        // 2. 遷移前（Group A）のシーケンス設定
        DrawClipSequenceGroup("再生モデル / 遷移前 (Group A)", sequenceA);
        loopSequenceA = EditorGUILayout.ToggleLeft("Group A をループ再生 (※単体確認用)", loopSequenceA);

        EditorGUILayout.Space();

        // 3. 遷移後（Group B）のシーケンス設定
        DrawClipSequenceGroup("遷移後 (Group B) ※空の場合はGroup Aのみ再生", sequenceB);
        loopSequenceB = EditorGUILayout.ToggleLeft("Group B をループ再生", loopSequenceB);

        EditorGUILayout.Space();

        // 4. 遷移設定 (フレーム単位)
        transitionFrames = EditorGUILayout.IntSlider("クロスフェード時間 (Frame)", transitionFrames, 0, 60);

        EditorGUILayout.Space();

        // 5. リアルタイム再生ステータス ＆ シークバー（フレーム調整機能付き）
        DrawPlaybackStatusAndSeekerGUI();

        EditorGUILayout.Space();

        // 6. メイン再生コントロール
        bool hasValidA = GetValidClips(sequenceA).Count > 0;
        EditorGUI.BeginDisabledGroup(previewModel == null || !hasValidA);

        if (GUILayout.Button(isPlaying ? "停止 (一時停止)" : "再生テスト", GUILayout.Height(35)))
        {
            if (isPlaying) PausePreview();
            else StartPreview();
        }

        EditorGUI.EndDisabledGroup();
    }

    // FBX一括ドラッグ＆ドロップエリア
    private void DrawFBXDropArea()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("FBXインポート先:", GUILayout.Width(110));
        importTarget = (ImportTargetGroup)EditorGUILayout.EnumPopup(importTarget);
        EditorGUILayout.EndHorizontal();

        Event evt = Event.current;
        Rect dropRect = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, $"【FBXドラッグ＆ドロップエリア】\nドロップされた全クリップを {importTarget} へ順番に配置します", EditorStyles.centeredGreyMiniLabel);

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropRect.Contains(evt.mousePosition)) break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    ProcessDroppedFBXObjects(DragAndDrop.objectReferences);
                }
                evt.Use();
                break;
        }

        EditorGUILayout.EndVertical();
    }

    private void ProcessDroppedFBXObjects(Object[] objects)
    {
        List<AnimationClip> extractedClips = new List<AnimationClip>();

        foreach (var obj in objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    extractedClips.Add(clip);
                }
            }
        }

        if (extractedClips.Count == 0)
        {
            Debug.LogWarning("ドロップされたアセット内に AnimationClip が見つかりませんでした。");
            return;
        }

        extractedClips = extractedClips.OrderBy(c =>
        {
            if (c.name.EndsWith("_I")) return 0;
            if (c.name.EndsWith("_L")) return 1;
            if (c.name.EndsWith("_O")) return 2;
            return 3;
        }).ThenBy(c => c.name).ToList();

        if (importTarget == ImportTargetGroup.GroupA)
        {
            sequenceA = extractedClips;
        }
        else
        {
            sequenceB = extractedClips;
        }

        PausePreview();
        RebuildPlayableGraph();
        currentTime = 0f;
        EvaluateAtTime(0f);
        Debug.Log($"FBX解析完了: {extractedClips.Count} 個のクリップを {importTarget} に配置しました。");
        Repaint();
    }

    private void DrawClipSequenceGroup(string label, List<AnimationClip> sequence)
    {
        GUILayout.Label(label, EditorStyles.boldLabel);

        int indexToRemove = -1;
        for (int i = 0; i < sequence.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sequence[i] = (AnimationClip)EditorGUILayout.ObjectField($"Clip {i + 1}", sequence[i], typeof(AnimationClip), false);

            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                indexToRemove = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (indexToRemove >= 0)
        {
            sequence.RemoveAt(indexToRemove);
        }

        if (GUILayout.Button("+ クリップを追加"))
        {
            sequence.Add(null);
        }
    }

    // ステータス表示・フレーム直接入力・シークバーGUI
    private void DrawPlaybackStatusAndSeekerGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("【リアルタイム再生ステータス (60fps換算)】", EditorStyles.boldLabel);

        float maxDuration = GetTotalPreviewLength();
        int currentFrame = Mathf.RoundToInt(currentTime * TARGET_FPS);
        int totalFrames = Mathf.RoundToInt(maxDuration * TARGET_FPS);

        EditorGUILayout.LabelField("全体フレーム / 全体時間:", $"F {totalFrames}  ({maxDuration:F2}s)");

        // 1. シークバーとフレーム数直接入力（相互連動）
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

        EditorGUILayout.LabelField("Group A 現在クリップ:", activeClipNameA);

        if (validClipsB.Count > 0)
        {
            EditorGUILayout.LabelField("Group B 現在クリップ:", activeClipNameB);
            EditorGUILayout.LabelField("遷移ブレンド率 (Group B):", $"{currentWeightB * 100f:F0} %");
        }
        else
        {
            EditorGUILayout.LabelField("動作モード:", "単体再生モード");
        }

        EditorGUILayout.EndVertical();
    }

    private float GetTotalPreviewLength()
    {
        var clipsA = GetValidClips(sequenceA);
        var clipsB = GetValidClips(sequenceB);

        float lenA = GetSequenceLength(clipsA);
        if (clipsB.Count == 0) return lenA;

        float transitionStartTime = Mathf.Max(0f, lenA - TransitionDurationSeconds);
        return transitionStartTime + GetSequenceLength(clipsB);
    }

    private List<AnimationClip> GetValidClips(List<AnimationClip> rawList)
    {
        List<AnimationClip> valid = new List<AnimationClip>();
        foreach (var c in rawList)
        {
            if (c != null) valid.Add(c);
        }
        return valid;
    }

    private void StartPreview()
    {
        RebuildPlayableGraph();
        if (!playableGraph.IsValid()) return;

        currentTime = 0f;
        EvaluateAtTime(0f);

        playableGraph.Play();
        isPlaying = true;
        lastEditorTime = EditorApplication.timeSinceStartup;

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

        Animator animator = previewModel != null ? previewModel.GetComponent<Animator>() : null;
        if (animator == null) return;

        validClipsA = GetValidClips(sequenceA);
        validClipsB = GetValidClips(sequenceB);

        totalDurationA = GetSequenceLength(validClipsA);
        totalDurationB = GetSequenceLength(validClipsB);

        if (totalDurationA <= 0f) return;

        playableGraph = PlayableGraph.Create("TransitionTesterGraph");
        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

        mainMixer = AnimationMixerPlayable.Create(playableGraph, 2);
        output.SetSourcePlayable(mainMixer);

        // Group A Mixer 構築
        mixerA = AnimationMixerPlayable.Create(playableGraph, validClipsA.Count);
        playablesA.Clear();
        for (int i = 0; i < validClipsA.Count; i++)
        {
            var cp = AnimationClipPlayable.Create(playableGraph, validClipsA[i]);
            playableGraph.Connect(cp, 0, mixerA, i);
            playablesA.Add(cp);
        }
        playableGraph.Connect(mixerA, 0, mainMixer, 0);

        // Group B Mixer 構築
        if (validClipsB.Count > 0)
        {
            mixerB = AnimationMixerPlayable.Create(playableGraph, validClipsB.Count);
            playablesB.Clear();
            for (int i = 0; i < validClipsB.Count; i++)
            {
                var cp = AnimationClipPlayable.Create(playableGraph, validClipsB[i]);
                playableGraph.Connect(cp, 0, mixerB, i);
                playablesB.Add(cp);
            }
            playableGraph.Connect(mixerB, 0, mainMixer, 1);
        }

        mainMixer.SetInputWeight(0, 1.0f);
        mainMixer.SetInputWeight(1, 0.0f);

        playableGraph.Stop();
    }

    private void UpdatePreview()
    {
        if (!isPlaying) return;

        double currentEditorTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentEditorTime - lastEditorTime);
        lastEditorTime = currentEditorTime;

        currentTime += deltaTime;

        float maxDuration = GetTotalPreviewLength();

        // ループ制御
        if (validClipsB.Count == 0)
        {
            if (loopSequenceA && currentTime >= totalDurationA)
            {
                currentTime %= totalDurationA;
            }
            else if (!loopSequenceA && currentTime >= totalDurationA)
            {
                currentTime = totalDurationA;
                PausePreview();
            }
        }
        else
        {
            float transitionStartTime = Mathf.Max(0f, totalDurationA - TransitionDurationSeconds);

            if (loopSequenceB && currentTime >= maxDuration)
            {
                float elapsedInB = currentTime - transitionStartTime;
                currentTime = transitionStartTime + (elapsedInB % totalDurationB);
            }
            else if (!loopSequenceB && currentTime >= maxDuration)
            {
                currentTime = maxDuration;
                PausePreview();
            }
        }

        EvaluateAtTime(currentTime);
    }

    private void EvaluateAtTime(float evalTime)
    {
        if (!playableGraph.IsValid()) return;

        // 1. 単体再生モード (Group B が空の場合)
        if (validClipsB.Count == 0)
        {
            float targetTimeA = loopSequenceA ? (evalTime % totalDurationA) : Mathf.Min(evalTime, totalDurationA);
            UpdateSequenceGroup(mixerA, playablesA, validClipsA, targetTimeA, out activeClipNameA);

            mainMixer.SetInputWeight(0, 1.0f);
            mainMixer.SetInputWeight(1, 0.0f);
            currentWeightB = 0f;
        }
        else
        {
            // 2. 遷移再生モード (Group A -> Group B)
            float targetEvalA = Mathf.Min(evalTime, totalDurationA);
            UpdateSequenceGroup(mixerA, playablesA, validClipsA, targetEvalA, out activeClipNameA);

            float transitionStartTime = Mathf.Max(0f, totalDurationA - TransitionDurationSeconds);
            currentWeightB = 0f;

            if (evalTime >= transitionStartTime)
            {
                float fadeDuration = Mathf.Max(0.001f, TransitionDurationSeconds);
                currentWeightB = Mathf.Clamp01((evalTime - transitionStartTime) / fadeDuration);

                float elapsedB = evalTime - transitionStartTime;
                float targetTimeB = loopSequenceB ? (elapsedB % totalDurationB) : Mathf.Min(elapsedB, totalDurationB);

                UpdateSequenceGroup(mixerB, playablesB, validClipsB, targetTimeB, out activeClipNameB);
            }
            else
            {
                activeClipNameB = "(待機中)";
            }

            mainMixer.SetInputWeight(0, 1.0f - currentWeightB);
            mainMixer.SetInputWeight(1, currentWeightB);
        }

        playableGraph.Evaluate(0f);

        Repaint();
        SceneView.RepaintAll();
    }

    private void UpdateSequenceGroup(AnimationMixerPlayable mixer, List<AnimationClipPlayable> playables, List<AnimationClip> clips, float targetTime, out string activeClipName)
    {
        float accumulated = 0f;
        activeClipName = "-";

        for (int i = 0; i < clips.Count; i++)
        {
            float clipLen = clips[i].length;

            if (targetTime >= accumulated && (targetTime < accumulated + clipLen || i == clips.Count - 1))
            {
                mixer.SetInputWeight(i, 1.0f);
                float localTime = Mathf.Clamp(targetTime - accumulated, 0f, clipLen);
                playables[i].SetTime(localTime);

                int frame = Mathf.RoundToInt(localTime * TARGET_FPS);
                int totalFrame = Mathf.RoundToInt(clipLen * TARGET_FPS);
                activeClipName = $"{clips[i].name} (F {frame} / F {totalFrame})";
            }
            else
            {
                mixer.SetInputWeight(i, 0.0f);
            }
            accumulated += clipLen;
        }
    }

    private float GetSequenceLength(List<AnimationClip> clips)
    {
        float len = 0f;
        foreach (var c in clips) len += c.length;
        return len;
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
        playablesA.Clear();
        playablesB.Clear();

        activeClipNameA = "-";
        activeClipNameB = "-";
        currentWeightB = 0f;

        Repaint();
        SceneView.RepaintAll();
    }

    private void OnDisable()
    {
        StopPreview();
    }
}