using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class DistanceHandler
{
    [SerializeField] Transform target;

    [SerializeField] int lines = 3;
    [SerializeField] float firstLineDistance = 3;
    [SerializeField] float lineToLineDistance = 2;

    public Transform Target { get => target; set => target = value; }
    public int Lines { get => lines; set => lines = value; }
    public float FirstLineDistance { get => firstLineDistance; set => firstLineDistance = value; }
    public float LineToLineDistance { get => lineToLineDistance; set => lineToLineDistance = value; }
    public float LastLineRadius { get => firstLineDistance + lineToLineDistance * lines; }

    public int GetLine(Transform agent)
    {
        var distance = Vector3.Distance(agent.position, target.position);
        
        if (Mathf.RoundToInt(distance / firstLineDistance) >= 1) //considerare un floor to int
        {
            distance -= firstLineDistance;
            return Mathf.Clamp
                        (Mathf.RoundToInt(distance / lineToLineDistance) + 1,
                        1,
                        lines + 1);
        }
        return 0;
    }

    public float LineDistance(int _line)
    {
        if (lines > 1) return firstLineDistance + lineToLineDistance * (_line - 1);
        return firstLineDistance;
    }
}