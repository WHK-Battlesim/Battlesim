using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour {

    public GameObject buttonPrefab;

	// Use this for initialization
	void Start () {
        for (int i = 0; i < 5; i++) {
            GenerateButton("Button" + i.ToString());
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
