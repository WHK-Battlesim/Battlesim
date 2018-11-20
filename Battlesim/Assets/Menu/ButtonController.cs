using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

public class ButtonController : MonoBehaviour {

    public GameObject buttonPrefab;

	// Use this for initialization
	void Start () {
        var availibleMaps = AssetDatabase.GetSubFolders("Assets/Data");
        char[] delimiter = new char[] { '/' };
        foreach (var subfolder in availibleMaps) {
            name = subfolder.Split(delimiter).Last();
            GenerateButton(name);
        }
	}
	
    void GenerateButton(string text) {
        GameObject button = Instantiate(buttonPrefab);
        button.transform.parent = gameObject.transform;
        button.GetComponentInChildren<Text>().text = text;

        button.GetComponent<Button>().onClick.AddListener(
            () => ButtonClick(text));
    }

    void ButtonClick(string text) {
        Debug.Log(text);
    }
}
