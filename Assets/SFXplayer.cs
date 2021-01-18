using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXplayer : MonoBehaviour
{
    AudioSource audio;

    void Start()
    {
        audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySFX()
    {
        audio.Play();
    }

}
