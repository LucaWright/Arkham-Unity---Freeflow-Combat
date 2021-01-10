using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesManager : MonoBehaviour
{
    public GameObject playerHitParticles;
    public GameObject thugHitParticles;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPlayerHit(Vector3 origin)
    {
        Instantiate(playerHitParticles, origin, Quaternion.identity);
    }
}
