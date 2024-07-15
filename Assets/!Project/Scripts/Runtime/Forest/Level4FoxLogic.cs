using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Common.Attributes;
using MisterGames.Common.GameObjects;
using MisterGames.Tweens;
using UnityEngine;

public class Level4FoxLogic : MonoBehaviour {
    [SerializeField] private TweenRunner foxTween;
    [SerializeField] private FireFliesParticlesController particles;
    [SerializeField] private GameObject trigger;
    [SerializeField] private GameObject enemy;
    [SerializeField] private ActionGroup respawnAction;

    private Vector3 _enemyPosition;
    private Vector3 _enemyRotation;
    private IActor _char;
    
    private void Awake() {
        _enemyPosition = enemy.transform.position;
        _enemyRotation = enemy.transform.eulerAngles;
    }

    private void Start() {
        _char = CharacterSystem.Instance.GetCharacter();
    }

    public void Reset() {
        trigger.SetEnabled(true);
        
        foxTween.TweenPlayer.Speed = 1;
        foxTween.TweenPlayer.Progress = 0;
        
        foxTween.gameObject.SetEnabled(true);
        
        particles.Reset();

        enemy.transform.position = _enemyPosition;
        enemy.transform.eulerAngles = _enemyRotation;
        
        enemy.SetEnabled(false);

        respawnAction.Apply(_char);
    }
}
