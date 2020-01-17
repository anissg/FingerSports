using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int Minutes;
    public static int PlayerOneScore;
    public TextMeshPro PlayerOneScoreLabel;
    public static int PlayerTwoScore;
    public TextMeshPro PlayerTwoScoreLabel;
    public float Seconds;
    public static bool Timeout;
    public TextMeshPro TimerMinutesLabel;
    public TextMeshPro TimerSecondsLabel;

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
        TimerMinutesLabel.text = Minutes < 10 ? $"0{Minutes}" : $"{Minutes}";
        TimerSecondsLabel.text = Seconds < 10 ? $"0{Mathf.Floor(this.Seconds)}" : $"{Mathf.Floor(this.Seconds)}";
    }

}
