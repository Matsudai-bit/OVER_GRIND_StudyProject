using UnityEngine;
using TMPro;

public class ValueTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI paramNameText;
    [SerializeField] private TextMeshProUGUI valueText;

    public void Render(string paramName, string valueString)
    {
        if (paramNameText != null) paramNameText.text = paramName;
        if (valueText != null) valueText.text = valueString;
    }
}