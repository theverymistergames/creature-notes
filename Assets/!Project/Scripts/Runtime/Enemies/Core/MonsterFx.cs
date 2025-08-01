﻿using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterFx : MonoBehaviour, IActorComponent, IUpdate {
        
        [Header("Progress")]
        [SerializeField] private ProgressAction[] _progressActions;
        
        [Header("Monster Events")]
        [SerializeField] private EventAction[] _eventActions;
        
        [Serializable]
        private struct ProgressAction {
            [Min(0f)] public float progressSmoothing;
            public AnimationCurve progressCurve;
            [SerializeReference] [SubclassSelector] public IProgressModulator progressModulator;
            public TweenDirection notifyProgressDirection;
            [SerializeReference] [SubclassSelector] public ITweenProgressAction progressAction;
            public TweenEvent[] events;
        }

        [Serializable]
        private struct EventAction {
            public MonsterEventType eventType;
            [SerializeReference] [SubclassSelector] public IActorAction action;
        }

        private const float Tolerance = 0.00001f;

        private float[] _progressArray;
        private float[] _linearProgressArray;
        private CancellationTokenSource _enableCts;
        private CancellationTokenSource _healthCts;
        private IActor _actor;
        private Monster _monster;
        private MonsterEventType _lastEventType;
        
        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _monster = actor.GetComponent<Monster>();
            
            _progressArray = new float[_progressActions?.Length ?? 0];
            _linearProgressArray = new float[_progressArray.Length];
        }
        
        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            
            _monster.OnMonsterEvent += OnMonsterEvent;
            
            if (!_monster.IsDead) AsyncExt.RecreateCts(ref _healthCts);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            AsyncExt.DisposeCts(ref _healthCts);
            
            PlayerLoopStage.Update.Unsubscribe(this);
         
            _monster.OnMonsterEvent -= OnMonsterEvent;
        }

        void IUpdate.OnUpdate(float dt) {
            if (_lastEventType == MonsterEventType.Reset) {
                PlayerLoopStage.Update.Unsubscribe(this);
                return;
            }
            
            float progress = _monster.Progress;
            float targetProgress = _monster.TargetProgress;
            
            bool reachedTargetProgress = true;
            
            for (int i = 0; i < _progressActions.Length; i++) {
                ref var action = ref _progressActions[i];
                ref float pSmooth = ref _progressArray[i];
                ref float pLinearSmooth = ref _linearProgressArray[i];

                float pNotSmoothed = action.progressCurve?.Evaluate(progress) ?? progress;
                pNotSmoothed = action.progressModulator?.Modulate(pNotSmoothed) ?? pNotSmoothed;
                
                float pSmoothOld = pSmooth;
                pSmooth = pSmooth.SmoothExpNonZero(pNotSmoothed, action.progressSmoothing, dt);
                
                if (action.notifyProgressDirection.NeedNotifyProgress(pSmoothOld, pSmooth)) {
                    action.progressAction?.OnProgressUpdate(pSmooth);
                }
                
                action.events.NotifyTweenEvents(_actor, pSmooth, pSmoothOld, _enableCts.Token);
                
                pLinearSmooth = pLinearSmooth.SmoothExpNonZero(progress, action.progressSmoothing, dt);
                reachedTargetProgress &= pLinearSmooth.IsNearlyEqual(targetProgress, Tolerance);
            }
            
            if (reachedTargetProgress) PlayerLoopStage.Update.Unsubscribe(this);
        }

        private void OnMonsterEvent(MonsterEventType evt) {
            _lastEventType = evt;
            
            if (evt is MonsterEventType.Respawn or MonsterEventType.Death or MonsterEventType.Reset) {
                AsyncExt.RecreateCts(ref _healthCts);
            }

            for (int i = 0; i < _eventActions.Length; i++) {
                ref var eventAction = ref _eventActions[i];
                if (eventAction.eventType != evt) continue;

                eventAction.action?.Apply(_actor, _healthCts.Token).Forget();
            }
            
            if (evt != MonsterEventType.Reset) PlayerLoopStage.Update.Subscribe(this);
        }
    }
    
}