using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogVariant : MonoBehaviour
{

    [NonSerialized]public UnityEvent<int> pressed = new UnityEvent<int>();

    [NonSerialized]public int id;

    public void OnPress()
    {
        pressed.Invoke(id);
    }
}
