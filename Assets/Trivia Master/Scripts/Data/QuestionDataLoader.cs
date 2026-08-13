using System;

namespace TriviaGame
{

[Serializable]
public class QuestionDatabase
{
    public Category[] categories;
    public QuestionData[] randomQuestions; // ADD THIS LINE
}

[Serializable]
public class Category
{
    public string name;
    public QuestionData[] questions;
}

[Serializable]
public class QuestionData
{
    public string questionText;
    public string[] answerChoices;
    public int correctAnswerIndex;
    public string difficulty;
}

[Serializable]
public class Question
{
    public string question;
    public string[] answers;
    public int correctAnswerIndex;
    public string category;
}
}
