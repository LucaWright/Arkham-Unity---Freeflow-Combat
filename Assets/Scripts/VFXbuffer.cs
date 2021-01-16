using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXbuffer : MonoBehaviour
{
    public Queue<ParticleSystem> particlesRingBuffer;

    private void Awake()
    {
        particlesRingBuffer = new Queue<ParticleSystem>();

        foreach (Transform trans in transform)
        {
            particlesRingBuffer.Enqueue(trans.GetComponent<ParticleSystem>());
        }
    }

    public void SpawnVFX(Transform t)
    {
        var vfx = particlesRingBuffer.Dequeue();
        vfx.transform.position = t.position;
        vfx.Play();
        particlesRingBuffer.Enqueue(vfx);
    }
}
