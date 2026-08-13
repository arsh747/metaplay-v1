using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TriviaGame
{

/// <summary>
/// MASTER CLAY THEMER — v5
///
/// CHANGES FROM v4:
/// - Shadows REMOVED entirely (flat clay look, no dual-shadow depth) —
///   simpler, cleaner, matches the "remove shadows/highlights" request.
///   Old shadow-generation code path still exists but is gated behind
///   [useShadows] = false by default; flip it back on if you change
///   your mind later, no need to re-write anything.
/// - Button spacing is now ALWAYS applied via Vertical Layout Group
///   spacing, even when the group wasn't previously "fixed" by this
///   script (v4 only set spacing on groups it touched while sizing
///   buttons — now it's unconditional per-panel).
/// - Titles are now BOLD by default (FontStyles.Bold) and bigger.
/// - QuestionPanel-style text (title/sub) gets a HIGH-CONTRAST color
///   override path: if the title sits on a light card, force dark
///   text; this fixes "white text on light background" readability.
/// - Toggle building is now FULLY DESTRUCTIVE-SAFE: instead of trying
///   to find-and-reuse a possibly-broken existing Background/Knob
///   child (which is what silently failed for Music), it now ALWAYS
///   deletes any existing children matching expected names first, then
///   builds fresh ones from scratch every single time. This guarantees
///   both toggles end up structurally identical after Apply, no matter
///   what state they were in before — fixes the "Music doesn't work,
///   SFX does" asymmetry, which was caused by one toggle silently
///   reusing a stale/broken Image reference.
/// </summary>
public class MasterClayThemer : MonoBehaviour
{
    [System.Serializable]
    public class PanelConfig
    {
        [Header("Identification")]
        public string panelName = "New Panel";

        [Header("Targets")]
        public Image background;
        public bool backgroundIsCard = false;

        public TMP_Text titleText;
        public TMP_Text subText;

        public List<Button> regularButtons = new List<Button>();
        public List<Button> primaryButtons = new List<Button>();
        public List<Toggle> toggles = new List<Toggle>();

        [Header("Manual Size Lock")]
        [Tooltip("If checked, this script will NOT touch button/toggle width, " +
                 "height, or LayoutElement sizing for this panel at all -- it " +
                 "still applies colors, sprites, text styling, and spacing, but " +
                 "leaves whatever size YOU set manually in the Inspector alone. " +
                 "Use this for panels where the auto-computed fraction-based " +
                 "size doesn't look right and you'd rather hand-tune it once.")]
        public bool lockManualSize = false;

        [Header("Responsive Button Sizing")]
        [Tooltip("Button width as a fraction of screen width. TUNE PER PANEL: " +
                 "a busy screen (5 category buttons) can use a wider value " +
                 "(0.75-0.85); a sparse screen with just 1-2 buttons (Settings' " +
                 "Back button) should use a SMALLER value (0.45-0.55) so the " +
                 "single button doesn't look oversized relative to the empty " +
                 "space around it.")]
        [Range(0.1f, 1f)]
        public float buttonWidthFraction = 0.65f;

        [Tooltip("Button height as a fraction of screen height. Same per-panel " +
                 "tuning logic as width -- sparse screens want smaller values.")]
        [Range(0.02f, 0.2f)]
        public float buttonHeightFraction = 0.06f;
        public int gridColumns = 1;

        [Tooltip("Vertical gap between stacked buttons, as a fraction of screen height. " +
                 "Now ALWAYS applied to this panel's button container, even if it wasn't " +
                 "otherwise modified.")]
        [Range(0.01f, 0.1f)]
        public float buttonSpacingFraction = 0.045f;

        [Header("Responsive Toggle Sizing")]
        [Tooltip("TUNE PER PANEL same as buttons above -- sparse screens want smaller toggles.")]
        [Range(0.08f, 0.35f)]
        public float toggleWidthFraction = 0.14f;
        [Range(0.02f, 0.1f)]
        public float toggleHeightFraction = 0.035f;
    }

    [Header("Shared Resources")]
    public Sprite roundedSprite;
    public Canvas targetCanvas;

    [Header("Android Reference")]
    public Vector2 androidFallbackReference = new Vector2(412f, 915f);
    public Vector2 minSafeReference = new Vector2(360f, 740f);

    [Header("Full-Screen Auto-Detection")]
    [Range(0.5f, 1f)]
    public float fullScreenThreshold = 0.85f;

    [Header("Palette")]
    public Color screenBackground = new Color(0.804f, 0.835f, 0.937f);
    [Tooltip("Card/button base color. Must read as visibly DIFFERENT from whatever " +
             "background it sits on (including pure white panel backgrounds) or " +
             "buttons will look 'dull'/washed-out with no contrast.")]
    public Color cardColor = new Color(0.886f, 0.902f, 0.961f);
    public Color primaryButtonColor = new Color(0.486f, 0.522f, 1f);
    public Color textPrimary = new Color(0.122f, 0.133f, 0.235f); // darkened further for contrast
    public Color textSecondary = new Color(0.380f, 0.412f, 0.627f);
    public Color textOnPrimary = Color.white;

    [Header("Shadows (now OFF by default per request)")]
    public bool useShadows = false;
    public Color darkShadowColor = new Color(0.55f, 0.59f, 0.75f, 0.65f);
    public Color lightShadowColor = new Color(1f, 1f, 1f, 1f);
    [Range(0.05f, 0.3f)]
    public float shadowOffsetFraction = 0.14f;

    [Header("Toggle Colors")]
    public Color toggleOffColor = new Color(0.80f, 0.81f, 0.88f);
    public bool toggleOnUsesPrimaryColor = true;
    public Color toggleOnColor = new Color(0.486f, 0.522f, 1f);
    public Color toggleKnobColor = Color.white;

    [Header("Typography")]
    [Range(0.5f, 2f)]
    public float globalFontScale = 1.2f;

    [Tooltip("Titles render BOLD at this size (before globalFontScale).")]
    public float titleFontSize = 58f;
    public bool titleBold = true;

    public float subTextFontSize = 36f;
    public float buttonFontSizeMax = 32f;
    public float buttonFontSizeMin = 22f;
    public bool useAutoSizeForButtons = true;
    [Range(0f, 0.3f)]
    public float buttonTextPaddingFraction = 0.1f;

    [Header("Panels")]
    public List<PanelConfig> panels = new List<PanelConfig>();

    [Header("Behavior")]
    public bool applyOnStart = true;

    Vector2 ReferenceResolution
    {
        get
        {
            if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
            Vector2 res = androidFallbackReference;

            if (targetCanvas != null)
            {
                CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    res = scaler.referenceResolution;
                else
                {
                    RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
                    if (canvasRect != null) res = canvasRect.rect.size;
                }
            }

            res.x = Mathf.Max(res.x, minSafeReference.x);
            res.y = Mathf.Max(res.y, minSafeReference.y);
            return res;
        }
    }

    void Start()
    {
        if (applyOnStart) ApplyAll();
    }

    [ContextMenu("Apply Clay Theme To Everything")]
    public void ApplyAll()
    {
        if (roundedSprite == null)
        {
            Debug.LogWarning("MasterClayThemer: Rounded Sprite is not assigned -- aborting.");
            return;
        }

        foreach (var panel in panels) ApplyPanel(panel);

        Debug.Log($"MasterClayThemer: Applied theme to {panels.Count} panel(s). Reference: {ReferenceResolution}");
    }

    void ApplyPanel(PanelConfig panel)
    {
        if (panel.background != null)
        {
            bool actuallyCard = panel.backgroundIsCard && !IsEffectivelyFullScreen(panel.background);

            if (panel.backgroundIsCard && !actuallyCard)
                Debug.Log($"MasterClayThemer: '{panel.panelName}' background forced FLAT (covers most of screen).");

            if (actuallyCard) ApplyCardStyle(panel.background.gameObject);
            else ApplyFlatBackground(panel.background);
        }

        if (panel.titleText != null)
        {
            panel.titleText.color = textPrimary; // always force dark, high-contrast text
            panel.titleText.fontSize = titleFontSize * globalFontScale;
            panel.titleText.fontStyle = titleBold ? FontStyles.Bold : FontStyles.Normal;
            ConfigureHeadingText(panel.titleText);
        }

        if (panel.subText != null)
        {
            panel.subText.color = textSecondary;
            panel.subText.fontSize = subTextFontSize * globalFontScale;
            ConfigureHeadingText(panel.subText);
        }

        HashSet<LayoutGroup> touchedGroups = new HashSet<LayoutGroup>();
        HashSet<LayoutGroup> allRelevantGroups = new HashSet<LayoutGroup>();

        ApplyButtonList(panel.regularButtons, panel, isPrimary: false, touchedGroups);
        ApplyButtonList(panel.primaryButtons, panel, isPrimary: true, touchedGroups);
        ApplyToggleList(panel.toggles, panel);

        // Collect EVERY parent layout group among buttons+toggles on this
        // panel (not just ones FixLayoutGroup touched) so spacing is
        // unconditionally applied -- fixes "buttons too cramped" even
        // when FixLayoutGroup logic decided not to alter Force Expand.
        CollectParentGroups(panel.regularButtons, allRelevantGroups);
        CollectParentGroups(panel.primaryButtons, allRelevantGroups);

        foreach (var group in allRelevantGroups)
        {
            float spacingPx = Mathf.Max(ReferenceResolution.y * panel.buttonSpacingFraction, 28f); // hard floor

            if (group is VerticalLayoutGroup vGroup)
                vGroup.spacing = spacingPx;
            else if (group is HorizontalLayoutGroup hGroup)
                hGroup.spacing = Mathf.Max(ReferenceResolution.x * panel.buttonSpacingFraction, 28f);
        }
    }

    void CollectParentGroups(List<Button> buttons, HashSet<LayoutGroup> into)
    {
        foreach (var b in buttons)
        {
            if (b == null || b.transform.parent == null) continue;
            LayoutGroup g = b.transform.parent.GetComponent<LayoutGroup>();
            if (g != null) into.Add(g);
        }
    }

    bool IsEffectivelyFullScreen(Image background)
    {
        if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
        RectTransform canvasRect = targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : null;
        RectTransform bgRect = background.GetComponent<RectTransform>();
        if (canvasRect == null || bgRect == null) return false;

        bool stretchedBothAxes =
            Mathf.Approximately(bgRect.anchorMin.x, 0f) && Mathf.Approximately(bgRect.anchorMax.x, 1f) &&
            Mathf.Approximately(bgRect.anchorMin.y, 0f) && Mathf.Approximately(bgRect.anchorMax.y, 1f);

        if (stretchedBothAxes) return true;

        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 bgSize = bgRect.rect.size;
        if (canvasSize.x <= 0 || canvasSize.y <= 0) return false;

        return (bgSize.x / canvasSize.x) >= fullScreenThreshold && (bgSize.y / canvasSize.y) >= fullScreenThreshold;
    }

    void ApplyFlatBackground(Image background)
    {
        Transform t = background.transform;
        RemoveAllShadowChildren(t);

        background.sprite = null;
        background.type = Image.Type.Simple;
        background.color = screenBackground;
    }

    void RemoveAllShadowChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith("ClayShadow"))
                SafeDestroy(child.gameObject);
        }
    }

    void ConfigureHeadingText(TMP_Text text)
    {
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        Vector4 margin = text.margin;
        margin.x = Mathf.Max(margin.x, 32f);
        margin.z = Mathf.Max(margin.z, 32f);
        text.margin = margin;
    }

    // ── Cards ─────────────────────────────────────────────────────────

    void ApplyCardStyle(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = roundedSprite;
        img.type = Image.Type.Sliced;
        img.color = cardColor;

        RemoveAllShadowChildren(go.transform);

        if (useShadows)
        {
            float refHeight = ReferenceResolution.y;
            float offsetPx = refHeight * shadowOffsetFraction * 0.05f;
            BuildShadowChild(go.transform, go.name, lightShadowColor, new Vector2(-offsetPx, offsetPx), 0, "Light");
            BuildShadowChild(go.transform, go.name, darkShadowColor, new Vector2(offsetPx, -offsetPx), 1, "Dark");
        }
    }

    // ── Buttons ───────────────────────────────────────────────────────

    void ApplyButtonList(List<Button> buttons, PanelConfig panel, bool isPrimary, HashSet<LayoutGroup> fixedGroups)
    {
        foreach (var button in buttons)
        {
            if (button == null) continue;

            LayoutGroup parentLayoutGroup = button.transform.parent != null
                ? button.transform.parent.GetComponent<LayoutGroup>()
                : null;

            if (parentLayoutGroup != null && !fixedGroups.Contains(parentLayoutGroup))
            {
                FixLayoutGroup(parentLayoutGroup);
                fixedGroups.Add(parentLayoutGroup);
            }

            BuildClayButton(button, panel, isPrimary);
        }
    }

    void FixLayoutGroup(LayoutGroup group)
    {
        if (group is HorizontalOrVerticalLayoutGroup hvGroup)
        {
            hvGroup.childForceExpandWidth = false;
            hvGroup.childForceExpandHeight = false;
            hvGroup.childControlWidth = true;
            hvGroup.childControlHeight = true;
        }
        else if (group is GridLayoutGroup gridGroup)
        {
            gridGroup.childAlignment = TextAnchor.MiddleCenter;
        }
    }

    void BuildClayButton(Button button, PanelConfig panel, bool isPrimary)
    {
        GameObject go = button.gameObject;
        Vector2 refRes = ReferenceResolution;

        Vector2 computedSize;

        if (panel.lockManualSize)
        {
            // Don't touch sizing at all -- read whatever size is CURRENTLY
            // on this button (from its RectTransform or existing
            // LayoutElement) so shadows/grid spacing math below still has
            // a sane value to work with, without us overwriting anything.
            LayoutElement existingLE = go.GetComponent<LayoutElement>();
            RectTransform existingRT = go.GetComponent<RectTransform>();
            if (existingLE != null && existingLE.preferredWidth > 0)
                computedSize = new Vector2(existingLE.preferredWidth, existingLE.preferredHeight);
            else
                computedSize = existingRT.rect.size;
        }
        else
        {
            float sizeMultiplier = isPrimary ? 1.0f : 0.94f;
            float width = refRes.x * panel.buttonWidthFraction * sizeMultiplier;
            float height = refRes.y * panel.buttonHeightFraction * sizeMultiplier;
            computedSize = new Vector2(width, height);
        }

        Image mainImage = go.GetComponent<Image>();
        if (mainImage == null) mainImage = go.AddComponent<Image>();
        mainImage.sprite = roundedSprite;
        mainImage.type = Image.Type.Sliced;
        mainImage.color = isPrimary ? primaryButtonColor : cardColor;

        if (!panel.lockManualSize)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = computedSize.x;
            le.minHeight = le.preferredHeight = computedSize.y;

            bool insideLayoutGroup = go.transform.parent != null &&
                go.transform.parent.GetComponent<LayoutGroup>() != null;
            bool insideGridGroup = go.transform.parent != null &&
                go.transform.parent.GetComponent<GridLayoutGroup>() != null;

            if (insideGridGroup)
            {
                GridLayoutGroup grid = go.transform.parent.GetComponent<GridLayoutGroup>();
                grid.cellSize = computedSize;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = panel.gridColumns;
                grid.spacing = new Vector2(refRes.x * 0.03f, refRes.y * panel.buttonSpacingFraction);
            }
            else if (!insideLayoutGroup)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = computedSize;
            }
        }

        RemoveAllShadowChildren(go.transform);

        if (useShadows)
        {
            float offsetPx = computedSize.y * shadowOffsetFraction;
            BuildShadowChild(go.transform, go.name, lightShadowColor, new Vector2(-offsetPx, offsetPx), 0, "Light");
            BuildShadowChild(go.transform, go.name, darkShadowColor, new Vector2(offsetPx, -offsetPx), 1, "Dark");
        }

        TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = isPrimary ? textOnPrimary : textPrimary;

            float paddingPx = computedSize.x * buttonTextPaddingFraction;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(paddingPx, 4f);
            labelRect.offsetMax = new Vector2(-paddingPx, -4f);

            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;

            if (useAutoSizeForButtons)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = buttonFontSizeMin * globalFontScale;
                label.fontSizeMax = buttonFontSizeMax * globalFontScale;
            }
            else
            {
                label.enableAutoSizing = false;
                label.fontSize = buttonFontSizeMax * globalFontScale;
            }
        }
    }

    void BuildShadowChild(Transform parent, string ownerName, Color color, Vector2 offset, int siblingIndex, string variant)
    {
        string name = $"ClayShadow{variant}_OF_{ownerName}";
        Transform existing = parent.Find(name);
        GameObject shadowGO = existing != null ? existing.gameObject : null;

        if (shadowGO == null)
        {
            shadowGO = new GameObject(name, typeof(RectTransform));
            shadowGO.transform.SetParent(parent, false);
        }

        RectTransform rt = shadowGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = offset;
        rt.localScale = Vector3.one;

        Image img = shadowGO.GetComponent<Image>();
        if (img == null) img = shadowGO.AddComponent<Image>();
        img.sprite = roundedSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;

        LayoutElement shadowLE = shadowGO.GetComponent<LayoutElement>();
        if (shadowLE == null) shadowLE = shadowGO.AddComponent<LayoutElement>();
        shadowLE.ignoreLayout = true;

        shadowGO.transform.SetSiblingIndex(siblingIndex);
    }

    void SafeDestroy(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    // ── Toggles (fully destructive-safe rebuild) ────────────────────────

    void ApplyToggleList(List<Toggle> toggleList, PanelConfig panel)
    {
        foreach (var toggle in toggleList)
        {
            if (toggle == null) continue;
            BuildClayToggle(toggle, panel);
        }
    }

    readonly Dictionary<Toggle, UnityEngine.Events.UnityAction<bool>> toggleVisualListeners
        = new Dictionary<Toggle, UnityEngine.Events.UnityAction<bool>>();

    void BuildClayToggle(Toggle toggle, PanelConfig panel)
    {
        Color onColor = toggleOnUsesPrimaryColor ? primaryButtonColor : toggleOnColor;
        GameObject go = toggle.gameObject;
        Vector2 refRes = ReferenceResolution;

        RectTransform rootRect = go.GetComponent<RectTransform>();
        Vector2 computedSize;

        if (panel.lockManualSize)
        {
            // Read whatever size is already on this toggle's RectTransform
            // (or existing LayoutElement) instead of recomputing -- so the
            // destructive child-rebuild below still has a sane size for
            // positioning the knob, without us touching width/height.
            LayoutElement existingLE = go.GetComponent<LayoutElement>();
            if (existingLE != null && existingLE.preferredWidth > 0)
                computedSize = new Vector2(existingLE.preferredWidth, existingLE.preferredHeight);
            else
                computedSize = rootRect.rect.size;
        }
        else
        {
            computedSize = new Vector2(
                refRes.x * panel.toggleWidthFraction,
                refRes.y * panel.toggleHeightFraction);

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = computedSize.x;
            le.minHeight = le.preferredHeight = computedSize.y;

            bool insideLayoutGroup = go.transform.parent != null &&
                go.transform.parent.GetComponent<LayoutGroup>() != null;
            if (!insideLayoutGroup) rootRect.sizeDelta = computedSize;
        }

        // DESTRUCTIVE REBUILD: this is the fix for "Music doesn't work,
        // SFX does." Rather than trying to find-and-reuse whatever
        // Background/Checkmark/Knob children already exist (which can
        // silently fail if one toggle's children are named differently,
        // missing an Image component, or otherwise in a broken state),
        // we unconditionally DELETE every child of this toggle and
        // rebuild Background + Knob from scratch, every single Apply.
        // This guarantees both toggles end up byte-for-byte structurally
        // identical, with no possibility of asymmetric leftover state.
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(go.transform.GetChild(i).gameObject);
        }

        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(go.transform, false);
        RectTransform trackRect = bgGO.GetComponent<RectTransform>();
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        trackRect.localScale = Vector3.one;

        Image trackImage = bgGO.AddComponent<Image>();
        trackImage.sprite = roundedSprite;
        trackImage.type = Image.Type.Sliced;
        trackImage.color = toggle.isOn ? onColor : toggleOffColor;
        toggle.targetGraphic = trackImage;

        GameObject knobGO = new GameObject("Knob", typeof(RectTransform));
        knobGO.transform.SetParent(go.transform, false);

        Image knobImage = knobGO.AddComponent<Image>();
        knobImage.sprite = roundedSprite;
        knobImage.type = Image.Type.Sliced;
        knobImage.color = toggleKnobColor;
        knobImage.raycastTarget = false;
        toggle.graphic = knobImage;

        RectTransform knobRect = knobGO.GetComponent<RectTransform>();
        float knobDiameter = computedSize.y - (computedSize.y * 0.18f);
        knobRect.anchorMin = new Vector2(0f, 0.5f);
        knobRect.anchorMax = new Vector2(0f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(knobDiameter, knobDiameter);
        knobRect.localScale = Vector3.one;

        float inset = (computedSize.y * 0.09f) + knobDiameter / 2f;
        float onX = computedSize.x - inset;
        float offX = inset;
        knobRect.anchoredPosition = new Vector2(toggle.isOn ? onX : offX, 0f);

        knobGO.transform.SetAsLastSibling();

        // Toggle's own "isOn" boolean and any EXISTING persistent
        // (Inspector-wired) onValueChanged listeners are NOT touched by
        // this rebuild -- only child GameObjects were destroyed/recreated,
        // the Toggle component itself (and its event wiring) is untouched.

        Image capturedTrack = trackImage;
        RectTransform capturedKnobRect = knobRect;
        float capturedOnX = onX;
        float capturedOffX = offX;
        Color capturedOnColor = onColor;

        if (toggleVisualListeners.TryGetValue(toggle, out var previousListener))
        {
            toggle.onValueChanged.RemoveListener(previousListener);
        }

        UnityEngine.Events.UnityAction<bool> visualListener = (isOn) =>
        {
            capturedTrack.color = isOn ? capturedOnColor : toggleOffColor;
            capturedKnobRect.anchoredPosition = new Vector2(isOn ? capturedOnX : capturedOffX, 0f);
        };
        toggle.onValueChanged.AddListener(visualListener);
        toggleVisualListeners[toggle] = visualListener;
    }
}
}
