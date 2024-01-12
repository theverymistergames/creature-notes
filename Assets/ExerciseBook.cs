using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExerciseBook : MonoBehaviour {
    [SerializeField] private List<AnswerGroup> _answerGroups = new List<AnswerGroup>();
    
    private int answersCounter = 0;
    
    public UnityEvent done;
    
    // Start is called before the first frame update
    void Start() {
        foreach (var answerGroup in _answerGroups) {
            answerGroup.answered.AddListener(OnAnswered);
        }
    }

    void OnAnswered() {
        answersCounter++;

        if (answersCounter == _answerGroups.Count) {
            done.Invoke();
        }
    }
}
