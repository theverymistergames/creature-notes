using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MisterGames.Interact.Interactives;
using UnityEngine;

public class Book : MonoBehaviour
{
    public Interactive colliderLeft;
    public Interactive colliderRight;
    [NonSerialized] private List<Animator> pages = new List<Animator>();
    [NonSerialized] private List<Interactive> interactives = new List<Interactive>();

    private int step = 0;
    
    void Start()
    {
        pages = GetComponentsInChildren<Animator>().ToList();
        StartCoroutine(stopAnim(pages[0]));

        for (int i = 1; i < pages.Count; i++)
        {
            pages[i].speed = 0;
        }

        var counter = 0;

        colliderRight.OnStartInteract += _ => MoveRight();
        

        // foreach (var animator in pages)
        // {
        //     counter++;
        //     
        //     var inter = animator.GetComponentInChildren<Interactive>();
        //     interactives.Add(inter);
        //     inter.OnStartInteract += _ => onPagePressed(counter);
        // }
        // StartCoroutine(Init());
    }

    void MoveRight()
    {
        var id = step + 1;
        
        var animator = pages[id];
        animator.speed = 1;
        StartCoroutine(startAnim(pages[id], "PageAnim"));
    }

    void onPagePressed(int id)
    {
        if (id > step)
        {
            step++;
            var animator = pages[id];
            animator.speed = 1;
            StartCoroutine(startAnim(pages[id], "PageAnim"));
        }
    }

    IEnumerator Init()
    {
        foreach (var animator in pages)
        {
            yield return new WaitForSeconds(0);
            animator.enabled = false;
        }
    }

    IEnumerator startAnim(Animator animator, string stateName) {
        animator.PlayInFixedTime(stateName, 0, 0);
        yield return new WaitForSeconds(2);
        animator.speed = 0;
    }

    IEnumerator stopAnim(Animator animator) {
        animator.PlayInFixedTime("PageAnim", 0, 2.042f);
        yield return new WaitForSeconds(0);
        animator.speed = 0;
    }
}
