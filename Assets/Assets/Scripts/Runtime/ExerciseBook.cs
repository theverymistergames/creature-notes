using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExerciseBook : MonoBehaviour {
    [SerializeField] private List<AnswerGroup> _answerGroups = new List<AnswerGroup>();
    
    private int answersCounter = 0;
    
    public UnityEvent done;
    public UnityEvent started;
    
    // Start is called before the first frame update
    void Start() {
        foreach (var answerGroup in _answerGroups) {
            answerGroup.answered.AddListener(OnAnswered);
            answerGroup.numberSelected.AddListener(OnNumberSelected);
        }
    }

    void OnNumberSelected() {
        foreach (var answerGroup in _answerGroups) {
            answerGroup.numberSelected.RemoveListener(OnNumberSelected);
        }
        
        started.Invoke();
    }

    void OnAnswered() {
        answersCounter++;

        if (answersCounter == _answerGroups.Count) {
            done.Invoke();
        }
    }
}
