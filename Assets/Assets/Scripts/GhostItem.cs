using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Detectables;
using MisterGames.Interact.Interactives;
using Unity.VisualScripting;
using UnityEngine;

public class GhostItem : MonoBehaviour {
    [SerializeField] private InventoryItemType id = InventoryItemType.BALL;

    public GameObject realItem;
    
    private Interactive _interactive;
    private Detectable _detectable;
    
    public float distanceTreshold = 2;
    
    private GameObject _player;
    private Material _mat;
    
    void Start() {
        _player = GameObject.FindGameObjectWithTag("Player");
        _mat = GetComponent<MeshRenderer>().material;
        
        _interactive = GetComponent<Interactive>();
        _interactive.OnStartInteract += OnStartInteract;

        // _detectable = GetComponent<Detectable>();
        // _detectable.enabled = false;
        
        realItem.SetActive(false);
    }

    private void OnStartInteract(IInteractiveUser user) {
        if (Inventory.instance.HasItem(id)) {
            RoomIntroController.instance.ItemWasSet();
            
            realItem.SetActive(true);
            gameObject.SetActive(false);
        }
    }

    void Update() {
        var distance = Vector3.Distance(transform.position, _player.transform.position);
        
        if (distance < distanceTreshold) {
            _mat.SetFloat("_alpha", (distanceTreshold - distance) / distanceTreshold * 1f);
        }
        else {
            _mat.SetFloat("_alpha", 0);
        }
    }
}
