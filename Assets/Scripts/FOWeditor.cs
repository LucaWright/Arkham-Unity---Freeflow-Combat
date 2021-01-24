using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerCombatStrike))]
public class FOWeditor : Editor
{
    private void OnSceneGUI()
    {
        PlayerCombatStrike player = (PlayerCombatStrike)target;
        Handles.color = Color.blue;
        Handles.DrawWireArc(player.transform.position + Vector3.up, Vector3.up, player.DirFromAngle(-player.fow / 2), player.fow, 10f);
        Handles.DrawLine(player.transform.position + Vector3.up, player.transform.position + Vector3.up + player.DirFromAngle(-player.fow / 2) * 10f);
        Handles.DrawLine(player.transform.position + Vector3.up, player.transform.position + Vector3.up + player.DirFromAngle(player.fow / 2) * 10f);
        if (Application.isPlaying)
        {
            Handles.color = Color.red;
            Handles.DrawWireArc(player.transform.position + Vector3.up, Vector3.up, Vector3.forward, 360, CombatDirector.DistanceInfo.FirstLineDistance);
            Handles.color = Color.yellow;
            for (int i = 1; i < CombatDirector.DistanceInfo.Lines; i++)
            {
                Handles.DrawWireArc(player.transform.position + Vector3.up, Vector3.up, Vector3.forward, 360, CombatDirector.DistanceInfo.FirstLineDistance + CombatDirector.DistanceInfo.LineToLineDistance * i);
            }
        }
    }
}
