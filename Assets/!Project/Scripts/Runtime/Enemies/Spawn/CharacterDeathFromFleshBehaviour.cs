using Cysharp.Threading.Tasks;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Common.Attributes;
using MisterGames.Common.Pooling;
using MisterGames.Logic.Damage;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class CharacterDeathFromFleshBehaviour : MonoBehaviour {

        [SerializeField] private EventReference _deathEvent;
        [SerializeField] private GameObject _deathEffect;
        [SerializeReference] [SubclassSelector] private IActorAction _deathAction;

        private void OnEnable() {
            _deathEvent.Subscribe(OnDeathEvent);
        }

        private void OnDisable() {
            _deathEvent.Unsubscribe(OnDeathEvent);
        }

        private void OnDeathEvent() {
            var hero = CharacterSystem.Instance.GetCharacter();
            
            hero.GetComponent<HealthBehaviour>().Kill();
            
            PrefabPool.Main.Get(_deathEffect, hero.GetComponent<Camera>().transform);
            
            _deathAction?.Apply(CharacterSystem.Instance.GetCharacter()).Forget();
        }

#if UNITY_EDITOR
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void ApplyDeath() {
            OnDeathEvent();
        }
#endif
    }
    
}