using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Standalone Android camera look script. Attach this directly to the SAME
/// GameObject MouseLook.cs was on (usually the Main Camera). Does the same
/// job as MouseLook.cs (rotates camera up/down, player body left/right) but
/// reads raw touch input instead of a mouse.
/// </summary>
/// <remarks>
/// SETUP:
/// 1. Disable (or remove) the MouseLook component on this same GameObject -
///    it reads Input.GetAxis("Mouse X"/"Mouse Y") which doesn't exist on touch
///    devices and will throw errors.
/// 2. Add this TouchLook script to the same GameObject (Main Camera).
/// 3. Assign "pb" (player body) exactly like MouseLook's own "pb" field was
///    assigned - the Transform that should rotate left/right.
/// 4. If you use the sensitivity slider, wire its OnValueChanged(Single) to
///    THIS script's Adjustls method instead of MouseLook's.
/// </remarks>
public class TouchLook : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("The player's body transform, rotated left/right (same as MouseLook's 'pb')")]
    public Transform pb;
    [Tooltip("Optional sensitivity slider")]
    public Slider slider;

    [Header("Settings")]
    [Tooltip("Degrees rotated per pixel of finger movement. 0.02-0.04 feels like a typical mobile shooter.")]
    [Range(0.005f, 1f)]
    public float lookSpeed = 0.03f;
    [Tooltip("Only touches that begin at or beyond this X position (in pixels from the left edge) can start a look-drag. Set this to just past the right edge of your joystick/buttons so it can never grab their touches.")]
    public float lookZoneStartX = 500f;

    private float xRotation = 0f;
    private int lookTouchId = -1;
    private Vector2 lastTouchPos;

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first Update call.
    /// Loads saved sensitivity, same behavior as MouseLook.Start()
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    void Start()
    {
        lookSpeed = PlayerPrefs.GetFloat("touchLookSensitivity", lookSpeed);
        if (slider != null)
        {
            slider.value = lookSpeed;
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once every frame. Scans raw touches -
    /// any touch that begins at or past lookZoneStartX is adopted as the look
    /// touch and its movement rotates the camera/player body.
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    void Update()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.fingerId != lookTouchId)
            {
                if (touch.phase != TouchPhase.Began) continue;
                if (touch.position.x < lookZoneStartX) continue;
                if (lookTouchId != -1) continue;

                lookTouchId = touch.fingerId;
                lastTouchPos = touch.position;
                continue;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 delta = touch.position - lastTouchPos;
                ApplyLook(delta);
                lastTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lookTouchId = -1;
            }
        }
    }

    /// <summary>
    /// Description:
    /// Applies a raw screen-space delta to the camera pitch and player body yaw,
    /// same math as MouseLook.Update().
    /// Input:
    /// Vector2 screenDelta
    /// Return:
    /// void (no return)
    /// </summary>
    void ApplyLook(Vector2 screenDelta)
    {
        float mouseX = screenDelta.x * lookSpeed;
        float mouseY = screenDelta.y * lookSpeed;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (pb != null)
        {
            pb.Rotate(Vector3.up * mouseX);
        }
    }

    /// <summary>
    /// Description:
    /// Adjusts look sensitivity from a UI slider, same behavior as MouseLook.Adjustls()
    /// Input:
    /// float newSpeed
    /// Return:
    /// void (no return)
    /// </summary>
    public void Adjustls(float newSpeed)
    {
        lookSpeed = newSpeed;
        PlayerPrefs.SetFloat("touchLookSensitivity", lookSpeed);
    }
}