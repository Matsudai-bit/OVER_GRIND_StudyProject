using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    public void DrawText(int number)
    {
        text.text = $"{number}";
    }
}
