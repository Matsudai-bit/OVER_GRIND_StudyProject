using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 既にインスタンスが存在する場合（重複して作られそうになった場合）
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // このインスタンスを唯一のものとして設定
        _instance = this as T;

        // シーンが切り替わっても破棄されないように設定
        DontDestroyOnLoad(gameObject);

        // 初期化処理
        Init();
    }

    // 初期化処理をオーバーライドできるように仮想メソッドとして定義
    protected virtual void Init()
    {
        // 子クラスで初期化処理を記述
    }
}
