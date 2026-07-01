using UnityEngine;

public class EnemyData : MonoBehaviour
{
    [DebugParameterField]
    private int hp = 100;

    [DebugParameterField]
    private float speed = 5f;

    [DebugParameterField]
    private int attack = 30;
}