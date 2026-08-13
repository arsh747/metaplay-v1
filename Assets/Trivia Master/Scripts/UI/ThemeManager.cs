using UnityEngine;
using UnityEngine.UIElements;

namespace TriviaGame
{

/// <summary>
/// Applies / persists the light-dark theme for the UI Toolkit UI.
/// The whole theme is just one USS class ("dark-theme") toggled on the
/// root VisualElement — every color in theme.uss is a custom property
/// that flips automatically, so no other script needs to know about
/// colors at all.
/// </summary>
public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;

    const string DarkThemeClass = "dark-theme";
    const string PrefKey = "trivia_dark_theme";

    VisualElement root;
    bool isDark;

    public bool IsDark => isDark;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Called by UIManager once the UIDocument's root is ready.</summary>
    public void Initialize(VisualElement themeRoot)
    {
        root = themeRoot;
        isDark = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        Apply();
    }

    public void SetDark(bool dark)
    {
        isDark = dark;
        PlayerPrefs.SetInt(PrefKey, dark ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    public void ToggleTheme()
    {
        SetDark(!isDark);
    }

    void Apply()
    {
        if (root == null) return;
        root.EnableInClassList(DarkThemeClass, isDark);
    }
}

}
