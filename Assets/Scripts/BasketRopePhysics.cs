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
                FirstAnchor.position, 
                transform.position, 
                SecondAnchor.position
            };
            LineRenderer.SetPositions(positions);
        }
        else
        {
            Vector3[] vectorArray2 =
            {
                FirstAnchor.position,
                transform.position
            };
            LineRenderer.SetPositions(vectorArray2);
        }
    }

}
