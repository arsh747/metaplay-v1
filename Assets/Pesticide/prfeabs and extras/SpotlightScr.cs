using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotlightScr : MonoBehaviour
{
    public GameObject theplayer;
    // Update is called once per frame
    void Update()
    {
        transform.LookAt(theplayer.transform);
    }
}
