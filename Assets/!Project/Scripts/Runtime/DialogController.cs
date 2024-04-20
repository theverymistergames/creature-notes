using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public struct Replic {
    [XmlElement("header")] public string header;
    [XmlElement("line")] public string[] lines;
    [XmlAttribute("self")] public bool self;
    [XmlArray("questions")] [XmlArrayItem("replic")]
    public Replic[] questions;
    [XmlArray("answers")] [XmlArrayItem("replic")]
    public Replic[] answers;
}

[XmlRoot("dialog")]
public struct Dialog {
    [XmlArray("replics")] [XmlArrayItem("replic")]
    public Replic[] replics;
}

public class DialogController : MonoBehaviour {
    public Text yourText;
    public Text hisText;
    public Text skipButton;
    public Text choiseText;

    [SerializeField] private EventReference dialogFinishedEvent;

    private int _currentReplicID;
    private int _currentLineID;

    private Dialog _dialog;

    private bool _replicInProgress;
    private bool _waitInProgress;
    private bool _skipped;
    private int _chosenQuestionID = -1;

    private bool _finished;

    private void Start() {
        yourText.text = "";
        hisText.text = "";

        _dialog = Deserialize<Dialog>("Assets/Chapter1Dialog.xml");
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static T Deserialize<T>(string path) {
        var serializer = new XmlSerializer(typeof(T));
        var reader = new StreamReader(path);
        var deserialized = (T)serializer.Deserialize(reader.BaseStream);
        reader.Close();
        return deserialized;
    }

    private void StartDialog() {
        StartCoroutine(NextReplic());
    }

    private IEnumerator NextReplic() {
        if (_finished) yield break;
        
        skipButton.text = "SKIP";
        skipButton.color = new Color(1, 1, 1, 0.15f);
        _replicInProgress = true;

        Debug.Log(_dialog.replics.Length);
        Debug.Log(_currentReplicID);
        
        if (_currentReplicID >= _dialog.replics.Length) {
            _finished = true;
            dialogFinishedEvent.Raise();
            yield break;
        }
        
        var replic = _dialog.replics[_currentReplicID];
        var isSelfReplic = replic.self;

        // Debug.Log(replic.answers?.Length);
        if (replic.answers?.Length > 0) replic = replic.answers[_chosenQuestionID];
        _chosenQuestionID = -1;
        
        if (replic.questions?.Length > 0) {
            _waitInProgress = true;
            skipButton.gameObject.SetActive(false);
            var chosen = -1;
            var tmp = new List<Text>();
            
            for (var i = replic.questions.Length - 1; i >= 0; i--) {
                var choiseTextObj = Instantiate(choiseText, gameObject.transform);
                choiseTextObj.gameObject.SetActive(true);
                choiseTextObj.text = replic.questions[i].header;
                choiseTextObj.transform.position -= new Vector3(0, -80 + 105 * i, 0);
                choiseTextObj.GetComponent<DialogVariant>().id = i;
                choiseTextObj.GetComponent<DialogVariant>().pressed.AddListener((int id) => { chosen = id; });

                tmp.Add(choiseTextObj);
            }
            
            yield return new WaitUntil(() => chosen >= 0);

            foreach (var text in tmp) Destroy(text);

            replic = replic.questions[chosen];
            skipButton.gameObject.SetActive(true);
            _waitInProgress = false;

            _chosenQuestionID = chosen;
        }
        
        var lines = replic.lines;

        for (var i = 0; i < lines.Length; i++) {
            var textObj = Instantiate(isSelfReplic ? yourText : hisText, gameObject.transform);
            textObj.transform.position += new Vector3(0, -40 -65 * _currentLineID - _currentReplicID * 20, 0);
            var line = lines[i];

            if (!isSelfReplic) {
                textObj.text = line;
                textObj.rectTransform.sizeDelta = new Vector2(textObj.preferredWidth, 0);
                textObj.text = "";
            }
            
            yield return StartCoroutine(DrawLine(textObj, line));
            
            _currentLineID++;
        }

        _currentReplicID++;

        _skipped = false;
        _replicInProgress = false;
        skipButton.text = "CONTINUE";
        skipButton.color = new Color(1, 1, 1, 0.5f);

        if (_dialog.replics.Length > _currentReplicID && _dialog.replics[_currentReplicID].questions?.Length > 0) {
            Next();
        }
    }

    private IEnumerator DrawLine(Text textObj, string line) {
        for (var i = 0; i < line.Length; i++) {
            if (_skipped) {
                textObj.text = line;
                yield break;
            }
            
            textObj.text = line.Substring(0, i + 1);

            yield return new WaitForSeconds(0.02f + Random.Range(0, 0.02f));
        }
    }

    public void Next() {
        if (_waitInProgress) return;

        if (_replicInProgress) {
            _skipped = true;
        } else {
            StartCoroutine(NextReplic());
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.S)) StartDialog();
        if (Input.GetKeyDown(KeyCode.Space)) Next();
    }
}