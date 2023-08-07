using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBook : MonoBehaviour {
    [SerializeField] private GameObject book;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            book.SetActive(!book.activeSelf);
        }
    }
}
