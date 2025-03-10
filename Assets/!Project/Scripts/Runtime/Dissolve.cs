using System.Collections.Generic;
using System.Linq;
using LitMotion;
using MisterGames.Interact.Detectables;
using UnityEngine;

public class Dissolve : MonoBehaviour {
    [SerializeField] private Vector2 cutoffHeightFromTo = new(1, -1);
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool dissolveOnDetected;
    [SerializeField] private Detectable _detectable;

    private readonly List<Material> _materials = new();
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");
    

    private void DetectableOnOnDetectedBy(IDetector obj) {
        StartDissolve();
    }

    private void Awake() {
        var renderers = gameObject.GetComponentsInChildren<MeshRenderer>().ToList();
        
        foreach (var meshRenderer in renderers) {
            _materials.AddRange(meshRenderer.materials);
        }
    }

    public void StartDissolve() {
        LMotion.Create(cutoffHeightFromTo[0], cutoffHeightFromTo[1], duration).WithOnComplete(() => {
            gameObject.SetActive(false);
        }).Bind(value => {
            foreach (var material in _materials) {
                material.SetFloat(CutoffHeight, value);
            }
        });
    }
    
    private void OnEnable() {
        if (dissolveOnDetected) _detectable.OnDetectedBy += DetectableOnOnDetectedBy;
    }

    private void OnDisable() {
        if (dissolveOnDetected) _detectable.OnDetectedBy -= DetectableOnOnDetectedBy;
    }
}
