using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private bool Scoring;

    // Methods
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("ScoringColliderTop"))
        {
            this.Scoring = true;
        }
        if (collision.gameObject.name.Contains("ScoringColliderBottom") && this.Scoring)
        {
            if (base.transform.position.x > 0f)
            {
                GameManager.PlayerOneScore++;
            }
            else
            {
                GameManager.PlayerTwoScore++;
            }
            this.Scoring = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("ScoringColliderTop"))
        {
            this.Scoring = false;
        }
    }

    private void Start()
    {
        this.Scoring = false;
    }
}
