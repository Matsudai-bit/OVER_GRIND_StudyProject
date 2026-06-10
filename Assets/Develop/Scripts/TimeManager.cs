using UnityEngine;

public class TimeManager : MonoBehaviour
{
    const float TIME_LIMIT = 10.0f;

    [SerializeField] HUD timeText;
    float elapsedTime = 0;

    void Start()
    {
        // ŽžŠÔ‚Ì‰Šú‰»
        elapsedTime = TIME_LIMIT;
    }

    void Update()
    {
        // ŽžŠÔ‚ÌXV
        if (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
        }

        // •`‰æ
        timeText.DrawText((int)elapsedTime);
    }
}
