using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterDebuff : MonoBehaviour, IActorComponent {

        [SerializeField] private DebuffAction[] _debuffActions;

        [Serializable]
        private struct DebuffAction {
            public MonsterEventType eventType;
            [SerializeReference] [SubclassSelector] public IActorAction action;
        }
        
        private CancellationTokenSource _enableCts;
        private IActor _actor;
        private Monster _monster;
        private MonsterDebuffData _debuffData;
        
        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _monster = actor.GetComponent<Monster>();
        }

        void IActorComponent.OnSetData(IActor actor) {
            _debuffData = actor.GetData<MonsterDebuffData>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            _monster.OnMonsterEvent += OnMonsterEvent;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            _monster.OnMonsterEvent -= OnMonsterEvent;
        }

        private void OnMonsterEvent(MonsterEventType evt) {
            for (int i = 0; i < _debuffData.debuffImages.Length; i++) {
                ref var debuffImage = ref _debuffData.debuffImages[i];
                if (debuffImage.eventType != evt) continue;

                ApplyDebuff(debuffImage.delay, debuffImage.sprite, _enableCts.Token).Forget();
            }

            for (int i = 0; i < _debuffActions.Length; i++) {
                ref var debuffAction = ref _debuffActions[i];
                if (debuffAction.eventType != evt) continue;

                debuffAction.action?.Apply(_actor, _enableCts.Token).Forget();
            }
        }

        private async UniTask ApplyDebuff(float delay, Sprite sprite, CancellationToken cancellationToken) {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken)
                         .SuppressCancellationThrow();
            
            if (cancellationToken.IsCancellationRequested) return;
            
            _debuffData.debuffEvent.Raise(sprite);
        }

#if UNITY_EDITOR
        [SerializeField] private MonsterEventType _testMonsterEventType;
        [Button] private void TestDebuff() => OnMonsterEvent(_testMonsterEventType);
#endif
    }
    
}