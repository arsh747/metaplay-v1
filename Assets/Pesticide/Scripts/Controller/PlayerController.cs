using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PesticideGame
{

/// <summary>
/// This class handles the movement of the player with given input from the input manager
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 2f;
    // public float lookSpeed = 60f;
    public float jumpSpeed = 8f;
    public float gravity = 9.81f;
    [Header("Jump Timing")]
    public float jumpTimeLeniency = 0.1f;
    float timeToStopLenient = 0;
    [Header("Required References")]
    public Shooter playerShooter;
    public Health playerHealth;
    public List<GameObject> disableWhileDead;
    bool doubleJumpAv =false;
    // public Slider slider;
    // public Transform pb;
    // float xRotation = 0f;

    private CharacterController controller;
    private InputManager inputManager;
    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first Update call
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    void Start()
    {
        // lookSpeed = PlayerPrefs.GetFloat("currentSensitivity",100);
        // slider.value = lookSpeed/10;
        // Cursor.lockState = CursorLockMode.Locked;
        SetUpCharacterContoller();
        SetUpInputManager(); 
    }

    private void SetUpCharacterContoller()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("This player object does not have character controller script on it");
        }
    }

    void SetUpInputManager()
    {
        inputManager = InputManager.instance;
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once every frame
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    void Update()
    {
        // PlayerPrefs.SetFloat("currentSensitivity",lookSpeed);
        // float mouseX = Input.GetAxis("Mouse X")*lookSpeed*Time.deltaTime;
        // float mouseY = Input.GetAxis("Mouse Y")*lookSpeed*Time.deltaTime;

        // xRotation -= mouseY;
        // xRotation = Mathf.Clamp(xRotation,-90f,90f);
        // transform.localRotation = Quaternion.Euler(xRotation,0f,0f);
        // pb.Rotate(Vector3.up*mouseX);

        if(playerHealth.currentHealth <= 0)
        {
            foreach(GameObject inGameObject in disableWhileDead)
            {
                inGameObject.SetActive(false);
            }
            return;
        }

        else
        {
            foreach(GameObject inGameObject in disableWhileDead)
            {
                inGameObject.SetActive(true);
            }
        }

        ProcessMovement();
        //ProcessRotation();
    }

    Vector3 moveDirection;

    void ProcessMovement()
    {
        float leftrightinput = inputManager.horizontalMoveAxis;
        float forwardbackwardinput = inputManager.verticalMoveAxis;
        bool jumpPressed = inputManager.jumpPressed;

        if (controller.isGrounded)
        {
            doubleJumpAv = true;
            timeToStopLenient = Time.time + jumpTimeLeniency;
            moveDirection = new Vector3 (leftrightinput,0,forwardbackwardinput);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection = moveDirection * moveSpeed;

            if (jumpPressed)
            {
                moveDirection.y = jumpSpeed;
            }
        }

        else 
        {
            moveDirection = new Vector3(leftrightinput * moveSpeed, moveDirection.y, forwardbackwardinput * moveSpeed);
            moveDirection = transform.TransformDirection(moveDirection);

            if(jumpPressed && Time.time < timeToStopLenient)
            {
                moveDirection.y = jumpSpeed;
            }

            else if(jumpPressed && doubleJumpAv)
            {
                moveDirection.y = jumpSpeed;
                doubleJumpAv = false;
            }
        }

        moveDirection.y -= gravity * Time.deltaTime;

        if (controller.isGrounded && moveDirection.y < 0 )
        {
            moveDirection.y = -0.3f;
        }
        controller.Move(moveDirection * Time.deltaTime);
    }

    // void ProcessRotation()
    // {
    //     float horizontalLookInput = inputManager.horizontalLookAxis;
    //     Vector3 playerRotation = transform.rotation.eulerAngles;
    //     transform.rotation = Quaternion.Euler(new Vector3(playerRotation.x,playerRotation.y + horizontalLookInput * lookSpeed * Time.deltaTime , playerRotation.z));
    // }

    // public void Adjustls(float newSpeed)
    // {
    //     lookSpeed = newSpeed*10;
    // }
}

}
