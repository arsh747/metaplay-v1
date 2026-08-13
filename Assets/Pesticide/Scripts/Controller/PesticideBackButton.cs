using UnityEngine;

/// <summary>
/// Local bridge for UI buttons in this scene to call the persistent
/// SceneLoader, which lives on a DontDestroyOnLoad object created in Boot
/// and therefore can't be dragged directly into a Button's OnClick() list.
/// </summary>
public class PesticideBackButton : MonoBehaviour
{
    public void OnBackPressed()
    {
        SceneLoader.RequestReturnToHub();
    }
}