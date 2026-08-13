using UnityEngine;

namespace TriviaGame
{

/// <summary>
/// Tiny marker component — tells ClayThemeApplier what role this element
/// plays so it knows which color from the theme to apply.
/// Add this to: the screen's background Image, your "Start"/primary
/// buttons, and any text you want auto-colored.
/// </summary>
public class ClayTag : MonoBehaviour
{
    public enum Role { Background, Primary, TextPrimary, TextSecondary }

    public Role role = Role.TextPrimary;
}

}
