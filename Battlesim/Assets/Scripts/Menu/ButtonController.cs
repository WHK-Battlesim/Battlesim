using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

public static class CurrentMap {
    public static string Subfolder { get; set; }
    public static int Situation { get; set; }
}

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
        CurrentMap.Subfolder = null;
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
                subfolder,
                GetDescription(subfolder),
                GetThumbnailSprite(subfolder))
        );
    }

    Sprite GetThumbnailSprite(string subfolder) {
        Texture2D t = Resources.Load<Texture2D>("Maps/" + subfolder + "/Thumbnail");
        Rect r;
        if (t == null) {
            t = Texture2D.whiteTexture;
            r = new Rect(0, 0, 1, 1);
        }
        else {
            var imageRect = mapThumbnail.GetComponent<Image>().GetPixelAdjustedRect();
            if (imageRect.width > imageRect.height) {
                var ratio = imageRect.height / imageRect.width;
                Debug.Log(ratio);
                Debug.Log(t.width);
                Debug.Log(t.height * ratio);
                r = new Rect(0, 0, t.width, t.height * ratio);
            }
            else {
                var ratio = imageRect.width / imageRect.height;
                Debug.Log(ratio);
                Debug.Log(t.height);
                Debug.Log(t.width * ratio);
                r = new Rect(0, 0, t.width * ratio, t.height);
            }
        }
        return Sprite.Create(t, r, Vector2.zero);
    }

    string GetDescription(string subfolder) {
        TextAsset descriptionAsset = Resources.Load<TextAsset>("Maps/" + subfolder + "/Description");
        return descriptionAsset != null ? descriptionAsset.text : "Description missing";
    }

    void ButtonClick(string subfolder, string text, Sprite thumbnailSprite) {
        CurrentMap.Subfolder = subfolder;
        descriptionTextBox.GetComponent<Text>().text = text;
        mapThumbnail.GetComponent<Image>().overrideSprite = thumbnailSprite;
    }
}
