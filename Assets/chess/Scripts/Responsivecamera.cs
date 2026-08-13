using UnityEngine;

namespace ChessGame
{

/// <summary>
/// Automatically fits the orthographic camera so the chess board
/// fills the screen responsively on any device / simulator resolution.
///
/// === SETUP ===
/// 1. Add this script to your Main Camera.
/// 2. Make sure the Camera is set to Projection: Orthographic.
/// 3. Set [boardWorldSize] to match your board's world-unit width (default 8).
/// 4. [paddingPercent] adds breathing room around the board (0 = edge-to-edge).
/// </summary>
[RequireComponent(typeof(Camera))]
public class ResponsiveCamera : MonoBehaviour
{
    [Header("Board Settings")]
    [Tooltip("The world-unit width/height of the chess board. " +
             "Default board uses 8 units (squares are 1x1 each).")]
    public float boardWorldSize = 8f;

    [Tooltip("Extra padding around the board as a fraction of board size. " +
             "0.05 = 5% padding on each side.")]
    [Range(0f, 0.5f)]
    public float paddingPercent = 0.05f;

    [Header("Fit Mode")]
    [Tooltip("Fit to WIDTH (board always full-width) or HEIGHT, or AUTO " +
             "(picks whichever keeps the whole board visible).")]
    public FitMode fitMode = FitMode.Auto;

    public enum FitMode { Auto, Width, Height }

    Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    private void Update()
    {
        // Re-fit if resolution changes (e.g. rotating device / resizing simulator)
        Fit();
    }

    void Fit()
    {
        if (cam == null || !cam.orthographic) return;

        float paddedSize = boardWorldSize * (1f + paddingPercent * 2f);

        float screenAspect = (float)Screen.width / Screen.height;

        // Half-height needed to show the board edge-to-edge vertically
        float fitHeight = paddedSize / 2f;

        // Half-height needed so the board fits horizontally
        // orthographicSize controls half-height; width = size * aspect
        // So to fit a given world-width: size = worldWidth / (2 * aspect)
        float fitWidth = paddedSize / (2f * screenAspect);

        switch (fitMode)
        {
            case FitMode.Height:
                cam.orthographicSize = fitHeight;
                break;
            case FitMode.Width:
                cam.orthographicSize = fitWidth;
                break;
            case FitMode.Auto:
            default:
                // Use whichever is larger � ensures the whole board is always visible
                cam.orthographicSize = Mathf.Max(fitHeight, fitWidth);
                break;
        }
    }

#if UNITY_EDITOR
    // Refresh in Edit mode when inspector values change
    private void OnValidate()
    {
        cam = GetComponent<Camera>();
        Fit();
    }
#endif
}
}
