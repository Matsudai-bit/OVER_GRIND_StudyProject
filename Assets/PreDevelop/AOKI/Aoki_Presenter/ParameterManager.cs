using System;
using System.Collections.Generic;
using UnityEngine;

public class ParameterManager : MonoBehaviour
{
    // インスペクターで設定する項目
    [Serializable]
    public struct ParameterConfig
    {
        public string name;      // "HP", "スタミナ", "Vゲージ"
        public int initialValue; // 初期値
        public int maxValue;     // 最大値

        // ★追加：個別に生成先の箱を指定できるようにする
        [Header("生成先の箱 (空欄なら下のContainerへ)")]
        public Transform customContainer;
    }

    [Header("★ ここにリスト形式で追加するだけ！")]
    [SerializeField] private List<ParameterConfig> parameterConfigs = new List<ParameterConfig>();

    [Header("UI自動生成用の設定")]
    [SerializeField] private ValueTextView viewPrefab; // UIのプレハブ
    [SerializeField] private Transform viewContainer;  // UIを並べる親オブジェクト

    // 生成されたModelを名前で検索して操作するための辞書
    private Dictionary<string, ParameterModel> _models = new Dictionary<string, ParameterModel>();
    private List<ISubPresenter> _subPresenters = new List<ISubPresenter>();

    private void Start()
    {
        // リストに書いた設定の数だけ、Model・View・Presenterを全自動生成
        foreach (var config in parameterConfigs)
        {
            // 1. Modelを自動生成
            var model = new ParameterModel(config.name, config.initialValue, config.maxValue);
            _models[config.name] = model;

            Transform targetParent = config.customContainer != null ? config.customContainer : viewContainer;
            ValueTextView viewInstance = Instantiate(viewPrefab, targetParent);

            // 3. SubPresenterを作って結合
            var presenter = new ParameterSubPresenter(model, viewInstance);
            presenter.Bind();
            _subPresenters.Add(presenter);
        }
    }

    // テスト操作用
    private void Update()
    {
        // 最新のInput Systemでのスペースキー判定
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // スペースキーを押すと「HP」が1減る
            ChangeValue("HP", GetValue("HP") - 1);

            // 同時に「Gauge」が2増える
            ChangeValue("Gauge", GetValue("Gauge") + 2);
        }
    }

    // 名前で値を変更するための便利メソッド
    public void ChangeValue(string paramName, int newValue)
    {
        if (_models.TryGetValue(paramName, out var model))
        {
            model.SetValue(newValue);
        }
    }

    // 名前で現在の値を取得するための便利メソッド
    public int GetValue(string paramName)
    {
        if (_models.TryGetValue(paramName, out var model))
        {
            return model.CurrentValue;
        }
        return 0;
    }

    private void OnDestroy()
    {
        foreach (var presenter in _subPresenters)
        {
            presenter.Unbind();
        }
    }
}