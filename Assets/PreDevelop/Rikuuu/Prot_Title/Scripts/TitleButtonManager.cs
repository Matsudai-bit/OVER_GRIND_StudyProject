using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class TitleButtonManager : MonoBehaviour
{
    Vector3 pos = Vector3.zero;
    public void MoveCursor(
        UnityEngine.UI.Image cursor,             // �J�[�\��
        Vector3 targetPosition)     // �ڕW���W
    {
        if (pos == targetPosition)
        {
            cursor.DOFade(endValue: 1.0f, duration: 0.2f);
        }
        else if (pos != targetPosition)
        {
            cursor.DOFade(endValue: 0.0f, duration: 0.01f);

            pos = targetPosition;
        }

        cursor.transform.DOMove(targetPosition, 0.2f);

    }
}