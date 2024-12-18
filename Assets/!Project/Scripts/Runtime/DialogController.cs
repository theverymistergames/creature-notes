using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
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

    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scroll;
    
    private Dialog _dialog;

    private bool _replicInProgress;
    private bool _finished;
    
    private float _totalHeight;
    private int _currentStepID;

    private CancellationTokenSource _cts = new();
    
    private readonly List<string> _specialSigns = new() { ".", "?", "!" };

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
        
        if (_currentStepID >= _dialog.Steps.Length) {
            _finished = true;
            dialogFinishedEvent.Raise();
            
            return;
        }
        
        _replicInProgress = true;
        skipButton.text = "SKIP";
        
        var step = _dialog.Steps[_currentStepID];
        
        var replics = step.Replics;
        var role = _dialog.Roles[step.RoleId];

        foreach (var replic in replics) {
            var textObj = Instantiate(textInstance, content);
            textObj.transform.position -= Vector3.up * _totalHeight;
            
            await DrawReplic(textObj, replic, role, _cts.Token);
            
            _totalHeight += textObj.preferredHeight;
        }

        _currentStepID++;
        
        if (_currentStepID < _dialog.Steps.Length) {
            var nextReplic = _dialog.Steps[_currentStepID];
            if (nextReplic.RoleId != step.RoleId) _totalHeight += replicOffset;  
        } else {
            _totalHeight += replicOffset;
        }
        
        _replicInProgress = false;
        skipButton.text = _currentStepID >= _dialog.Steps.Length ? "EXIT" : "CONTINUE";
    }

    async UniTask DrawReplic(Text textObject, string line, string role, CancellationToken token) {
        for (var i = 0; i < line.Length; i++) {
            var addMS = 0;

            var part = line[..(i + 1)];
            
            textObject.text = role + part;
            var last = part[^1..];

            if (part.Length > 3) {
                addMS = last switch {
                    "…" => 400,
                    "," => 100,
                    _ => addMS
                };
                
                if (_specialSigns.Contains(last)) addMS = 200;
            }

            await UniTask.Delay(20 + Random.Range(0, 20) + addMS, cancellationToken: token).SuppressCancellationThrow();
            
            if (token.IsCancellationRequested) {
                textObject.text = role + line;
                UpdateContentSizeWithCurrentTextSize(textObject);
                return;
            }

            UpdateContentSizeWithCurrentTextSize(textObject);
        }
    }

    private void UpdateContentSizeWithCurrentTextSize(Text textObject) {
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Math.Max(-textObject.rectTransform.localPosition.y + textObject.preferredHeight + 300, 600));
    }

    public void Next() {
        if (_replicInProgress) {
            AsyncExt.RecreateCts(ref _cts);
        } else {
            NextReplic();
        }
    }

    private void Update() {
        scroll.normalizedPosition = Vector2.Lerp(scroll.normalizedPosition, Vector2.zero, Time.deltaTime * 3);
        
        if (Input.GetKeyDown(KeyCode.S)) StartDialog();
        if (Input.GetKeyDown(KeyCode.Space)) Next();
    }

    //TODO: remove debug
    private void LateUpdate() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static T Deserialize<T>(string path) {
        var serializer = new XmlSerializer(typeof(T));
        var reader = new StreamReader(path);
        var deserialized = (T)serializer.Deserialize(reader.BaseStream);
        reader.Close();
        return deserialized;
    }

    async UniTask EraseReplic(Text text) {
        var line = text.text;

        for (var i = line.Length - 1; i >= 0; i -= 5) {
            text.text = line[..i];
            await UniTask.Delay(5);
        }

        text.text = "";
    }
}