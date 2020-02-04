using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootBall : MonoBehaviour
{
    private bool Scoring = false;

    // Methods
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("ScoringCollider") && !Scoring)
        {
            Scoring = true;

            if (transform.position.x > 0f)
            {
                GameManager.PlayerOneScore++;
            }
            else
            {
                GameManager.PlayerTwoScore++;
            }

            StartCoroutine("Score");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GetComponent<AudioSource>().Play();
    }

    IEnumerator Score()
    {
        yield return new WaitForSeconds(2);

        Scoring = false;
        transform.position = new Vector3(0,2);
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }
}
