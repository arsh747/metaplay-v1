using UnityEngine;

namespace TriviaGame
{

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;

    public float timeLeft;
    public bool isRunning;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!isRunning) return;

        timeLeft -= Time.deltaTime;
        UIManager.Instance.UpdateTimerDisplay(FormatTime(timeLeft));

        if (timeLeft <= 0)
        {
            timeLeft = 0;
            isRunning = false;

            UIManager.Instance.ShowGameOver(
                QuestionManager.Instance.GetScore(),
                QuestionManager.Instance.GetTotalQuestions()
            );
        }
    }

    public void StartTimer(float time)
    {
        timeLeft = time;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public bool TimeUp()
    {
        return timeLeft <= 0;
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60);
        int s = Mathf.FloorToInt(t % 60);
        return m.ToString("00") + ":" + s.ToString("00");
    }
}

}
