using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int Minutes;
    public static int PlayerOneScore;
    public TextMeshProUGUI PlayerOneScoreLabel;
    public static int PlayerTwoScore;
    public TextMeshProUGUI PlayerTwoScoreLabel;
    public float Seconds;
    public static bool Timeout;
    public TextMeshProUGUI TimerLabel;

    // Methods
    private void Start()
    {
        Timeout = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PlayerOneScore = 0;
            PlayerTwoScore = 0;
            Timeout = false;
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Seconds > 0f)
        {
            Seconds -= Mathf.Min(Seconds, Time.deltaTime);
        }
        else if (this.Minutes > 0f)
        {
            this.Minutes--;
            this.Seconds = 60f - Time.deltaTime;
        }
        else
        {
            Timeout = true;
        }
        PlayerOneScoreLabel.text = PlayerOneScore.ToString();
        PlayerTwoScoreLabel.text = PlayerTwoScore.ToString();
        TimerLabel.text = (Minutes < 10 ? $"0{Minutes}" : $"{Minutes}") + ":" + 
            (Seconds < 10 ? $"0{Mathf.Floor(this.Seconds)}" : $"{Mathf.Floor(this.Seconds)}");
    }

}
