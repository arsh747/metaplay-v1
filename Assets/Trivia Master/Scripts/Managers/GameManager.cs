using UnityEngine;

namespace TriviaGame
{

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int maxLives = 3;
    public int currentLives;

    [HideInInspector]
    public string selectedCategory;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        currentLives = maxLives;
    }

    public void StartGame()
    {
        UIManager.Instance.ShowCategoryPanel();
    }

    public void SelectCategory(string category)
    {
        selectedCategory = category;
        currentLives = maxLives;

        TimerManager.Instance.StartTimer(15 * 60);
        UIManager.Instance.ShowQuestionPanel();
        QuestionManager.Instance.LoadCategory(category);
    }

    public void LoseLife()
    {
        currentLives--;

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        TimerManager.Instance.StopTimer();
        UIManager.Instance.ShowGameOver(
            QuestionManager.Instance.GetScore(),
            QuestionManager.Instance.GetTotalQuestions()
        );
    }

    public void CategoryCompleted(int score, int total)
    {
        TimerManager.Instance.StopTimer();
        UIManager.Instance.ShowCategoryComplete(score, total);
    }

    public void ReplayCategory()
    {
        SelectCategory(selectedCategory);
    }

        public void QuitGame()
        {
            SceneLoader.RequestReturnToHub();
        }
    }

}
