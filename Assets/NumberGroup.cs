using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Common.Maths;
using MisterGames.Interact.Detectables;
using MisterGames.Interact.Interactives;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NumberGroup : MonoBehaviour {
    [SerializeField] private Interactive arrowUp;
    [SerializeField] private Interactive arrowDown;

    [SerializeField] private Text number;

    private int currentNumber = 0;

    public UnityEvent<int> numberSet;

    // Start is called before the first frame update
    void Start() {
        number.text = "";
        
        arrowUp.OnStartInteract += OnArrowUp;
        arrowDown.OnStartInteract += OnArrowDown;
    }

    private void OnDestroy() {
        arrowUp.OnStartInteract -= OnArrowUp;
        arrowDown.OnStartInteract -= OnArrowDown;
    }

    void OnArrowUp(IInteractiveUser user) {
        currentNumber++;
        setNumber();
    }

    void OnArrowDown(IInteractiveUser user) {
        currentNumber--;
        setNumber();
    }

    void setNumber() {
        currentNumber = currentNumber.Clamp(ClampMode.Full, 0, 9);
        number.text = currentNumber.ToString();
        
        numberSet.Invoke(currentNumber);
    }

    public void Disable() {
        arrowUp.GetComponent<Detectable>().enabled = false;
        arrowDown.GetComponent<Detectable>().enabled = false;
    }
}
