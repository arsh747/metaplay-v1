using UnityEngine;

/// <summary>
/// Now UI-independent: since the game targets mobile portrait only, the Options panel
/// no longer exposes Full Screen / Resolution controls (replaced with a Sound Effects
/// toggle in WordGameUI.uxml). This script just quietly applies a sensible native
/// fullscreen resolution on non-mobile platforms (useful if you ever build for
/// Windows/Mac standalone) and otherwise does nothing.
///
/// Safe to remove entirely from the scene if you only ever target mobile.
/// </summary>
public class ResolutionManager : MonoBehaviour {

    private void Awake() {
        if (Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer
            || Application.platform == RuntimePlatform.WebGLPlayer) {
            return;
        }

        Resolution native = Screen.resolutions.Length > 0
            ? Screen.resolutions[Screen.resolutions.Length - 1]
            : Screen.currentResolution;

        Screen.SetResolution(native.width, native.height, FullScreenMode.ExclusiveFullScreen);
    }
}
