using System.Collections;
using System.Collections.Generic;
using MisterGames.Character.Steps;
using UnityEngine;

public class StepsSounds : MonoBehaviour {
    public AudioClip[] sounds;
    
    void Start() {
        var source = GetComponent<AudioSource>();
        
        var test = GetComponent<CharacterStepsPipeline>();
        
        test.OnStep += (foot, distance, point) => {
            source.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
        };
    }
}
