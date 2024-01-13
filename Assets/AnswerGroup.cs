using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
using MisterGames.Interact.Detectables;
using MisterGames.Interact.Interactives;
using UnityEngine;
using UnityEngine.Events;

public class AnswerGroup : MonoBehaviour {
    [SerializeField] private NumberGroup firstNumber;
    [SerializeField] private NumberGroup secondNumber;
    [SerializeField] private GameObject check;

    private int firstValue = 0;
    private int secondValue = 0;
    private int totalValue = 0;

    public UnityEvent answered;
    public UnityEvent numberSelected;
    [SerializeField] private int answer = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        firstNumber.numberSet.AddListener(OnFirstNumberSet);
        secondNumber.numberSet.AddListener(OnSecondNumberSet);
    }

    void OnFirstNumberSet(int num) {
        firstValue = num;
        UpdateValue();
    }

    void OnSecondNumberSet(int num) {
        secondValue = num;
        UpdateValue();
    }
    
    void UpdateValue() {
        numberSelected.Invoke();
        totalValue = 10 * firstValue + secondValue;

        if (totalValue == answer) {
            firstNumber.Disable();
            secondNumber.Disable();
            
            check.SetActive(true);
            answered.Invoke();

            var scale = check.transform.localScale.x;
            
            TweenFactory.Tween(null, 0, 1, 1f, TweenScaleFunctions.CubicEaseOut,
                (t) => {
                    check.transform.localScale = new Vector3(scale * t.CurrentValue, scale * t.CurrentValue,
                        scale * t.CurrentValue);
                });
        }
    }
    
}
