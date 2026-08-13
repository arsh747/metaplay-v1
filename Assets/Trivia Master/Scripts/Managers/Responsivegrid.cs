using UnityEngine;
using UnityEngine.UI;

namespace TriviaGame
{

/// <summary>
/// Makes a GridLayoutGroup's cell size responsive to the actual rendered
/// width of its RectTransform, instead of using a hardcoded pixel Cell Size
/// that breaks on different aspect ratios.
///
/// === SETUP ===
/// 1. Attach to the same GameObject as the GridLayoutGroup (AnswerGrid).
/// 2. Make sure AnswerGrid's RectTransform is STRETCHED (anchor min 0,0 �
///    max 1,1 relative to its parent), NOT a single point anchor.
///    See "Fix AnswerGrid anchor" note below � this script will also
///    force-correct it at runtime as a safety net.
/// 3. Set [columns] and [rows] to match your layout (2x2 by default).
/// 4. Set [cellAspect] to control button shape (width/height ratio).
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGrid : MonoBehaviour
{
    [Header("Grid Shape")]
    public int columns = 2;
    public int rows = 2;

    [Tooltip("Width / Height ratio of each cell. 3.2 = wide button, 1 = square.")]
    public float cellAspect = 3.2f;

    [Tooltip("Spacing between cells, in pixels (reference resolution units).")]
    public Vector2 spacing = new Vector2(40, 30);

    [Tooltip("Outer padding inside the grid container.")]
    public RectOffset padding;

    [Header("Container Anchor (applied at runtime)")]
    [Tooltip("Stretched anchor box this grid's RectTransform should occupy " +
             "within its parent. Defaults work for a bottom answer-grid; " +
             "for a vertically-centered category list use e.g. " +
             "Min(0,0.05) Max(1,0.85).")]
    public Vector2 anchorMin = new Vector2(0f, 0f);
    public Vector2 anchorMax = new Vector2(1f, 0.4f);

    [Tooltip("Inner padding from the anchor box edges (Left, Bottom, Right, Top).")]
    public Vector4 insetPadding = new Vector4(20, 20, 20, 20);

    RectTransform rect;
    GridLayoutGroup grid;
    Vector2 lastSize;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();

        // Safety net: force correct stretched anchors + reset any bad
        // manual offset values (e.g. a stray fixed-point anchor leftover
        // from manual editing) so the grid always sits where intended,
        // scaling with the actual screen size instead of a hardcoded box.
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(insetPadding.x, insetPadding.y);   // Left, Bottom
        rect.offsetMax = new Vector2(-insetPadding.z, -insetPadding.w); // -Right, -Top
        rect.anchoredPosition = Vector2.zero;

        grid.padding = padding ?? new RectOffset(20, 20, 10, 10);
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        ApplyCellSize();
    }

    void Update()
    {
        // Re-fit if the rect's actual rendered size changes
        // (rotation, different device, resolution change)
        Vector2 currentSize = rect.rect.size;
        if (currentSize != lastSize)
        {
            ApplyCellSize();
        }
    }

    void ApplyCellSize()
    {
        Vector2 size = rect.rect.size;
        lastSize = size;

        if (size.x <= 0 || size.y <= 0) return;

        float totalSpacingX = spacing.x * (columns - 1) + grid.padding.left + grid.padding.right;
        float totalSpacingY = spacing.y * (rows - 1) + grid.padding.top + grid.padding.bottom;

        float cellWidth = (size.x - totalSpacingX) / columns;
        float cellHeight = cellWidth / cellAspect;

        // Make sure total height fits within available space;
        // if not, recompute from height instead.
        float totalHeight = cellHeight * rows + totalSpacingY;
        if (totalHeight > size.y)
        {
            cellHeight = (size.y - totalSpacingY) / rows;
            cellWidth = cellHeight * cellAspect;
        }

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
}
