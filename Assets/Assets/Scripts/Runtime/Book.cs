using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MisterGames.Interact.Detectables;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;


enum State
{
    None,
    Normal,
    Reversed,
}

public class Book : MonoBehaviour
{
    public float pageSwitchTreshold = 0.02f;
    public float totalAnimTime = 2.042f;
    public float animSpeed = 2;
    public Interactive colliderLeft;
    public Interactive colliderRight;
    public EventReference pageFlipEvent;
    [NonSerialized] private List<Animator> _pages = new List<Animator>();

    private int _step = 0;
    private State _state = State.None;
    
    void Start() {
        _pages = GetComponentsInChildren<Animator>().ToList();
        StartCoroutine(StopAnimAtEnd(_pages[0]));

        for (var i = 1; i < _pages.Count; i++) {
            _pages[i].speed = 0;
            
            if (i > 1) {
                _pages[i].gameObject.SetActive(false);
            }
        }
        
        colliderLeft.OnStartInteract += _ => StartCoroutine(MoveLeft());
        colliderRight.OnStartInteract += _ => StartCoroutine(MoveRight());
    }

    public void SetInteractive(bool interactive)
    {
        colliderLeft.GetComponent<Detectable>().enabled = interactive;
        colliderRight.GetComponent<Detectable>().enabled = interactive;
    }

    IEnumerator MoveLeft()
    {
        if (_state == State.Normal || _step == 0) yield break;
        _state = State.Reversed;
        
        _step--;
        
        var step = _step;
        
        yield return StartCoroutine(StartAnim(_pages[step + 1], true, () =>
        {
            _pages[step]?.gameObject.SetActive(true);
            StartCoroutine(StopAnimAtEnd(_pages[step]));
        },
        () =>
        {
            _pages[step + 2]?.gameObject.SetActive(false);
            pageFlipEvent.Raise();
        }));
    }

    IEnumerator MoveRight() {
        if (_state == State.Reversed || _step == _pages.Count - 2) yield break;
        _state = State.Normal;
        
        _step++;
        var step = _step;

        yield return StartCoroutine(StartAnim(_pages[step], false, 
            () =>
            {
                _pages[step + 1]?.gameObject.SetActive(true);
            },
            () =>
            {
                _pages[step - 1]?.gameObject.SetActive(false);
                pageFlipEvent.Raise();
            }));
    }

    private IEnumerator StartAnim(Animator animator, bool reversed, Action startCallback, Action finishCallback) {
        if (animator.speed != 0) yield break;
        
        var stateName = reversed ? "PageAnim_Reversed" : "PageAnim";
        
        animator.speed = animSpeed;
        animator.PlayInFixedTime(stateName, 0, 0);

        yield return new WaitForSeconds((pageSwitchTreshold) * totalAnimTime / animSpeed);
        
        startCallback();
        
        yield return new WaitForSeconds((1 - pageSwitchTreshold * 2) * totalAnimTime / animSpeed);

        finishCallback();
        
        yield return new WaitForSeconds((pageSwitchTreshold) * totalAnimTime / animSpeed);

        animator.speed = 0;
        
        if (!_pages.Find(p => p.speed != 0)) {
            _state = State.None;
        }
    }

    IEnumerator StopAnimAtEnd(Animator animator) {
        animator.PlayInFixedTime("PageAnim", 0, totalAnimTime);
        yield return new WaitForSeconds(0);
        animator.speed = 0;
    }
}
