using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class SituationMenuButtonController : MonoBehaviour {
   
    public GameObject historischButton;
    public GameObject resetButton;
    public GameObject situation1Button;
    public GameObject situation2Button;
    public GameObject situation3Button;
    public GameObject startButton;
    public GameObject editButton;
    public GameObject backButton;

    const int HISTORISCH = 0;

    void Start()
    {
        SetSituation(HISTORISCH);

        historischButton.GetComponent<Button>().onClick.AddListener(
            () => SetSituation(HISTORISCH)
        );
        situation1Button.GetComponent<Button>().onClick.AddListener(
            () => SetSituation(1)
        );
        situation2Button.GetComponent<Button>().onClick.AddListener(
            () => SetSituation(2)
        );
        situation3Button.GetComponent<Button>().onClick.AddListener(
            () => SetSituation(3)
        );

        startButton.GetComponent<Button>().onClick.AddListener(
            () => Launch("Battlefield")
        );
        editButton.GetComponent<Button>().onClick.AddListener(
            () => Launch("Editor")
        );
        backButton.GetComponent<Button>().onClick.AddListener(
            () => Launch("Main Menu")
        );
    }

    void SetSituation(int situation)
    {
        CurrentMap.Situation = situation;
    }

    void Launch(string scene)
    {
        if (CurrentMap.Subfolder != null)
        {
            SceneManager.LoadScene(scene);
        }
    }
}
