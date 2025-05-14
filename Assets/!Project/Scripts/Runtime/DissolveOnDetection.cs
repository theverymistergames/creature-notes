using System.Collections.Generic;
using LitMotion;
using MisterGames.Interact.Detectables;
using UnityEngine;

public sealed class DissolveOnDetection : MonoBehaviour {
    
    [SerializeField] private Vector2 cutoffHeightFromTo = new(1, -1);
    [SerializeField] [Min(0f)] private float duration = 2f;
    [SerializeField] private Detectable _detectable;

    private readonly List<Material> _materials = new();
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");

    private void Awake() {
        var renderers = gameObject.GetComponentsInChildren<MeshRenderer>();

        for (int i = 0; i < renderers.Length; i++) {
            _materials.AddRange(renderers[i].materials);
        }
    }

    private void OnEnable() {
        _detectable.OnDetectedBy += OnDetectedBy;
    }

    private void OnDisable() {
        _detectable.OnDetectedBy -= OnDetectedBy;
    }

    private void OnDetectedBy(IDetector obj) {
        StartDissolve();
    }

    private void StartDissolve() {
        LMotion.Create(cutoffHeightFromTo[0], cutoffHeightFromTo[1], duration).WithOnComplete(() => {
            gameObject.SetActive(false);
        }).Bind(value => {
            for (int i = 0; i < _materials.Count; i++) {
                _materials[i].SetFloat(CutoffHeight, value);
            }
        });
    }
}
