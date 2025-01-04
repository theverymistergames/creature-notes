using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Inventory;
using MisterGames.Common.Tick;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class ActionDeathFullscreenAnimation : IActorAction {
        public CustomPassVolume volume;
        public Material material;
        public float duration = 5;
        public Vector2 bounds = new(-2f, 2f);
        
        private static readonly int CutoffHeight = Shader.PropertyToID("_CutoffHeight");

        public async UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var pass = volume.customPasses.Find(p => p.name == "Death");
            pass.enabled = true;

            await LMotion.Create(bounds.x, bounds.y, duration).WithOnComplete(() => {
                material.SetFloat(CutoffHeight, bounds.x);
                pass.enabled = false;
            }).Bind(t => {
                material.SetFloat(CutoffHeight, t);
            });
        }
    }
    
}