using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChooseButtonScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
        gameObject.GetComponent<Button>().onClick.AddListener(
            () => SceneManager.LoadScene("Situation Menu")
        );
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
