using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerCombatStrike))]
public class FOWeditor : Editor
{
    //private void OnSceneGUI()
    //{
    //    PlayerCombatStrike fow = (PlayerCombatStrike)target;
    //    Handles.color = Color.red;
    //    Handles.DrawWireArc(fow.transform.position + Vector3.up, Vector3.up, Vector3.forward, 360, fow.shortRangeRadius);
    //    Handles.color = Color.white;
    //    Handles.DrawWireArc(fow.transform.position + Vector3.up, Vector3.up, fow.DirFromAngle(-fow.viewAngle / 2), fow.viewAngle, fow.longRangeRadius);
    //    Handles.DrawLine(fow.transform.position + Vector3.up, fow.transform.position + Vector3.up + fow.DirFromAngle(-fow.viewAngle / 2) * fow.longRangeRadius);
    //    Handles.DrawLine(fow.transform.position + Vector3.up, fow.transform.position + Vector3.up + fow.DirFromAngle(fow.viewAngle / 2) * fow.longRangeRadius);
    //}
}
