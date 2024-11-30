using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterDebuff  : MonoBehaviour, IActorComponent {

        [SerializeReference] [SubclassSelector] private IActorAction _onAttack;

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
            _monster.OnAttackPerformed += OnAttackPerformed;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            _monster.OnAttackPerformed -= OnAttackPerformed;
        }

        private void OnAttackPerformed() {
            _debuffData.debuffEvent.Raise(_debuffData.debuffImage);
            _onAttack?.Apply(_actor, _enableCts.Token).Forget();
        }
    }
    
}