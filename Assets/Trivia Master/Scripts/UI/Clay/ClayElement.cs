using UnityEngine;
using UnityEngine.UI;

namespace TriviaGame
{

/// <summary>
/// Turns any RectTransform with an Image into a "clay" UI element:
/// a soft inflated look made of two offset blurred-shadow copies
/// (one dark, bottom-right; one light, top-left) sitting behind the
/// main shape — the core trick of claymorphism, since Unity UI has
/// no native dual-direction box-shadow.
///
/// IMPORTANT DESIGN CHOICE: shadow layers are created as CHILDREN of
/// this element (not siblings). This is critical when the button sits
/// inside a Vertical/Horizontal/Grid Layout Group — if shadows were
/// siblings, the layout group would try to lay them out as their own
/// rows/columns, causing buttons to compress, wrap, or fly off to
/// huge negative positions. As children, they're invisible to the
/// parent layout group entirely and just render behind the button's
/// own Image within its own rect.
///
/// === SETUP ===
/// 1. Import a single ROUNDED-RECT sprite (provided: clay_rounded_rect.png)
///    into Assets/Sprites, set Texture Type: Sprite (2D and UI).
/// 2. On ANY existing Button or Panel that already has an Image
///    component, just Add Component → ClayElement.
/// 3. Assign [roundedSprite] to that same rounded-rect sprite.
/// 4. If this element sits inside a Layout Group (e.g. a vertical
///    button list) and needs a FIXED size rather than whatever the
///    layout group assigns, check [overrideSize] and set [fixedSize].
/// 5. Press Play (or it rebuilds live in the Editor) — done.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class ClayElement : MonoBehaviour
{
    [Header("Sprite")]
    [Tooltip("A simple white rounded-rectangle sprite (provided). " +
             "Used for the shadow layers; your main Image can use any sprite.")]
    public Sprite roundedSprite;

    [Header("Clay Palette")]
    [Tooltip("Background color of the element itself.")]
    public Color baseColor = new Color(0.957f, 0.965f, 0.988f); // #F4F6FC

    [Tooltip("Dark shadow color (bottom-right) — usually a muted blue-grey.")]
    public Color darkShadowColor = new Color(0.667f, 0.706f, 0.824f, 0.55f);

    [Tooltip("Light highlight color (top-left) — usually near-white.")]
    public Color lightShadowColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Shadow Offsets (pixels)")]
    public Vector2 darkOffset = new Vector2(7, -7);
    public Vector2 lightOffset = new Vector2(-7, 7);

    [Header("Image Settings")]
    public Image.Type imageType = Image.Type.Simple;

    [Header("Size Override (use when inside a Layout Group)")]
    [Tooltip("If enabled, forces this element to a fixed pill-button size " +
             "via LayoutElement, so a parent Vertical/Horizontal/Grid " +
             "Layout Group sizes it correctly instead of stretching it " +
             "full-width.")]
    public bool overrideSize = false;
    public Vector2 fixedSize = new Vector2(190, 50);

    const string SHADOW_DARK_NAME = "ClayShadowDark__ClayGenerated";
    const string SHADOW_LIGHT_NAME = "ClayShadowLight__ClayGenerated";

    void OnEnable()
    {
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
    }
#endif

    [ContextMenu("Rebuild Clay Effect")]
    public void Rebuild()
    {
        if (roundedSprite == null) return;

        // Apply size override via LayoutElement — this is respected by
        // any parent Layout Group without fighting its own positioning
        // logic (unlike directly setting anchors/sizeDelta, which a
        // Layout Group overwrites every rebuild anyway).
        if (overrideSize)
        {
            LayoutElement le = GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = fixedSize.x;
            le.preferredHeight = fixedSize.y;
            le.minWidth = fixedSize.x;
            le.minHeight = fixedSize.y;
        }

        Image mainImage = GetComponent<Image>();
        mainImage.color = baseColor;
        mainImage.sprite = roundedSprite;
        mainImage.type = imageType;

        BuildShadowChild(SHADOW_LIGHT_NAME, lightShadowColor, lightOffset, siblingIndex: 0);
        BuildShadowChild(SHADOW_DARK_NAME, darkShadowColor, darkOffset, siblingIndex: 1);
    }

    void BuildShadowChild(string name, Color color, Vector2 offset, int siblingIndex)
    {
        // Find existing shadow child by name (created on a previous
        // Rebuild) so we update in place instead of duplicating.
        Transform existing = transform.Find(name);
        GameObject shadowGO = existing != null ? existing.gameObject : null;

        if (shadowGO == null)
        {
            shadowGO = new GameObject(name, typeof(RectTransform));
            shadowGO.transform.SetParent(transform, false);
        }

        RectTransform shadowRect = shadowGO.GetComponent<RectTransform>();

        // Fill the ENTIRE parent (this button) — stretched to 0,0 / 1,1 —
        // then nudge via anchoredPosition. Since it's a child, "parent"
        // here means THIS element's own rect, not the Layout Group's
        // rect, so the Layout Group never sees or measures this object.
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = Vector2.zero;
        shadowRect.anchoredPosition = offset;
        shadowRect.localScale = Vector3.one;

        Image shadowImage = shadowGO.GetComponent<Image>();
        if (shadowImage == null) shadowImage = shadowGO.AddComponent<Image>();
        shadowImage.sprite = roundedSprite;
        shadowImage.type = imageType;
        shadowImage.color = color;
        shadowImage.raycastTarget = false;

        // Shadows must never be measured by a Layout Group even though
        // they're children of this element (which itself might be a
        // Layout Group target) — defensive safety, harmless either way.
        LayoutElement shadowLE = shadowGO.GetComponent<LayoutElement>();
        if (shadowLE == null) shadowLE = shadowGO.AddComponent<LayoutElement>();
        shadowLE.ignoreLayout = true;

        // Light shadow furthest back (index 0), dark shadow above it
        // (index 1), so the rendering order is: light, dark, then the
        // main element's own Image (which is on this same GameObject
        // and always renders via its own Graphic, unaffected by child
        // sibling order).
        shadowGO.transform.SetSiblingIndex(siblingIndex);
    }

    void OnDestroy()
    {
        // Children are destroyed automatically with this GameObject —
        // nothing extra to clean up.
    }
}
}
