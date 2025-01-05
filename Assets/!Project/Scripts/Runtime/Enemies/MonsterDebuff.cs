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
        
        private CancellationTokenSource _healthCts;
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
            if (!_monster.IsDead) AsyncExt.RecreateCts(ref _healthCts);
            
            _monster.OnMonsterEvent += OnMonsterEvent;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _healthCts);
            
            _monster.OnMonsterEvent -= OnMonsterEvent;
        }

        private void OnMonsterEvent(MonsterEventType evt) {
            if (evt is MonsterEventType.Respawn or MonsterEventType.Death) {
                AsyncExt.RecreateCts(ref _healthCts);
            }

            ProcessActions(evt, _healthCts.Token);
        }

        private void ProcessActions(MonsterEventType eventType, CancellationToken cancellationToken) {
            for (int i = 0; i < _debuffData.debuffImages.Length; i++) {
                ref var debuffImage = ref _debuffData.debuffImages[i];
                if (debuffImage.eventType != eventType) continue;

                ApplyDebuff(debuffImage.delay, debuffImage.sprite, cancellationToken).Forget();
            }

            for (int i = 0; i < _debuffActions.Length; i++) {
                ref var debuffAction = ref _debuffActions[i];
                if (debuffAction.eventType != eventType) continue;

                debuffAction.action?.Apply(_actor, cancellationToken).Forget();
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
        
        [Button] 
        private void TestDebuff() {
            if (Application.isPlaying) ProcessActions(_testMonsterEventType, destroyCancellationToken);
        }
#endif
    }
    
}