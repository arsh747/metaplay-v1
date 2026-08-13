using UnityEngine;

namespace TriviaGame
{

/// <summary>
/// Pushes a RectTransform down/up/sideways to clear the device's safe area
/// (notches, rounded corners, home indicator) WITHOUT altering its anchor
/// type. Works correctly for both:
///   - Stretched bars (e.g. TopBar: anchored full-width, fixed height)
///   - Point-anchored elements (e.g. a title text anchored top-center)
///
/// This fixes the earlier version, which directly overwrote anchorMin/Max
/// and produced an inverted (Min > Max) anchor box for point-anchored
/// elements like a title label � causing it to render in the wrong place
/// and get clipped by the notch.
///
/// === SETUP ===
/// Add this to any UI element that touches a screen edge: TopBar, a
/// title text under a notch, a bottom button row, etc.
/// Leave the element's own anchors/pivot exactly as you originally
/// designed them � this script only nudges its position via
/// anchoredPosition, never changes anchorMin/anchorMax.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform rect;
    Vector2 originalAnchoredPosition;
    bool initialized = false;

    Rect lastSafeArea = new Rect(0, 0, 0, 0);
    Vector2Int lastScreenSize = new Vector2Int(0, 0);
    ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    [Tooltip("Push down to clear a TOP notch/cutout. ON for title bars, " +
             "headers, or any text/icon sitting near the top edge.")]
    public bool applyTop = true;

    [Tooltip("Push up to clear a BOTTOM inset (home indicator). " +
             "ON for bottom button rows.")]
    public bool applyBottom = false;

    [Tooltip("Push inward to clear LEFT/RIGHT insets (landscape notch). " +
             "Usually OFF for portrait-only games.")]
    public bool applySides = false;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalAnchoredPosition = rect.anchoredPosition;
        initialized = true;
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (!initialized) return;

        Rect safeArea = Screen.safeArea;

        if (safeArea != lastSafeArea ||
            Screen.width != lastScreenSize.x ||
            Screen.height != lastScreenSize.y ||
            Screen.orientation != lastOrientation)
        {
            lastScreenSize.x = Screen.width;
            lastScreenSize.y = Screen.height;
            lastOrientation = Screen.orientation;
            lastSafeArea = safeArea;

            ApplySafeArea(safeArea);
        }
    }

    void ApplySafeArea(Rect safeArea)
    {
        // Inset distances in pixels, from each screen edge to the safe area.
        float topInset = Screen.height - (safeArea.y + safeArea.height);
        float bottomInset = safeArea.y;
        float leftInset = safeArea.x;
        float rightInset = Screen.width - (safeArea.x + safeArea.width);

        Vector2 offset = Vector2.zero;

        if (applyTop) offset.y -= topInset;
        if (applyBottom) offset.y += bottomInset;
        if (applySides)
        {
            offset.x += leftInset;
            offset.x -= rightInset;
        }

        rect.anchoredPosition = originalAnchoredPosition + offset;
    }
}
}
