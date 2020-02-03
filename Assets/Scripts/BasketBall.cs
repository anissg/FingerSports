using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasketBall : MonoBehaviour
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
                    BasketballGameManager.PlayerOneScore++;
                }
                else
                {
                    BasketballGameManager.PlayerTwoScore++;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GetComponent<AudioSource>().Play();
    }

    private void Start()
    {
        Scoring = false;
    }
}
