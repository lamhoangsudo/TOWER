using System;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;

public static class UtilClass
{
    public static Enum.Direction GetChildDirection(Transform transform)
    {
        Vector3 direction = Vector3.Normalize(transform.localPosition);
        Enum.Direction directionEnum = Enum.Direction.none;
        if (Vector3.Dot(direction, Vector3.forward) == 1)
        {
            directionEnum = Enum.Direction.forward;
        }
        else if (Vector3.Dot(direction, Vector3.back) == 1)
        {
            directionEnum = Enum.Direction.backward;
        }
        else if (Vector3.Dot(direction, Vector3.left) == 1)
        {
            directionEnum = Enum.Direction.left;
        }
        else if (Vector3.Dot(direction, Vector3.right) == 1)
        {
            directionEnum = Enum.Direction.right;
        }
        else if (Vector3.Dot(direction, Vector3.up) == 1)
        {
            directionEnum = Enum.Direction.up;
        }
        else if (Vector3.Dot(direction, Vector3.down) == 1)
        {
            directionEnum = Enum.Direction.down;
        }
        return directionEnum;
    }
}
