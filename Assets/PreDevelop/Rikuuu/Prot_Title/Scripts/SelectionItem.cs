using System;
using UnityEngine;

public class SelectionItem : MonoBehaviour
{
    // カーソルが乗ったら
    public virtual void OnCursor()
    {
        Debug.Log("OnCursor");
    }

    // カーソルが離れたら
    public virtual void OnExit()
    {
        Debug.Log("OnExit");
    }

    // クリックされたら
    public virtual void OnClick()
    {
        Debug.Log("OnClick");
    }
}
