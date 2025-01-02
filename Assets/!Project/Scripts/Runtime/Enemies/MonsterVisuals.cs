using System;
using System.Threading;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Tick.Core;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterVisuals : MonoBehaviour, IActorComponent, IUpdate {
        
        [SerializeField] private Action[] _actions;
        
        [Serializable]
        private struct Action {
            [Min(0f)] public float progressSmoothing;
            public AnimationCurve progressCurve;

            [SerializeReference] [SubclassSelector]
            public IProgressModulator progressModulator;

            [SerializeReference] [SubclassSelector]
            public ITweenProgressAction progressAction;
            
            public TweenEvent[] events;
        }

        private float[] _progressArray;
        private CancellationTokenSource _enableCts;
        private IActor _actor;
        private Monster _monster;

        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _monster = actor.GetComponent<Monster>();
            _progressArray = new float[_actions?.Length ?? 0];
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            float progress = _monster.Progress;

            for (int i = 0; i < _actions.Length; i++) {
                ref var action = ref _actions[i];
                ref float p = ref _progressArray[i];

                float targetProgress = action.progressCurve?.Evaluate(progress) ?? progress;
                targetProgress = action.progressModulator?.Modulate(targetProgress) ?? targetProgress;
                
                float oldP = p;
                p = p.SmoothExpNonZero(targetProgress, action.progressSmoothing * dt);
                
                action.progressAction?.OnProgressUpdate(p);
                action.events.NotifyTweenEvents(_actor, p, oldP, _enableCts.Token);
            }
        }
    }
    
}