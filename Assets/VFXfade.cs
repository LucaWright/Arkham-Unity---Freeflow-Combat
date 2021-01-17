using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXfade : MonoBehaviour
{
    Material mat;

    private void Awake()
    {
        mat = transform.GetComponent<MeshRenderer>().material;
    }

    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnFX()
    {
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        this.gameObject.SetActive(true);
        yield return new WaitForSeconds(.75f);
        this.gameObject.SetActive(false);
    }


}
