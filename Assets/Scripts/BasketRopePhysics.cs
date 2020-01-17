using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasketRopePhysics : MonoBehaviour
{
    public Transform FirstAnchor;
    private LineRenderer LineRenderer;
    public Transform SecondAnchor;

    // Methods
    private void Start()
    {
        LineRenderer = GetComponent<LineRenderer>();
        LineRenderer.positionCount = 2;
        if (SecondAnchor != null)
        {
            LineRenderer.positionCount++;
        }
    }

    private void Update()
    {
        if (SecondAnchor != null)
        {
            Vector3[] positions =
            {
                FirstAnchor.position + new Vector3(0f, 0f, -1f), 
                transform.position + new Vector3(0f, 0f, -1f), 
                SecondAnchor.position + new Vector3(0f, 0f, -1f)
            };
            LineRenderer.SetPositions(positions);
        }
        else
        {
            Vector3[] vectorArray2 =
            {
                FirstAnchor.position + new Vector3(0f, 0f, -1f),
                transform.position + new Vector3(0f, 0f, -1f)
            };
            LineRenderer.SetPositions(vectorArray2);
        }
    }

}
