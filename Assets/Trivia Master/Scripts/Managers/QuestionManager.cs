using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriviaGame
{

/// <summary>
/// Drives the question/answer gameplay screen. Rewritten for UI Toolkit:
/// pulls its Label/Button references out of UIManager's shared visual
/// tree (UIManager.Instance.Root) instead of Inspector-assigned
/// TextMeshProUGUI/Button fields.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    // ---------------- SINGLETON ----------------
    public static QuestionManager Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // ---------------- UI REFERENCES (resolved at Start) ----------------
    Label questionText;
    Button[] answerButtons;

    const string CorrectClass = "answer-btn--correct";
    const string WrongClass = "answer-btn--wrong";

    // ---------------- SETTINGS ----------------
    [Header("Question Settings")]
    public int questionsPerCategory = 10;

    [Tooltip("How long the correct/wrong answer flash stays on screen before the next question loads.")]
    public float answerFeedbackSeconds = 0.35f;

    // ---------------- INTERNAL STATE ----------------
    private List<Question> currentQuestions = new List<Question>();
    private Question currentQuestion;

    private int currentIndex = 0;
    private int score = 0;
    private string currentCategory;
    private bool inputLocked = false;

    void Start()
    {
        // Best-effort early resolve. Not relied upon for correctness --
        // Unity doesn't guarantee Start() order across different
        // GameObjects, so this can run before UIManager's. The real
        // guarantee comes from EnsureUIRefs() below, called again at the
        // top of LoadCategory(), which only ever fires after a button
        // click, i.e. long after every singleton has finished Awake/Start.
        EnsureUIRefs();
    }

    bool uiRefsResolved = false;

    void EnsureUIRefs()
    {
        if (uiRefsResolved) return;
        if (UIManager.Instance == null || UIManager.Instance.Root == null) return;

        VisualElement root = UIManager.Instance.Root;
        questionText = root.Q<Label>("QuestionText");

        answerButtons = new[]
        {
            root.Q<Button>("AnswerButton0"),
            root.Q<Button>("AnswerButton1"),
            root.Q<Button>("AnswerButton2"),
            root.Q<Button>("AnswerButton3"),
        };

        uiRefsResolved = questionText != null && answerButtons != null && answerButtons[0] != null;
    }

    // ---------------- PUBLIC API ----------------
    public void LoadCategory(string category)
    {
        EnsureUIRefs();

        if (!uiRefsResolved)
        {
            Debug.LogError("QuestionManager: could not resolve QuestionText/AnswerButton elements from UIManager.Root. " +
                "Check that the UIDocument's Source Asset is GameRoot.uxml and that UIManager has already run.");
            return;
        }

        currentCategory = category;
        currentIndex = 0;
        score = 0; // Reset score to 0
        inputLocked = false;

        LoadQuestionsFromJson();
        Shuffle(currentQuestions);

        // Limit to questionsPerCategory
        if (currentQuestions.Count > questionsPerCategory)
            currentQuestions = currentQuestions.GetRange(0, questionsPerCategory);

        Debug.Log("Final loaded questions: " + currentQuestions.Count + " for category: " + category);

        // Update UI displays immediately after loading
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScoreDisplay();
            UIManager.Instance.UpdateLivesDisplay();
        }

        ShowQuestion();
    }

    public int GetScore()
    {
        return score;
    }

    public int GetTotalQuestions()
    {
        return questionsPerCategory;
    }

    // ---------------- CORE LOGIC ----------------
    void LoadQuestionsFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/questions");

        if (jsonFile == null)
        {
            Debug.LogError("questions.json not found in Resources/Data/");
            return;
        }

        QuestionDatabase database = JsonUtility.FromJson<QuestionDatabase>(jsonFile.text);

        if (database == null)
        {
            Debug.LogError("Failed to parse JSON!");
            return;
        }

        currentQuestions.Clear();

        // Check if this is the "Random" category
        if (currentCategory.ToLower() == "random")
        {
            Debug.Log("Loading random questions...");

            if (database.randomQuestions == null || database.randomQuestions.Length == 0)
            {
                Debug.LogWarning("No random questions found in JSON - pooling one question from every category instead.");
                if (database.categories != null)
                {
                    foreach (Category cat in database.categories)
                    {
                        if (cat.questions == null) continue;
                        foreach (QuestionData qData in cat.questions)
                        {
                            currentQuestions.Add(ToQuestion(qData, cat.name));
                        }
                    }
                }
                return;
            }

            foreach (QuestionData qData in database.randomQuestions)
            {
                currentQuestions.Add(ToQuestion(qData, "Random"));
            }
        }
        else
        {
            // Load from categories
            if (database.categories == null)
            {
                Debug.LogError("Categories is null!");
                return;
            }

            bool categoryFound = false;
            foreach (Category cat in database.categories)
            {
                if (cat.name == currentCategory)
                {
                    categoryFound = true;

                    foreach (QuestionData qData in cat.questions)
                    {
                        currentQuestions.Add(ToQuestion(qData, currentCategory));
                    }
                    break;
                }
            }

            if (!categoryFound)
            {
                Debug.LogError("Category '" + currentCategory + "' not found!");
            }
        }

        Debug.Log("Total questions loaded: " + currentQuestions.Count);
    }

    Question ToQuestion(QuestionData qData, string category)
    {
        return new Question
        {
            question = qData.questionText,
            answers = qData.answerChoices,
            correctAnswerIndex = qData.correctAnswerIndex,
            category = category
        };
    }

    void ShowQuestion()
    {
        ClearAnswerFeedback();
        inputLocked = false;

        if (currentIndex >= currentQuestions.Count)
        {
            // All questions completed
            GameManager.Instance.CategoryCompleted(score, questionsPerCategory);
            return;
        }

        currentQuestion = currentQuestions[currentIndex];

        if (questionText == null)
        {
            Debug.LogError("QuestionManager: QuestionText label not found in UI tree!");
            return;
        }

        if (answerButtons == null || answerButtons.Length == 0)
        {
            Debug.LogError("QuestionManager: answer buttons not found in UI tree!");
            return;
        }

        // Set question text
        questionText.text = currentQuestion.question;

        // Set counter text
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateQuestionCounter((currentIndex + 1) + "/" + questionsPerCategory);

        // Setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
            {
                Debug.LogError("Answer button " + i + " is NULL!");
                continue;
            }

            int index = i;

            if (i < currentQuestion.answers.Length)
            {
                button.text = currentQuestion.answers[i];
                button.style.display = DisplayStyle.Flex;

                // Clear then (re)wire the click callback for this slot.
                button.clicked -= button.userData as System.Action;
                System.Action handler = () => AnswerSelected(index);
                button.userData = handler;
                button.clicked += handler;
            }
            else
            {
                button.style.display = DisplayStyle.None;
            }
        }
    }

    public void AnswerSelected(int selectedIndex)
    {
        if (inputLocked) return;
        inputLocked = true;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

        bool correct = selectedIndex == currentQuestion.correctAnswerIndex;

        if (correct)
        {
            score++;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCorrect();
            Debug.Log("Correct! Score: " + score);
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWrong();

            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake();
            }

            GameManager.Instance.LoseLife();
            Debug.Log("Wrong! Lives remaining: " + GameManager.Instance.currentLives);
        }

        ShowAnswerFeedback(selectedIndex, correct);

        // Update UI displays
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScoreDisplay();
            UIManager.Instance.UpdateLivesDisplay();
        }

        currentIndex++;
        StartCoroutine(AdvanceAfterFeedback());
    }

    IEnumerator AdvanceAfterFeedback()
    {
        yield return new WaitForSeconds(answerFeedbackSeconds);

        // GameOver may have already swapped the screen away (out of lives);
        // don't stomp the question panel state if that happened.
        if (GameManager.Instance != null && GameManager.Instance.currentLives <= 0)
            yield break;

        ShowQuestion();
    }

    void ShowAnswerFeedback(int selectedIndex, bool correct)
    {
        if (answerButtons == null) return;

        if (correct)
        {
            answerButtons[selectedIndex]?.AddToClassList(CorrectClass);
        }
        else
        {
            answerButtons[selectedIndex]?.AddToClassList(WrongClass);
            int correctIndex = currentQuestion.correctAnswerIndex;
            if (correctIndex >= 0 && correctIndex < answerButtons.Length)
                answerButtons[correctIndex]?.AddToClassList(CorrectClass);
        }
    }

    void ClearAnswerFeedback()
    {
        if (answerButtons == null) return;
        foreach (var button in answerButtons)
        {
            button?.RemoveFromClassList(CorrectClass);
            button?.RemoveFromClassList(WrongClass);
        }
    }

    // ---------------- UTILITIES ----------------
    void Shuffle(List<Question> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            Question temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}

}
