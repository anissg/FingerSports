using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallBounciness : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Ball"))
        {
            collision.gameObject.GetComponent<Rigidbody2D>().velocity *= .5f;
        }
    }
}
