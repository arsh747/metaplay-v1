using UnityEngine;

[System.Serializable]
public class GameEntry
{
    [Tooltip("Unique key, e.g. \"chess\". Internal only, never shown to the player.")]
    public string id;

    [Tooltip("Shown on the card, e.g. \"CHESS\" or \"RACING RIVAL\".")]
    public string displayName;

    [Tooltip("Exact scene name — must be added to Build Settings.")]
    public string sceneName;

    [Tooltip("Icon shown on the card. TO CHANGE A CARD'S IMAGE LATER: just drag a new PNG/texture in here, nothing else needs to change.")]
    public Texture2D icon;

    [Tooltip("Card background color (matches the flat color block behind the icon).")]
    public Color cardColor = new Color(0.16f, 0.14f, 0.13f);

    [Tooltip("Title text + underline color for this card.")]
    public Color accentColor = Color.white;

    [Tooltip("If true, the card shows a lock overlay and can't be tapped.")]
    public bool isLocked;
}
