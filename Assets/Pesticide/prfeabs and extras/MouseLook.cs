using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseLook : MonoBehaviour
{
public Slider slider;
    public Transform pb;
    float xRotation = 0f;
        public float lookSpeed = 60f;


    // Start is called before the first frame update
    void Start()
    {
        lookSpeed = PlayerPrefs.GetFloat("currentSensitivity",100);
        slider.value = lookSpeed/10;
    }

    // Update is called once per frame
    void Update()
    {
PlayerPrefs.SetFloat("currentSensitivity",lookSpeed);
        float mouseX = Input.GetAxis("Mouse X")*lookSpeed*Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y")*lookSpeed*Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation,-90f,90f);
        transform.localRotation = Quaternion.Euler(xRotation,0f,0f);
        pb.Rotate(Vector3.up*mouseX);
    }

        public void Adjustls(float newSpeed)
    {
        lookSpeed = newSpeed*50;
    }
}
