using UnityEngine;

namespace TriviaGame
{

/// <summary>
/// Centralized clay color palette — create ONE asset, drag it onto every
/// ClayThemeApplier in your scenes, and changing colors here updates
/// every screen at once instead of hunting through each panel manually.
///
/// === SETUP ===
/// Right-click in Project window → Create → Clay UI → Theme
/// Fill in the fields below (defaults match Direction 6 from the mockup).
/// </summary>
[CreateAssetMenu(fileName = "ClayTheme", menuName = "Clay UI/Theme")]
public class ClayTheme : ScriptableObject
{
    [Header("Backgrounds")]
    public Color screenBackgroundTop    = new Color(0.867f, 0.902f, 0.961f); // #DDE6F5
    public Color screenBackgroundBottom = new Color(0.933f, 0.941f, 0.980f); // #EEF0FA

    [Header("Element Base Colors")]
    public Color cardColor   = new Color(0.957f, 0.965f, 0.988f); // #F4F6FC
    public Color primaryBtn  = new Color(0.545f, 0.576f, 1f);     // #8B93FF
    public Color primaryBtn2 = new Color(0.420f, 0.420f, 1f);     // #6B6BFF (gradient end)

    [Header("Text")]
    public Color textPrimary   = new Color(0.235f, 0.247f, 0.357f); // #3C3F5C
    public Color textSecondary = new Color(0.545f, 0.576f, 0.788f); // #8B93C9

    [Header("Shadow Tuning")]
    public Color darkShadow  = new Color(0.667f, 0.706f, 0.824f, 0.55f);
    public Color lightShadow = new Color(1f, 1f, 1f, 0.95f);
    public Vector2 shadowOffset = new Vector2(7, 7);
}

}
