using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DetectionConfig
{
    public float minValueH;
    public float minValueS;
    public float minValueV;

    public float maxValueH;
    public float maxValueS;
    public float maxValueV;
}
