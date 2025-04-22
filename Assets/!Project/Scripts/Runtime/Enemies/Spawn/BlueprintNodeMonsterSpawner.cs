using System;
using MisterGames.BlueprintLib;
using MisterGames.Blueprints;
using MisterGames.Common.Attributes;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {

    [Serializable]
    [BlueprintNode(Name = "Monster Spawner", Category = "Monsters", Color = BlueprintLibColors.Node.Scenario)]
    public sealed class BlueprintNodeMonsterSpawner : IBlueprintNode, IBlueprintEnter {

        [SerializeField] private MonsterSpawnerConfig _config;
        [SerializeField] private bool _resetFlesh;

        private IBlueprint _blueprint;
        private NodeToken _token;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Start"));
            meta.AddPort(id, Port.Enter("Continue"));
            meta.AddPort(id, Port.Enter("Stop"));
            meta.AddPort(id, Port.Input<MonsterSpawner>("Spawner"));
            meta.AddPort(id, Port.Exit("On Complete Battle"));
            meta.AddPort(id, Port.Exit("On Start Wave"));
            meta.AddPort(id, Port.Exit("On Complete Wave"));
        }

        public void OnInitialize(IBlueprint blueprint, NodeToken token, NodeId root) {
            _blueprint = blueprint;
            _token = token;
        }

        public void OnDeInitialize(IBlueprint blueprint, NodeToken token, NodeId root) {
            _blueprint = null;
            
            _config.startedWaveEvent.Unsubscribe<int>(OnStartWave);
            _config.completedWaveEvent.Unsubscribe<int>(OnCompleteWave);
            _config.completedBattleEvent.Unsubscribe(OnCompleteBattle);
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            var spawner = blueprint.Read<MonsterSpawner>(token, 3);
            if (spawner == null) return;
            
            switch (port) {
                case 0:
                    spawner.StartSpawning(_config, _resetFlesh);
                    _config.startedWaveEvent.Subscribe<int>(OnStartWave);
                    _config.completedWaveEvent.Subscribe<int>(OnCompleteWave);
                    _config.completedBattleEvent.Subscribe(OnCompleteBattle);
                    break;
                
                case 1:
                    spawner.ContinueSpawningFromCompletedWaves(_resetFlesh);
                    _config.startedWaveEvent.Subscribe<int>(OnStartWave);
                    _config.completedWaveEvent.Subscribe<int>(OnCompleteWave);
                    _config.completedBattleEvent.Subscribe(OnCompleteBattle);
                    break;
                
                case 2:
                    spawner.StopSpawning(_resetFlesh);
                    _config.startedWaveEvent.Unsubscribe<int>(OnStartWave);
                    _config.completedWaveEvent.Unsubscribe<int>(OnCompleteWave);
                    _config.completedBattleEvent.Unsubscribe(OnCompleteBattle);
                    break;
            }
        }

        private void OnCompleteBattle() {
            _blueprint.Call(_token, 4);
        }

        private void OnStartWave(int waveIndex) {
            _blueprint.Call(_token, 5);
        }

        private void OnCompleteWave(int waveIndex) {
            _blueprint.Call(_token, 6);
        }
    }

}
