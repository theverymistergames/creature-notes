using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneInputsManager : MonoBehaviour {
    
    private RuneInput[] _inputs;
    private List<int> _activeRunesTypes = new List<int>();

    public List<int> activeRunesTypes => _activeRunesTypes;

    void Start() {
        _inputs = GetComponentsInChildren<RuneInput>();

        foreach (var runeInput in _inputs) {
            runeInput.runeActivated += RuneActivated;
        }
    }

    private void RuneActivated(int type, bool active) {
        if (active) _activeRunesTypes.Add(type);
        else _activeRunesTypes.Remove(type);
    }
}
