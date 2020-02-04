using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int Minutes;
    private int min;
    public static int PlayerOneScore;
    public TextMeshProUGUI PlayerOneScoreLabel;
    public static int PlayerTwoScore;
    public TextMeshProUGUI PlayerTwoScoreLabel;
    public float Seconds;
    private float sec;
    public static bool Timeout;
    public TextMeshProUGUI TimerLabel;
    public GameObject ResultPanel;
    public TextMeshProUGUI resultText;
    public GameObject ball;

    // Methods
    private void Start()
    {
        Timeout = false;
        min = Minutes;
        sec = Seconds;
        PlayerOneScore = 0;
        PlayerTwoScore = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MainMenu();
        }
        if (sec > 0f)
        {
            sec -= Mathf.Min(sec, Time.deltaTime);
        }
        else if (min > 0f)
        {
            min--;
            sec = 60f - Time.deltaTime;
        }
        else
        {
            Timeout = true;
        }
        PlayerOneScoreLabel.text = PlayerOneScore.ToString();
        PlayerTwoScoreLabel.text = PlayerTwoScore.ToString();
        TimerLabel.text = (min < 10 ? $"0{min}" : $"{min}") + ":" + 
            (sec < 10 ? $"0{Mathf.Floor(this.sec)}" : $"{Mathf.Floor(this.sec)}");

        if(Timeout)
        {
            ResultPanel.SetActive(true);
            ball.SetActive(false);
            if(PlayerOneScore > PlayerTwoScore)
            {
                resultText.text = "Player One Win";
            }
            else if(PlayerOneScore < PlayerTwoScore)
            {
                resultText.text = "Player Two Win";
            }
            else
            {
                resultText.text = "Draw";
            }
        }
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
