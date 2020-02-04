using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Settings settings;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Play1()
    {
        SceneManager.LoadScene(1);
    }

    public void Play2()
    {
        SceneManager.LoadScene(2);
    }

    public void ShowSettings()
    {
        settings.gameObject.SetActive(true);
        settings.LoadPrefs();
        gameObject.SetActive(false);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
