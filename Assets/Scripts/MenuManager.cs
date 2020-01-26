using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button buttonPlay;
    [SerializeField] private UnityEngine.UI.Button buttonSettings;
    [SerializeField] private UnityEngine.UI.Button buttonExit;
    [SerializeField] private Settings settings;

    // Start is called before the first frame update
    void Start()
    {
        buttonPlay.onClick.AddListener(Play);
        buttonSettings.onClick.AddListener(ShowSettings);
        buttonExit.onClick.AddListener(Exit);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void Play()
    {
        // Lancer le jeu
    }

    void ShowSettings()
    {
        settings.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    void Exit()
    {
        Application.Quit();
    }
}
