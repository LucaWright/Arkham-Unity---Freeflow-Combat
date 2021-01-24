using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEditor.Animations;
using UnityEngine;

[SelectionBase]
public class CrowdTestScript : MonoBehaviour
{
    public Transform reference;

    float timer = 0;
    //public float everyXseconds = .5f;
    public Vector2 everyMinMaxSeconds;

    public float speed;
    public float avoidanceRadius;
    public LayerMask avoidanceMask;
    public float minMovementMagnitudue = .35f;

    public float[] interestMapWeights;
    public Vector3[] contextVectors;
    public float[] dangerMap;
    public float[] interestMap;

    public Vector3 desiredDir;

    CapsuleCollider avoidanceCollider;

    bool isOverriding;
    
    
    // Start is called before the first frame update
    void Start()
    {
        avoidanceCollider = GetComponent<CapsuleCollider>();
        contextVectors = new Vector3[]
        {
            Vector3.forward,
            Vector3.right,
            Vector3.left,
            Vector3.back
        };
        interestMapWeights = new float[]
        {
            1f,
            .7f,
            .7f,
            .5f
        };

        interestMap = new float[contextVectors.Length];
        dangerMap = new float[contextVectors.Length];
    }

    void Update()
    {
        timer += Time.deltaTime;

        var everyXseconds = Random.Range(everyMinMaxSeconds.x, everyMinMaxSeconds.y);

        if (timer >= everyXseconds)
        {
            timer -= everyXseconds;
            ResetAllAvoidanceMaps();
            CheckSurroundings();
        }
        MoveTo();
    }

    void ResetAllAvoidanceMaps()
    {
        for (int i = 0; i < dangerMap.Length; i++)
        {
            dangerMap[i] = 0;
        }
        for (int i = 0; i < interestMap.Length; i++)
        {
            interestMap[i] = 0;
        } 
    }

    public void CheckSurroundings()
    {
        avoidanceCollider.enabled = false;
        Collider[] characterColliders = Physics.OverlapSphere(avoidanceCollider.transform.position, avoidanceRadius, avoidanceMask);
        avoidanceCollider.enabled = true;

        int characterArrayLength = characterColliders.Length;
        if (characterArrayLength > 0)
        {
            for (int i = 0; i < characterArrayLength; i++)
            {
                Vector3 distanceVector = (characterColliders[i].transform.position - transform.position);
                UpdateDangerMap(distanceVector);
                Debug.DrawRay(transform.position + Vector3.up, distanceVector, Color.red, .02f);
            }
        }
        if (isOverriding)
        {
            Debug.Log("C'è entrata lo stesso dopo il break");
        }
        else
        {
            EvaluateMovement();
        }
    }

    void UpdateDangerMap(Vector3 distanceVector)
    {
        for (int  i = 0;  i < contextVectors.Length;  i++)
        {
            float dot = Vector3.Dot(distanceVector.normalized, WorldToLocalDir(contextVectors[i])); //il dot non dà 1. Perché il vettore non è normalizzato???
            //Check if OVERRIDE!
            if (i == 0)
            {
                if (distanceVector.sqrMagnitude <= Mathf.Pow(avoidanceRadius, 2) && dot > .7f)
                {
                    //OVERRIDE!
                    isOverriding = true;
                    PullBack();
                    return;
                }
            }
            //if (dot > 0)
            //{
            //    //Anzichè sommare... vedere se è più alto di quello già storato?
            //    dangerMap[i] += dot;
            //}
            if (dot > dangerMap[i])
            {
                //Anzichè sommare... vedere se è più alto di quello già storato?
                dangerMap[i] = dot;
            }
        }
    }

    void EvaluateMovement()
    {
        float interest = 0f;
        int index = 0;

        for (int i = 0; i < dangerMap.Length; i++)
        {
            //Update Interest Map (weighed)
            interestMap[i] = Mathf.Clamp01(1 - dangerMap[i]) * interestMapWeights[i];

            //And check for the highest interest
            if (interestMap[i] > interest)
            {
                interest = interestMap[i];
                index = i;
            }
        }
        Debug.DrawRay(transform.position + Vector3.up, WorldToLocalDir(contextVectors[index]) * interestMap[index], Color.blue, .02f);

        //Finally, find the most desireble movement direction
        desiredDir = (WorldToLocalDir(contextVectors[index]) * interestMap[index]).sqrMagnitude >= Mathf.Pow(minMovementMagnitudue, 2) ?
                     WorldToLocalDir(contextVectors[index]) : Vector3.zero;
    }

    void PullBack()
    {
        desiredDir = WorldToLocalDir(Vector3.back);
        //Debug.Log(this.gameObject.name + " is Overriding!");
    }

    void MoveTo()
    {
        if (isOverriding)
        {
            isOverriding = false;
        }
        Quaternion lookAt = Quaternion.LookRotation(reference.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, .25f);
        transform.position += desiredDir * speed * Time.deltaTime;
    }

    Vector3 WorldToLocalDir(Vector3 direction)
    {
        return direction.z * transform.forward + direction.x * transform.right + direction.y * transform.up;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position + Vector3.up, avoidanceRadius);
    }
}
