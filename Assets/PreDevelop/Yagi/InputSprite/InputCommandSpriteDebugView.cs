using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 入力中のコマンドに対応する画像を表示するデバッグ用UIです。
/// </summary>
[DisallowMultipleComponent]
public sealed class InputCommandSpriteDebugView : MonoBehaviour
{
    [Serializable]
    private sealed class CommandBinding
    {
        [SerializeField, Header("入力アクション")]
        private InputActionReference m_actionReference;

        [SerializeField, Header("InputAction名（参照未設定時に使用）")]
        private string m_actionName;

        [SerializeField, Header("コマンドDataアセット名")]
        private string m_commandAssetName;

        public InputAction Action => m_actionReference != null
            ? m_actionReference.action
            : null;

        public InputAction ResolveAction(InputActionAsset inputActionsAsset)
        {
            if (m_actionReference != null)
            {
                return m_actionReference.action;
            }

            if (inputActionsAsset == null || string.IsNullOrWhiteSpace(m_actionName))
            {
                return null;
            }

            return inputActionsAsset.FindAction($"Player/{m_actionName}");
        }

        public string CommandAssetName => m_commandAssetName;

        /// <summary>
        /// InputAction名とコマンドData名から対応設定を作成します。
        /// </summary>
        /// <param name="actionName">Playerマップ内のInputAction名。</param>
        /// <param name="commandAssetName">コマンドDataアセット名。</param>
        /// <returns>作成した対応設定。</returns>
        public static CommandBinding Create(
            string actionName,
            string commandAssetName)
        {
            return new CommandBinding
            {
                m_actionName = actionName,
                m_commandAssetName = commandAssetName
            };
        }
    }

    [SerializeField, Header("入力画像Manager")]
    private InputCommandSpriteManager m_spriteManager;

    [SerializeField, Header("InputActionAsset")]
    private InputActionAsset m_inputActionsAsset;

    [SerializeField, Header("入力デバイス切替")]
    private InputDeviceTypeSwitcher m_deviceTypeSwitcher;

    [SerializeField, Header("表示先Image")]
    private Image m_targetImage;

    [SerializeField, Header("入力アクションとコマンドの対応")]
    private List<CommandBinding> m_commandBindings = new();

    [SerializeField, Header("デバッグUI有効時に未有効のActionを有効化")]
    private bool m_enableActions = true;

    private readonly List<InputAction> m_actionsEnabledByThisView = new();
    private CommandBinding m_currentBinding;

    /// <summary>
    /// 現在のInputSpriteManagerに対応する初期リストを作成します。
    /// </summary>
    private void Reset()
    {
        CreateDefaultCommandBindings();
    }

    /// <summary>
    /// 現在のInputSpriteManagerに対応する入力Action一覧を再生成します。
    /// </summary>
    [ContextMenu("InputSpriteManager対応リストを再生成")]
    private void CreateDefaultCommandBindings()
    {
        m_commandBindings = new List<CommandBinding>
        {
            CommandBinding.Create("Move", "Move"),
            CommandBinding.Create("Attack", "Attack"),
            CommandBinding.Create("Charge", "Charge"),
            CommandBinding.Create("Jump", "Jump"),
            CommandBinding.Create("Target", "Target"),
            CommandBinding.Create("TargetLeft", "TargetChangeLeft"),
            CommandBinding.Create("TargetRight", "TargetChangeRight"),
            CommandBinding.Create("Look", "CameraMove"),
            CommandBinding.Create(string.Empty, "Pause")
        };
    }

