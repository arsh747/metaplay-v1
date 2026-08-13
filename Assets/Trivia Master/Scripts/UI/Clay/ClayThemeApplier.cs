using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TriviaGame
{

/// <summary>
/// Drop this on the ROOT of each screen (MainMenuPanel, QuestionPanel,
/// CategoryPanel, etc). On Start, it walks all children and:
///   - applies the theme's gradient/background color to the screen's
///     background Image (if tagged "ClayBackground")
///   - applies cardColor + shadow settings to every ClayElement found
///   - applies primaryBtn gradient to elements tagged "ClayPrimary"
///   - applies textPrimary/textSecondary to TMP texts tagged accordingly
///
/// === SETUP ===
/// 1. Add this to the root Panel of each screen.
/// 2. Assign your ClayTheme asset.
/// 3. Tag elements using the small ClayTag component (see below) so
///    this script knows which role each piece plays — OR just rely on
///    defaults: any ClayElement gets cardColor automatically, you only
///    need ClayTag for backgrounds and primary (highlighted) buttons.
/// </summary>
public class ClayThemeApplier : MonoBehaviour
{
    public ClayTheme theme;

    void Start()
    {
        if (theme == null) return;
        Apply();
    }

    [ContextMenu("Apply Theme Now")]
    public void Apply()
    {
        if (theme == null) return;

        // Apply to every ClayElement in this screen
        ClayElement[] clayElements = GetComponentsInChildren<ClayElement>(true);
        foreach (var clay in clayElements)
        {
            ClayTag tag = clay.GetComponent<ClayTag>();
            bool isPrimary = tag != null && tag.role == ClayTag.Role.Primary;

            clay.baseColor        = isPrimary ? theme.primaryBtn : theme.cardColor;
            clay.darkShadowColor  = theme.darkShadow;
            clay.lightShadowColor = theme.lightShadow;
            clay.darkOffset       = new Vector2(theme.shadowOffset.x, -theme.shadowOffset.y);
            clay.lightOffset      = new Vector2(-theme.shadowOffset.x, theme.shadowOffset.y);
            clay.Rebuild();
        }

        // Apply text colors
        ClayTag[] taggedTexts = GetComponentsInChildren<ClayTag>(true);
        foreach (var tag in taggedTexts)
        {
            TMP_Text text = tag.GetComponent<TMP_Text>();
            if (text == null) continue;

            switch (tag.role)
            {
                case ClayTag.Role.TextPrimary:
                    text.color = theme.textPrimary;
                    break;
                case ClayTag.Role.TextSecondary:
                    text.color = theme.textSecondary;
                    break;
                case ClayTag.Role.Primary:
                    text.color = Color.white; // text on top of primary button
                    break;
            }
        }

        // Apply background gradient if a ClayBackground-tagged Image exists
        ClayTag[] allTags = taggedTexts; // reuse search results scope is fine since GetComponentsInChildren already covers all
        foreach (var tag in GetComponentsInChildren<ClayTag>(true))
        {
            if (tag.role != ClayTag.Role.Background) continue;
            Image bg = tag.GetComponent<Image>();
            if (bg != null) bg.color = theme.screenBackgroundTop;
        }
    }
}

}
