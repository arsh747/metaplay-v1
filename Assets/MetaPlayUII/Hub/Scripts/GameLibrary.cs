using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameLibrary", menuName = "Hub/Game Library")]
public class GameLibrary : ScriptableObject
{
    public List<GameEntry> games = new List<GameEntry>();
}
