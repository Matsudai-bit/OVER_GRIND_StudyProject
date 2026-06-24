using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [DebugParameterField]
    private int hp = 100;

    [DebugParameterField]
    private float speed = 5f;

    [DebugParameterField]
    private int attack = 30;

    [DebugParameterField]
    private bool isDead = false;

    [DebugParameterField]
    private string playerName = "Hero";

    [DebugParameterField]
    Vector3 vectorrrrrr = Vector3.zero;
}