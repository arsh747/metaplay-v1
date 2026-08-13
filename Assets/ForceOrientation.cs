using UnityEngine;

public class ForceOrientation : MonoBehaviour
{
    public enum Mode { Portrait, Landscape }

    [Tooltip("Set this to match how THIS game should be played.")]
    [SerializeField] private Mode orientation = Mode.Portrait;

    void Awake()
    {
        if (orientation == Mode.Landscape)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }
        else
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }
    }
}