using System;
using Cysharp.Threading.Tasks;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Events;

public class AnswerGroup : MonoBehaviour {
    [SerializeField] private NumberGroup firstNumber;
    [SerializeField] private NumberGroup secondNumber;
    [SerializeField] private GameObject check;

    private int firstValue = 0;
    private int secondValue = 0;
    private int totalValue = 0;

    [NonSerialized] public UnityEvent answered = new UnityEvent();
    [NonSerialized] public UnityEvent numberSelected = new UnityEvent();
    [SerializeField] private int answer = 0;

    private TweenRunner _checkTweenRunner;
    
    // Start is called before the first frame update
    void Start()
    {
        firstNumber.numberSet.AddListener(OnFirstNumberSet);
        secondNumber.numberSet.AddListener(OnSecondNumberSet);

        _checkTweenRunner = GetComponent<TweenRunner>();
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

            _checkTweenRunner.TweenPlayer.Play().Forget();
        }
    }
    
}
