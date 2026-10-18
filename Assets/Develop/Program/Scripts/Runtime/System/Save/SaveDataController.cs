using UnityEngine;

/// <summary>
/// ランタイム中のセーブデータを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class SaveDataController : MonoBehaviour
{
    [SerializeField, Header("セーブデータ")]
    /// <summary>初期データとして使用する SaveData Asset</summary>
    private SaveData m_defaultSaveData;

    [SerializeField]
    /// <summary>保存ファイル名</summary>
    private string m_saveFileName = SaveSystem.DEFAULT_SAVE_FILE_NAME;

    [SerializeField, Header("ランタイム確認用")]
    /// <summary>実行時に使用する SaveData</summary>
    private SaveData m_runtimeSaveData;

    [SerializeField, Header("イベント")]
    /// <summary>セーブ要求イベント</summary>
    private SaveRequestEvent m_saveRequestEvent;

    [SerializeField]
    /// <summary>セーブ状態変更イベント</summary>
    private SaveChangedEvent m_saveChangedEvent;

    /// <summary>未保存の変更があるかどうか</summary>
    private bool m_isDirty = false;

    /// <summary>初期化済みかどうか</summary>
    private bool m_isInitialized = false;

    /// <summary>JSON からロードしたかどうか</summary>
    private bool m_hasLoadedSaveFile = false;

    /// <summary>
    /// 現在使用中のセーブデータを取得します。
    /// </summary>
    public SaveData CurrentData => m_runtimeSaveData;

    /// <summary>
    /// ゲーム進行データを取得します。
    /// </summary>
    public GameProgressSaveData Progress => m_runtimeSaveData != null ? m_runtimeSaveData.Progress : null;

    /// <summary>
    /// ゲーム設定データを取得します。
    /// </summary>
    public GameSettingsSaveData Settings => m_runtimeSaveData != null ? m_runtimeSaveData.Settings : null;

    /// <summary>
    /// セーブファイル名を取得します。
    /// </summary>
    public string SaveFileName => m_saveFileName;

    /// <summary>
    /// 未保存の変更が存在するか取得します。
    /// </summary>
    public bool IsDirty => m_isDirty;

    private void Awake()
    {
        InitializeSaveData();
    }

    private void OnEnable()
    {
        if (m_saveRequestEvent != null)
        {
            m_saveRequestEvent.RegisterListener(OnSaveRequested);
        }
    }

    private void Start()
    {
        if (!m_isInitialized)
        {
            return;
        }

        RaiseSaveChangedEvent(m_hasLoadedSaveFile ? SaveChangedEventType.LOADED : SaveChangedEventType.INITIALIZED);
    }

    private void OnDisable()
    {
        if (m_saveRequestEvent != null)
        {
            m_saveRequestEvent.UnregisterListener(OnSaveRequested);
        }
    }

    private void OnDestroy()
    {
        if (m_runtimeSaveData == null)
        {
            return;
        }

        Destroy(m_runtimeSaveData);
        m_runtimeSaveData = null;
    }

    /// <summary>
    /// 現在のデータを保存します。
    /// </summary>
    /// <returns>
    /// true：保存に成功しました。
    /// false：保存に失敗しました。
    /// </returns>
    public bool Save()
    {
        if (!EnsureInitialized())
        {
            return false;
        }

        if (!SaveSystem.TrySave(m_runtimeSaveData, m_saveFileName))
        {
            return false;
        }

        m_isDirty = false;
        RaiseSaveChangedEvent(SaveChangedEventType.SAVED);

        return true;
    }

    /// <summary>
    /// 未保存の変更が存在する場合のみ保存します。
    /// </summary>
    /// <returns>
    /// true：保存に成功した、または保存の必要がありませんでした。
    /// false：保存に失敗しました。
    /// </returns>
    public bool SaveIfDirty()
    {
        if (!m_isDirty)
        {
            return true;
        }

        return Save();
    }

    /// <summary>
    /// ステージクリア情報を更新し、自動保存します。
    /// </summary>
    /// <param name="stageNumber">クリアしたステージ番号。</param>
    /// <returns>
    /// true：更新と保存に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool MarkStageCleared(int stageNumber)
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Progress.MarkStageCleared(stageNumber))
        {
            return false;
        }

        MarkDirty();
        return Save();
    }

    /// <summary>
    /// OPムービーを視聴済みにし、自動保存します。
    /// </summary>
    /// <returns>
    /// true：更新と保存に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool MarkOpeningWatched()
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Progress.MarkOpeningWatched())
        {
            return false;
        }

        MarkDirty();
        return Save();
    }

    /// <summary>
    /// BGM音量を更新します。
    /// </summary>
    /// <param name="volume">設定するBGM音量。</param>
    /// <returns>
    /// true：更新に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool SetBgmVolume(float volume)
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Settings.SetBgmVolume(volume))
        {
            return false;
        }

        MarkDirty();
        return true;
    }

    /// <summary>
    /// SE音量を更新します。
    /// </summary>
    /// <param name="volume">設定するSE音量。</param>
    /// <returns>
    /// true：更新に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool SetSeVolume(float volume)
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Settings.SetSeVolume(volume))
        {
            return false;
        }

        MarkDirty();
        return true;
    }

    /// <summary>
    /// マスター音量を更新します。
    /// </summary>
    /// <param name="volume">設定するマスター音量。</param>
    /// <returns>
    /// true：更新に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool SetMasterVolume(float volume)
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Settings.SetMasterVolume(volume))
        {
            return false;
        }

        MarkDirty();
        return true;
    }

    /// <summary>
    /// 画面サイズ設定を更新します。
    /// </summary>
    /// <param name="screenSize">設定する画面サイズのインデックス。</param>
    /// <returns>
    /// true：更新に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool SetScreenSize(int screenSize)
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Settings.SetScreenSize(screenSize))
        {
            return false;
        }

        MarkDirty();
        return true;
    }

    /// <summary>
    /// カメラ感度を更新します。
    /// </summary>
    /// <param name="sensitivity">設定するカメラ感度。</param>
    /// <returns>
    /// true：更新に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool SetCameraSensitivity(float sensitivity)
    {
        if (!EnsureInitialized()) return false;

        if (!m_runtimeSaveData.Settings.SetCameraSensitivity(sensitivity))
        {
            return false;
        }

        MarkDirty();
        return true;
    }
    /// <summary>
    /// カメラの反転設定を更新します。
    /// </summary>
    /// <param name="isReverse">反転させるかどうか。</param>
    /// <returns>
    /// true：更新に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool SetCameraReverse(bool isReverse)
    {
        if (!EnsureInitialized()) return false;

        // 直接代入するのではなく、設定クラス側のメソッドを呼び出す
        if (!m_runtimeSaveData.Settings.SetCameraReverse(isReverse))
        {
            return false;
        }

        MarkDirty();
        return true;
    }

    /// <summary>
    /// ランタイムデータを初期データへ戻します。
    /// </summary>
    /// <param name="saveImmediately">リセット後すぐに保存を行うか。</param>
    /// <returns>
    /// true：リセット処理に成功しました。
    /// false：初期化エラーなどで失敗しました。
    /// </returns>
    public bool ResetSaveData(bool saveImmediately = true)
    {
        if (!EnsureInitialized()) return false;

        string defaultJson = JsonUtility.ToJson(m_defaultSaveData);
        JsonUtility.FromJsonOverwrite(defaultJson, m_runtimeSaveData);

        m_runtimeSaveData.EnsureInitialized();

        MarkDirty();
        RaiseSaveChangedEvent(SaveChangedEventType.RESET);

        if (!saveImmediately)
        {
            return true;
        }

        return Save();
    }

    /// <summary>
    /// セーブデータを初期化します。
    /// </summary>
    /// <returns>
    /// true：初期化に成功しました。
    /// false：初期化に失敗しました。
    /// </returns>
    private bool InitializeSaveData()
    {
        if (m_isInitialized) return true;

        if (m_defaultSaveData == null)
        {
            Debug.LogError($"{nameof(SaveData)} が設定されていません。", this);
            return false;
        }

        // 元Assetを変更しないようランタイム用に複製
        m_runtimeSaveData = Instantiate(m_defaultSaveData);
        m_runtimeSaveData.name = $"{m_defaultSaveData.name}_Runtime";
        m_runtimeSaveData.EnsureInitialized();

        // JSONがあればランタイムデータへ上書き
        m_hasLoadedSaveFile = SaveSystem.TryLoadOverwrite(m_runtimeSaveData, m_saveFileName);

        m_isDirty = false;
        m_isInitialized = true;

        return true;
    }

    /// <summary>
    /// 必要に応じて初期化します。
    /// </summary>
    /// <returns>
    /// true：初期化済み、または初期化に成功しました。
    /// false：初期化に失敗しました。
    /// </returns>
    private bool EnsureInitialized()
    {
        if (m_isInitialized) return true;

        return InitializeSaveData();
    }

    /// <summary>
    /// 未保存状態にします。
    /// </summary>
    private void MarkDirty()
    {
        m_isDirty = true;
    }

    /// <summary>
    /// セーブ要求を処理します。
    /// </summary>
    private void OnSaveRequested()
    {
        SaveIfDirty();
    }

    /// <summary>
    /// セーブ状態変更を通知します。
    /// </summary>
    /// <param name="eventType">発生したイベントのタイプ。</param>
    private void RaiseSaveChangedEvent(SaveChangedEventType eventType)
    {
        if (m_saveChangedEvent == null) return;

        SaveChangedEventData eventData = new SaveChangedEventData(eventType, m_isDirty);
        m_saveChangedEvent.Raise(eventData);
    }
}