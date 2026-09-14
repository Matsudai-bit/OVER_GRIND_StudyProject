using UnityEngine;

public class RightContainerArrow : MonoBehaviour
{
    public void MoveCursor(
    UnityEngine.UI.Image cursor,
    Vector3 targetPosition)
    {
        Vector3 result = cursor.transform.position;
        result.y = targetPosition.y;

        cursor.transform.position = result;
    }
}
