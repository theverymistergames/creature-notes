using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Cysharp.Threading.Tasks;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Random = UnityEngine.Random;

public struct Step {
    [XmlElement("replic")] public string[] Replics;
    [XmlAttribute("role")] public int RoleId;
    [XmlAttribute("page")] public bool IsPage;
}

[XmlRoot("dialog")]
public struct Dialog {
    [XmlArray("roles")] [XmlArrayItem("role")]
    public string[] Roles;
    [XmlArray("steps")] [XmlArrayItem("step")]
    public Step[] Steps;
}

public class DialogController : MonoBehaviour {
    [SerializeField] private Text textInstance;
    [SerializeField] private Text skipButton;

    [SerializeField] private EventReference dialogFinishedEvent;
    
    [SerializeField] private float replicOffset = 25f;
    
    private Dialog _dialog;

    private bool _replicInProgress;
    private bool _skipped;
    private bool _finished;
    
    private float _totalHeight;
    private int _currentStepID;
    
    private List<Text> _texts = new();

    private void OnEnable() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Awake() {
        textInstance.text = "";

        _dialog = Deserialize<Dialog>("Assets/!Project/Levels/Chapter1/Chapter1Dialog.xml");
    }

    private void StartDialog() {
        NextReplic();
    }

    async UniTask NextReplic() {
        if (_finished) return;
        
        //TODO: move skip logic
        skipButton.text = "SKIP";
        skipButton.color = new Color(1, 1, 1, 0.15f);
        
        _replicInProgress = true;
        
        if (_currentStepID >= _dialog.Steps.Length) {
            _finished = true;
            dialogFinishedEvent.Raise();
            
            return;
        }
        
        var step = _dialog.Steps[_currentStepID];

        if (step.IsPage) {
            _currentStepID++;
            await Clear();
            NextReplic();
            return;
        }
        
        var replics = step.Replics;
        var role = _dialog.Roles[step.RoleId];

        foreach (var replic in replics) {
            var textObj = Instantiate(textInstance, gameObject.transform);
            textObj.transform.position -= Vector3.up * _totalHeight;
            _texts.Add(textObj);
            
            await DrawReplic(textObj, replic, role);
            
            _skipped = false;
            _totalHeight += textObj.preferredHeight;
        }

        if (_currentStepID + 1 < _dialog.Steps.Length) {
            var nextReplic = _dialog.Steps[_currentStepID + 1];
            if (nextReplic.RoleId != step.RoleId) _totalHeight += replicOffset;  
        } else {
            _totalHeight += replicOffset;
        }

        _currentStepID++;

        _replicInProgress = false;
        
        //TODO: move skip logic

        skipButton.text = "CONTINUE";
        skipButton.color = new Color(1, 1, 1, 0.5f);
    }

    async UniTask Clear() {
        _totalHeight = 0;

        var tasks = new List<UniTask>();
        _texts.ForEach(t => tasks.Add(EraseReplic(t)));

        await UniTask.WhenAll(tasks);
        
        foreach (var text in _texts) Destroy(text);
    }

    async UniTask EraseReplic(Text text) {
        var line = text.text;

        for (var i = line.Length - 1; i >= 0; i -= 5) {
            text.text = line[..i];
            
            await UniTask.Delay(5);
        }

        text.text = "";
    }

    async UniTask DrawReplic(Text text, string line, string role) {
        for (var i = 0; i < line.Length; i++) {
            if (_skipped) {
                text.text = role + line;
                return;
            }

            var add = 0;

            var part = line[..(i + 1)];
            
            text.text = role + part;
            
            if (part.Length > 3) {
                var last = part[^1..];
                if (last.Equals("…")) add = 300;
            }
            
            await UniTask.Delay(20 + Random.Range(0, 20) + add);
        }
    }

    public void Next() {
        if (_replicInProgress) {
            _skipped = true;
        } else {
            NextReplic();
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.S)) StartDialog();
        if (Input.GetKeyDown(KeyCode.Space)) Next();
    }

    private static T Deserialize<T>(string path) {
        var serializer = new XmlSerializer(typeof(T));
        var reader = new StreamReader(path);
        var deserialized = (T)serializer.Deserialize(reader.BaseStream);
        reader.Close();
        return deserialized;
    }
}