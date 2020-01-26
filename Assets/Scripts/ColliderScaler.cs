using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ColliderScaler : MonoBehaviour
{
    void Start()
    {
        float xFactor = Camera.main.pixelRect.width / 1920;
        float yFactor = Camera.main.pixelRect.height / 1080;

        EdgeCollider2D ec = GetComponent<EdgeCollider2D>();
        
        Vector2[] points = new Vector2[ec.pointCount];
        for (int i = 0; i < ec.pointCount; i++)
        {
            points[i] = new Vector2(ec.points[i].x * xFactor, ec.points[i].y * yFactor);
        }

        ec.points = points;
    }
}
