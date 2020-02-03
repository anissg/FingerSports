using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallReset : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Ball"))
        {
            collision.gameObject.transform.position = new Vector3(0, 2, 0);
            collision.gameObject.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
        }
    }
}
