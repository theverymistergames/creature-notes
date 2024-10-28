using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class Monster : MonoBehaviour, IActorComponent {

        public void Bind(TweenRunner tweenRunner) {
            
        }

        public async UniTask Unbind(CancellationToken cancellationToken) {
            
        }
    }
    
}