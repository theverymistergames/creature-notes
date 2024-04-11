using System.Collections.Generic;
using LitMotion;
using UnityEngine;

public class Dissolve : MonoBehaviour {
    [SerializeField] private GameObject[] gameObjects;
    [SerializeField] private Vector2 cutoffHeightFromTo = new(1, -1);
    [SerializeField] private float duration = 2f;

    private readonly List<Material> _materials = new();
    private static readonly int CutoffHeight = Shader.PropertyToID("_Cutoff_Height");

    private void Start() {
        foreach (var go in gameObjects) {
            _materials.AddRange(go.GetComponent<MeshRenderer>().materials);
        }
    }

    public void StartDissolve() {
        LMotion.Create(cutoffHeightFromTo[0], cutoffHeightFromTo[1], duration).Bind(value => {
            foreach (var material in _materials) {
                material.SetFloat(CutoffHeight, value);
            }
        });
    }
}
