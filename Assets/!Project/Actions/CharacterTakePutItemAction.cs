using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Inventory;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;


namespace MisterGames.ActionLib.GameObjects {
    
    [Serializable]
    public sealed class CharacterTakePutItemAction : IActorAction {
        
        public TakePut method = TakePut.Take;
        [Min(0f)] public float duration = 0.3f;
        public Ease ease = Ease.InCubic;
        public bool disableCollider = true;
        public Transform item;
    
        public enum TakePut {
            Take,
            Put,
        }
        
        public async UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var transform = context.Transform;
            
            if (disableCollider) item.GetComponent<Collider>().enabled = false;

            var start = method == TakePut.Take ? item.position : transform.position;
            var end = method == TakePut.Put ? item.position : transform.position;
            
            await LMotion.Create(start, end, duration).WithEase(ease).BindToPosition(item);
            
            switch (method) {
                case TakePut.Take:
                    item.gameObject.SetActive(false);
                    break;
                
                case TakePut.Put: {
                    if (disableCollider) item.GetComponent<Collider>().enabled = true;
                    break;
                }
            }
        }
    }
    
}