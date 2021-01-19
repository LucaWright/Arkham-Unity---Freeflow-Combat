using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class CrowdTestScript : MonoBehaviour
{
    public Transform reference;

    float timer = 0;
    public float everyXseconds = .5f;

    public float speed;
    public float avoidanceRadius;
    public LayerMask avoidanceMask;

    public Vector3 movementVector = Vector3.zero;

    CapsuleCollider collider;
    
    
    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > everyXseconds)
        {
            timer = 0;
            CheckSurroundings();
        }
        MoveTo();
    }

    void CheckSurroundings()
    {
        collider.enabled = false;
        Collider[] agents = Physics.OverlapSphere(transform.position + Vector3.up, avoidanceRadius, avoidanceMask);
        collider.enabled = true;

        Debug.Log("Colliders: " + agents.Length);

        if (agents.Length > 0)
        {
            Vector3[] distanceVectors = new Vector3[agents.Length];

            for (int i = 0; i < agents.Length; i++)
            {
                Transform agentTransform = agents[i].transform;
                distanceVectors[i] = transform.position - agentTransform.position;
            }

            movementVector = Vector3.zero;

            for (int i = 0; i < distanceVectors.Length; i++)
            {
                movementVector += distanceVectors[i];
            }            
        }
        else
        {
            movementVector = transform.forward;
        }
    }

    void MoveTo()
    {
        Quaternion lookAt = Quaternion.LookRotation(reference.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, .25f);
        transform.position += movementVector.normalized * speed * Time.deltaTime; //Anziché normalizzare e basta, vedi anche di calcolare modifiche alla speed;

        Debug.DrawRay(transform.position + Vector3.up, movementVector * avoidanceRadius, Color.green, .02f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + Vector3.up, avoidanceRadius);
    }
}
