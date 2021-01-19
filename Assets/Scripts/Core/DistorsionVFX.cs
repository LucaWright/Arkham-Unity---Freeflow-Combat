using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistorsionVFX : MonoBehaviour
{    
    public float distorsionTime = .35f;
    public float growthRateSpeed = 1f;

    void Start()
    {
        transform.localScale = new Vector3(0, 0, 0);        
    }

    public void SpawnAirDistorsion(Transform trans)
    {
        transform.position = trans.position;
        this.enabled = true;
        StartCoroutine(AirDistorsion());
    }

    IEnumerator AirDistorsion()
    {
        float timer = 0;

        //Debug.Break();

        while (timer < distorsionTime)
        {
            timer += Time.fixedDeltaTime;

            var growthRate = growthRateSpeed * timer;

            transform.localScale = new Vector3(growthRate, growthRate, growthRate);
            yield return new WaitForFixedUpdate();
        }

        transform.localScale = new Vector3(0, 0, 0);
    }
}
