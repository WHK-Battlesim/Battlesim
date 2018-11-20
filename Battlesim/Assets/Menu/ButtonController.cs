using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

public class ButtonController : MonoBehaviour {

    public GameObject buttonPrefab;
    public GameObject descriptionTextBox;

	// Use this for initialization
	void Start () {
        var availibleMaps = AssetDatabase.GetSubFolders("Assets/Resources");
        foreach (var subfolder in availibleMaps) {
            GenerateButton(subfolder);
        }
	}
	
    void GenerateButton(string subfolder) {
        char[] delimiter = new char[] { '/' };
        var splitPath = subfolder.Split(delimiter);
        var buttonName = subfolder.Split(delimiter).Last();
        var resourcePath = string.Join("", subfolder.Split(delimiter).Skip(2));

        TextAsset descriptionAsset = Resources.Load<TextAsset>(resourcePath + "/Description");
        string description = descriptionAsset != null ? descriptionAsset.text : "Description missing";

        GameObject button = Instantiate(buttonPrefab);
        button.transform.parent = gameObject.transform;
        button.GetComponentInChildren<Text>().text = buttonName;

        button.GetComponent<Button>().onClick.AddListener(
            () => ButtonClick(description));
    }

    void ButtonClick(string text) {
        descriptionTextBox.GetComponent<Text>().text = text;
    }
}
