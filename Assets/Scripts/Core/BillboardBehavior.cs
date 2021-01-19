using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardBehavior : MonoBehaviour
{
    void Update()
    {
        var towardsDirection = Camera.main.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(towardsDirection);
    }
}
