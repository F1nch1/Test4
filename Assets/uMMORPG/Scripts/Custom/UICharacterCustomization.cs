﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class UICharacterCustomization : MonoBehaviour
{
    [Header("Components")]
    public GameObject panel;
    public TMP_InputField nameInput;
    public Button createButton;
    public Slider classSlider;
    public Toggle gameMasterToggle;
    public NetworkManagerMMO manager;
    public Button cancelButton;
    public GameObject preview;
    public Transform[] cameraLocations;
    public UICharacterSelection selector;
    public SkinnedMeshRenderer playerSMR;
    public Material skinMaterial;
    public Slider skinColor;
    public float r;
    public float g;
    public float b;
    public FlexibleColorPicker fcp;



    void Update()
    {
        // only update while visible (after character selection made it visible)
        if (panel.activeSelf)
        {
            // still in lobby?
            if (manager.state == NetworkState.Lobby)
            {
                Show();

                // copy player classes to class selection
                /*classSlider.options = manager.playerClasses.Select(
                    p => new Dropdown.OptionData(p.name)
                ).ToList();*/

                // only show GameMaster option for host connection
                // -> this helps to test and create GameMasters more easily
                // -> use the database field for dedicated servers!
                gameMasterToggle.gameObject.SetActive(NetworkServer.localClientActive);

                //classSlider.onValueChanged.AddListener(debugSlider);

                // create
                createButton.interactable = manager.IsAllowedCharacterName(nameInput.text);
                createButton.onClick.SetListener(() => {
                    CharacterCreateMsg message = new CharacterCreateMsg
                    {
                        name = nameInput.text,
                        classIndex = (int)classSlider.value,
                        gameMaster = gameMasterToggle.isOn,
                        skinColor = fcp.color
                        
                        
                    };
                    NetworkClient.Send(message);
                    Debug.Log(message);
                    selector.creatingCharacter = false;
                    Destroy(preview.gameObject);
                    Hide();
                });

                // cancel
                cancelButton.onClick.SetListener(() => {
                    nameInput.text = "";
                    Hide();
                    selector.creatingCharacter = false;
                    Camera.main.transform.position = manager.selectionCameraLocation.position;
                    Camera.main.transform.rotation = manager.selectionCameraLocation.rotation;
                });
            }
            else Hide();
        }
        playerSMR = manager.playerClasses[(int)classSlider.value].SMR;

        r = 1 - skinColor.value;
        b = 1 - skinColor.value;
        g = 1 - skinColor.value;
        for (int i = 0; i < playerSMR.sharedMaterials.Length; i++)
        {
            skinMaterial = playerSMR.sharedMaterials[i];
            skinMaterial.SetColor("_Color", new Color(fcp.color.r, fcp.color.g, fcp.color.b, .5f));
        }
    }

    public void Hide() { panel.SetActive(false); }
    public void Show() { panel.SetActive(true); }
    public void StartCreation()
    {
        if (preview != null)
        {
            Destroy(preview.gameObject);
        }
        GameObject basePrefab = manager.playerClasses[(int)classSlider.value].model;
        preview = Instantiate(basePrefab, manager.creationLocation.position, manager.creationLocation.rotation);
        preview.transform.localScale = new Vector3(4.8f, 4.8f, 4.8f);
        Camera.main.transform.position = cameraLocations[(int)classSlider.value].position;
        Camera.main.transform.rotation = cameraLocations[(int)classSlider.value].rotation;
        playerSMR = manager.playerClasses[(int)classSlider.value].SMR;
        skinMaterial = playerSMR.sharedMaterial;
        r = 1 - skinColor.value;
        b = 1 - skinColor.value;
        g = 1 - skinColor.value;
        skinMaterial.SetColor("_Color", new Color(fcp.color.r, fcp.color.g, fcp.color.b, .5f));
    }
    public bool IsVisible() { return panel.activeSelf; }

    public void debugSlider()
    {
        Debug.Log(manager.playerClasses[(int)classSlider.value]);
    }

    public void changePrefab()
    {
        Camera.main.transform.position = cameraLocations[(int)classSlider.value].position;
        Camera.main.transform.rotation = cameraLocations[(int)classSlider.value].rotation;
        if (preview != null)
        {
            Destroy(preview.gameObject);
        }
        GameObject basePrefab = manager.playerClasses[(int)classSlider.value].model;
        preview = Instantiate(basePrefab, manager.creationLocation.position, manager.creationLocation.rotation);
        preview.transform.localScale = new Vector3(4.8f, 4.8f, 4.8f);
    }

    public void skinColorSlider()
    {
        playerSMR = manager.playerClasses[(int)classSlider.value].SMR;
        
        r = 1 - skinColor.value;
        b = 1 - skinColor.value;
        g = 1 - skinColor.value;
        for(int i = 0; i < playerSMR.sharedMaterials.Length; i++)
        {
            skinMaterial = playerSMR.sharedMaterials[i];
            skinMaterial.SetColor("_Color", new Color(fcp.color.r, fcp.color.g, fcp.color.b, .5f));
        }

    }
}
