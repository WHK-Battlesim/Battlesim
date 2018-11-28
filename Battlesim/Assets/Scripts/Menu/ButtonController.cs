﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

public class ButtonController : MonoBehaviour {

    public GameObject buttonPrefab;
    public GameObject descriptionTextBox;
    public GameObject mapThumbnail;

    [Serializable]
    class MapIndex {
        public List<string> folders = new List<string>();
    }

    // Use this for initialization
    void Start () {
        string mapIndexJson = Resources.Load<TextAsset>("Maps/mapIndex").text;
        MapIndex mapIndex = JsonUtility.FromJson<MapIndex>(mapIndexJson);
        foreach (var folder in mapIndex.folders) {
            GenerateButton(folder);
        }
	}

    void GenerateButton(string subfolder) {
        char[] delimiter = new char[] { '/' };
        var splitPath = subfolder.Split(delimiter);
        var buttonName = subfolder.Split(delimiter).Last();

        // Instantiate button
        GameObject button = Instantiate(buttonPrefab);
        button.transform.parent = gameObject.transform;
        button.GetComponentInChildren<Text>().text = buttonName;

        button.GetComponent<Button>().onClick.AddListener(
            () => ButtonClick(
                GetDescription(subfolder),
                GetThumbnailSprite(subfolder))
        );
    }

    Sprite GetThumbnailSprite(string subfolder) {
        Texture2D t = Resources.Load<Texture2D>("Maps/" + subfolder + "/Tiles_old");
        int w, h;
        if (t == null) {
            t = Texture2D.whiteTexture;
            w = 1;
            h = 1;
        }
        else {
            w = t.width;
            h = t.height;
        }
        return Sprite.Create(t, new Rect(0, 0, w, h), Vector2.zero);
    }

    string GetDescription(string subfolder) {
        TextAsset descriptionAsset = Resources.Load<TextAsset>("Maps/" + subfolder + "/Description");
        return descriptionAsset != null ? descriptionAsset.text : "Description missing";
    }

    void ButtonClick(string text, Sprite thumbnailSprite) {
        descriptionTextBox.GetComponent<Text>().text = text;
        mapThumbnail.GetComponent<Image>().overrideSprite = thumbnailSprite;
    }
}