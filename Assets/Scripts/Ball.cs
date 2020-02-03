using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private bool Scoring = false;
    private bool BottomScoring = false;

    // Methods
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("ScoringColliderTop"))
        {
            Scoring = true;
        }
        if (collision.gameObject.name.Contains("ScoringColliderBottom") && this.Scoring)
        {
            if(!BottomScoring)
            { 
                if (transform.position.x > 0f)
                {
                    GameManager.PlayerTwoScore++;
                }
                else
                {
                    GameManager.PlayerOneScore++;
                }
                Scoring = false;
            }
        }
        if (collision.gameObject.name.Contains("ScoringColliderBottom") && !this.Scoring)
        {
            BottomScoring = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("ScoringColliderTop"))
        {
            Scoring = false;
        }
        if (collision.gameObject.name.Contains("ScoringColliderBottom"))
        {
            BottomScoring = false;
        }
    }

    private void Start()
    {
        Scoring = false;
    }
}