    /// <summary>
    /// 入力アクションを購読してデバッグUIを初期化します。
    /// </summary>
    private void OnEnable()
    {
        if (m_targetImage != null)
        {
            m_targetImage.enabled = false;
        }

        if (m_deviceTypeSwitcher != null)
        {
            m_deviceTypeSwitcher.InputDeviceTypeChanged += OnInputDeviceTypeChanged;
        }

        foreach (CommandBinding binding in m_commandBindings)
        {
            InputAction inputAction = binding?.ResolveAction(m_inputActionsAsset);
            if (inputAction == null)
            {
                continue;
            }

            inputAction.started += OnActionStarted;
            inputAction.canceled += OnActionCanceled;

            if (m_enableActions && !inputAction.enabled)
            {
                inputAction.Enable();
                m_actionsEnabledByThisView.Add(inputAction);
            }
        }
    }

    /// <summary>
    /// 入力アクションの購読を解除してデバッグUIを停止します。
    /// </summary>
    private void OnDisable()
    {
        foreach (CommandBinding binding in m_commandBindings)
        {
            InputAction inputAction = binding?.ResolveAction(m_inputActionsAsset);
            if (inputAction == null)
            {
                continue;
            }

            inputAction.started -= OnActionStarted;
            inputAction.canceled -= OnActionCanceled;
        }

        foreach (InputAction inputAction in m_actionsEnabledByThisView)
        {
            inputAction.Disable();
        }

        m_actionsEnabledByThisView.Clear();
        m_currentBinding = null;

        if (m_deviceTypeSwitcher != null)
        {
            m_deviceTypeSwitcher.InputDeviceTypeChanged -= OnInputDeviceTypeChanged;
        }

        HideImage();
    }

    /// <summary>
    /// 入力アクション開始時に対応するコマンド画像を表示します。
    /// </summary>
    /// <param name="context">入力アクションのコールバック情報。</param>
    private void OnActionStarted(InputAction.CallbackContext context)
    {
        CommandBinding binding = FindBinding(context.action);
        if (binding == null)
        {
            return;
        }

        m_currentBinding = binding;
        ShowCommandImage(binding.CommandAssetName);
    }

    /// <summary>
    /// 入力アクション終了時に表示中のコマンド画像を非表示にします。
    /// </summary>
    /// <param name="context">入力アクションのコールバック情報。</param>
    private void OnActionCanceled(InputAction.CallbackContext context)
    {
        CommandBinding binding = FindBinding(context.action);
        if (binding == null || m_currentBinding != binding)
        {
            return;
        }

        m_currentBinding = null;
        HideImage();
    }

    /// <summary>
    /// 入力デバイス切替時に表示中の画像を再取得します。
    /// </summary>
    /// <param name="inputDeviceType">新しい入力デバイス。</param>
    private void OnInputDeviceTypeChanged(InputDeviceType inputDeviceType)
    {
        if (m_currentBinding != null)
        {
            ShowCommandImage(m_currentBinding.CommandAssetName);
        }
    }

    /// <summary>
    /// 指定されたInputActionに対応する設定を検索します。
    /// </summary>
    /// <param name="inputAction">検索対象のInputAction。</param>
    /// <returns>対応する設定。見つからない場合はnull。</returns>
    private CommandBinding FindBinding(InputAction inputAction)
    {
        foreach (CommandBinding binding in m_commandBindings)
        {
            if (binding?.ResolveAction(m_inputActionsAsset) == inputAction)
            {
                return binding;
            }
        }

        return null;
    }

    /// <summary>
    /// 指定したコマンド画像を表示します。
    /// </summary>
    /// <param name="commandAssetName">表示するコマンドDataアセット名。</param>
    private void ShowCommandImage(string commandAssetName)
    {
        if (m_targetImage == null || m_spriteManager == null)
        {
            return;
        }

        Sprite sprite = m_spriteManager.GetSprite(commandAssetName);
        m_targetImage.sprite = sprite;
        m_targetImage.preserveAspect = true;
        m_targetImage.enabled = sprite != null;
    }

    /// <summary>
    /// デバッグ画像を非表示にします。
    /// </summary>
    private void HideImage()
    {
        if (m_targetImage != null)
        {
            m_targetImage.enabled = false;
        }
    }
}
