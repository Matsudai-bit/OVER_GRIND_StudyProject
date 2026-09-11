using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;
using UniRx;

public class PlayerHealthView : MonoBehaviour
{
    [Header("UIテキスト設定")]
    [SerializeField] private TMP_Text hpText;   //体力の描画用
    [SerializeField] private TMP_Text mpText;   //魔力の描画用
    [SerializeField] private TMP_Text goldText; //お金の描画用

    // ユーザー操作を通知するイベント
    public event Action OnDamageInput;      //体力のダメージ通知
    public event Action OnMagicInput;       //魔力の使用時の通知
    public event Action OnGainGoldInput;    //お金の使用通知

    //更新処理
    void Update()
    {
        // スペースキーでダメージ
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) OnDamageInput?.Invoke();
        // Mキーで魔法使用
        if (Keyboard.current?.mKey.wasPressedThisFrame == true) OnMagicInput?.Invoke();
        // Gキーでお金ゲット
        if (Keyboard.current?.gKey.wasPressedThisFrame == true) OnGainGoldInput?.Invoke();
    }

    // それぞれの数値をUIに反映するメソッド
    public void UpdateHPDisplay(int hp) => hpText.text = $"HP: {hp}";
    public void UpdateMPDisplay(int mp) => mpText.text = $"MP: {mp}";
    public void UpdateGoldDisplay(int gold) => goldText.text = $"Gold: {gold}";
}