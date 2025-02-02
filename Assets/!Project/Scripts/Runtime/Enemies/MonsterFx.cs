using System;
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

        private float[] _progressArray;
        private CancellationTokenSource _enableCts;
        private CancellationTokenSource _healthCts;
        private IActor _actor;
        private Monster _monster;
        
        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _monster = actor.GetComponent<Monster>();
            _progressArray = new float[_progressActions?.Length ?? 0];
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
            float progress = _monster.Progress;

            for (int i = 0; i < _progressActions.Length; i++) {
                ref var action = ref _progressActions[i];
                ref float p = ref _progressArray[i];

                float targetProgress = action.progressCurve?.Evaluate(progress) ?? progress;
                targetProgress = action.progressModulator?.Modulate(targetProgress) ?? targetProgress;
                
                float oldP = p;
                p = p.SmoothExpNonZero(targetProgress, action.progressSmoothing, dt);

                if (action.notifyProgressDirection.NeedNotifyProgress(oldP, p)) {
                    action.progressAction?.OnProgressUpdate(p);
                }
                
                action.events.NotifyTweenEvents(_actor, p, oldP, _enableCts.Token);
            }
        }

        private void OnMonsterEvent(MonsterEventType evt) {
            if (evt is MonsterEventType.Respawn or MonsterEventType.Death) {
                AsyncExt.RecreateCts(ref _healthCts);
            }

            for (int i = 0; i < _eventActions.Length; i++) {
                ref var eventAction = ref _eventActions[i];
                if (eventAction.eventType != evt) continue;

                eventAction.action?.Apply(_actor, _healthCts.Token).Forget();
            }
        }
    }
    
}