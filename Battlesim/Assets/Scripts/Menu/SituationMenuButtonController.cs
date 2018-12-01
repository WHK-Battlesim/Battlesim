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

    public GameObject previewImage;

    const int HISTORISCH = 0;

    void Start()
    {
        SetPreviewImage();
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

    void SetPreviewImage() {
        Texture2D t = Resources.Load<Texture2D>("Maps/" + CurrentMap.Subfolder + "/Thumbnail");
        Rect r;
        if (t == null)
        {
            t = Texture2D.whiteTexture;
            r = new Rect(0, 0, 1, 1);
        }
        else
        {
            var imageRect = previewImage.GetComponent<Image>().GetPixelAdjustedRect();
            if (imageRect.width > imageRect.height)
            {
                var ratio = imageRect.height / imageRect.width;
                r = new Rect(0, 0, t.width, t.width * ratio);
            }
            else
            {
                var ratio = imageRect.width / imageRect.height;
                r = new Rect(0, 0, t.height * ratio, t.height);
            }
        }
        var s = Sprite.Create(t, r, Vector2.zero);
        previewImage.GetComponent<Image>().overrideSprite = s;
    }
}
