using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using PinePie.SimpleJoystick;
using PesticideGame;

/// <summary>
/// ALL Android touch controls in a single script: movement joystick, camera
/// look (drag), and action buttons (Fire, Jump, Pause, Next/Previous Weapon).
/// Drives InputManager's public fields directly - PlayerController.cs and
/// Shooter.cs need ZERO changes.
///
/// This is plain C#, no FSM/PlayMaker involved.
/// </summary>
/// <remarks>
/// SETUP (one time):
/// 1. Create an empty GameObject anywhere in the scene, name it "TouchControls",
///    and add THIS script to it.
/// 2. Movement joystick is the existing "PinePie Joystick" asset already in your
///    hierarchy - it handles its own touch input. Just drag its JoystickController
///    component (on the "PinePie Joystick" object) into the "joystick" field below.
/// 3. Camera look is handled by the separate "TouchLook.cs" script - add that
///    directly to the Main Camera (same place MouseLook.cs was on).
/// 4. Buttons (Fire, Jump, Pause, Next Weapon, Previous Weapon): just drag each
///    button GameObject straight from the Hierarchy into the matching field
///    below (fireButton, jumpButton, etc). NOTHING needs to be added to the
///    button objects themselves - this script wires them up automatically at
///    runtime.
/// 5. Disable the old MouseLook component on Android (it relies on a mouse,
///    which doesn't exist on touch devices).
/// </remarks>
public class TouchControls : MonoBehaviour
{
    // ---------------------------------------------------------------
    // JOYSTICK (Movement) - uses the existing PinePie Simple Joystick asset
    // ---------------------------------------------------------------
    [Header("Joystick - Movement (PinePie asset)")]
    [Tooltip("Drag the JoystickController component from the 'PinePie Joystick' GameObject here")]
    public JoystickController joystick;

    /// <summary>
    /// Description: Standard Unity function called every frame. Reads the PinePie joystick's current InputDirection and pushes it into InputManager.
    /// Input: none
    /// Return: void (no return)
    /// </summary>
    void Update()
    {
        if (joystick != null && InputManager.instance != null)
        {
            InputManager.instance.horizontalMoveAxis = joystick.InputDirection.x;
            InputManager.instance.verticalMoveAxis = joystick.InputDirection.y;
        }
    }

    // ---------------------------------------------------------------
    // BUTTONS (Fire, Jump, Pause, Next/Previous Weapon)
    // Just drag each button GameObject here - nothing needs to be
    // added to the buttons themselves, this script wires them up.
    // ---------------------------------------------------------------
    [Header("Buttons - drag button GameObjects here")]
    public GameObject fireButton;
    public GameObject jumpButton;
    public GameObject pauseButton;
    public GameObject nextWeaponButton;
    public GameObject previousWeaponButton;

    /// <summary>
    /// Description: Standard Unity function called when the script instance is loaded. Automatically wires up PointerDown/PointerUp events on every assigned button by adding an EventTrigger component to each at runtime.
    /// Input: none
    /// Return: void (no return)
    /// </summary>
    void Awake()
    {
        SetupButton(fireButton, OnFireDown, OnFireUp);
        SetupButton(jumpButton, OnJumpDown, OnJumpUp);
        SetupButton(pauseButton, OnPauseDown, null);
        SetupButton(nextWeaponButton, OnNextWeaponDown, null);
        SetupButton(previousWeaponButton, OnPreviousWeaponDown, null);
    }

    /// <summary>
    /// Description: Adds an EventTrigger component to the given button (if it doesn't already have one) and wires up the given down/up callbacks.
    /// Input: GameObject buttonObj, UnityAction&lt;BaseEventData&gt; downAction, UnityAction&lt;BaseEventData&gt; upAction
    /// Return: void (no return)
    /// </summary>
    /// <param name="buttonObj">The button GameObject to wire up</param>
    /// <param name="downAction">Method to call on PointerDown</param>
    /// <param name="upAction">Method to call on PointerUp (pass null if not needed)</param>
    void SetupButton(GameObject buttonObj, UnityAction<BaseEventData> downAction, UnityAction<BaseEventData> upAction)
    {
        if (buttonObj == null)
        {
            return;
        }

        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener(downAction);
        trigger.triggers.Add(downEntry);

        if (upAction != null)
        {
            EventTrigger.Entry upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener(upAction);
            trigger.triggers.Add(upEntry);
        }
    }

    void OnFireDown(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.firePressed = true;
        InputManager.instance.fireHeld = true;
        StartCoroutine(ResetFlagNextFrame(() => InputManager.instance.firePressed = false));
    }

    void OnFireUp(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.fireHeld = false;
    }

    void OnJumpDown(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.jumpPressed = true;
        InputManager.instance.jumpHeld = true;
        StartCoroutine(ResetFlagNextFrame(() => InputManager.instance.jumpPressed = false));
    }

    void OnJumpUp(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.jumpHeld = false;
    }

    void OnPauseDown(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.pausePressed = true;
        StartCoroutine(ResetFlagNextFrame(() => InputManager.instance.pausePressed = false));
    }

    void OnNextWeaponDown(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.nextWeaponPressed = true;
        StartCoroutine(ResetFlagNextFrame(() => InputManager.instance.nextWeaponPressed = false));
    }

    void OnPreviousWeaponDown(BaseEventData data)
    {
        if (InputManager.instance == null) return;
        InputManager.instance.previousWeaponPressed = true;
        StartCoroutine(ResetFlagNextFrame(() => InputManager.instance.previousWeaponPressed = false));
    }

    /// <summary>
    /// Description: Resets a single-frame "pressed" pulse one frame later, matching how InputManager resets its own keyboard/gamepad pressed flags.
    /// Input: System.Action resetAction
    /// Return: IEnumerator
    /// </summary>
    private IEnumerator ResetFlagNextFrame(System.Action resetAction)
    {
        yield return new WaitForEndOfFrame();
        resetAction.Invoke();
    }

}