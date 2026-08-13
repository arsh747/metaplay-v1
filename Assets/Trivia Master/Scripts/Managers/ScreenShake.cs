using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace TriviaGame
{

/// <summary>
/// Shakes the UI Toolkit root element (instead of a uGUI RectTransform)
/// on a wrong answer. Uses VisualElement.style.translate, which UI
/// Toolkit renders as a cheap transform offset with no layout relayout.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance;

    public float duration = 0.2f;
    public float magnitude = 10f;

    VisualElement target;
    Coroutine running;

    void Awake()
    {
        Instance = this;
    }

    void EnsureTarget()
    {
        if (target != null) return;
        if (UIManager.Instance != null)
            target = UIManager.Instance.Root;
    }

    public void Shake()
    {
        EnsureTarget();
        if (target == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            target.style.translate = new Translate(x, y);
            elapsed += Time.deltaTime;

            yield return null;
        }

        target.style.translate = new Translate(0, 0);
        running = null;
    }
}

}
