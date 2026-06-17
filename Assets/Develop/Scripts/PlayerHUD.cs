using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] TMP_Text text;

    void Update()
    {
        text.text = $"{player.GetAmmo()}";
    }

}
