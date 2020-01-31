using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private bool Scoring = false;
    private bool BottomScring = false;

    // Methods
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("ScoringColliderTop"))
        {
            Scoring = true;
        }
        if (collision.gameObject.name.Contains("ScoringColliderBottom") && this.Scoring)
        {
            if(!BottomScring)
            { 
                if (transform.position.x > 0f)
                {
                    GameManager.PlayerOneScore++;
                }
                else
                {
                    GameManager.PlayerTwoScore++;
                }
                Scoring = false;
            }
        }
        if (collision.gameObject.name.Contains("ScoringColliderBottom") && !this.Scoring)
        {
            BottomScring = true;
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
            BottomScring = false;
        }
    }

    private void Start()
    {
        Scoring = false;
    }
}
