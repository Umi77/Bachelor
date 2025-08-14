using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    public static float GetLowestPoint<T>(this Transform origin) where T : Collider =>
        origin.GetComponent<T>().bounds.min.y;
}
