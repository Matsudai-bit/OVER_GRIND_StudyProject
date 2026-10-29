using UnityEngine;

public class GameOverButtonManager : MonoBehaviour
{
    // —h‚ê•(ƒsƒNƒZƒ‹)
    [SerializeField] 
    private float m_swayAmplitude = 2.5f;
    // —h‚ê‚Ì‘¬‚³
    [SerializeField] 
    private float m_swaySpeed = 5f;

    // ‘OƒtƒŒ[ƒ€‚Å‰Á‚¦‚½—h‚ê—Ê
    private float prevOffset = 0;

    public void MoveCursor(
        UnityEngine.UI.Image cursor,
        Vector3 targetPosition)
    {
        Vector3 position = cursor.transform.position;

        // ‰¡—h‚ê
        float offset = Mathf.Sin(Time.unscaledTime * m_swaySpeed) * m_swayAmplitude;
        position.x += offset - prevOffset;
        prevOffset = offset;

        // c‚ÌˆÚ“®
        position.y = targetPosition.y;

        cursor.transform.position = position;
    }
}
